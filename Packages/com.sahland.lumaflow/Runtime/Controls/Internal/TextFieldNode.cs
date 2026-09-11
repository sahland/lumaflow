#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class TextFieldNode : WidgetNode {
        private UnityEngine.UIElements.TextField? _textField;
        private VisualElement? _root;
        private Label? _supportingLabel;
        private WidgetStates _appliedStates;
        private bool _hasStyleApplied;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldErrorSubscription;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _formRegistration;

        public TextFieldNode(TextField widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (TextField)Widget;
            _textField = new UnityEngine.UIElements.TextField(widget.Label) {
                value = widget.Value.Value,
                isPasswordField = widget.ObscureText,
                multiline = widget.Multiline
            };
            _textField.textEdition.placeholder = widget.Placeholder ?? string.Empty;
            _textField.SetEnabled(widget.Enabled);
            _textField.style.marginTop = 0f;
            _textField.style.marginRight = 0f;
            _textField.style.marginBottom = 0f;
            _textField.style.marginLeft = 0f;
            ApplyStyle(ResolveStates());

            var supportingText = GetErrorText(widget) ?? widget.HelperText;
            var requiresSupportingSlot = RequiresSupportingSlot(widget);
            if (requiresSupportingSlot) {
                _root = new VisualElement();
                _root.style.flexDirection = FlexDirection.Column;
                _root.style.flexShrink = 1f;
                _root.style.minWidth = 0f;
                _root.Add(_textField);
                _supportingLabel = new Label(supportingText ?? string.Empty);
                _root.Add(_supportingLabel);
                ApplySupportingStyle();
                _supportingLabel.style.display = string.IsNullOrEmpty(supportingText) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            return _root ?? _textField;
        }

        protected override void OnMounted() {
            var widget = (TextField)Widget;
            var textField = _textField ?? throw new InvalidOperationException("Text field was not created.");
            textField.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => textField.UnregisterValueChangedCallback(OnValueChanged));
            textField.RegisterCallback<FocusInEvent>(HandleFocusIn);
            textField.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            textField.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            textField.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            textField.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            textField.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            textField.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            textField.RegisterCallback<AttachToPanelEvent>(HandleAttachedToPanel);
            Bindings.Add(UnregisterCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
            // TextField creates its internal input hierarchy during construction.
            // Re-apply after mounting so the surface is assigned to the real
            // container even when the parent has not yet joined a UI panel.
            ApplyStyle(ResolveStates(), force: true);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TextField textField) return false;
            if (RequiresSupportingSlot(textField) != (_root is not null)) return false;

            var previous = (TextField)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, textField.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, textField.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, textField.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBindings();

            UpdateWidget(textField);
            _textField!.label = textField.Label ?? string.Empty;
            _textField.textEdition.placeholder = textField.Placeholder ?? string.Empty;
            _textField.isPasswordField = textField.ObscureText;
            _textField.multiline = textField.Multiline;
            _textField.SetEnabled(textField.Enabled);
            _textField.SetValueWithoutNotify(textField.Value.Value);

            if (valueChanged) BindValue(textField);
            if (focusChanged) BindFocus(textField);
            if (fieldChanged) BindField(textField);

            _hasStyleApplied = false;
            ReevaluateInheritedDependencies(() =>
            {
                RefreshValidationPresentation();
                return true;
            });
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            _hasStyleApplied = false;
            RefreshValidationPresentation();
        }

        internal void HandleValueChanged(string value) {
            var widget = (TextField)Widget;
            if (!IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        internal bool HandleSubmitted(KeyCode keyCode, Action? dispatchSubmitIntent = null) {
            var widget = (TextField)Widget;
            if (!IsMounted || !widget.Enabled || widget.Multiline) return false;
            if (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter) return false;
            widget.OnSubmitted?.Invoke(widget.Value.Value);
            dispatchSubmitIntent?.Invoke();
            return true;
        }

        private void OnValueChanged(ChangeEvent<string> changeEvent) {
            HandleValueChanged(changeEvent.newValue);
        }

        private void HandleKeyDown(KeyDownEvent keyEvent) {
            HandleSubmitted(keyEvent.keyCode, DispatchSubmitIntent);
        }

        private void DispatchSubmitIntent() {
            using var submitEvent = TextInputSubmitEvent.GetPooled();
            submitEvent.target = _textField;
            _textField!.SendEvent(submitEvent);
        }

        private void UpdateNativeValue(string value) {
            _textField!.SetValueWithoutNotify(value);
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (TextField)Widget;
            return new SemanticsProperties(
                label: widget.Label ?? widget.Placeholder,
                value: widget.ObscureText ? string.Empty : widget.Value.Value,
                hint: CurrentErrorText ?? widget.HelperText,
                role: SemanticsRole.TextField,
                enabled: widget.Enabled,
                allowsDirectInteraction: true,
                onAccessibilityFocusChanged: focused => { if (focused) _textField?.Focus(); });
        }

        private TextFieldStyle EffectiveStyle => ((TextField)Widget).Style ?? Context.Theme?.TextFieldTheme.Style ?? ThemeData.FallbackTextFieldTheme.Style;

        private string? CurrentErrorText => ((TextField)Widget).Field?.ErrorText.Value ?? ((TextField)Widget).ErrorText;

        private bool HasError => !string.IsNullOrEmpty(CurrentErrorText);

        private void HandleFocusIn(FocusInEvent _) {
            HandleFocusChanged(isFocused: true);
        }

        private void HandleFocusOut(FocusOutEvent _) {
            HandleFocusChanged(isFocused: false);
        }

        internal void HandleFocusChanged(bool isFocused) {
            if (!IsMounted) {
                return;
            }

            _isFocused = isFocused;
            ((TextField)Widget).FocusNode?.SetFocused(isFocused);
            ApplyStyle(ResolveStates());
        }

        internal void SetHovered(bool value) {
            _isHovered = value;
            ApplyStyle(ResolveStates());
        }

        internal void SetPressed(bool value) {
            _isPressed = value;
            ApplyStyle(ResolveStates());
        }

        private void HandlePointerEnter(PointerEnterEvent _) => SetHovered(true);
        private void HandlePointerLeave(PointerLeaveEvent _) => SetHovered(false);
        private void HandlePointerDown(PointerDownEvent _) => SetPressed(true);
        private void HandlePointerUp(PointerUpEvent _) => SetPressed(false);

        private void HandleAttachedToPanel(AttachToPanelEvent _) => ApplyStyle(ResolveStates(), force: true);

        private WidgetStates ResolveStates() {
            var widget = (TextField)Widget;
            var states = WidgetStates.None;
            if (!widget.Enabled) states |= WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            if (HasError) states |= WidgetStates.Error;
            return states;
        }

        private void ApplyStyle(WidgetStates states, bool force = false) {
            var style = EffectiveStyle;
            if (!force && _hasStyleApplied && _appliedStates == states) return;
            _hasStyleApplied = true;
            _appliedStates = states;
            var textField = _textField ?? throw new InvalidOperationException("Text field was not created.");
            // TextField's own public input USS class identifies the actual input
            // container. BaseField.inputUssClassName names an outer base wrapper.
            var input = textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName) ?? textField;
            var textInput = textField.Q<VisualElement>(UnityEngine.UIElements.TextField.textInputUssName);
            // UI Toolkit TextField has a root, an input container and its actual
            // text-input element. The public USS names above identify those exact
            // elements. Only the input container owns LumaFlow's visual surface;
            // styling all three creates a nested native rectangle.
            textField.style.backgroundColor = UnityEngine.Color.clear;
            textField.style.borderTopWidth = 0f;
            textField.style.borderRightWidth = 0f;
            textField.style.borderBottomWidth = 0f;
            textField.style.borderLeftWidth = 0f;
            if (textInput is not null && textInput != input) {
                textInput.style.backgroundColor = UnityEngine.Color.clear;
                textInput.style.borderTopWidth = 0f;
                textInput.style.borderRightWidth = 0f;
                textInput.style.borderBottomWidth = 0f;
                textInput.style.borderLeftWidth = 0f;
            }
            ClearStyledInput(input, textInput);
            textField.labelElement.style.color = StyleKeyword.Null;
            textField.labelElement.style.fontSize = StyleKeyword.Null;
            textField.labelElement.style.unityFontStyleAndWeight = StyleKeyword.Null;
            if (style.ResolveBackground(states) is { } background) {
                input.style.backgroundColor = background;
            }
            if (style.ResolveForeground(states) is { } foreground) {
                input.style.color = foreground;
                if (textInput is not null) textInput.style.color = foreground;
            }
            if (style.ResolveBorder(states) is { } border) {
                input.style.borderTopColor = border;
                input.style.borderRightColor = border;
                input.style.borderBottomColor = border;
                input.style.borderLeftColor = border;
                input.style.borderTopWidth = 1f;
                input.style.borderRightWidth = 1f;
                input.style.borderBottomWidth = 1f;
                input.style.borderLeftWidth = 1f;
            }
            if (style.ResolvePadding(states) is { } padding) PaddingStyleMapper.Apply(input, padding);
            if (style.ResolveShape(states) is { } shape) {
                input.style.borderTopLeftRadius = shape.TopLeft;
                input.style.borderTopRightRadius = shape.TopRight;
                input.style.borderBottomRightRadius = shape.BottomRight;
                input.style.borderBottomLeftRadius = shape.BottomLeft;
            }
            if (style.ResolveTypography(states) is { } typography) TextStyleMapper.Apply(input, typography, Context.TextScaler);
            if (style.LabelStyle is not null) TextStyleMapper.Apply(textField.labelElement, style.LabelStyle, Context.TextScaler);
        }

        private void ApplySupportingStyle() {
            var style = EffectiveStyle;
            var textStyle = style.SupportingStyle;
            _supportingLabel!.style.color = StyleKeyword.Null;
            _supportingLabel.style.fontSize = StyleKeyword.Null;
            _supportingLabel.style.unityFontStyleAndWeight = StyleKeyword.Null;
            if (textStyle is not null) TextStyleMapper.Apply(_supportingLabel!, textStyle, Context.TextScaler);
            if (HasError && style.Error?.Foreground is { } errorColor) _supportingLabel!.style.color = errorColor;
            _supportingLabel!.style.marginTop = 4f;
        }

        private void RefreshValidationPresentation() {
            var widget = (TextField)Widget;
            var supportingText = CurrentErrorText ?? widget.HelperText;
            if (_supportingLabel is null && !string.IsNullOrEmpty(supportingText)) {
                _supportingLabel = new Label();
                _root!.Add(_supportingLabel);
            }
            if (_supportingLabel is not null) {
                _supportingLabel.text = supportingText ?? string.Empty;
                _supportingLabel.style.display = string.IsNullOrEmpty(supportingText) ? DisplayStyle.None : DisplayStyle.Flex;
                ApplySupportingStyle();
            }
            ApplyStyle(ResolveStates(), force: true);
            RefreshSemantics();
        }

        private void UnregisterCallbacks() {
            var textField = _textField ?? throw new InvalidOperationException("Text field was not created.");
            textField.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            textField.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
            textField.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            textField.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            textField.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            textField.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            textField.UnregisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            textField.UnregisterCallback<AttachToPanelEvent>(HandleAttachedToPanel);
        }

        private static string? GetErrorText(TextField widget) =>
            widget.Field?.ErrorText.Value ?? widget.ErrorText;

        private static bool RequiresSupportingSlot(TextField widget) =>
            widget.Field is not null
            || !string.IsNullOrEmpty(GetErrorText(widget) ?? widget.HelperText);

        private void BindValue(TextField widget) {
            _valueSubscription = widget.Value.Subscribe(UpdateNativeValue);
        }

        private void BindFocus(TextField widget) {
            if (widget.FocusNode is { } focusNode) {
                _focusBinding = FocusNodeBinding.Attach(focusNode, _textField!, Context.FocusTraversal);
            }
        }

        private void BindField(TextField widget) {
            if (widget.Field is not { } field) return;
            _fieldErrorSubscription = field.ErrorText.Subscribe(_ => RefreshValidationPresentation());
            _fieldFocusBinding = field.AttachFocus(_textField!.Focus);
            if (Context.Form is { } form) _formRegistration = form.Register(field);
        }

        private void ReleaseValueBinding() {
            _valueSubscription?.Dispose();
            _valueSubscription = null;
        }

        private void ReleaseFocusBinding() {
            _focusBinding?.Dispose();
            _focusBinding = null;
        }

        private void ReleaseFieldBindings() {
            _formRegistration?.Dispose();
            _formRegistration = null;
            _fieldFocusBinding?.Dispose();
            _fieldFocusBinding = null;
            _fieldErrorSubscription?.Dispose();
            _fieldErrorSubscription = null;
        }

        private void ReleaseDynamicBindings() {
            ReleaseFieldBindings();
            ReleaseFocusBinding();
            ReleaseValueBinding();
        }

        private static void ClearStyledInput(VisualElement input, VisualElement? textInput) {
            input.style.backgroundColor = StyleKeyword.Null;
            input.style.color = StyleKeyword.Null;
            BorderStyleMapper.Clear(input);
            PaddingStyleMapper.Clear(input);
            input.style.borderTopLeftRadius = StyleKeyword.Null;
            input.style.borderTopRightRadius = StyleKeyword.Null;
            input.style.borderBottomRightRadius = StyleKeyword.Null;
            input.style.borderBottomLeftRadius = StyleKeyword.Null;
            input.style.fontSize = StyleKeyword.Null;
            input.style.unityFontStyleAndWeight = StyleKeyword.Null;
            if (textInput is null || textInput == input) return;
            textInput.style.color = StyleKeyword.Null;
            textInput.style.fontSize = StyleKeyword.Null;
            textInput.style.unityFontStyleAndWeight = StyleKeyword.Null;
        }
    }

}
