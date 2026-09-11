#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {
    public readonly struct WidgetKey : IEquatable<WidgetKey> {
        public WidgetKey(string value) {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Key is required.", nameof(value))
                : value;
        }

        public string Value { get; }

        /// <summary>
        /// Whether this instance was created with a non-empty key value.
        /// A default struct value is invalid.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(WidgetKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is WidgetKey other && Equals(other);

        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    }

    public sealed class KeyedChild {
        public KeyedChild(WidgetKey key, Widget child) {
            if (!key.IsValid) {
                throw new ArgumentException("A keyed child requires a valid WidgetKey.", nameof(key));
            }

            Key = key;
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public WidgetKey Key { get; }

        public Widget Child { get; }
    }

    public sealed class KeyedColumn : Widget {
        public KeyedColumn(State<IReadOnlyList<KeyedChild>> children, float gap = 0f) {
            Children = children ?? throw new ArgumentNullException(nameof(children));
            Gap = ValidateGap(gap);
        }

        public State<IReadOnlyList<KeyedChild>> Children { get; }

        public float Gap { get; }

        internal override WidgetNode CreateNode() => new KeyedColumnNode(this);

        private static float ValidateGap(float gap) => float.IsNaN(gap) || float.IsInfinity(gap) || gap < 0f
            ? throw new ArgumentOutOfRangeException(nameof(gap), "Gap must be finite and non-negative.")
            : gap;
    }

    /// <summary>
    /// Arranges keyed children horizontally while retaining compatible stateful children.
    /// </summary>
    public sealed class KeyedRow : Widget {
        public KeyedRow(State<IReadOnlyList<KeyedChild>> children, float gap = 0f) {
            Children = children ?? throw new ArgumentNullException(nameof(children));
            Gap = ValidateGap(gap);
        }

        public State<IReadOnlyList<KeyedChild>> Children { get; }

        public float Gap { get; }

        internal override WidgetNode CreateNode() => new KeyedRowNode(this);

        private static float ValidateGap(float gap) => float.IsNaN(gap) || float.IsInfinity(gap) || gap < 0f
            ? throw new ArgumentOutOfRangeException(nameof(gap), "Gap must be finite and non-negative.")
            : gap;
    }

    /// <summary>
    /// Reconciles a direct native flex hierarchy by key. The concrete layout nodes own
    /// only axis-specific flex direction and child spacing.
    /// </summary>
    internal abstract class KeyedFlexNode : WidgetNode, IFlexParentNode {
        private IDisposable? _childrenSubscription;

        protected KeyedFlexNode(Widget widget)
            : base(widget) {
        }

        protected abstract State<IReadOnlyList<KeyedChild>> ChildrenState { get; }

        protected abstract float Gap { get; }

        protected abstract FlexDirection Direction { get; }

        protected abstract string LayoutName { get; }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexDirection = Direction;
            return element;
        }

        protected override void OnMounted() {
            Reconcile(ChildrenState.Value, Gap);
            BindChildrenState(ChildrenState);
            Bindings.Add(ReleaseChildrenState);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || !TryReadConfiguration(nextWidget, out var children, out var gap)) {
                return false;
            }

            var previousState = ChildrenState;
            Reconcile(children.Value, gap);
            if (!ReferenceEquals(previousState, children)) ReleaseChildrenState();
            UpdateWidget(nextWidget);
            if (!ReferenceEquals(previousState, children)) BindChildrenState(children);
            RefreshChildSpacing();
            return true;
        }

        public void RefreshChildSpacing() {
            for (var index = 0; index < Element.childCount; index++) {
                ApplyChildSpacing(Element[index], index < Element.childCount - 1 ? Gap : 0f);
            }
        }

        protected abstract void ApplyChildSpacing(VisualElement child, float spacing);

        protected abstract bool TryReadConfiguration(
            Widget widget,
            out State<IReadOnlyList<KeyedChild>> children,
            out float gap);

        private void Reconcile(IReadOnlyList<KeyedChild> children) => Reconcile(children, Gap);

        private void Reconcile(IReadOnlyList<KeyedChild> children, float gap) {
            var keyIndexes = new Dictionary<WidgetKey, int>();
            var widgets = new Widget[children.Count];
            for (var index = 0; index < children.Count; index++) {
                var child = children[index]
                    ?? throw new ArgumentException($"{LayoutName} children cannot contain null.", nameof(children));
                if (keyIndexes.TryGetValue(child.Key, out var previousIndex)) {
                    throw new InvalidOperationException(
                        $"{LayoutName} received duplicate key '{child.Key.Value}' at indexes "
                        + $"{previousIndex} and {index}. Assign a unique WidgetKey to each child.");
                }
                keyIndexes.Add(child.Key, index);
                widgets[index] = new KeyedSubtree(child.Key, child.Child);
            }

            ReconcileChildren(widgets, Element);
            for (var index = 0; index < Element.childCount; index++) {
                ApplyChildSpacing(Element[index], index < Element.childCount - 1 ? gap : 0f);
            }
        }

        private void BindChildrenState(State<IReadOnlyList<KeyedChild>> state) =>
            _childrenSubscription = state.Subscribe(Reconcile);

        private void ReleaseChildrenState() {
            _childrenSubscription?.Dispose();
            _childrenSubscription = null;
        }

    }

    internal sealed class KeyedColumnNode : KeyedFlexNode {
        public KeyedColumnNode(KeyedColumn widget)
            : base(widget) {
        }

        protected override State<IReadOnlyList<KeyedChild>> ChildrenState =>
            ((KeyedColumn)Widget).Children;

        protected override float Gap => ((KeyedColumn)Widget).Gap;

        protected override FlexDirection Direction => FlexDirection.Column;

        protected override string LayoutName => nameof(KeyedColumn);

        protected override bool TryReadConfiguration(
            Widget widget,
            out State<IReadOnlyList<KeyedChild>> children,
            out float gap) {
            if (widget is KeyedColumn column) {
                children = column.Children;
                gap = column.Gap;
                return true;
            }
            children = null!;
            gap = 0f;
            return false;
        }

        protected override void ApplyChildSpacing(VisualElement child, float spacing) {
            child.style.marginBottom = spacing;
        }
    }

    internal sealed class KeyedRowNode : KeyedFlexNode {
        public KeyedRowNode(KeyedRow widget)
            : base(widget) {
        }

        protected override State<IReadOnlyList<KeyedChild>> ChildrenState =>
            ((KeyedRow)Widget).Children;

        protected override float Gap => ((KeyedRow)Widget).Gap;

        protected override FlexDirection Direction => FlexDirection.Row;

        protected override string LayoutName => nameof(KeyedRow);

        protected override bool TryReadConfiguration(
            Widget widget,
            out State<IReadOnlyList<KeyedChild>> children,
            out float gap) {
            if (widget is KeyedRow row) {
                children = row.Children;
                gap = row.Gap;
                return true;
            }
            children = null!;
            gap = 0f;
            return false;
        }

        protected override void ApplyChildSpacing(VisualElement child, float spacing) {
            child.style.marginRight = spacing;
        }
    }
}
