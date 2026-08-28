#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// A controlled checkbox backed by an externally owned state.
    /// </summary>
    public sealed class Checkbox : Widget {
        public Checkbox(
            State<bool> value,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            CheckboxStyle? style = null,
            Action<bool>? onChanged = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
            Style = style;
            OnChanged = onChanged;
        }

        public Checkbox(
            FormField<bool> field,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            CheckboxStyle? style = null,
            Action<bool>? onChanged = null)
            : this(field?.Value ?? throw new ArgumentNullException(nameof(field)), label, enabled, focusNode, style, onChanged) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned checked state.
        /// </summary>
        public State<bool> Value { get; }

        public string? Label { get; }

        public bool Enabled { get; }

        public FocusNode? FocusNode { get; }

        /// <summary>Optional visual override; omitted values inherit the active theme.</summary>
        public CheckboxStyle? Style { get; }

        /// <summary>Called after a user-originated value is committed to <see cref="Value"/>.</summary>
        public Action<bool>? OnChanged { get; }

        public FormField<bool>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new CheckboxNode(this);
        }
    }

    /// <summary>Defines the visual configuration of a <see cref="Checkbox"/>.</summary>
    public sealed class CheckboxStyle {
        public CheckboxStyle(
            Color? fillColor = null,
            Color? inactiveColor = null,
            Color? checkColor = null,
            Color? borderColor = null,
            float? size = null,
            BorderRadius? shape = null,
            WidgetStateProperty<Color?>? fillColorByState = null,
            WidgetStateProperty<Color?>? checkColorByState = null,
            WidgetStateProperty<Color?>? borderColorByState = null,
            WidgetStateProperty<float?>? sizeByState = null,
            WidgetStateProperty<BorderRadius?>? shapeByState = null) {
            if (size is { } explicitSize && (float.IsNaN(explicitSize) || float.IsInfinity(explicitSize) || explicitSize <= 0f)) throw new ArgumentOutOfRangeException(nameof(size));
            FillColor = fillColor;
            InactiveColor = inactiveColor;
            CheckColor = checkColor;
            BorderColor = borderColor;
            Size = size;
            Shape = shape;
            FillColorByState = fillColorByState;
            CheckColorByState = checkColorByState;
            BorderColorByState = borderColorByState;
            SizeByState = sizeByState;
            ShapeByState = shapeByState;
        }

        public Color? FillColor { get; }
        public Color? InactiveColor { get; }
        public Color? CheckColor { get; }
        public Color? BorderColor { get; }
        public float? Size { get; }
        public BorderRadius? Shape { get; }
        public WidgetStateProperty<Color?>? FillColorByState { get; }
        public WidgetStateProperty<Color?>? CheckColorByState { get; }
        public WidgetStateProperty<Color?>? BorderColorByState { get; }
        public WidgetStateProperty<float?>? SizeByState { get; }
        public WidgetStateProperty<BorderRadius?>? ShapeByState { get; }

        internal Color? ResolveFillColor(WidgetStates states) =>
            FillColorByState?.Resolve(states)
            ?? ((states & WidgetStates.Selected) != 0 ? FillColor : InactiveColor);

        internal Color? ResolveCheckColor(WidgetStates states) => CheckColorByState?.Resolve(states) ?? CheckColor;

        internal Color? ResolveBorderColor(WidgetStates states) => BorderColorByState?.Resolve(states) ?? BorderColor;

        internal float? ResolveSize(WidgetStates states) {
            var value = SizeByState?.Resolve(states) ?? Size;
            if (value is { } resolved && (float.IsNaN(resolved) || float.IsInfinity(resolved) || resolved <= 0f)) {
                throw new ArgumentOutOfRangeException(nameof(SizeByState), "Resolved checkbox size must be finite and greater than zero.");
            }
            return value;
        }

        internal BorderRadius? ResolveShape(WidgetStates states) => ShapeByState?.Resolve(states) ?? Shape;
    }

}
