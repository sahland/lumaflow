#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class RadioNode<T> : WidgetNode {
        private RadioButton? _radio;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _fieldErrorSubscription;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public RadioNode(Radio<T> widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Radio<T>)Widget;
            _radio = new RadioButton(widget.Label) {
                value = IsSelected(widget.SelectedValue.Value)
            };
            _radio.AddToClassList("lumaflow-radio");
            _radio.SetEnabled(widget.Enabled);
            ApplyStyle();
            return _radio;
        }

        protected override void OnMounted() {
            var widget = (Radio<T>)Widget;
            var radio = _radio ?? throw new InvalidOperationException("Radio was not created.");
            radio.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => radio.UnregisterValueChangedCallback(OnValueChanged));
            radio.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            radio.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            radio.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            radio.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            radio.RegisterCallback<FocusInEvent>(HandleFocusIn);
            radio.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterInteractionCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
            // The radio indicator is created by UI Toolkit while mounting.
            ApplyStyle();
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Radio<T> radio) return false;
            var previous = (Radio<T>)Widget;
            var valueChanged = !ReferenceEquals(previous.SelectedValue, radio.SelectedValue);
            var focusChanged = !ReferenceEquals(previous.FocusNode, radio.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, radio.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();

            UpdateWidget(radio);
            _radio!.label = radio.Label ?? string.Empty;
            _radio.SetEnabled(radio.Enabled);
            _radio.SetValueWithoutNotify(IsSelected(radio.SelectedValue.Value));
            if (valueChanged) BindValue(radio);
            if (focusChanged) BindFocus(radio);
            if (fieldChanged) BindField(radio);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyStyle();
                return true;
            });
            return true;
        }

        internal void HandleValueChanged(bool isSelected) {
            var widget = (Radio<T>)Widget;
            if (!isSelected || !IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.SelectedValue, widget.Value, widget.OnChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> changeEvent) {
            HandleValueChanged(changeEvent.newValue);
            ApplyStyle();
        }

        private void UpdateNativeValue(T selectedValue) {
            _radio!.SetValueWithoutNotify(IsSelected(selectedValue));
            ApplyStyle();
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Radio<T>)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                role: SemanticsRole.Toggle,
                enabled: widget.Enabled,
                selected: IsSelected(widget.SelectedValue.Value),
                onSelect: () => HandleValueChanged(true),
                onAccessibilityFocusChanged: focused => { if (focused) _radio?.Focus(); });
        }

        private bool IsSelected(T selectedValue) {
            return EqualityComparer<T>.Default.Equals(((Radio<T>)Widget).Value, selectedValue);
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) =>
            ApplyStyle();

        private WidgetStates ResolveStates() {
            var widget = (Radio<T>)Widget;
            var states = WidgetStates.None;
            if (!widget.Enabled) states |= WidgetStates.Disabled;
            if (IsSelected(widget.SelectedValue.Value)) states |= WidgetStates.Selected;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            if (!string.IsNullOrEmpty(widget.Field?.ErrorText.Value)) states |= WidgetStates.Error;
            return states;
        }

        private void ApplyStyle() =>
            InputThemeStyleMapper.ApplyRadio(_radio!, Context.Theme, ((Radio<T>)Widget).Style, ResolveStates(), Context.TextScaler);

        private void HandlePointerEnter(PointerEnterEvent _) { _isHovered = true; ApplyStyle(); }
        private void HandlePointerLeave(PointerLeaveEvent _) { _isHovered = false; ApplyStyle(); }
        private void HandlePointerDown(PointerDownEvent _) { _isPressed = true; ApplyStyle(); }
        private void HandlePointerUp(PointerUpEvent _) { _isPressed = false; ApplyStyle(); }
        private void HandleFocusIn(FocusInEvent _) { _isFocused = true; ApplyStyle(); }
        private void HandleFocusOut(FocusOutEvent _) { _isFocused = false; ApplyStyle(); }

        private void BindValue(Radio<T> widget) => _valueSubscription = widget.SelectedValue.Subscribe(UpdateNativeValue);
        private void BindFocus(Radio<T> widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _radio!, Context.FocusTraversal);
        }
        private void BindField(Radio<T> widget) {
            if (widget.Field is not { } field) return;
            _fieldFocusBinding = field.AttachFocus(_radio!.Focus);
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
        private void ReleaseDynamicBindings() { ReleaseFieldBinding(); ReleaseFocusBinding(); ReleaseValueBinding(); }
        private void UnregisterInteractionCallbacks() {
            _radio!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _radio.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _radio.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _radio.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _radio.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _radio.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
