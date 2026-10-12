#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewQuickStart {
        private const string Pending = "LumaFlow.QuickStart.Script";
        private const string SceneKey = "LumaFlow.QuickStart.Scene";
        internal const string Folder = "Assets/LumaFlowApp";
        static UxmlPreviewQuickStart() {
            if (!string.IsNullOrEmpty(SessionState.GetString(Pending, ""))) EditorApplication.update += ResumeWhenReady;
        }
        private static void ResumeWhenReady() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= ResumeWhenReady;
            Resume();
        }
        internal static bool NeedsStarter => UxmlPreviewSceneSetup.FindBootstrap() == null
            && !System.Linq.Enumerable.Any(TypeCache.GetTypesDerivedFrom<MonoBehaviour>(), type => type.Name == "AppBootstrap" && !type.IsAbstract && !type.ContainsGenericParameters);

        internal static void CreateStarter() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before auto setup.");
            var scriptPath = Folder + "/AppBootstrap.cs";
            if (File.Exists(scriptPath)) throw new InvalidOperationException("Starter script already exists. Resolve compilation errors before trying again.");
            // Resolve the installed package location instead of assuming its folder name.
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UxmlPreviewQuickStart).Assembly);
            var templatePath = package != null ? Path.Combine(package.resolvedPath, "Editor/Templates/AppBootstrap.cs.txt")
                : System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.Select(AssetDatabase.FindAssets("t:TextAsset"), AssetDatabase.GUIDToAssetPath),
                    path => path.EndsWith("/Editor/Templates/AppBootstrap.cs.txt", StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("Cannot locate the starter template.");
            var text = File.ReadAllText(templatePath);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "LumaFlowApp");
            SessionState.SetString(Pending, scriptPath);
            SessionState.SetInt(SceneKey, SceneManager.GetActiveScene().GetHashCode());
            File.WriteAllText(scriptPath, text);
            AssetDatabase.ImportAsset(scriptPath);
        }
        private static void Resume() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Resume; return; }
            var path = SessionState.GetString(Pending, "");
            SessionState.EraseString(Pending);
            if (string.IsNullOrEmpty(path)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().GetHashCode() != SessionState.GetInt(SceneKey, -1)) {
                Debug.LogWarning("LumaFlow starter created. Return to the intended scene and press Auto setup to finish."); return;
            }
            try {
                var type = AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass()
                    ?? throw new InvalidOperationException("Starter has not compiled. Fix Console errors, then press Auto setup again.");
                var method = UxmlPreviewSceneSetup.FindFactory(type)!;
                var factory = LumaPreviewRegistry.RegisterBootstrap(method);
                var tree = UxmlPreviewGenerator.Generate(factory, out _);
                var document = UxmlPreviewSceneSetup.Configure(tree, "code:" + factory.Id, type);
                Selection.activeGameObject = document.gameObject;
                UxmlPreviewWindow.Open();
                foreach (var window in Resources.FindObjectsOfTypeAll<UxmlPreviewWindow>()) window.CreateGUI();
            } catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
