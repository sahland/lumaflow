#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewSceneSync {
        static UxmlPreviewSceneSync() {
            EditorSceneManager.sceneOpened += (_, __) => Schedule();
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingEditMode) RestoreRuntimeTrees();
                if (state == PlayModeStateChange.EnteredEditMode) Schedule();
            };
            Schedule();
        }
        private static bool _queued;
        private static UxmlPreviewBinding[] Bindings() => Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt).Where(scene => scene.isLoaded)
            .SelectMany(scene => scene.GetRootGameObjects())
            .SelectMany(root => root.GetComponentsInChildren<UxmlPreviewBinding>(true)).ToArray();
        private static void Schedule() {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += Refresh;
        }
        private static void Refresh() {
            _queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            foreach (var binding in Bindings()) {
                if (EditorUtility.IsPersistent(binding) || !binding.gameObject.scene.IsValid()) continue;
                try {
                    VisualTreeAsset tree;
                    if (binding.SourceId.StartsWith("code:")) {
                        if (binding.Bootstrap != null && UxmlPreviewSceneSetup.FindFactory(binding.Bootstrap.GetType()) is { } method)
                            LumaPreviewRegistry.RegisterBootstrap(method);
                        var factory = LumaPreviewRegistry.Find(binding.SourceId.Substring(5));
                        if (factory == null) continue;
                        tree = UxmlPreviewGenerator.Generate(factory, out _);
                    } else {
                        var definition = AssetDatabase.LoadAssetAtPath<UxmlPreviewDefinition>(AssetDatabase.GUIDToAssetPath(binding.SourceId.Substring(6)));
                        if (definition == null || !definition.AutoGenerate) continue;
                        tree = UxmlPreviewGenerator.Generate(definition, out _);
                    }
                    binding.Document.visualTreeAsset = tree;
                } catch (Exception exception) { Debug.LogError("LumaFlow scene preview: " + exception.Message, binding); }
            }
        }
        internal static void Apply(string sourceId, VisualTreeAsset tree) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var binding in Bindings())
                if (!EditorUtility.IsPersistent(binding) && binding.SourceId == sourceId && binding.Document.visualTreeAsset != tree)
                    binding.Document.visualTreeAsset = tree;
        }
        internal static void RestoreRuntimeTrees() {
            foreach (var binding in Bindings())
                if (!EditorUtility.IsPersistent(binding)) binding.Document.visualTreeAsset = binding.RuntimeTree;
        }
    }
}
