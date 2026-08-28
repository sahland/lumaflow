#nullable enable

using System;

namespace LumaFlow {

    /// <summary>Creates a scoped layer above its normal child content.</summary>
    public sealed class OverlayHost : Widget {
        public OverlayHost(Widget child, OverlayController controller) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public Widget Child { get; }
        public OverlayController Controller { get; }

        internal override WidgetNode CreateNode() => new OverlayHostNode(this);
    }

}
