#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class ListViewNode<T> : WidgetNode {
        private readonly List<ItemHostState> _activeHostStates = new();
        private IReadOnlyList<T> _items = null!;
        private UnityEngine.UIElements.ListView? _listView;
        private IDisposable? _itemsSubscription;
        private IDisposable? _selectionSubscription;
        private IDisposable? _controllerAttachment;
        private IDisposable? _controllerOffsetSubscription;
        private IVisualElementScheduledItem? _pendingCleanup;
        private UnityEngine.UIElements.ScrollView? _nativeScrollView;
        private bool _applyingSelection;
        private bool _applyingControllerOffset;
        private bool _updatingControllerFromNative;

        public ListViewNode(ListView<T> widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (ListView<T>)Widget;
            _items = widget.Items;
            _listView = new UnityEngine.UIElements.ListView {
                itemsSource = new List<T>(_items),
                makeItem = MakeItem,
                bindItem = BindItem,
                unbindItem = UnbindItem,
                destroyItem = DestroyItem
            };
            _listView.selectedIndicesChanged += HandleSelectedIndicesChanged;
            _listView.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            ApplyConfiguration(widget);
            return _listView;
        }

        protected override void OnMounted() {
            Bindings.Add(ReleaseItemHostsOnOwnerUnmount);
            Bindings.Add(ReleaseAllBindings);
            BindItemsState((ListView<T>)Widget);
            BindSelection((ListView<T>)Widget);
            BindController((ListView<T>)Widget);
        }

        protected override SemanticsProperties DescribeSemantics() => new(
            role: SemanticsRole.ScrollView);

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not ListView<T> next) return false;
            var previous = (ListView<T>)Widget;
            var nextItems = ListView<T>.CopyAndValidateItems(next.ItemsState?.Value ?? next.Items, next.ItemKey);
            var itemStateChanged = !ReferenceEquals(previous.ItemsState, next.ItemsState);
            var selectionChanged = !ReferenceEquals(previous.SelectedKeys, next.SelectedKeys);
            var controllerChanged = !ReferenceEquals(previous.Controller, next.Controller);
            var keySelectorChanged = (previous.ItemKey is null) != (next.ItemKey is null);

            if (keySelectorChanged || next.ItemKey is null) UnmountAllItems();
            else PrepareRowsForKeyedRefresh();
            if (itemStateChanged) ReleaseItemsState();
            if (selectionChanged) ReleaseSelection();
            if (controllerChanged) ReleaseController();

            UpdateWidget(next);
            ReplaceNativeItems(nextItems);
            ApplyConfiguration(next);
            if (itemStateChanged) BindItemsState(next);
            if (selectionChanged) BindSelection(next);
            if (controllerChanged) BindController(next);
            _listView!.RefreshItems();
            SchedulePendingCleanup();
            ApplySelection(next.SelectedKeys?.Value);
            ApplyControllerOffset(next.Controller?.Offset);
            return true;
        }

        internal void BindItem(VisualElement host, int index) {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (index < 0 || index >= _items.Count) throw new ArgumentOutOfRangeException(nameof(index));

            var widget = (ListView<T>)Widget;
            var hostState = GetHostState(host);
            var key = widget.ItemKey?.Invoke(_items[index]);
            var itemWidget = widget.ItemBuilder(_items[index], index)
                ?? throw new InvalidOperationException("List item builder returned null.");
            if (key is { } stableKey) itemWidget = itemWidget.WithKey(stableKey);

            if (key is { } desiredKey && !Nullable.Equals(hostState.Key, desiredKey)) {
                var matchingState = FindPendingState(desiredKey, hostState);
                if (matchingState is not null) SwapRows(hostState, matchingState);
            }

            var sameIdentity = key is { }
                ? Nullable.Equals(hostState.Key, key)
                : hostState.Key is null && hostState.Index == index;
            if (hostState.ItemNode is not null && sameIdentity) {
                var current = hostState.ItemNode;
                ReconcileSingleChild(ref current, itemWidget, host);
                hostState.ItemNode = current;
            } else {
                UnmountItem(hostState);
                hostState.ItemNode = MountChild(itemWidget, host);
            }

            hostState.Key = key;
            hostState.Index = index;
            hostState.PendingRebind = false;
            if (!_activeHostStates.Contains(hostState)) _activeHostStates.Add(hostState);
        }

        internal void UnbindItem(VisualElement host, int index) {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var hostState = GetHostState(host);
            if (!hostState.PendingRebind) UnmountItem(hostState);
        }

        internal void DestroyItem(VisualElement host) {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var hostState = GetHostState(host);
            UnmountItem(hostState);
            _activeHostStates.Remove(hostState);
            host.userData = null;
        }

        internal int ActiveItemNodeCount {
            get {
                var count = 0;
                foreach (var state in _activeHostStates) {
                    if (state.ItemNode is not null) count++;
                }
                return count;
            }
        }

        internal void HandleSelectedIndicesChanged(IEnumerable<int> indices) {
            if (_applyingSelection) return;
            var widget = (ListView<T>)Widget;
            if (widget.SelectionMode == ListSelectionMode.None
                || widget.SelectedKeys is null
                || widget.ItemKey is null) return;

            var keys = new List<WidgetKey>();
            var seen = new HashSet<WidgetKey>();
            foreach (var index in indices) {
                if (index < 0 || index >= _items.Count) continue;
                var key = widget.ItemKey(_items[index]);
                if (seen.Add(key)) keys.Add(key);
                if (widget.SelectionMode == ListSelectionMode.Single) break;
            }
            var value = keys.AsReadOnly();
            ControlledInputChange.Commit(widget.SelectedKeys, value, widget.OnSelectionChanged);
        }

        private static VisualElement MakeItem() {
            var host = new VisualElement();
            host.userData = new ItemHostState(host);
            return host;
        }

        private void UpdateItems(IReadOnlyList<T> items) {
            var widget = (ListView<T>)Widget;
            var nextItems = ListView<T>.CopyAndValidateItems(items, widget.ItemKey);
            if (widget.ItemKey is null) UnmountAllItems();
            else PrepareRowsForKeyedRefresh();
            ReplaceNativeItems(nextItems);
            _listView!.RefreshItems();
            SchedulePendingCleanup();
            ApplySelection(widget.SelectedKeys?.Value);
        }

        private void ReplaceNativeItems(IReadOnlyList<T> items) {
            _items = items;
            _listView!.itemsSource = new List<T>(_items);
        }

        private void ApplyConfiguration(ListView<T> widget) {
            if (widget.ItemHeight is { } itemHeight) {
                _listView!.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
                _listView.fixedItemHeight = itemHeight;
            } else {
                _listView!.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            }
            _listView!.selectionType = widget.SelectionMode switch {
                ListSelectionMode.Single => SelectionType.Single,
                ListSelectionMode.Multiple => SelectionType.Multiple,
                _ => SelectionType.None
            };
        }

        private void BindItemsState(ListView<T> widget) {
            if (widget.ItemsState is { } state) _itemsSubscription = state.Subscribe(UpdateItems);
        }

        private void ReleaseItemsState() {
            _itemsSubscription?.Dispose();
            _itemsSubscription = null;
        }

        private void BindSelection(ListView<T> widget) {
            if (widget.SelectedKeys is not { } state) return;
            _selectionSubscription = state.Subscribe(ApplySelection);
            ApplySelection(state.Value);
        }

        private void ReleaseSelection() {
            _selectionSubscription?.Dispose();
            _selectionSubscription = null;
        }

        private void ApplySelection(IReadOnlyList<WidgetKey>? selectedKeys) {
            if (_listView is null) return;
            var widget = (ListView<T>)Widget;
            if (widget.SelectionMode == ListSelectionMode.None || widget.ItemKey is null) {
                _applyingSelection = true;
                try { _listView.ClearSelection(); } finally { _applyingSelection = false; }
                return;
            }
            if (selectedKeys is null) throw new ArgumentException("Selected keys cannot be null.", nameof(selectedKeys));
            if (widget.SelectionMode == ListSelectionMode.Single && selectedKeys.Count > 1) {
                throw new ArgumentException("Single selection accepts at most one selected key.", nameof(selectedKeys));
            }

            var selected = new HashSet<WidgetKey>();
            foreach (var key in selectedKeys) {
                if (!key.IsValid || !selected.Add(key)) {
                    throw new ArgumentException("Selected keys must be valid and unique.", nameof(selectedKeys));
                }
            }
            var indices = new List<int>();
            for (var index = 0; index < _items.Count; index++) {
                if (selected.Contains(widget.ItemKey(_items[index]))) indices.Add(index);
            }
            _applyingSelection = true;
            try { _listView.SetSelectionWithoutNotify(indices); }
            finally { _applyingSelection = false; }
        }

        private void BindController(ListView<T> widget) {
            if (widget.Controller is not { } controller) return;
            _controllerAttachment = controller.Attach(ScrollToItem);
            _controllerOffsetSubscription = controller.SubscribeOffset(offset => {
                if (!_updatingControllerFromNative) ApplyControllerOffset(offset);
            });
            TryBindNativeScrollView();
            ApplyControllerOffset(controller.Offset);
        }

        private void ReleaseController() {
            if (_nativeScrollView is not null) {
                _nativeScrollView.verticalScroller.valueChanged -= HandleNativeScrollOffset;
                _nativeScrollView = null;
            }
            _controllerOffsetSubscription?.Dispose();
            _controllerOffsetSubscription = null;
            _controllerAttachment?.Dispose();
            _controllerAttachment = null;
        }

        private bool ScrollToItem(WidgetKey key) {
            var widget = (ListView<T>)Widget;
            if (widget.ItemKey is null) return false;
            for (var index = 0; index < _items.Count; index++) {
                if (!widget.ItemKey(_items[index]).Equals(key)) continue;
                _listView!.ScrollToItem(index);
                return true;
            }
            return false;
        }

        private void TryBindNativeScrollView() {
            if (_nativeScrollView is not null || _listView is null) return;
            _nativeScrollView = _listView.Q<UnityEngine.UIElements.ScrollView>();
            if (_nativeScrollView is not null) {
                _nativeScrollView.verticalScroller.valueChanged += HandleNativeScrollOffset;
            }
        }

        private void ApplyControllerOffset(float? offset) {
            if (offset is null) return;
            TryBindNativeScrollView();
            if (_nativeScrollView is null) return;
            _applyingControllerOffset = true;
            try {
                var current = _nativeScrollView.scrollOffset;
                _nativeScrollView.scrollOffset = new Vector2(current.x, offset.Value);
            } finally {
                _applyingControllerOffset = false;
            }
        }

        internal void HandleNativeScrollOffset(float offset) {
            if (_applyingControllerOffset) return;
            _updatingControllerFromNative = true;
            try {
                ((ListView<T>)Widget).Controller?.SetOffsetFromNative(offset);
            } finally {
                _updatingControllerFromNative = false;
            }
        }

        private void HandleGeometryChanged(GeometryChangedEvent _) {
            var controller = ((ListView<T>)Widget).Controller;
            if (controller is not null) ApplyControllerOffset(controller.Offset);
        }

        private void PrepareRowsForKeyedRefresh() {
            CleanupPendingRows();
            foreach (var state in _activeHostStates) {
                if (state.ItemNode is not null) state.PendingRebind = true;
            }
        }

        private ItemHostState? FindPendingState(WidgetKey key, ItemHostState except) {
            foreach (var state in _activeHostStates) {
                if (!ReferenceEquals(state, except) && state.PendingRebind && Nullable.Equals(state.Key, key)) return state;
            }
            return null;
        }

        private void SwapRows(ItemHostState target, ItemHostState matching) {
            var displacedNode = target.ItemNode;
            var displacedKey = target.Key;
            var displacedIndex = target.Index;
            var desiredNode = matching.ItemNode
                ?? throw new InvalidOperationException("A pending keyed row must own a mounted node.");

            MoveChildToNativeParent(desiredNode, target.Host);
            if (displacedNode is not null) MoveChildToNativeParent(displacedNode, matching.Host);

            target.ItemNode = desiredNode;
            target.Key = matching.Key;
            target.Index = matching.Index;
            matching.ItemNode = displacedNode;
            matching.Key = displacedKey;
            matching.Index = displacedIndex;
            matching.PendingRebind = displacedNode is not null;
            if (displacedNode is null) _activeHostStates.Remove(matching);
        }

        private void SchedulePendingCleanup() {
            _pendingCleanup?.Pause();
            _pendingCleanup = _listView!.schedule.Execute(CleanupPendingRows);
        }

        private void CleanupPendingRows() {
            _pendingCleanup?.Pause();
            _pendingCleanup = null;
            var snapshot = new List<ItemHostState>(_activeHostStates);
            foreach (var state in snapshot) {
                if (state.PendingRebind) UnmountItem(state);
            }
        }

        private void UnmountAllItems() {
            _pendingCleanup?.Pause();
            _pendingCleanup = null;
            var snapshot = new List<ItemHostState>(_activeHostStates);
            foreach (var state in snapshot) UnmountItem(state);
        }

        private void ReleaseItemHostsOnOwnerUnmount() {
            _pendingCleanup?.Pause();
            _pendingCleanup = null;
            foreach (var state in _activeHostStates) {
                state.ItemNode = null;
                state.Key = null;
                state.Index = -1;
                state.PendingRebind = false;
            }
            _activeHostStates.Clear();
        }

        private ItemHostState GetHostState(VisualElement host) {
            if (host.userData is ItemHostState state) return state;
            state = new ItemHostState(host);
            host.userData = state;
            return state;
        }

        private void UnmountItem(ItemHostState hostState) {
            if (hostState.ItemNode is not null) {
                var itemNode = hostState.ItemNode;
                hostState.ItemNode = null;
                UnmountChild(itemNode);
            }
            hostState.Key = null;
            hostState.Index = -1;
            hostState.PendingRebind = false;
            _activeHostStates.Remove(hostState);
        }

        private void ReleaseAllBindings() {
            _listView?.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            if (_listView is not null) _listView.selectedIndicesChanged -= HandleSelectedIndicesChanged;
            ReleaseItemsState();
            ReleaseSelection();
            ReleaseController();
        }

        private sealed class ItemHostState {
            public ItemHostState(VisualElement host) => Host = host;
            public VisualElement Host { get; }
            public WidgetNode? ItemNode { get; set; }
            public WidgetKey? Key { get; set; }
            public int Index { get; set; } = -1;
            public bool PendingRebind { get; set; }
        }
    }
}
