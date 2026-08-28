#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>A custom pointer cursor supported by Unity UI Toolkit.</summary>
    public sealed class PointerCursor {
        public PointerCursor(Texture2D texture, Vector2 hotspot = default) {
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            if (!float.IsFinite(hotspot.x) || !float.IsFinite(hotspot.y)) {
                throw new ArgumentOutOfRangeException(nameof(hotspot), "Cursor hotspot must be finite.");
            }
            Hotspot = hotspot;
        }

        public Texture2D Texture { get; }
        public Vector2 Hotspot { get; }
    }

    /// <summary>
    /// Adds button semantics and keyboard/pointer activation to an arbitrary child
    /// without imposing a visual surface.
    /// </summary>
    public sealed class Pressable : Widget {
        public Pressable(
            Widget child,
            Action onPressed,
            bool enabled = true,
            FocusNode? focusNode = null,
            string? semanticsLabel = null,
            PointerCursor? cursor = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
            Enabled = enabled;
            FocusNode = focusNode;
            SemanticsLabel = semanticsLabel;
            Cursor = cursor;
        }

        /// <summary>Builds state-aware content for an otherwise unpainted interactive surface.</summary>
        public Pressable(
            Func<WidgetStates, Widget> builder,
            Action onPressed,
            bool enabled = true,
            FocusNode? focusNode = null,
            string? semanticsLabel = null,
            PointerCursor? cursor = null) {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
            Enabled = enabled;
            FocusNode = focusNode;
            SemanticsLabel = semanticsLabel;
            Cursor = cursor;
        }

        public Widget? Child { get; }
        public Func<WidgetStates, Widget>? Builder { get; }
        public bool Enabled { get; }
        public FocusNode? FocusNode { get; }
        public string? SemanticsLabel { get; }
        public PointerCursor? Cursor { get; }
        internal Action OnPressed { get; }
        internal override WidgetNode CreateNode() => new PressableNode(this);
    }

    internal sealed class PressableNode : SingleChildWidgetNode<Pressable> {
        private UnityEngine.UIElements.Button? _button;
        private FocusNodeBinding? _focusBinding;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public PressableNode(Pressable widget) : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            _button = new UnityEngine.UIElements.Button();
            ResetNativeChrome(_button);
            _button.SetEnabled(((Pressable)Widget).Enabled);
            ApplyCursor((Pressable)Widget);
            return _button;
        }

        protected override Widget GetChild(Pressable widget) {
            var child = widget.Builder?.Invoke(ResolveStates(widget)) ?? widget.Child;
            return child ?? throw new InvalidOperationException("Pressable builder returned null.");
        }

        protected override void OnMounted() {
            base.OnMounted();
            _button!.clicked += HandleClicked;
            Bindings.Add(() => _button.clicked -= HandleClicked);
            _button.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _button.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _button.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            _button.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            _button.RegisterCallback<FocusInEvent>(HandleFocusIn);
            _button.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Bindings.Add(UnregisterStateCallbacks);
            BindFocusNode((Pressable)Widget);
            Bindings.Add(ReleaseFocusBinding);
        }

        protected override void ApplyConfiguration(Pressable widget) {
            var previous = (Pressable)Widget;
            _button!.SetEnabled(widget.Enabled);
            if (!widget.Enabled) _isPressed = false;
            ApplyCursor(widget);
            if (!ReferenceEquals(previous.FocusNode, widget.FocusNode)) {
                ReleaseFocusBinding();
                BindFocusNode(widget);
            }
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Pressable)Widget;
            return new SemanticsProperties(
                label: widget.SemanticsLabel,
                role: SemanticsRole.Button,
                enabled: widget.Enabled,
                onTap: HandleClicked,
                onAccessibilityFocusChanged: focused => { if (focused) _button?.Focus(); });
        }

        internal void HandleClicked() {
            if (IsMounted && ((Pressable)Widget).Enabled) ((Pressable)Widget).OnPressed();
        }

        internal void SetHovered(bool value) { _isHovered = value; RebuildForState(); }
        internal void SetPressed(bool value) { _isPressed = value; RebuildForState(); }
        internal void SetFocused(bool value) { _isFocused = value; RebuildForState(); }

        private void HandlePointerEnter(PointerEnterEvent _) => SetHovered(true);
        private void HandlePointerLeave(PointerLeaveEvent _) { SetHovered(false); SetPressed(false); }
        private void HandlePointerDown(PointerDownEvent _) => SetPressed(true);
        private void HandlePointerUp(PointerUpEvent _) => SetPressed(false);
        private void HandleFocusIn(FocusInEvent _) => SetFocused(true);
        private void HandleFocusOut(FocusOutEvent _) { SetFocused(false); SetPressed(false); }

        private WidgetStates ResolveStates(Pressable widget) {
            var states = widget.Enabled ? WidgetStates.None : WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isPressed) states |= WidgetStates.Pressed;
            return states;
        }

        private void RebuildForState() {
            if (!IsMounted || ((Pressable)Widget).Builder is null) return;
            ReconcileConfiguredChild((Pressable)Widget);
        }

        private void BindFocusNode(Pressable widget) {
            if (widget.FocusNode is not { } focusNode) return;
            _focusBinding = FocusNodeBinding.Attach(focusNode, _button!, Context.FocusTraversal);
        }

        private void ReleaseFocusBinding() {
            _focusBinding?.Dispose();
            _focusBinding = null;
        }

        private void ApplyCursor(Pressable widget) {
            _button!.style.cursor = widget.Enabled && widget.Cursor is { } cursor
                ? new StyleCursor(new UnityEngine.UIElements.Cursor {
                    texture = cursor.Texture,
                    hotspot = cursor.Hotspot
                })
                : StyleKeyword.Null;
        }

        private void UnregisterStateCallbacks() {
            _button!.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            _button.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            _button.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            _button.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            _button.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            _button.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
        }

        private static void ResetNativeChrome(UnityEngine.UIElements.Button button) {
            button.text = string.Empty;
            button.style.backgroundImage = StyleKeyword.None;
            button.style.backgroundColor = Color.clear;
            BorderStyleMapper.Apply(button, Border.All(Color.clear, 0f));
            button.style.marginTop = 0f;
            button.style.marginRight = 0f;
            button.style.marginBottom = 0f;
            button.style.marginLeft = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingBottom = 0f;
            button.style.paddingLeft = 0f;
            button.style.justifyContent = Justify.FlexStart;
            button.style.alignItems = UnityEngine.UIElements.Align.Stretch;
        }
    }
}
