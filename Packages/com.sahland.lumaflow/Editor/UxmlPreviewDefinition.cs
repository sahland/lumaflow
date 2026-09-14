#nullable enable

using UnityEngine;

namespace LumaFlow.Editor {
    /// <summary>Opt-in preview data and a pure factory, executed outside Play Mode.</summary>
    public abstract class UxmlPreviewDefinition : ScriptableObject {
        public abstract Widget CreateWidget();
    }
}
