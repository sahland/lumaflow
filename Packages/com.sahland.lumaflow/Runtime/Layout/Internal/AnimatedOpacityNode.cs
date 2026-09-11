#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class AnimatedOpacityNode : WidgetNode {
        private WidgetNode? _currentChild;
        private IDisposable? _valueSubscription;
        private readonly ImplicitAnimation<float> _animation = new();
        private IVisualElementScheduledItem? _ticker;
        private float _currentOpacity;
        private bool _disableAnimations;

        public AnimatedOpacityNode(AnimatedOpacity widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            _currentOpacity = ((AnimatedOpacity)Widget).Value.Value;
            element.style.opacity = _currentOpacity;
            return element;
        }

        protected override void OnMounted() {
            var widget = (AnimatedOpacity)Widget;
            _disableAnimations = ReadDisableAnimations();
            _currentChild = MountChild(widget.Child, Element);
            BindValue(widget);
            Bindings.Add(ReleaseValue);
            Bindings.Add(StopTicker);
            Bindings.Add(_animation.Cancel);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not AnimatedOpacity opacity) return false;
            var previous = (AnimatedOpacity)Widget;
            ReconcileSingleChild(ref _currentChild, opacity.Child, Element);
            if (!ReferenceEquals(previous.Value, opacity.Value)) ReleaseValue();
            UpdateWidget(opacity);
            if (!ReferenceEquals(previous.Value, opacity.Value)) BindValue(opacity);
            _disableAnimations = ReadDisableAnimations();
            if (!ReferenceEquals(previous.Value, opacity.Value)
                || previous.Spec != opacity.Spec
                || !UnityEngine.Mathf.Approximately(_animation.Target, opacity.Value.Value)) {
                AnimateTo(opacity.Value.Value);
            }
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            if (aspect != InheritedAspect.MediaQuery) return;
            var disabled = ReadDisableAnimations();
            if (_disableAnimations == disabled) return;
            _disableAnimations = disabled;
            var widget = (AnimatedOpacity)Widget;
            if (!disabled
                || widget.Behavior == AnimationBehavior.Preserve
                || !_animation.SnapToEnd()) return;
            StopTicker();
            ApplyCurrent();
            widget.OnEnd?.Invoke();
        }

        private void AnimateTo(float targetOpacity) {
            AnimatedOpacity.ValidateOpacity(targetOpacity, "value");
            StopTicker();
            var widget = (AnimatedOpacity)Widget;
            var changed = !UnityEngine.Mathf.Approximately(_currentOpacity, targetOpacity);
            var completed = _animation.Start(
                _currentOpacity,
                targetOpacity,
                new FloatTween(_currentOpacity, targetOpacity),
                widget.Spec,
                NowSeconds(),
                _disableAnimations);
            ApplyCurrent();
            if (_animation.IsRunning) StartTicker();
            else if (completed && changed) widget.OnEnd?.Invoke();
        }

        private void Tick() => TickAt(NowSeconds());

        internal void TickAt(double nowSeconds) {
            if (!_animation.IsRunning) return;
            var sample = _animation.Sample(nowSeconds);
            ApplyCurrent();
            if (!sample.Completed) return;
            StopTicker();
            ((AnimatedOpacity)Widget).OnEnd?.Invoke();
        }

        private void StartTicker() {
            if (_ticker is not null) return;
            _ticker = Element.schedule.Execute(Tick).Every(16L);
        }

        private void StopTicker() {
            _ticker?.Pause();
            _ticker = null;
        }

        private void ApplyCurrent() {
            _currentOpacity = _animation.Current;
            Element.style.opacity = _currentOpacity;
        }

        private bool ReadDisableAnimations() => Context.MediaQuery.DisableAnimations;

        private static double NowSeconds() => UnityEngine.Time.realtimeSinceStartupAsDouble;

        internal double AnimationStartedAt => _animation.StartedAt;
        internal float CurrentOpacity => _currentOpacity;
        internal bool IsAnimating => _animation.IsRunning;

        private void BindValue(AnimatedOpacity widget) =>
            _valueSubscription = widget.Value.Subscribe(AnimateTo);

        private void ReleaseValue() {
            _valueSubscription?.Dispose();
            _valueSubscription = null;
        }
    }

}
