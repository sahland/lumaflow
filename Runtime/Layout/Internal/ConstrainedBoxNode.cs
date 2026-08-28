using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ConstrainedBoxNode : SingleChildWidgetNode<ConstrainedBox> {
        public ConstrainedBoxNode(ConstrainedBox widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var box = new VisualElement();
            ApplyConstraints(box, ((ConstrainedBox)Widget).Constraints);
            return box;
        }

        protected override Widget GetChild(ConstrainedBox widget) => widget.Child;

        protected override void ApplyConfiguration(ConstrainedBox widget) {
            ApplyConstraints(Element, widget.Constraints);
        }

        private static void ApplyConstraints(VisualElement box, BoxConstraints constraints) {
            box.style.minWidth = StyleKeyword.Null;
            box.style.maxWidth = StyleKeyword.Null;
            box.style.minHeight = StyleKeyword.Null;
            box.style.maxHeight = StyleKeyword.Null;

            if (constraints.MinWidth is { } minWidth) {
                box.style.minWidth = minWidth;
            }

            if (constraints.MaxWidth is { } maxWidth) {
                box.style.maxWidth = maxWidth;
            }

            if (constraints.MinHeight is { } minHeight) {
                box.style.minHeight = minHeight;
            }

            if (constraints.MaxHeight is { } maxHeight) {
                box.style.maxHeight = maxHeight;
            }

        }
    }

}
