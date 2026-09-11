using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ExpandedNode : SingleChildWidgetNode<Expanded> {
        public ExpandedNode(Expanded widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexGrow = ((Expanded)Widget).Flex;
            return element;
        }

        protected override Widget GetChild(Expanded widget) => widget.Child;

        protected override void ApplyConfiguration(Expanded widget) {
            Element.style.flexGrow = widget.Flex;
        }

        protected override void OnChildReconciled(Expanded widget, VisualElement child) {
            child.style.flexGrow = 1f;
            child.style.width = Length.Percent(100f);
            child.style.height = Length.Percent(100f);
        }
    }

}
