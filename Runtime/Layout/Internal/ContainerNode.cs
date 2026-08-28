using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ContainerNode : SingleChildWidgetNode<Container> {
        public ContainerNode(Container widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            var widget = (Container)Widget;
            ApplyContainer(element, widget);
            return element;
        }

        protected override Widget GetChild(Container widget) => widget.Child;

        protected override void ApplyConfiguration(Container widget) => ApplyContainer(Element, widget);

        private static void ApplyContainer(VisualElement element, Container widget) {
            BoxDecorationStyleMapper.Clear(element);
            PaddingStyleMapper.Clear(element);
            if (widget.Decoration is { } decoration) BoxDecorationStyleMapper.Apply(element, decoration);
            if (widget.Padding is { } padding) PaddingStyleMapper.Apply(element, padding);
        }
    }

}
