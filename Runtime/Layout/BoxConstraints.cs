#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Immutable optional minimum and maximum pixel-like dimensions for a widget.
    /// </summary>
    public readonly struct BoxConstraints : IEquatable<BoxConstraints> {
        public BoxConstraints(
            float? minWidth = null,
            float? maxWidth = null,
            float? minHeight = null,
            float? maxHeight = null) {
            ValidateDimension(minWidth, nameof(minWidth));
            ValidateDimension(maxWidth, nameof(maxWidth));
            ValidateDimension(minHeight, nameof(minHeight));
            ValidateDimension(maxHeight, nameof(maxHeight));
            ValidateRange(minWidth, maxWidth, nameof(minWidth), nameof(maxWidth));
            ValidateRange(minHeight, maxHeight, nameof(minHeight), nameof(maxHeight));

            MinWidth = minWidth;
            MaxWidth = maxWidth;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }

        public float? MinWidth { get; }

        public float? MaxWidth { get; }

        public float? MinHeight { get; }

        public float? MaxHeight { get; }

        public bool Equals(BoxConstraints other) {
            return MinWidth == other.MinWidth
                && MaxWidth == other.MaxWidth
                && MinHeight == other.MinHeight
                && MaxHeight == other.MaxHeight;
        }

        public override bool Equals(object? obj) {
            return obj is BoxConstraints other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight);
        }

        public static bool operator ==(BoxConstraints left, BoxConstraints right) {
            return left.Equals(right);
        }

        public static bool operator !=(BoxConstraints left, BoxConstraints right) {
            return !left.Equals(right);
        }

        private static void ValidateDimension(float? value, string parameterName) {
            if (value is { } dimension && (float.IsNaN(dimension) || float.IsInfinity(dimension) || dimension < 0f)) {
                throw new ArgumentOutOfRangeException(parameterName, "Constraints must be finite and non-negative.");
            }
        }

        private static void ValidateRange(float? minimum, float? maximum, string minimumName, string maximumName) {
            if (minimum is { } minimumValue && maximum is { } maximumValue && minimumValue > maximumValue) {
                throw new ArgumentException("Minimum constraint cannot exceed maximum constraint.", maximumName);
            }
        }
    }

}
