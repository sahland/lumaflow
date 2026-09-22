using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class AlignNode : SingleChildWidgetNode<Align> {
        public AlignNode(Align widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            ApplyAlignment(element, ((Align)Widget).Alignment);
            return element;
        }

        protected override Widget GetChild(Align widget) => widget.Child;

        protected override void ApplyConfiguration(Align widget) => ApplyAlignment(Element, widget.Alignment);

        internal static void ApplyAlignment(VisualElement element, Alignment alignment) {
            switch (alignment) {
                case Alignment.TopLeft:
                    element.style.justifyContent = Justify.FlexStart;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexStart;
                    break;
                case Alignment.TopCenter:
                    element.style.justifyContent = Justify.FlexStart;
                    element.style.alignItems = UnityEngine.UIElements.Align.Center;
                    break;
                case Alignment.TopRight:
                    element.style.justifyContent = Justify.FlexStart;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexEnd;
                    break;
                case Alignment.CenterLeft:
                    element.style.justifyContent = Justify.Center;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexStart;
                    break;
                case Alignment.Center:
                    element.style.justifyContent = Justify.Center;
                    element.style.alignItems = UnityEngine.UIElements.Align.Center;
                    break;
                case Alignment.CenterRight:
                    element.style.justifyContent = Justify.Center;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexEnd;
                    break;
                case Alignment.BottomLeft:
                    element.style.justifyContent = Justify.FlexEnd;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexStart;
                    break;
                case Alignment.BottomCenter:
                    element.style.justifyContent = Justify.FlexEnd;
                    element.style.alignItems = UnityEngine.UIElements.Align.Center;
                    break;
                case Alignment.BottomRight:
                    element.style.justifyContent = Justify.FlexEnd;
                    element.style.alignItems = UnityEngine.UIElements.Align.FlexEnd;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(alignment));
            }
        }
    }

}
