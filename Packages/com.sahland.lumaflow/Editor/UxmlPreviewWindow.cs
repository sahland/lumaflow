#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NativeScrollView = UnityEngine.UIElements.ScrollView;

namespace LumaFlow.Editor {
    public sealed class UxmlPreviewWindow : EditorWindow {
        [SerializeField] private UxmlPreviewDefinition? _definition;
        [SerializeField] private string _codeFactoryId = "";
        [SerializeField] private bool _compare;
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private int _viewportWidth = 640;
        [SerializeField] private int _viewportHeight = 480;
        [SerializeField] private int _zoomPercent = 100;
        private readonly State<string> _sourceValue = new("");
        private readonly State<string> _widthValue = new("640");
        private readonly State<string> _heightValue = new("480");
        private readonly State<string> _presetValue = new("Custom");
        private readonly State<int> _zoomValue = new(100);
        private readonly State<bool> _compareValue = new(false);
        private readonly State<bool> _autoValue = new(true);
        private readonly State<bool> _startupValue = new(true);
        private readonly State<bool> _gamePreviewValue = new(true);
        private readonly State<string> _status = new("Preparing preview…");
        private readonly State<string> _generatedTitle = new("Generated UXML · visual snapshot");
        private readonly List<PreviewChoice> _choices = new();
        private UnityEditor.Editor? _factoryEditor;
        private IMGUIContainer? _inspector;
        private string? _outputFolder;
        private MountHandle? _uiMount;
        private MountHandle? _mount;
        private MountHandle? _generatedMount;
        private VisualElement? _runtime;
        private VisualElement? _generated;
        private Func<VisualElement>? _createRuntimeViewport;
        private Func<VisualElement>? _createGeneratedViewport;
        private bool _queued;
        private bool _sourcesQueued;
        private string? _sceneSourceId;
        private bool _connectExisting;

        [MenuItem("Tools/LumaFlow/UXML")]
        public static void Open() => GetWindow<UxmlPreviewWindow>("LumaFlow UXML");

        public void CreateGUI() {
            _generatedMount?.Dispose(); _generatedMount = null;
            _mount?.Dispose(); _mount = null;
            _uiMount?.Dispose(); _uiMount = null;
            rootVisualElement.Clear();
            minSize = new Vector2(820, 520);
            try {
                _sceneSourceId = UxmlPreviewSceneSetup.DiscoverSceneSource();
                _connectExisting = _sceneSourceId != null;
            } catch (Exception exception) {
                _sceneSourceId = null; _connectExisting = false;
                _status.Value = exception.Message;
            }
            RefreshChoices();
            var saved = _sceneSourceId ?? (_definition != null ? "asset:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_definition))
                : !string.IsNullOrEmpty(_codeFactoryId) ? "code:" + _codeFactoryId
                : EditorPrefs.GetString(UxmlPreviewStartup.SourceKey, ""));
            var preferred = _choices.FirstOrDefault(item => item.Id == saved);
            var choice = preferred
                ?? _choices.FirstOrDefault(item => item.Factory?.Method.DeclaringType != typeof(BuiltInUxmlPreview))
                ?? _choices.First();
            SelectChoice(choice, resetViewport: preferred == null, persist: false);
            _autoValue.Value = _autoRefresh;
            _compareValue.Value = _compare;
            _startupValue.Value = UxmlPreviewStartup.ShowOnStartup;
            _gamePreviewValue.Value = UxmlPreviewSceneSync.GamePreviewEnabled;
            SyncViewportState();
            if (preferred == null) FitPreview();
            RefreshInspector();
            _uiMount = global::LumaFlow.LumaFlow.Mount(
                new Theme(EditorToolWidgets.Theme, new Scaffold(BuildWindow())), rootVisualElement);
            ApplyViewport();
            QueueRebuild();
        }

        private Widget BuildWindow() => SizedBox.Expand(new Padding(new Column(new Widget[] {
            new Row(new Widget[] {
                new Expanded(new Column(new Widget[] {
                    new Text("LumaFlow UXML", EditorToolWidgets.Theme.Typography.Title),
                    EditorToolWidgets.Hint("C# → UXML + USS · automatic edit-mode preview")
                }, gap: 4)),
                new Button("GUI Performance", GuiPerformanceWindow.Open, ButtonVariant.Secondary)
            }, gap: 16),
            new ReactiveBuilder<string>(_status, text => EditorToolWidgets.Hint(text)),
            new Expanded(new Row(new Widget[] {
                new SizedBox(new ScrollView(BuildSettings()), width: 304),
                new Expanded(new ReactiveBuilder<bool>(_compareValue, BuildPreviewArea))
            }, gap: 16))
        }, gap: 16), EdgeInsets.All(16)));

        private Widget BuildSettings() => new Column(new Widget[] {
            EditorToolWidgets.Card(new Column(new Widget[] {
            new Text("Scene preview", EditorToolWidgets.Theme.Typography.Headline),
            new Button("Auto setup scene", AutoSetupScene),
            new Checkbox(_gamePreviewValue, label: "Preview in Game View", onChanged: value => {
                UxmlPreviewSceneSync.GamePreviewEnabled = value;
                QueueRebuild();
            }),
            EditorToolWidgets.Hint("Game preview is optional. This window works independently."),
            }, gap: 12)),
            EditorToolWidgets.Card(new Column(new Widget[] {
            new Text("Preview source", EditorToolWidgets.Theme.Typography.Headline),
            new Dropdown<string>(_sourceValue, _choices.Select(item => item.Id),
                id => _choices.First(item => item.Id == id).Label,
                onChanged: SelectSource),
            new Native(() => _inspector = new IMGUIContainer(DrawFactoryInspector)),
            new Button("Edit factory C#", EditFactory, ButtonVariant.Secondary),
            }, gap: 12)),
            EditorToolWidgets.Card(new Column(new Widget[] {
            new Text("Viewport", EditorToolWidgets.Theme.Typography.Headline),
            new Dropdown<string>(_presetValue, new[] { "Custom", "Phone 390×844", "Tablet 768×1024", "Desktop 1920×1080" },
                value => value, onChanged: ApplyPreset),
            new Row(new Widget[] {
                new Expanded(EditorToolWidgets.Field("Width", new TextField(_widthValue,
                    onSubmitted: _ => ApplyDimensions()))),
                new Expanded(EditorToolWidgets.Field("Height", new TextField(_heightValue,
                    onSubmitted: _ => ApplyDimensions())))
            }, gap: 8),
            new Row(new Widget[] {
                new Expanded(new Button("Apply size", ApplyDimensions, ButtonVariant.Secondary)),
                new Expanded(new Button("Fit preview", FitPreview, ButtonVariant.Secondary))
            }, gap: 8),
            EditorToolWidgets.Field("Display zoom", new Dropdown<int>(_zoomValue, new[] { 25, 50, 75, 100 }, value => value + "%",
                onChanged: value => { _zoomPercent = value; ApplyViewport(); })),
            }, gap: 12)),
            EditorToolWidgets.Card(new Column(new Widget[] {
            new Text("Behaviour", EditorToolWidgets.Theme.Typography.Headline),
            new Checkbox(_autoValue, label: "Auto refresh", onChanged: value => { _autoRefresh = value; if (value) QueueRebuild(); }),
            new Checkbox(_compareValue, label: "Compare runtime", onChanged: SetCompare),
            new Checkbox(_startupValue, label: "Show on project startup",
                onChanged: value => UxmlPreviewStartup.ShowOnStartup = value),
            new Button("Generate now", QueueRebuild),
            }, gap: 10)),
            EditorToolWidgets.Card(new Column(new Widget[] {
            new Text("Generated assets", EditorToolWidgets.Theme.Typography.Headline),
            new Button("Bind selected UIDocument", BindSelectedDocument, ButtonVariant.Secondary),
            EditorToolWidgets.Hint("Binding is explicit and Undoable. Existing scene documents are never overwritten on startup."),
            new Button("New demo asset", CreateDemo, ButtonVariant.Secondary),
            new Row(new Widget[] {
                new Expanded(new Button("Open UXML", () => OpenGenerated("Preview.uxml"), ButtonVariant.Secondary)),
                new Expanded(new Button("Open USS", () => OpenGenerated("Preview.uss"), ButtonVariant.Secondary))
            }, gap: 8),
            new Button("Show generated files", () => {
                if (_outputFolder != null) EditorUtility.RevealInFinder(Path.GetFullPath(_outputFolder));
            }, ButtonVariant.Secondary)
            }, gap: 12)),
        }, gap: 12);

        private Widget BuildPreviewArea(bool compare) {
            var panes = new List<Widget>();
            if (compare) panes.Add(new Expanded(BuildPane("Live LumaFlow", true)).WithKey(new WidgetKey("live")));
            panes.Add(new Expanded(BuildPane("Generated UXML · visual snapshot", false)).WithKey(new WidgetKey("generated")));
            return SizedBox.Expand(new Row(panes, gap: 12));
        }

        private Widget BuildPane(string title, bool runtime) => SizedBox.Expand(EditorToolWidgets.Card(
            SizedBox.Expand(new Column(new Widget[] {
                runtime ? new Text(title, EditorToolWidgets.Theme.Typography.Headline)
                    : new ReactiveBuilder<string>(_generatedTitle, value => new Text(value, EditorToolWidgets.Theme.Typography.Headline)),
                new Expanded(new Native(runtime
                    ? (_createRuntimeViewport ??= () => CreateViewport(true))
                    : (_createGeneratedViewport ??= () => CreateViewport(false))))
            }, gap: 12))));

        // The only native UI here is the exported document canvas and Unity's serialized-data inspector.
        private VisualElement CreateViewport(bool runtime) {
            var content = new VisualElement { name = runtime ? "runtime-viewport" : "generated-viewport" };
            content.style.position = Position.Absolute;
            content.style.left = 0; content.style.top = 0; content.style.flexShrink = 0;
            var frame = new VisualElement();
            frame.style.flexShrink = 0; frame.style.overflow = Overflow.Hidden;
            frame.Add(content);
            var scroll = new NativeScrollView(ScrollViewMode.VerticalAndHorizontal) {
                name = runtime ? "runtime-pane" : "generated-pane"
            };
            scroll.style.flexGrow = 1; scroll.style.minHeight = 0; scroll.style.minWidth = 0;
            scroll.Add(frame);
            if (runtime) _runtime = content; else _generated = content;
            ApplyViewport();
            return scroll;
        }

        private void RefreshChoices() {
            _choices.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:UxmlPreviewDefinition").OrderBy(value => value, StringComparer.Ordinal)) {
                var asset = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) _choices.Add(new PreviewChoice("asset:" + guid, asset.name + " (asset)", asset, null));
            }
            foreach (var factory in LumaPreviewRegistry.Factories)
                _choices.Add(new PreviewChoice("code:" + factory.Id, factory.DisplayName, null, factory));
        }

        private void SelectSource(string id) {
            SelectChoice(_choices.First(item => item.Id == id), resetViewport: true, persist: true);
            RefreshInspector();
            _outputFolder = null;
            QueueRebuild();
        }

        private void SelectChoice(PreviewChoice choice, bool resetViewport, bool persist) {
            _definition = choice.Definition;
            _codeFactoryId = choice.Factory?.Id ?? "";
            _sourceValue.Value = choice.Id;
            if (persist) EditorPrefs.SetString(UxmlPreviewStartup.SourceKey, choice.Id);
            if (resetViewport) {
                var size = choice.Definition != null ? choice.Definition.Environment.ResolveViewport() : choice.Factory!.ViewportSize;
                SetPreviewViewport(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), _zoomPercent);
            }
        }

        private void DrawFactoryInspector() {
            if (_factoryEditor != null && _factoryEditor.DrawDefaultInspector()) AutoRebuild();
        }

        private void ApplyPreset(string preset) {
            if (preset.StartsWith("Phone")) SetPreviewViewport(390, 844, 50);
            else if (preset.StartsWith("Tablet")) SetPreviewViewport(768, 1024, 50);
            else if (preset.StartsWith("Desktop")) SetPreviewViewport(1920, 1080, 25);
        }

        private void ApplyDimensions() {
            if (!int.TryParse(_widthValue.Value, out var width) || !int.TryParse(_heightValue.Value, out var height)) {
                _status.Value = "Width and height must be whole numbers (100–4096).";
                return;
            }
            _presetValue.Value = "Custom";
            SetPreviewViewport(width, height, _zoomPercent);
        }

        internal void SetPreviewViewport(int width, int height, int zoom) {
            _viewportWidth = Mathf.Clamp(width, 100, 4096);
            _viewportHeight = Mathf.Clamp(height, 100, 4096);
            _zoomPercent = new[] {25, 50, 75, 100}.Contains(zoom) ? zoom : 100;
            SyncViewportState(); ApplyViewport();
        }

        private void SyncViewportState() {
            _widthValue.Value = _viewportWidth.ToString();
            _heightValue.Value = _viewportHeight.ToString();
            _zoomValue.Value = _zoomPercent;
        }

        private void FitPreview() {
            var width = (Mathf.Max(minSize.x, position.width) - 340f) / (_compare ? 2f : 1f) - 32f;
            var height = Mathf.Max(minSize.y, position.height) - 160f;
            var fit = Mathf.Min(width / _viewportWidth, height / _viewportHeight) * 100f;
            _zoomPercent = fit >= 100 ? 100 : fit >= 75 ? 75 : fit >= 50 ? 50 : 25;
            _zoomValue.Value = _zoomPercent;
            ApplyViewport();
        }

        private void SetCompare(bool value) {
            _compare = value;
            _mount?.Dispose(); _mount = null;
            if (!value) _runtime = null;
            QueueRebuild();
        }

        private void RefreshInspector() {
            if (_factoryEditor != null) DestroyImmediate(_factoryEditor);
            _factoryEditor = _definition != null ? UnityEditor.Editor.CreateEditor(_definition) : null;
            _inspector?.MarkDirtyRepaint();
        }

        private void CreateDemo() {
            var path = EditorUtility.SaveFilePanelInProject("Create preview factory", "UxmlPreviewDemo", "asset", "Choose where to save preview data.");
            if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<UxmlPreviewDemo>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create preview factory");
            RefreshChoices();
            // Recreate only the settings UI when the available source list changes.
            _definition = asset; _codeFactoryId = "";
            CreateGUI();
            EditorPrefs.SetString(UxmlPreviewStartup.SourceKey, _sourceValue.Value);
        }

        private void EditFactory() {
            if (_definition != null) AssetDatabase.OpenAsset(MonoScript.FromScriptableObject(_definition));
            else OpenCodeFactory();
        }

        private void OpenGenerated(string name) {
            if (_outputFolder != null) AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(_outputFolder + "/" + name));
        }

        private void ApplyViewport() {
            foreach (var viewport in new[] { _runtime, _generated }) {
                if (viewport?.parent == null) continue;
                viewport.style.width = _viewportWidth; viewport.style.height = _viewportHeight;
                var scale = _zoomPercent / 100f;
                viewport.style.scale = new Scale(new Vector3(scale, scale, 1));
                viewport.style.transformOrigin = new TransformOrigin(0, 0, 0);
                viewport.parent.style.width = _viewportWidth * scale;
                viewport.parent.style.height = _viewportHeight * scale;
            }
        }

        private void OnEnable() {
            Undo.undoRedoPerformed += AutoRebuild;
            EditorApplication.projectChanged += QueueSourceRefresh;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.delayCall -= RefreshScene;
            EditorApplication.delayCall += RefreshScene;
        }
        private void RefreshScene() { if (this != null) CreateGUI(); }

        private void OnPlayModeChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode) {
                _generatedMount?.Dispose(); _generatedMount = null;
                _status.Value = "Preview generation is paused during Play Mode.";
            }
            if (state == PlayModeStateChange.EnteredEditMode) QueueRebuild();
        }

        private void QueueSourceRefresh() {
            if (_sourcesQueued) return;
            _sourcesQueued = true; EditorApplication.delayCall += RefreshSources;
        }

        private void RefreshSources() {
            _sourcesQueued = false;
            if (this == null || _uiMount == null) return;
            var oldIds = _choices.Select(item => item.Id + "=" + item.Label).ToArray();
            RefreshChoices();
            if (!oldIds.SequenceEqual(_choices.Select(item => item.Id + "=" + item.Label))) CreateGUI();
            else AutoRebuild();
        }

        private void OnDisable() {
            _generatedMount?.Dispose(); _generatedMount = null;
            Undo.undoRedoPerformed -= AutoRebuild;
            EditorApplication.projectChanged -= QueueSourceRefresh;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
            EditorApplication.delayCall -= RefreshScene;
            EditorApplication.delayCall -= Rebuild;
            EditorApplication.delayCall -= RefreshSources;
            _queued = false; _sourcesQueued = false;
            _mount?.Dispose(); _mount = null;
            _uiMount?.Dispose(); _uiMount = null;
            if (_factoryEditor != null) DestroyImmediate(_factoryEditor);
            _factoryEditor = null; _runtime = null; _generated = null;
        }

        private sealed class PreviewChoice {
            internal PreviewChoice(string id, string label, UxmlPreviewDefinition? definition, LumaPreviewFactory? factory) {
                Id = id; Label = label; Definition = definition; Factory = factory;
            }
            internal string Id { get; }
            internal string Label { get; }
            internal UxmlPreviewDefinition? Definition { get; }
            internal LumaPreviewFactory? Factory { get; }
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
            if (this == null || _generated == null || (_compare && _runtime == null)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var codeFactory = CurrentCodeFactory();
            if (_definition == null && codeFactory == null) {
                _mount?.Dispose();
                _mount = null;
                _runtime?.Clear();
                _generatedMount?.Dispose(); _generatedMount = null;
                _generated.Clear();
                return;
            }
            try {
                var tree = _definition != null
                    ? UxmlPreviewGenerator.Generate(_definition, out var uxmlPath)
                    : UxmlPreviewGenerator.Generate(codeFactory!, out uxmlPath);
                var folder = Path.GetDirectoryName(uxmlPath)!.Replace('\\', '/');
                _outputFolder = folder;
                _mount?.Dispose();
                _mount = null;
                _runtime?.Clear();
                _generatedMount?.Dispose(); _generatedMount = null;
                _generated.Clear();
                if (_compare) {
                    var widget = _definition != null ? _definition.CreatePreviewWidget() : codeFactory!.CreateWidget();
                    _mount = global::LumaFlow.LumaFlow.Mount(widget, _runtime!);
                }
                tree.CloneTree(_generated);
                _generatedTitle.Value = "Generated UXML · visual snapshot";
                var sourceId = CurrentStatusId(codeFactory);
                UxmlPreviewSceneSync.Apply(sourceId, tree);
                if (_connectExisting && _sceneSourceId == sourceId) {
                    _connectExisting = false;
                    var existing = UxmlPreviewSceneSetup.FindDocument(UxmlPreviewSceneSetup.FindBootstrap());
                    if (existing != null && existing.GetComponent<UxmlPreviewBinding>() == null)
                        UxmlPreviewSceneSetup.Configure(tree, sourceId);
                }
                UxmlPreviewStatus.ReportSuccess(CurrentStatusId(codeFactory));
                _status.Value = "Updated " + DateTime.Now.ToString("HH:mm:ss") + " · visual snapshot (no callbacks)";

            } catch (NotSupportedException) {
                try {
                    Func<Widget> factory = () => _definition != null ? _definition.CreatePreviewWidget() : codeFactory!.CreateWidget();
                    var host = new VisualElement();
                    var mount = UxmlPreviewLiveSnapshot.Mount(factory(), host, new Vector2(_viewportWidth, _viewportHeight));
                    _generatedMount?.Dispose(); _generatedMount = mount;
                    _mount?.Dispose(); _mount = null;
                    _runtime?.Clear();
                    if (_compare) _mount = UxmlPreviewLiveSnapshot.Mount(factory(), _runtime!, new Vector2(_viewportWidth, _viewportHeight));
                    _generated.Clear(); _generated.Add(host);
                    _generatedTitle.Value = "Live C# · edit-mode preview";
                    _outputFolder = null;
                    var sourceId = CurrentStatusId(codeFactory);
                    if (_connectExisting && _sceneSourceId == sourceId) {
                        var existing = UxmlPreviewSceneSetup.FindDocument(UxmlPreviewSceneSetup.FindBootstrap());
                        if (existing != null && existing.GetComponent<UxmlPreviewBinding>() == null)
                            UxmlPreviewSceneSetup.Configure(null, sourceId);
                        _connectExisting = false;
                    }
                    UxmlPreviewSceneSync.ApplyLive(sourceId, factory);
                    UxmlPreviewStatus.ReportSuccess(sourceId);
                    _status.Value = "Live C# preview · custom controls are rendered directly; UXML export is unavailable for this source.";
                } catch (Exception liveException) {
                    _status.Value = "Live preview failed: " + liveException.Message;
                    UxmlPreviewStatus.ReportFailure(CurrentStatusId(codeFactory), CurrentSourceLabel(codeFactory), liveException);
                    Debug.LogException(liveException);
                }
            } catch (Exception exception) {
                _status.Value = "Preview failed: " + exception.Message;
                UxmlPreviewStatus.ReportFailure(CurrentStatusId(codeFactory), CurrentSourceLabel(codeFactory), exception);
                Debug.LogException(exception);
            }
        }

        internal void AutoSetupScene() {
            try {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before auto setup.");
                if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                    throw new InvalidOperationException("Open a regular scene, not Prefab Mode.");
                if (UxmlPreviewQuickStart.NeedsStarter) {
                    UxmlPreviewQuickStart.CreateStarter();
                    _status.Value = "Starter scripts created in Assets/LumaFlowApp. Setup will finish automatically after compilation.";
                    return;
                }
                var bootstrap = UxmlPreviewSceneSetup.FindBootstrap();
                var type = bootstrap?.GetType();
                if (type == null) {
                    var types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Where(candidate => candidate.Name == "AppBootstrap" && !candidate.IsAbstract && !candidate.ContainsGenericParameters).ToArray();
                    if (types.Length != 1) throw new InvalidOperationException("Add the intended AppBootstrap to the scene first.");
                    type = types[0];
                }
                var method = UxmlPreviewSceneSetup.FindFactory(type);
                if (method == null) throw new InvalidOperationException("AppBootstrap needs a parameterless static Widget CreateApplication(), or a static [LumaPreview] Widget factory.");
                var factory = LumaPreviewRegistry.RegisterBootstrap(method);
                _definition = null; _codeFactoryId = factory.Id;
                CreateGUI();
                VisualTreeAsset? tree = null;
                string? path = null;
                try { tree = UxmlPreviewGenerator.Generate(factory, out path); }
                catch (NotSupportedException) { /* Custom controls use the explicit live-preview path. */ }
                var document = UxmlPreviewSceneSetup.Configure(tree, "code:" + factory.Id, type);
                if (tree == null) UxmlPreviewSceneSync.ApplyLive("code:" + factory.Id, factory.CreateWidget);
                _outputFolder = path == null ? null : Path.GetDirectoryName(path)!.Replace('\\', '/');
                Selection.activeGameObject = document.gameObject;
                _status.Value = UxmlPreviewSceneSync.GamePreviewEnabled
                    ? "Scene connected · open Game without Play Mode. Save the scene to keep setup; Undo restores the previous configuration."
                    : "Scene connected · Game preview is disabled. Preview remains in this window. Save the scene to keep the connection.";
            } catch (Exception exception) {
                _status.Value = "Auto setup: " + exception.Message;
                Debug.LogWarning(_status.Value);
            }
        }

        private void BindSelectedDocument() {
            if (!UxmlPreviewSceneSync.GamePreviewEnabled) {
                _status.Value = "Enable Preview in Game View before binding a scene document.";
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                _status.Value = "Stop Play Mode before binding a scene preview.";
                return;
            }
            var codeFactory = CurrentCodeFactory();
            if (_definition == null && codeFactory == null) {
                _status.Value = "Select a preview factory first.";
                return;
            }

            var target = Selection.activeGameObject;
            if (target == null || EditorUtility.IsPersistent(target)) {
                _status.Value = "Select a scene GameObject. Bind will configure its UIDocument and missing Panel Settings automatically.";
                return;
            }

            try {
                var tree = _definition != null
                    ? UxmlPreviewGenerator.Generate(_definition, out var path)
                    : UxmlPreviewGenerator.Generate(codeFactory!, out path);
                UxmlPreviewSetup.Bind(target, tree);
                _outputFolder = Path.GetDirectoryName(path)!.Replace('\\', '/');
                UxmlPreviewStatus.ReportSuccess(CurrentStatusId(codeFactory));
                _status.Value = "Bound to " + target.name
                    + " · leave Play Mode stopped; C# compilation now regenerates this UXML automatically.";
                Selection.activeObject = target;
            } catch (Exception exception) {
                _status.Value = "Binding failed: " + exception.Message;
                UxmlPreviewStatus.ReportFailure(CurrentStatusId(codeFactory), CurrentSourceLabel(codeFactory), exception);
                Debug.LogException(exception);
            }
        }

        private LumaPreviewFactory? CurrentCodeFactory() => LumaPreviewRegistry.Find(_codeFactoryId);

        private string CurrentStatusId(LumaPreviewFactory? codeFactory) => _definition != null
            ? "asset:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_definition))
            : "code:" + codeFactory!.Id;

        private string CurrentSourceLabel(LumaPreviewFactory? codeFactory) => _definition != null
            ? AssetDatabase.GetAssetPath(_definition)
            : codeFactory!.DisplayName;

        private void OpenCodeFactory() {
            var factory = CurrentCodeFactory();
            if (factory == null) return;
            var script = AssetDatabase.FindAssets(factory.Method.DeclaringType!.Name + " t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .FirstOrDefault(candidate => candidate != null && candidate.GetClass() == factory.Method.DeclaringType);
            if (script != null) AssetDatabase.OpenAsset(script);
        }
    }

    [CustomEditor(typeof(UxmlPreviewDefinition), true)]
    internal sealed class UxmlPreviewDefinitionInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            if (!DrawDefaultInspector()) return;
            UxmlPreviewAutoGenerator.Schedule((UxmlPreviewDefinition)target);
            foreach (var window in Resources.FindObjectsOfTypeAll<UxmlPreviewWindow>()) window.AutoRebuild();
        }
    }
}
