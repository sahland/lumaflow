#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class DropdownNode<T> : WidgetNode {
        private PopupField<T>? _dropdown;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _fieldErrorSubscription;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public DropdownNode(Dropdown<T> widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Dropdown<T>)Widget;
            _dropdown = new PopupField<T>(
                widget.Label,
                new List<T>(widget.Items),
                widget.Value.Value,
                widget.LabelBuilder,
                widget.LabelBuilder);
            _dropdown.SetEnabled(widget.Enabled);
            ApplyStyle();
            return _dropdown;
        }

        protected override void OnMounted() {
            var widget = (Dropdown<T>)Widget;
            var dropdown = _dropdown ?? throw new System.InvalidOperationException("Dropdown was not created.");
            dropdown.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => dropdown.UnregisterValueChangedCallback(OnValueChanged));
            dropdown.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            dropdown.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            dropdown.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            dropdown.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            dropdown.RegisterCallback<FocusInEvent>(HandleFocusIn);
            dropdown.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterInteractionCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
            ApplyStyle();
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Dropdown<T> dropdown) return false;
            var previous = (Dropdown<T>)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, dropdown.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, dropdown.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, dropdown.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();
            UpdateWidget(dropdown);
            _dropdown!.label = dropdown.Label ?? string.Empty;
            _dropdown.choices = new List<T>(dropdown.Items);
            _dropdown.formatListItemCallback = dropdown.LabelBuilder;
            _dropdown.formatSelectedValueCallback = dropdown.LabelBuilder;
            _dropdown.SetEnabled(dropdown.Enabled);
            _dropdown.SetValueWithoutNotify(dropdown.Value.Value);
            if (valueChanged) BindValue(dropdown);
            if (focusChanged) BindFocus(dropdown);
            if (fieldChanged) BindField(dropdown);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyStyle();
                return true;
            });
            return true;
        }

        internal void HandleValueChanged(T value) {
            var widget = (Dropdown<T>)Widget;
            if (!IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private void OnValueChanged(ChangeEvent<T> changeEvent) {
            HandleValueChanged(changeEvent.newValue);
        }

        private void UpdateNativeValue(T value) {
            _dropdown!.SetValueWithoutNotify(value);
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Dropdown<T>)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                value: widget.LabelBuilder(widget.Value.Value),
                role: SemanticsRole.Dropdown,
                enabled: widget.Enabled,
                onAccessibilityFocusChanged: focused => { if (focused) _dropdown?.Focus(); });
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => ApplyStyle();

        internal void SetHovered(bool value) { _isHovered = value; ApplyStyle(); }
        internal void SetPressed(bool value) { _isPressed = value; ApplyStyle(); }

        private WidgetStates ResolveStates() {
            var widget = (Dropdown<T>)Widget;
            var states = WidgetStates.None;
            if (!widget.Enabled) states |= WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            if (!string.IsNullOrEmpty(widget.Field?.ErrorText.Value)) states |= WidgetStates.Error;
            return states;
        }

        private void ApplyStyle() {
            var widget = (Dropdown<T>)Widget;
            var style = widget.Style ?? Context.Theme?.DropdownTheme.Style;
            InputThemeStyleMapper.ApplyDropdown(_dropdown!, Context.Theme, style, ResolveStates(), Context.TextScaler);
        }

        private void HandlePointerEnter(PointerEnterEvent _) => SetHovered(true);
        private void HandlePointerLeave(PointerLeaveEvent _) => SetHovered(false);
        private void HandlePointerDown(PointerDownEvent _) => SetPressed(true);
        private void HandlePointerUp(PointerUpEvent _) => SetPressed(false);
        private void HandleFocusIn(FocusInEvent _) { _isFocused = true; ApplyStyle(); }
        private void HandleFocusOut(FocusOutEvent _) { _isFocused = false; ApplyStyle(); }

        private void BindValue(Dropdown<T> widget) => _valueSubscription = widget.Value.Subscribe(UpdateNativeValue);
        private void BindFocus(Dropdown<T> widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _dropdown!, Context.FocusTraversal);
        }
        private void BindField(Dropdown<T> widget) {
            if (widget.Field is not { } field) return;
            _fieldFocusBinding = field.AttachFocus(_dropdown!.Focus);
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
            _dropdown!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _dropdown.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _dropdown.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _dropdown.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _dropdown.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _dropdown.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
