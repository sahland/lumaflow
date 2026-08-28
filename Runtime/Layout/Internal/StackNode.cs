using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class StackNode : WidgetNode {
        public StackNode(Stack widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.position = Position.Relative;
            return element;
        }

        protected override void OnMounted() {
            ReconcileChildren(((Stack)Widget).Children, Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not Stack next || !CanUpdateWith(next)) return false;
            ReconcileChildren(next.Children, Element);
            UpdateWidget(next);
            return true;
        }
    }

}
