#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// The resolved available size passed to a <see cref="LayoutBuilder"/>.
    /// </summary>
    public readonly struct LayoutConstraints : IEquatable<LayoutConstraints> {
        public LayoutConstraints(float maxWidth, float maxHeight) {
            ValidateDimension(maxWidth, nameof(maxWidth));
            ValidateDimension(maxHeight, nameof(maxHeight));
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
        }

        /// <summary>Gets the resolved maximum width available to the builder.</summary>
        public float MaxWidth { get; }

        /// <summary>Gets the resolved maximum height available to the builder.</summary>
        public float MaxHeight { get; }

        public bool Equals(LayoutConstraints other) => MaxWidth == other.MaxWidth && MaxHeight == other.MaxHeight;

        public override bool Equals(object? obj) => obj is LayoutConstraints other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(MaxWidth, MaxHeight);

        public static bool operator ==(LayoutConstraints left, LayoutConstraints right) => left.Equals(right);

        public static bool operator !=(LayoutConstraints left, LayoutConstraints right) => !left.Equals(right);

        private static void ValidateDimension(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName, "Layout dimensions must be finite and non-negative.");
            }
        }
    }

}
