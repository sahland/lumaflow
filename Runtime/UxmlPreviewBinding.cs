#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>Stores an edit-mode preview connection, without running the application in Edit Mode.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument)), DefaultExecutionOrder(-10000)]
    public sealed class UxmlPreviewBinding : MonoBehaviour {
        [SerializeField, HideInInspector] private string _sourceId = "";
        [SerializeField, HideInInspector] private VisualTreeAsset? _runtimeTree;
        [SerializeField, HideInInspector] private Component? _bootstrap;
        public string SourceId => _sourceId;
        public Component? Bootstrap => _bootstrap;
        public VisualTreeAsset? RuntimeTree => _runtimeTree;
        public UIDocument Document => GetComponent<UIDocument>();

        /// <summary>Editor tooling records Undo before calling this method.</summary>
        public void Configure(string sourceId, Component bootstrap, VisualTreeAsset? runtimeTree) {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new System.ArgumentException("A preview source is required.", nameof(sourceId));
            if (bootstrap == null) throw new System.ArgumentNullException(nameof(bootstrap));
            if (string.IsNullOrEmpty(_sourceId)) _runtimeTree = runtimeTree;
            _sourceId = sourceId;
            _bootstrap = bootstrap;
        }

        // A saved scene contains the editor snapshot. Restore the original asset
        // before the application's bootstrap mounts, including in Player builds.
        private void Awake() => RestoreRuntimeTree();
        private void OnEnable() => RestoreRuntimeTree();
        private void RestoreRuntimeTree() {
            if (Application.isPlaying && !string.IsNullOrEmpty(_sourceId)) Document.visualTreeAsset = _runtimeTree;
        }
    }
}
