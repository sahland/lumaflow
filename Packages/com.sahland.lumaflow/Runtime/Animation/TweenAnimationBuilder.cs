#nullable enable

using System;

namespace LumaFlow {
    public delegate Widget TweenWidgetBuilder<T>(T value, Widget? child);

    /// <summary>
    /// Rebuilds one local subtree with values produced by an implicitly owned tween.
    /// Retargeting starts from the current displayed value rather than the old begin value.
    /// </summary>
    public sealed class TweenAnimationBuilder<T> : Widget {
        public TweenAnimationBuilder(
            Tween<T> tween,
            TimeSpan duration,
            TweenWidgetBuilder<T> builder,
            Curve? curve = null,
            Action? onEnd = null,
            Widget? child = null,
            AnimationBehavior behavior = AnimationBehavior.Normal)
            : this(tween, new AnimationSpec(duration, curve, behavior), builder, onEnd, child) {
        }

        public TweenAnimationBuilder(
            Tween<T> tween,
            TimeSpan duration,
            Func<T, Widget> builder,
            Curve? curve = null,
            Action? onEnd = null,
            AnimationBehavior behavior = AnimationBehavior.Normal)
            : this(
                tween,
                new AnimationSpec(duration, curve, behavior),
                (value, _) => builder(value),
                onEnd,
                child: null) {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
        }

        public TweenAnimationBuilder(
            Tween<T> tween,
            AnimationSpec spec,
            TweenWidgetBuilder<T> builder,
            Action? onEnd = null,
            Widget? child = null) {
            Tween = tween ?? throw new ArgumentNullException(nameof(tween));
            if (spec.Curve is null || spec.Duration <= TimeSpan.Zero) {
                throw new ArgumentException("AnimationSpec must be initialized with a positive duration.", nameof(spec));
            }
            Spec = spec;
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            OnEnd = onEnd;
            Child = child;
        }

        public Tween<T> Tween { get; }
        public AnimationSpec Spec { get; }
        public TweenWidgetBuilder<T> Builder { get; }
        public Action? OnEnd { get; }
        public Widget? Child { get; }

        internal override WidgetNode CreateNode() => new TweenAnimationBuilderNode<T>(this);
    }
}
