#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>One typed destination in a <see cref="TabBar{T}"/>.</summary>
    public sealed class TabItem<T> {
        public TabItem(T value, string label)
            : this(value, new Text(ValidateLabel(label)), label) {
        }

        public TabItem(T value, Widget child, string semanticsLabel) {
            Value = value;
            Child = child ?? throw new ArgumentNullException(nameof(child));
            SemanticsLabel = string.IsNullOrWhiteSpace(semanticsLabel)
                ? throw new ArgumentException("Tab semantics label cannot be empty.", nameof(semanticsLabel))
                : semanticsLabel;
        }
        public T Value { get; }
        public Widget Child { get; }
        public string SemanticsLabel { get; }

        private static string ValidateLabel(string label) => string.IsNullOrWhiteSpace(label)
            ? throw new ArgumentException("Tab label cannot be empty.", nameof(label))
            : label;
    }

    /// <summary>A controlled horizontal tab strip. Compose the matching content beside or below it.</summary>
    public sealed class TabBar<T> : Widget {
        public TabBar(State<T> value, IReadOnlyList<TabItem<T>> items, Action<T> onChanged)
            : this(value, items, style: null, onChanged: onChanged) { }

        public TabBar(State<T> value, IReadOnlyList<TabItem<T>> items, TabBarStyle? style = null, Action<T>? onChanged = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            if (items is null || items.Count == 0) throw new ArgumentException("Tab bar requires at least one item.", nameof(items));
            var copy = new TabItem<T>[items.Count];
            var containsValue = false;
            for (var index = 0; index < items.Count; index++) {
                copy[index] = items[index] ?? throw new ArgumentException("Tab items cannot contain null.", nameof(items));
                for (var previous = 0; previous < index; previous++) {
                    if (EqualityComparer<T>.Default.Equals(copy[previous].Value, copy[index].Value)) {
                        throw new ArgumentException("Tab item values must be unique.", nameof(items));
                    }
                }
                containsValue |= EqualityComparer<T>.Default.Equals(copy[index].Value, value.Value);
            }
            if (!containsValue) throw new ArgumentException("The controlled value must match one of the supplied tabs.", nameof(value));
            Items = Array.AsReadOnly(copy);
            Style = style;
            OnChanged = onChanged;
        }
        public State<T> Value { get; }
        public IReadOnlyList<TabItem<T>> Items { get; }
        public TabBarStyle? Style { get; }
        public Action<T>? OnChanged { get; }
        internal override WidgetNode CreateNode() => new TabBarNode<T>(this);
    }

    /// <summary>Visual configuration for a <see cref="TabBar{T}"/>.</summary>
    public sealed class TabBarStyle {
        public TabBarStyle(Color? selectedForeground = null, Color? foreground = null, Color? indicatorColor = null, Color? dividerColor = null) {
            SelectedForeground = selectedForeground;
            Foreground = foreground;
            IndicatorColor = indicatorColor;
            DividerColor = dividerColor;
        }
        public Color? SelectedForeground { get; }
        public Color? Foreground { get; }
        public Color? IndicatorColor { get; }
        public Color? DividerColor { get; }
    }

    internal sealed class TabBarNode<T> : WidgetNode {
        private IDisposable? _subscription;
        private Widget[]? _children;
        private WidgetKey[]? _keys;
        private T _selectedValue = default!;
        private bool _hasSelectedValue;
        public TabBarNode(TabBar<T> widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.borderBottomWidth = 1f;
            ApplySurface(element, (TabBar<T>)Widget, context.Theme);
            return element;
        }

        protected override SemanticsProperties DescribeSemantics() => new(role: SemanticsRole.TabBar);

        protected override void OnMounted() {
            var widget = (TabBar<T>)Widget;
            RebuildAll(widget);
            _subscription = widget.Value.Subscribe(RebuildSelection);
            Bindings.Add(ReleaseSubscription);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TabBar<T> tabs) return false;
            var previous = (TabBar<T>)Widget;
            if (!ReferenceEquals(previous.Value, tabs.Value)) ReleaseSubscription();
            UpdateWidget(tabs);
            RebuildAll(tabs);
            if (!ReferenceEquals(previous.Value, tabs.Value)) _subscription = tabs.Value.Subscribe(RebuildSelection);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplySurface(Element, (TabBar<T>)Widget, Context.Theme);
            RebuildAll((TabBar<T>)Widget);
        }

        private void RebuildAll(TabBar<T> widget) {
            EnsureStorage(widget.Items.Count);
            for (var index = 0; index < widget.Items.Count; index++) {
                _children![index] = BuildButton(widget, index, IsSelected(widget.Value.Value, widget.Items[index].Value));
            }
            _selectedValue = widget.Value.Value;
            _hasSelectedValue = true;
            ReconcileChildren(_children!, Element);
        }

        private void RebuildSelection(T value) {
            var widget = (TabBar<T>)Widget;
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

        private Widget BuildButton(TabBar<T> widget, int index, bool selected) {
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
            for (var index = 0; index < count; index++) _keys[index] = new WidgetKey($"tab-{index}");
        }

        private static int FindItemIndex(TabBar<T> widget, T value) {
            for (var index = 0; index < widget.Items.Count; index++) {
                if (IsSelected(value, widget.Items[index].Value)) return index;
            }
            return -1;
        }

        private static bool IsSelected(T left, T right) => EqualityComparer<T>.Default.Equals(left, right);

        internal void HandleSelection(T value) {
            var widget = (TabBar<T>)Widget;
            if (EqualityComparer<T>.Default.Equals(widget.Value.Value, value)) return;
            ControlledInputChange.Commit(widget.Value, value, widget.OnChanged);
        }

        private static ButtonStyle StyleFor(bool selected, TabBarStyle? style, ThemeData? theme) {
            var themedStyle = theme?.TabBarTheme.Style;
            return new ButtonStyle(
            background: Color.clear,
            foreground: selected
                ? style?.SelectedForeground ?? themedStyle?.SelectedForeground
                    ?? theme?.Colors.Primary ?? new Color(0.15f, 0.39f, 0.92f)
                : style?.Foreground ?? themedStyle?.Foreground
                    ?? theme?.Colors.OnSurfaceVariant ?? new Color(0.42f, 0.45f, 0.51f),
            padding: EdgeInsets.Symmetric(horizontal: 12f, vertical: 8f),
            shape: BorderRadius.All(0f),
            typography: new TextStyle(fontSize: 12f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(0f, 34f),
            border: selected
                ? IndicatorBorder(style?.IndicatorColor ?? themedStyle?.IndicatorColor
                    ?? theme?.Colors.Primary ?? new Color(0.15f, 0.39f, 0.92f))
                : null);
        }

        private static void ApplySurface(VisualElement element, TabBar<T> widget, ThemeData? theme) =>
            element.style.borderBottomColor = widget.Style?.DividerColor
                ?? theme?.TabBarTheme.Style.DividerColor
                ?? theme?.Colors.Outline
                ?? new Color(0.85f, 0.87f, 0.91f);

        private static Border IndicatorBorder(Color color) => new(
            new BorderSide(Color.clear, 0f), new BorderSide(Color.clear, 0f),
            new BorderSide(Color.clear, 0f), new BorderSide(color, 2f));

        private void ReleaseSubscription() { _subscription?.Dispose(); _subscription = null; }
    }
}
