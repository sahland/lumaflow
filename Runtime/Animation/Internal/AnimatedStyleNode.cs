#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class AnimatedStyleNode<T> : WidgetNode {
        private readonly ImplicitAnimation<T> _animation = new();
        private WidgetNode? _currentChild;
        private IVisualElementScheduledItem? _ticker;
        private T _currentValue = default!;
        private bool _disableAnimations;

        public AnimatedStyleNode(AnimatedStyle<T> widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) => new();

        protected override void OnMounted() {
            var widget = (AnimatedStyle<T>)Widget;
            _disableAnimations = ReadDisableAnimations();
            _currentChild = MountChild(widget.Child, Element);
            var hasDistance = !EqualityComparer<T>.Default.Equals(widget.Tween.Begin, widget.Tween.End);
            var completed = _animation.Start(
                widget.Tween.Begin,
                widget.Tween.End,
                widget.Tween,
                widget.Spec,
                NowSeconds(),
                _disableAnimations);
            ApplyCurrent(widget);
            if (_animation.IsRunning) StartTicker();
            else if (completed && hasDistance) widget.OnEnd?.Invoke();
            Bindings.Add(StopTicker);
            Bindings.Add(_animation.Cancel);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not AnimatedStyle<T> next) return false;
            var previous = (AnimatedStyle<T>)Widget;
            ReconcileSingleChild(ref _currentChild, next.Child, Element);
            var targetChanged = !EqualityComparer<T>.Default.Equals(_animation.Target, next.Tween.End);
            var timingChanged = previous.Spec != next.Spec;
            var interpolationChanged = previous.Tween.GetType() != next.Tween.GetType();
            UpdateWidget(next);
            _disableAnimations = ReadDisableAnimations();
            if (targetChanged || timingChanged || interpolationChanged) Restart(next);
            else ApplyCurrent(next);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            if (aspect != InheritedAspect.MediaQuery) return;
            var disabled = ReadDisableAnimations();
            if (_disableAnimations == disabled) return;
            _disableAnimations = disabled;
            var widget = (AnimatedStyle<T>)Widget;
            if (!disabled
                || widget.Spec.Behavior == AnimationBehavior.Preserve
                || !_animation.SnapToEnd()) return;
            StopTicker();
            ApplyCurrent(widget);
            widget.OnEnd?.Invoke();
        }

        private void Restart(AnimatedStyle<T> widget) {
            StopTicker();
            var from = _currentValue;
            var changed = !EqualityComparer<T>.Default.Equals(from, widget.Tween.End);
            var completed = _animation.Start(
                from,
                widget.Tween.End,
                widget.Tween,
                widget.Spec,
                NowSeconds(),
                _disableAnimations);
            ApplyCurrent(widget);
            if (_animation.IsRunning) StartTicker();
            else if (completed && changed) widget.OnEnd?.Invoke();
        }

        private void Tick() => TickAt(NowSeconds());

        internal void TickAt(double nowSeconds) {
            if (!_animation.IsRunning) return;
            var sample = _animation.Sample(nowSeconds);
            ApplyCurrent((AnimatedStyle<T>)Widget);
            if (!sample.Completed) return;
            StopTicker();
            ((AnimatedStyle<T>)Widget).OnEnd?.Invoke();
        }

        private void ApplyCurrent(AnimatedStyle<T> widget) {
            _currentValue = _animation.Current;
            widget.Apply(Element, _currentValue);
        }

        private void StartTicker() {
            if (_ticker is not null) return;
            _ticker = Element.schedule.Execute(Tick).Every(16L);
        }

        private void StopTicker() {
            _ticker?.Pause();
            _ticker = null;
        }

        private bool ReadDisableAnimations() => Context.MediaQuery.DisableAnimations;

        private static double NowSeconds() => Time.realtimeSinceStartupAsDouble;

        internal double AnimationStartedAt => _animation.StartedAt;
        internal T CurrentValue => _currentValue;
        internal bool IsAnimating => _animation.IsRunning;
    }
}
