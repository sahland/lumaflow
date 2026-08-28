#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ButtonNode : WidgetNode {
        private UnityEngine.UIElements.Button? _button;
        private WidgetStates _appliedStates;
        private bool _hasStyleApplied;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;
        private FocusNodeBinding? _focusBinding;
        private WidgetNode? _childNode;

        public ButtonNode(Button widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Button)Widget;
            _button = new UnityEngine.UIElements.Button {
                text = widget.Text
            };

            // A styled LumaFlow button owns its surface; suppress the editor skin's
            // inset background, border, and margins before applying widget styles.
            _button.style.backgroundImage = StyleKeyword.None;
            _button.style.borderTopWidth = 0f;
            _button.style.borderRightWidth = 0f;
            _button.style.borderBottomWidth = 0f;
            _button.style.borderLeftWidth = 0f;
            _button.style.marginTop = 0f;
            _button.style.marginRight = 0f;
            _button.style.marginBottom = 0f;
            _button.style.marginLeft = 0f;
            _button.style.justifyContent = Justify.Center;
            _button.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;

            _button.SetEnabled(widget.Enabled);
            ApplyResolvedInteractionStyle();

            return _button;
        }

        protected override void OnMounted() {
            MountOrUpdateChild((Button)Widget);
            _button!.clicked += HandleClicked;
            Bindings.Add(() => _button.clicked -= HandleClicked);
            _button.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _button.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _button.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            _button.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            _button.RegisterCallback<FocusInEvent>(HandleFocusIn);
            _button.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterCallbacks);
            BindFocusNode((Button)Widget);
            Bindings.Add(ReleaseFocusBinding);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Button button) return false;

            var previous = (Button)Widget;
            UpdateWidget(button);
            _button!.text = button.HasCustomChild ? string.Empty : button.Text;
            _button.SetEnabled(button.Enabled);
            if (!ReferenceEquals(previous.FocusNode, button.FocusNode)
                || previous.ReplaceFocusNodeAttachment != button.ReplaceFocusNodeAttachment) {
                ReleaseFocusBinding();
                BindFocusNode(button);
            }

            MountOrUpdateChild(button);

            _hasStyleApplied = false;
            ReevaluateInheritedDependencies(() =>
            {
                ApplyResolvedInteractionStyle();
                return true;
            });
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            _hasStyleApplied = false;
            ApplyResolvedInteractionStyle();
            RefreshStyledChild();
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Button)Widget;
            return new SemanticsProperties(
                label: widget.SemanticsLabel ?? (widget.HasCustomChild ? null : widget.Text),
                role: SemanticsRole.Button,
                enabled: widget.Enabled,
                onTap: HandleClicked,
                onAccessibilityFocusChanged: focused => { if (focused) _button?.Focus(); });
        }

        internal void HandleClicked() {
            if (IsMounted && ((Button)Widget).Enabled) {
                ((Button)Widget).OnPressed();
            }
        }

        private void HandlePointerEnter(PointerEnterEvent _) => SetHovered(true);

        private void HandlePointerLeave(PointerLeaveEvent _) => SetHovered(false);

        private void HandlePointerDown(PointerDownEvent _) => SetPressed(true);

        private void HandlePointerUp(PointerUpEvent _) => SetPressed(false);

        private void HandleFocusIn(FocusInEvent _) => SetFocused(true);

        private void HandleFocusOut(FocusOutEvent _) => SetFocused(false);

        internal void ApplyHoveredStyle() => SetHovered(true);

        internal void ApplyPressedStyle() => SetPressed(true);

        internal void SetHovered(bool value) {
            _isHovered = value;
            ApplyResolvedInteractionStyle();
            RefreshStyledChild();
        }

        internal void SetPressed(bool value) {
            _isPressed = value;
            ApplyResolvedInteractionStyle();
            RefreshStyledChild();
        }

        internal void SetFocused(bool value) {
            _isFocused = value;
            ApplyResolvedInteractionStyle();
            RefreshStyledChild();
        }

        private void ApplyResolvedInteractionStyle() {
            var style = EffectiveStyle;
            ApplyStyle(ResolveStates());
        }

        private WidgetStates ResolveStates() {
            var states = WidgetStates.None;
            if (!((Button)Widget).Enabled) states |= WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            return states;
        }

        private void ApplyStyle(WidgetStates states) {
            var style = EffectiveStyle;
            if (_hasStyleApplied && _appliedStates == states) {
                return;
            }

            _hasStyleApplied = true;
            _appliedStates = states;
            ClearAppliedStyle();
            if (style is null) return;
            var background = style.ResolveBackground(states);
            if (background is { } backgroundColor) {
                _button!.style.backgroundColor = backgroundColor;
            }

            var foreground = style.ResolveForeground(states);
            if (foreground is { } foregroundColor) {
                _button!.style.color = foregroundColor;
            }

            if (style.ResolvePadding(states) is { } padding) {
                PaddingStyleMapper.Apply(_button!, padding);
            }

            if (style.ResolveShape(states) is { } shape) {
                _button!.style.borderTopLeftRadius = shape.TopLeft;
                _button.style.borderTopRightRadius = shape.TopRight;
                _button.style.borderBottomRightRadius = shape.BottomRight;
                _button.style.borderBottomLeftRadius = shape.BottomLeft;
            }

            if (style.ResolveBorder(states) is { } border) {
                BorderStyleMapper.Apply(_button!, border);
            }

            if (style.ResolveTypography(states) is { } typography) {
                TextStyleMapper.Apply(_button!, typography, Context.TextScaler);
            }

            if (style.ResolveMinimumSize(states) is { } minimumSize) {
                _button!.style.minWidth = minimumSize.x;
                _button.style.minHeight = minimumSize.y;
            }
        }

        private ButtonStyle? EffectiveStyle {
            get {
                var widget = (Button)Widget;
                return widget.Style ?? Context.Theme?.ButtonTheme.Resolve(widget.Variant);
            }
        }

        private void BindFocusNode(Button widget) {
            if (widget.FocusNode is not { } focusNode) return;
            _focusBinding = FocusNodeBinding.Attach(
                focusNode,
                _button!,
                Context.FocusTraversal,
                widget.ReplaceFocusNodeAttachment);
        }

        private void ReleaseFocusBinding() {
            _focusBinding?.Dispose();
            _focusBinding = null;
        }

        private void ClearAppliedStyle() {
            _button!.style.backgroundColor = StyleKeyword.Null;
            _button.style.color = StyleKeyword.Null;
            PaddingStyleMapper.Clear(_button);
            _button.style.borderTopLeftRadius = StyleKeyword.Null;
            _button.style.borderTopRightRadius = StyleKeyword.Null;
            _button.style.borderBottomRightRadius = StyleKeyword.Null;
            _button.style.borderBottomLeftRadius = StyleKeyword.Null;
            BorderStyleMapper.Apply(_button, Border.All(UnityEngine.Color.clear, 0f));
            _button.style.fontSize = StyleKeyword.Null;
            _button.style.unityFontStyleAndWeight = StyleKeyword.Null;
            _button.style.minWidth = StyleKeyword.Null;
            _button.style.minHeight = StyleKeyword.Null;
        }

        private void MountOrUpdateChild(Button widget) {
            if (!widget.HasCustomChild) {
                if (_childNode is not null) {
                    UnmountChild(_childNode);
                    _childNode = null;
                }
                _button!.text = widget.Text;
                return;
            }

            _button!.text = string.Empty;
            ReconcileSingleChild(ref _childNode, BuildStyledChild(widget), _button);
        }

        private Widget BuildStyledChild(Button widget) {
            var theme = Context.Theme;
            var style = EffectiveStyle;
            if (theme is null || style is null) return widget.Child;

            var states = ResolveStates();
            var foreground = style.ResolveForeground(states);
            var typographyOverride = style.ResolveTypography(states);
            var inheritedBody = theme.Typography.Body;
            var body = new TextStyle(
                color: typographyOverride?.Color ?? foreground ?? inheritedBody.Color,
                fontSize: typographyOverride?.FontSize ?? inheritedBody.FontSize,
                fontStyle: typographyOverride?.FontStyle ?? inheritedBody.FontStyle);
            var typography = new TypographyTheme(
                theme.Typography.Title,
                theme.Typography.Headline,
                body,
                theme.Typography.Label);
            var iconTheme = foreground is { } color
                ? new IconThemeData(theme.IconTheme.Size, color, color)
                : theme.IconTheme;
            var childTheme = theme.CopyWith(
                typography: typography,
                iconTheme: iconTheme);
            return new Theme(childTheme, widget.Child);
        }

        private void RefreshStyledChild() {
            if (IsMounted && ((Button)Widget).HasCustomChild) MountOrUpdateChild((Button)Widget);
        }

        private void UnregisterCallbacks() {
            _button!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _button.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _button.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _button.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _button.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _button.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }
    }

}
