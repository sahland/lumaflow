#if UNITY_6000_4_OR_NEWER
#nullable enable

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

using Framework = LumaFlow.LumaFlow;

namespace LumaFlow.Editor {

    /// <summary>
    /// Connects one active PlayMode application to the LumaFlow controls shown
    /// immediately after Unity's Play, Pause, and Step group.
    /// </summary>
    public static class PlayModePreviewToolbar {
        private const string ContentName = "lumaflow-playmode-preview-toolbar";
        private static readonly State<bool> Availability = new(false);
        private static Action? _reload;
        private static Action? _restart;
        private static int _registrationId;

        [InitializeOnLoadMethod]
        private static void ScheduleInstallation() {
            CleanupLegacyInPlaceCompilationState();
            CompileRestartCoordinator.Initialize();
            EditorApplication.playModeStateChanged -= ClearPreviewRegistration;
            EditorApplication.playModeStateChanged += ClearPreviewRegistration;
            EditorApplication.update -= TryInstall;
            EditorApplication.update += TryInstall;
        }

        // Domain reload can be disabled, so a registration from the previous
        // Play Mode session must not retain a destroyed MonoBehaviour delegate.
        private static void ClearPreviewRegistration(PlayModeStateChange state) {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            _reload = null;
            _restart = null;
            Availability.Value = false;
            unchecked { ++_registrationId; }
        }

        private static void CleanupLegacyInPlaceCompilationState() {
            const string restoreKey = "LumaFlow.PlayModePreview.RestoreScriptCompilationPreference";
            const string previousKey = "LumaFlow.PlayModePreview.PreviousScriptCompilationPreference";
            if (SessionState.GetBool(restoreKey, false)) {
                EditorPrefs.SetInt(
                    "ScriptCompilationDuringPlay",
                    SessionState.GetInt(previousKey, 0));
            }

            SessionState.EraseBool(restoreKey);
            SessionState.EraseInt(previousKey);
            SessionState.EraseInt("LumaFlow.PlayModePreview.PendingCodeRefreshAction");
            SessionState.EraseString("LumaFlow.PlayModePreview.PendingHostInstanceId");
        }

        /// <summary>Registers the currently active preview lifecycle callbacks.</summary>
        public static IDisposable Register(Action reload, Action restart) {
            if (reload is null) throw new ArgumentNullException(nameof(reload));
            if (restart is null) throw new ArgumentNullException(nameof(restart));

            var id = unchecked(++_registrationId);
            _reload = reload;
            _restart = restart;
            Availability.Value = true;
            return new Registration(id);
        }

        private static void TryInstall() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var toolbarWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(window => window.GetType().FullName == "UnityEditor.MainToolbarWindow");
            if (toolbarWindow is null) return;

            var root = toolbarWindow.rootVisualElement;
            root.Q<VisualElement>(ContentName)?.RemoveFromHierarchy();

            var playModeGroup = FindPlayModeGroup(root);
            if (playModeGroup is null) return;

            var content = new PreviewToolbarContent(root, playModeGroup);
            root.Add(content);
            content.BringToFront();
            EditorApplication.update -= TryInstall;
        }

        private static VisualElement? FindPlayModeGroup(VisualElement root) {
            var elements = root.Query<VisualElement>().ToList();
            var play = FindToolbarControl(elements, "Play");
            var pause = FindToolbarControl(elements, "Pause");
            var step = FindToolbarControl(elements, "Step");
            if (play is not null && pause is not null && step is not null) {
                for (var ancestor = play; ancestor is not null; ancestor = ancestor.parent) {
                    if ((ReferenceEquals(ancestor, pause) || ancestor.Contains(pause))
                        && (ReferenceEquals(ancestor, step) || ancestor.Contains(step))) {
                        return ancestor;
                    }
                }
            }

            return elements.FirstOrDefault(element =>
                Marker(element).IndexOf("PlayModeButtons", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static VisualElement? FindToolbarControl(
            System.Collections.Generic.IEnumerable<VisualElement> elements,
            string token) {
            return elements.FirstOrDefault(element => {
                var typeName = element.GetType().Name;
                var interactive = element is UnityEngine.UIElements.Button
                    || element is UnityEngine.UIElements.Toggle
                    || typeName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Toggle", StringComparison.OrdinalIgnoreCase) >= 0;
                return interactive
                    && Marker(element).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            });
        }

        private static string Marker(VisualElement element) => string.Join(" ", new[] {
            element.name ?? string.Empty,
            element.tooltip ?? string.Empty,
            element.GetType().FullName ?? element.GetType().Name,
            string.Join(" ", element.GetClasses())
        });

        private static Widget BuildControls(bool available) {
            var normal = EditorGUIUtility.isProSkin
                ? new Color32(73, 62, 88, 255)
                : new Color32(205, 205, 210, 255);
            var foreground = EditorGUIUtility.isProSkin
                ? new Color32(215, 208, 228, 255)
                : new Color32(55, 55, 60, 255);
            var hovered = EditorGUIUtility.isProSkin
                ? new Color32(91, 77, 108, 255)
                : new Color32(220, 220, 225, 255);
            var pressed = EditorGUIUtility.isProSkin
                ? new Color32(104, 86, 124, 255)
                : new Color32(185, 185, 192, 255);
            var style = new ButtonStyle(
                background: normal,
                foreground: foreground,
                shape: BorderRadius.All(3f),
                hovered: new ButtonStateStyle(background: hovered),
                pressed: new ButtonStateStyle(background: pressed),
                disabled: new ButtonStateStyle(
                    background: new Color(normal.r, normal.g, normal.b, 0.45f),
                    foreground: new Color(foreground.r, foreground.g, foreground.b, 0.35f)));

            return new Row(
                new Widget[] {
                    new IconButton(
                        LumaIcons.Refresh,
                        InvokeReload,
                        tooltip: "LumaFlow Reassemble (no C# compilation)",
                        enabled: available,
                        hitSize: 24f,
                        style: style),
                    new IconButton(
                        LumaIcons.Redo,
                        InvokeRestart,
                        tooltip: "Compile scripts and restart Play Mode",
                        enabled: available,
                        hitSize: 24f,
                        style: style)
                },
                gap: 2f,
                crossAxisAlignment: CrossAxisAlignment.Center);
        }

        private static void InvokeReload() => Invoke(_reload);

        private static void InvokeRestart() => CompileRestartCoordinator.Request();

        private static void Invoke(Action? action) {
            if (action is null) return;
            try {
                action();
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }

        private static void Unregister(int id) {
            if (id != _registrationId) return;
            _reload = null;
            _restart = null;
            Availability.Value = false;
        }

        private static class CompileRestartCoordinator {
            private const string PhaseKey = "LumaFlow.PlayModePreview.CompileRestartPhase";
            private const double QuietPeriodSeconds = 1.5d;
            private static double _notBusySince;

            internal static void Initialize() {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.update -= Pump;
                EditorApplication.update += Pump;
                _notBusySince = EditorApplication.timeSinceStartup;
            }

            internal static void Request() {
                if (!EditorApplication.isPlaying) return;
                SessionState.SetInt(PhaseKey, (int)Phase.ExitRequested);
                EditorApplication.ExitPlaymode();
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state) {
                var phase = CurrentPhase;
                if (state == PlayModeStateChange.EnteredEditMode && phase == Phase.ExitRequested) {
                    SessionState.SetInt(PhaseKey, (int)Phase.CompileRequested);
                    _notBusySince = EditorApplication.timeSinceStartup;
                    EditorApplication.delayCall += RequestCompilation;
                } else if (state == PlayModeStateChange.EnteredPlayMode && phase == Phase.EnterRequested) {
                    Clear();
                }
            }

            private static void RequestCompilation() {
                if (CurrentPhase != Phase.CompileRequested || EditorApplication.isPlaying) return;
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                CompilationPipeline.RequestScriptCompilation();
                _notBusySince = EditorApplication.timeSinceStartup;
            }

            private static void Pump() {
                if (CurrentPhase != Phase.CompileRequested) return;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                    _notBusySince = EditorApplication.timeSinceStartup;
                    return;
                }

                if (EditorApplication.timeSinceStartup - _notBusySince < QuietPeriodSeconds) return;
                if (EditorUtility.scriptCompilationFailed) {
                    Clear();
                    Debug.LogError("LumaFlow Compile & Restart was cancelled because script compilation failed.");
                    return;
                }

                SessionState.SetInt(PhaseKey, (int)Phase.EnterRequested);
                EditorApplication.EnterPlaymode();
            }

            private static Phase CurrentPhase =>
                (Phase)SessionState.GetInt(PhaseKey, (int)Phase.None);

            private static void Clear() => SessionState.EraseInt(PhaseKey);

            private enum Phase {
                None,
                ExitRequested,
                CompileRequested,
                EnterRequested
            }
        }

        private sealed class Registration : IDisposable {
            private readonly int _id;
            private bool _disposed;

            public Registration(int id) => _id = id;

            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                Unregister(_id);
            }
        }

        private sealed class PreviewToolbarContent : VisualElement {
            private readonly VisualElement _toolbarRoot;
            private readonly VisualElement _playModeGroup;
            private MountHandle? _mount;

            public PreviewToolbarContent(VisualElement toolbarRoot, VisualElement playModeGroup) {
                _toolbarRoot = toolbarRoot;
                _playModeGroup = playModeGroup;
                name = ContentName;
                style.position = Position.Absolute;
                style.flexDirection = FlexDirection.Row;
                style.flexGrow = 0f;
                style.flexShrink = 0f;
                style.width = 50f;
                style.height = 24f;
                style.marginLeft = 4f;
                style.alignSelf = UnityEngine.UIElements.Align.Center;
                RegisterCallback<AttachToPanelEvent>(OnAttached);
                RegisterCallback<DetachFromPanelEvent>(OnDetached);
                _toolbarRoot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _playModeGroup.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            private void OnAttached(AttachToPanelEvent _) {
                PositionBesidePlayModeGroup();
                if (_mount?.IsMounted == true) return;
                _mount = Framework.Mount(
                    new ReactiveBuilder<bool>(Availability, BuildControls),
                    this);
                var mountHost = this.Q<VisualElement>("lumaflow-mount-host");
                if (mountHost is null) return;
                mountHost.style.flexGrow = 0f;
                mountHost.style.flexShrink = 0f;
                mountHost.style.width = 50f;
                mountHost.style.height = 24f;
            }

            private void OnGeometryChanged(GeometryChangedEvent _) => PositionBesidePlayModeGroup();

            private void PositionBesidePlayModeGroup() {
                if (panel is null || _playModeGroup.panel is null) return;
                var anchor = _playModeGroup.worldBound;
                var rootOrigin = _toolbarRoot.worldBound.position;
                style.left = anchor.xMax - rootOrigin.x + 4f;
                style.top = anchor.center.y - rootOrigin.y - 12f;
            }

            private void OnDetached(DetachFromPanelEvent _) {
                _toolbarRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _playModeGroup.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                _mount?.Dispose();
                _mount = null;
                EditorApplication.delayCall += ScheduleInstallation;
            }
        }
    }
}
#endif
