#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// A controlled float input constrained to an explicit inclusive range.
    /// </summary>
    public sealed class Slider : Widget {
        public Slider(
            State<float> value,
            float min,
            float max,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            SliderStyle? style = null,
            Action<float>? onChanged = null,
            Action<float>? onChangeStart = null,
            Action<float>? onChangeEnd = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ValidateFinite(min, nameof(min));
            ValidateFinite(max, nameof(max));
            if (min >= max) {
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than minimum.");
            }

            if (value.Value < min || value.Value > max) {
                throw new ArgumentOutOfRangeException(nameof(value), "Initial value must be within the slider range.");
            }

            Min = min;
            Max = max;
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
            Style = style;
            OnChanged = onChanged;
            OnChangeStart = onChangeStart;
            OnChangeEnd = onChangeEnd;
        }

        /// <summary>Creates a controlled slider backed by a <see cref="FormField{T}"/>.</summary>
        public Slider(
            FormField<float> field,
            float min,
            float max,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            SliderStyle? style = null,
            Action<float>? onChanged = null,
            Action<float>? onChangeStart = null,
            Action<float>? onChangeEnd = null)
            : this(
                field?.Value ?? throw new ArgumentNullException(nameof(field)),
                min,
                max,
                label,
                enabled,
                focusNode,
                style,
                onChanged,
                onChangeStart,
                onChangeEnd) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned value state.
        /// </summary>
        public State<float> Value { get; }

        public float Min { get; }

        public float Max { get; }

        public string? Label { get; }

        public bool Enabled { get; }

        public FocusNode? FocusNode { get; }

        public SliderStyle? Style { get; }

        /// <summary>Called after a user-originated value is committed to <see cref="Value"/>.</summary>
        public Action<float>? OnChanged { get; }

        /// <summary>Called once with the value held before a user interaction begins.</summary>
        public Action<float>? OnChangeStart { get; }

        /// <summary>Called once with the final value after a user interaction ends.</summary>
        public Action<float>? OnChangeEnd { get; }

        /// <summary>Gets the optional form field that supplies reactive validation errors.</summary>
        public FormField<float>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new SliderNode(this);
        }

        private static void ValidateFinite(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                throw new ArgumentOutOfRangeException(parameterName, "Range values must be finite.");
            }
        }
    }

    /// <summary>State-aware visual configuration for a <see cref="Slider"/>.</summary>
    public sealed class SliderStyle {
        public SliderStyle(
            WidgetStateProperty<Color?>? activeTrackColor = null,
            WidgetStateProperty<Color?>? inactiveTrackColor = null,
            WidgetStateProperty<Color?>? thumbColor = null,
            WidgetStateProperty<float?>? trackHeight = null,
            WidgetStateProperty<float?>? thumbSize = null) {
            ActiveTrackColor = activeTrackColor;
            InactiveTrackColor = inactiveTrackColor;
            ThumbColor = thumbColor;
            TrackHeight = trackHeight;
            ThumbSize = thumbSize;
        }

        public WidgetStateProperty<Color?>? ActiveTrackColor { get; }
        public WidgetStateProperty<Color?>? InactiveTrackColor { get; }
        public WidgetStateProperty<Color?>? ThumbColor { get; }
        public WidgetStateProperty<float?>? TrackHeight { get; }
        public WidgetStateProperty<float?>? ThumbSize { get; }
    }

}
