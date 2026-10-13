#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;

namespace LumaFlow.Editor.Tests {
    public sealed class UxmlPreviewTests {
        [Test]
        public void GeneratedUxmlKeepsAStableAssetIdentity() {
            var rootExisted = AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot);
            var definitionPath = "Assets/UxmlPreviewDefinition-" + Guid.NewGuid().ToString("N") + ".asset";
            var definition = ScriptableObject.CreateInstance<UxmlPreviewDemo>();
            AssetDatabase.CreateAsset(definition, definitionPath);
            string generatedFolder = "";
            try {
                Assert.That(definition.AutoGenerate, Is.True);
                var first = UxmlPreviewGenerator.Generate(definition, out var firstPath);
                var firstGuid = AssetDatabase.AssetPathToGUID(firstPath);
                generatedFolder = UxmlPreviewGenerator.GetGeneratedFolder(definition);
                var second = UxmlPreviewGenerator.Generate(definition, out var secondPath);
                Assert.That(secondPath, Is.EqualTo(firstPath));
                Assert.That(AssetDatabase.AssetPathToGUID(secondPath), Is.EqualTo(firstGuid));
                Assert.That(second, Is.SameAs(first));
            } finally {
                if (!string.IsNullOrEmpty(generatedFolder)) AssetDatabase.DeleteAsset(generatedFolder);
                AssetDatabase.DeleteAsset(definitionPath);
                if (!rootExisted && AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot))
                    AssetDatabase.DeleteAsset(UxmlPreviewGenerator.GeneratedRoot);
            }
        }

        [Test]
        public void InspectorBindingGeneratesAndAssignsPreviewToDocument() {
            var rootExisted = AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot);
            var definitionPath = "Assets/InspectorUxmlPreviewDefinition-" + Guid.NewGuid().ToString("N") + ".asset";
            var definition = ScriptableObject.CreateInstance<UxmlPreviewDemo>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var gameObject = new GameObject("Preview document");
            AssetDatabase.CreateAsset(definition, definitionPath);
            var generatedFolder = "";
            try {
                var document = gameObject.AddComponent<UIDocument>();
                document.panelSettings = panelSettings;
                var tree = UxmlPreviewDocumentInspector.Bind(definition, new[] { document });
                generatedFolder = UxmlPreviewGenerator.GetGeneratedFolder(definition);
                Assert.That(document.visualTreeAsset, Is.SameAs(tree));
                Assert.That(AssetDatabase.GetAssetPath(tree), Is.EqualTo(generatedFolder + "/Preview.uxml"));
            } finally {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(panelSettings);
                if (!string.IsNullOrEmpty(generatedFolder)) AssetDatabase.DeleteAsset(generatedFolder);
                AssetDatabase.DeleteAsset(definitionPath);
                if (!rootExisted && AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot))
                    AssetDatabase.DeleteAsset(UxmlPreviewGenerator.GeneratedRoot);
            }
        }

        [Test]
        public void FailedRegenerationPreservesTheLastSuccessfulUxml() {
            var rootExisted = AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot);
            var definitionPath = "Assets/FailingUxmlPreviewDefinition-" + Guid.NewGuid().ToString("N") + ".asset";
            var definition = ScriptableObject.CreateInstance<FailingPreviewDefinition>();
            AssetDatabase.CreateAsset(definition, definitionPath);
            var generatedFolder = "";
            try {
                UxmlPreviewGenerator.Generate(definition, out var uxmlPath);
                generatedFolder = UxmlPreviewGenerator.GetGeneratedFolder(definition);
                var successful = File.ReadAllText(uxmlPath);
                definition.Fail = true;
                Assert.Throws<InvalidOperationException>(() => UxmlPreviewGenerator.Generate(definition, out _));
                Assert.That(File.ReadAllText(uxmlPath), Is.EqualTo(successful));
            } finally {
                if (!string.IsNullOrEmpty(generatedFolder)) AssetDatabase.DeleteAsset(generatedFolder);
                AssetDatabase.DeleteAsset(definitionPath);
                if (!rootExisted && AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot))
                    AssetDatabase.DeleteAsset(UxmlPreviewGenerator.GeneratedRoot);
            }
        }

        [Test]
        public void PreviewFailureStatusClearsOnlyAfterTheSameSourceSucceeds() {
            UxmlPreviewStatus.Clear();
            try {
                UxmlPreviewStatus.ReportFailure("first", "First preview", new InvalidOperationException("broken"));
                UxmlPreviewStatus.ReportFailure("second", "Second preview", new InvalidOperationException("also broken"));
                Assert.That(UxmlPreviewStatus.FailureCount, Is.EqualTo(2));
                Assert.That(UxmlPreviewStatus.CurrentMessage, Does.Contain("last successful UXML"));
                UxmlPreviewStatus.ReportSuccess("first");
                Assert.That(UxmlPreviewStatus.FailureCount, Is.EqualTo(1));
                UxmlPreviewStatus.ReportSuccess("second");
                Assert.That(UxmlPreviewStatus.CurrentMessage, Is.Null);
            } finally {
                UxmlPreviewStatus.Clear();
            }
        }

        [Test]
        public void WindowControlsResizeCanvasWithoutChangingItsLayoutScale() {
            var window = ScriptableObject.CreateInstance<UxmlPreviewWindow>();
            try {
                window.Show();
                window.CreateGUI();
                var root = window.rootVisualElement;
                var sources = root.Query<PopupField<string>>().ToList().First();
                Assert.That(sources.choices.Select(sources.formatListItemCallback), Does.Contain("Tests / Code preview"));
                var viewport = root.Q<VisualElement>("generated-viewport");
                window.SetPreviewViewport(800, 480, 50);
                Assert.That(viewport.style.width.value.value, Is.EqualTo(800));
                Assert.That(viewport.parent.style.width.value.value, Is.EqualTo(400));
                Assert.That(root.Q<VisualElement>("runtime-pane"), Is.Null);
                root.Query<Toggle>().ToList().Single(toggle => toggle.label == "Compare runtime").value = true;
                Assert.That(root.Q<VisualElement>("runtime-pane"), Is.Not.Null);
                window.SetPreviewViewport(-1, 480, 50);
                Assert.That(viewport.style.width.value.value, Is.EqualTo(100));
            } finally {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ExportEscapesTextAndPreservesLayoutStylesAcrossCultures() {
            var previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var export = UxmlPreviewExporter.Export(new SizedBox(new Text("<b>" + "&\""), width: 123.5f), "Preview.uss");
                var xml = XDocument.Parse(export.Uxml);
                Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "Label").Attribute("text")!.Value,
                    Is.EqualTo("<b>&\""));
                Assert.That(export.Uss, Does.Contain("width: 123.5px"));
            } finally {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ExportSettlesAnimationsWithoutCompletionCallbacks() {
            var calls = 0;
            var widget = new AnimatedStyle<float>(
                new TweenAnimationBuilder<float>(new FloatTween(0f, 1f), TimeSpan.FromSeconds(1),
                    value => new Text(value == 1f ? "Settled" : "Hidden"),
                    onEnd: () => calls++, behavior: AnimationBehavior.Preserve),
                new FloatTween(0f, 1f), TimeSpan.FromSeconds(1), AnimatedStyleProperties.Opacity,
                onEnd: () => calls++, behavior: AnimationBehavior.Preserve);
            var first = UxmlPreviewExporter.Export(widget, "Preview.uss");
            Assert.That(first.Uxml, Does.Contain("Settled"));
            Assert.That(first.Uss, Does.Contain("opacity: 1"));
            Assert.That(UxmlPreviewExporter.Export(widget, "Preview.uss"), Is.EqualTo(first));
            Assert.That(calls, Is.Zero);

            // Export must not leak its snapshot mode into a subsequent runtime mount.
            var root = new VisualElement();
            using var mount = global::LumaFlow.LumaFlow.Mount(widget, root);
            Assert.That(root.Q<Label>().text, Is.EqualTo("Hidden"));
            Assert.That(root[0].style.opacity.value, Is.Zero);
        }

        [Test]
        public void LivePreviewSupportsCustomNativeAndReleasesItBeforeRuntime() {
            var globalEvents = typeof(EditorApplication).GetField("globalEventHandler",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var previousHandler = globalEvents?.GetValue(null);
            var gameObject = new GameObject("Live preview test");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            var native = new PreviewNativeLabel { text = "Custom renderer" };
            try {
                var document = gameObject.AddComponent<UIDocument>();
                document.panelSettings = panel;
                var binding = gameObject.AddComponent<UxmlPreviewBinding>();
                binding.Configure("code:Tests.Live", binding, null);
                UxmlPreviewSceneSync.ApplyLive(binding.SourceId, () => new Native(native));
                Assert.That(UxmlPreviewSceneSync.LivePreviewCount, Is.EqualTo(1));
                Assert.That(document.rootVisualElement.Q<PreviewNativeLabel>(), Is.SameAs(native));
                Assert.That(native.enabledSelf, Is.True, "Preview must not grey out controls.");
                UxmlPreviewSceneSync.RestoreRuntimeTrees();
                Assert.That(UxmlPreviewSceneSync.LivePreviewCount, Is.Zero);
                Assert.That(native.parent, Is.Null);
                Assert.That(document.rootVisualElement.Q("lumaflow-live-preview"), Is.Null);
                Assert.That(document.visualTreeAsset, Is.Null);
                Assert.That(globalEvents?.GetValue(null), Is.EqualTo(previousHandler), "Game input hook must be released with the last live preview.");
            } finally {
                UxmlPreviewSceneSync.RestoreRuntimeTrees();
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        private sealed class PreviewNativeLabel : Label { }

        [Test]
        public void GamePreviewCheckboxDisablesSceneMountButKeepsWindowPreview() {
            var previous = UxmlPreviewSceneSync.GamePreviewEnabled;
            var gameObject = new GameObject("Optional Game preview");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            var window = ScriptableObject.CreateInstance<UxmlPreviewWindow>();
            var originalTree = ScriptableObject.CreateInstance<VisualTreeAsset>();
            var previewTree = ScriptableObject.CreateInstance<VisualTreeAsset>();
            try {
                var document = gameObject.AddComponent<UIDocument>();
                document.panelSettings = panel;
                var binding = gameObject.AddComponent<UxmlPreviewBinding>();
                binding.Configure("code:Tests.OptionalGame", binding, originalTree);
                UxmlPreviewSceneSync.GamePreviewEnabled = true;
                UxmlPreviewSceneSync.ApplyLive(binding.SourceId, () => new Text("Game preview"));
                Assert.That(document.rootVisualElement.Q<Label>().text, Is.EqualTo("Game preview"));
                window.Show(); window.CreateGUI();
                var checkbox = window.rootVisualElement.Query<Toggle>().ToList().Single(item => item.label == "Preview in Game View");
                Assert.That(checkbox.value, Is.True);
                checkbox.value = false;
                Assert.That(UxmlPreviewSceneSync.GamePreviewEnabled, Is.False);
                Assert.That(UxmlPreviewSceneSync.LivePreviewCount, Is.Zero);
                Assert.That(document.rootVisualElement.Q("lumaflow-live-preview"), Is.Null);
                Assert.That(document.visualTreeAsset, Is.SameAs(originalTree));
                UxmlPreviewSceneSync.Apply(binding.SourceId, previewTree);
                Assert.That(document.visualTreeAsset, Is.SameAs(originalTree), "Generated UXML must also respect the Game preview switch.");
                Assert.That(window.rootVisualElement.Q("generated-viewport"), Is.Not.Null);
                UxmlPreviewSceneSync.ApplyLive(binding.SourceId, () => throw new Exception("Disabled Game factory must not run"));
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Single(item => item.label == "Preview in Game View").value, Is.False);
                Assert.That(window.rootVisualElement.Query<UnityEngine.UIElements.Button>().ToList().Any(item => item.text == "Auto setup scene"), Is.True);
                UxmlPreviewSceneSync.GamePreviewEnabled = true;
                UxmlPreviewSceneSync.ApplyLive(binding.SourceId, () => new Text("Restored Game preview"));
                Assert.That(document.rootVisualElement.Q<Label>().text, Is.EqualTo("Restored Game preview"));
            } finally {
                UnityEngine.Object.DestroyImmediate(window);
                UxmlPreviewSceneSync.RestoreRuntimeTrees();
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(originalTree);
                UnityEngine.Object.DestroyImmediate(previewTree);
                UxmlPreviewSceneSync.GamePreviewEnabled = previous;
            }
        }

        [Test]
        public void LivePreviewDoesNotBlockLocalNavigationEvents() {
            var window = ScriptableObject.CreateInstance<InputTestWindow>();
            try {
                window.Show();
                var root = window.rootVisualElement;
                var calls = 0;
                using var mount = UxmlPreviewLiveSnapshot.Mount(new global::LumaFlow.Button("Local navigation", () => calls++), root, new Vector2(640, 480));
                var button = root.Q<UnityEngine.UIElements.Button>();
                using var submit = NavigationSubmitEvent.GetPooled();
                submit.target = button;
                button.SendEvent(submit);
                Assert.That(calls, Is.EqualTo(1));
            } finally { UnityEngine.Object.DestroyImmediate(window); }
        }
        private sealed class InputTestWindow : EditorWindow { }

        [Test]
        public void ExportRejectsNativeAndReleasesItsBorrowedElement() {
            var element = new VisualElement();
            Assert.Throws<NotSupportedException>(() => UxmlPreviewExporter.Export(new Native(element), "Preview.uss"));
            Assert.That(element.parent, Is.Null);
        }

        [Test]
        public void ExportIsDeterministicAndDoesNotInvokeButtonCallbacks() {
            var calls = 0;
            var widget = new Button("Preview", () => calls++);
            var first = UxmlPreviewExporter.Export(widget, "Preview.uss");
            Assert.That(UxmlPreviewExporter.Export(widget, "Preview.uss"), Is.EqualTo(first));
            Assert.That(calls, Is.Zero);
            Assert.That(first.Uxml, Does.Contain("ui:Button"));
        }

        [Test]
        public void ExportRequiresSiblingStylesheet() {
            Assert.Throws<ArgumentException>(() => UxmlPreviewExporter.Export(new Text("Test"), "../other.uss"));
        }

        [Test]
        public void ExportResolvesLayoutBuilderFromPreviewViewport() {
            var export = UxmlPreviewExporter.Export(
                new LayoutBuilder((context, constraints) => new Text(
                    $"{context.MediaQuery.Width:0} x {constraints.MaxWidth:0}")),
                "Preview.uss",
                new Vector2(390f, 844f));
            var xml = XDocument.Parse(export.Uxml);
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "Label").Attribute("text")!.Value,
                Is.EqualTo("390 x 390"));
        }

        [Test]
        public void ExportPreservesFractionalLayoutLengths() {
            var export = UxmlPreviewExporter.Export(
                new SizedBox(
                    new FractionallySizedBox(new Text("Relative"), 0.5f, 0.25f),
                    width: 400f,
                    height: 200f),
                "Preview.uss");

            Assert.That(export.Uss, Does.Contain("width: 50%"));
            Assert.That(export.Uss, Does.Contain("height: 25%"));
        }

        [Test]
        public void ExportSupportsCommonControlsWithoutInvokingCallbacks() {
            var callbacks = 0;
            var export = UxmlPreviewExporter.Export(new Column(new Widget[] {
                new Slider(new State<float>(0.25f), 0f, 1f, "Scale", onChanged: _ => callbacks++),
                new Checkbox(new State<bool>(true), "Enabled", onChanged: _ => callbacks++),
                new Switch(new State<bool>(false), "Visible", onChanged: _ => callbacks++),
                new TextField(new State<string>("Luma"), "Name", "Project name", onChanged: _ => callbacks++),
                new Dropdown<string>(new State<string>("Two"), new[] { "One", "Two" }, value => value,
                    "Mode", onChanged: _ => callbacks++),
                new SizedBox(new ScrollView(new Text("Scrollable")), height: 80f)
            }), "Preview.uss");
            var xml = XDocument.Parse(export.Uxml);

            Assert.That(callbacks, Is.Zero);
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "Slider").Attribute("value")!.Value,
                Is.EqualTo("0.25"));
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "Toggle").Attribute("value")!.Value,
                Is.EqualTo("true"));
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "TextField")
                .Attribute("placeholder-text")!.Value, Is.EqualTo("Project name"));
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "DropdownField")
                .Attribute("choices")!.Value, Is.EqualTo("One,Two"));
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "ScrollView")
                .Descendants().Single(element => element.Name.LocalName == "Label").Attribute("text")!.Value,
                Is.EqualTo("Scrollable"));
            Assert.That(export.Uss, Does.Contain("border-top-width: 1px"));

            var folder = "Assets/UxmlControlPreview-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            try {
                File.WriteAllText(folder + "/Preview.uss", export.Uss);
                File.WriteAllText(folder + "/Preview.uxml", export.Uxml);
                AssetDatabase.ImportAsset(folder + "/Preview.uss", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(folder + "/Preview.uxml", ImportAssetOptions.ForceSynchronousImport);
                var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folder + "/Preview.uxml");
                Assert.That(tree, Is.Not.Null);
                var clone = new VisualElement();
                tree!.CloneTree(clone);
                Assert.That(clone.Q<UnityEngine.UIElements.Slider>().value, Is.EqualTo(0.25f));
                Assert.That(clone.Q<UnityEngine.UIElements.Toggle>().value, Is.True);
                Assert.That(clone.Q<UnityEngine.UIElements.TextField>().textEdition.placeholder, Is.EqualTo("Project name"));
                Assert.That(clone.Q<DropdownField>().choices, Is.EqualTo(new[] { "One", "Two" }));
                Assert.That(clone.Q<UnityEngine.UIElements.ScrollView>().Q<Label>().text, Is.EqualTo("Scrollable"));
            } finally {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void CodePreviewAppliesScenarioViewportLocaleAndTextScale() {
            var factory = LumaPreviewRegistry.Factories.Single(candidate => candidate.DisplayName == "Tests / Code preview");
            Assert.That(factory.ViewportSize, Is.EqualTo(new Vector2(390f, 844f)));

            var export = UxmlPreviewExporter.Export(factory.CreateWidget(), "Preview.uss", factory.ViewportSize);
            var xml = XDocument.Parse(export.Uxml);
            Assert.That(xml.Descendants().Single(element => element.Name.LocalName == "Label").Attribute("text")!.Value,
                Is.EqualTo("ru-RU / 390x844 / 1.25"));
            Assert.That(export.Uss, Does.Contain("font-size: 12.5px"));
            Assert.Throws<ArgumentException>(() => new UxmlPreviewEnvironment(390, 0, null, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UxmlPreviewEnvironment(0, 0, null, 0f));
        }

        [LumaPreview("Tests / Code preview", Width = 390, Height = 844, Locale = "ru-RU", TextScale = 1.25f)]
        private static Widget CreateCodePreview() => new EnvironmentProbe();

        private sealed class EnvironmentProbe : StatelessWidget {
            public override Widget Build(BuildContext context) => new Text(
                $"{Localizations.LocaleOf(context)} / {context.MediaQuery.Width:0}x{context.MediaQuery.Height:0} / "
                + context.TextScaler.ScaleFactor.ToString("0.##", CultureInfo.InvariantCulture),
                new TextStyle(fontSize: 10f));
        }

        private sealed class FailingPreviewDefinition : UxmlPreviewDefinition {
            internal bool Fail { get; set; }

            public override Widget CreateWidget() {
                if (Fail) throw new InvalidOperationException("Intentional preview failure.");
                return new Text("Last known good");
            }
        }
    }
}
