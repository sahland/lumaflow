#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Defines visual overrides for a text field interaction state.
    /// </summary>
    public sealed class TextFieldStateStyle {
        public TextFieldStateStyle(Color? background = null, Color? foreground = null, Color? border = null) {
            Background = background;
            Foreground = foreground;
            Border = border;
        }

        public Color? Background { get; }
        public Color? Foreground { get; }
        public Color? Border { get; }
    }

    /// <summary>
    /// Defines the semantic surface and typography of a <see cref="TextField"/>.
    /// </summary>
    public sealed class TextFieldStyle {
        public TextFieldStyle(
            Color? background = null,
            Color? foreground = null,
            Color? border = null,
            Color? placeholder = null,
            TextStyle? typography = null,
            TextStyle? labelStyle = null,
            TextStyle? supportingStyle = null,
            EdgeInsets? padding = null,
            BorderRadius? shape = null,
            TextFieldStateStyle? focused = null,
            TextFieldStateStyle? disabled = null,
            TextFieldStateStyle? error = null,
            WidgetStateProperty<Color?>? backgroundColor = null,
            WidgetStateProperty<Color?>? foregroundColor = null,
            WidgetStateProperty<Color?>? borderColor = null,
            WidgetStateProperty<EdgeInsets?>? paddingByState = null,
            WidgetStateProperty<BorderRadius?>? shapeByState = null,
            WidgetStateProperty<TextStyle?>? typographyByState = null) {
            Background = background;
            Foreground = foreground;
            Border = border;
            Placeholder = placeholder;
            Typography = typography;
            LabelStyle = labelStyle;
            SupportingStyle = supportingStyle;
            Padding = padding;
            Shape = shape;
            Focused = focused;
            Disabled = disabled;
            Error = error;
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
        public Color? Placeholder { get; }
        public TextStyle? Typography { get; }
        public TextStyle? LabelStyle { get; }
        public TextStyle? SupportingStyle { get; }
        public EdgeInsets? Padding { get; }
        public BorderRadius? Shape { get; }
        public TextFieldStateStyle? Focused { get; }
        public TextFieldStateStyle? Disabled { get; }
        public TextFieldStateStyle? Error { get; }

        /// <summary>State-aware background override. A null result falls back to the legacy state or base value.</summary>
        public WidgetStateProperty<Color?>? BackgroundColor { get; }

        /// <summary>State-aware foreground override. A null result falls back to the legacy state or base value.</summary>
        public WidgetStateProperty<Color?>? ForegroundColor { get; }

        /// <summary>State-aware border override. A null result falls back to the legacy state or base value.</summary>
        public WidgetStateProperty<Color?>? BorderColor { get; }

        public WidgetStateProperty<EdgeInsets?>? PaddingByState { get; }
        public WidgetStateProperty<BorderRadius?>? ShapeByState { get; }
        public WidgetStateProperty<TextStyle?>? TypographyByState { get; }

        internal Color? ResolveBackground(WidgetStates states) =>
            BackgroundColor?.Resolve(states) ?? ResolveLegacyState(states)?.Background ?? Background;

        internal Color? ResolveForeground(WidgetStates states) =>
            ForegroundColor?.Resolve(states) ?? ResolveLegacyState(states)?.Foreground ?? Foreground;

        internal Color? ResolveBorder(WidgetStates states) =>
            BorderColor?.Resolve(states) ?? ResolveLegacyState(states)?.Border ?? Border;

        internal EdgeInsets? ResolvePadding(WidgetStates states) => PaddingByState?.Resolve(states) ?? Padding;

        internal BorderRadius? ResolveShape(WidgetStates states) => ShapeByState?.Resolve(states) ?? Shape;

        internal TextStyle? ResolveTypography(WidgetStates states) => TypographyByState?.Resolve(states) ?? Typography;

        private TextFieldStateStyle? ResolveLegacyState(WidgetStates states) {
            if ((states & WidgetStates.Disabled) != 0) return Disabled;
            if ((states & WidgetStates.Error) != 0) return Error;
            if ((states & WidgetStates.Focused) != 0) return Focused;
            return null;
        }
    }

    /// <summary>
    /// A controlled text input backed by an externally owned string state.
    /// </summary>
    public sealed class TextField : Widget {
        public TextField(
            State<string> value,
            string? label = null,
            string? placeholder = null,
            bool obscureText = false,
            bool enabled = true,
            string? helperText = null,
            string? errorText = null,
            TextFieldStyle? style = null,
            bool multiline = false,
            FocusNode? focusNode = null,
            Action<string>? onChanged = null,
            Action<string>? onSubmitted = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Label = label;
            Placeholder = placeholder;
            ObscureText = obscureText;
            Enabled = enabled;
            HelperText = helperText;
            ErrorText = errorText;
            Style = style;
            Multiline = multiline;
            FocusNode = focusNode;
            OnChanged = onChanged;
            OnSubmitted = onSubmitted;
        }

        /// <summary>Creates a controlled text input participating in an optional ancestor <see cref="Form"/>.</summary>
        public TextField(
            FormField<string> field,
            string? label = null,
            string? placeholder = null,
            bool obscureText = false,
            bool enabled = true,
            string? helperText = null,
            TextFieldStyle? style = null,
            bool multiline = false,
            FocusNode? focusNode = null,
            Action<string>? onChanged = null,
            Action<string>? onSubmitted = null)
            : this(
                field?.Value ?? throw new ArgumentNullException(nameof(field)),
                label,
                placeholder,
                obscureText,
                enabled,
                helperText,
                style: style,
                multiline: multiline,
                focusNode: focusNode,
                onChanged: onChanged,
                onSubmitted: onSubmitted) {
            Field = field;
        }

        /// <summary>
        /// Gets the externally owned value state.
        /// </summary>
        public State<string> Value { get; }

        public string? Label { get; }

        public string? Placeholder { get; }

        public bool ObscureText { get; }

        /// <summary>
        /// Whether the native input accepts line breaks. Multi-line inputs do not trigger
        /// an ancestor <see cref="Form"/> submit on Return or keypad Enter.
        /// </summary>
        public bool Multiline { get; }

        /// <summary>
        /// Optional externally owned controller for requesting and observing focus.
        /// </summary>
        public FocusNode? FocusNode { get; }

        public bool Enabled { get; }

        /// <summary>
        /// Supporting text shown below the native input when no error is present.
        /// </summary>
        public string? HelperText { get; }

        /// <summary>
        /// Error text shown below the native input and used for the error state.
        /// </summary>
        public string? ErrorText { get; }

        public TextFieldStyle? Style { get; }

        /// <summary>Called after a user edit is committed to <see cref="Value"/>.</summary>
        public Action<string>? OnChanged { get; }

        /// <summary>
        /// Called with the current controlled value when a single-line field receives
        /// Return or keypad Enter. Multi-line fields keep those keys for line breaks.
        /// </summary>
        public Action<string>? OnSubmitted { get; }

        /// <summary>Gets the optional form field that supplies reactive validation errors.</summary>
        public FormField<string>? Field { get; }

        internal override WidgetNode CreateNode() {
            return new TextFieldNode(this);
        }
    }

}
