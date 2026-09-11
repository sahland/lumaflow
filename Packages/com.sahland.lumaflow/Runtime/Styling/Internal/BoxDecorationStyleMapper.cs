using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class BoxDecorationStyleMapper {
        public static void Clear(VisualElement element) {
            element.style.backgroundColor = StyleKeyword.Null;
            element.style.borderTopLeftRadius = StyleKeyword.Null;
            element.style.borderTopRightRadius = StyleKeyword.Null;
            element.style.borderBottomRightRadius = StyleKeyword.Null;
            element.style.borderBottomLeftRadius = StyleKeyword.Null;
            BorderStyleMapper.Clear(element);
        }

        public static void Apply(VisualElement element, BoxDecoration decoration) {
            if (decoration.BackgroundColor is { } backgroundColor) {
                element.style.backgroundColor = backgroundColor;
            }

            if (decoration.BorderRadius is { } borderRadius) {
                element.style.borderTopLeftRadius = borderRadius.TopLeft;
                element.style.borderTopRightRadius = borderRadius.TopRight;
                element.style.borderBottomRightRadius = borderRadius.BottomRight;
                element.style.borderBottomLeftRadius = borderRadius.BottomLeft;
            }

            if (decoration.Border is { } border) {
                BorderStyleMapper.Apply(element, border);
            }
        }
    }

}
