using System;

namespace LumaFlow {

    /// <summary>
    /// Lets one child participate in native Flexbox sizing without requiring it to grow.
    /// </summary>
    public sealed class Flexible : Widget {
        public Flexible(Widget child, int flex = 1, FlexFit fit = FlexFit.Loose) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (flex < 1) {
                throw new ArgumentOutOfRangeException(nameof(flex), "Flex must be at least one.");
            }

            if (!Enum.IsDefined(typeof(FlexFit), fit)) {
                throw new ArgumentOutOfRangeException(nameof(fit));
            }

            Flex = flex;
            Fit = fit;
        }

        public Widget Child { get; }

        /// <summary>
        /// Gets the relative native flex factor used by this widget.
        /// </summary>
        public int Flex { get; }

        public FlexFit Fit { get; }

        internal override WidgetNode CreateNode() {
            return new FlexibleNode(this);
        }
    }

}
