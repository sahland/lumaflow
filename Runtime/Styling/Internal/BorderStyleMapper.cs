using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class BorderStyleMapper {
        public static void Clear(VisualElement element) {
            element.style.borderLeftColor = StyleKeyword.Null;
            element.style.borderLeftWidth = StyleKeyword.Null;
            element.style.borderTopColor = StyleKeyword.Null;
            element.style.borderTopWidth = StyleKeyword.Null;
            element.style.borderRightColor = StyleKeyword.Null;
            element.style.borderRightWidth = StyleKeyword.Null;
            element.style.borderBottomColor = StyleKeyword.Null;
            element.style.borderBottomWidth = StyleKeyword.Null;
        }

        public static void Apply(VisualElement element, Border border) {
            element.style.borderLeftColor = border.Left.Color;
            element.style.borderLeftWidth = border.Left.Width;
            element.style.borderTopColor = border.Top.Color;
            element.style.borderTopWidth = border.Top.Width;
            element.style.borderRightColor = border.Right.Color;
            element.style.borderRightWidth = border.Right.Width;
            element.style.borderBottomColor = border.Bottom.Color;
            element.style.borderBottomWidth = border.Bottom.Width;
        }
    }

}
