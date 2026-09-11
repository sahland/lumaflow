using System;

namespace LumaFlow {

    /// <summary>
    /// Expands one child to occupy available space in an immediate <see cref="Row"/>
    /// or <see cref="Column"/> parent.
    /// </summary>
    public sealed class Expanded : Widget {
        public Expanded(Widget child, int flex = 1) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (flex < 1) {
                throw new ArgumentOutOfRangeException(nameof(flex), "Flex must be at least one.");
            }

            Flex = flex;
        }

        public Widget Child { get; }

        /// <summary>
        /// Gets the relative share of available space requested by this widget.
        /// </summary>
        public int Flex { get; }

        internal override WidgetNode CreateNode() {
            return new ExpandedNode(this);
        }
    }

}
