#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    internal static class UxmlPreviewSetup {
        internal const string Folder = UxmlPreviewGenerator.GeneratedRoot + "/EditorTools";

        internal static PanelSettings GetOrCreatePanelSettings() {
            if (!AssetDatabase.IsValidFolder(UxmlPreviewGenerator.GeneratedRoot))
                AssetDatabase.CreateFolder("Assets", "LumaFlowGenerated");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(UxmlPreviewGenerator.GeneratedRoot, "EditorTools");
            var panelPath = Folder + "/PreviewPanelSettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (existing != null) return existing;
            var themePath = Folder + "/PreviewTheme.tss";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            if (theme == null) {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath, ImportAssetOptions.ForceSynchronousImport);
                theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            }
            if (theme == null) throw new System.InvalidOperationException("Unity could not import the default preview theme.");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = 1;
            panel.themeStyleSheet = theme;
            AssetDatabase.CreateAsset(panel, panelPath);
            return panel;
        }

        internal static void Bind(GameObject target, VisualTreeAsset tree) {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (tree == null) throw new System.ArgumentNullException(nameof(tree));
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(target) || !target.scene.IsValid())
                throw new System.InvalidOperationException("Bind a scene GameObject outside Play Mode.");
            // Prepare assets before the scene mutation; a failed import leaves the scene untouched.
            var document = target.GetComponent<UIDocument>();
            var panel = document != null && document.panelSettings != null
                ? document.panelSettings : GetOrCreatePanelSettings();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure LumaFlow UXML preview");
            try {
                if (document == null) document = Undo.AddComponent<UIDocument>(target);
                Undo.RecordObject(document, "Bind LumaFlow UXML preview");
                document.panelSettings = panel;
                document.visualTreeAsset = tree;
                EditorUtility.SetDirty(document);
                Undo.CollapseUndoOperations(group);
            } catch {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
    }
}
