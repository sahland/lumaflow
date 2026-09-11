using System;

namespace LumaFlow {

    /// <summary>
    /// Positions one child at a supported location within its available space.
    /// </summary>
    public sealed class Align : Widget {
        public Align(Widget child, Alignment alignment = Alignment.Center) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (!Enum.IsDefined(typeof(Alignment), alignment)) {
                throw new ArgumentOutOfRangeException(nameof(alignment));
            }

            Alignment = alignment;
        }

        public Widget Child { get; }

        public Alignment Alignment { get; }

        public static Align TopLeft(Widget child) => new(child, Alignment.TopLeft);
        public static Align TopCenter(Widget child) => new(child, Alignment.TopCenter);
        public static Align TopRight(Widget child) => new(child, Alignment.TopRight);
        public static Align CenterLeft(Widget child) => new(child, Alignment.CenterLeft);
        public static Align CenterRight(Widget child) => new(child, Alignment.CenterRight);
        public static Align BottomLeft(Widget child) => new(child, Alignment.BottomLeft);
        public static Align BottomCenter(Widget child) => new(child, Alignment.BottomCenter);
        public static Align BottomRight(Widget child) => new(child, Alignment.BottomRight);

        internal override WidgetNode CreateNode() {
            return new AlignNode(this);
        }
    }

}
