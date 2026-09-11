using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class TextStyleMapper {
        public static void Apply(VisualElement element, TextStyle style, TextScaler textScaler = default) {
            if (style.Color is { } color) {
                element.style.color = color;
            }

            if (style.FontSize is { } fontSize) {
                element.style.fontSize = textScaler.OrDefault().Scale(fontSize);
            }

            if (style.FontStyle is { } fontStyle) {
                element.style.unityFontStyleAndWeight = fontStyle;
            }
        }
    }

}
