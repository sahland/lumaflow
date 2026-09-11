#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Describes the available environment of the current UI panel layout scope.
    /// </summary>
    /// <remarks>
    /// These values represent UI Toolkit panel space, not a physical device category.
    /// They are therefore suitable for editor windows, embedded UI, split views, and
    /// windowed applications alike.
    /// </remarks>
    public readonly struct MediaQueryData : IEquatable<MediaQueryData> {
        public MediaQueryData(float width, float height, bool disableAnimations = false) {
            ValidateDimension(width, nameof(width));
            ValidateDimension(height, nameof(height));
            Width = width;
            Height = height;
            DisableAnimations = disableAnimations;
        }

        /// <summary>Gets the resolved available width of this layout scope.</summary>
        public float Width { get; }

        /// <summary>Gets the resolved available height of this layout scope.</summary>
        public float Height { get; }

        /// <summary>Gets whether motion should be disabled or reduced as much as possible.</summary>
        public bool DisableAnimations { get; }

        /// <summary>Gets whether this scope has received a non-zero geometry measurement.</summary>
        public bool HasResolvedSize => Width > 0f || Height > 0f;

        /// <summary>Returns this policy with a different resolved panel size.</summary>
        public MediaQueryData WithSize(float width, float height) =>
            new(width, height, DisableAnimations);

        public bool Equals(MediaQueryData other) =>
            Width == other.Width
            && Height == other.Height
            && DisableAnimations == other.DisableAnimations;

        public override bool Equals(object? obj) => obj is MediaQueryData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Width, Height, DisableAnimations);

        public static bool operator ==(MediaQueryData left, MediaQueryData right) => left.Equals(right);

        public static bool operator !=(MediaQueryData left, MediaQueryData right) => !left.Equals(right);

        private static void ValidateDimension(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName, "Media query dimensions must be finite and non-negative.");
            }
        }
    }

}
