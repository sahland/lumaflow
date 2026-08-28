#nullable enable

using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class SliderNode : WidgetNode {
        private UnityEngine.UIElements.Slider? _slider;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _fieldErrorSubscription;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;
        private bool _isInteracting;

        public SliderNode(Slider widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Slider)Widget;
            _slider = new UnityEngine.UIElements.Slider(widget.Label, widget.Min, widget.Max) {
                value = widget.Value.Value,
                // ActiveTrackColor is part of LumaFlow's Slider contract, so
                // request Unity's native fill element on every mounted slider.
                fill = true
            };
            _slider.SetEnabled(widget.Enabled);
            ApplyStyle();
            return _slider;
        }

        protected override void OnMounted() {
            var widget = (Slider)Widget;
            var slider = _slider ?? throw new InvalidOperationException("Slider was not created.");
            slider.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => slider.UnregisterValueChangedCallback(OnValueChanged));
            slider.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            slider.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            slider.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            slider.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            slider.RegisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOut);
            slider.RegisterCallback<FocusInEvent>(HandleFocusIn);
            slider.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterInteractionCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
            ApplyStyle();
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Slider slider) return false;
            var previous = (Slider)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, slider.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, slider.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, slider.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();

            UpdateWidget(slider);
            _slider!.label = slider.Label ?? string.Empty;
            _slider.lowValue = slider.Min;
            _slider.highValue = slider.Max;
            _slider.SetEnabled(slider.Enabled);
            _slider.SetValueWithoutNotify(slider.Value.Value);
            if (valueChanged) BindValue(slider);
            if (focusChanged) BindFocus(slider);
            if (fieldChanged) BindField(slider);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyStyle();
                return true;
            });
            return true;
        }

        internal void HandleValueChanged(float value) {
            var widget = (Slider)Widget;
            if (!IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private void OnValueChanged(ChangeEvent<float> changeEvent) {
            HandleNativeValueChanged(changeEvent.newValue);
        }

        internal void HandleNativeValueChanged(float value) {
            if (_isInteracting) {
                HandleValueChanged(value);
                return;
            }

            HandleInteractionStart();
            Exception? changeFailure = null;
            Exception? endFailure = null;
            try {
                HandleValueChanged(value);
            } catch (Exception exception) {
                changeFailure = exception;
            } finally {
                try {
                    HandleInteractionEnd();
                } catch (Exception exception) {
                    endFailure = exception;
                }
            }

            if (changeFailure is not null && endFailure is not null) {
                throw new AggregateException(
                    "The slider value change and onChangeEnd callback both failed.",
                    changeFailure,
                    endFailure);
            }
            if (changeFailure is not null) ExceptionDispatchInfo.Capture(changeFailure).Throw();
            if (endFailure is not null) ExceptionDispatchInfo.Capture(endFailure).Throw();
        }

        internal bool HandleInteractionStart() {
            var widget = (Slider)Widget;
            if (!IsMounted || !widget.Enabled || _isInteracting) return false;
            _isInteracting = true;
            try {
                widget.OnChangeStart?.Invoke(widget.Value.Value);
            } catch {
                _isInteracting = false;
                throw;
            }
            return true;
        }

        internal bool HandleInteractionEnd() {
            if (!_isInteracting) return false;
            _isInteracting = false;
            var widget = (Slider)Widget;
            if (!IsMounted || !widget.Enabled) return false;
            widget.OnChangeEnd?.Invoke(widget.Value.Value);
            return true;
        }

        private void UpdateNativeValue(float value) {
            _slider!.SetValueWithoutNotify(value);
            ApplyStyle();
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Slider)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                value: widget.Value.Value.ToString(CultureInfo.InvariantCulture),
                role: SemanticsRole.Slider,
                enabled: widget.Enabled,
                onIncrease: () => AdjustSemanticsValue(1f),
                onDecrease: () => AdjustSemanticsValue(-1f),
                onAccessibilityFocusChanged: focused => { if (focused) _slider?.Focus(); });
        }

        private void AdjustSemanticsValue(float direction) {
            var widget = (Slider)Widget;
            var step = (widget.Max - widget.Min) / 10f;
            HandleNativeValueChanged(UnityEngine.Mathf.Clamp(
                widget.Value.Value + step * direction,
                widget.Min,
                widget.Max));
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) =>
            ApplyStyle();

        private WidgetStates ResolveStates() {
            var states = WidgetStates.None;
            if (!((Slider)Widget).Enabled) states |= WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed | WidgetStates.Dragged;
            if (!string.IsNullOrEmpty(((Slider)Widget).Field?.ErrorText.Value)) states |= WidgetStates.Error;
            return states;
        }
        private void ApplyStyle() => InputThemeStyleMapper.ApplySlider(_slider!, Context.Theme, ((Slider)Widget).Style, ResolveStates(), Context.TextScaler);
        private void HandlePointerEnter(PointerEnterEvent _) { _isHovered = true; ApplyStyle(); }
        private void HandlePointerLeave(PointerLeaveEvent _) { _isHovered = false; ApplyStyle(); }
        private void HandlePointerDown(PointerDownEvent _) {
            if (!HandleInteractionStart()) return;
            _isPressed = true;
            ApplyStyle();
        }
        private void HandlePointerUp(PointerUpEvent _) { FinishPointerInteraction(); }
        private void HandlePointerCaptureOut(PointerCaptureOutEvent _) { FinishPointerInteraction(); }
        private void HandleFocusIn(FocusInEvent _) { _isFocused = true; ApplyStyle(); }
        private void HandleFocusOut(FocusOutEvent _) { _isFocused = false; ApplyStyle(); }
        private void BindValue(Slider widget) => _valueSubscription = widget.Value.Subscribe(UpdateNativeValue);
        private void BindFocus(Slider widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _slider!, Context.FocusTraversal);
        }
        private void BindField(Slider widget) {
            if (widget.Field is not { } field) return;
            _fieldFocusBinding = field.AttachFocus(_slider!.Focus);
            _fieldErrorSubscription = field.ErrorText.Subscribe(_ => ApplyStyle());
        }
        private void ReleaseValueBinding() { _valueSubscription?.Dispose(); _valueSubscription = null; }
        private void ReleaseFocusBinding() { _focusBinding?.Dispose(); _focusBinding = null; }
        private void ReleaseFieldBinding() {
            _fieldErrorSubscription?.Dispose();
            _fieldErrorSubscription = null;
            _fieldFocusBinding?.Dispose();
            _fieldFocusBinding = null;
        }
        private void ReleaseDynamicBindings() {
            _isInteracting = false;
            _isPressed = false;
            ReleaseFieldBinding();
            ReleaseFocusBinding();
            ReleaseValueBinding();
        }
        private void FinishPointerInteraction() {
            if (!_isInteracting) return;
            _isPressed = false;
            ApplyStyle();
            HandleInteractionEnd();
        }
        private void UnregisterInteractionCallbacks() {
            _slider!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _slider.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _slider.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _slider.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _slider.UnregisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOut);
            _slider.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _slider.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
