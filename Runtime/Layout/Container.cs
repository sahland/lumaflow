#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Hosts one child widget and can apply optional box decoration and padding.
    /// </summary>
    public sealed class Container : Widget {
        public Container(Widget child, BoxDecoration? decoration = null, EdgeInsets? padding = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Decoration = decoration;
            Padding = padding;
        }

        public Widget Child { get; }

        public BoxDecoration? Decoration { get; }

        /// <summary>
        /// Gets the optional inner padding.
        /// </summary>
        public EdgeInsets? Padding { get; }

        internal override WidgetNode CreateNode() {
            return new ContainerNode(this);
        }
    }

    /// <summary>
    /// A themed elevated surface for grouping related content.
    /// </summary>
    public sealed class Card : Widget {
        public Card(
            Widget child,
            EdgeInsets? padding = null,
            Color? backgroundColor = null,
            BorderRadius? borderRadius = null,
            Border? border = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Padding = padding;
            BackgroundColor = backgroundColor;
            BorderRadius = borderRadius;
            Border = border;
        }

        /// <summary>Creates a card from the shared box-decoration contract.</summary>
        public Card(Widget child, BoxDecoration decoration, EdgeInsets? padding = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Padding = padding;
            BackgroundColor = decoration.BackgroundColor;
            BorderRadius = decoration.BorderRadius;
            Border = decoration.Border;
        }

        public Widget Child { get; }
        public EdgeInsets? Padding { get; }
        public Color? BackgroundColor { get; }
        public BorderRadius? BorderRadius { get; }
        public Border? Border { get; }
        internal override WidgetNode CreateNode() => new CardNode(this);
    }

    internal sealed class CardNode : SingleChildWidgetNode<Card> {
        public CardNode(Card widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var card = (Card)Widget;
            var element = new VisualElement();
            ApplyCard(element, card);
            return element;
        }

        protected override Widget GetChild(Card widget) => widget.Child;

        protected override void ApplyConfiguration(Card widget) => ApplyCard(Element, widget);

        protected override void OnInheritedChanged(InheritedAspect aspect) => ApplyCard(Element, (Card)Widget);

        private void ApplyCard(VisualElement element, Card card) {
            BoxDecorationStyleMapper.Clear(element);
            PaddingStyleMapper.Clear(element);
            var requiresTheme = card.BackgroundColor is null || card.BorderRadius is null || card.Padding is null;
            var theme = requiresTheme ? Context.Theme : null;
            var background = card.BackgroundColor ?? theme?.Colors.Surface;
            var radius = card.BorderRadius ?? theme?.Radius.Small;
            BoxDecorationStyleMapper.Apply(element, new BoxDecoration(background, radius, card.Border));
            var padding = card.Padding ?? (theme is null ? (EdgeInsets?)null : EdgeInsets.All(theme.Spacing.Medium));
            if (padding is { } insets) PaddingStyleMapper.Apply(element, insets);
        }
    }

    /// <summary>
    /// A standard settings-style row with title, optional subtitle, and optional trailing content.
    /// </summary>
    public sealed class ListTile : Widget {
        public ListTile(
            string title,
            string? subtitle = null,
            Widget? trailing = null,
            Action? onPressed = null,
            Widget? leading = null,
            TextStyle? titleStyle = null,
            TextStyle? subtitleStyle = null
        ) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Subtitle = subtitle;
            Leading = leading;
            Trailing = trailing;
            TitleStyle = titleStyle;
            SubtitleStyle = subtitleStyle;
            OnPressed = onPressed;
        }

        /// <summary>Creates a tile whose primary and supporting content are arbitrary widgets.</summary>
        public ListTile(
            Widget title,
            Widget? subtitle = null,
            Widget? leading = null,
            Widget? trailing = null,
            Action? onPressed = null) {
            TitleWidget = title ?? throw new ArgumentNullException(nameof(title));
            SubtitleWidget = subtitle;
            Leading = leading;
            Trailing = trailing;
            OnPressed = onPressed;
        }

        public string? Title { get; }
        public string? Subtitle { get; }

        public Widget? TitleWidget { get; }
        public Widget? SubtitleWidget { get; }

        public Widget? Leading { get; }
        public Widget? Trailing { get; }

        public TextStyle? TitleStyle { get; }
        public TextStyle? SubtitleStyle { get; }

        internal Action? OnPressed { get; }
        internal override WidgetNode CreateNode() => new ListTileNode(this);
    }

    internal sealed class ListTileNode : WidgetNode, IFlexParentNode {
        private VisualElement? _tile;

        private WidgetNode? _leadingNode;
        private WidgetNode? _textNode;
        private WidgetNode? _trailingNode;

        private bool _clickHandlerBound;

        public ListTileNode(ListTile widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            _tile = new VisualElement();
            _tile.style.flexDirection = FlexDirection.Row;
            _tile.style.alignItems = UnityEngine.UIElements.Align.Center;
            _tile.style.justifyContent = Justify.SpaceBetween;
            _tile.focusable = ((ListTile)Widget).OnPressed is not null;
            return _tile;
        }

        protected override void OnMounted() {
            var widget = (ListTile)Widget;

            if (widget.Leading is not null) {
                _leadingNode =
                    MountChild(widget.Leading, Element);
            }

            _textNode =
                MountChild(BuildText(widget), Element);

            if (widget.Trailing is not null) {
                _trailingNode =
                    MountChild(widget.Trailing, Element);
            }

            ApplyInteraction(widget);

            Bindings.Add(ReleaseClickHandler);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) ||
                nextWidget is not ListTile tile) {
                return false;
            }
            ReevaluateInheritedDependencies(() => {
                ReconcileOptionalLeading(ref _leadingNode, tile.Leading);

                ReconcileSingleChild(
                    ref _textNode,
                    BuildText(tile),
                    Element);

                ReconcileOptionalTrailing(
                    ref _trailingNode,
                    tile.Trailing);

                return true;
            });

            ReleaseClickHandler();
            UpdateWidget(tile);
            ApplyInteraction(tile);

            return true;
        }

        internal void HandleClicked(ClickEvent _) {
            if (IsMounted) ((ListTile)Widget).OnPressed?.Invoke();
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ReconcileSingleChild(ref _textNode, BuildText((ListTile)Widget), Element);
        }

        public void RefreshChildSpacing() {}

        private Widget BuildText(ListTile widget) {
            var theme = Context.Theme;
            var titleStyle =
                widget.TitleStyle ??
                theme?.Typography.Headline;
            var subtitleStyle =
                widget.SubtitleStyle ??
                theme?.Typography.Body;
            var title = widget.TitleWidget ?? new Text(widget.Title!, titleStyle);
            var subtitle = widget.SubtitleWidget
                ?? (widget.Subtitle is null ? null : new Text(widget.Subtitle, subtitleStyle));
            var text = new Column(
                subtitle is null
                    ? new[] { title }
                    : new[] { title, subtitle },
                gap: theme?.Spacing.ExtraSmall ?? 0f);
            return new Expanded(text);
        }

        private void ReconcileOptionalLeading(ref WidgetNode? current, Widget? leading) {
            if (leading is null) {
                var previous = current;
                current = null;
                if (previous is not null) UnmountChild(previous);
                return;
            }
            ReconcileSingleChild(ref current, leading, Element);
            MoveChildToIndex(current!, 0);
        }

        private void ReconcileOptionalTrailing(ref WidgetNode? current, Widget? trailing) {
            if (trailing is null) {
                var previous = current;
                current = null;
                if (previous is not null) UnmountChild(previous);
                return;
            }
            ReconcileSingleChild(ref current, trailing, Element);
        }

        private void ApplyInteraction(ListTile widget) {
            _tile!.focusable = widget.OnPressed is not null;
            if (widget.OnPressed is null) return;
            _tile.RegisterCallback<ClickEvent>(HandleClicked);
            _clickHandlerBound = true;
        }

        private void ReleaseClickHandler() {
            if (!_clickHandlerBound) return;
            _tile!.UnregisterCallback<ClickEvent>(HandleClicked);
            _clickHandlerBound = false;
        }
    }

    /// <summary>
    /// A themed top application bar with a title and optional action widgets.
    /// </summary>
    public sealed class AppBar : Widget {
        private readonly Widget[] _actions;

        public AppBar(Widget title, IReadOnlyList<Widget>? actions = null) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            _actions = WidgetCollection.Copy(actions, nameof(actions));
        }

        public AppBar(string title, IReadOnlyList<Widget>? actions = null) {
            TitleText = title ?? throw new ArgumentNullException(nameof(title));
            Title = null;
            _actions = WidgetCollection.Copy(actions, nameof(actions));
        }

        public Widget? Title { get; }
        public string? TitleText { get; }
        public IReadOnlyList<Widget> Actions => _actions;
        internal override WidgetNode CreateNode() => new AppBarNode(this);
    }

    internal sealed class AppBarNode : WidgetNode, IFlexParentNode {
        private static readonly WidgetKey TitleKey = new("app-bar.title");
        private static readonly WidgetKey SpacerKey = new("app-bar.spacer");

        public AppBarNode(AppBar widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var theme = context.Theme;
            var element = new VisualElement();
            element.style.height = 64f;
            element.style.flexShrink = 0f;
            element.style.paddingLeft = theme?.Spacing.Large ?? 20f;
            element.style.paddingRight = theme?.Spacing.Large ?? 20f;
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = UnityEngine.UIElements.Align.Center;
            if (theme is not null) element.style.borderBottomColor = theme.Colors.Outline;
            element.style.borderBottomWidth = 1f;
            return element;
        }

        protected override void OnMounted() {
            ReconcileChildren(BuildChildren((AppBar)Widget), Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not AppBar appBar) return false;
            ReconcileChildren(BuildChildren(appBar), Element);
            UpdateWidget(appBar);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyTheme();
                return true;
            });
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyTheme();
        }

        private IReadOnlyList<Widget> BuildChildren(AppBar widget) {
            var children = new Widget[widget.Actions.Count + 2];
            children[0] = (widget.Title ?? new Text(widget.TitleText!, Context.Theme?.Typography.Headline)).WithKey(TitleKey);
            children[1] = new Spacer().WithKey(SpacerKey);
            for (var index = 0; index < widget.Actions.Count; index++) {
                children[index + 2] = widget.Actions[index];
            }
            return children;
        }

        private void ApplyTheme() {
            var theme = Context.Theme;
            Element.style.paddingLeft = theme?.Spacing.Large ?? 20f;
            Element.style.paddingRight = theme?.Spacing.Large ?? 20f;
            Element.style.borderBottomColor = theme is null ? StyleKeyword.Null : theme.Colors.Outline;
            if (((AppBar)Widget).TitleText is null) return;
            var title = (Label)Element[0];
            title.style.color = StyleKeyword.Null;
            title.style.fontSize = StyleKeyword.Null;
            title.style.unityFontStyleAndWeight = StyleKeyword.Null;
            if (theme?.Typography.Headline is { } headline) TextStyleMapper.Apply(title, headline, Context.TextScaler);
        }

        public void RefreshChildSpacing() {
        }
    }

    /// <summary>Describes one destination in a <see cref="NavigationBar"/>.</summary>
    public sealed class NavigationDestination {
        public NavigationDestination(string label, IconData icon) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            if (!icon.IsDefined) throw new ArgumentException("Navigation destination requires an icon.", nameof(icon));
            Icon = icon;
        }

        public string Label { get; }
        public IconData Icon { get; }
    }

    /// <summary>A themed bottom navigation region.</summary>
    public sealed class NavigationBar : Widget {
        private readonly NavigationDestination[] _destinations;

        public NavigationBar(
            State<int> selectedIndex,
            IReadOnlyList<NavigationDestination> destinations,
            Action<int> onDestinationSelected) {
            SelectedIndex = selectedIndex ?? throw new ArgumentNullException(nameof(selectedIndex));
            OnDestinationSelected = onDestinationSelected ?? throw new ArgumentNullException(nameof(onDestinationSelected));
            if (destinations is null || destinations.Count == 0) {
                throw new ArgumentException("Navigation bar requires at least one destination.", nameof(destinations));
            }

            _destinations = new NavigationDestination[destinations.Count];
            for (var i = 0; i < destinations.Count; i++) {
                _destinations[i] = destinations[i] ?? throw new ArgumentException(
                    "Navigation destinations cannot contain null.",
                    nameof(destinations));
            }

            ValidateSelectedIndex(SelectedIndex.Value, _destinations.Length);
        }

        public State<int> SelectedIndex { get; }
        public IReadOnlyList<NavigationDestination> Destinations => _destinations;
        internal Action<int> OnDestinationSelected { get; }
        internal override WidgetNode CreateNode() => new NavigationBarNode(this);

        internal static void ValidateSelectedIndex(int index, int destinationCount) {
            if (index < 0 || index >= destinationCount) {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Selected index must reference a navigation destination.");
            }
        }
    }

    internal sealed class NavigationBarNode : WidgetNode {
        private readonly UnityEngine.UIElements.Button[] _destinationElements;
        private readonly WidgetNode?[] _destinationNodes;
        private IDisposable? _selectionSubscription;

        public NavigationBarNode(NavigationBar widget) : base(widget) {
            _destinationElements = new UnityEngine.UIElements.Button[widget.Destinations.Count];
            _destinationNodes = new WidgetNode?[widget.Destinations.Count];
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var theme = context.Theme;
            var element = new VisualElement();
            element.style.height = 60f;
            // Navigation is fixed application chrome. Scrollable content must yield
            // before it does when a viewport is shorter than the route content.
            element.style.flexShrink = 0f;
            element.style.paddingLeft = 26f;
            element.style.paddingRight = 26f;
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = UnityEngine.UIElements.Align.Center;
            element.style.justifyContent = Justify.SpaceBetween;
            if (theme is not null) element.style.backgroundColor = theme.Colors.Surface;
            return element;
        }

        protected override void OnMounted() {
            var widget = (NavigationBar)Widget;
            for (var index = 0; index < widget.Destinations.Count; index++) {
                var button = CreateDestinationElement(index);
                _destinationElements[index] = button;
                Element.Add(button);
                _destinationNodes[index] = MountChild(BuildDestination(widget, index), button);
            }
            ApplySelection(widget.SelectedIndex.Value);
            BindSelection(widget);
            Bindings.Add(ReleaseSelection);
        }

        protected override SemanticsProperties DescribeSemantics() => new(
            role: SemanticsRole.TabBar);

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not NavigationBar navigation) return false;
            var previous = (NavigationBar)Widget;
            if (previous.Destinations.Count != navigation.Destinations.Count) return false;
            NavigationBar.ValidateSelectedIndex(navigation.SelectedIndex.Value, navigation.Destinations.Count);
            for (var index = 0; index < navigation.Destinations.Count; index++) {
                ReconcileSingleChild(
                    ref _destinationNodes[index],
                    BuildDestination(navigation, index),
                    _destinationElements[index]);
            }
            if (!ReferenceEquals(previous.SelectedIndex, navigation.SelectedIndex)) ReleaseSelection();
            UpdateWidget(navigation);
            if (!ReferenceEquals(previous.SelectedIndex, navigation.SelectedIndex)) BindSelection(navigation);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyThemeAndSelection();
                return true;
            });
            return true;
        }

        internal void HandleDestinationSelected(int index) {
            var widget = (NavigationBar)Widget;
            NavigationBar.ValidateSelectedIndex(index, widget.Destinations.Count);
            if (IsMounted) widget.OnDestinationSelected(index);
        }

        private UnityEngine.UIElements.Button CreateDestinationElement(int index) {
            var button = new UnityEngine.UIElements.Button(() => HandleDestinationSelected(index));
            button.style.backgroundImage = StyleKeyword.None;
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.marginTop = 0f;
            button.style.marginRight = 0f;
            button.style.marginBottom = 0f;
            button.style.marginLeft = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingBottom = 0f;
            button.style.paddingLeft = 0f;
            button.style.flexGrow = 1f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = UnityEngine.UIElements.Align.Center;
            return button;
        }

        private void ApplySelection(int selectedIndex) {
            var widget = (NavigationBar)Widget;
            NavigationBar.ValidateSelectedIndex(selectedIndex, widget.Destinations.Count);
            var theme = Context.Theme;
            for (var index = 0; index < _destinationElements.Length; index++) {
                var row = _destinationElements[index].childCount == 0 ? null : _destinationElements[index][0];
                if (row is null) continue;
                var color = index == selectedIndex ? theme?.Colors.Primary : theme?.Colors.OnSurfaceVariant;
                ((UnityEngine.UIElements.Image)row[0]).tintColor = color ?? Color.white;
                var label = (Label)row[1];
                label.style.color = color ?? Color.white;
                label.style.unityFontStyleAndWeight = index == selectedIndex ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyThemeAndSelection();
        }

        private Widget BuildDestination(NavigationBar widget, int index) {
            var destination = widget.Destinations[index];
            return new Semantics(
                new Row(new Widget[]
                {
                new Icon(destination.Icon, size: 16f),
                new Text(destination.Label, new TextStyle(fontSize: 13f))
                }, gap: 6f, crossAxisAlignment: CrossAxisAlignment.Center),
                new SemanticsProperties(
                    label: destination.Label,
                    role: SemanticsRole.Tab,
                    selected: widget.SelectedIndex.Value == index,
                    onSelect: () => HandleDestinationSelected(index)),
                excludeDescendantSemantics: true);
        }

        private void BindSelection(NavigationBar widget) =>
            _selectionSubscription = widget.SelectedIndex.Subscribe(HandleSelectionChanged);

        private void HandleSelectionChanged(int selectedIndex) {
            var widget = (NavigationBar)Widget;
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
            ApplySelection(((NavigationBar)Widget).SelectedIndex.Value);
        }
    }

    /// <summary>
    /// Defines the primary screen regions: app bar, scrollable body, and navigation bar.
    /// </summary>
    public sealed class Scaffold : Widget {
        public Scaffold(Widget body, AppBar? appBar = null, NavigationBar? navigationBar = null) {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            AppBar = appBar;
            NavigationBar = navigationBar;
        }

        public Widget Body { get; }
        public AppBar? AppBar { get; }
        public NavigationBar? NavigationBar { get; }
        internal override WidgetNode CreateNode() => new ScaffoldNode(this);
    }

    internal sealed class ScaffoldNode : WidgetNode, IFlexParentNode {
        private static readonly WidgetKey AppBarKey = new("scaffold.app-bar");
        private static readonly WidgetKey BodyKey = new("scaffold.body");
        private static readonly WidgetKey NavigationBarKey = new("scaffold.navigation-bar");

        public ScaffoldNode(Scaffold widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Column;
            element.style.flexGrow = 1f;
            element.style.flexShrink = 1f;
            element.style.minWidth = 0f;
            element.style.minHeight = 0f;
            if (context.Theme is { } theme) element.style.backgroundColor = theme.Colors.Canvas;
            return element;
        }

        protected override void OnMounted() {
            ReconcileChildren(BuildChildren((Scaffold)Widget), Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Scaffold scaffold) return false;
            ReconcileChildren(BuildChildren(scaffold), Element);
            UpdateWidget(scaffold);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyTheme();
                return true;
            });
            return true;
        }

        public void RefreshChildSpacing() {
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyTheme();
        }

        private static IReadOnlyList<Widget> BuildChildren(Scaffold widget) {
            var children = new List<Widget>(3);
            if (widget.AppBar is not null) children.Add(widget.AppBar.WithKey(AppBarKey));
            children.Add(new Expanded(widget.Body).WithKey(BodyKey));
            if (widget.NavigationBar is not null) children.Add(widget.NavigationBar.WithKey(NavigationBarKey));
            return children;
        }

        private void ApplyTheme() {
            var theme = Context.Theme;
            Element.style.backgroundColor = theme is null ? StyleKeyword.Null : theme.Colors.Canvas;
        }
    }

    internal static class WidgetCollection {
        public static Widget[] Copy(IReadOnlyList<Widget>? widgets, string parameterName) {
            if (widgets is null) return Array.Empty<Widget>();
            var copy = new Widget[widgets.Count];
            for (var i = 0; i < widgets.Count; i++) {
                copy[i] = widgets[i] ?? throw new ArgumentException("Widgets cannot contain null.", parameterName);
            }
            return copy;
        }
    }

}
