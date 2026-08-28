#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Defines optional typography properties for text widgets.
    /// </summary>
    public sealed class TextStyle {
        public TextStyle(Color? color = null, float? fontSize = null, FontStyle? fontStyle = null) {
            if (fontSize is { } size && (float.IsNaN(size) || float.IsInfinity(size) || size <= 0f)) {
                throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be finite and greater than zero.");
            }

            Color = color;
            FontSize = fontSize;
            FontStyle = fontStyle;
        }

        public Color? Color { get; }

        public float? FontSize { get; }

        public FontStyle? FontStyle { get; }
    }

    /// <summary>
    /// Semantic colors used by a LumaFlow widget tree.
    /// </summary>
    public sealed class ColorScheme {
        public ColorScheme(Color canvas, Color surface, Color surfaceVariant, Color primary, Color primaryContainer, Color onPrimary, Color onSurface, Color onSurfaceVariant, Color outline) {
            Canvas = canvas;
            Surface = surface;
            SurfaceVariant = surfaceVariant;
            Primary = primary;
            PrimaryContainer = primaryContainer;
            OnPrimary = onPrimary;
            OnSurface = onSurface;
            OnSurfaceVariant = onSurfaceVariant;
            Outline = outline;
        }

        public Color Canvas { get; }
        public Color Surface { get; }
        public Color SurfaceVariant { get; }
        public Color Primary { get; }
        public Color PrimaryContainer { get; }
        public Color OnPrimary { get; }
        public Color OnSurface { get; }
        public Color OnSurfaceVariant { get; }
        public Color Outline { get; }
    }

    /// <summary>
    /// Semantic text styles used by a LumaFlow widget tree.
    /// </summary>
    public sealed class TypographyTheme {
        public TypographyTheme(TextStyle title, TextStyle headline, TextStyle body, TextStyle label) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Headline = headline ?? throw new ArgumentNullException(nameof(headline));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public TextStyle Title { get; }
        public TextStyle Headline { get; }
        public TextStyle Body { get; }
        public TextStyle Label { get; }
    }

    /// <summary>
    /// Named spacing tokens used by a LumaFlow widget tree.
    /// </summary>
    public sealed class SpacingTheme {
        public SpacingTheme(float extraSmall, float small, float medium, float large, float extraLarge) {
            Validate(extraSmall, nameof(extraSmall));
            Validate(small, nameof(small));
            Validate(medium, nameof(medium));
            Validate(large, nameof(large));
            Validate(extraLarge, nameof(extraLarge));
            ExtraSmall = extraSmall;
            Small = small;
            Medium = medium;
            Large = large;
            ExtraLarge = extraLarge;
        }

        public float ExtraSmall { get; }
        public float Small { get; }
        public float Medium { get; }
        public float Large { get; }
        public float ExtraLarge { get; }

        private static void Validate(float value, string name) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(name, "Spacing must be finite and non-negative.");
            }
        }
    }

    /// <summary>
    /// Named corner-radius tokens used by a LumaFlow widget tree.
    /// </summary>
    public sealed class RadiusTheme {
        public RadiusTheme(BorderRadius small, BorderRadius medium, BorderRadius large) {
            Small = small;
            Medium = medium;
            Large = large;
        }

        public BorderRadius Small { get; }
        public BorderRadius Medium { get; }
        public BorderRadius Large { get; }
    }

    /// <summary>
    /// Semantic button styles used by a LumaFlow widget tree.
    /// </summary>
    public sealed class ButtonTheme {
        public ButtonTheme(ButtonStyle primary, ButtonStyle secondary, ButtonStyle? destructive = null) {
            Primary = primary ?? throw new ArgumentNullException(nameof(primary));
            Secondary = secondary ?? throw new ArgumentNullException(nameof(secondary));
            Destructive = destructive ?? ButtonStyle.Destructive;
        }

        public ButtonStyle Primary { get; }
        public ButtonStyle Secondary { get; }
        public ButtonStyle Destructive { get; }

        /// <summary>
        /// Resolves a semantic button variant to this theme's component default.
        /// </summary>
        public ButtonStyle Resolve(ButtonVariant variant) {
            return variant switch {
                ButtonVariant.Primary => Primary,
                ButtonVariant.Secondary => Secondary,
                ButtonVariant.Destructive => Destructive,
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
        }
    }

    /// <summary>
    /// Semantic defaults used by text inputs in a LumaFlow widget tree.
    /// </summary>
    public sealed class TextFieldTheme {
        public TextFieldTheme(TextFieldStyle style) {
            Style = style ?? throw new ArgumentNullException(nameof(style));
        }

        public TextFieldStyle Style { get; }
    }

    /// <summary>Semantic defaults used by dropdown anchor fields in a LumaFlow widget tree.</summary>
    public sealed class DropdownTheme {
        public DropdownTheme(DropdownStyle style) {
            Style = style ?? throw new ArgumentNullException(nameof(style));
        }

        public DropdownStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for checkboxes.</summary>
    public sealed class CheckboxTheme {
        public CheckboxTheme(CheckboxStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public CheckboxStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for radio controls.</summary>
    public sealed class RadioTheme {
        public RadioTheme(RadioStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public RadioStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for switches.</summary>
    public sealed class SwitchTheme {
        public SwitchTheme(SwitchStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public SwitchStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for sliders.</summary>
    public sealed class SliderTheme {
        public SliderTheme(SliderStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public SliderStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for tab bars.</summary>
    public sealed class TabBarTheme {
        public TabBarTheme(TabBarStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public TabBarStyle Style { get; }
    }

    /// <summary>Tree-scoped visual defaults for segmented controls.</summary>
    public sealed class SegmentedControlTheme {
        public SegmentedControlTheme(SegmentedControlStyle style) =>
            Style = style ?? throw new ArgumentNullException(nameof(style));

        public SegmentedControlStyle Style { get; }
    }

    /// <summary>
    /// Collects semantic colors, typography, spacing, radii, and component themes.
    /// </summary>
    public sealed class ThemeData {
        internal static IconThemeData FallbackIconTheme { get; } = new();
        internal static TextFieldTheme FallbackTextFieldTheme { get; } = new(
            new TextFieldStyle(
                background: new Color(0.12f, 0.14f, 0.20f),
                foreground: Color.white,
                border: new Color(0.30f, 0.33f, 0.43f),
                placeholder: new Color(0.58f, 0.60f, 0.68f),
                padding: EdgeInsets.Symmetric(horizontal: 14f, vertical: 10f),
                shape: BorderRadius.All(12f),
                focused: new TextFieldStateStyle(border: new Color(0.56f, 0.47f, 1f)),
                disabled: new TextFieldStateStyle(foreground: new Color(0.55f, 0.56f, 0.62f)),
                error: new TextFieldStateStyle(
                    foreground: new Color(1f, 0.46f, 0.48f),
                    border: new Color(1f, 0.38f, 0.42f))));

        public ThemeData(
            ColorScheme colors,
            TypographyTheme typography,
            SpacingTheme spacing,
            RadiusTheme radius,
            ButtonTheme buttonTheme,
            IconThemeData? iconTheme = null,
            TextFieldTheme? textFieldTheme = null,
            DropdownTheme? dropdownTheme = null,
            ProgressIndicatorTheme? progressIndicatorTheme = null,
            CheckboxTheme? checkboxTheme = null,
            RadioTheme? radioTheme = null,
            SwitchTheme? switchTheme = null,
            SliderTheme? sliderTheme = null,
            TabBarTheme? tabBarTheme = null,
            SegmentedControlTheme? segmentedControlTheme = null) {
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Typography = typography ?? throw new ArgumentNullException(nameof(typography));
            Spacing = spacing ?? throw new ArgumentNullException(nameof(spacing));
            Radius = radius ?? throw new ArgumentNullException(nameof(radius));
            ButtonTheme = buttonTheme ?? throw new ArgumentNullException(nameof(buttonTheme));
            IconTheme = iconTheme ?? FallbackIconTheme;
            // A tree-scoped Theme must never leak the unrelated dark runtime
            // fallback into native text input internals. When no component
            // override is supplied, derive the field from this Theme's semantic
            // color scheme.
            TextFieldTheme = textFieldTheme ?? new TextFieldTheme(new TextFieldStyle(
                background: colors.Surface,
                foreground: colors.OnSurface,
                border: colors.Outline,
                placeholder: colors.OnSurfaceVariant,
                padding: EdgeInsets.Symmetric(horizontal: 12f, vertical: 8f),
                shape: radius.Small,
                focused: new TextFieldStateStyle(border: colors.Primary),
                disabled: new TextFieldStateStyle(
                    background: colors.SurfaceVariant,
                    foreground: colors.OnSurfaceVariant)));
            DropdownTheme = dropdownTheme ?? new DropdownTheme(new DropdownStyle(
                background: colors.SurfaceVariant,
                foreground: colors.OnSurface,
                border: colors.Outline,
                shape: radius.Small));
            ProgressIndicatorTheme = progressIndicatorTheme ?? new ProgressIndicatorTheme(
                new LinearProgressIndicatorStyle(
                    valueColor: colors.Primary,
                    trackColor: colors.SurfaceVariant,
                    minHeight: 4f,
                    borderRadius: BorderRadius.All(2f)));
            CheckboxTheme = checkboxTheme ?? new CheckboxTheme(new CheckboxStyle());
            RadioTheme = radioTheme ?? new RadioTheme(new RadioStyle());
            SwitchTheme = switchTheme ?? new SwitchTheme(new SwitchStyle());
            SliderTheme = sliderTheme ?? new SliderTheme(new SliderStyle());
            TabBarTheme = tabBarTheme ?? new TabBarTheme(new TabBarStyle());
            SegmentedControlTheme = segmentedControlTheme
                ?? new SegmentedControlTheme(new SegmentedControlStyle());
        }

        public ColorScheme Colors { get; }
        public TypographyTheme Typography { get; }
        public SpacingTheme Spacing { get; }
        public RadiusTheme Radius { get; }
        public ButtonTheme ButtonTheme { get; }
        public IconThemeData IconTheme { get; }
        public TextFieldTheme TextFieldTheme { get; }
        public DropdownTheme DropdownTheme { get; }
        public ProgressIndicatorTheme ProgressIndicatorTheme { get; }
        public CheckboxTheme CheckboxTheme { get; }
        public RadioTheme RadioTheme { get; }
        public SwitchTheme SwitchTheme { get; }
        public SliderTheme SliderTheme { get; }
        public TabBarTheme TabBarTheme { get; }
        public SegmentedControlTheme SegmentedControlTheme { get; }

        /// <summary>
        /// Creates a theme with selected values replaced while retaining every
        /// unspecified immutable theme object.
        /// </summary>
        public ThemeData CopyWith(
            ColorScheme? colors = null,
            TypographyTheme? typography = null,
            SpacingTheme? spacing = null,
            RadiusTheme? radius = null,
            ButtonTheme? buttonTheme = null,
            IconThemeData? iconTheme = null,
            TextFieldTheme? textFieldTheme = null,
            DropdownTheme? dropdownTheme = null,
            ProgressIndicatorTheme? progressIndicatorTheme = null,
            CheckboxTheme? checkboxTheme = null,
            RadioTheme? radioTheme = null,
            SwitchTheme? switchTheme = null,
            SliderTheme? sliderTheme = null,
            TabBarTheme? tabBarTheme = null,
            SegmentedControlTheme? segmentedControlTheme = null) => new(
                colors ?? Colors,
                typography ?? Typography,
                spacing ?? Spacing,
                radius ?? Radius,
                buttonTheme ?? ButtonTheme,
                iconTheme ?? IconTheme,
                textFieldTheme ?? TextFieldTheme,
                dropdownTheme ?? DropdownTheme,
                progressIndicatorTheme ?? ProgressIndicatorTheme,
                checkboxTheme ?? CheckboxTheme,
                radioTheme ?? RadioTheme,
                switchTheme ?? SwitchTheme,
                sliderTheme ?? SliderTheme,
                tabBarTheme ?? TabBarTheme,
                segmentedControlTheme ?? SegmentedControlTheme);
    }

    /// <summary>
    /// Provides <see cref="ThemeData"/> to its child without adding a native wrapper element.
    /// </summary>
    public sealed class Theme : Widget {
        public Theme(ThemeData data, Widget child) {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public ThemeData Data { get; }
        public Widget Child { get; }

        internal override WidgetNode CreateNode() => new ThemeNode(this);
    }

    internal sealed class ThemeNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;

        public ThemeNode(Theme widget)
            : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (Theme)Widget;
            _childContext = Context.WithTheme(widget.Data);
            _currentChild = MountChild(widget.Child, NativeParent, _childContext);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Theme theme) return false;
            var previous = (Theme)Widget;
            if (!ReferenceEquals(previous.Data, theme.Data)) _childContext!.UpdateTheme(theme.Data);
            ReconcileSingleChild(
                ref _currentChild,
                theme.Child,
                NativeParent,
                _childContext!);
            UpdateWidget(theme);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }
    }

}
