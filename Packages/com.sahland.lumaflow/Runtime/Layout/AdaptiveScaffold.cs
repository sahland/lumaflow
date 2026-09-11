#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Keeps an application shell mounted while selecting a rail or bottom navigation
    /// presentation from the resolved available width.
    /// </summary>
    public sealed class AdaptiveScaffold : Widget {
        public AdaptiveScaffold(
            Widget body,
            NavigationBar navigationBar,
            NavigationRail navigationRail,
            AppBar? appBar = null,
            float railBreakpoint = 840f,
            float expandedRailBreakpoint = 1100f) {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            NavigationBar = navigationBar ?? throw new ArgumentNullException(nameof(navigationBar));
            NavigationRail = navigationRail ?? throw new ArgumentNullException(nameof(navigationRail));
            AppBar = appBar;
            if (float.IsNaN(railBreakpoint) || float.IsInfinity(railBreakpoint) || railBreakpoint <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(railBreakpoint));
            }
            if (float.IsNaN(expandedRailBreakpoint)
                || float.IsInfinity(expandedRailBreakpoint)
                || expandedRailBreakpoint <= railBreakpoint) {
                throw new ArgumentOutOfRangeException(nameof(expandedRailBreakpoint));
            }

            RailBreakpoint = railBreakpoint;
            ExpandedRailBreakpoint = expandedRailBreakpoint;
        }

        public Widget Body { get; }
        public AppBar? AppBar { get; }
        public NavigationBar NavigationBar { get; }
        public NavigationRail NavigationRail { get; }
        public float RailBreakpoint { get; }
        public float ExpandedRailBreakpoint { get; }
        internal override WidgetNode CreateNode() => new AdaptiveScaffoldNode(this);
    }

    internal sealed class AdaptiveScaffoldNode : WidgetNode {
        private WidgetNode? _appBarNode;
        private WidgetNode? _bodyNode;
        private WidgetNode? _navigationBarNode;
        private NavigationRailNode? _navigationRailNode;
        private VisualElement? _contentContainer;
        private VisualElement? _bodySlot;
        private bool? _isRailVisible;
        private bool _isUpdateScheduled;
        private IVisualElementScheduledItem? _scheduledUpdate;

        public AdaptiveScaffoldNode(AdaptiveScaffold widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var element = new VisualElement();
            element.style.flexGrow = 1f;
            element.style.flexShrink = 1f;
            element.style.minWidth = 0f;
            element.style.minHeight = 0f;
            element.style.flexDirection = FlexDirection.Column;
            if (context.Theme is { } theme) element.style.backgroundColor = theme.Colors.Canvas;
            return element;
        }

        protected override void OnMounted() {
            var widget = (AdaptiveScaffold)Widget;
            if (widget.AppBar is not null) _appBarNode = MountChild(widget.AppBar, Element);

            _contentContainer = new VisualElement();
            _contentContainer.style.flexDirection = FlexDirection.Row;
            _contentContainer.style.flexGrow = 1f;
            _contentContainer.style.flexShrink = 1f;
            _contentContainer.style.minWidth = 0f;
            _contentContainer.style.minHeight = 0f;
            Element.Add(_contentContainer);
            _navigationRailNode = (NavigationRailNode)MountChild(widget.NavigationRail, _contentContainer);

            _bodySlot = new VisualElement();
            _bodySlot.style.flexDirection = FlexDirection.Column;
            _bodySlot.style.flexGrow = 1f;
            _bodySlot.style.flexShrink = 1f;
            _bodySlot.style.minWidth = 0f;
            _bodySlot.style.minHeight = 0f;
            _contentContainer.Add(_bodySlot);
            _bodyNode = MountChild(widget.Body, _bodySlot);
            _navigationBarNode = MountChild(widget.NavigationBar, Element);

            Element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Bindings.Add(() => Element.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged));
            Bindings.Add(() => _scheduledUpdate?.Pause());
            ApplyNavigationPresentation();
            ScheduleNavigationPresentation();
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not AdaptiveScaffold scaffold) return false;
            ReconcileOptionalAppBar(ref _appBarNode, scaffold.AppBar);
            ReconcileSingleChild(
                ref _navigationRailNode,
                scaffold.NavigationRail,
                _contentContainer!);
            ReconcileSingleChild(ref _bodyNode, scaffold.Body, _bodySlot!);
            ReconcileSingleChild(
                ref _navigationBarNode,
                scaffold.NavigationBar,
                Element);
            UpdateWidget(scaffold);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyTheme();
                ApplyNavigationPresentation();
                return true;
            });
            return true;
        }

        private void OnGeometryChanged(GeometryChangedEvent change) {
            ScheduleNavigationPresentation();
        }

        private void ScheduleNavigationPresentation() {
            if (_isUpdateScheduled) return;
            _isUpdateScheduled = true;
            _scheduledUpdate = Element.schedule.Execute(FlushNavigationPresentation).StartingIn(0L);
        }

        private void FlushNavigationPresentation() {
            _isUpdateScheduled = false;
            _scheduledUpdate = null;
            ApplyNavigationPresentation();
        }

        private void ApplyNavigationPresentation() {
            var width = Element.contentRect.width;
            var railVisible = !float.IsNaN(width)
                && !float.IsInfinity(width)
                && width >= ((AdaptiveScaffold)Widget).RailBreakpoint;
            if (railVisible) {
                _navigationRailNode!.SetMode(width >= ((AdaptiveScaffold)Widget).ExpandedRailBreakpoint
                    ? NavigationRailMode.Expanded
                    : NavigationRailMode.Collapsed);
            }
            if (_isRailVisible == railVisible) return;

            _isRailVisible = railVisible;
            _navigationRailNode!.NativeElement.style.display = railVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _navigationBarNode!.NativeElement.style.display = railVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyTheme();
        }

        private void ReconcileOptionalAppBar(ref WidgetNode? current, AppBar? next) {
            if (next is null) {
                var previous = current;
                current = null;
                if (previous is not null) UnmountChild(previous);
                return;
            }

            ReconcileSingleChild(ref current, next, Element);
            if (Element.IndexOf(current!.NativeElement) != 0) {
                current.NativeElement.RemoveFromHierarchy();
                Element.Insert(0, current.NativeElement);
            }
        }

        private void ApplyTheme() {
            var theme = Context.Theme;
            Element.style.backgroundColor = theme is null ? StyleKeyword.Null : theme.Colors.Canvas;
        }
    }

}
