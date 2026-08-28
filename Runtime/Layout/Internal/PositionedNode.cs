using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class PositionedNode : SingleChildWidgetNode<Positioned> {
        public PositionedNode(Positioned widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            ApplyPosition(element, (Positioned)Widget);
            return element;
        }

        protected override Widget GetChild(Positioned widget) => widget.Child;

        protected override void ApplyConfiguration(Positioned widget) => ApplyPosition(Element, widget);

        private static void ApplyPosition(VisualElement element, Positioned positioned) {
            element.style.left = StyleKeyword.Null;
            element.style.top = StyleKeyword.Null;
            element.style.right = StyleKeyword.Null;
            element.style.bottom = StyleKeyword.Null;
            element.style.width = StyleKeyword.Null;
            element.style.height = StyleKeyword.Null;

            if (positioned.Left is { } left) {
                element.style.left = left;
            }

            if (positioned.Top is { } top) {
                element.style.top = top;
            }

            if (positioned.Right is { } right) {
                element.style.right = right;
            }

            if (positioned.Bottom is { } bottom) {
                element.style.bottom = bottom;
            }

            if (positioned.Width is { } width) {
                element.style.width = width;
            }

            if (positioned.Height is { } height) {
                element.style.height = height;
            }

        }
    }

}
