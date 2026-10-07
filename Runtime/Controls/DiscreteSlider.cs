#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>One typed stop rendered by a <see cref="DiscreteSlider{T}" />.</summary>
    public sealed class DiscreteSliderItem<T> {
        private readonly Func<bool, Widget> _builder;

        public DiscreteSliderItem(T value, string label)
            : this(
                value,
                _ => new Text(ValidateLabel(label), softWrap: false, overflow: TextOverflow.Ellipsis, maxLines: 1),
                label) {
        }

        public DiscreteSliderItem(T value, Widget child, string semanticsLabel)
            : this(value, CreateBuilder(child), semanticsLabel) {
        }

        public DiscreteSliderItem(T value, Func<bool, Widget> builder, string semanticsLabel) {
            Value = value;
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            SemanticsLabel = ValidateLabel(semanticsLabel);
        }

        public T Value { get; }
        public string SemanticsLabel { get; }

        internal Widget Build(bool selected) => _builder(selected)
            ?? throw new InvalidOperationException("Discrete slider item builder returned null.");

        private static string ValidateLabel(string label) => string.IsNullOrWhiteSpace(label)
            ? throw new ArgumentException("A non-empty semantics label is required.", nameof(label))
            : label;

        private static Func<bool, Widget> CreateBuilder(Widget child) {
            if (child is null) throw new ArgumentNullException(nameof(child));
            return _ => child;
        }
    }

    /// <summary>
    /// A typed slider for a finite ordered set. Markers, track, and thumb remain
    /// declarative widgets while the node owns pointer capture and snapping.
    /// </summary>
    public sealed class DiscreteSlider<T> : Widget {
        private readonly DiscreteSliderItem<T>[] _items;

        public DiscreteSlider(
            State<T> value,
            IReadOnlyList<DiscreteSliderItem<T>> items,
            Func<T, WidgetStates, Widget> thumbBuilder,
            Widget? track = null,
            string? semanticsLabel = null,
            bool enabled = true,
            float height = 48f,
            float startInset = 0f,
            float endInset = 0f,
            float snapThreshold = 0.025f,
            FocusNode? focusNode = null,
            Action<T>? onChanged = null,
            Action<T>? onChangeStart = null,
            Action<T>? onChangeEnd = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            if (items is null || items.Count < 2) {
                throw new ArgumentException("A discrete slider requires at least two items.", nameof(items));
            }
            _items = new DiscreteSliderItem<T>[items.Count];
            var comparer = EqualityComparer<T>.Default;
            var containsValue = false;
            for (var index = 0; index < items.Count; index++) {
                var item = items[index] ?? throw new ArgumentException("Slider items cannot contain null.", nameof(items));
                for (var previous = 0; previous < index; previous++) {
                    if (comparer.Equals(_items[previous].Value, item.Value)) {
                        throw new ArgumentException("Slider item values must be unique.", nameof(items));
                    }
                }
                _items[index] = item;
                containsValue |= comparer.Equals(item.Value, value.Value);
            }
            if (!containsValue) {
                throw new ArgumentException("The controlled value must match one of the slider items.", nameof(value));
            }
            if (!float.IsFinite(height) || height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
            if (!float.IsFinite(startInset) || startInset < 0f) throw new ArgumentOutOfRangeException(nameof(startInset));
            if (!float.IsFinite(endInset) || endInset < 0f) throw new ArgumentOutOfRangeException(nameof(endInset));
            if (!float.IsFinite(snapThreshold) || snapThreshold < 0f || snapThreshold > 0.5f) {
                throw new ArgumentOutOfRangeException(nameof(snapThreshold), "Snap threshold must be between zero and 0.5.");
            }

            ThumbBuilder = thumbBuilder ?? throw new ArgumentNullException(nameof(thumbBuilder));
            Track = track;
            SemanticsLabel = semanticsLabel;
            Enabled = enabled;
            Height = height;
            StartInset = startInset;
            EndInset = endInset;
            SnapThreshold = snapThreshold;
            FocusNode = focusNode;
            OnChanged = onChanged;
            OnChangeStart = onChangeStart;
            OnChangeEnd = onChangeEnd;
        }

        public State<T> Value { get; }
        public IReadOnlyList<DiscreteSliderItem<T>> Items => _items;
        public Func<T, WidgetStates, Widget> ThumbBuilder { get; }
        public Widget? Track { get; }
        public string? SemanticsLabel { get; }
        public bool Enabled { get; }
        public float Height { get; }
        public float StartInset { get; }
        public float EndInset { get; }
        public float SnapThreshold { get; }
        public FocusNode? FocusNode { get; }
        public Action<T>? OnChanged { get; }
        public Action<T>? OnChangeStart { get; }
        public Action<T>? OnChangeEnd { get; }

        internal override WidgetNode CreateNode() => new DiscreteSliderNode<T>(this);
    }

    internal sealed class DiscreteSliderNode<T> : WidgetNode {
        private IDisposable? _valueSubscription;
        private FocusNodeBinding? _focusBinding;
        private Widget[] _visuals = Array.Empty<Widget>();
        private float _availableWidth;
        private float _displayPosition;
        private int _activePointerId = -1;
        private bool _isInteracting;
        private bool _isHovered;
        private bool _isFocused;

        public DiscreteSliderNode(DiscreteSlider<T> widget)
            : base(widget) {
        }

        private DiscreteSlider<T> Slider => (DiscreteSlider<T>)Widget;

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement {
                focusable = true,
                pickingMode = PickingMode.Position
            };
            element.style.position = Position.Relative;
            element.style.height = Slider.Height;
            element.style.minWidth = 0f;
            element.style.overflow = Overflow.Visible;
            element.SetEnabled(Slider.Enabled);
            return element;
        }

        protected override void OnMounted() {
            _displayPosition = PositionForValue(Slider.Value.Value);
            RefreshVisuals();
            Element.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            Element.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Element.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            Element.RegisterCallback<PointerDownEvent>(HandlePointerDownEvent);
            Element.RegisterCallback<PointerMoveEvent>(HandlePointerMoveEvent);
            Element.RegisterCallback<PointerUpEvent>(HandlePointerUpEvent);
            Element.RegisterCallback<PointerCancelEvent>(HandlePointerCancelEvent);
            Element.RegisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOut);
            Element.RegisterCallback<FocusInEvent>(HandleFocusIn);
            Element.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            Element.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            Bindings.Add(UnregisterCallbacks);
            BindValue(Slider);
            BindFocus(Slider);
            Bindings.Add(ReleaseBindings);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not DiscreteSlider<T> slider) return false;
            var previous = Slider;
            var preserveInteraction = _isInteracting
                && slider.Enabled
                && ReferenceEquals(previous.Value, slider.Value)
                && HaveSameItemValues(previous.Items, slider.Items);
            if (!preserveInteraction) ResetInteraction();
            if (!ReferenceEquals(previous.Value, slider.Value)) ReleaseValueBinding();
            if (!ReferenceEquals(previous.FocusNode, slider.FocusNode)) ReleaseFocusBinding();
            UpdateWidget(slider);
            Element.SetEnabled(slider.Enabled);
            Element.style.height = slider.Height;
            if (!preserveInteraction) _displayPosition = PositionForValue(slider.Value.Value);
            if (!ReferenceEquals(previous.Value, slider.Value)) BindValue(slider);
            if (!ReferenceEquals(previous.FocusNode, slider.FocusNode)) BindFocus(slider);
            RefreshVisuals();
            RefreshSemantics();
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => RefreshVisuals();

        protected override SemanticsProperties DescribeSemantics() {
            var slider = Slider;
            var selected = FindIndex(slider.Value.Value);
            return new SemanticsProperties(
                label: slider.SemanticsLabel,
                value: selected >= 0 ? slider.Items[selected].SemanticsLabel : null,
                role: SemanticsRole.Slider,
                enabled: slider.Enabled,
                onIncrease: () => SelectRelative(1),
                onDecrease: () => SelectRelative(-1),
                onAccessibilityFocusChanged: focused => { if (focused) Element.Focus(); });
        }

        internal void SetAvailableWidth(float width) {
            _availableWidth = float.IsFinite(width) ? Mathf.Max(0f, width) : 0f;
            PositionVisuals();
        }

        internal bool HandlePointerDown(int pointerId, float localX, int button = 0) {
            var slider = Slider;
            if (!IsMounted || !slider.Enabled || button != 0 || _activePointerId >= 0) return false;
            _activePointerId = pointerId;
            _isInteracting = true;
            try {
                slider.OnChangeStart?.Invoke(slider.Value.Value);
            } catch {
                _activePointerId = -1;
                _isInteracting = false;
                throw;
            }
            if (!IsMounted) {
                _activePointerId = -1;
                _isInteracting = false;
                return false;
            }
            RefreshVisuals();
            try {
                UpdateDrag(localX);
            } catch {
                ResetInteraction();
                _displayPosition = PositionForValue(slider.Value.Value);
                RefreshVisuals();
                throw;
            }
            return true;
        }

        internal bool HandlePointerMove(int pointerId, float localX) {
            if (pointerId != _activePointerId || !_isInteracting) return false;
            UpdateDrag(localX);
            return true;
        }

        internal bool HandlePointerUp(int pointerId, float localX) {
            if (pointerId != _activePointerId || !_isInteracting) return false;
            Exception? changeFailure = null;
            Exception? endFailure = null;
            try {
                UpdateDrag(localX);
            } catch (Exception exception) {
                changeFailure = exception;
            } finally {
                if (IsMounted && pointerId == _activePointerId && _isInteracting) {
                    try {
                        FinishInteraction();
                    } catch (Exception exception) {
                        endFailure = exception;
                    }
                }
            }
            if (changeFailure is not null && endFailure is not null) {
                throw new AggregateException("The slider value change and onChangeEnd callback both failed.", changeFailure, endFailure);
            }
            if (changeFailure is not null) ExceptionDispatchInfo.Capture(changeFailure).Throw();
            if (endFailure is not null) ExceptionDispatchInfo.Capture(endFailure).Throw();
            return true;
        }

        internal bool HandlePointerCancel(int pointerId) {
            if (pointerId != _activePointerId || !_isInteracting) return false;
            FinishInteraction();
            return true;
        }

        private void UpdateDrag(float localX) {
            if (_availableWidth <= 0f) return;
            var slider = Slider;
            var usableWidth = Mathf.Max(1f, _availableWidth - slider.StartInset - slider.EndInset);
            var raw = Mathf.Clamp01((localX - slider.StartInset) / usableWidth);
            var nearest = NearestIndex(raw);
            var stop = PositionForIndex(nearest);
            _displayPosition = Mathf.Abs(raw - stop) <= slider.SnapThreshold ? stop : raw;
            PositionVisuals();
            if (!EqualityComparer<T>.Default.Equals(slider.Value.Value, slider.Items[nearest].Value)) {
                ControlledInputChange.Commit(slider.Value, slider.Items[nearest].Value, slider.OnChanged);
            }
        }

        private void FinishInteraction() {
            var slider = Slider;
            var pointerId = _activePointerId;
            _activePointerId = -1;
            _isInteracting = false;
            _displayPosition = PositionForValue(slider.Value.Value);
            RefreshVisuals();
            if (Element.HasPointerCapture(pointerId)) Element.ReleasePointer(pointerId);
            slider.OnChangeEnd?.Invoke(slider.Value.Value);
        }

        internal bool HandleStep(int direction) {
            if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
            return SelectRelative(direction);
        }

        private bool SelectRelative(int direction) {
            var slider = Slider;
            if (!IsMounted || !slider.Enabled) return false;
            var current = FindIndex(slider.Value.Value);
            var next = Mathf.Clamp(current + direction, 0, slider.Items.Count - 1);
            if (next == current) return false;
            slider.OnChangeStart?.Invoke(slider.Value.Value);
            if (!IsMounted) return false;
            ControlledInputChange.Commit(slider.Value, slider.Items[next].Value, slider.OnChanged);
            if (!IsMounted) return false;
            slider.OnChangeEnd?.Invoke(slider.Value.Value);
            return true;
        }

        private void UpdateValue(T value) {
            if (!_isInteracting) _displayPosition = PositionForValue(value);
            RefreshVisuals();
            RefreshSemantics();
        }

        private void RefreshVisuals() {
            var slider = Slider;
            var selected = FindIndex(slider.Value.Value);
            _visuals = new Widget[slider.Items.Count + 2];
            _visuals[0] = new DiscreteSliderTrackSlotWidget(
                slider.Track ?? new DefaultDiscreteSliderTrack(),
                slider.StartInset,
                slider.EndInset);
            for (var index = 0; index < slider.Items.Count; index++) {
                _visuals[index + 1] = new DiscreteSliderPositionSlot(
                    slider.Items[index].Build(index == selected));
            }
            var thumb = slider.ThumbBuilder(slider.Value.Value, ResolveStates())
                ?? throw new InvalidOperationException("Discrete slider thumb builder returned null.");
            _visuals[_visuals.Length - 1] = new DiscreteSliderPositionSlot(thumb);
            ReconcileChildren(_visuals, Element);
            PositionVisuals();
        }

        private void PositionVisuals() {
            if (Element.childCount != Slider.Items.Count + 2) return;
            var usableWidth = Mathf.Max(0f, _availableWidth - Slider.StartInset - Slider.EndInset);
            for (var index = 0; index < Slider.Items.Count; index++) {
                Element[index + 1].style.left = Slider.StartInset + usableWidth * PositionForIndex(index);
            }
            Element[Element.childCount - 1].style.left = Slider.StartInset + usableWidth * _displayPosition;
        }

        private WidgetStates ResolveStates() {
            var states = Slider.Enabled ? WidgetStates.None : WidgetStates.Disabled;
            if (_isHovered) states |= WidgetStates.Hovered;
            if (_isFocused) states |= WidgetStates.Focused;
            if (_isInteracting) states |= WidgetStates.Pressed | WidgetStates.Dragged;
            return states;
        }

        private int FindIndex(T value) {
            for (var index = 0; index < Slider.Items.Count; index++) {
                if (EqualityComparer<T>.Default.Equals(value, Slider.Items[index].Value)) return index;
            }
            return -1;
        }

        private int NearestIndex(float position) => Mathf.Clamp(
            Mathf.RoundToInt(position * (Slider.Items.Count - 1)),
            0,
            Slider.Items.Count - 1);

        private float PositionForValue(T value) {
            var index = FindIndex(value);
            return index < 0 ? 0f : PositionForIndex(index);
        }

        private float PositionForIndex(int index) => index / (float)(Slider.Items.Count - 1);

        private static bool HaveSameItemValues(
            IReadOnlyList<DiscreteSliderItem<T>> previous,
            IReadOnlyList<DiscreteSliderItem<T>> next) {
            if (previous.Count != next.Count) return false;
            var comparer = EqualityComparer<T>.Default;
            for (var index = 0; index < previous.Count; index++) {
                if (!comparer.Equals(previous[index].Value, next[index].Value)) return false;
            }
            return true;
        }

        private void BindValue(DiscreteSlider<T> slider) => _valueSubscription = slider.Value.Subscribe(UpdateValue);

        private void BindFocus(DiscreteSlider<T> slider) {
            if (slider.FocusNode is { } focusNode) {
                _focusBinding = FocusNodeBinding.Attach(focusNode, Element, Context.FocusTraversal);
            }
        }

        private void ReleaseValueBinding() {
            _valueSubscription?.Dispose();
            _valueSubscription = null;
        }

        private void ReleaseFocusBinding() {
            _focusBinding?.Dispose();
            _focusBinding = null;
        }

        private void ReleaseBindings() {
            ResetInteraction();
            ReleaseFocusBinding();
            ReleaseValueBinding();
        }

        private void ResetInteraction() {
            var pointerId = _activePointerId;
            _activePointerId = -1;
            _isInteracting = false;
            if (pointerId >= 0 && Element.HasPointerCapture(pointerId)) Element.ReleasePointer(pointerId);
        }

        private void HandleGeometryChanged(GeometryChangedEvent evt) => SetAvailableWidth(evt.newRect.width);

        private void HandlePointerEnter(PointerEnterEvent _) {
            _isHovered = true;
            RefreshVisuals();
        }

        private void HandlePointerLeave(PointerLeaveEvent _) {
            _isHovered = false;
            RefreshVisuals();
        }

        private void HandleFocusIn(FocusInEvent _) {
            _isFocused = true;
            RefreshVisuals();
        }

        private void HandleFocusOut(FocusOutEvent _) {
            _isFocused = false;
            RefreshVisuals();
        }

        private void HandlePointerDownEvent(PointerDownEvent evt) {
            if (!HandlePointerDown(evt.pointerId, evt.localPosition.x, evt.button)) return;
            if (IsMounted && _activePointerId == evt.pointerId) Element.CapturePointer(evt.pointerId);
        }

        private void HandlePointerMoveEvent(PointerMoveEvent evt) => HandlePointerMove(evt.pointerId, evt.localPosition.x);

        private void HandlePointerUpEvent(PointerUpEvent evt) => HandlePointerUp(evt.pointerId, evt.localPosition.x);

        private void HandlePointerCancelEvent(PointerCancelEvent evt) => HandlePointerCancel(evt.pointerId);

        private void HandlePointerCaptureOut(PointerCaptureOutEvent evt) => HandlePointerCancel(evt.pointerId);

        private void HandleKeyDown(KeyDownEvent evt) {
            if (evt.keyCode is KeyCode.LeftArrow or KeyCode.DownArrow) {
                HandleStep(-1);
                evt.StopPropagation();
            } else if (evt.keyCode is KeyCode.RightArrow or KeyCode.UpArrow) {
                HandleStep(1);
                evt.StopPropagation();
            }
        }

        private void UnregisterCallbacks() {
            Element.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            Element.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Element.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            Element.UnregisterCallback<PointerDownEvent>(HandlePointerDownEvent);
            Element.UnregisterCallback<PointerMoveEvent>(HandlePointerMoveEvent);
            Element.UnregisterCallback<PointerUpEvent>(HandlePointerUpEvent);
            Element.UnregisterCallback<PointerCancelEvent>(HandlePointerCancelEvent);
            Element.UnregisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOut);
            Element.UnregisterCallback<FocusInEvent>(HandleFocusIn);
            Element.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
            Element.UnregisterCallback<KeyDownEvent>(HandleKeyDown);
        }
    }

    internal sealed class DiscreteSliderTrackSlot : SingleChildWidgetNode<DiscreteSliderTrackSlotWidget> {
        public DiscreteSliderTrackSlot(DiscreteSliderTrackSlotWidget widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) => CreateHost((DiscreteSliderTrackSlotWidget)Widget);
        protected override Widget GetChild(DiscreteSliderTrackSlotWidget widget) => widget.Child;
        protected override void ApplyConfiguration(DiscreteSliderTrackSlotWidget widget) => ApplyHost(Element, widget);

        private static VisualElement CreateHost(DiscreteSliderTrackSlotWidget widget) {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.top = Length.Percent(50f);
            element.style.left = widget.StartInset;
            element.style.right = widget.EndInset;
            element.style.height = 0f;
            element.style.justifyContent = Justify.Center;
            element.style.overflow = Overflow.Visible;
            return element;
        }

        private static void ApplyHost(VisualElement element, DiscreteSliderTrackSlotWidget widget) {
            element.style.left = widget.StartInset;
            element.style.right = widget.EndInset;
        }
    }

    internal sealed class DiscreteSliderTrackSlotWidget : Widget {
        public DiscreteSliderTrackSlotWidget(Widget child, float startInset, float endInset) {
            Child = child;
            StartInset = startInset;
            EndInset = endInset;
        }
        public Widget Child { get; }
        public float StartInset { get; }
        public float EndInset { get; }
        internal override WidgetNode CreateNode() => new DiscreteSliderTrackSlot(this);
    }

    internal sealed class DiscreteSliderPositionSlot : Widget {
        public DiscreteSliderPositionSlot(Widget child) => Child = child;
        public Widget Child { get; }
        internal override WidgetNode CreateNode() => new DiscreteSliderPositionSlotNode(this);
    }

    internal sealed class DiscreteSliderPositionSlotNode : SingleChildWidgetNode<DiscreteSliderPositionSlot> {
        public DiscreteSliderPositionSlotNode(DiscreteSliderPositionSlot widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.top = 0f;
            element.style.bottom = 0f;
            element.style.width = 0f;
            element.style.alignItems = UnityEngine.UIElements.Align.Center;
            element.style.justifyContent = Justify.Center;
            element.style.overflow = Overflow.Visible;
            return element;
        }
        protected override Widget GetChild(DiscreteSliderPositionSlot widget) => widget.Child;
        protected override void OnChildReconciled(DiscreteSliderPositionSlot widget, VisualElement child) {
            child.style.flexShrink = 0f;
        }
    }

    internal sealed class DefaultDiscreteSliderTrack : Widget {
        internal override WidgetNode CreateNode() => new DefaultDiscreteSliderTrackNode(this);
    }

    internal sealed class DefaultDiscreteSliderTrackNode : WidgetNode {
        public DefaultDiscreteSliderTrackNode(DefaultDiscreteSliderTrack widget) : base(widget) { }
        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.height = 4f;
            element.style.borderTopLeftRadius = 2f;
            element.style.borderTopRightRadius = 2f;
            element.style.borderBottomLeftRadius = 2f;
            element.style.borderBottomRightRadius = 2f;
            element.style.backgroundColor = context.Theme?.Colors.Outline ?? new Color(0.65f, 0.67f, 0.72f);
            return element;
        }
        protected override void OnInheritedChanged(InheritedAspect aspect) {
            Element.style.backgroundColor = Context.Theme?.Colors.Outline ?? new Color(0.65f, 0.67f, 0.72f);
        }
    }
}
