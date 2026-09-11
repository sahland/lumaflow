using System;

namespace LumaFlow {

    /// <summary>
    /// Hosts one scrollable child using the native UI Toolkit scroll view.
    /// </summary>
    public sealed class ScrollView : Widget {
        public ScrollView(Widget child, Axis direction = Axis.Vertical, EdgeInsets? padding = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (!Enum.IsDefined(typeof(Axis), direction)) {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            Direction = direction;
            Padding = padding;
        }

        public Widget Child { get; }

        public Axis Direction { get; }

        /// <summary>
        /// Gets the native content insets applied without inserting a wrapper widget.
        /// </summary>
        public EdgeInsets? Padding { get; }

        internal override WidgetNode CreateNode() {
            return new ScrollViewNode(this);
        }
    }

}
