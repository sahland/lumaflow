#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor.Tests {
    public sealed class EditorToolStartupTests {
#if !LUMAFLOW_DEVELOPMENT
        [Test]
        public void ConsumerMenuContainsOnlyPrimaryTools() {
            var entries = typeof(UxmlPreviewWindow).Assembly.GetTypes()
                .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                .SelectMany(method => method.GetCustomAttributes(typeof(MenuItem), false).Cast<MenuItem>())
                .Select(item => item.menuItem).Where(path => path.StartsWith("Tools/LumaFlow/", System.StringComparison.Ordinal)).ToArray();
            Assert.That(entries, Is.EquivalentTo(new[] { "Tools/LumaFlow/UXML", "Tools/LumaFlow/GUI Performance" }));
        }
#endif
        [Test]
        public void EditorControlsUseBrandColorsAndSkinIndependentCheckmarks() {
            Assert.That(EditorToolWidgets.Background, Is.EqualTo((Color)new Color32(243, 242, 238, 255)));
            Assert.That(EditorToolWidgets.Theme.Colors.Primary, Is.EqualTo((Color)new Color32(63, 70, 84, 255)));
            var root = new VisualElement();
            var value = new State<bool>(true);
            using var mount = global::LumaFlow.LumaFlow.Mount(new Theme(EditorToolWidgets.Theme, new Checkbox(value, "Selected")), root);
            var toggle = root.Q<UnityEngine.UIElements.Toggle>();
            var mark = toggle.Q(className: "unity-toggle__checkmark");
            Assert.That(mark.style.backgroundImage.value.vectorImage, Is.Not.Null);
            Assert.That(mark.style.unityBackgroundImageTintColor.value, Is.EqualTo(EditorToolWidgets.Theme.Colors.OnPrimary));
            value.Value = false;
            Assert.That(mark.style.backgroundImage.value.vectorImage, Is.Null);
            Assert.That(mark.style.backgroundImage.value.texture, Is.Null);
            Assert.That(mark.style.backgroundColor.value, Is.EqualTo(EditorToolWidgets.Surface));
            value.Value = true;
            Assert.That(mark.style.backgroundImage.value.vectorImage, Is.Not.Null);
        }

        [Test]
        public void DropdownSelectedTextUsesTheThemeForeground() {
            var root = new VisualElement();
            using var mount = global::LumaFlow.LumaFlow.Mount(new Theme(EditorToolWidgets.Theme,
                new Dropdown<string>(new State<string>("One"), new[] { "One", "Two" }, value => value)), root);
            var input = root.Q<PopupField<string>>().Q(className: "unity-base-field__input");
            var text = input.Q<TextElement>();
            Assert.That(text, Is.Not.Null);
            Assert.That(text.style.color.value, Is.EqualTo(EditorToolWidgets.Ink));
        }

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
