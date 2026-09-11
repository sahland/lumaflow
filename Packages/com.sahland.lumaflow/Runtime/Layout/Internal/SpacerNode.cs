using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class SpacerNode : WidgetNode {
        public SpacerNode(Spacer widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexGrow = ((Spacer)Widget).Flex;
            return element;
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Spacer spacer) return false;
            Element.style.flexGrow = spacer.Flex;
            UpdateWidget(spacer);
            return true;
        }
    }

}
