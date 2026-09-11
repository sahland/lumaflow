#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Defines a visual override for one button interaction state.
    /// </summary>
    public sealed class ButtonStateStyle {
        public ButtonStateStyle(Color? background = null, Color? foreground = null, Border? border = null) {
            Background = background;
            Foreground = foreground;
            Border = border;
        }

        public Color? Background { get; }

        public Color? Foreground { get; }

        public Border? Border { get; }
    }

    /// <summary>
    /// Defines the surface, typography, size, and interaction states of a <see cref="Button"/>.
    /// </summary>
    public sealed class ButtonStyle {
        private static readonly Color PrimaryBackground = new(0.56f, 0.47f, 1f);
        private static readonly Color PrimaryForeground = new(0.10f, 0.08f, 0.18f);

        public ButtonStyle(
            Color? background = null,
            Color? foreground = null,
            EdgeInsets? padding = null,
            BorderRadius? shape = null,
            TextStyle? typography = null,
            Vector2? minimumSize = null,
            Border? border = null,
            ButtonStateStyle? hovered = null,
            ButtonStateStyle? pressed = null,
            ButtonStateStyle? focused = null,
            ButtonStateStyle? disabled = null,
            WidgetStateProperty<Color?>? backgroundColor = null,
            WidgetStateProperty<Color?>? foregroundColor = null,
            WidgetStateProperty<EdgeInsets?>? paddingByState = null,
            WidgetStateProperty<BorderRadius?>? shapeByState = null,
            WidgetStateProperty<TextStyle?>? typographyByState = null,
            WidgetStateProperty<Vector2?>? minimumSizeByState = null,
            WidgetStateProperty<Border?>? borderByState = null) {
            ValidateMinimumSize(minimumSize);
            Background = background;
            Foreground = foreground;
            Padding = padding;
            Shape = shape;
            Typography = typography;
            MinimumSize = minimumSize;
            Border = border;
            Hovered = hovered;
            Pressed = pressed;
            Focused = focused;
            Disabled = disabled;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
            PaddingByState = paddingByState;
            ShapeByState = shapeByState;
            TypographyByState = typographyByState;
            MinimumSizeByState = minimumSizeByState;
            BorderByState = borderByState;
        }

        /// <summary>
        /// A filled, high-emphasis action button.
        /// </summary>
        public static ButtonStyle Primary { get; } = new(
            background: PrimaryBackground,
            foreground: PrimaryForeground,
            padding: EdgeInsets.Symmetric(horizontal: 20f, vertical: 10f),
            shape: BorderRadius.All(22f),
            typography: new TextStyle(fontSize: 14f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(64f, 40f),
            hovered: new ButtonStateStyle(background: new Color(0.63f, 0.55f, 1f)),
            pressed: new ButtonStateStyle(background: new Color(0.46f, 0.37f, 0.84f)),
            focused: new ButtonStateStyle(background: new Color(0.61f, 0.53f, 1f)),
            disabled: new ButtonStateStyle(background: new Color(0.26f, 0.24f, 0.32f), foreground: new Color(0.56f, 0.55f, 0.62f)));

        /// <summary>A high-emphasis action that requires deliberate visual caution.</summary>
        public static ButtonStyle Destructive { get; } = new(
            background: new Color(0.84f, 0.28f, 0.34f),
            foreground: Color.white,
            padding: EdgeInsets.Symmetric(horizontal: 20f, vertical: 10f),
            shape: BorderRadius.All(22f),
            typography: new TextStyle(fontSize: 14f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(64f, 40f),
            hovered: new ButtonStateStyle(background: new Color(0.94f, 0.34f, 0.40f)),
            pressed: new ButtonStateStyle(background: new Color(0.68f, 0.20f, 0.26f)),
            focused: new ButtonStateStyle(background: new Color(0.90f, 0.31f, 0.37f)),
            disabled: new ButtonStateStyle(background: new Color(0.32f, 0.22f, 0.25f), foreground: new Color(0.60f, 0.54f, 0.56f)));

        public Color? Background { get; }

        public Color? Foreground { get; }

        public EdgeInsets? Padding { get; }

        public BorderRadius? Shape { get; }

        public TextStyle? Typography { get; }

        public Vector2? MinimumSize { get; }

        /// <summary>Base outline drawn inside the button bounds.</summary>
        public Border? Border { get; }

        public ButtonStateStyle? Hovered { get; }

        public ButtonStateStyle? Pressed { get; }

        public ButtonStateStyle? Focused { get; }

        public ButtonStateStyle? Disabled { get; }

        /// <summary>State-aware background override. A null result falls back to the legacy state or base value.</summary>
        public WidgetStateProperty<Color?>? BackgroundColor { get; }

        /// <summary>State-aware foreground override. A null result falls back to the legacy state or base value.</summary>
        public WidgetStateProperty<Color?>? ForegroundColor { get; }

        public WidgetStateProperty<EdgeInsets?>? PaddingByState { get; }

        public WidgetStateProperty<BorderRadius?>? ShapeByState { get; }

        public WidgetStateProperty<TextStyle?>? TypographyByState { get; }

        public WidgetStateProperty<Vector2?>? MinimumSizeByState { get; }

        /// <summary>State-aware outline override.</summary>
        public WidgetStateProperty<Border?>? BorderByState { get; }

        internal Color? ResolveBackground(WidgetStates states) =>
            BackgroundColor?.Resolve(states) ?? ResolveLegacyState(states)?.Background ?? Background;

        internal Color? ResolveForeground(WidgetStates states) =>
            ForegroundColor?.Resolve(states) ?? ResolveLegacyState(states)?.Foreground ?? Foreground;

        internal EdgeInsets? ResolvePadding(WidgetStates states) => PaddingByState?.Resolve(states) ?? Padding;

        internal BorderRadius? ResolveShape(WidgetStates states) => ShapeByState?.Resolve(states) ?? Shape;

        internal TextStyle? ResolveTypography(WidgetStates states) => TypographyByState?.Resolve(states) ?? Typography;

        internal Vector2? ResolveMinimumSize(WidgetStates states) {
            var value = MinimumSizeByState?.Resolve(states) ?? MinimumSize;
            ValidateMinimumSize(value);
            return value;
        }

        internal Border? ResolveBorder(WidgetStates states) =>
            BorderByState?.Resolve(states) ?? ResolveLegacyState(states)?.Border ?? Border;

        private ButtonStateStyle? ResolveLegacyState(WidgetStates states) {
            if ((states & WidgetStates.Disabled) != 0) return Disabled;
            if ((states & WidgetStates.Pressed) != 0) return Pressed;
            if ((states & WidgetStates.Hovered) != 0) return Hovered;
            if ((states & WidgetStates.Focused) != 0) return Focused;
            return null;
        }

        private static void ValidateMinimumSize(Vector2? minimumSize) {
            if (minimumSize is not { } size) {
                return;
            }

            if (float.IsNaN(size.x) || float.IsInfinity(size.x) || size.x < 0f
                || float.IsNaN(size.y) || float.IsInfinity(size.y) || size.y < 0f) {
                throw new ArgumentOutOfRangeException(nameof(minimumSize), "Minimum size must be finite and non-negative.");
            }
        }
    }

}
