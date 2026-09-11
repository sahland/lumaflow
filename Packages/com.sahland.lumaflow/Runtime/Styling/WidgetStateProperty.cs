#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Describes the interactive conditions currently active for a widget.
    /// Multiple conditions can be combined without allocating a collection per event.
    /// </summary>
    [Flags]
    public enum WidgetStates {
        None = 0,
        Hovered = 1 << 0,
        Focused = 1 << 1,
        Pressed = 1 << 2,
        Dragged = 1 << 3,
        Selected = 1 << 4,
        ScrolledUnder = 1 << 5,
        Disabled = 1 << 6,
        Error = 1 << 7
    }

    /// <summary>
    /// Resolves one style value from a widget's current interactive states.
    /// </summary>
    /// <typeparam name="T">The resolved style value type.</typeparam>
    public abstract class WidgetStateProperty<T> {
        /// <summary>Returns the value appropriate for <paramref name="states"/>.</summary>
        public abstract T Resolve(WidgetStates states);

        /// <summary>Creates a property that always resolves to <paramref name="value"/>.</summary>
        public static WidgetStateProperty<T> All(T value) => new WidgetStatePropertyAll<T>(value);

        /// <summary>Creates a property backed by a state resolver.</summary>
        public static WidgetStateProperty<T> ResolveWith(Func<WidgetStates, T> resolver) =>
            new ResolverWidgetStateProperty<T>(resolver);

        private sealed class ResolverWidgetStateProperty<TValue> : WidgetStateProperty<TValue> {
            private readonly Func<WidgetStates, TValue> _resolver;

            public ResolverWidgetStateProperty(Func<WidgetStates, TValue> resolver) {
                _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            }

            public override TValue Resolve(WidgetStates states) => _resolver(states);

            public override bool Equals(object? obj) =>
                obj is ResolverWidgetStateProperty<TValue> other && Equals(_resolver, other._resolver);

            public override int GetHashCode() => _resolver.GetHashCode();
        }
    }

    /// <summary>A state property that returns the same value for every state set.</summary>
    public sealed class WidgetStatePropertyAll<T> : WidgetStateProperty<T>, IEquatable<WidgetStatePropertyAll<T>> {
        public WidgetStatePropertyAll(T value) {
            Value = value;
        }

        public T Value { get; }

        public override T Resolve(WidgetStates states) => Value;

        public bool Equals(WidgetStatePropertyAll<T>? other) =>
            other is not null && Equals(Value, other.Value);

        public override bool Equals(object? obj) => Equals(obj as WidgetStatePropertyAll<T>);

        public override int GetHashCode() => Value is null ? 0 : Value.GetHashCode();
    }

}
