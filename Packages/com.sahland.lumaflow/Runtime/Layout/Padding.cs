using System;

namespace LumaFlow {

    /// <summary>
    /// Adds padding around one child widget.
    /// </summary>
    public sealed class Padding : Widget {
        /// <summary>
        /// Creates a padded child widget.
        /// </summary>
        public Padding(Widget child, EdgeInsets padding) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Insets = padding;
        }

        public Widget Child { get; }

        public EdgeInsets Insets { get; }

        internal override WidgetNode CreateNode() {
            return new PaddingNode(this);
        }
    }

}
