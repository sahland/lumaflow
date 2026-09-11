#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ToggleNode : WidgetNode {
        private UnityEngine.UIElements.Toggle? _toggle;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;

        public ToggleNode(Toggle widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Toggle)Widget;
            _toggle = new UnityEngine.UIElements.Toggle(widget.Label) {
                value = widget.Value.Value
            };
            _toggle.SetEnabled(widget.Enabled);
            InputThemeStyleMapper.ApplyToggle(_toggle, context.Theme, Context.TextScaler);
            return _toggle;
        }

        protected override void OnMounted() {
            var widget = (Toggle)Widget;
            var toggle = _toggle ?? throw new System.InvalidOperationException("Toggle was not created.");
            toggle.RegisterValueChangedCallback(OnValueChanged);
            Bindings.Add(() => toggle.UnregisterValueChangedCallback(OnValueChanged));
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Toggle toggle) return false;
            var previous = (Toggle)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, toggle.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, toggle.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, toggle.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();
            UpdateWidget(toggle);
            _toggle!.label = toggle.Label ?? string.Empty;
            _toggle.SetEnabled(toggle.Enabled);
            _toggle.SetValueWithoutNotify(toggle.Value.Value);
            if (valueChanged) BindValue(toggle);
            if (focusChanged) BindFocus(toggle);
            if (fieldChanged) BindField(toggle);
            ReevaluateInheritedDependencies(() =>
            {
                InputThemeStyleMapper.ApplyToggle(_toggle, Context.Theme, Context.TextScaler);
                return true;
            });
            return true;
        }

        internal void HandleValueChanged(bool value) {
            if (IsMounted) {
                ((Toggle)Widget).Value.Value = value;
            }
        }

        private void OnValueChanged(ChangeEvent<bool> changeEvent) {
            HandleValueChanged(changeEvent.newValue);
            InputThemeStyleMapper.ApplyToggle(_toggle!, Context.Theme, Context.TextScaler);
        }

        private void UpdateNativeValue(bool value) {
            _toggle!.SetValueWithoutNotify(value);
            InputThemeStyleMapper.ApplyToggle(_toggle, Context.Theme, Context.TextScaler);
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Toggle)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                role: SemanticsRole.Toggle,
                enabled: widget.Enabled,
                checkedValue: widget.Value.Value,
                onTap: () => HandleValueChanged(!widget.Value.Value),
                onAccessibilityFocusChanged: focused => { if (focused) _toggle?.Focus(); });
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) =>
            InputThemeStyleMapper.ApplyToggle(_toggle!, Context.Theme, Context.TextScaler);

        private void BindValue(Toggle widget) => _valueSubscription = widget.Value.Subscribe(UpdateNativeValue);
        private void BindFocus(Toggle widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _toggle!, Context.FocusTraversal);
        }
        private void BindField(Toggle widget) {
            if (widget.Field is { } field) _fieldFocusBinding = field.AttachFocus(_toggle!.Focus);
        }
        private void ReleaseValueBinding() { _valueSubscription?.Dispose(); _valueSubscription = null; }
        private void ReleaseFocusBinding() { _focusBinding?.Dispose(); _focusBinding = null; }
        private void ReleaseFieldBinding() { _fieldFocusBinding?.Dispose(); _fieldFocusBinding = null; }
        private void ReleaseDynamicBindings() { ReleaseFieldBinding(); ReleaseFocusBinding(); ReleaseValueBinding(); }
    }

    internal sealed class SwitchNode : WidgetNode {
        private VisualElement? _root;
        private VisualElement? _track;
        private VisualElement? _thumb;
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private IDisposable? _fieldFocusBinding;
        private IDisposable? _fieldErrorSubscription;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public SwitchNode(Switch widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Switch)Widget;
            _root = new VisualElement { focusable = widget.Enabled, tooltip = widget.Label ?? string.Empty };
            _root.style.flexDirection = FlexDirection.Row;
            _root.style.alignItems = UnityEngine.UIElements.Align.Center;
            _root.style.width = 40f;
            _root.style.height = 24f;
            _track = new VisualElement();
            _track.style.position = Position.Relative;
            _track.style.width = 38f;
            _track.style.height = 22f;
            _track.style.borderTopLeftRadius = 12f;
            _track.style.borderTopRightRadius = 12f;
            _track.style.borderBottomRightRadius = 12f;
            _track.style.borderBottomLeftRadius = 12f;
            _thumb = new VisualElement();
            _thumb.style.position = Position.Absolute;
            _thumb.style.top = 3f;
            _thumb.style.width = 16f;
            _thumb.style.height = 16f;
            _thumb.style.borderTopLeftRadius = 8f;
            _thumb.style.borderTopRightRadius = 8f;
            _thumb.style.borderBottomRightRadius = 8f;
            _thumb.style.borderBottomLeftRadius = 8f;
            _track.Add(_thumb);
            _root.Add(_track);
            _root.SetEnabled(widget.Enabled);
            ApplyValue(widget.Value.Value);
            return _root;
        }

        protected override void OnMounted() {
            var widget = (Switch)Widget;
            _root!.RegisterCallback<ClickEvent>(HandleClicked);
            _root.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            _root.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _root.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _root.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            _root.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            _root.RegisterCallback<FocusInEvent>(HandleFocusIn);
            _root.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(() => _root.UnregisterCallback<ClickEvent>(HandleClicked));
            Bindings.Add(() => _root.UnregisterCallback<KeyDownEvent>(HandleKeyDown));
            Bindings.Add(UnregisterInteractionCallbacks);
            BindValue(widget);
            BindFocus(widget);
            BindField(widget);
            Bindings.Add(ReleaseDynamicBindings);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Switch toggle) return false;
            var previous = (Switch)Widget;
            var valueChanged = !ReferenceEquals(previous.Value, toggle.Value);
            var focusChanged = !ReferenceEquals(previous.FocusNode, toggle.FocusNode);
            var fieldChanged = !ReferenceEquals(previous.Field, toggle.Field);
            if (valueChanged) ReleaseValueBinding();
            if (focusChanged) ReleaseFocusBinding();
            if (fieldChanged) ReleaseFieldBinding();
            UpdateWidget(toggle);
            _root!.tooltip = toggle.Label ?? string.Empty;
            _root.focusable = toggle.Enabled;
            _root.SetEnabled(toggle.Enabled);
            if (valueChanged) BindValue(toggle);
            if (focusChanged) BindFocus(toggle);
            if (fieldChanged) BindField(toggle);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyValue(toggle.Value.Value);
                return true;
            });
            return true;
        }

        private void HandleClicked(ClickEvent _) => ToggleValue();

        private void HandleKeyDown(KeyDownEvent keyEvent) {
            if (keyEvent.keyCode is KeyCode.Space or KeyCode.Return or KeyCode.KeypadEnter) {
                ToggleValue();
                keyEvent.StopPropagation();
            }
        }

        private void ToggleValue() {
            var widget = (Switch)Widget;
            HandleValueChanged(!widget.Value.Value);
        }

        internal void HandleValueChanged(bool value) {
            var widget = (Switch)Widget;
            if (!IsMounted || !widget.Enabled) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private void ApplyValue(bool value) {
            if (_track is null || _thumb is null) return;
            var states = ResolveStates(value);
            var theme = Context.Theme;
            var style = ((Switch)Widget).Style;
            var themedStyle = theme?.SwitchTheme.Style;
            var trackColor = style?.TrackColor?.Resolve(states)
                ?? themedStyle?.TrackColor?.Resolve(states)
                ?? (value ? theme?.Colors.Primary ?? Color.blue : theme?.Colors.SurfaceVariant ?? new Color(0.86f, 0.89f, 0.94f));
            var thumbColor = style?.ThumbColor?.Resolve(states)
                ?? themedStyle?.ThumbColor?.Resolve(states)
                ?? (value ? theme?.Colors.OnPrimary ?? Color.white : theme?.Colors.Surface ?? Color.white);
            var width = ResolveDimension(
                style?.Width?.Resolve(states) ?? themedStyle?.Width?.Resolve(states),
                40f,
                nameof(SwitchStyle.Width));
            var height = ResolveDimension(
                style?.Height?.Resolve(states) ?? themedStyle?.Height?.Resolve(states),
                24f,
                nameof(SwitchStyle.Height));
            if (width <= height) throw new ArgumentOutOfRangeException(nameof(SwitchStyle.Width), "Switch width must be greater than its height.");
            var trackWidth = width - 2f;
            var trackHeight = height - 2f;
            var thumbSize = height - 8f;
            _root!.style.width = width;
            _root.style.height = height;
            _track.style.width = trackWidth;
            _track.style.height = trackHeight;
            _track.style.borderTopLeftRadius = trackHeight / 2f;
            _track.style.borderTopRightRadius = trackHeight / 2f;
            _track.style.borderBottomRightRadius = trackHeight / 2f;
            _track.style.borderBottomLeftRadius = trackHeight / 2f;
            _track.style.backgroundColor = trackColor;
            _thumb.style.width = thumbSize;
            _thumb.style.height = thumbSize;
            _thumb.style.top = 3f;
            _thumb.style.borderTopLeftRadius = thumbSize / 2f;
            _thumb.style.borderTopRightRadius = thumbSize / 2f;
            _thumb.style.borderBottomRightRadius = thumbSize / 2f;
            _thumb.style.borderBottomLeftRadius = thumbSize / 2f;
            _thumb.style.backgroundColor = thumbColor;
            _thumb.style.left = value ? trackWidth - thumbSize - 3f : 3f;
            RefreshSemantics();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Switch)Widget;
            return new SemanticsProperties(
                label: widget.Label,
                role: SemanticsRole.Toggle,
                enabled: widget.Enabled,
                checkedValue: widget.Value.Value,
                onTap: ToggleValue,
                onAccessibilityFocusChanged: focused => { if (focused) _root?.Focus(); });
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) =>
            ApplyValue(((Switch)Widget).Value.Value);

        private WidgetStates ResolveStates(bool selected) {
            var states = selected ? WidgetStates.Selected : WidgetStates.None;
            if (!((Switch)Widget).Enabled) states |= WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            if (!string.IsNullOrEmpty(((Switch)Widget).Field?.ErrorText.Value)) states |= WidgetStates.Error;
            return states;
        }

        private static float ResolveDimension(float? value, float fallback, string propertyName) {
            var resolved = value ?? fallback;
            if (float.IsNaN(resolved) || float.IsInfinity(resolved) || resolved <= 8f) {
                throw new ArgumentOutOfRangeException(propertyName, "Switch dimensions must be finite and greater than eight.");
            }
            return resolved;
        }

        private void HandlePointerEnter(PointerEnterEvent _) { _isHovered = true; ApplyValue(((Switch)Widget).Value.Value); }
        private void HandlePointerLeave(PointerLeaveEvent _) { _isHovered = false; ApplyValue(((Switch)Widget).Value.Value); }
        private void HandlePointerDown(PointerDownEvent _) { _isPressed = true; ApplyValue(((Switch)Widget).Value.Value); }
        private void HandlePointerUp(PointerUpEvent _) { _isPressed = false; ApplyValue(((Switch)Widget).Value.Value); }
        private void HandleFocusIn(FocusInEvent _) { _isFocused = true; ApplyValue(((Switch)Widget).Value.Value); }
        private void HandleFocusOut(FocusOutEvent _) { _isFocused = false; ApplyValue(((Switch)Widget).Value.Value); }
        private void BindValue(Switch widget) => _valueSubscription = widget.Value.Subscribe(ApplyValue);
        private void BindFocus(Switch widget) {
            if (widget.FocusNode is { } focusNode) _focusBinding = FocusNodeBinding.Attach(focusNode, _root!, Context.FocusTraversal);
        }
        private void BindField(Switch widget) {
            if (widget.Field is not { } field) return;
            _fieldFocusBinding = field.AttachFocus(_root!.Focus);
            _fieldErrorSubscription = field.ErrorText.Subscribe(_ => ApplyValue(((Switch)Widget).Value.Value));
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
            _root!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _root.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _root.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _root.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _root.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _root.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
