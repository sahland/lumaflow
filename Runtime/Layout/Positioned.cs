#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Applies optional absolute offsets and size inside a Stack or compatible container.
    /// </summary>
    public sealed class Positioned : Widget {
        public Positioned(
            Widget child,
            float? left = null,
            float? top = null,
            float? right = null,
            float? bottom = null,
            float? width = null,
            float? height = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Validate(left, nameof(left));
            Validate(top, nameof(top));
            Validate(right, nameof(right));
            Validate(bottom, nameof(bottom));
            Validate(width, nameof(width));
            Validate(height, nameof(height));

            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            Width = width;
            Height = height;
        }

        public Widget Child { get; }

        public float? Left { get; }

        public float? Top { get; }

        public float? Right { get; }

        public float? Bottom { get; }

        public float? Width { get; }

        public float? Height { get; }

        internal override WidgetNode CreateNode() {
            return new PositionedNode(this);
        }

        private static void Validate(float? value, string parameterName) {
            if (value is { } number && (float.IsNaN(number) || float.IsInfinity(number))) {
                throw new ArgumentOutOfRangeException(parameterName, "Position values must be finite.");
            }
        }
    }

}
