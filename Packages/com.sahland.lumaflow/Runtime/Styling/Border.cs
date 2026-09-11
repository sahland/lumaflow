using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Defines border sides for a box decoration.
    /// </summary>
    public readonly struct Border : IEquatable<Border> {
        public Border(BorderSide left, BorderSide top, BorderSide right, BorderSide bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public BorderSide Left { get; }

        public BorderSide Top { get; }

        public BorderSide Right { get; }

        public BorderSide Bottom { get; }

        /// <summary>
        /// Creates an identical border on every side.
        /// </summary>
        public static Border All(Color color, float width = 1f) {
            var side = new BorderSide(color, width);
            return new Border(side, side, side, side);
        }

        public bool Equals(Border other) {
            return Left.Equals(other.Left)
                && Top.Equals(other.Top)
                && Right.Equals(other.Right)
                && Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object obj) {
            return obj is Border other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Top, Right, Bottom);
        }

        public static bool operator ==(Border left, Border right) {
            return left.Equals(right);
        }

        public static bool operator !=(Border left, Border right) {
            return !left.Equals(right);
        }
    }

}
