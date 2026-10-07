#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {
    public delegate void StyleValueApplier<T>(VisualElement element, T value);

    /// <summary>
    /// Animates a native style value without rebuilding the child widget tree on each frame.
    /// </summary>
    public sealed class AnimatedStyle<T> : Widget {
        public AnimatedStyle(
            Widget child,
            Tween<T> tween,
            TimeSpan duration,
            StyleValueApplier<T> apply,
            Curve? curve = null,
            Action? onEnd = null,
            AnimationBehavior behavior = AnimationBehavior.Normal)
            : this(child, tween, new AnimationSpec(duration, curve, behavior), apply, onEnd) {
        }

        public AnimatedStyle(
            Widget child,
            Tween<T> tween,
            AnimationSpec spec,
            StyleValueApplier<T> apply,
            Action? onEnd = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Tween = tween ?? throw new ArgumentNullException(nameof(tween));
            if (spec.Curve is null || spec.Duration <= TimeSpan.Zero) {
                throw new ArgumentException("AnimationSpec must be initialized with a positive duration.", nameof(spec));
            }
            Spec = spec;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
            OnEnd = onEnd;
        }

        public Widget Child { get; }
        public Tween<T> Tween { get; }
        public AnimationSpec Spec { get; }
        public StyleValueApplier<T> Apply { get; }
        public Action? OnEnd { get; }

        internal override WidgetNode CreateNode() => new AnimatedStyleNode<T>(this);
    }
}
