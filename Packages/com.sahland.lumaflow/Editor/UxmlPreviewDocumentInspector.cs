#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewDocumentInspector {
        private static IReadOnlyList<PreviewSource>? _sources;

        static UxmlPreviewDocumentInspector() {
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawHeader;
            EditorApplication.projectChanged += InvalidateSources;
        }

        internal static VisualTreeAsset Bind(
            UxmlPreviewDefinition definition,
            IReadOnlyList<UIDocument> documents
        ) => Bind(new PreviewSource(definition), documents);

        private static void DrawHeader(UnityEditor.Editor editor) {
            if (editor.target is not UIDocument || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var documents = editor.targets.OfType<UIDocument>().ToArray();
            if (documents.Length == 0) return;

            var currentPath = AssetDatabase.GetAssetPath(documents[0].visualTreeAsset);
            var current = Sources.FirstOrDefault(source => source.UxmlPath == currentPath);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("LumaFlow Preview", EditorStyles.miniBoldLabel, GUILayout.Width(110f));
            if (GUILayout.Button(current?.Label ?? "Select preview...", EditorStyles.popup)) {
                ShowSourceMenu(current, documents);
            }

            using (new EditorGUI.DisabledScope(current == null)) {
                if (GUILayout.Button("Regenerate", EditorStyles.miniButton, GUILayout.Width(78f))) {
                    TryBind(current!, documents);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static IReadOnlyList<PreviewSource> Sources => _sources ??= DiscoverSources();

        private static IReadOnlyList<PreviewSource> DiscoverSources() => AssetDatabase
            .FindAssets("t:UxmlPreviewDefinition")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>)
            .Where(definition => definition != null)
            .Select(definition => new PreviewSource(definition!))
            .Concat(LumaPreviewRegistry.Factories.Select(factory => new PreviewSource(factory)))
            .OrderBy(source => source.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.UxmlPath, StringComparer.Ordinal)
            .ToArray();

        private static void ShowSourceMenu(
            PreviewSource? current,
            IReadOnlyList<UIDocument> documents
        ) {
            var menu = new GenericMenu();
            if (Sources.Count == 0) {
                menu.AddDisabledItem(new GUIContent("No preview factories found"));
            } else {
                foreach (var source in Sources) {
                    menu.AddItem(new GUIContent(source.Label), source == current, () => TryBind(source, documents));
                }
            }
            menu.ShowAsContext();
        }

        private static void TryBind(PreviewSource source, IReadOnlyList<UIDocument> documents) {
            try {
                Bind(source, documents);
                UxmlPreviewStatus.ReportSuccess(source.StatusId);
            } catch (Exception exception) {
                UxmlPreviewStatus.ReportFailure(source.StatusId, source.Label, exception);
                Debug.LogException(exception);
            }
        }

        private static VisualTreeAsset Bind(PreviewSource source, IReadOnlyList<UIDocument> documents) {
            if (documents.Count == 0) throw new ArgumentException("Select at least one UIDocument.", nameof(documents));
            var missingPanel = documents.FirstOrDefault(document => document == null || document.panelSettings == null);
            if (missingPanel != null) {
                throw new InvalidOperationException(
                    $"UIDocument '{missingPanel.gameObject.name}' needs Panel Settings before binding a preview.");
            }

            var tree = source.Generate();
            Undo.RecordObjects(documents.Cast<UnityEngine.Object>().ToArray(), "Bind LumaFlow UXML preview");
            foreach (var document in documents) {
                document.visualTreeAsset = tree;
                EditorUtility.SetDirty(document);
            }
            return tree;
        }

        private static void InvalidateSources() => _sources = null;

        private sealed class PreviewSource {
            private readonly UxmlPreviewDefinition? _definition;
            private readonly LumaPreviewFactory? _factory;

            internal PreviewSource(UxmlPreviewDefinition definition) {
                _definition = definition;
                var path = AssetDatabase.GetAssetPath(definition);
                Label = "Asset / " + definition.name;
                StatusId = "asset:" + AssetDatabase.AssetPathToGUID(path);
                UxmlPath = UxmlPreviewGenerator.GetGeneratedFolder(definition) + "/Preview.uxml";
            }

            internal PreviewSource(LumaPreviewFactory factory) {
                _factory = factory;
                Label = "Code / " + factory.DisplayName;
                StatusId = "code:" + factory.Id;
                UxmlPath = UxmlPreviewGenerator.GetGeneratedFolder(factory) + "/Preview.uxml";
            }

            internal string Label { get; }
            internal string StatusId { get; }
            internal string UxmlPath { get; }

            internal VisualTreeAsset Generate() => _definition != null
                ? UxmlPreviewGenerator.Generate(_definition, out _)
                : UxmlPreviewGenerator.Generate(_factory!, out _);
        }
    }
}
