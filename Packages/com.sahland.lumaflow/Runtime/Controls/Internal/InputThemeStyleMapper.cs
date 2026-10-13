#nullable enable

using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class InputThemeStyleMapper {
        private static VectorImage? _checkboxCheckmark;
        public static void ApplyDropdown<T>(BaseField<T> field, ThemeData? theme, DropdownStyle? style, WidgetStates states, TextScaler textScaler = default) {
            if (theme is not null) ApplyLabel(field, theme, textScaler);
            var input = field.Q<VisualElement>(className: "unity-base-field__input") ?? field;
            if (theme is not null || style is not null) input.style.backgroundImage = new StyleBackground(default(Background));
            input.style.backgroundColor = style?.ResolveBackground(states) is { } background ? background : StyleKeyword.Null;
            input.style.color = style?.ResolveForeground(states) is { } foreground ? foreground : StyleKeyword.Null;
            // Popup text has its own Editor USS color; parent color does not override it.
            var popupText = input.Q<TextElement>();
            if (popupText != null) {
                if (style?.ResolveTypography(states) is { } textStyle) TextStyleMapper.Apply(popupText, textStyle, textScaler);
                popupText.style.color = style?.ResolveForeground(states) is { } textColor ? textColor : StyleKeyword.Null;
            }
            var arrow = input.Q<VisualElement>(className: "unity-base-popup-field__arrow");
            if (arrow != null) arrow.style.unityBackgroundImageTintColor = style?.ResolveForeground(states) is { } arrowColor ? arrowColor : StyleKeyword.Null;
            if (style?.ResolveBorder(states) is { } border) {
                input.style.borderTopColor = border;
                input.style.borderRightColor = border;
                input.style.borderBottomColor = border;
                input.style.borderLeftColor = border;
            } else {
                input.style.borderTopColor = StyleKeyword.Null;
                input.style.borderRightColor = StyleKeyword.Null;
                input.style.borderBottomColor = StyleKeyword.Null;
                input.style.borderLeftColor = StyleKeyword.Null;
            }
            PaddingStyleMapper.Clear(input);
            if (style?.ResolvePadding(states) is { } padding) PaddingStyleMapper.Apply(input, padding);
            if (style?.ResolveShape(states) is { } shape) {
                input.style.borderTopLeftRadius = shape.TopLeft;
                input.style.borderTopRightRadius = shape.TopRight;
                input.style.borderBottomRightRadius = shape.BottomRight;
                input.style.borderBottomLeftRadius = shape.BottomLeft;
            } else {
                input.style.borderTopLeftRadius = StyleKeyword.Null;
                input.style.borderTopRightRadius = StyleKeyword.Null;
                input.style.borderBottomRightRadius = StyleKeyword.Null;
                input.style.borderBottomLeftRadius = StyleKeyword.Null;
            }
            input.style.fontSize = StyleKeyword.Null;
            input.style.unityFontStyleAndWeight = StyleKeyword.Null;
            if (style?.ResolveTypography(states) is { } typography) TextStyleMapper.Apply(input, typography, textScaler);
        }

        public static void ApplyToggle(UnityEngine.UIElements.Toggle toggle, ThemeData? theme, TextScaler textScaler = default) {
            if (theme is null) return;
            ApplyLabel(toggle, theme, textScaler);
            var input = toggle.Q<VisualElement>(className: "unity-toggle__input")
                ?? toggle.Q<VisualElement>(className: "unity-base-field__input");
            if (input is null) return;
            input.style.backgroundColor = toggle.value ? theme.Colors.Primary : theme.Colors.Surface;
            input.style.borderTopColor = theme.Colors.Outline;
            input.style.borderRightColor = theme.Colors.Outline;
            input.style.borderBottomColor = theme.Colors.Outline;
            input.style.borderLeftColor = theme.Colors.Outline;
            input.style.borderTopLeftRadius = theme.Radius.Small.TopLeft;
            input.style.borderTopRightRadius = theme.Radius.Small.TopRight;
            input.style.borderBottomRightRadius = theme.Radius.Small.BottomRight;
            input.style.borderBottomLeftRadius = theme.Radius.Small.BottomLeft;
            var checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark is not null) checkmark.style.color = theme.Colors.OnPrimary;
        }

        public static void ApplyCheckbox(UnityEngine.UIElements.Toggle checkbox, ThemeData? theme, CheckboxStyle? style, WidgetStates states, TextScaler textScaler = default) {
            if (theme is not null) ApplyLabel(checkbox, theme, textScaler);
            // A checkbox is not an Inspector field: keep the indicator before its label.
            checkbox.style.flexDirection = FlexDirection.RowReverse;
            checkbox.style.justifyContent = Justify.FlexEnd;
            checkbox.style.minHeight = 24;
            var row = checkbox.Q<VisualElement>(className: "unity-toggle__input");
            if (row != null) { row.style.flexGrow = 0; row.style.flexShrink = 0; row.style.alignSelf = UnityEngine.UIElements.Align.Center; }
            checkbox.labelElement.style.minWidth = 0;
            checkbox.labelElement.style.width = StyleKeyword.Auto;
            checkbox.labelElement.style.flexGrow = 1;
            checkbox.labelElement.style.flexShrink = 1;
            checkbox.labelElement.style.marginLeft = 8;
            checkbox.labelElement.style.marginRight = 0;
            checkbox.labelElement.style.alignSelf = UnityEngine.UIElements.Align.Center;
            // The input is a flex row containing the indicator, not its visual surface.
            // Painting/sizing that row produces a full-width colored bar in Editor panels.
            var input = checkbox.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (input is null) return;
            var themedStyle = theme?.CheckboxTheme.Style;
            var selected = (states & WidgetStates.Selected) != 0;
            // Unity's Editor skin paints a complete checkbox texture over the fill.
            // Use our shared vector glyph instead, in both Editor and Player panels.
            if (selected && _checkboxCheckmark == null)
                _checkboxCheckmark = UnityEngine.Resources.Load<VectorImage>(LumaIcons.Check.ResourceId);
            input.style.backgroundImage = selected && _checkboxCheckmark != null
                ? new StyleBackground(_checkboxCheckmark) : new StyleBackground(default(Background));
            input.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            var fill = style?.ResolveFillColor(states)
                ?? themedStyle?.ResolveFillColor(states)
                ?? (selected ? theme?.Colors.Primary ?? UnityEngine.Color.blue : theme?.Colors.Surface ?? UnityEngine.Color.white);
            var border = style?.ResolveBorderColor(states)
                ?? themedStyle?.ResolveBorderColor(states)
                ?? theme?.Colors.Outline
                ?? new UnityEngine.Color(0.65f, 0.68f, 0.73f);
            input.style.backgroundColor = fill;
            input.style.borderTopColor = border;
            input.style.borderRightColor = border;
            input.style.borderBottomColor = border;
            input.style.borderLeftColor = border;
            input.style.borderTopWidth = 1f;
            input.style.borderRightWidth = 1f;
            input.style.borderBottomWidth = 1f;
            input.style.borderLeftWidth = 1f;
            input.style.flexGrow = 0f;
            input.style.flexShrink = 0f;
            if ((style?.ResolveSize(states) ?? themedStyle?.ResolveSize(states)) is { } size) {
                input.style.width = size;
                input.style.height = size;
            } else {
                input.style.width = StyleKeyword.Null;
                input.style.height = StyleKeyword.Null;
            }
            var shape = style?.ResolveShape(states) ?? themedStyle?.ResolveShape(states) ?? theme?.Radius.Small;
            if (shape is { } resolvedShape) {
                input.style.borderTopLeftRadius = resolvedShape.TopLeft;
                input.style.borderTopRightRadius = resolvedShape.TopRight;
                input.style.borderBottomRightRadius = resolvedShape.BottomRight;
                input.style.borderBottomLeftRadius = resolvedShape.BottomLeft;
            } else {
                input.style.borderTopLeftRadius = StyleKeyword.Null;
                input.style.borderTopRightRadius = StyleKeyword.Null;
                input.style.borderBottomRightRadius = StyleKeyword.Null;
                input.style.borderBottomLeftRadius = StyleKeyword.Null;
            }
            var checkmark = checkbox.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark is not null) checkmark.style.unityBackgroundImageTintColor = style?.ResolveCheckColor(states)
                ?? themedStyle?.ResolveCheckColor(states)
                ?? theme?.Colors.OnPrimary
                ?? UnityEngine.Color.white;
        }

        public static void ApplyRadio(RadioButton radio, ThemeData? theme, RadioStyle? style, WidgetStates states, TextScaler textScaler = default) {
            if (theme is not null) ApplyLabel(radio, theme, textScaler);
            var input = radio.Q<VisualElement>(className: "unity-radio-button__input");
            if (input is null) return;
            var themedStyle = theme?.RadioTheme.Style;
            var selected = (states & WidgetStates.Selected) != 0;
            var fill = style?.ResolveFillColor(states)
                ?? themedStyle?.ResolveFillColor(states)
                ?? (selected ? theme?.Colors.Primary ?? UnityEngine.Color.blue : theme?.Colors.Surface ?? UnityEngine.Color.white);
            var border = style?.ResolveBorderColor(states)
                ?? themedStyle?.ResolveBorderColor(states)
                ?? (selected ? fill : theme?.Colors.Outline ?? new UnityEngine.Color(0.65f, 0.68f, 0.73f));
            input.style.backgroundColor = fill;
            input.style.borderTopColor = border;
            input.style.borderRightColor = border;
            input.style.borderBottomColor = border;
            input.style.borderLeftColor = border;
            if ((style?.ResolveSize(states) ?? themedStyle?.ResolveSize(states)) is { } size) {
                input.style.width = size;
                input.style.height = size;
            } else {
                input.style.width = StyleKeyword.Null;
                input.style.height = StyleKeyword.Null;
            }
        }

        public static void ApplySlider(UnityEngine.UIElements.Slider slider, ThemeData? theme, SliderStyle? style, WidgetStates states, TextScaler textScaler = default) {
            if (theme is not null) ApplyLabel(slider, theme, textScaler);
            var themedStyle = theme?.SliderTheme.Style;
            var tracker = slider.Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.trackerUssClassName);
            var inactiveTrackColor = style?.InactiveTrackColor?.Resolve(states)
                ?? themedStyle?.InactiveTrackColor?.Resolve(states)
                ?? theme?.Colors.Outline;
            if (tracker is not null) {
                tracker.style.backgroundColor = inactiveTrackColor is { } inactive
                    ? inactive
                    : StyleKeyword.Null;
            }
            var progress = slider.Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.fillUssClassName);
            var activeTrackColor = style?.ActiveTrackColor?.Resolve(states)
                ?? themedStyle?.ActiveTrackColor?.Resolve(states)
                ?? theme?.Colors.Primary;
            if (progress is not null) {
                progress.style.backgroundColor = activeTrackColor is { } active
                    ? active
                    : StyleKeyword.Null;
            }
            var trackHeight = ResolvePositive(
                style?.TrackHeight?.Resolve(states) ?? themedStyle?.TrackHeight?.Resolve(states),
                nameof(SliderStyle.TrackHeight));
            if (tracker is not null) tracker.style.height = trackHeight is { } height ? height : StyleKeyword.Null;
            if (progress is not null) progress.style.height = trackHeight is { } progressHeight ? progressHeight : StyleKeyword.Null;
            // State properties resolve independently of Unity's private visual
            // hierarchy. This keeps their semantics deterministic even if a
            // native child is materialized only after attachment.
            var thumbColor = style?.ThumbColor?.Resolve(states)
                ?? themedStyle?.ThumbColor?.Resolve(states)
                ?? theme?.Colors.Primary;
            var thumbSize = ResolvePositive(
                style?.ThumbSize?.Resolve(states) ?? themedStyle?.ThumbSize?.Resolve(states),
                nameof(SliderStyle.ThumbSize));
            var dragger = slider.Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.draggerUssClassName);
            if (dragger is not null) {
                if (thumbColor is { } thumb) {
                    dragger.style.backgroundColor = thumb;
                    dragger.style.borderTopColor = thumb;
                    dragger.style.borderRightColor = thumb;
                    dragger.style.borderBottomColor = thumb;
                    dragger.style.borderLeftColor = thumb;
                } else {
                    dragger.style.backgroundColor = StyleKeyword.Null;
                    dragger.style.borderTopColor = StyleKeyword.Null;
                    dragger.style.borderRightColor = StyleKeyword.Null;
                    dragger.style.borderBottomColor = StyleKeyword.Null;
                    dragger.style.borderLeftColor = StyleKeyword.Null;
                }
                if (thumbSize is { } size) {
                    dragger.style.width = size;
                    dragger.style.height = size;
                } else {
                    dragger.style.width = StyleKeyword.Null;
                    dragger.style.height = StyleKeyword.Null;
                }
            }
        }

        private static float? ResolvePositive(float? value, string propertyName) {
            if (value is not { } resolved) return null;
            if (float.IsNaN(resolved) || float.IsInfinity(resolved) || resolved <= 0f) {
                throw new System.ArgumentOutOfRangeException(propertyName, "Resolved size must be finite and greater than zero.");
            }
            return resolved;
        }

        private static void ApplyLabel<T>(BaseField<T> field, ThemeData theme, TextScaler textScaler) {
            field.style.color = theme.Colors.OnSurface;
            TextStyleMapper.Apply(field.labelElement, theme.Typography.Label, textScaler);
            // Field labels retain their semantic OnSurfaceVariant color even when
            // the shared typography token also supplies a foreground color.
            field.labelElement.style.color = theme.Colors.OnSurfaceVariant;
            field.style.marginTop = 0f;
            field.style.marginRight = 0f;
            field.style.marginBottom = 0f;
            field.style.marginLeft = 0f;
        }
    }

}
