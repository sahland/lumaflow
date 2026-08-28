#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Legacy checkbox-shaped boolean input. Prefer <see cref="Checkbox"/> for
    /// checkbox semantics or <see cref="Switch"/> for a track-and-thumb control.
    /// </summary>
    internal sealed class Toggle : Widget {
        internal Toggle(
            State<bool> value,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
        }

        internal Toggle(
            FormField<bool> field,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null)
            : this(field?.Value ?? throw new ArgumentNullException(nameof(field)), label, enabled, focusNode) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned value state.
        /// </summary>
        public State<bool> Value { get; }

        public string? Label { get; }

        public bool Enabled { get; }

        public FocusNode? FocusNode { get; }

        public FormField<bool>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new ToggleNode(this);
        }
    }

    /// <summary>A controlled switch with a track-and-thumb presentation.</summary>
    public sealed class Switch : Widget {
        public Switch(State<bool> value, string? label = null, bool enabled = true, FocusNode? focusNode = null, SwitchStyle? style = null, Action<bool>? onChanged = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
            Style = style;
            OnChanged = onChanged;
        }

        public Switch(FormField<bool> field, string? label = null, bool enabled = true, FocusNode? focusNode = null, SwitchStyle? style = null, Action<bool>? onChanged = null)
            : this(field?.Value ?? throw new ArgumentNullException(nameof(field)), label, enabled, focusNode, style, onChanged) {
            Field = field;
        }

        public State<bool> Value { get; }
        public string? Label { get; }
        public bool Enabled { get; }
        public FocusNode? FocusNode { get; }
        public SwitchStyle? Style { get; }
        /// <summary>Called after a user-originated value is committed to <see cref="Value"/>.</summary>
        public Action<bool>? OnChanged { get; }
        public FormField<bool>? Field { get; }
        internal override WidgetNode CreateNode() => new SwitchNode(this);
    }

    /// <summary>State-aware visual configuration for a <see cref="Switch"/>.</summary>
    public sealed class SwitchStyle {
        public SwitchStyle(
            WidgetStateProperty<UnityEngine.Color?>? trackColor = null,
            WidgetStateProperty<UnityEngine.Color?>? thumbColor = null,
            WidgetStateProperty<float?>? width = null,
            WidgetStateProperty<float?>? height = null) {
            TrackColor = trackColor;
            ThumbColor = thumbColor;
            Width = width;
            Height = height;
        }

        public WidgetStateProperty<UnityEngine.Color?>? TrackColor { get; }
        public WidgetStateProperty<UnityEngine.Color?>? ThumbColor { get; }
        public WidgetStateProperty<float?>? Width { get; }
        public WidgetStateProperty<float?>? Height { get; }
    }

}
