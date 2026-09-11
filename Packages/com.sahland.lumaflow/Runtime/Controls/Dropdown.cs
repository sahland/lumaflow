#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// A controlled, typed selection input backed by an externally owned state.
    /// </summary>
    public sealed class Dropdown<T> : Widget {
        public Dropdown(
            State<T> value,
            IEnumerable<T> items,
            Func<T, string> labelBuilder,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            DropdownStyle? style = null,
            Action<T>? onChanged = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            LabelBuilder = labelBuilder ?? throw new ArgumentNullException(nameof(labelBuilder));
            var itemList = new List<T>(items);
            if (itemList.Count == 0) {
                throw new ArgumentException("Dropdown requires at least one item.", nameof(items));
            }

            if (!itemList.Contains(Value.Value)) {
                throw new ArgumentException("The controlled value must be one of the supplied items.", nameof(value));
            }

            Items = itemList.AsReadOnly();
            Label = label;
            Enabled = enabled;
            FocusNode = focusNode;
            Style = style;
            OnChanged = onChanged;
        }

        public Dropdown(
            FormField<T> field,
            IEnumerable<T> items,
            Func<T, string> labelBuilder,
            string? label = null,
            bool enabled = true,
            FocusNode? focusNode = null,
            DropdownStyle? style = null,
            Action<T>? onChanged = null)
            : this(field?.Value ?? throw new ArgumentNullException(nameof(field)), items, labelBuilder, label, enabled, focusNode, style, onChanged) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned selected value.
        /// </summary>
        public State<T> Value { get; }

        /// <summary>
        /// Gets the immutable snapshot of selectable values.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        public Func<T, string> LabelBuilder { get; }

        public string? Label { get; }

        public bool Enabled { get; }

        public FocusNode? FocusNode { get; }

        /// <summary>Optional visual override for the native dropdown anchor field.</summary>
        public DropdownStyle? Style { get; }

        /// <summary>Called after a user selection is committed to <see cref="Value"/>.</summary>
        public Action<T>? OnChanged { get; }

        public FormField<T>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new DropdownNode<T>(this);
        }
    }

    /// <summary>Defines the visual configuration of a <see cref="Dropdown{T}"/> anchor field.</summary>
    public sealed class DropdownStyle {
        public DropdownStyle(
            Color? background = null,
            Color? foreground = null,
            Color? border = null,
            EdgeInsets? padding = null,
            BorderRadius? shape = null,
            TextStyle? typography = null,
            WidgetStateProperty<Color?>? backgroundColor = null,
            WidgetStateProperty<Color?>? foregroundColor = null,
            WidgetStateProperty<Color?>? borderColor = null,
            WidgetStateProperty<EdgeInsets?>? paddingByState = null,
            WidgetStateProperty<BorderRadius?>? shapeByState = null,
            WidgetStateProperty<TextStyle?>? typographyByState = null) {
            Background = background;
            Foreground = foreground;
            Border = border;
            Padding = padding;
            Shape = shape;
            Typography = typography;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
            BorderColor = borderColor;
            PaddingByState = paddingByState;
            ShapeByState = shapeByState;
            TypographyByState = typographyByState;
        }

        public Color? Background { get; }
        public Color? Foreground { get; }
        public Color? Border { get; }
        public EdgeInsets? Padding { get; }
        public BorderRadius? Shape { get; }
        public TextStyle? Typography { get; }
        public WidgetStateProperty<Color?>? BackgroundColor { get; }
        public WidgetStateProperty<Color?>? ForegroundColor { get; }
        public WidgetStateProperty<Color?>? BorderColor { get; }
        public WidgetStateProperty<EdgeInsets?>? PaddingByState { get; }
        public WidgetStateProperty<BorderRadius?>? ShapeByState { get; }
        public WidgetStateProperty<TextStyle?>? TypographyByState { get; }

        internal Color? ResolveBackground(WidgetStates states) => BackgroundColor?.Resolve(states) ?? Background;
        internal Color? ResolveForeground(WidgetStates states) => ForegroundColor?.Resolve(states) ?? Foreground;
        internal Color? ResolveBorder(WidgetStates states) => BorderColor?.Resolve(states) ?? Border;
        internal EdgeInsets? ResolvePadding(WidgetStates states) => PaddingByState?.Resolve(states) ?? Padding;
        internal BorderRadius? ResolveShape(WidgetStates states) => ShapeByState?.Resolve(states) ?? Shape;
        internal TextStyle? ResolveTypography(WidgetStates states) => TypographyByState?.Resolve(states) ?? Typography;
    }

}
