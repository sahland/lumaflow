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

        [LumaPreview("Tests / Code preview")]
        private static Widget CreateCodePreview() => new Text("Code preview");
    }
}
