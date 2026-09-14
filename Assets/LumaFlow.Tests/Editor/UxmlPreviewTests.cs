#nullable enable

using System;
using System.Globalization;
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

        [LumaPreview("Tests / Code preview")]
        private static Widget CreateCodePreview() => new Text("Code preview");
    }
}
