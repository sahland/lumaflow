#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using NativeScrollView = UnityEngine.UIElements.ScrollView;

namespace LumaFlow.Editor {
    public sealed class UxmlPreviewWindow : EditorWindow {
        [SerializeField] private UxmlPreviewDefinition? _definition;
        [SerializeField] private bool _compare;
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private int _viewportWidth = 640;
        [SerializeField] private int _viewportHeight = 480;
        [SerializeField] private int _zoomPercent = 100;
        private UnityEditor.Editor? _factoryEditor;
        private IMGUIContainer? _inspector;
        private VisualElement? _runtimePane;
        private string? _outputFolder;
        private MountHandle? _mount;
        private VisualElement? _runtime;
        private VisualElement? _generated;
        private Label? _status;
        private bool _queued;

        [MenuItem("Tools/LumaFlow/UXML Preview (Experimental)")]
        public static void Open() => GetWindow<UxmlPreviewWindow>("LumaFlow UXML");

        public void CreateGUI() {
            rootVisualElement.Clear();
            minSize = new Vector2(800, 520);
            if (_definition == null) {
                var existing = AssetDatabase.FindAssets("t:UxmlPreviewDefinition").FirstOrDefault();
                if (existing != null) _definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(AssetDatabase.GUIDToAssetPath(existing));
            }
            rootVisualElement.style.backgroundColor = (Color)new Color32(32, 35, 42, 255);
            var toolbar = new Toolbar();
            var field = new ObjectField("Preview factory") {
                objectType = typeof(UxmlPreviewDefinition), allowSceneObjects = false, value = _definition
            };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(change => {
                _definition = change.newValue as UxmlPreviewDefinition;
                RefreshInspector();
                _outputFolder = null;
                QueueRebuild();
            });
            toolbar.Add(field);
            toolbar.Add(new ToolbarButton(() => CreateDemo(field)) { text = "New demo" });
            toolbar.Add(new ToolbarButton(QueueRebuild) { text = "Generate" });
            toolbar.Add(new ToolbarButton(BindSelectedDocument) { text = "Bind selected UIDocument" });
            rootVisualElement.Add(toolbar);
            var options = new Toolbar();
            var auto = new ToolbarToggle { text = "Auto refresh", value = _autoRefresh };
            auto.RegisterValueChangedCallback(change => { _autoRefresh = change.newValue; if (_autoRefresh) QueueRebuild(); });
            options.Add(auto);
            var compare = new ToolbarToggle { name = "compare-runtime", text = "Compare runtime", value = _compare };
            compare.RegisterValueChangedCallback(change => {
                _compare = change.newValue;
                if (_runtimePane != null) _runtimePane.style.display = _compare ? DisplayStyle.Flex : DisplayStyle.None;
            });
            options.Add(compare);
            var presets = new PopupField<string>(new List<string> { "Custom", "Phone 390×844", "Tablet 768×1024", "Desktop 1920×1080" }, 0);
            var width = new IntegerField { name = "viewport-width", value = _viewportWidth, tooltip = "Viewport width (pixels)" };
            var height = new IntegerField { value = _viewportHeight, tooltip = "Viewport height (pixels)" };
            width.style.width = 65;
            height.style.width = 65;
            width.RegisterValueChangedCallback(change => { _viewportWidth = Mathf.Clamp(change.newValue, 100, 4096); width.SetValueWithoutNotify(_viewportWidth); ApplyViewport(); });
            height.RegisterValueChangedCallback(change => { _viewportHeight = Mathf.Clamp(change.newValue, 100, 4096); height.SetValueWithoutNotify(_viewportHeight); ApplyViewport(); });
            presets.RegisterValueChangedCallback(change => {
                var size = change.newValue.StartsWith("Phone") ? new Vector2Int(390, 844)
                    : change.newValue.StartsWith("Tablet") ? new Vector2Int(768, 1024)
                    : change.newValue.StartsWith("Desktop") ? new Vector2Int(1920, 1080) : new Vector2Int(_viewportWidth, _viewportHeight);
                width.value = size.x;
                height.value = size.y;
            });
            options.Add(presets);
            options.Add(width);
            options.Add(new Label("×"));
            options.Add(height);
            var zoomValues = new List<string> { "25%", "50%", "75%", "100%" };
            var zoom = new PopupField<string>(zoomValues, Math.Max(0, zoomValues.IndexOf(_zoomPercent + "%")));
            zoom.name = "viewport-zoom";
            zoom.tooltip = "Display scale only; layout is calculated at the selected viewport size.";
            zoom.RegisterValueChangedCallback(change => { _zoomPercent = int.Parse(change.newValue.TrimEnd('%')); ApplyViewport(); });
            options.Add(zoom);
            rootVisualElement.Add(options);
            _status = new Label("Select a saved preview factory asset. Preview executes its C# outside Play Mode.");
            _status.style.color = (Color)new Color32(184, 200, 214, 255);
            _status.style.marginLeft = 16;
            _status.style.marginTop = 12;
            _status.style.marginBottom = 4;
            _status.style.whiteSpace = WhiteSpace.Normal;
            rootVisualElement.Add(_status);
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.minHeight = 0;
            var sidebar = new NativeScrollView();
            sidebar.style.width = 230;
            sidebar.style.flexShrink = 0;
            sidebar.style.paddingTop = 12;
            sidebar.style.paddingLeft = 12;
            sidebar.style.paddingRight = 12;
            sidebar.Add(new Label("Preview data"));
            _inspector = new IMGUIContainer(() => {
                if (_factoryEditor != null && _factoryEditor.DrawDefaultInspector()) AutoRebuild();
                else if (_factoryEditor == null) EditorGUILayout.HelpBox("Select a factory or create a demo to begin.", MessageType.Info);
            });
            sidebar.Add(_inspector);
            sidebar.Add(new UnityEngine.UIElements.Button(() => {
                if (_definition != null) AssetDatabase.OpenAsset(MonoScript.FromScriptableObject(_definition));
            }) { text = "Edit factory C#" });
            sidebar.Add(new UnityEngine.UIElements.Button(() => OpenGenerated("Preview.uxml")) { text = "Open UXML" });
            sidebar.Add(new UnityEngine.UIElements.Button(() => OpenGenerated("Preview.uss")) { text = "Open USS" });
            sidebar.Add(new UnityEngine.UIElements.Button(() => {
                if (_outputFolder != null) EditorUtility.RevealInFinder(Path.GetFullPath(_outputFolder));
            }) { text = "Show generated files" });
            body.Add(sidebar);
            var panes = new VisualElement();
            panes.style.flexDirection = FlexDirection.Row;
            panes.style.flexGrow = 1;
            panes.style.minWidth = 0;
            panes.style.paddingLeft = 8;
            panes.style.paddingRight = 8;
            panes.style.paddingBottom = 8;
            _runtime = AddPane(panes, "Runtime mount");
            _runtimePane = panes[0];
            _runtimePane.name = "runtime-pane";
            _runtimePane.style.display = _compare ? DisplayStyle.Flex : DisplayStyle.None;
            _generated = AddPane(panes, "Generated UXML + USS");
            _generated.name = "generated-viewport";
            body.Add(panes);
            rootVisualElement.Add(body);
            RefreshInspector();
            ApplyViewport();
            QueueRebuild();
        }

        private void RefreshInspector() {
            if (_factoryEditor != null) DestroyImmediate(_factoryEditor);
            _factoryEditor = _definition != null ? UnityEditor.Editor.CreateEditor(_definition) : null;
            _inspector?.MarkDirtyRepaint();
        }

        private void CreateDemo(ObjectField field) {
            var path = EditorUtility.SaveFilePanelInProject("Create preview factory", "UxmlPreviewDemo", "asset", "Choose where to save preview data.");
            if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<UxmlPreviewDemo>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create preview factory");
            field.value = asset;
        }

        private void OpenGenerated(string name) {
            if (_outputFolder == null) return;
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(_outputFolder + "/" + name));
        }

        private void ApplyViewport() {
            foreach (var viewport in new[] { _runtime, _generated }) {
                if (viewport == null) continue;
                viewport.style.width = _viewportWidth;
                viewport.style.height = _viewportHeight;
                var scale = _zoomPercent / 100f;
                viewport.style.scale = new Scale(new Vector3(scale, scale, 1));
                viewport.style.transformOrigin = new TransformOrigin(0, 0, 0);
                viewport.parent.style.width = _viewportWidth * scale;
                viewport.parent.style.height = _viewportHeight * scale;
            }
        }

        private static VisualElement AddPane(VisualElement parent, string title) {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.flexBasis = 0;
            pane.style.minWidth = 0;
            pane.style.minHeight = 0;
            pane.style.marginLeft = 8;
            pane.style.marginRight = 8;
            pane.style.marginTop = 8;
            var label = new Label(title);
            label.style.color = (Color)new Color32(243, 242, 238, 255);
            label.style.fontSize = 13;
            label.style.marginBottom = 12;
            pane.Add(label);
            var content = new VisualElement();
            content.style.flexShrink = 0;
            content.style.position = Position.Absolute;
            content.style.left = 0;
            content.style.top = 0;
            var frame = new VisualElement();
            frame.style.flexShrink = 0;
            frame.style.overflow = Overflow.Hidden;
            frame.Add(content);
            var scroll = new NativeScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
            scroll.Add(frame);
            pane.Add(scroll);
            parent.Add(pane);
            return content;
        }

        private void OnEnable() {
            Undo.undoRedoPerformed += AutoRebuild;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= AutoRebuild;
            EditorApplication.delayCall -= Rebuild;
            _mount?.Dispose();
            _mount = null;
            if (_factoryEditor != null) DestroyImmediate(_factoryEditor);
        }

        internal void QueueRebuild() {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += Rebuild;
        }

        internal void AutoRebuild() {
            if (_autoRefresh) QueueRebuild();
        }

        private void Rebuild() {
            _queued = false;
            if (this == null || _runtime == null || _generated == null) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (_definition == null) {
                _mount?.Dispose();
                _mount = null;
                _runtime.Clear();
                _generated.Clear();
                return;
            }
            try {
                var tree = UxmlPreviewGenerator.Generate(_definition, out var uxmlPath);
                var folder = Path.GetDirectoryName(uxmlPath)!.Replace('\\', '/');
                _outputFolder = folder;
                _mount?.Dispose();
                _mount = null;
                _runtime.Clear();
                _generated.Clear();
                _mount = global::LumaFlow.LumaFlow.Mount(_definition.CreateWidget(), _runtime);
                tree.CloneTree(_generated);
                _status!.text = "Updated " + DateTime.Now.ToString("HH:mm:ss") + " · visual snapshot (no callbacks)";
                _status.tooltip = folder + "/Preview.uxml";
            } catch (Exception exception) {
                _status!.text = "Preview failed: " + exception.Message;
                Debug.LogException(exception);
            }
        }

        private void BindSelectedDocument() {
            if (_definition == null) {
                _status!.text = "Select a preview factory first.";
                return;
            }

            var document = Selection.activeGameObject?.GetComponent<UIDocument>();
            if (document == null) {
                _status!.text = "Select a scene GameObject containing UIDocument, then bind again.";
                return;
            }

            if (document.panelSettings == null) {
                _status!.text = "The selected UIDocument needs Panel Settings before it can render in Game View.";
                return;
            }

            try {
                var tree = UxmlPreviewGenerator.Generate(_definition, out var path);
                Undo.RecordObject(document, "Bind LumaFlow UXML preview");
                document.visualTreeAsset = tree;
                EditorUtility.SetDirty(document);
                _outputFolder = Path.GetDirectoryName(path)!.Replace('\\', '/');
                _status!.text = "Bound to " + document.gameObject.name
                    + " · leave Play Mode stopped; C# compilation now regenerates this UXML automatically.";
                Selection.activeObject = document.gameObject;
            } catch (Exception exception) {
                _status!.text = "Binding failed: " + exception.Message;
                Debug.LogException(exception);
            }
        }
    }

    [CustomEditor(typeof(UxmlPreviewDefinition), true)]
    internal sealed class UxmlPreviewDefinitionInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            if (!DrawDefaultInspector()) return;
            UxmlPreviewAutoGenerator.Schedule();
            foreach (var window in Resources.FindObjectsOfTypeAll<UxmlPreviewWindow>()) window.AutoRebuild();
        }
    }
}
