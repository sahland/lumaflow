#nullable enable
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    [InitializeOnLoad]
    internal static class UxmlPreviewStartup {
        private static string ProjectKey => "LumaFlow.Uxml." + Hash128.Compute(Application.dataPath);
        internal static string PreferenceKey => ProjectKey + ".ShowOnStartup";
        internal static string SourceKey => ProjectKey + ".Source";
        private static string SessionKey => ProjectKey + ".Opened";
        internal static bool ShowOnStartup {
            get => EditorPrefs.GetBool(PreferenceKey, true);
            set => EditorPrefs.SetBool(PreferenceKey, value);
        }

        static UxmlPreviewStartup() {
            if (!Application.isBatchMode) EditorApplication.delayCall += TryOpen;
        }

        internal static bool ShouldOpen(bool enabled, bool alreadyOpened, bool batchMode) => enabled && !alreadyOpened && !batchMode;

        private static void TryOpen() {
            if (!ShouldOpen(ShowOnStartup, SessionState.GetBool(SessionKey, false), Application.isBatchMode)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += TryOpen;
                return;
            }
            SessionState.SetBool(SessionKey, true);
            // A restored layout already owns its window; do not steal focus after a reload.
            if (!EditorWindow.HasOpenInstances<UxmlPreviewWindow>()) UxmlPreviewWindow.Open();
        }
    }

    internal static class BuiltInUxmlPreview {
        [LumaPreview("LumaFlow / Welcome", Width = 640, Height = 480)]
        private static Widget Welcome() {
            var demo = ScriptableObject.CreateInstance<UxmlPreviewDemo>();
            demo.hideFlags = HideFlags.HideAndDontSave;
            try { return demo.CreateWidget(); }
            finally { Object.DestroyImmediate(demo); }
        }
    }
}
