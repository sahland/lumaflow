using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class OpacityNode : SingleChildWidgetNode<Opacity> {
        public OpacityNode(Opacity widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.opacity = ((Opacity)Widget).Value;
            return element;
        }

        protected override Widget GetChild(Opacity widget) => widget.Child;

        protected override void ApplyConfiguration(Opacity widget) => Element.style.opacity = widget.Value;
    }

}
