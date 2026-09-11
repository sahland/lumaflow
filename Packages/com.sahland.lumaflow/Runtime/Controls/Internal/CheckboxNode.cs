#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class CheckboxNode : WidgetNode {
        private UnityEngine.UIElements.Toggle? _checkbox;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _fieldErrorSubscription;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public CheckboxNode(Checkbox widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Checkbox)Widget;
            _checkbox = new UnityEngine.UIElements.Toggle(widget.Label) {
                value = widget.Value.Value
            };
            _checkbox.AddToClassList("lumaflow-checkbox");
            _checkbox.SetEnabled(widget.Enabled);
            ApplyStyle();
            return _checkbox;
        }

        protected override void OnMounted() {
            var widget = (Checkbox)Widget;
            var checkbox = _checkbox ?? throw new System.InvalidOperationException("Checkbox was not created.");
            checkbox.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => checkbox.UnregisterValueChangedCallback(OnValueChanged));
            checkbox.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            checkbox.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            checkbox.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            checkbox.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            checkbox.RegisterCallback<FocusInEvent>(HandleFocusIn);
            checkbox.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterInteractionCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
            // UI Toolkit creates the visual input subtree during mount, not during
            // Toggle construction. Apply again once it is available.
            ApplyStyle();
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Checkbox checkbox) return false;
            var previous = (Checkbox)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, checkbox.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, checkbox.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, checkbox.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();

            UpdateWidget(checkbox);
            _checkbox!.label = checkbox.Label ?? string.Empty;
            _checkbox.SetEnabled(checkbox.Enabled);
            _checkbox.SetValueWithoutNotify(checkbox.Value.Value);
            if (valueChanged) BindValue(checkbox);
            if (focusChanged) BindFocus(checkbox);
            if (fieldChanged) BindField(checkbox);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyStyle();
                return true;
            });
            return true;
        }

        internal void HandleValueChanged(bool value) {
            var widget = (Checkbox)Widget;
            if (!IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> changeEvent) {
            HandleValueChanged(changeEvent.newValue);
            ApplyStyle();
        }

        private void UpdateNativeValue(bool value) {
            _checkbox!.SetValueWithoutNotify(value);
            ApplyStyle();
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Checkbox)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                role: SemanticsRole.Toggle,
                enabled: widget.Enabled,
                checkedValue: widget.Value.Value,
                onTap: () => HandleValueChanged(!widget.Value.Value),
                onAccessibilityFocusChanged: focused => { if (focused) _checkbox?.Focus(); });
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) =>
            ApplyStyle();

        private WidgetStates ResolveStates() {
            var widget = (Checkbox)Widget;
            var states = WidgetStates.None;
            if (!widget.Enabled) states |= WidgetStates.Disabled;
            if (widget.Value.Value) states |= WidgetStates.Selected;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            if (!string.IsNullOrEmpty(widget.Field?.ErrorText.Value)) states |= WidgetStates.Error;
            return states;
        }

        private void ApplyStyle() =>
            InputThemeStyleMapper.ApplyCheckbox(_checkbox!, Context.Theme, ((Checkbox)Widget).Style, ResolveStates(), Context.TextScaler);

        private void HandlePointerEnter(PointerEnterEvent _) { _isHovered = true; ApplyStyle(); }
        private void HandlePointerLeave(PointerLeaveEvent _) { _isHovered = false; ApplyStyle(); }
        private void HandlePointerDown(PointerDownEvent _) { _isPressed = true; ApplyStyle(); }
        private void HandlePointerUp(PointerUpEvent _) { _isPressed = false; ApplyStyle(); }
        private void HandleFocusIn(FocusInEvent _) { _isFocused = true; ApplyStyle(); }
        private void HandleFocusOut(FocusOutEvent _) { _isFocused = false; ApplyStyle(); }

        private void BindValue(Checkbox widget) => _valueSubscription = widget.Value.Subscribe(UpdateNativeValue);
        private void BindFocus(Checkbox widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _checkbox!, Context.FocusTraversal);
        }
        private void BindField(Checkbox widget) {
            if (widget.Field is not { } field) return;
            _fieldFocusBinding = field.AttachFocus(_checkbox!.Focus);
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
            _checkbox!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _checkbox.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _checkbox.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _checkbox.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _checkbox.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _checkbox.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
