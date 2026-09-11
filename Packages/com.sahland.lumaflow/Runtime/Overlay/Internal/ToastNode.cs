#nullable enable

using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ToastNode : SingleChildWidgetNode<Toast> {
        public ToastNode(Toast widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var toast = new VisualElement();
            toast.AddToClassList("lumaflow-toast");
            return toast;
        }

        protected override Widget GetChild(Toast widget) => widget.Content;
    }

}
