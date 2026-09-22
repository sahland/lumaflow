#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {
    /// <summary>Describes a two-color linear background gradient.</summary>
    public readonly struct LinearGradient : IEquatable<LinearGradient> {
        public LinearGradient(Color startColor, Color endColor, float angle = 0f) {
            if (float.IsNaN(angle) || float.IsInfinity(angle)) {
                throw new ArgumentOutOfRangeException(nameof(angle), angle, "Gradient angle must be finite.");
            }
            StartColor = startColor;
            EndColor = endColor;
            Angle = Mathf.Repeat(angle, 360f);
        }

        public Color StartColor { get; }
        public Color EndColor { get; }

        /// <summary>Clockwise direction in degrees. Zero runs from left to right.</summary>
        public float Angle { get; }

        public bool Equals(LinearGradient other) => StartColor.Equals(other.StartColor)
            && EndColor.Equals(other.EndColor) && Angle.Equals(other.Angle);

        public override bool Equals(object? obj) => obj is LinearGradient other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(StartColor, EndColor, Angle);
        public static bool operator ==(LinearGradient left, LinearGradient right) => left.Equals(right);
        public static bool operator !=(LinearGradient left, LinearGradient right) => !left.Equals(right);
    }
}
