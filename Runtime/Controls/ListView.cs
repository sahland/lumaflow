#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>Defines whether a virtualized list exposes controlled item selection.</summary>
    public enum ListSelectionMode {
        None,
        Single,
        Multiple
    }

    /// <summary>
    /// Displays a typed collection through native UI Toolkit virtualization.
    /// </summary>
    public sealed class ListView<T> : Widget {
        public ListView(
            IReadOnlyList<T> items,
            Func<T, Widget> itemBuilder,
            float? itemHeight = null,
            Func<T, WidgetKey>? itemKey = null,
            ListSelectionMode selectionMode = ListSelectionMode.None,
            State<IReadOnlyList<WidgetKey>>? selectedKeys = null,
            Action<IReadOnlyList<WidgetKey>>? onSelectionChanged = null,
            ListViewController? controller = null)
            : this(items, ToIndexedBuilder(itemBuilder), itemHeight, itemKey, selectionMode,
                selectedKeys, onSelectionChanged, controller) {
        }

        public ListView(
            IReadOnlyList<T> items,
            Func<T, int, Widget> itemBuilder,
            float? itemHeight = null,
            Func<T, WidgetKey>? itemKey = null,
            ListSelectionMode selectionMode = ListSelectionMode.None,
            State<IReadOnlyList<WidgetKey>>? selectedKeys = null,
            Action<IReadOnlyList<WidgetKey>>? onSelectionChanged = null,
            ListViewController? controller = null) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
            if (itemHeight is { } height && (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)) {
                throw new ArgumentOutOfRangeException(nameof(itemHeight), "Item height must be finite and greater than zero.");
            }

            ValidateSelectionConfiguration(itemKey, selectionMode, selectedKeys, onSelectionChanged);
            Items = CopyAndValidateItems(items, itemKey);
            ItemHeight = itemHeight;
            ItemKey = itemKey;
            SelectionMode = selectionMode;
            SelectedKeys = selectedKeys;
            OnSelectionChanged = onSelectionChanged;
            Controller = controller;
        }

        public ListView(
            State<IReadOnlyList<T>> items,
            Func<T, Widget> itemBuilder,
            float? itemHeight = null,
            Func<T, WidgetKey>? itemKey = null,
            ListSelectionMode selectionMode = ListSelectionMode.None,
            State<IReadOnlyList<WidgetKey>>? selectedKeys = null,
            Action<IReadOnlyList<WidgetKey>>? onSelectionChanged = null,
            ListViewController? controller = null)
            : this(items, ToIndexedBuilder(itemBuilder), itemHeight, itemKey, selectionMode,
                selectedKeys, onSelectionChanged, controller) {
        }

        public ListView(
            State<IReadOnlyList<T>> items,
            Func<T, int, Widget> itemBuilder,
            float? itemHeight = null,
            Func<T, WidgetKey>? itemKey = null,
            ListSelectionMode selectionMode = ListSelectionMode.None,
            State<IReadOnlyList<WidgetKey>>? selectedKeys = null,
            Action<IReadOnlyList<WidgetKey>>? onSelectionChanged = null,
            ListViewController? controller = null)
            : this(GetCurrentItems(items), itemBuilder, itemHeight, itemKey, selectionMode,
                selectedKeys, onSelectionChanged, controller) {
            ItemsState = items;
        }

        /// <summary>
        /// Gets the immutable collection snapshot used by this list instance.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the optional state that replaces the complete list source reactively.
        /// In-place collection mutations are not observed.
        /// </summary>
        public State<IReadOnlyList<T>>? ItemsState { get; }

        /// <summary>
        /// Gets the builder invoked when a native row is bound to an item.
        /// It can run repeatedly as rows are recycled.
        /// </summary>
        public Func<T, int, Widget> ItemBuilder { get; }

        /// <summary>
        /// Gets the optional fixed native row height.
        /// </summary>
        public float? ItemHeight { get; }

        /// <summary>
        /// Gets the optional stable identity selector. Keys must be valid and unique in every snapshot.
        /// Keyed rows retain compatible mounted state while realized rows move during collection updates.
        /// </summary>
        public Func<T, WidgetKey>? ItemKey { get; }

        /// <summary>Gets the native selection policy.</summary>
        public ListSelectionMode SelectionMode { get; }

        /// <summary>
        /// Gets the externally owned selected-key state. Selection is controlled: user changes commit
        /// keys here before <see cref="OnSelectionChanged"/> is invoked.
        /// </summary>
        public State<IReadOnlyList<WidgetKey>>? SelectedKeys { get; }

        /// <summary>Gets the callback invoked after a user-originated selected-key commit.</summary>
        public Action<IReadOnlyList<WidgetKey>>? OnSelectionChanged { get; }

        /// <summary>Gets the optional externally owned scroll controller.</summary>
        public ListViewController? Controller { get; }

        internal override WidgetNode CreateNode() {
            return new ListViewNode<T>(this);
        }

        private static IReadOnlyList<T> GetCurrentItems(State<IReadOnlyList<T>> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            return items.Value ?? throw new ArgumentException("List items cannot be null.", nameof(items));
        }

        private static Func<T, int, Widget> ToIndexedBuilder(Func<T, Widget> itemBuilder) {
            if (itemBuilder is null) {
                throw new ArgumentNullException(nameof(itemBuilder));
            }

            return (item, _) => itemBuilder(item);
        }

        internal static IReadOnlyList<T> CopyAndValidateItems(
            IReadOnlyList<T> items,
            Func<T, WidgetKey>? itemKey) {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var copy = new List<T>(items);
            if (itemKey is not null) {
                var keyIndexes = new Dictionary<WidgetKey, int>();
                for (var index = 0; index < copy.Count; index++) {
                    var key = itemKey(copy[index]);
                    if (!key.IsValid) {
                        throw new ArgumentException("List item keys must be non-empty.", nameof(itemKey));
                    }
                    if (keyIndexes.TryGetValue(key, out var previousIndex)) {
                        throw new ArgumentException(
                            $"List item key '{key.Value}' is duplicated at indexes {previousIndex} and {index}. "
                            + "Return a stable unique WidgetKey for every item.",
                            nameof(items));
                    }
                    keyIndexes.Add(key, index);
                }
            }
            return copy.AsReadOnly();
        }

        private static void ValidateSelectionConfiguration(
            Func<T, WidgetKey>? itemKey,
            ListSelectionMode selectionMode,
            State<IReadOnlyList<WidgetKey>>? selectedKeys,
            Action<IReadOnlyList<WidgetKey>>? onSelectionChanged) {
            if (!Enum.IsDefined(typeof(ListSelectionMode), selectionMode)) {
                throw new ArgumentOutOfRangeException(nameof(selectionMode));
            }

            var hasSelection = selectionMode != ListSelectionMode.None
                || selectedKeys is not null
                || onSelectionChanged is not null;
            if (!hasSelection) return;
            if (selectionMode == ListSelectionMode.None) {
                throw new ArgumentException("A selection mode is required for controlled selection.", nameof(selectionMode));
            }
            if (itemKey is null) {
                throw new ArgumentException("Controlled selection requires stable item keys.", nameof(itemKey));
            }
            if (selectedKeys is null) {
                throw new ArgumentNullException(nameof(selectedKeys), "Controlled selection requires selected-key state.");
            }
        }
    }

}
