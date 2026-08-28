using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Defines the color and width of one border side.
    /// </summary>
    public readonly struct BorderSide : IEquatable<BorderSide> {
        public BorderSide(Color color, float width = 1f) {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 0f) {
                throw new ArgumentOutOfRangeException(nameof(width), "Border width must be finite and non-negative.");
            }

            Color = color;
            Width = width;
        }

        public Color Color { get; }

        public float Width { get; }

        public bool Equals(BorderSide other) {
            return Color.Equals(other.Color) && Width.Equals(other.Width);
        }

        public override bool Equals(object obj) {
            return obj is BorderSide other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Color, Width);
        }

        public static bool operator ==(BorderSide left, BorderSide right) {
            return left.Equals(right);
        }

        public static bool operator !=(BorderSide left, BorderSide right) {
            return !left.Equals(right);
        }
    }

}
