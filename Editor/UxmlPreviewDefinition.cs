#nullable enable

using UnityEngine;

namespace LumaFlow.Editor {
    /// <summary>Opt-in preview data and a pure factory, executed outside Play Mode.</summary>
    public abstract class UxmlPreviewDefinition : ScriptableObject {
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField, Min(0)] private int _previewWidth = 0;
        [SerializeField, Min(0)] private int _previewHeight = 0;
        [SerializeField] private string _locale = "";
        [SerializeField, Min(0.1f)] private float _textScale = 1f;

        /// <summary>Gets whether this factory regenerates its UXML after script reload and data edits.</summary>
        public bool AutoGenerate => _autoGenerate;

        internal UxmlPreviewEnvironment Environment => new(
            _previewWidth,
            _previewHeight,
            _locale,
            _textScale > 0f ? _textScale : 1f);

        internal Widget CreatePreviewWidget() => Environment.Wrap(CreateWidget());

        public abstract Widget CreateWidget();
    }
}
