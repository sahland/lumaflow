#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor.Tests {
    public sealed class EditorToolStartupTests {
        [TestCase(true, false, false, true)]
        [TestCase(false, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, false, true, false)]
        public void StartupRunsOnlyWhenEnabledOncePerInteractiveSession(bool enabled, bool opened, bool batch, bool expected) {
            Assert.That(UxmlPreviewStartup.ShouldOpen(enabled, opened, batch), Is.EqualTo(expected));
        }

        [Test]
        public void StartupPreferenceIsPersistentAndProjectScoped() {
            var before = UxmlPreviewStartup.ShowOnStartup;
            try {
                UxmlPreviewStartup.ShowOnStartup = false;
                Assert.That(EditorPrefs.GetBool(UxmlPreviewStartup.PreferenceKey, true), Is.False);
                Assert.That(UxmlPreviewStartup.PreferenceKey, Does.Contain(Hash128.Compute(Application.dataPath).ToString()));
            } finally { UxmlPreviewStartup.ShowOnStartup = before; }
        }

        [Test]
        public void PerformanceWindowMountsLumaFlowAndDoesNotRecordInEditMode() {
            var window = ScriptableObject.CreateInstance<GuiPerformanceWindow>();
            try {
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Q("lumaflow-mount-host"), Is.Not.Null);
                window.BeginCapture();
                Assert.That(window.IsCapturing, Is.False);
                Assert.That(window.HasRecorders, Is.False);
                window.CancelCapture();
                Assert.That(window.HasRecorders, Is.False);
            } finally { Object.DestroyImmediate(window); }
        }

        [Test]
        public void BindingAddsMissingDocumentAndDefaultPanelWithUndo() {
            var target = new GameObject("LumaFlow preview setup test");
            var tree = ScriptableObject.CreateInstance<VisualTreeAsset>();
            var folderExisted = AssetDatabase.IsValidFolder(UxmlPreviewSetup.Folder);
            try {
                UxmlPreviewSetup.Bind(target, tree);
                var document = target.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                Assert.That(document.panelSettings, Is.Not.Null);
                Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
                Assert.That(document.visualTreeAsset, Is.SameAs(tree));
                Assert.That(UxmlPreviewSetup.GetOrCreatePanelSettings(), Is.SameAs(document.panelSettings));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(target.GetComponent<UIDocument>(), Is.Null);
            } finally {
                Undo.ClearUndo(target);
                Object.DestroyImmediate(target); Object.DestroyImmediate(tree);
                if (!folderExisted) AssetDatabase.DeleteAsset(UxmlPreviewSetup.Folder);
            }
        }

        [Test]
        public void BindingPreservesExistingPanelSettings() {
            var target = new GameObject("LumaFlow existing panel test");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            var tree = ScriptableObject.CreateInstance<VisualTreeAsset>();
            try {
                var document = target.AddComponent<UIDocument>();
                document.panelSettings = panel;
                UxmlPreviewSetup.Bind(target, tree);
                Assert.That(document.panelSettings, Is.SameAs(panel));
                Assert.That(document.visualTreeAsset, Is.SameAs(tree));
            } finally {
                Undo.ClearUndo(target);
                Object.DestroyImmediate(target); Object.DestroyImmediate(panel); Object.DestroyImmediate(tree);
            }
        }
    }
}
