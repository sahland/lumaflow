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
                Assert.That(root.Q<PopupField<string>>("code-preview").choices, Does.Contain("Tests / Code preview"));
                var viewport = root.Q<VisualElement>("generated-viewport");
                root.Q<IntegerField>("viewport-width").value = 800;
                root.Q<PopupField<string>>("viewport-zoom").value = "50%";
                Assert.That(viewport.style.width.value.value, Is.EqualTo(800));
                Assert.That(viewport.parent.style.width.value.value, Is.EqualTo(400));
                Assert.That(root.Q<VisualElement>("runtime-pane").style.display.value, Is.EqualTo(DisplayStyle.None));
                root.Q<ToolbarToggle>("compare-runtime").value = true;
                Assert.That(root.Q<VisualElement>("runtime-pane").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                root.Q<IntegerField>("viewport-width").value = -1;
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
