using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class SizedBoxNode : SingleChildWidgetNode<SizedBox> {
        public SizedBoxNode(SizedBox widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var box = new VisualElement();
            ApplySize(box, (SizedBox)Widget);
            return box;
        }

        protected override Widget GetChild(SizedBox widget) => widget.Child;

        protected override void ApplyConfiguration(SizedBox widget) => ApplySize(Element, widget);

        protected override void OnChildReconciled(SizedBox widget, VisualElement child) {
            child.style.width = widget.Width is null && !widget.ExpandsWidth
                ? StyleKeyword.Null
                : Length.Percent(100f);
            child.style.height = widget.Height is null && !widget.ExpandsHeight
                ? StyleKeyword.Null
                : Length.Percent(100f);
        }

        private static void ApplySize(VisualElement box, SizedBox widget) {
            box.style.width = StyleKeyword.Null;
            box.style.minWidth = StyleKeyword.Null;
            box.style.height = StyleKeyword.Null;
            box.style.minHeight = StyleKeyword.Null;
            box.style.flexShrink = StyleKeyword.Null;
            box.style.flexGrow = StyleKeyword.Null;
            if (widget.Width is { } width) {
                box.style.width = width;
                box.style.minWidth = width;
                box.style.flexShrink = 0f;
                box.style.flexGrow = 0f;
            } else if (widget.ExpandsWidth) {
                box.style.width = Length.Percent(100f);
                box.style.flexGrow = 1f;
            }
            if (widget.Height is { } height) {
                box.style.height = height;
                box.style.minHeight = height;
            } else if (widget.ExpandsHeight) {
                box.style.height = Length.Percent(100f);
                box.style.flexGrow = 1f;
            }
        }
    }

}
