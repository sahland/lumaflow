#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Applies optional minimum and maximum dimensions to one child widget.
    /// </summary>
    public sealed class ConstrainedBox : Widget {
        public ConstrainedBox(Widget child, BoxConstraints constraints) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Constraints = constraints;
        }

        public Widget Child { get; }

        public BoxConstraints Constraints { get; }

        /// <summary>Constrains each supplied axis to one exact finite size.</summary>
        public static ConstrainedBox Tight(Widget child, float? width = null, float? height = null) =>
            new(child, new BoxConstraints(width, width, height, height));

        /// <summary>Applies only maximum bounds to a child.</summary>
        public static ConstrainedBox AtMost(Widget child, float? width = null, float? height = null) =>
            new(child, new BoxConstraints(maxWidth: width, maxHeight: height));

        /// <summary>Applies only minimum bounds to a child.</summary>
        public static ConstrainedBox AtLeast(Widget child, float? width = null, float? height = null) =>
            new(child, new BoxConstraints(minWidth: width, minHeight: height));

        internal override WidgetNode CreateNode() {
            return new ConstrainedBoxNode(this);
        }
    }

}
