using System;

namespace LumaFlow {

    /// <summary>
    /// Centers one child on both axes using native Flexbox layout.
    /// </summary>
    public sealed class Center : Widget {
        public Center(Widget child) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public Widget Child { get; }

        internal override WidgetNode CreateNode() {
            return new CenterNode(this);
        }
    }

}
