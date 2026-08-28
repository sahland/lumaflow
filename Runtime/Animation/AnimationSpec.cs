#nullable enable

using System;

namespace LumaFlow {
    public enum AnimationBehavior {
        Normal,
        Preserve
    }

    /// <summary>Immutable timing and accessibility behavior for one implicit animation.</summary>
    public readonly struct AnimationSpec : IEquatable<AnimationSpec> {
        public AnimationSpec(
            TimeSpan duration,
            Curve? curve = null,
            AnimationBehavior behavior = AnimationBehavior.Normal) {
            if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration), "Animation duration must be positive.");
            if (!Enum.IsDefined(typeof(AnimationBehavior), behavior)) throw new ArgumentOutOfRangeException(nameof(behavior));
            Duration = duration;
            Curve = curve ?? Curves.Linear;
            Behavior = behavior;
        }

        public TimeSpan Duration { get; }
        public Curve Curve { get; }
        public AnimationBehavior Behavior { get; }

        public bool Equals(AnimationSpec other) =>
            Duration == other.Duration
            && ReferenceEquals(Curve, other.Curve)
            && Behavior == other.Behavior;

        public override bool Equals(object? obj) => obj is AnimationSpec other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Duration, Curve, Behavior);

        public static bool operator ==(AnimationSpec left, AnimationSpec right) => left.Equals(right);
        public static bool operator !=(AnimationSpec left, AnimationSpec right) => !left.Equals(right);
    }
}
