#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>A controlled vertical navigation region for expanded layouts.</summary>
    public sealed class NavigationRail : Widget {
        private readonly NavigationDestination[] _destinations;

        public NavigationRail(
            State<int> selectedIndex,
            IReadOnlyList<NavigationDestination> destinations,
            Action<int> onDestinationSelected,
            NavigationRailMode mode = NavigationRailMode.Expanded) {
            SelectedIndex = selectedIndex ?? throw new ArgumentNullException(nameof(selectedIndex));
            OnDestinationSelected = onDestinationSelected ?? throw new ArgumentNullException(nameof(onDestinationSelected));
            if (destinations is null || destinations.Count == 0) {
                throw new ArgumentException("Navigation rail requires at least one destination.", nameof(destinations));
            }

            _destinations = new NavigationDestination[destinations.Count];
            for (var index = 0; index < destinations.Count; index++) {
                _destinations[index] = destinations[index] ?? throw new ArgumentException(
                    "Navigation destinations cannot contain null.",
                    nameof(destinations));
            }

            NavigationBar.ValidateSelectedIndex(SelectedIndex.Value, _destinations.Length);
            if (!Enum.IsDefined(typeof(NavigationRailMode), mode)) {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Mode = mode;
        }

        public State<int> SelectedIndex { get; }
        public IReadOnlyList<NavigationDestination> Destinations => _destinations;
        public NavigationRailMode Mode { get; }
        internal Action<int> OnDestinationSelected { get; }
        internal override WidgetNode CreateNode() => new NavigationRailNode(this);
    }

    internal sealed class NavigationRailNode : WidgetNode {
        private readonly UnityEngine.UIElements.Button[] _destinationElements;
        private readonly WidgetNode?[] _destinationNodes;
        private NavigationRailMode? _mode;
        private IDisposable? _selectionSubscription;

        public NavigationRailNode(NavigationRail widget)
            : base(widget) {
            _destinationElements = new UnityEngine.UIElements.Button[widget.Destinations.Count];
            _destinationNodes = new WidgetNode?[widget.Destinations.Count];
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var theme = context.Theme;
            var element = new VisualElement();
            element.AddToClassList("lumaflow-navigation-rail");
            element.style.paddingTop = 16f;
            element.style.paddingBottom = 16f;
            element.style.flexDirection = FlexDirection.Column;
            // Prevent navigation rail from shrinking or growing unexpectedly in a horizontal Row
            element.style.flexShrink = 0f;
            element.style.flexGrow = 0f;
            if (theme is not null) {
                element.style.backgroundColor = theme.Colors.Surface;
                element.style.borderRightColor = theme.Colors.Outline;
            }
            element.style.borderRightWidth = 1f;
            return element;
        }

        protected override void OnMounted() {
            var widget = (NavigationRail)Widget;
            for (var index = 0; index < widget.Destinations.Count; index++) {
                var button = CreateDestinationElement(index);
                _destinationElements[index] = button;
                Element.Add(button);
                _destinationNodes[index] = MountChild(BuildDestination(widget, index), button);
            }

            ApplySelection(widget.SelectedIndex.Value);
            ApplyMode(widget.Mode);
            BindSelection(widget);
            Bindings.Add(ReleaseSelection);
        }

        protected override SemanticsProperties DescribeSemantics() => new(
            role: SemanticsRole.TabBar);

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not NavigationRail navigation) return false;
            var previous = (NavigationRail)Widget;
            if (previous.Destinations.Count != navigation.Destinations.Count) return false;
            NavigationBar.ValidateSelectedIndex(navigation.SelectedIndex.Value, navigation.Destinations.Count);
            for (var index = 0; index < navigation.Destinations.Count; index++) {
                _destinationElements[index].tooltip = navigation.Destinations[index].Label;
                ReconcileSingleChild(
                    ref _destinationNodes[index],
                    BuildDestination(navigation, index),
                    _destinationElements[index]);
            }
            if (!ReferenceEquals(previous.SelectedIndex, navigation.SelectedIndex)) ReleaseSelection();
            UpdateWidget(navigation);
            if (!ReferenceEquals(previous.SelectedIndex, navigation.SelectedIndex)) BindSelection(navigation);
            _mode = null;
            ApplyMode(navigation.Mode);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyThemeAndSelection();
                return true;
            });
            return true;
        }

        internal void HandleDestinationSelected(int index) {
            var widget = (NavigationRail)Widget;
            NavigationBar.ValidateSelectedIndex(index, widget.Destinations.Count);
            if (IsMounted) widget.OnDestinationSelected(index);
        }

        internal void SetMode(NavigationRailMode mode) {
            if (!Enum.IsDefined(typeof(NavigationRailMode), mode)) {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (IsMounted) ApplyMode(mode);
        }

        private UnityEngine.UIElements.Button CreateDestinationElement(int index) {
            var button = new UnityEngine.UIElements.Button(() => HandleDestinationSelected(index));
            button.AddToClassList("lumaflow-navigation-rail__destination");
            button.tooltip = ((NavigationRail)Widget).Destinations[index].Label;
            button.style.height = 44f;
            button.style.marginBottom = 6f;
            button.style.paddingLeft = 12f;
            button.style.paddingRight = 12f;
            button.style.backgroundImage = StyleKeyword.None;
            button.style.borderTopWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.justifyContent = Justify.FlexStart;
            button.style.alignItems = UnityEngine.UIElements.Align.FlexStart;
            return button;
        }

        private void ApplyMode(NavigationRailMode mode) {
            if (_mode == mode) return;

            _mode = mode;
            var collapsed = mode == NavigationRailMode.Collapsed;
            var width = collapsed ? 72f : 208f;
            Element.style.width = width;
            Element.style.minWidth = width;
            Element.style.paddingLeft = collapsed ? 8f : 12f;
            Element.style.paddingRight = collapsed ? 8f : 12f;
            for (var index = 0; index < _destinationElements.Length; index++) {
                var button = _destinationElements[index];
                var row = button.childCount == 0 ? null : button[0];
                if (row is null) continue;

                row.style.justifyContent = collapsed ? Justify.Center : Justify.FlexStart;
                button.style.justifyContent = collapsed ? Justify.Center : Justify.FlexStart;
                button.style.alignItems = collapsed ? UnityEngine.UIElements.Align.Center : UnityEngine.UIElements.Align.FlexStart;
                button.style.paddingLeft = collapsed ? 0f : 12f;
                button.style.paddingRight = collapsed ? 0f : 12f;
                row[1].style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void ApplySelection(int selectedIndex) {
            var widget = (NavigationRail)Widget;
            NavigationBar.ValidateSelectedIndex(selectedIndex, widget.Destinations.Count);
            var theme = Context.Theme;
            for (var index = 0; index < _destinationElements.Length; index++) {
                var button = _destinationElements[index];
                var row = button.childCount == 0 ? null : button[0];
                if (row is null) continue;

                var selected = index == selectedIndex;
                var color = selected ? theme?.Colors.Primary : theme?.Colors.OnSurfaceVariant;
                button.style.backgroundColor = selected
                    ? theme?.Colors.SurfaceVariant ?? Color.clear
                    : Color.clear;
                ((UnityEngine.UIElements.Image)row[0]).tintColor = color ?? Color.white;
                var label = (Label)row[1];
                label.style.color = color ?? Color.white;
                label.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyThemeAndSelection();
        }

        private Widget BuildDestination(NavigationRail widget, int index) {
            var destination = widget.Destinations[index];
            return new Semantics(
                new Row(new Widget[]
                {
                new Icon(destination.Icon, size: 18f),
                new Text(destination.Label, new TextStyle(fontSize: 14f))
                }, gap: 12f, crossAxisAlignment: CrossAxisAlignment.Center),
                new SemanticsProperties(
                    label: destination.Label,
                    role: SemanticsRole.Tab,
                    selected: widget.SelectedIndex.Value == index,
                    onSelect: () => HandleDestinationSelected(index)),
                excludeDescendantSemantics: true);
        }

        private void BindSelection(NavigationRail widget) =>
            _selectionSubscription = widget.SelectedIndex.Subscribe(HandleSelectionChanged);

        private void HandleSelectionChanged(int selectedIndex) {
            var widget = (NavigationRail)Widget;
            for (var index = 0; index < widget.Destinations.Count; index++) {
                ReconcileSingleChild(
                    ref _destinationNodes[index],
                    BuildDestination(widget, index),
                    _destinationElements[index]);
            }
            ApplySelection(selectedIndex);
        }

        private void ReleaseSelection() {
            _selectionSubscription?.Dispose();
            _selectionSubscription = null;
        }

        private void ApplyThemeAndSelection() {
            var theme = Context.Theme;
            Element.style.backgroundColor = theme is null ? StyleKeyword.Null : theme.Colors.Surface;
            Element.style.borderRightColor = theme is null ? StyleKeyword.Null : theme.Colors.Outline;
            ApplySelection(((NavigationRail)Widget).SelectedIndex.Value);
        }
    }

}
