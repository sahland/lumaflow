using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class PaddingNode : SingleChildWidgetNode<Padding> {
        public PaddingNode(Padding widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            PaddingStyleMapper.Apply(element, ((Padding)Widget).Insets);
            return element;
        }

        protected override Widget GetChild(Padding widget) => widget.Child;

        protected override void ApplyConfiguration(Padding widget) => PaddingStyleMapper.Apply(Element, widget.Insets);
    }

}
