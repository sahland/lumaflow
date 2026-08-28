#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>
    /// Represents an explicitly owned observable value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class State<T> {
        private readonly List<Subscription> _subscriptions = new();
        private T _value;

        /// <summary>
        /// Creates state with an initial value.
        /// </summary>
        public State(T value) {
            _value = value;
        }

        /// <summary>
        /// Gets or sets the current value. Assigning an equal value does not notify subscribers.
        /// The new value is committed before synchronous notification begins. A failing
        /// subscriber does not prevent remaining active subscribers from being notified;
        /// failures are rethrown after notification completes.
        /// </summary>
        public T Value {
            get => _value;
            set {
                if (EqualityComparer<T>.Default.Equals(_value, value)) {
                    return;
                }

                _value = value;
                Notify(value);
            }
        }

        /// <summary>
        /// Registers a synchronous listener. Dispose the returned subscription to stop notifications.
        /// </summary>
        public IDisposable Subscribe(Action<T> listener) {
            if (listener is null) {
                throw new ArgumentNullException(nameof(listener));
            }

            var subscription = new Subscription(this, listener);
            _subscriptions.Add(subscription);
            return subscription;
        }

        private void Notify(T value) {
            if (_subscriptions.Count == 0) {
                return;
            }

            var snapshot = _subscriptions.ToArray();
            List<Exception>? failures = null;
            foreach (var subscription in snapshot) {
                try {
                    subscription.Notify(value);
                } catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures is { Count: 1 }) {
                throw failures[0];
            }

            if (failures is { Count: > 1 }) {
                throw new AggregateException("One or more State subscribers failed during notification.", failures);
            }
        }

        private void Unsubscribe(Subscription subscription) {
            _subscriptions.Remove(subscription);
        }

        private sealed class Subscription : IDisposable {
            private readonly State<T> _owner;
            private readonly Action<T> _listener;
            private bool _isDisposed;

            public Subscription(State<T> owner, Action<T> listener) {
                _owner = owner;
                _listener = listener;
            }

            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _isDisposed = true;
                _owner.Unsubscribe(this);
            }

            public void Notify(T value) {
                if (!_isDisposed) {
                    _listener(value);
                }
            }
        }
    }

    /// <summary>
    /// Replaces only its local subtree when an externally owned <see cref="State{T}"/> changes.
    /// </summary>
    public sealed class ReactiveBuilder<T> : Widget {
        public ReactiveBuilder(State<T> state, Func<T, Widget> builder) {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>Gets the borrowed state that drives this subtree.</summary>
        public State<T> State { get; }

        /// <summary>Builds one replacement subtree for the current state value.</summary>
        public Func<T, Widget> Builder { get; }

        internal override WidgetNode CreateNode() => new ReactiveBuilderNode<T>(this);
    }

    internal sealed class ReactiveBuilderNode<T> : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private IDisposable? _stateSubscription;

        public ReactiveBuilderNode(ReactiveBuilder<T> widget)
            : base(widget) {
        }

        protected override UnityEngine.UIElements.VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (ReactiveBuilder<T>)Widget;
            ReplaceChild(widget.State.Value);
            BindState(widget);
            Bindings.Add(ReleaseState);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not ReactiveBuilder<T> builder) return false;
            var previous = (ReactiveBuilder<T>)Widget;
            RebuildChild(builder, builder.State.Value);
            if (!ReferenceEquals(previous.State, builder.State)) ReleaseState();
            UpdateWidget(builder);
            if (!ReferenceEquals(previous.State, builder.State)) BindState(builder);
            return true;
        }

        internal WidgetNode CurrentChild => _currentChild
            ?? throw new InvalidOperationException("ReactiveBuilder does not have a mounted child.");

        internal override void Reassemble() => ReplaceChild(((ReactiveBuilder<T>)Widget).State.Value);

        private void ReplaceChild(T value) {
            RebuildChild((ReactiveBuilder<T>)Widget, value);
        }

        private void RebuildChild(ReactiveBuilder<T> widget, T value) {
            var nextWidget = widget.Builder(value)
                ?? throw new InvalidOperationException("ReactiveBuilder builder cannot return null.");

            ReconcileSingleChild(ref _currentChild, nextWidget, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        private void BindState(ReactiveBuilder<T> widget) =>
            _stateSubscription = widget.State.Subscribe(ReplaceChild);

        private void ReleaseState() {
            _stateSubscription?.Dispose();
            _stateSubscription = null;
        }
    }

}
