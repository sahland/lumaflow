using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class MarginStyleMapper {
        public static void Clear(VisualElement element) {
            element.style.marginLeft = StyleKeyword.Null;
            element.style.marginTop = StyleKeyword.Null;
            element.style.marginRight = StyleKeyword.Null;
            element.style.marginBottom = StyleKeyword.Null;
        }

        public static void Apply(VisualElement element, EdgeInsets margin) {
            element.style.marginLeft = margin.Left;
            element.style.marginTop = margin.Top;
            element.style.marginRight = margin.Right;
            element.style.marginBottom = margin.Bottom;
        }
    }

}
