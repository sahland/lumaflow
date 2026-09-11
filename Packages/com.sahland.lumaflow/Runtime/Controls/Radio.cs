#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// A typed radio option selected through a shared externally owned state.
    /// </summary>
    public sealed class Radio<T> : Widget {
        public Radio(
            State<T> selectedValue,
            T value,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            RadioStyle? style = null,
            Action<T>? onChanged = null) {
            SelectedValue = selectedValue ?? throw new ArgumentNullException(nameof(selectedValue));
            Value = value;
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
            Style = style;
            OnChanged = onChanged;
        }

        public Radio(
            FormField<T> field,
            T value,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            RadioStyle? style = null,
            Action<T>? onChanged = null)
            : this(field?.Value ?? throw new ArgumentNullException(nameof(field)), value, label, enabled, focusNode, style, onChanged) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned value shared by all options in this group.
        /// </summary>
        public State<T> SelectedValue { get; }

        /// <summary>
        /// Gets the value selected when this option is chosen.
        /// </summary>
        public T Value { get; }

        public string? Label { get; }

        public bool Enabled { get; }

        public FocusNode? FocusNode { get; }

        /// <summary>Optional visual override; omitted values inherit the active theme.</summary>
        public RadioStyle? Style { get; }

        /// <summary>Called after this option is selected and committed to <see cref="SelectedValue"/>.</summary>
        public Action<T>? OnChanged { get; }

        public FormField<T>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new RadioNode<T>(this);
        }
    }

    /// <summary>Defines the visual configuration of a <see cref="Radio{T}"/>.</summary>
    public sealed class RadioStyle {
        public RadioStyle(
            Color? fillColor = null,
            Color? inactiveColor = null,
            Color? borderColor = null,
            float? size = null,
            WidgetStateProperty<Color?>? fillColorByState = null,
            WidgetStateProperty<Color?>? borderColorByState = null,
            WidgetStateProperty<float?>? sizeByState = null) {
            if (size is { } explicitSize && (float.IsNaN(explicitSize) || float.IsInfinity(explicitSize) || explicitSize <= 0f)) throw new ArgumentOutOfRangeException(nameof(size));
            FillColor = fillColor;
            InactiveColor = inactiveColor;
            BorderColor = borderColor;
            Size = size;
            FillColorByState = fillColorByState;
            BorderColorByState = borderColorByState;
            SizeByState = sizeByState;
        }

        public Color? FillColor { get; }
        public Color? InactiveColor { get; }
        public Color? BorderColor { get; }
        public float? Size { get; }
        public WidgetStateProperty<Color?>? FillColorByState { get; }
        public WidgetStateProperty<Color?>? BorderColorByState { get; }
        public WidgetStateProperty<float?>? SizeByState { get; }

        internal Color? ResolveFillColor(WidgetStates states) =>
            FillColorByState?.Resolve(states)
            ?? ((states & WidgetStates.Selected) != 0 ? FillColor : InactiveColor);

        internal Color? ResolveBorderColor(WidgetStates states) => BorderColorByState?.Resolve(states) ?? BorderColor;

        internal float? ResolveSize(WidgetStates states) {
            var value = SizeByState?.Resolve(states) ?? Size;
            if (value is { } resolved && (float.IsNaN(resolved) || float.IsInfinity(resolved) || resolved <= 0f)) {
                throw new ArgumentOutOfRangeException(nameof(SizeByState), "Resolved radio size must be finite and greater than zero.");
            }
            return value;
        }
    }

}
