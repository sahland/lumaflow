using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class FractionallySizedBoxNode : SingleChildWidgetNode<FractionallySizedBox> {
        public FractionallySizedBoxNode(FractionallySizedBox widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexGrow = 1f;
            element.style.flexShrink = 1f;
            element.style.minWidth = 0f;
            element.style.minHeight = 0f;
            AlignNode.ApplyAlignment(element, ((FractionallySizedBox)Widget).Alignment);
            return element;
        }

        protected override Widget GetChild(FractionallySizedBox widget) => widget.Child;

        protected override void ApplyConfiguration(FractionallySizedBox widget) {
            AlignNode.ApplyAlignment(Element, widget.Alignment);
        }

        protected override void OnChildReconciled(FractionallySizedBox widget, VisualElement child) {
            child.style.width = widget.WidthFactor is { } width
                ? Length.Percent(width * 100f)
                : StyleKeyword.Null;
            child.style.height = widget.HeightFactor is { } height
                ? Length.Percent(height * 100f)
                : StyleKeyword.Null;
            child.style.flexGrow = 0f;
            child.style.flexShrink = 0f;
        }
    }
}
