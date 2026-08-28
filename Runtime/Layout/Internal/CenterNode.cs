using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class CenterNode : SingleChildWidgetNode<Center> {
        public CenterNode(Center widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.justifyContent = Justify.Center;
            element.style.alignItems = UnityEngine.UIElements.Align.Center;
            return element;
        }

        protected override Widget GetChild(Center widget) => widget.Child;
    }

}
