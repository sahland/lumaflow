using System;

namespace LumaFlow {

    /// <summary>
    /// Adds outer margin around one child widget.
    /// </summary>
    public sealed class Margin : Widget {
        public Margin(Widget child, EdgeInsets margin) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Insets = margin;
        }

        public Widget Child { get; }

        public EdgeInsets Insets { get; }

        internal override WidgetNode CreateNode() {
            return new MarginNode(this);
        }
    }

}
