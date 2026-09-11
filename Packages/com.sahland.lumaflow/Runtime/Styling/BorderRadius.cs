using System;

namespace LumaFlow {

    /// <summary>
    /// Defines pixel-like corner radii for a box decoration.
    /// </summary>
    public readonly struct BorderRadius : IEquatable<BorderRadius> {
        /// <summary>
        /// Creates corner radii with explicit values for every corner.
        /// </summary>
        public BorderRadius(float topLeft, float topRight, float bottomRight, float bottomLeft) {
            Validate(topLeft, nameof(topLeft));
            Validate(topRight, nameof(topRight));
            Validate(bottomRight, nameof(bottomRight));
            Validate(bottomLeft, nameof(bottomLeft));

            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        public float TopLeft { get; }

        public float TopRight { get; }

        public float BottomRight { get; }

        public float BottomLeft { get; }

        /// <summary>
        /// Creates equal radii on every corner.
        /// </summary>
        public static BorderRadius All(float value) {
            return new BorderRadius(value, value, value, value);
        }

        public bool Equals(BorderRadius other) {
            return TopLeft.Equals(other.TopLeft)
                && TopRight.Equals(other.TopRight)
                && BottomRight.Equals(other.BottomRight)
                && BottomLeft.Equals(other.BottomLeft);
        }

        public override bool Equals(object obj) {
            return obj is BorderRadius other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
        }

        public static bool operator ==(BorderRadius left, BorderRadius right) {
            return left.Equals(right);
        }

        public static bool operator !=(BorderRadius left, BorderRadius right) {
            return !left.Equals(right);
        }

        private static void Validate(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName, "Border radii must be finite and non-negative.");
            }
        }
    }

}
