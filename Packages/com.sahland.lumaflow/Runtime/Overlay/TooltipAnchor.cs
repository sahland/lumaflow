#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Shows a visual tooltip while the supplied child is hovered or focused.</summary>
    public sealed class TooltipAnchor : Widget {
        public TooltipAnchor(
            Widget child,
            Tooltip tooltip,
            OverlayController controller,
            PopoverPlacement placement = PopoverPlacement.Top) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Tooltip = tooltip ?? throw new ArgumentNullException(nameof(tooltip));
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            if (!Enum.IsDefined(typeof(PopoverPlacement), placement)) {
                throw new ArgumentOutOfRangeException(nameof(placement));
            }
            Placement = placement;
        }

        public Widget Child { get; }
        public Tooltip Tooltip { get; }
        public OverlayController Controller { get; }
        public PopoverPlacement Placement { get; }

        internal override WidgetNode CreateNode() => new TooltipAnchorNode(this);
    }

    internal sealed class TooltipAnchorNode : WidgetNode {
        private WidgetNode? _child;
        private OverlayHandle? _tooltipHandle;

        public TooltipAnchorNode(TooltipAnchor widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexShrink = 1f;
            return element;
        }

        protected override void OnMounted() {
            var widget = (TooltipAnchor)Widget;
            Element.tooltip = widget.Tooltip.Text;
            _child = MountChild(widget.Child, Element);
            Element.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Element.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            Element.RegisterCallback<FocusInEvent>(HandleFocusIn);
            Element.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(DisposeBindings);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TooltipAnchor anchor) return false;
            var previous = (TooltipAnchor)Widget;
            if (!ReferenceEquals(previous.Tooltip, anchor.Tooltip)
                || !ReferenceEquals(previous.Controller, anchor.Controller)
                || previous.Placement != anchor.Placement) {
                CloseTooltip();
            }

            ReconcileSingleChild(ref _child, anchor.Child, Element);
            Element.tooltip = anchor.Tooltip.Text;
            UpdateWidget(anchor);
            return true;
        }

        private void HandlePointerEnter(PointerEnterEvent _) => ShowTooltip();
        private void HandlePointerLeave(PointerLeaveEvent _) => CloseTooltip();
        private void HandleFocusIn(FocusInEvent _) => ShowTooltip();
        private void HandleFocusOut(FocusOutEvent _) => CloseTooltip();

        private void ShowTooltip() {
            if (_tooltipHandle?.IsOpen == true) return;
            var widget = (TooltipAnchor)Widget;
            _tooltipHandle = widget.Tooltip.Show(widget.Controller, Element, widget.Placement);
        }

        private void CloseTooltip() {
            _tooltipHandle?.Close();
            _tooltipHandle = null;
        }

        private void DisposeBindings() {
            Element.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Element.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            Element.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            Element.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
            CloseTooltip();
        }
    }
}
