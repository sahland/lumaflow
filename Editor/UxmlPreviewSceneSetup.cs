#nullable enable
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {
    internal static class UxmlPreviewSceneSetup {
        internal static T[] InScene<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        internal static Component? FindBootstrap() {
            var candidates = InScene<MonoBehaviour>().Where(item => item != null && item.GetType().Name == "AppBootstrap").Cast<Component>().ToArray();
            var selected = Selection.activeGameObject;
            if (selected != null) {
                var match = candidates.FirstOrDefault(item => item.gameObject == selected);
                if (match != null) return match;
            }
            if (candidates.Length > 1) throw new InvalidOperationException("Several AppBootstrap components exist. Select the intended one in Hierarchy.");
            return candidates.SingleOrDefault();
        }

        internal static UIDocument? FindDocument(Component? bootstrap) {
            if (bootstrap != null) {
                var serialized = new SerializedObject(bootstrap);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is UIDocument referenced)
                        return referenced;
                var sameObject = bootstrap.GetComponent<UIDocument>();
                if (sameObject != null) return sameObject;
            }
            var documents = InScene<UIDocument>();
            if (Selection.activeGameObject != null) {
                var selected = documents.FirstOrDefault(doc => doc.gameObject == Selection.activeGameObject);
                if (selected != null) return selected;
            }
            if (documents.Length > 1) throw new InvalidOperationException("Several UIDocuments exist. Select the intended one in Hierarchy.");
            return documents.SingleOrDefault();
        }

        internal static MethodInfo? FindFactory(Type bootstrap) => bootstrap.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => !method.IsGenericMethod && method.GetParameters().Length == 0
                && typeof(Widget).IsAssignableFrom(method.ReturnType)
                && (method.GetCustomAttribute<LumaPreviewAttribute>() != null || method.Name == "CreateApplication"));

        internal static string? DiscoverSceneSource() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null) return null;
            var bootstrap = FindBootstrap();
            var document = FindDocument(bootstrap);
            var binding = document != null ? document.GetComponent<UxmlPreviewBinding>() : null;
            if (bootstrap != null && FindFactory(bootstrap.GetType()) is { } sceneFactory)
                LumaPreviewRegistry.RegisterBootstrap(sceneFactory);
            if (binding != null && !string.IsNullOrEmpty(binding.SourceId)) return binding.SourceId;
            if (bootstrap == null || document == null) return null;
            var method = FindFactory(bootstrap.GetType());
            return method == null ? null : "code:" + LumaPreviewRegistry.RegisterBootstrap(method).Id;
        }

        internal static UIDocument Configure(VisualTreeAsset? tree, string sourceId, Type? bootstrapType = null) {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Auto setup requires a regular scene outside Play Mode.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
            var bootstrap = FindBootstrap();
            var document = FindDocument(bootstrap);
            if (bootstrap == null && bootstrapType == null) {
                var types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                    .Where(type => type.Name == "AppBootstrap" && !type.IsAbstract && !type.ContainsGenericParameters).ToArray();
                if (types.Length > 1) throw new InvalidOperationException("Several AppBootstrap scripts exist. Add the intended component to the scene first.");
                bootstrapType = types.SingleOrDefault();
            }
            if (bootstrap == null && bootstrapType == null) throw new InvalidOperationException("Create the starter AppBootstrap script first.");
            var panel = document != null && document.panelSettings != null ? document.panelSettings : UxmlPreviewSetup.GetOrCreatePanelSettings();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Auto configure LumaFlow scene");
            try {
                var target = bootstrap != null ? bootstrap.gameObject : document != null ? document.gameObject : CreateObject("LumaFlow UI");
                if (bootstrap == null) bootstrap = Undo.AddComponent(target, bootstrapType!);
                if (document == null) document = target.GetComponent<UIDocument>() ?? Undo.AddComponent<UIDocument>(target);
                var binding = document.GetComponent<UxmlPreviewBinding>() ?? Undo.AddComponent<UxmlPreviewBinding>(document.gameObject);
                Undo.RecordObject(binding, "Connect LumaFlow preview");
                var originalTree = document.visualTreeAsset;
                // Older manual bindings may already contain an exported snapshot.
                // That is not application-owned runtime content to restore.
                if (AssetDatabase.GetAssetPath(originalTree).StartsWith(UxmlPreviewGenerator.GeneratedRoot + "/", StringComparison.Ordinal))
                    originalTree = null;
                binding.Configure(sourceId, bootstrap, originalTree);
                Undo.RecordObject(document, "Configure UIDocument");
                document.panelSettings = panel;
                document.visualTreeAsset = UxmlPreviewSceneSync.GamePreviewEnabled ? tree : binding.RuntimeTree;
                var serialized = new SerializedObject(bootstrap);
                var property = serialized.GetIterator();
                while (property.NextVisible(true)) {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null) continue;
                    var field = bootstrap.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field?.FieldType == typeof(UIDocument)) property.objectReferenceValue = document;
                }
                serialized.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(bootstrap);
                PrefabUtility.RecordPrefabInstancePropertyModifications(document);
                PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
                if (InScene<Camera>().Length == 0) {
                    var cameraObject = CreateObject("Main Camera");
                    cameraObject.tag = "MainCamera";
                    cameraObject.transform.position = new Vector3(0,0,-10);
                    Undo.AddComponent<Camera>(cameraObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                return document;
            } catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static GameObject CreateObject(string name) {
            var target = new GameObject(name);
            SceneManager.MoveGameObjectToScene(target, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(target, "Create LumaFlow scene object");
            return target;
        }
    }
}
