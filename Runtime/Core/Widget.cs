#nullable enable

namespace LumaFlow {

    /// <summary>
    /// Describes a declarative piece of LumaFlow UI.
    /// </summary>
    public abstract class Widget {
        protected Widget(WidgetKey? key = null) {
            if (key is { } value && !value.IsValid) {
                throw new System.ArgumentException("A widget key must be non-empty.", nameof(key));
            }

            Key = key;
        }

        /// <summary>
        /// Gets the optional identity used when a multi-child parent reconciles this widget.
        /// </summary>
        public WidgetKey? Key { get; }

        /// <summary>
        /// Wraps this widget in a keyed identity boundary without mutating its configuration.
        /// </summary>
        public Widget WithKey(WidgetKey key) => new KeyedSubtree(key, this);

        internal abstract WidgetNode CreateNode();
    }

    /// <summary>
    /// Assigns stable sibling identity to an arbitrary widget subtree.
    /// </summary>
    public sealed class KeyedSubtree : Widget {
        public KeyedSubtree(WidgetKey key, Widget child)
            : base(key) {
            Child = child ?? throw new System.ArgumentNullException(nameof(child));
        }

        public Widget Child { get; }

        internal override WidgetNode CreateNode() => new KeyedSubtreeNode(this);
    }

    internal sealed class KeyedSubtreeNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;

        public KeyedSubtreeNode(KeyedSubtree widget)
            : base(widget) {
        }

        protected override UnityEngine.UIElements.VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            _currentChild = MountChild(((KeyedSubtree)Widget).Child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not KeyedSubtree next || !CanUpdateWith(next)) return false;
            ReconcileSingleChild(ref _currentChild, next.Child, NativeParent);
            UpdateWidget(next);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }
    }

    /// <summary>
    /// A widget whose subtree is built from the inherited <see cref="BuildContext"/>.
    /// </summary>
    public abstract class StatelessWidget : Widget {
        /// <summary>
        /// Builds this widget's child using the nearest inherited theme and other tree context.
        /// </summary>
        public abstract Widget Build(BuildContext context);

        internal override WidgetNode CreateNode() => new StatelessWidgetNode(this);
    }

    /// <summary>
    /// Describes a widget that owns one mount-local <see cref="WidgetState"/> instance.
    /// </summary>
    /// <remarks>
    /// State is retained for the lifetime of this mounted node only. LumaFlow does
    /// not yet preserve state when a parent replaces or reorders child widgets.
    /// </remarks>
    public abstract class StatefulWidget : Widget {
        internal abstract WidgetState CreateState();

        internal override WidgetNode CreateNode() => new StatefulWidgetNode(this);
    }

    /// <summary>Convenience base for stateful widgets whose state has a parameterless constructor.</summary>
    public abstract class StatefulWidget<TState> : StatefulWidget
        where TState : WidgetState, new() {
        internal override WidgetState CreateState() => new TState();
    }

    /// <summary>Owns mount-local mutable data and builds a stateful widget's local subtree.</summary>
    public abstract class WidgetState {
        private StatefulWidgetNode? _node;

        /// <summary>Gets the widget description associated with this mounted state.</summary>
        protected StatefulWidget Widget => _node?.StatefulWidget
            ?? throw new System.InvalidOperationException("WidgetState is not mounted.");

        /// <summary>Gets whether this state can currently rebuild its local subtree.</summary>
        public bool IsMounted => _node?.IsMounted ?? false;

        /// <summary>Called once after the state is attached and before its first build.</summary>
        protected internal virtual void InitState() {
        }

        /// <summary>Builds the current local subtree.</summary>
        public abstract Widget Build(BuildContext context);

        /// <summary>Called exactly once when the owning mounted node is released.</summary>
        protected internal virtual void Dispose() {
        }

        /// <summary>Called when a keyed parent retains this state with a new widget description.</summary>
        protected internal virtual void DidUpdateWidget(StatefulWidget oldWidget) {
        }

        /// <summary>Applies a local mutation and rebuilds only this stateful widget's subtree.</summary>
        protected void SetState(System.Action mutation) {
            if (mutation is null) throw new System.ArgumentNullException(nameof(mutation));
            var node = _node ?? throw new System.InvalidOperationException("WidgetState is not mounted.");
            node.SetState(mutation);
        }

        internal void Attach(StatefulWidgetNode node) {
            if (_node is not null) throw new System.InvalidOperationException("WidgetState can belong to only one mounted widget.");
            _node = node;
        }

        internal void DisposeFromNode() {
            try {
                Dispose();
            } finally {
                _node = null;
            }
        }
    }

    internal sealed class StatefulWidgetNode : WidgetNode, ITransparentWidgetNode {
        private readonly WidgetState _state;
        private WidgetNode? _currentChild;
        private bool _initialized;

        public StatefulWidgetNode(StatefulWidget widget)
            : base(widget) {
            StatefulWidget = widget;
            _state = widget.CreateState() ?? throw new System.InvalidOperationException("StatefulWidget.CreateState cannot return null.");
        }

        internal StatefulWidget StatefulWidget { get; private set; }
        internal WidgetState State => _state;
        internal override string? DiagnosticStateType => _state.GetType().FullName ?? _state.GetType().Name;

        protected override UnityEngine.UIElements.VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            _state.Attach(this);
            Bindings.Add(_state.DisposeFromNode);
            _state.InitState();
            _initialized = true;
            ReplaceChild();
        }

        internal void SetState(System.Action mutation) {
            if (_initialized && !IsMounted) {
                throw new System.InvalidOperationException("WidgetState.setState was called after the widget was unmounted.");
            }
            mutation();
            if (!_initialized) return;
            ReplaceChild();
        }

        internal override void Reassemble() => ReplaceChild();

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not StatefulWidget next || !CanUpdateWith(next)) return false;
            var previous = StatefulWidget;
            StatefulWidget = next;
            UpdateWidget(next);
            _state.DidUpdateWidget(previous);
            ReplaceChild();
            return true;
        }

        private void ReplaceChild() {
            var nextWidget = ReevaluateInheritedDependencies(() => _state.Build(Context))
                ?? throw new System.InvalidOperationException("WidgetState.Build cannot return null.");
            ReconcileSingleChild(ref _currentChild, nextWidget, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => ReplaceChild();
    }

    internal sealed class StatelessWidgetNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;

        public StatelessWidgetNode(StatelessWidget widget)
            : base(widget) {
        }

        protected override UnityEngine.UIElements.VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var child = ReevaluateInheritedDependencies(() => ((StatelessWidget)Widget).Build(Context))
                ?? throw new System.InvalidOperationException("StatelessWidget.Build cannot return null.");
            _currentChild = MountChild(child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not StatelessWidget next || !CanUpdateWith(next)) return false;
            var nextChild = ReevaluateInheritedDependencies(() => next.Build(Context))
                ?? throw new System.InvalidOperationException("StatelessWidget.Build cannot return null.");
            ReconcileSingleChild(ref _currentChild, nextChild, NativeParent);
            UpdateWidget(next);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            var nextChild = ReevaluateInheritedDependencies(() => ((StatelessWidget)Widget).Build(Context))
                ?? throw new System.InvalidOperationException("StatelessWidget.Build cannot return null.");
            ReconcileSingleChild(ref _currentChild, nextChild, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        internal override void Reassemble() {
            var nextChild = ReevaluateInheritedDependencies(() => ((StatelessWidget)Widget).Build(Context))
                ?? throw new System.InvalidOperationException("StatelessWidget.Build cannot return null.");
            ReconcileSingleChild(ref _currentChild, nextChild, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
        }
    }

    /// <summary>
    /// Embeds an existing or mount-local native UI Toolkit element in a LumaFlow tree.
    /// </summary>
    /// <remarks>
    /// Existing elements are borrowed: LumaFlow attaches them while mounted and detaches
    /// them during unmount without clearing their styles, classes, or child hierarchy.
    /// </remarks>
    public sealed class Native : Widget {
        private readonly UnityEngine.UIElements.VisualElement? _existingElement;
        private readonly System.Func<UnityEngine.UIElements.VisualElement>? _factory;

        internal System.Action<UnityEngine.UIElements.VisualElement>? OnMounted { get; }
        internal System.Action<UnityEngine.UIElements.VisualElement>? OnUpdated { get; }
        internal System.Action<UnityEngine.UIElements.VisualElement>? OnUnmounted { get; }

        /// <summary>
        /// Creates a widget that borrows <paramref name="element" /> for one active mount.
        /// The element must be detached before mounting.
        /// </summary>
        public Native(UnityEngine.UIElements.VisualElement element)
            : this(element, null, null, null) {
        }

        /// <summary>
        /// Borrows <paramref name="element" /> and invokes lifecycle hooks while it is
        /// attached to the LumaFlow tree.
        /// </summary>
        public Native(
            UnityEngine.UIElements.VisualElement element,
            System.Action<UnityEngine.UIElements.VisualElement>? onMounted = null,
            System.Action<UnityEngine.UIElements.VisualElement>? onUpdated = null,
            System.Action<UnityEngine.UIElements.VisualElement>? onUnmounted = null) {
            _existingElement = element ?? throw new System.ArgumentNullException(nameof(element));
            OnMounted = onMounted;
            OnUpdated = onUpdated;
            OnUnmounted = onUnmounted;
        }

        /// <summary>
        /// Creates a widget whose factory supplies one detached element per mount.
        /// </summary>
        public Native(System.Func<UnityEngine.UIElements.VisualElement> factory)
            : this(factory, null, null, null) {
        }

        /// <summary>
        /// Creates one detached element per mount and invokes lifecycle hooks while
        /// that element is attached to the LumaFlow tree.
        /// </summary>
        public Native(
            System.Func<UnityEngine.UIElements.VisualElement> factory,
            System.Action<UnityEngine.UIElements.VisualElement>? onMounted = null,
            System.Action<UnityEngine.UIElements.VisualElement>? onUpdated = null,
            System.Action<UnityEngine.UIElements.VisualElement>? onUnmounted = null) {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            OnMounted = onMounted;
            OnUpdated = onUpdated;
            OnUnmounted = onUnmounted;
        }

        internal override WidgetNode CreateNode() {
            return new NativeNode(this);
        }

        internal UnityEngine.UIElements.VisualElement CreateElementForMount() {
            var element = _factory is null
                ? _existingElement!
                : _factory() ?? throw new System.InvalidOperationException("Native factory returned null.");

            if (element.parent is not null) {
                throw new System.InvalidOperationException(
                    "Native requires a detached VisualElement. "
                    + "The supplied element already has a parent; detach it explicitly before mounting. "
                    + "LumaFlow does not silently reparent borrowed native elements.");
            }

            return element;
        }

        internal bool CanRetainElementFor(Native next) {
            if (_existingElement is not null) {
                return ReferenceEquals(_existingElement, next._existingElement);
            }

            return _factory is not null && Equals(_factory, next._factory);
        }
    }

    internal sealed class NativeNode : WidgetNode {
        private System.Action<UnityEngine.UIElements.VisualElement>? _onUnmounted;

        public NativeNode(Native widget)
            : base(widget) {
        }

        protected override UnityEngine.UIElements.VisualElement CreateElement(BuildContext context) {
            return ((Native)Widget).CreateElementForMount();
        }

        protected override void OnMounted() {
            var native = (Native)Widget;
            _onUnmounted = native.OnUnmounted;
            Bindings.Add(InvokeUnmounted);
            native.OnMounted?.Invoke(Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Native native) return false;
            if (!((Native)Widget).CanRetainElementFor(native)) return false;
            UpdateWidget(native);
            _onUnmounted = native.OnUnmounted;
            native.OnUpdated?.Invoke(Element);
            return true;
        }

        private void InvokeUnmounted() {
            var callback = _onUnmounted;
            _onUnmounted = null;
            callback?.Invoke(Element);
        }
    }

}
