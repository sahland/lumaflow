#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>Builds the content associated with the current value of a <see cref="TabBar{T}"/>.</summary>
    public sealed class TabView<T> : Widget {
        public TabView(State<T> value, Func<T, Widget> builder) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public State<T> Value { get; }
        public Func<T, Widget> Builder { get; }
        internal override WidgetNode CreateNode() => new TabViewNode<T>(this);
    }

    internal sealed class TabViewNode<T> : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private IDisposable? _subscription;

        public TabViewNode(TabView<T> widget) : base(widget) { }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (TabView<T>)Widget;
            Rebuild(widget);
            _subscription = widget.Value.Subscribe(_ => Rebuild((TabView<T>)Widget));
            Bindings.Add(ReleaseSubscription);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TabView<T> view) return false;
            var previous = (TabView<T>)Widget;
            if (!ReferenceEquals(previous.Value, view.Value)) ReleaseSubscription();
            UpdateWidget(view);
            Rebuild(view);
            if (!ReferenceEquals(previous.Value, view.Value)) _subscription = view.Value.Subscribe(_ => Rebuild((TabView<T>)Widget));
            return true;
        }

        private void Rebuild(TabView<T> widget) {
            var child = widget.Builder(widget.Value.Value)
                ?? throw new InvalidOperationException("TabView builder cannot return null.");
            ReconcileSingleChild(ref _currentChild, child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        private void ReleaseSubscription() {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
