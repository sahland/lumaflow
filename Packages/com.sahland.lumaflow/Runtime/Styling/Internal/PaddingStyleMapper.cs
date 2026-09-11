using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class PaddingStyleMapper {
        public static void Clear(VisualElement element) {
            element.style.paddingLeft = StyleKeyword.Null;
            element.style.paddingTop = StyleKeyword.Null;
            element.style.paddingRight = StyleKeyword.Null;
            element.style.paddingBottom = StyleKeyword.Null;
        }

        public static void Apply(VisualElement element, EdgeInsets padding) {
            element.style.paddingLeft = padding.Left;
            element.style.paddingTop = padding.Top;
            element.style.paddingRight = padding.Right;
            element.style.paddingBottom = padding.Bottom;
        }
    }

}
