using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class FlexibleNode : SingleChildWidgetNode<Flexible> {
        public FlexibleNode(Flexible widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            ApplyFlex(element, (Flexible)Widget);
            return element;
        }

        protected override Widget GetChild(Flexible widget) => widget.Child;

        protected override void ApplyConfiguration(Flexible widget) => ApplyFlex(Element, widget);

        protected override void OnChildReconciled(Flexible widget, VisualElement child) {
            child.style.flexGrow = StyleKeyword.Null;
            child.style.width = StyleKeyword.Null;
            child.style.height = StyleKeyword.Null;
            if (widget.Fit != FlexFit.Tight) return;
            child.style.flexGrow = 1f;
            child.style.width = Length.Percent(100f);
            child.style.height = Length.Percent(100f);
        }

        private static void ApplyFlex(VisualElement element, Flexible widget) {
            element.style.flexGrow = StyleKeyword.Null;
            element.style.flexShrink = StyleKeyword.Null;
            element.style.flexGrow = widget.Flex;
            element.style.flexShrink = 1f;
        }
    }

}
