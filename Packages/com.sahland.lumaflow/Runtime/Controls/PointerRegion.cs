#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>A stable snapshot of one UI Toolkit pointer event.</summary>
    public readonly struct PointerEventDetails {
        public PointerEventDetails(int pointerId, Vector2 position, Vector2 localPosition, int button) {
            PointerId = pointerId;
            Position = position;
            LocalPosition = localPosition;
            Button = button;
        }

        public int PointerId { get; }
        public Vector2 Position { get; }
        public Vector2 LocalPosition { get; }
        public int Button { get; }
    }

    /// <summary>Describes one phase of a drag recognized by <see cref="PointerRegion" />.</summary>
    public readonly struct DragEventDetails {
        public DragEventDetails(
            PointerEventDetails pointer,
            Vector2 delta,
            Vector2 totalDelta,
            bool cancelled = false) {
            Pointer = pointer;
            Delta = delta;
            TotalDelta = totalDelta;
            Cancelled = cancelled;
        }

        public PointerEventDetails Pointer { get; }
        public Vector2 Delta { get; }
        public Vector2 TotalDelta { get; }
        public bool Cancelled { get; }
    }

    /// <summary>
    /// Adds raw pointer callbacks and drag recognition to a child without painting
    /// a visual surface or imposing button semantics.
    /// </summary>
    public sealed class PointerRegion : Widget {
        public PointerRegion(
            Widget child,
            Action<PointerEventDetails>? onPointerDown = null,
            Action<PointerEventDetails>? onPointerMove = null,
            Action<PointerEventDetails>? onPointerUp = null,
            Action<PointerEventDetails>? onPointerCancel = null,
            Action<DragEventDetails>? onDragStart = null,
            Action<DragEventDetails>? onDragUpdate = null,
            Action<DragEventDetails>? onDragEnd = null,
            float dragThreshold = 4f,
            int dragButton = 0,
            bool enabled = true) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (!float.IsFinite(dragThreshold) || dragThreshold < 0f) {
                throw new ArgumentOutOfRangeException(nameof(dragThreshold), "Drag threshold must be finite and non-negative.");
            }
            if (dragButton < 0) {
                throw new ArgumentOutOfRangeException(nameof(dragButton), "Drag button must be non-negative.");
            }

            OnPointerDown = onPointerDown;
            OnPointerMove = onPointerMove;
            OnPointerUp = onPointerUp;
            OnPointerCancel = onPointerCancel;
            OnDragStart = onDragStart;
            OnDragUpdate = onDragUpdate;
            OnDragEnd = onDragEnd;
            DragThreshold = dragThreshold;
            DragButton = dragButton;
            Enabled = enabled;
        }

        public Widget Child { get; }
        public float DragThreshold { get; }
        public int DragButton { get; }
        public bool Enabled { get; }
        internal Action<PointerEventDetails>? OnPointerDown { get; }
        internal Action<PointerEventDetails>? OnPointerMove { get; }
        internal Action<PointerEventDetails>? OnPointerUp { get; }
        internal Action<PointerEventDetails>? OnPointerCancel { get; }
        internal Action<DragEventDetails>? OnDragStart { get; }
        internal Action<DragEventDetails>? OnDragUpdate { get; }
        internal Action<DragEventDetails>? OnDragEnd { get; }
        internal bool RecognizesDrag => OnDragStart is not null || OnDragUpdate is not null || OnDragEnd is not null;

        internal override WidgetNode CreateNode() => new PointerRegionNode(this);
    }

    internal sealed class PointerRegionNode : SingleChildWidgetNode<PointerRegion> {
        private int _activePointerId = -1;
        private PointerEventDetails _lastPointer;
        private Vector2 _dragOrigin;
        private bool _isDragging;

        public PointerRegionNode(PointerRegion widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement { pickingMode = PickingMode.Position };
            element.style.flexShrink = 1f;
            element.style.alignSelf = UnityEngine.UIElements.Align.Stretch;
            return element;
        }

        protected override Widget GetChild(PointerRegion widget) => widget.Child;

        protected override void OnMounted() {
            base.OnMounted();
            Element.RegisterCallback<PointerDownEvent>(HandlePointerDownEvent);
            Element.RegisterCallback<PointerMoveEvent>(HandlePointerMoveEvent);
            Element.RegisterCallback<PointerUpEvent>(HandlePointerUpEvent);
            Element.RegisterCallback<PointerCancelEvent>(HandlePointerCancelEvent);
            Element.RegisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOutEvent);
            Bindings.Add(UnregisterCallbacks);
            Bindings.Add(CancelActiveDrag);
        }

        protected override void ApplyConfiguration(PointerRegion widget) {
            if (!widget.Enabled) CancelActiveDrag();
        }

        internal void HandlePointerDown(PointerEventDetails details) {
            var widget = CurrentWidget;
            if (!widget.Enabled) return;
            widget.OnPointerDown?.Invoke(details);
            if (!widget.RecognizesDrag || details.Button != widget.DragButton || _activePointerId >= 0) return;

            _activePointerId = details.PointerId;
            _dragOrigin = details.Position;
            _lastPointer = details;
            _isDragging = false;
        }

        internal void HandlePointerMove(PointerEventDetails details) {
            var widget = CurrentWidget;
            if (!widget.Enabled) return;
            widget.OnPointerMove?.Invoke(details);
            if (details.PointerId != _activePointerId) return;

            var delta = details.Position - _lastPointer.Position;
            var totalDelta = details.Position - _dragOrigin;
            _lastPointer = details;
            if (!_isDragging && totalDelta.sqrMagnitude >= widget.DragThreshold * widget.DragThreshold) {
                _isDragging = true;
                widget.OnDragStart?.Invoke(new DragEventDetails(details, Vector2.zero, totalDelta));
            }
            if (_isDragging) {
                widget.OnDragUpdate?.Invoke(new DragEventDetails(details, delta, totalDelta));
            }
        }

        internal void HandlePointerUp(PointerEventDetails details) {
            var widget = CurrentWidget;
            if (widget.Enabled) widget.OnPointerUp?.Invoke(details);
            if (details.PointerId != _activePointerId) return;
            FinishDrag(details, cancelled: false);
        }

        internal void HandlePointerCancel(PointerEventDetails details) {
            var widget = CurrentWidget;
            if (widget.Enabled) widget.OnPointerCancel?.Invoke(details);
            if (details.PointerId != _activePointerId) return;
            FinishDrag(details, cancelled: true);
        }

        private void HandlePointerDownEvent(PointerDownEvent evt) {
            HandlePointerDown(ToDetails(evt));
            if (_activePointerId == evt.pointerId && !Element.HasPointerCapture(evt.pointerId)) {
                Element.CapturePointer(evt.pointerId);
            }
        }

        private void HandlePointerMoveEvent(PointerMoveEvent evt) => HandlePointerMove(ToDetails(evt));

        private void HandlePointerUpEvent(PointerUpEvent evt) {
            HandlePointerUp(ToDetails(evt));
            ReleasePointer(evt.pointerId);
        }

        private void HandlePointerCancelEvent(PointerCancelEvent evt) {
            HandlePointerCancel(ToDetails(evt));
            ReleasePointer(evt.pointerId);
        }

        private void HandlePointerCaptureOutEvent(PointerCaptureOutEvent evt) {
            if (evt.pointerId != _activePointerId) return;
            FinishDrag(_lastPointer, cancelled: true);
        }

        private void FinishDrag(PointerEventDetails details, bool cancelled) {
            var wasDragging = _isDragging;
            var totalDelta = details.Position - _dragOrigin;
            var delta = details.Position - _lastPointer.Position;
            _activePointerId = -1;
            _isDragging = false;
            _lastPointer = details;
            if (wasDragging) {
                CurrentWidget.OnDragEnd?.Invoke(new DragEventDetails(details, delta, totalDelta, cancelled));
            }
        }

        private void CancelActiveDrag() {
            if (_activePointerId < 0) return;
            var pointerId = _activePointerId;
            FinishDrag(_lastPointer, cancelled: true);
            ReleasePointer(pointerId);
        }

        private void ReleasePointer(int pointerId) {
            if (Element.HasPointerCapture(pointerId)) Element.ReleasePointer(pointerId);
        }

        private void UnregisterCallbacks() {
            Element.UnregisterCallback<PointerDownEvent>(HandlePointerDownEvent);
            Element.UnregisterCallback<PointerMoveEvent>(HandlePointerMoveEvent);
            Element.UnregisterCallback<PointerUpEvent>(HandlePointerUpEvent);
            Element.UnregisterCallback<PointerCancelEvent>(HandlePointerCancelEvent);
            Element.UnregisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOutEvent);
        }

        private static PointerEventDetails ToDetails<TEvent>(PointerEventBase<TEvent> evt)
            where TEvent : PointerEventBase<TEvent>, new() {
            return new PointerEventDetails(evt.pointerId, evt.position, evt.localPosition, evt.button);
        }
    }
}
