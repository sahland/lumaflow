#nullable enable

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    public sealed class UxmlPreviewWindow : EditorWindow {
        [SerializeField] private UxmlPreviewDefinition? _definition;
        [SerializeField] private bool _showGameView;
        private MountHandle? _mount;
        private GameObject? _gameHost;
        private VisualElement? _runtime;
        private VisualElement? _generated;
        private Label? _status;
        private bool _queued;

        [MenuItem("Tools/LumaFlow/UXML Preview (Experimental)")]
        public static void Open() => GetWindow<UxmlPreviewWindow>("LumaFlow UXML");

        public void CreateGUI() {
            minSize = new Vector2(940, 520);
            rootVisualElement.style.backgroundColor = (Color)new Color32(32, 35, 42, 255);
            var toolbar = new Toolbar();
            var field = new ObjectField("Preview factory") {
                objectType = typeof(UxmlPreviewDefinition), allowSceneObjects = false, value = _definition
            };
            field.RegisterValueChangedCallback(change => { _definition = change.newValue as UxmlPreviewDefinition; QueueRebuild(); });
            toolbar.Add(field);
            toolbar.Add(new ToolbarButton(QueueRebuild) { text = "Generate" });
            var gameView = new ToolbarToggle { text = "Game View", value = _showGameView };
            gameView.RegisterValueChangedCallback(change => { _showGameView = change.newValue; QueueRebuild(); });
            toolbar.Add(gameView);
            rootVisualElement.Add(toolbar);
            _status = new Label("Select a saved preview factory asset. Preview executes its C# outside Play Mode.");
            _status.style.color = (Color)new Color32(184, 200, 214, 255);
            _status.style.marginLeft = 16;
            _status.style.marginTop = 12;
            _status.style.marginBottom = 4;
            rootVisualElement.Add(_status);
            var panes = new VisualElement();
            panes.style.flexDirection = FlexDirection.Row;
            panes.style.flexGrow = 1;
            panes.style.paddingLeft = 8;
            panes.style.paddingRight = 8;
            panes.style.paddingBottom = 8;
            _runtime = AddPane(panes, "Runtime mount");
            _generated = AddPane(panes, "Generated UXML + USS");
            rootVisualElement.Add(panes);
            QueueRebuild();
        }

        private static VisualElement AddPane(VisualElement parent, string title) {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.flexBasis = 0;
            pane.style.marginLeft = 8;
            pane.style.marginRight = 8;
            pane.style.marginTop = 8;
            var label = new Label(title);
            label.style.color = (Color)new Color32(243, 242, 238, 255);
            label.style.fontSize = 13;
            label.style.marginBottom = 12;
            pane.Add(label);
            var content = new VisualElement();
            content.style.flexGrow = 1;
            pane.Add(content);
            parent.Add(pane);
            return content;
        }

        private void OnEnable() {
            Undo.undoRedoPerformed += QueueRebuild;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= QueueRebuild;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall -= Rebuild;
            _mount?.Dispose();
            _mount = null;
            if (_gameHost != null) DestroyImmediate(_gameHost);
        }

        private void OnPlayModeChanged(PlayModeStateChange change) {
            if (change == PlayModeStateChange.ExitingEditMode && _gameHost != null) DestroyImmediate(_gameHost);
            if (change == PlayModeStateChange.EnteredEditMode) QueueRebuild();
        }

        internal void QueueRebuild() {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += Rebuild;
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
                if (_gameHost != null) DestroyImmediate(_gameHost);
                return;
            }
            try {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_definition));
                if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Save the preview factory as an asset first.");
                const string generatedRoot = "Assets/LumaFlowGenerated";
                if (!AssetDatabase.IsValidFolder(generatedRoot)) AssetDatabase.CreateFolder("Assets", "LumaFlowGenerated");
                var folder = generatedRoot + "/" + guid;
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(generatedRoot, guid);
                var export = UxmlPreviewExporter.Export(_definition.CreateWidget(), "Preview.uss");
                WriteChanged(folder + "/Preview.uss", export.Uss);
                WriteChanged(folder + "/Preview.uxml", export.Uxml);
                AssetDatabase.ImportAsset(folder + "/Preview.uss", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(folder + "/Preview.uxml", ImportAssetOptions.ForceSynchronousImport);
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folder + "/Preview.uxml");
                if (tree == null) throw new InvalidOperationException("Unity could not import the generated UXML.");
                _mount?.Dispose();
                _mount = null;
                _runtime.Clear();
                _generated.Clear();
                _mount = global::LumaFlow.LumaFlow.Mount(_definition.CreateWidget(), _runtime);
                tree.CloneTree(_generated);
                if (_gameHost != null) DestroyImmediate(_gameHost);
                if (_showGameView) ShowInGameView(tree, folder);
                _status!.text = "Generated · visual snapshot · edit the factory's Inspector fields or C# to update";
                _status.tooltip = folder + "/Preview.uxml";
            } catch (Exception exception) {
                _status!.text = "Preview failed: " + exception.Message;
                Debug.LogException(exception);
            }
        }

        private void ShowInGameView(VisualTreeAsset tree, string folder) {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(folder + "/PreviewPanel.asset");
            if (panel == null) {
                var themePath = AssetDatabase.FindAssets("t:ThemeStyleSheet").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
                if (themePath == null) throw new InvalidOperationException("Create a UI Toolkit Theme Style Sheet asset for Game View preview.");
                panel = CreateInstance<PanelSettings>();
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                AssetDatabase.CreateAsset(panel, folder + "/PreviewPanel.asset");
            }
            _gameHost = new GameObject("LumaFlow UXML preview (temporary)") { hideFlags = HideFlags.HideAndDontSave };
            var document = _gameHost.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = tree;
        }

        private static void WriteChanged(string path, string contents) {
            if (!File.Exists(path) || File.ReadAllText(path) != contents) File.WriteAllText(path, contents);
        }
    }

    [CustomEditor(typeof(UxmlPreviewDefinition), true)]
    internal sealed class UxmlPreviewDefinitionInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            if (!DrawDefaultInspector()) return;
            foreach (var window in Resources.FindObjectsOfTypeAll<UxmlPreviewWindow>()) window.QueueRebuild();
        }
    }
}
