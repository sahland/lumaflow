#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewSceneSync {
        static UxmlPreviewSceneSync() {
            AssemblyReloadEvents.beforeAssemblyReload += StopLivePreviews;
            EditorApplication.quitting += StopLivePreviews;
            EditorApplication.hierarchyChanged += CleanupLivePreviews;
            EditorSceneManager.sceneClosing += (scene, _) => {
                foreach (var document in Live.Keys.Where(doc => doc == null || doc.gameObject.scene == scene).ToArray())
                    StopLive(document);
            };
            EditorSceneManager.sceneOpened += (_, __) => Schedule();
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingEditMode) RestoreRuntimeTrees();
                if (state == PlayModeStateChange.EnteredEditMode) Schedule();
            };
            Schedule();
        }
        private static bool _queued;
        private static readonly Dictionary<UIDocument, (MountHandle Mount, VisualElement Host)> Live = new();
        internal static int LivePreviewCount => Live.Count;
        internal static IEnumerable<UIDocument> LiveDocuments => Live.Keys;
        private static string GamePreviewKey => UxmlPreviewStartup.PreferenceKey + ".GamePreview";
        internal static bool GamePreviewEnabled {
            get => EditorPrefs.GetBool(GamePreviewKey, true);
            set {
                EditorPrefs.SetBool(GamePreviewKey, value);
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (value) Schedule(); else RestoreRuntimeTrees();
            }
        }
        private static UxmlPreviewBinding[] Bindings() => Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt).Where(scene => scene.isLoaded)
            .SelectMany(scene => scene.GetRootGameObjects())
            .SelectMany(root => root.GetComponentsInChildren<UxmlPreviewBinding>(true)).ToArray();
        private static void Schedule() {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += Refresh;
        }
        private static void Refresh() {
            _queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!GamePreviewEnabled) { RestoreRuntimeTrees(); return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            foreach (var binding in Bindings()) {
                if (EditorUtility.IsPersistent(binding) || !binding.gameObject.scene.IsValid()) continue;
                try {
                    VisualTreeAsset tree;
                    if (binding.SourceId.StartsWith("code:")) {
                        if (binding.Bootstrap != null && UxmlPreviewSceneSetup.FindFactory(binding.Bootstrap.GetType()) is { } method)
                            LumaPreviewRegistry.RegisterBootstrap(method);
                        var factory = LumaPreviewRegistry.Find(binding.SourceId.Substring(5));
                        if (factory == null) continue;
                        try { tree = UxmlPreviewGenerator.Generate(factory, out _); }
                        catch (NotSupportedException) {
                            MountLive(binding.Document, factory.CreateWidget);
                            continue;
                        }
                    } else {
                        var definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(AssetDatabase.GUIDToAssetPath(binding.SourceId.Substring(6)));
                        if (definition == null || !definition.AutoGenerate) continue;
                        try { tree = UxmlPreviewGenerator.Generate(definition, out _); }
                        catch (NotSupportedException) {
                            MountLive(binding.Document, definition.CreatePreviewWidget);
                            continue;
                        }
                    }
                    StopLive(binding.Document);
                    binding.Document.visualTreeAsset = tree;
                } catch (Exception exception) { Debug.LogError("LumaFlow scene preview: " + exception.Message, binding); }
            }
        }
        internal static void Apply(string sourceId, VisualTreeAsset tree) {
            if (!GamePreviewEnabled || EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var binding in Bindings())
                if (!EditorUtility.IsPersistent(binding) && binding.SourceId == sourceId) {
                    StopLive(binding.Document);
                    if (binding.Document.visualTreeAsset != tree) binding.Document.visualTreeAsset = tree;
                }
        }
        internal static void ApplyLive(string sourceId, Func<Widget> factory) {
            if (!GamePreviewEnabled || EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var binding in Bindings())
                if (!EditorUtility.IsPersistent(binding) && binding.SourceId == sourceId)
                    MountLive(binding.Document, factory);
        }
        private static void MountLive(UIDocument document, Func<Widget> factory) {
            // Prepare detached first: a failing factory must not erase a working preview.
            var host = new VisualElement { name = "lumaflow-live-preview" };
            var mount = UxmlPreviewLiveSnapshot.Mount(factory(), host, UxmlPreviewViewport.GetGameViewSize());
            try {
                StopLive(document);
                document.visualTreeAsset = null;
                document.rootVisualElement.Clear();
                document.rootVisualElement.Add(host);
                Live.Add(document, (mount, host));
                UxmlPreviewGameInput.Start();
            } catch { mount.Dispose(); throw; }
        }
        private static void StopLive(UIDocument document) {
            if (!Live.TryGetValue(document, out var live)) return;
            Live.Remove(document);
            live.Mount.Dispose();
            live.Host.RemoveFromHierarchy();
            if (Live.Count == 0) UxmlPreviewGameInput.Stop();
        }
        private static void StopLivePreviews() {
            foreach (var document in Live.Keys.ToArray()) StopLive(document);
        }
        private static void CleanupLivePreviews() {
            foreach (var document in Live.Keys.Where(doc => doc == null || doc.GetComponent<UxmlPreviewBinding>() == null).ToArray())
                StopLive(document);
        }
        internal static void RestoreRuntimeTrees() {
            StopLivePreviews();
            foreach (var binding in Bindings())
                if (!EditorUtility.IsPersistent(binding)) binding.Document.visualTreeAsset = binding.RuntimeTree;
        }
    }

    // Game View normally queues input for the player loop. In Edit Mode forward
    // editor-panel events only to connected live preview panels, never to gameplay.
    internal static class UxmlPreviewGameInput {
        private const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        private static readonly Dictionary<EditorWindow, Action> Windows = new();
        private static bool _active;
        private static double _nextScan;
        private static readonly System.Reflection.FieldInfo? GlobalEvents = typeof(EditorApplication).GetField("globalEventHandler", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        private static Delegate? _globalHandler;
        internal static void Start() {
            if (_active) return;
            _active = true;
            if (GlobalEvents != null) {
                _globalHandler = Delegate.CreateDelegate(GlobalEvents.FieldType, typeof(UxmlPreviewGameInput).GetMethod(nameof(ForwardGlobal), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!);
                GlobalEvents.SetValue(null, Delegate.Combine(GlobalEvents.GetValue(null) as Delegate, _globalHandler));
            }
            EditorApplication.update += Scan;
            Scan();
        }
        internal static void Stop() {
            if (GlobalEvents != null && _globalHandler != null)
                GlobalEvents.SetValue(null, Delegate.Remove(GlobalEvents.GetValue(null) as Delegate, _globalHandler));
            _globalHandler = null;
            EditorApplication.update -= Scan;
            foreach (var remove in Windows.Values) remove();
            Windows.Clear(); _active = false; _nextScan = 0;
        }
        private static void Scan() {
            if (EditorApplication.timeSinceStartup < _nextScan) return;
            _nextScan = EditorApplication.timeSinceStartup + .5;
            foreach (var closed in Windows.Keys.Where(window => window == null).ToArray()) {
                Windows[closed](); Windows.Remove(closed);
            }
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>()) {
                if (window.GetType().FullName != "UnityEditor.GameView" || Windows.ContainsKey(window)) continue;
                var root = window.rootVisualElement;
                EventCallback<PointerDownEvent> down = evt => Forward(window, evt, EventType.MouseDown);
                EventCallback<PointerUpEvent> up = evt => Forward(window, evt, EventType.MouseUp);
                EventCallback<PointerMoveEvent> move = evt => Forward(window, evt, EventType.MouseMove);
                EventCallback<WheelEvent> wheel = evt => ForwardWheel(window, evt);
                EventCallback<KeyDownEvent> key = evt => ForwardKey(window, evt);
                root.RegisterCallback(down, TrickleDown.TrickleDown);
                root.RegisterCallback(up, TrickleDown.TrickleDown);
                root.RegisterCallback(move, TrickleDown.TrickleDown);
                root.RegisterCallback(wheel, TrickleDown.TrickleDown);
                root.RegisterCallback(key, TrickleDown.TrickleDown);
                Windows.Add(window, () => {
                    if (root.HasPointerCapture(PointerId.mousePointerId)) root.ReleasePointer(PointerId.mousePointerId);
                    root.UnregisterCallback(down, TrickleDown.TrickleDown);
                    root.UnregisterCallback(up, TrickleDown.TrickleDown);
                    root.UnregisterCallback(move, TrickleDown.TrickleDown);
                    root.UnregisterCallback(wheel, TrickleDown.TrickleDown);
                    root.UnregisterCallback(key, TrickleDown.TrickleDown);
                });
            }
        }
        private static bool Resolve(EditorWindow window, Vector2 position, out IPanel panel, out VisualElement target, out Vector2 point) {
            panel = null!; target = null!; point = default;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            var type = window.GetType();
            if (!(type.GetProperty("targetInView", Hidden)?.GetValue(window) is Rect rect)
                || !(type.GetProperty("viewInWindow", Hidden)?.GetValue(window) is Rect view)) return false;
            rect.position += view.position;
            var local = window.rootVisualElement.WorldToLocal(position);
            if (rect.width <= 0 || rect.height <= 0) return false;
            foreach (var document in UxmlPreviewSceneSync.LiveDocuments.OrderByDescending(item => item.sortingOrder)) {
                if (!document.isActiveAndEnabled || document.rootVisualElement.Q("lumaflow-live-preview") == null) continue;
                if (type.GetProperty("targetDisplay", Hidden)?.GetValue(window) is int display
                    && document.panelSettings != null && document.panelSettings.targetDisplay != display) continue;
                var candidate = document.rootVisualElement.panel;
                if (candidate == null) continue;
                var capture = candidate.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
                // Navigation can remove the pressed button during its own MouseUp.
                // Do not send the next click to a detached, stale capture owner.
                if (capture != null && (capture.panel != candidate || !candidate.visualTree.Contains(capture))) {
                    candidate.ReleasePointer(PointerId.mousePointerId);
                    capture = null;
                }
                if (!rect.Contains(local) && capture == null) continue;
                var bounds = candidate.visualTree.worldBound;
                point = new Vector2((local.x - rect.x) / rect.width * bounds.width + bounds.x,
                    (local.y - rect.y) / rect.height * bounds.height + bounds.y);
                var picked = capture ?? candidate.Pick(point);
                if (picked == null) continue;
                panel = candidate; target = picked; return true;
            }
            return false;
        }
        private static void Forward<T>(EditorWindow window, PointerEventBase<T> evt, EventType type) where T : PointerEventBase<T>, new() {
            if (!Resolve(window, evt.position, out _, out var target, out var point)) return;
            var system = new Event { type = type, mousePosition = point, button = Mathf.Max(0, evt.button), clickCount = evt.clickCount, modifiers = evt.modifiers };
            using EventBase forwarded = type == EventType.MouseDown ? PointerDownEvent.GetPooled(system)
                : type == EventType.MouseUp ? PointerUpEvent.GetPooled(system) : PointerMoveEvent.GetPooled(system);
            forwarded.target = target;
            target.SendEvent(forwarded);
            if (type == EventType.MouseDown) window.rootVisualElement.CapturePointer(evt.pointerId);
            if (type == EventType.MouseUp) window.rootVisualElement.ReleasePointer(evt.pointerId);
            evt.StopImmediatePropagation();
            evt.imguiEvent?.Use();
            window.Repaint();
        }
        private static void ForwardGlobal() {
            var system = Event.current;
            if (system == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var guiViewType = Type.GetType("UnityEditor.GUIView,UnityEditor");
            var current = guiViewType?.GetProperty("current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(null);
            var window = current?.GetType().GetProperty("actualView", Hidden)?.GetValue(current) as EditorWindow;
            if (window == null || window.GetType().FullName != "UnityEditor.GameView") return;
            if (system.type != EventType.MouseDown && system.type != EventType.MouseUp
                && system.type != EventType.MouseMove && system.type != EventType.MouseDrag && system.type != EventType.ScrollWheel) return;
            if (!Resolve(window, window.rootVisualElement.LocalToWorld(system.mousePosition), out _, out var target, out var point)) return;
            var copy = new Event(system) { mousePosition = point };
            using EventBase forwarded = system.type == EventType.MouseDown ? PointerDownEvent.GetPooled(copy)
                : system.type == EventType.MouseUp ? PointerUpEvent.GetPooled(copy)
                : system.type == EventType.ScrollWheel ? WheelEvent.GetPooled(copy) : PointerMoveEvent.GetPooled(copy);
            forwarded.target = target; target.SendEvent(forwarded);
            if (system.type == EventType.MouseDown) window.rootVisualElement.CapturePointer(PointerId.mousePointerId);
            if (system.type == EventType.MouseUp) window.rootVisualElement.ReleasePointer(PointerId.mousePointerId);
            system.Use(); window.Repaint();
        }
        private static void ForwardWheel(EditorWindow window, WheelEvent evt) {
            if (!Resolve(window, evt.mousePosition, out _, out var target, out var point)) return;
            using var forwarded = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, mousePosition = point, delta = evt.delta, modifiers = evt.modifiers });
            forwarded.target = target; target.SendEvent(forwarded);
            evt.StopImmediatePropagation(); window.Repaint();
        }
        private static void ForwardKey(EditorWindow window, KeyDownEvent evt) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var document in UxmlPreviewSceneSync.LiveDocuments) {
                if (!(document.rootVisualElement.panel?.focusController.focusedElement is VisualElement target)) continue;
                using var forwarded = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = evt.keyCode, character = evt.character, modifiers = evt.modifiers });
                forwarded.target = target; target.SendEvent(forwarded);
                evt.StopImmediatePropagation(); window.Repaint(); return;
            }
        }
    }

    // Deliberately not UXML: preserve custom drawing and procedural textures exactly.
    // Only the supplied pure preview factory runs, never the scene's MonoBehaviour lifecycle.
    internal static class UxmlPreviewLiveSnapshot {
        internal static MountHandle Mount(Widget widget, VisualElement host, Vector2 viewport) {
            host.style.width = Length.Percent(100);
            host.style.height = Length.Percent(100);
            host.style.flexGrow = 1;
            using var snapshot = new AnimationSnapshotScope();
            var preview = new MediaQuery(new MediaQueryData(Mathf.Max(1, viewport.x),
                Mathf.Max(1, viewport.y), disableAnimations: true), widget);
            return global::LumaFlow.LumaFlow.Mount(preview, host);
        }
    }
}
