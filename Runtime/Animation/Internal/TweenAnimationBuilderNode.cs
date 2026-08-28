#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class TweenAnimationBuilderNode<T> : WidgetNode, ITransparentWidgetNode {
        private readonly ImplicitAnimation<T> _animation = new();
        private WidgetNode? _currentChild;
        private IVisualElementScheduledItem? _ticker;
        private T _currentValue = default!;
        private bool _disableAnimations;

        public TweenAnimationBuilderNode(TweenAnimationBuilder<T> widget) : base(widget) { }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (TweenAnimationBuilder<T>)Widget;
            _disableAnimations = ReadDisableAnimations();
            var hasDistance = !EqualityComparer<T>.Default.Equals(widget.Tween.Begin, widget.Tween.End);
            var completed = _animation.Start(
                widget.Tween.Begin,
                widget.Tween.End,
                widget.Tween,
                widget.Spec,
                NowSeconds(),
                _disableAnimations);
            _currentValue = _animation.Current;
            Rebuild(widget);
            if (_animation.IsRunning) StartTicker();
            else if (completed && hasDistance) widget.OnEnd?.Invoke();
            Bindings.Add(StopTicker);
            Bindings.Add(_animation.Cancel);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TweenAnimationBuilder<T> next) return false;
            var previous = (TweenAnimationBuilder<T>)Widget;
            var targetChanged = !EqualityComparer<T>.Default.Equals(_animation.Target, next.Tween.End);
            var timingChanged = previous.Spec != next.Spec;
            var interpolationChanged = previous.Tween.GetType() != next.Tween.GetType();
            UpdateWidget(next);
            _disableAnimations = ReadDisableAnimations();

            if (targetChanged || timingChanged || interpolationChanged) {
                Restart(next);
            } else {
                Rebuild(next);
            }
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            if (aspect != InheritedAspect.MediaQuery) return;
            var disabled = ReadDisableAnimations();
            if (_disableAnimations == disabled) return;
            _disableAnimations = disabled;
            var widget = (TweenAnimationBuilder<T>)Widget;
            if (!disabled
                || widget.Spec.Behavior == AnimationBehavior.Preserve
                || !_animation.SnapToEnd()) return;
            StopTicker();
            _currentValue = _animation.Current;
            Rebuild(widget);
            widget.OnEnd?.Invoke();
        }

        private void Restart(TweenAnimationBuilder<T> widget) {
            StopTicker();
            var from = _currentValue;
            var hasDistance = !EqualityComparer<T>.Default.Equals(from, widget.Tween.End);
            var completed = _animation.Start(
                from,
                widget.Tween.End,
                widget.Tween,
                widget.Spec,
                NowSeconds(),
                _disableAnimations);
            _currentValue = _animation.Current;
            try {
                Rebuild(widget);
            } catch {
                _animation.Cancel();
                throw;
            }
            if (_animation.IsRunning) StartTicker();
            else if (completed && hasDistance) widget.OnEnd?.Invoke();
        }

        private void Tick() => TickAt(NowSeconds());

        internal void TickAt(double nowSeconds) {
            if (!_animation.IsRunning) return;
            var sample = _animation.Sample(nowSeconds);
            _currentValue = sample.Value;
            try {
                Rebuild((TweenAnimationBuilder<T>)Widget);
            } catch {
                StopTicker();
                _animation.Cancel();
                throw;
            }
            if (!sample.Completed) return;
            StopTicker();
            ((TweenAnimationBuilder<T>)Widget).OnEnd?.Invoke();
        }

        internal double AnimationStartedAt => _animation.StartedAt;
        internal T CurrentValue => _currentValue;
        internal bool IsAnimating => _animation.IsRunning;

        internal override void Reassemble() => Rebuild((TweenAnimationBuilder<T>)Widget);

        private void Rebuild(TweenAnimationBuilder<T> widget) {
            var child = widget.Builder(_currentValue, widget.Child)
                ?? throw new InvalidOperationException("TweenAnimationBuilder builder cannot return null.");
            ReconcileSingleChild(ref _currentChild, child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        private bool ReadDisableAnimations() => Context.MediaQuery.DisableAnimations;

        private void StartTicker() {
            if (_ticker is not null) return;
            _ticker = NativeParent.schedule.Execute(Tick).Every(16L);
        }

        private void StopTicker() {
            _ticker?.Pause();
            _ticker = null;
        }

        private static double NowSeconds() => Time.realtimeSinceStartupAsDouble;
    }
}
