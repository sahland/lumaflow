#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>One typed choice in a <see cref="SegmentedControl{T}"/>.</summary>
    public sealed class SegmentedControlItem<T> {
        public SegmentedControlItem(T value, string label)
            : this(value, new Text(ValidateLabel(label)), label) {
        }

        public SegmentedControlItem(T value, Widget child, string semanticsLabel) {
            Value = value;
            Child = child ?? throw new ArgumentNullException(nameof(child));
            SemanticsLabel = string.IsNullOrWhiteSpace(semanticsLabel)
                ? throw new ArgumentException("Segment semantics label cannot be empty.", nameof(semanticsLabel))
                : semanticsLabel;
        }

        public T Value { get; }
        public Widget Child { get; }
        public string SemanticsLabel { get; }

        private static string ValidateLabel(string label) => string.IsNullOrWhiteSpace(label)
            ? throw new ArgumentException("Segment label cannot be empty.", nameof(label))
            : label;
    }

    /// <summary>A controlled horizontal selection control for a small set of mutually exclusive values.</summary>
    public sealed class SegmentedControl<T> : Widget {
        public SegmentedControl(State<T> value, IReadOnlyList<SegmentedControlItem<T>> items, Action<T> onChanged)
            : this(value, items, style: null, onChanged: onChanged) { }

        public SegmentedControl(
            State<T> value,
            IReadOnlyList<SegmentedControlItem<T>> items,
            SegmentedControlStyle? style = null,
            Action<T>? onChanged = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            if (items is null || items.Count == 0) throw new ArgumentException("Segmented control requires at least one item.", nameof(items));
            var copy = new SegmentedControlItem<T>[items.Count];
            var comparer = EqualityComparer<T>.Default;
            var containsValue = false;
            for (var index = 0; index < items.Count; index++) {
                copy[index] = items[index] ?? throw new ArgumentException("Segmented control items cannot contain null.", nameof(items));
                for (var previous = 0; previous < index; previous++) {
                    if (EqualityComparer<T>.Default.Equals(copy[previous].Value, copy[index].Value)) {
                        throw new ArgumentException("Segment values must be unique.", nameof(items));
                    }
                }
                containsValue |= comparer.Equals(copy[index].Value, value.Value);
            }
            if (!containsValue) throw new ArgumentException("The controlled value must match one of the supplied segments.", nameof(value));
            Value = value;
            Items = Array.AsReadOnly(copy);
            Style = style;
            OnChanged = onChanged;
        }

        public State<T> Value { get; }
        public IReadOnlyList<SegmentedControlItem<T>> Items { get; }
        public SegmentedControlStyle? Style { get; }
        public Action<T>? OnChanged { get; }
        internal override WidgetNode CreateNode() => new SegmentedControlNode<T>(this);
    }

    /// <summary>Visual configuration for a <see cref="SegmentedControl{T}"/>.</summary>
    public sealed class SegmentedControlStyle {
        public SegmentedControlStyle(
            Color? background = null,
            Color? selectedBackground = null,
            Color? selectedForeground = null,
            Color? foreground = null,
            BorderRadius? shape = null) {
            Background = background;
            SelectedBackground = selectedBackground;
            SelectedForeground = selectedForeground;
            Foreground = foreground;
            Shape = shape;
        }

        public Color? Background { get; }
        public Color? SelectedBackground { get; }
        public Color? SelectedForeground { get; }
        public Color? Foreground { get; }
        public BorderRadius? Shape { get; }
    }

    internal sealed class SegmentedControlNode<T> : WidgetNode {
        private IDisposable? _subscription;
        private Widget[]? _children;
        private WidgetKey[]? _keys;
        private T _selectedValue = default!;
        private bool _hasSelectedValue;

        public SegmentedControlNode(SegmentedControl<T> widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.paddingLeft = 3f;
            element.style.paddingRight = 3f;
            element.style.paddingTop = 3f;
            element.style.paddingBottom = 3f;
            ApplySurface(element, (SegmentedControl<T>)Widget, context.Theme);
            return element;
        }

        protected override void OnMounted() {
            var widget = (SegmentedControl<T>)Widget;
            RebuildAll(widget);
            _subscription = widget.Value.Subscribe(RebuildSelection);
            Bindings.Add(ReleaseSubscription);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not SegmentedControl<T> control) return false;
            var previous = (SegmentedControl<T>)Widget;
            if (!ReferenceEquals(previous.Value, control.Value)) ReleaseSubscription();
            UpdateWidget(control);
            RebuildAll(control);
            if (!ReferenceEquals(previous.Value, control.Value)) {
                _subscription = control.Value.Subscribe(RebuildSelection);
            }
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplySurface(Element, (SegmentedControl<T>)Widget, Context.Theme);
            RebuildAll((SegmentedControl<T>)Widget);
        }

        private void RebuildAll(SegmentedControl<T> widget) {
            EnsureStorage(widget.Items.Count);
            for (var index = 0; index < widget.Items.Count; index++) {
                _children![index] = BuildButton(widget, index, IsSelected(widget.Value.Value, widget.Items[index].Value));
            }
            _selectedValue = widget.Value.Value;
            _hasSelectedValue = true;
            ReconcileChildren(_children!, Element);
        }

        private void RebuildSelection(T value) {
            var widget = (SegmentedControl<T>)Widget;
            if (_children is null || _children.Length != widget.Items.Count || !_hasSelectedValue) {
                RebuildAll(widget);
                return;
            }

            var previousIndex = FindItemIndex(widget, _selectedValue);
            var selectedIndex = FindItemIndex(widget, value);
            if (previousIndex >= 0 && previousIndex != selectedIndex) {
                _children[previousIndex] = BuildButton(widget, previousIndex, selected: false);
            }
            if (selectedIndex >= 0 && selectedIndex != previousIndex) {
                _children[selectedIndex] = BuildButton(widget, selectedIndex, selected: true);
            }

            _selectedValue = value;
            ReconcileChildren(_children, Element);
        }

        private Widget BuildButton(SegmentedControl<T> widget, int index, bool selected) {
            var item = widget.Items[index];
            var value = item.Value;
            return new Button(
                    item.Child,
                    () => HandleSelection(value),
                    buttonStyle: StyleFor(selected, widget.Style, Context.Theme),
                    semanticsLabel: item.SemanticsLabel)
                .WithKey(_keys![index]);
        }

        private void EnsureStorage(int count) {
            if (_children?.Length == count && _keys?.Length == count) return;
            _children = new Widget[count];
            _keys = new WidgetKey[count];
            for (var index = 0; index < count; index++) _keys[index] = new WidgetKey($"segment-{index}");
        }

        private static int FindItemIndex(SegmentedControl<T> widget, T value) {
            for (var index = 0; index < widget.Items.Count; index++) {
                if (IsSelected(value, widget.Items[index].Value)) return index;
            }
            return -1;
        }

        private static bool IsSelected(T left, T right) => EqualityComparer<T>.Default.Equals(left, right);

        internal void HandleSelection(T value) {
            var widget = (SegmentedControl<T>)Widget;
            if (EqualityComparer<T>.Default.Equals(widget.Value.Value, value)) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private static ButtonStyle StyleFor(bool selected, SegmentedControlStyle? style, ThemeData? theme) {
            var themedStyle = theme?.SegmentedControlTheme.Style;
            return new ButtonStyle(
            background: selected
                ? style?.SelectedBackground ?? themedStyle?.SelectedBackground
                    ?? theme?.Colors.Primary ?? new Color(0.15f, 0.39f, 0.92f)
                : Color.clear,
            foreground: selected
                ? style?.SelectedForeground ?? themedStyle?.SelectedForeground
                    ?? theme?.Colors.OnPrimary ?? Color.white
                : style?.Foreground ?? themedStyle?.Foreground
                    ?? theme?.Colors.OnSurfaceVariant ?? new Color(0.29f, 0.34f, 0.42f),
            padding: EdgeInsets.Symmetric(horizontal: 12f, vertical: 6f),
            shape: style?.Shape ?? themedStyle?.Shape ?? BorderRadius.All(6f),
            typography: new TextStyle(fontSize: 12f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(0f, 30f));
        }

        private static void ApplySurface(VisualElement element, SegmentedControl<T> widget, ThemeData? theme) {
            var style = widget.Style;
            var themedStyle = theme?.SegmentedControlTheme.Style;
            var radius = style?.Shape ?? themedStyle?.Shape ?? BorderRadius.All(8f);
            element.style.backgroundColor = style?.Background ?? themedStyle?.Background
                ?? theme?.Colors.SurfaceVariant ?? new Color(0.94f, 0.95f, 0.97f);
            element.style.borderTopLeftRadius = radius.TopLeft;
            element.style.borderTopRightRadius = radius.TopRight;
            element.style.borderBottomRightRadius = radius.BottomRight;
            element.style.borderBottomLeftRadius = radius.BottomLeft;
        }

        private void ReleaseSubscription() {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
