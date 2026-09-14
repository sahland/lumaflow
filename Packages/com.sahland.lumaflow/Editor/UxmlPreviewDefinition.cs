#nullable enable

using UnityEngine;

namespace LumaFlow.Editor {
    /// <summary>Opt-in preview data and a pure factory, executed outside Play Mode.</summary>
    public abstract class UxmlPreviewDefinition : ScriptableObject {
        [SerializeField] private bool _autoGenerate = true;

        /// <summary>Gets whether this factory regenerates its UXML after script reload and data edits.</summary>
        public bool AutoGenerate => _autoGenerate;

        public abstract Widget CreateWidget();
    }
}
