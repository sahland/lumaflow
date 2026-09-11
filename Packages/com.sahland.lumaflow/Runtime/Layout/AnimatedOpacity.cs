#nullable enable

using System;

namespace LumaFlow {

    /// <summary>Animates one child's opacity when the bound value changes.</summary>
    public sealed class AnimatedOpacity : Widget {
        public AnimatedOpacity(
            Widget child,
            State<float> value,
            TimeSpan duration,
            Curve? curve = null,
            Action? onEnd = null,
            AnimationBehavior behavior = AnimationBehavior.Normal) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ValidateOpacity(value.Value, nameof(value));
            Spec = new AnimationSpec(duration, curve, behavior);
            OnEnd = onEnd;
        }

        public Widget Child { get; }
        public State<float> Value { get; }
        public TimeSpan Duration => Spec.Duration;
        public Curve Curve => Spec.Curve;
        public AnimationBehavior Behavior => Spec.Behavior;
        public Action? OnEnd { get; }
        internal AnimationSpec Spec { get; }

        internal override WidgetNode CreateNode() => new AnimatedOpacityNode(this);

        internal static void ValidateOpacity(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f) {
                throw new ArgumentOutOfRangeException(parameterName, "Opacity must be finite and between zero and one.");
            }
        }
    }

}
