#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Owns a scoped stack of typed widget destinations.
    /// </summary>
    public sealed class Navigator {
        private readonly List<Route> _stack = new();
        private NavigatorHostNode? _host;
        private long _automaticKey;

        /// <summary>
        /// Creates a navigator with its root destination.
        /// </summary>
        public Navigator(Widget initialRoute)
            : this(new Route(new WidgetKey("route-0"), initialRoute ?? throw new ArgumentNullException(nameof(initialRoute)))) {
        }

        /// <summary>Creates a navigator with an explicitly keyed root route.</summary>
        public Navigator(Route initialRoute) {
            _stack.Add(initialRoute ?? throw new ArgumentNullException(nameof(initialRoute)));
            _automaticKey = 1;
            DepthState = new State<int>(_stack.Count);
            CanPopState = new State<bool>(false);
            CurrentRouteState = new State<Route>(_stack[^1]);
        }

        /// <summary>Restores a navigator from a previously captured in-memory snapshot.</summary>
        public Navigator(NavigationSnapshot snapshot) {
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
            _stack.AddRange(snapshot.Routes);
            _automaticKey = _stack.Count;
            DepthState = new State<int>(_stack.Count);
            CanPopState = new State<bool>(_stack.Count > 1);
            CurrentRouteState = new State<Route>(_stack[^1]);
        }

        /// <summary>Gets the active destination description.</summary>
        public Widget Current => _stack[^1].Child;

        /// <summary>Gets the active keyed route.</summary>
        public Route CurrentRoute => _stack[^1];

        internal IReadOnlyList<Route> Routes => _stack;

        /// <summary>Gets the current navigation stack depth.</summary>
        public int Depth => _stack.Count;

        /// <summary>Observes the current route-stack depth after successful navigation commits.</summary>
        public State<int> DepthState { get; }

        /// <summary>Gets whether the active destination has a previous entry.</summary>
        public bool CanPop => _stack.Count > 1;

        /// <summary>Observes whether the active route has a previous route to return to.</summary>
        public State<bool> CanPopState { get; }

        /// <summary>Observes the active keyed route after successful navigation commits.</summary>
        public State<Route> CurrentRouteState { get; }

        /// <summary>Pushes <paramref name="route" /> and makes it active.</summary>
        public void Push(Widget route) {
            if (route is null) throw new ArgumentNullException(nameof(route));
            Push(CreateAutomaticRoute(route));
        }

        /// <summary>Pushes an explicitly keyed route.</summary>
        public void Push(Route route) {
            if (route is null) throw new ArgumentNullException(nameof(route));
            EnsureUniqueKey(route.Key);
            RequireHost().Push(route);
        }

        /// <summary>
        /// Pops the active destination and returns whether a destination was removed.
        /// The root destination cannot be popped.
        /// </summary>
        public bool Pop() => RequireHost().Pop();

        /// <summary>Replaces the active destination without retaining it in history.</summary>
        public void Replace(Widget route) {
            if (route is null) throw new ArgumentNullException(nameof(route));
            Replace(CreateAutomaticRoute(route));
        }

        /// <summary>Replaces the active destination with an explicitly keyed route.</summary>
        public void Replace(Route route) {
            if (route is null) throw new ArgumentNullException(nameof(route));
            EnsureUniqueKey(route.Key, ignoreCurrent: true);
            RequireHost().Replace(route);
        }

        /// <summary>Removes every destination above the root and returns whether the stack changed.</summary>
        public bool PopToRoot() => RequireHost().PopToRoot();

        /// <summary>Removes the complete stack and installs one new root route.</summary>
        public void ClearAndPush(Route route) {
            if (route is null) throw new ArgumentNullException(nameof(route));
            RequireHost().ClearAndPush(route);
        }

        /// <summary>Captures the current keyed stack for in-memory restoration.</summary>
        public NavigationSnapshot CaptureSnapshot() => new(_stack);

        internal void Attach(NavigatorHostNode host) {
            if (_host is not null && _host != host) {
                throw new InvalidOperationException("A Navigator can belong to only one mounted NavigatorHost.");
            }

            _host = host;
        }

        internal void Detach(NavigatorHostNode host) {
            if (_host == host) _host = null;
        }

        internal void CommitPush(Route route) {
            _stack.Add(route);
            PublishStackState();
        }

        internal void CommitPop() {
            _stack.RemoveAt(_stack.Count - 1);
            PublishStackState();
        }

        internal void CommitReplace(Route route) {
            _stack[^1] = route;
            PublishStackState();
        }

        internal void CommitClearAndPush(Route route) {
            _stack.Clear();
            _stack.Add(route);
            PublishStackState();
        }

        internal void CommitPopToRoot() {
            if (_stack.Count == 1) return;
            _stack.RemoveRange(1, _stack.Count - 1);
            PublishStackState();
        }

        internal void PublishStackState() {
            List<Exception>? failures = null;
            try {
                DepthState.Value = _stack.Count;
            } catch (Exception exception) {
                failures = new List<Exception> { exception };
            }

            try {
                CanPopState.Value = _stack.Count > 1;
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                CurrentRouteState.Value = _stack[^1];
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            if (failures is { Count: 1 }) throw failures[0];
            if (failures is { Count: > 1 }) {
                throw new AggregateException("Navigator state observers failed after the route stack was committed.", failures);
            }
        }

        private NavigatorHostNode RequireHost() {
            return _host ?? throw new InvalidOperationException("The Navigator is not mounted below a NavigatorHost.");
        }

        private Route CreateAutomaticRoute(Widget child) {
            WidgetKey key;
            do {
                key = new WidgetKey($"route-{_automaticKey++}");
            } while (_stack.Exists(route => route.Key.Equals(key)));
            return new Route(key, child);
        }

        private void EnsureUniqueKey(WidgetKey key, bool ignoreCurrent = false) {
            if (!key.IsValid) throw new ArgumentException("A route requires a valid key.", nameof(key));
            var limit = ignoreCurrent ? _stack.Count - 1 : _stack.Count;
            for (var index = 0; index < limit; index++) {
                if (_stack[index].Key.Equals(key)) {
                    throw new InvalidOperationException($"Route key '{key.Value}' already exists in this navigator stack.");
                }
            }
        }
    }

    /// <summary>
    /// Establishes a navigation scope and displays the active destination of its <see cref="Navigator" />.
    /// </summary>
    public sealed class NavigatorHost : Widget {
        /// <summary>Creates a host that owns a new navigator rooted at <paramref name="initialRoute" />.</summary>
        public NavigatorHost(Widget initialRoute)
            : this(new Navigator(initialRoute)) {
        }

        /// <summary>Creates a host for the explicitly supplied navigator.</summary>
        public NavigatorHost(Navigator navigator) {
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        /// <summary>Gets the navigator scoped by this host.</summary>
        public Navigator Navigator { get; }

        internal override WidgetNode CreateNode() => new NavigatorHostNode(this);
    }

    internal sealed class NavigatorHostNode : WidgetNode {
        private readonly List<RouteEntry> _entries = new();
        private bool _isMountingRoute;

        public NavigatorHostNode(NavigatorHost widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var host = new VisualElement();
            host.style.flexGrow = 1f;
            host.style.flexShrink = 1f;
            host.style.minWidth = 0f;
            host.style.minHeight = 0f;
            return host;
        }

        protected override void OnMounted() {
            var navigator = ((NavigatorHost)Widget).Navigator;
            navigator.Attach(this);
            Bindings.Add(() => navigator.Detach(this));
            Bindings.Add(ClearRouteEntries);
            for (var index = 0; index < navigator.Routes.Count; index++) {
                _entries.Add(MountRoute(
                    navigator.Routes[index],
                    active: index == navigator.Routes.Count - 1));
            }
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not NavigatorHost host) return false;
            if (!ReferenceEquals(((NavigatorHost)Widget).Navigator, host.Navigator)) return false;
            UpdateWidget(host);
            return true;
        }

        internal void Push(Route route) {
            EnsureReadyForNavigation();
            var previous = _entries[^1];
            CaptureFocusedElement(previous);
            var next = MountRoute(route, active: true, animate: true);
            SetEntryActive(previous, active: false, restoreFocus: false);
            _entries.Add(next);
            ((NavigatorHost)Widget).Navigator.CommitPush(route);
        }

        internal bool Pop() {
            EnsureReadyForNavigation();
            var navigator = ((NavigatorHost)Widget).Navigator;
            if (!navigator.CanPop) return false;

            var outgoing = _entries[^1];
            var incoming = _entries[^2];
            CaptureFocusedElement(outgoing);
            _entries.RemoveAt(_entries.Count - 1);
            SetEntryActive(incoming, active: true, restoreFocus: true);
            CommitAndRelease(navigator.CommitPop, outgoing, "pop");
            return true;
        }

        internal void Replace(Route route) {
            EnsureReadyForNavigation();
            var outgoing = _entries[^1];
            CaptureFocusedElement(outgoing);
            var incoming = MountRoute(route, active: true, animate: true);
            _entries[^1] = incoming;
            SetEntryActive(outgoing, active: false, restoreFocus: false);
            var navigator = ((NavigatorHost)Widget).Navigator;
            CommitAndRelease(() => navigator.CommitReplace(route), outgoing, "replace");
        }

        internal void ClearAndPush(Route route) {
            EnsureReadyForNavigation();
            var incoming = MountRoute(route, active: true, animate: true);
            var removed = _entries.ToArray();
            _entries.Clear();
            _entries.Add(incoming);
            Exception? commitFailure = null;
            try {
                ((NavigatorHost)Widget).Navigator.CommitClearAndPush(route);
            } catch (Exception exception) {
                commitFailure = exception;
            }
            List<Exception>? failures = null;
            for (var index = removed.Length - 1; index >= 0; index--) {
                try { ReleaseEntry(removed[index]); }
                catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }
            Exception? cleanupFailure = failures switch {
                { Count: 1 } => failures[0],
                { Count: > 1 } => new AggregateException(failures),
                _ => null
            };
            ThrowNavigationFailures("clear", commitFailure, cleanupFailure);
        }

        internal bool PopToRoot() {
            EnsureReadyForNavigation();
            var navigator = ((NavigatorHost)Widget).Navigator;
            if (!navigator.CanPop) return false;

            CaptureFocusedElement(_entries[^1]);
            var removed = _entries.GetRange(1, _entries.Count - 1);
            _entries.RemoveRange(1, _entries.Count - 1);
            SetEntryActive(_entries[0], active: true, restoreFocus: true);

            Exception? commitFailure = null;
            try {
                navigator.CommitPopToRoot();
            } catch (Exception exception) {
                commitFailure = exception;
            }

            List<Exception>? cleanupFailures = null;
            for (var index = removed.Count - 1; index >= 0; index--) {
                try {
                    ReleaseEntry(removed[index]);
                } catch (Exception exception) {
                    cleanupFailures ??= new List<Exception>();
                    cleanupFailures.Add(exception);
                }
            }

            Exception? cleanupFailure = cleanupFailures switch {
                { Count: 1 } => cleanupFailures[0],
                { Count: > 1 } => new AggregateException(cleanupFailures),
                _ => null
            };

            ThrowNavigationFailures("pop to root", commitFailure, cleanupFailure);
            return true;
        }

        private RouteEntry MountRoute(Route route, bool active, bool animate = false) {
            var layer = CreateRouteLayer(active);
            Element.Add(layer);
            _isMountingRoute = true;
            try {
                var context = Context.WithNavigator(((NavigatorHost)Widget).Navigator);
                var node = MountChild(route.Child, layer, context);
                var entry = new RouteEntry(route, layer, node);
                if (active && animate) BeginTransition(entry);
                return entry;
            } catch {
                layer.RemoveFromHierarchy();
                throw;
            } finally {
                _isMountingRoute = false;
            }
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            if (aspect != InheritedAspect.MediaQuery || !Context.MediaQuery.DisableAnimations) return;
            for (var index = 0; index < _entries.Count; index++) {
                var entry = _entries[index];
                if (entry.Route.Transition.Behavior == AnimationBehavior.Normal) {
                    entry.SnapTransitionToEnd();
                }
            }
        }

        private static VisualElement CreateRouteLayer(bool active) {
            var layer = new VisualElement {
                name = "lumaflow-route-entry",
                pickingMode = active ? PickingMode.Position : PickingMode.Ignore
            };
            layer.style.flexGrow = 1f;
            layer.style.flexShrink = 1f;
            layer.style.minWidth = 0f;
            layer.style.minHeight = 0f;
            layer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            layer.SetEnabled(active);
            return layer;
        }

        private static void CaptureFocusedElement(RouteEntry entry) {
            var focused = entry.Layer.panel?.focusController.focusedElement as VisualElement;
            if (focused is not null
                && (ReferenceEquals(focused, entry.Layer) || entry.Layer.Contains(focused))) {
                entry.LastFocusedElement = focused;
            }
        }

        private void SetEntryActive(RouteEntry entry, bool active, bool restoreFocus) {
            if (!active) CaptureFocusedElement(entry);
            entry.Layer.pickingMode = active ? PickingMode.Position : PickingMode.Ignore;
            entry.Layer.SetEnabled(active);
            entry.Layer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (active) BeginTransition(entry);
            else entry.StopTransition();
            if (!active || !restoreFocus || entry.LastFocusedElement is not { } focused) return;
            if (entry.Layer.panel is null
                || !ReferenceEquals(focused.panel, entry.Layer.panel)
                || !focused.enabledInHierarchy
                || (!ReferenceEquals(focused, entry.Layer) && !entry.Layer.Contains(focused))) return;
            focused.Focus();
        }

        private void BeginTransition(RouteEntry entry) {
            entry.StopTransition();
            if (entry.Route.Transition.Kind != RouteTransitionKind.Fade) {
                entry.Layer.style.opacity = 1f;
                return;
            }
            var transition = entry.Route.Transition;
            entry.Animation.Start(
                0f,
                1f,
                new FloatTween(0f, 1f),
                new AnimationSpec(transition.Duration, transition.Curve, transition.Behavior),
                NowSeconds(),
                Context.MediaQuery.DisableAnimations);
            entry.Layer.style.opacity = entry.Animation.Current;
            if (!entry.Animation.IsRunning) return;
            entry.Transition = entry.Layer.schedule.Execute(() => TickTransition(entry)).Every(16L);
        }

        private static void TickTransition(RouteEntry entry) {
            var sample = entry.Animation.Sample(NowSeconds());
            entry.Layer.style.opacity = sample.Value;
            if (sample.Completed) entry.StopTransition();
        }

        private static double NowSeconds() => UnityEngine.Time.realtimeSinceStartupAsDouble;

        private void CommitAndRelease(Action commit, RouteEntry outgoing, string operation) {
            Exception? commitFailure = null;
            try {
                commit();
            } catch (Exception exception) {
                commitFailure = exception;
            }

            Exception? cleanupFailure = null;
            try {
                ReleaseEntry(outgoing);
            } catch (Exception exception) {
                cleanupFailure = exception;
            }

            ThrowNavigationFailures(operation, commitFailure, cleanupFailure);
        }

        private void ClearRouteEntries() {
            for (var index = _entries.Count - 1; index >= 0; index--) {
                _entries[index].StopTransition();
                _entries[index].Layer.RemoveFromHierarchy();
            }

            _entries.Clear();
        }

        private void ReleaseEntry(RouteEntry entry) {
            entry.StopTransition();
            try {
                UnmountChild(entry.Node);
            } finally {
                entry.Layer.RemoveFromHierarchy();
            }
        }

        private static void ThrowNavigationFailures(
            string operation,
            Exception? commitFailure,
            Exception? cleanupFailure) {
            if (commitFailure is not null && cleanupFailure is not null) {
                throw new InvalidOperationException(
                    $"Navigator {operation} committed, but observer notification and route cleanup both failed.",
                    new AggregateException(commitFailure, cleanupFailure));
            }
            if (commitFailure is not null) throw commitFailure;
            if (cleanupFailure is not null) {
                throw new InvalidOperationException(
                    $"Navigator {operation} committed, but the removed route failed to clean up.",
                    cleanupFailure);
            }
        }

        private void EnsureReadyForNavigation() {
            if (!IsMounted) {
                throw new InvalidOperationException("Navigator cannot change routes while its NavigatorHost is not mounted.");
            }

            if (_isMountingRoute) {
                throw new InvalidOperationException("Navigator cannot change routes while a route is building.");
            }
        }

        private sealed class RouteEntry {
            public RouteEntry(Route route, VisualElement layer, WidgetNode node) {
                Route = route;
                Layer = layer;
                Node = node;
            }

            public Route Route { get; }
            public VisualElement Layer { get; }
            public WidgetNode Node { get; }
            public VisualElement? LastFocusedElement { get; set; }
            public IVisualElementScheduledItem? Transition { get; set; }
            public ImplicitAnimation<float> Animation { get; } = new();

            public void StopTransition() {
                Transition?.Pause();
                Transition = null;
                Animation.Cancel();
            }

            public void SnapTransitionToEnd() {
                if (!Animation.SnapToEnd()) return;
                Layer.style.opacity = Animation.Current;
                Transition?.Pause();
                Transition = null;
            }
        }
    }

}
