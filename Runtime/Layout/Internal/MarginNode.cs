using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class MarginNode : SingleChildWidgetNode<Margin> {
        public MarginNode(Margin widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            MarginStyleMapper.Apply(element, ((Margin)Widget).Insets);
            return element;
        }

        protected override Widget GetChild(Margin widget) => widget.Child;

        protected override void ApplyConfiguration(Margin widget) => MarginStyleMapper.Apply(Element, widget.Insets);
    }

}
