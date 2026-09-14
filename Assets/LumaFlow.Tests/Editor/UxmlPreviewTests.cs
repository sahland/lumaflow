#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace LumaFlow.Editor.Tests {
    public sealed class UxmlPreviewTests {
        [Test]
        public void WindowControlsResizeCanvasWithoutChangingItsLayoutScale() {
            var window = ScriptableObject.CreateInstance<UxmlPreviewWindow>();
            try {
                window.Show();
                window.CreateGUI();
                var root = window.rootVisualElement;
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
    }
}
