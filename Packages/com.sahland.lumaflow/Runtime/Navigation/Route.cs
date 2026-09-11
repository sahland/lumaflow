#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {
    /// <summary>Identifies the small, navigation-specific transition used by a route.</summary>
    public enum RouteTransitionKind {
        None,
        Fade
    }

    /// <summary>
    /// Describes the transition used when a route becomes active or is restored.
    /// </summary>
    public readonly struct RouteTransition {
        public static RouteTransition None => new(RouteTransitionKind.None, TimeSpan.Zero);

        public RouteTransition(
            RouteTransitionKind kind,
            TimeSpan duration,
            Curve? curve = null,
            AnimationBehavior behavior = AnimationBehavior.Normal) {
            if (!Enum.IsDefined(typeof(RouteTransitionKind), kind)) {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported route transition kind.");
            }
            if (!Enum.IsDefined(typeof(AnimationBehavior), behavior)) {
                throw new ArgumentOutOfRangeException(nameof(behavior));
            }
            if (kind == RouteTransitionKind.None) {
                Kind = kind;
                Duration = TimeSpan.Zero;
                _curve = Curves.Linear;
                Behavior = behavior;
                return;
            }
            if (duration <= TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException(nameof(duration), "A visible route transition requires a positive duration.");
            }
            Kind = kind;
            Duration = duration;
            _curve = curve ?? Curves.Linear;
            Behavior = behavior;
        }

        public RouteTransitionKind Kind { get; }
        public TimeSpan Duration { get; }
        private readonly Curve? _curve;
        public Curve Curve => _curve ?? Curves.Linear;
        public AnimationBehavior Behavior { get; }

        public static RouteTransition Fade(
            TimeSpan duration,
            Curve? curve = null,
            AnimationBehavior behavior = AnimationBehavior.Normal) =>
            new(RouteTransitionKind.Fade, duration, curve, behavior);
    }

    /// <summary>A stable, restorable destination description.</summary>
    public sealed class Route {
        public Route(WidgetKey key, Widget child, RouteTransition transition = default) {
            if (!key.IsValid) throw new ArgumentException("A route requires a valid key.", nameof(key));
            Key = key;
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Transition = transition;
        }

        public WidgetKey Key { get; }
        public Widget Child { get; }
        public RouteTransition Transition { get; }
    }

    /// <summary>
    /// An immutable in-memory navigation restoration snapshot. Route widgets are retained by
    /// reference; persistence across process restarts belongs to the application serializer.
    /// </summary>
    public sealed class NavigationSnapshot {
        private readonly IReadOnlyList<Route> _routes;

        public NavigationSnapshot(IReadOnlyList<Route> routes) {
            if (routes is null) throw new ArgumentNullException(nameof(routes));
            if (routes.Count == 0) throw new ArgumentException("A navigation snapshot requires a root route.", nameof(routes));
            var copy = new Route[routes.Count];
            var keyIndexes = new Dictionary<WidgetKey, int>();
            for (var index = 0; index < routes.Count; index++) {
                var route = routes[index] ?? throw new ArgumentException("Navigation snapshot routes cannot contain null.", nameof(routes));
                if (keyIndexes.TryGetValue(route.Key, out var previousIndex)) {
                    throw new ArgumentException(
                        $"Navigation snapshot route key '{route.Key.Value}' is duplicated at indexes "
                        + $"{previousIndex} and {index}.",
                        nameof(routes));
                }
                keyIndexes.Add(route.Key, index);
                copy[index] = route;
            }
            _routes = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<Route> Routes => _routes;
    }
}
