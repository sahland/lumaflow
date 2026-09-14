#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewAutoGenerator {
        private static readonly HashSet<string> PendingDefinitions = new(StringComparer.Ordinal);
        private static bool _generateAll;
        private static bool _queued;
        private static bool _running;

        static UxmlPreviewAutoGenerator() {
            ScheduleAll();
        }

        internal static void Schedule() => ScheduleAll();

        internal static void Schedule(UxmlPreviewDefinition definition) {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            if (string.IsNullOrEmpty(guid)) return;
            PendingDefinitions.Add(guid);
            QueueGeneration();
        }

        [MenuItem("Tools/LumaFlow/Regenerate All UXML Previews")]
        internal static void GenerateAll() {
            ScheduleAll();
        }

        private static void ScheduleAll() {
            _generateAll = true;
            QueueGeneration();
        }

        private static void QueueGeneration() {
            if (_queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _queued = true;
            EditorApplication.delayCall += GenerateQueued;
        }

        private static void GenerateQueued() {
            EditorApplication.delayCall -= GenerateQueued;
            _queued = false;
            if (_running || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) {
                QueueGeneration();
                return;
            }

            var generateAll = _generateAll;
            var definitions = generateAll
                ? AssetDatabase.FindAssets("t:UxmlPreviewDefinition")
                : new List<string>(PendingDefinitions).ToArray();
            _generateAll = false;
            PendingDefinitions.Clear();
            _running = true;
            try {
                foreach (var guid in definitions) GenerateDefinition(guid);
                if (!generateAll) return;
                foreach (var factory in LumaPreviewRegistry.Factories) GenerateFactory(factory);
            } finally {
                _running = false;
            }
        }

        private static void GenerateDefinition(string guid) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(path);
            if (definition == null || !definition.AutoGenerate) return;
            var statusId = "asset:" + guid;
            try {
                UxmlPreviewGenerator.Generate(definition, out _);
                UxmlPreviewStatus.ReportSuccess(statusId);
            } catch (Exception exception) {
                UxmlPreviewStatus.ReportFailure(statusId, path, exception);
                Debug.LogError($"LumaFlow UXML preview generation failed for '{path}': {exception.Message}", definition);
                Debug.LogException(exception, definition);
            }
        }

        private static void GenerateFactory(LumaPreviewFactory factory) {
            var statusId = "code:" + factory.Id;
            try {
                UxmlPreviewGenerator.Generate(factory, out _);
                UxmlPreviewStatus.ReportSuccess(statusId);
            } catch (Exception exception) {
                UxmlPreviewStatus.ReportFailure(statusId, factory.DisplayName, exception);
                Debug.LogError($"LumaFlow UXML preview generation failed for '{factory.DisplayName}': {exception.Message}");
                Debug.LogException(exception);
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
                var definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(path);
                if (definition != null) UxmlPreviewAutoGenerator.Schedule(definition);
            }
        }
    }
}
