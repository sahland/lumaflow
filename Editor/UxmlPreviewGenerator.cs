#nullable enable

using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    /// <summary>Creates stable generated assets for a saved preview definition.</summary>
    public static class UxmlPreviewGenerator {
        public const string GeneratedRoot = "Assets/LumaFlowGenerated";

        public static VisualTreeAsset Generate(UxmlPreviewDefinition definition, out string uxmlPath) {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var definitionPath = AssetDatabase.GetAssetPath(definition);
            var guid = AssetDatabase.AssetPathToGUID(definitionPath);
            if (string.IsNullOrEmpty(guid)) {
                throw new InvalidOperationException("Save the preview definition as an asset before generating UXML.");
            }

            return Generate(definition.CreatePreviewWidget(), guid, definition.Environment.ResolveViewport(), out uxmlPath);
        }

        internal static VisualTreeAsset Generate(LumaPreviewFactory factory, out string uxmlPath) {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return Generate(factory.CreateWidget(), "code-" + UnityEngine.Hash128.Compute(factory.Id),
                factory.ViewportSize, out uxmlPath);
        }

        private static VisualTreeAsset Generate(Widget widget, string folderName, UnityEngine.Vector2 viewportSize, out string uxmlPath) {
            EnsureFolder("Assets", "LumaFlowGenerated");
            var folder = GeneratedRoot + "/" + folderName;
            EnsureFolder(GeneratedRoot, folderName);
            var ussPath = folder + "/Preview.uss";
            uxmlPath = folder + "/Preview.uxml";
            var export = UxmlPreviewExporter.Export(widget, "Preview.uss", viewportSize);
            var ussChanged = WriteChanged(ussPath, export.Uss);
            var uxmlChanged = WriteChanged(uxmlPath, export.Uxml);
            if (ussChanged) AssetDatabase.ImportAsset(ussPath, ImportAssetOptions.ForceSynchronousImport);
            if (uxmlChanged) AssetDatabase.ImportAsset(uxmlPath, ImportAssetOptions.ForceSynchronousImport);
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            return tree ?? throw new InvalidOperationException("Unity could not import the generated UXML.");
        }

        public static string GetGeneratedFolder(UxmlPreviewDefinition definition) {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            return string.IsNullOrEmpty(guid) ? string.Empty : GeneratedRoot + "/" + guid;
        }

        internal static string GetGeneratedFolder(LumaPreviewFactory factory) => factory == null
            ? throw new ArgumentNullException(nameof(factory))
            : GeneratedRoot + "/code-" + UnityEngine.Hash128.Compute(factory.Id);

        private static void EnsureFolder(string parent, string name) {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static bool WriteChanged(string path, string contents) {
            if (File.Exists(path) && File.ReadAllText(path) == contents) return false;
            File.WriteAllText(path, contents);
            return true;
        }
    }
}
