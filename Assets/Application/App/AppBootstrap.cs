#nullable enable

using System.Collections;
using LumaFlow;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR && UNITY_6000_4_OR_NEWER
using LumaFlow.Editor;
#endif

using Framework = LumaFlow.LumaFlow;

namespace Assets.Application.App {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppBootstrap : MonoBehaviour {
        [SerializeField] private UIDocument _uiDocument = null!;

        //private AppDependecies? _dependencies;
        private MountHandle? _mount;
        private Coroutine? _mountRoutine;
#if UNITY_EDITOR && UNITY_6000_4_OR_NEWER
        private System.IDisposable? _previewToolbarRegistration;
#endif

        private void Awake() {
            _uiDocument ??= GetComponent<UIDocument>();
        }

        private void OnEnable() {
            if (_uiDocument == null) return;
            if (_mount?.IsMounted == true || _mountRoutine != null) return;

            _mountRoutine = StartCoroutine(MountAfterDocumentInitialization());
        }

        private IEnumerator MountAfterDocumentInitialization() {
            // Runtime panels on devices can attach later than they do in the Editor.
            // Mount only after UIDocument owns a live panel.
            while (isActiveAndEnabled
                   && _uiDocument is not null
                   && _uiDocument.rootVisualElement?.panel is null) {
                yield return null;
            }

            _mountRoutine = null;
            if (!isActiveAndEnabled || _uiDocument == null) yield break;

            var root = _uiDocument.rootVisualElement;

            if (root?.panel is null) yield break;

            //_dependencies = AppDependecies.Create();

            _mount = Framework.Mount(
                CreateApplication(),
                root
                );
#if UNITY_EDITOR && UNITY_6000_4_OR_NEWER
            _previewToolbarRegistration?.Dispose();
            _previewToolbarRegistration = PlayModePreviewToolbar.Register(HotReload, HotRestart);
#endif
        }

        private static AppRoot CreateApplication() => new();

        private void HotReload() => _mount?.Rebuild();

        private void HotRestart() => _mount?.Restart(CreateApplication());


        private void OnDisable() {
            if (_mountRoutine != null) {
                StopCoroutine(_mountRoutine);
                _mountRoutine = null;
            }
#if UNITY_EDITOR && UNITY_6000_4_OR_NEWER
            _previewToolbarRegistration?.Dispose();
            _previewToolbarRegistration = null;
#endif
            _mount?.Dispose();
            _mount = null;

            //_dependencies?.Dispose();
            //_dependencies = null;
        }
    }
}
