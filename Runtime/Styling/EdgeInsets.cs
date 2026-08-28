using System;

namespace LumaFlow {

    /// <summary>
    /// Defines pixel-like inset values for padding and margin.
    /// </summary>
    public readonly struct EdgeInsets : IEquatable<EdgeInsets> {
        /// <summary>
        /// Creates insets with explicit values for each side.
        /// </summary>
        public EdgeInsets(float left, float top, float right, float bottom) {
            Validate(left, nameof(left));
            Validate(top, nameof(top));
            Validate(right, nameof(right));
            Validate(bottom, nameof(bottom));

            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Gets zero insets.
        /// </summary>
        public static EdgeInsets Zero => new(0f, 0f, 0f, 0f);

        public float Left { get; }

        public float Top { get; }

        public float Right { get; }

        public float Bottom { get; }

        /// <summary>
        /// Creates equal insets on every side.
        /// </summary>
        public static EdgeInsets All(float value) {
            return new EdgeInsets(value, value, value, value);
        }

        /// <summary>
        /// Creates equal horizontal and vertical insets.
        /// </summary>
        public static EdgeInsets Symmetric(float horizontal = 0f, float vertical = 0f) {
            return new EdgeInsets(horizontal, vertical, horizontal, vertical);
        }

        /// <summary>
        /// Creates insets with only the specified sides.
        /// </summary>
        public static EdgeInsets Only(float left = 0f, float top = 0f, float right = 0f, float bottom = 0f) {
            return new EdgeInsets(left, top, right, bottom);
        }

        public bool Equals(EdgeInsets other) {
            return Left.Equals(other.Left)
                && Top.Equals(other.Top)
                && Right.Equals(other.Right)
                && Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object obj) {
            return obj is EdgeInsets other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Top, Right, Bottom);
        }

        public static bool operator ==(EdgeInsets left, EdgeInsets right) {
            return left.Equals(right);
        }

        public static bool operator !=(EdgeInsets left, EdgeInsets right) {
            return !left.Equals(right);
        }

        private static void Validate(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName, "Insets must be finite and non-negative.");
            }
        }
    }

}
