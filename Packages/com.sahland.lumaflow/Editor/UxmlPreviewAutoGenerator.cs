#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewAutoGenerator {
        private static bool _queued;
        private static bool _running;

        static UxmlPreviewAutoGenerator() {
            Schedule();
        }

        internal static void Schedule() {
            if (_queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _queued = true;
            EditorApplication.delayCall += GenerateAll;
        }

        [MenuItem("Tools/LumaFlow/Regenerate All UXML Previews")]
        internal static void GenerateAll() {
            EditorApplication.delayCall -= GenerateAll;
            _queued = false;
            if (_running || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) {
                Schedule();
                return;
            }

            _running = true;
            try {
                foreach (var guid in AssetDatabase.FindAssets("t:UxmlPreviewDefinition")) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(path);
                    if (definition == null || !definition.AutoGenerate) continue;
                    try {
                        UxmlPreviewGenerator.Generate(definition, out _);
                    } catch (Exception exception) {
                        Debug.LogError($"LumaFlow UXML preview generation failed for '{path}': {exception.Message}", definition);
                        Debug.LogException(exception, definition);
                    }
                }
            } finally {
                _running = false;
            }
        }
    }

    internal sealed class UxmlPreviewDefinitionPostprocessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload) {
            if (didDomainReload) {
                UxmlPreviewAutoGenerator.Schedule();
                return;
            }

            foreach (var path in importedAssets) {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(path) == null) continue;
                UxmlPreviewAutoGenerator.Schedule();
                return;
            }
        }
    }
}
