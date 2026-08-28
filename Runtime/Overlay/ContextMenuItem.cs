#nullable enable

using System;

namespace LumaFlow {

    /// <summary>A selectable entry in a native UI Toolkit context menu.</summary>
    public sealed class ContextMenuItem {
        public ContextMenuItem(string label, Action onSelected, bool enabled = true) {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A context menu item label cannot be empty.", nameof(label));
            Label = label;
            OnSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            Enabled = enabled;
        }

        public string Label { get; }
        public Action OnSelected { get; }
        public bool Enabled { get; }
    }

}
