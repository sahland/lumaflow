#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class OverlayHostNode : WidgetNode {
        private readonly List<Entry> _entries = new();
        private VisualElement? _contentContainer;
        private VisualElement? _overlayContainer;
        private WidgetNode? _contentNode;

        internal bool HasOpenEntries => _entries.Count > 0;

        public OverlayHostNode(OverlayHost widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var root = new VisualElement();
            root.style.position = Position.Relative;
            root.style.flexGrow = 1f;
            root.style.flexShrink = 1f;
            root.style.minWidth = 0f;
            root.style.minHeight = 0f;
            root.style.flexDirection = FlexDirection.Column;
            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1f;
            _contentContainer.style.flexShrink = 1f;
            _contentContainer.style.minWidth = 0f;
            _contentContainer.style.minHeight = 0f;
            _overlayContainer = new VisualElement { pickingMode = PickingMode.Ignore };
            _overlayContainer.style.position = Position.Absolute;
            _overlayContainer.style.left = 0f;
            _overlayContainer.style.right = 0f;
            _overlayContainer.style.top = 0f;
            _overlayContainer.style.bottom = 0f;
            root.Add(_contentContainer);
            root.Add(_overlayContainer);
            return root;
        }

        protected override void OnMounted() {
            var widget = (OverlayHost)Widget;
            _contentNode = MountChild(widget.Child, _contentContainer!);
            widget.Controller.Attach(this);
            Bindings.Add(() => widget.Controller.Detach(this));
            Bindings.Add(CloseAll);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not OverlayHost host) return false;
            if (!ReferenceEquals(((OverlayHost)Widget).Controller, host.Controller)) return false;
            ReconcileSingleChild(ref _contentNode, host.Child, _contentContainer!);
            UpdateWidget(host);
            return true;
        }

        internal OverlayHandle Show(Widget content) {
            return Show(content, isModal: false, ModalOptions.Default);
        }

        internal OverlayHandle ShowModal(Widget content, ModalOptions options) {
            return Show(content, isModal: true, options);
        }

        internal OverlayHandle ShowDrawer(Widget content, DrawerPlacement placement, ModalOptions options) {
            var handle = Show(content, isModal: true, options, out var entry);
            entry.Host.AddToClassList("lumaflow-drawer-host");
            entry.Host.style.justifyContent = Justify.Center;
            entry.Host.style.alignItems = placement == DrawerPlacement.Left
                ? UnityEngine.UIElements.Align.FlexStart
                : UnityEngine.UIElements.Align.FlexEnd;
            return handle;
        }

        internal OverlayHandle ShowPopover(VisualElement anchor, Widget content, PopoverPlacement placement) {
            if (anchor.parent is null) {
                throw new InvalidOperationException("A popover anchor must be attached to a UI Toolkit hierarchy.");
            }

            var host = new VisualElement { pickingMode = PickingMode.Ignore };
            host.style.position = Position.Absolute;
            var barrier = new VisualElement { pickingMode = PickingMode.Position };
            barrier.style.position = Position.Absolute;
            barrier.style.left = 0f;
            barrier.style.right = 0f;
            barrier.style.top = 0f;
            barrier.style.bottom = 0f;
            var positioner = new VisualElement();
            positioner.AddToClassList("lumaflow-popover");
            positioner.style.position = Position.Absolute;
            host.Add(positioner);

            var entry = new Entry(host, barrier, isModal: false, ModalOptions.Default);
            var handle = new OverlayHandle(() => Close(entry));
            entry.Handle = handle;
            void CloseOnOutsidePointer(PointerDownEvent _) => handle.Close();
            barrier.RegisterCallback<PointerDownEvent>(CloseOnOutsidePointer);
            _overlayContainer!.Add(barrier);
            _overlayContainer!.Add(host);
            try {
                entry.Node = content.CreateNode();
                entry.Node.Mount(this, Context, positioner);
                _entries.Add(entry);
                RecomputeInputOwnership();

                void Reposition(GeometryChangedEvent _) => PositionPopover(entry, anchor, positioner, placement);
                void CloseWhenAnchorDetaches(DetachFromPanelEvent _) => handle.Close();

                anchor.RegisterCallback<GeometryChangedEvent>(Reposition);
                anchor.RegisterCallback<DetachFromPanelEvent>(CloseWhenAnchorDetaches);
                positioner.RegisterCallback<GeometryChangedEvent>(Reposition);
                host.RegisterCallback<GeometryChangedEvent>(Reposition);
                entry.Cleanup = () =>
                {
                    anchor.UnregisterCallback<GeometryChangedEvent>(Reposition);
                    anchor.UnregisterCallback<DetachFromPanelEvent>(CloseWhenAnchorDetaches);
                    positioner.UnregisterCallback<GeometryChangedEvent>(Reposition);
                    host.UnregisterCallback<GeometryChangedEvent>(Reposition);
                    barrier.UnregisterCallback<PointerDownEvent>(CloseOnOutsidePointer);
                };

                PositionPopover(entry, anchor, positioner, placement);
                return handle;
            } catch (Exception mountFailure) {
                var cleanupFailures = CleanupFailedEntry(entry);
                if (cleanupFailures is { Count: > 0 }) {
                    cleanupFailures.Insert(0, mountFailure);
                    throw new InvalidOperationException(
                        "Popover failed to mount and its rollback also reported cleanup failures.",
                        new AggregateException(cleanupFailures));
                }
                throw;
            }
        }

        internal OverlayHandle ShowToast(Widget content, TimeSpan duration) {
            var handle = Show(content, isModal: false, ModalOptions.Default, out var entry);
            entry.Host.AddToClassList("lumaflow-toast-host");
            entry.Host.style.justifyContent = Justify.FlexEnd;
            entry.Host.style.alignItems = UnityEngine.UIElements.Align.Center;
            PositionToasts();

            var delayMilliseconds = checked((long)Math.Ceiling(duration.TotalMilliseconds));
            var scheduledClose = entry.Host.schedule.Execute(handle.Close).StartingIn(delayMilliseconds);
            var previousCleanup = entry.Cleanup;
            entry.Cleanup = () =>
            {
                previousCleanup?.Invoke();
                scheduledClose.Pause();
            };
            return handle;
        }

        internal bool TryCloseTop() {
            if (_entries.Count == 0) return false;
            _entries[_entries.Count - 1].Handle!.Close();
            return true;
        }

        internal bool TryHandleBack() {
            if (_entries.Count == 0) return false;
            var top = _entries[_entries.Count - 1];
            if (!top.Options.DismissOnBack) return false;
            top.Handle!.Close();
            return true;
        }

        private OverlayHandle Show(Widget content, bool isModal, ModalOptions options) {
            return Show(content, isModal, options, out _);
        }

        private OverlayHandle Show(Widget content, bool isModal, ModalOptions options, out Entry entry) {
            var host = new VisualElement { pickingMode = PickingMode.Ignore };
            host.style.position = Position.Absolute;
            host.style.left = 0f; host.style.right = 0f; host.style.top = 0f; host.style.bottom = 0f;
            VisualElement? barrier = null;
            if (isModal) {
                host.style.justifyContent = Justify.Center;
                host.style.alignItems = UnityEngine.UIElements.Align.Center;
                barrier = new VisualElement { pickingMode = PickingMode.Position };
                barrier.AddToClassList("lumaflow-modal-barrier");
                barrier.style.position = Position.Absolute;
                barrier.style.left = 0f; barrier.style.right = 0f; barrier.style.top = 0f; barrier.style.bottom = 0f;
                barrier.style.backgroundColor = options.BarrierColor;
                _overlayContainer!.Add(barrier);
            }

            var createdEntry = new Entry(host, barrier, isModal, options) {
                PreviouslyFocused = CaptureFocusedElement()
            };
            entry = createdEntry;
            var handle = new OverlayHandle(() => Close(createdEntry));
            createdEntry.Handle = handle;
            if (barrier is not null && options.DismissOnBarrier) {
                void CloseOnBarrierClick(ClickEvent _) => handle.Close();
                barrier.RegisterCallback<ClickEvent>(CloseOnBarrierClick);
                createdEntry.Cleanup = () => barrier.UnregisterCallback<ClickEvent>(CloseOnBarrierClick);
            }
            _overlayContainer!.Add(host);
            try {
                createdEntry.Node = content.CreateNode();
                createdEntry.Node.Mount(this, Context, host);
                _entries.Add(createdEntry);
                RecomputeInputOwnership();
                if (isModal && options.RequestFocus) FocusFirstFocusable(host);
                return handle;
            } catch (Exception mountFailure) {
                var cleanupFailures = CleanupFailedEntry(createdEntry);
                if (cleanupFailures is { Count: > 0 }) {
                    cleanupFailures.Insert(0, mountFailure);
                    throw new InvalidOperationException(
                        "Overlay failed to mount and its rollback also reported cleanup failures.",
                        new AggregateException(cleanupFailures));
                }
                throw;
            }
        }

        private List<Exception>? CleanupFailedEntry(Entry entry) {
            _entries.Remove(entry);
            PositionToasts();
            List<Exception>? failures = null;
            void TryCleanup(Action cleanup) {
                try { cleanup(); }
                catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (entry.Cleanup is { } cleanup) TryCleanup(cleanup);
            if (entry.Node is { } node) TryCleanup(node.UnmountAfterFailedMount);
            TryCleanup(entry.Host.RemoveFromHierarchy);
            if (entry.Barrier is { } barrier) TryCleanup(barrier.RemoveFromHierarchy);
            entry.Handle?.CloseFromHost();
            RecomputeInputOwnership();
            return failures;
        }

        private void Close(Entry entry) {
            if (!_entries.Remove(entry)) return;
            PositionToasts();
            List<Exception>? failures = null;
            try {
                entry.Handle!.CloseFromHost();
            } catch (Exception exception) {
                failures = new List<Exception> { exception };
            }

            try {
                entry.Cleanup?.Invoke();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                entry.Node!.UnmountAfterFailedMount();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                entry.Host.RemoveFromHierarchy();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                entry.Barrier?.RemoveFromHierarchy();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            RecomputeInputOwnership();
            RestoreFocus(entry);

            if (failures is { Count: 1 }) {
                throw new InvalidOperationException("Overlay closed, but one owned resource failed to clean up.", failures[0]);
            }

            if (failures is { Count: > 1 }) {
                throw new InvalidOperationException(
                    "Overlay closed, but multiple owned resources failed to clean up.",
                    new AggregateException(failures));
            }
        }

        private void PositionPopover(Entry entry, VisualElement anchor, VisualElement positioner, PopoverPlacement placement) {
            if (!_entries.Contains(entry)) return;
            if (anchor.parent is null) {
                entry.Handle?.Close();
                return;
            }

            var viewport = _overlayContainer!.worldBound;
            var anchorBounds = anchor.worldBound;
            var popoverBounds = positioner.worldBound;
            var popoverWidth = popoverBounds.width;
            var popoverHeight = popoverBounds.height;
            var resolvedPlacement = ResolvePlacement(placement, anchorBounds, viewport, popoverWidth, popoverHeight);
            var position = GetPosition(resolvedPlacement, anchorBounds, popoverWidth, popoverHeight);
            var maxLeft = viewport.xMax - popoverWidth;
            var maxTop = viewport.yMax - popoverHeight;

            position.x = Clamp(position.x, viewport.xMin, maxLeft);
            position.y = Clamp(position.y, viewport.yMin, maxTop);
            positioner.style.left = position.x - viewport.xMin;
            positioner.style.top = position.y - viewport.yMin;
        }

        private static PopoverPlacement ResolvePlacement(PopoverPlacement placement, UnityEngine.Rect anchor, UnityEngine.Rect viewport, float width, float height) {
            if (placement != PopoverPlacement.Auto) return placement;
            if (viewport.yMax - anchor.yMax >= height || anchor.yMin - viewport.yMin < height) return PopoverPlacement.BottomStart;
            return PopoverPlacement.TopStart;
        }

        private static UnityEngine.Vector2 GetPosition(PopoverPlacement placement, UnityEngine.Rect anchor, float width, float height) {
            return placement switch {
                PopoverPlacement.BottomStart => new UnityEngine.Vector2(anchor.xMin, anchor.yMax),
                PopoverPlacement.Bottom => new UnityEngine.Vector2(anchor.center.x - width / 2f, anchor.yMax),
                PopoverPlacement.BottomEnd => new UnityEngine.Vector2(anchor.xMax - width, anchor.yMax),
                PopoverPlacement.TopStart => new UnityEngine.Vector2(anchor.xMin, anchor.yMin - height),
                PopoverPlacement.Top => new UnityEngine.Vector2(anchor.center.x - width / 2f, anchor.yMin - height),
                PopoverPlacement.TopEnd => new UnityEngine.Vector2(anchor.xMax - width, anchor.yMin - height),
                PopoverPlacement.LeftStart => new UnityEngine.Vector2(anchor.xMin - width, anchor.yMin),
                PopoverPlacement.Left => new UnityEngine.Vector2(anchor.xMin - width, anchor.center.y - height / 2f),
                PopoverPlacement.LeftEnd => new UnityEngine.Vector2(anchor.xMin - width, anchor.yMax - height),
                PopoverPlacement.RightStart => new UnityEngine.Vector2(anchor.xMax, anchor.yMin),
                PopoverPlacement.Right => new UnityEngine.Vector2(anchor.xMax, anchor.center.y - height / 2f),
                PopoverPlacement.RightEnd => new UnityEngine.Vector2(anchor.xMax, anchor.yMax - height),
                _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unsupported popover placement.")
            };
        }

        private static float Clamp(float value, float min, float max) {
            return max < min ? min : UnityEngine.Mathf.Clamp(value, min, max);
        }

        private void CloseAll() {
            var entries = _entries.ToArray();
            List<Exception>? failures = null;
            for (var index = entries.Length - 1; index >= 0; index--) {
                try {
                    Close(entries[index]);
                } catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures is { Count: 1 }) throw failures[0];
            if (failures is { Count: > 1 }) {
                throw new AggregateException("Multiple overlays failed while closing.", failures);
            }
        }

        private VisualElement? CaptureFocusedElement() =>
            Element.panel?.focusController.focusedElement as VisualElement;

        private void PositionToasts() {
            var index = 0;
            foreach (var entry in _entries) {
                if (!entry.Host.ClassListContains("lumaflow-toast-host")) continue;
                entry.Host.style.bottom = 24f + index * 76f;
                index++;
            }
        }

        private void RecomputeInputOwnership() {
            var topModal = -1;
            for (var index = _entries.Count - 1; index >= 0; index--) {
                if (_entries[index].IsModal) {
                    topModal = index;
                    break;
                }
            }

            var contentEnabled = topModal < 0;
            _contentContainer!.SetEnabled(contentEnabled);
            _contentContainer.pickingMode = contentEnabled ? PickingMode.Position : PickingMode.Ignore;
            for (var index = 0; index < _entries.Count; index++) {
                var interactive = topModal < 0 || index >= topModal;
                _entries[index].Host.SetEnabled(interactive);
                _entries[index].Host.pickingMode = PickingMode.Ignore;
                if (_entries[index].Barrier is { } barrier) {
                    barrier.pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;
                }
            }
        }

        private static void RestoreFocus(Entry entry) {
            if (!entry.Options.RestoreFocus || entry.PreviouslyFocused is not { } focused) return;
            if (focused.panel is null || !focused.enabledInHierarchy) return;
            focused.Focus();
        }

        private static bool FocusFirstFocusable(VisualElement root) {
            if (root.focusable && root.enabledInHierarchy) {
                root.Focus();
                return true;
            }
            for (var index = 0; index < root.childCount; index++) {
                if (FocusFirstFocusable(root[index])) return true;
            }
            return false;
        }

        private sealed class Entry {
            public Entry(VisualElement host, VisualElement? barrier, bool isModal, ModalOptions options) {
                Host = host;
                Barrier = barrier;
                IsModal = isModal;
                Options = options;
            }
            public VisualElement Host { get; }
            public VisualElement? Barrier { get; }
            public bool IsModal { get; }
            public ModalOptions Options { get; }
            public WidgetNode? Node { get; set; }
            public OverlayHandle? Handle { get; set; }
            public Action? Cleanup { get; set; }
            public VisualElement? PreviouslyFocused { get; set; }
        }
    }

}
