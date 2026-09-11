using System;

namespace LumaFlow {

    /// <summary>
    /// Creates an empty region that consumes available space in a Flexbox parent.
    /// </summary>
    public sealed class Spacer : Widget {
        public Spacer(int flex = 1) {
            if (flex < 1) {
                throw new ArgumentOutOfRangeException(nameof(flex), "Flex must be at least one.");
            }

            Flex = flex;
        }

        /// <summary>
        /// Gets the relative share of available space requested by this spacer.
        /// </summary>
        public int Flex { get; }

        internal override WidgetNode CreateNode() {
            return new SpacerNode(this);
        }
    }

}
