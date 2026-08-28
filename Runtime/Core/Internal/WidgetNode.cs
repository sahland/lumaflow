#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal abstract class WidgetNode {
        private readonly List<WidgetNode> _children = new();
        private readonly BindingScope _bindingScope = new();
        private readonly HashSet<IInheritedScope> _inheritedDependencies = new();
        private WidgetNode? _parent;
        private SemanticsOwner.Registration? _semanticsRegistration;
        private WidgetNodeState _state;

        protected WidgetNode(Widget widget) {
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
        }

        protected Widget Widget { get; private set; }

        internal Widget Configuration => Widget;

        protected BuildContext Context { get; private set; } = null!;

        protected VisualElement Element { get; private set; } = null!;

        protected VisualElement NativeParent { get; private set; } = null!;

        /// <summary>
        /// Gets this widget's native root element after it has been mounted.
        /// </summary>
        internal VisualElement NativeElement => Element;

        protected BindingScope Bindings => _bindingScope;

        internal bool IsMounted => _state == WidgetNodeState.Mounted;

        internal WidgetNode? Parent => _parent;

        internal virtual string? DiagnosticStateType => null;

        /// <summary>Re-evaluates declarative builder boundaries without remounting compatible nodes.</summary>
        internal virtual void Reassemble() {
            var children = _children.ToArray();
            for (var index = 0; index < children.Length; index++) {
                if (children[index].IsMounted) children[index].Reassemble();
            }
        }

        internal WidgetDiagnosticsNode CaptureDiagnostics() {
            if (_state != WidgetNodeState.Mounted) {
                throw new InvalidOperationException(
                    $"Cannot capture diagnostics for {Widget.GetType().Name} while its node is {_state}.");
            }

            var focused = Element.panel?.focusController.focusedElement as VisualElement;
            var children = new WidgetDiagnosticsNode[_children.Count];
            for (var index = 0; index < _children.Count; index++) {
                children[index] = _children[index].CaptureDiagnostics();
            }
            var classes = Element.GetClasses().OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var inheritedAspects = _inheritedDependencies
                .Select(scope => scope.Aspect.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var diagnosticTheme = Context.DiagnosticTheme;
            var properties = new SortedDictionary<string, string>(StringComparer.Ordinal);
            LayoutDiagnosticProperties.Append(Widget, Element, properties);
            AppendDiagnosticProperties(properties);
            return new WidgetDiagnosticsNode(
                Widget.GetType().FullName ?? Widget.GetType().Name,
                GetType().FullName ?? GetType().Name,
                Widget.Key?.Value,
                _state.ToString(),
                DiagnosticStateType,
                Element.GetType().FullName ?? Element.GetType().Name,
                Element.name ?? string.Empty,
                Element.layout,
                Element.enabledInHierarchy,
                focused is not null && (ReferenceEquals(focused, Element) || Element.Contains(focused)),
                this is ITransparentWidgetNode,
                diagnosticTheme is not null,
                diagnosticTheme?.Colors.Primary,
                diagnosticTheme?.Colors.Surface,
                Context.DiagnosticMediaQuery,
                Context.DiagnosticLocale?.ToString(),
                Context.DiagnosticTextScaler.ScaleFactor,
                Array.AsReadOnly(classes),
                Array.AsReadOnly(inheritedAspects),
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(properties),
                Array.AsReadOnly(children));
        }

        protected virtual void AppendDiagnosticProperties(IDictionary<string, string> properties) {
        }

        internal void Mount(WidgetNode? parent, BuildContext context, VisualElement nativeParent) {
            if (context is null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (nativeParent is null) {
                throw new ArgumentNullException(nameof(nativeParent));
            }

            if (Widget is Expanded or Flexible && FindLayoutParent(parent) is not IFlexParentNode) {
                throw new InvalidOperationException(
                    $"{Widget.GetType().Name} must be an immediate child of Row or Column. "
                    + "Place it directly inside a flex layout parent.");
            }

            if (_state != WidgetNodeState.Created) {
                throw new InvalidOperationException($"{GetType().Name} cannot be mounted from {_state} state.");
            }

            _state = WidgetNodeState.Mounting;
            _parent = parent;
            Context = context.ForNode(this);

            try {
                NativeParent = nativeParent;
                var element = CreateElement(context);
                if (element is not null) {
                    Element = element;
                    nativeParent.Add(element);
                }
                RefreshSemantics();
                OnMounted();
                if (Element is null) {
                    throw new InvalidOperationException(
                        $"{GetType().Name} did not provide a native {nameof(VisualElement)}.");
                }
                _state = WidgetNodeState.Mounted;
            } catch (Exception mountFailure) {
                try {
                    UnmountAfterFailedMount();
                } catch (Exception cleanupFailure) {
                    throw new InvalidOperationException(
                        $"{GetType().Name} failed while mounting and could not clean up completely.",
                        new AggregateException(mountFailure, cleanupFailure));
                }

                throw;
            }
        }

        internal void Unmount() {
            if (_state == WidgetNodeState.Disposed) {
                return;
            }

            if (_state != WidgetNodeState.Mounted) {
                throw new InvalidOperationException($"{GetType().Name} cannot be unmounted from {_state} state.");
            }

            UnmountCore();
        }

        internal void UnmountAfterFailedMount() {
            if (_state is WidgetNodeState.Disposed or WidgetNodeState.Created) {
                return;
            }

            UnmountCore();
        }

        protected WidgetNode MountChild(Widget child, VisualElement nativeParent) {
            return MountChild(child, nativeParent, Context);
        }

        protected WidgetNode MountChild(Widget child, VisualElement nativeParent, BuildContext context) {
            if (child is null) {
                throw new ArgumentNullException(nameof(child));
            }

            if (nativeParent is null) {
                throw new ArgumentNullException(nameof(nativeParent));
            }

            if (context is null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (_state is not (WidgetNodeState.Mounting or WidgetNodeState.Mounted)) {
                throw new InvalidOperationException("Children can only be mounted while their parent is active.");
            }

            var childNode = child.CreateNode();
            childNode.Mount(this, context, nativeParent);
            _children.Add(childNode);
            RefreshLayoutParentSpacing();
            return childNode;
        }

        /// <summary>
        /// Removes one child that this node previously mounted. This is used by
        /// localized structural widgets that replace only their own subtree.
        /// </summary>
        protected void UnmountChild(WidgetNode child) {
            if (child is null) {
                throw new ArgumentNullException(nameof(child));
            }

            if (!_children.Remove(child)) {
                throw new InvalidOperationException("The widget node does not own this child.");
            }

            child.UnmountAfterFailedMount();
            RefreshLayoutParentSpacing();
        }

        protected void MoveChildToIndex(WidgetNode child, int index) {
            if (_children.IndexOf(child) == index) {
                return;
            }

            if (!_children.Remove(child)) throw new InvalidOperationException("The widget node does not own this child.");
            _children.Insert(index, child);
            var nativeParent = child.NativeElement.parent
                ?? throw new InvalidOperationException("A mounted child must have a native parent before it can be reordered.");
            child.NativeElement.RemoveFromHierarchy();
            nativeParent.Insert(index, child.NativeElement);
            RefreshLayoutParentSpacing();
        }

        /// <summary>
        /// Moves one owned child's native root to another native host without changing
        /// its logical parent or mounted state. Transparent descendants that share the
        /// root receive the new native parent as well.
        /// </summary>
        protected void MoveChildToNativeParent(WidgetNode child, VisualElement nativeParent) {
            if (child is null) throw new ArgumentNullException(nameof(child));
            if (nativeParent is null) throw new ArgumentNullException(nameof(nativeParent));
            if (!_children.Contains(child)) {
                throw new InvalidOperationException("The widget node does not own this child.");
            }

            child.UpdateNativeParentForSharedRoot(nativeParent, child.NativeElement);
            if (!ReferenceEquals(child.NativeElement.parent, nativeParent)) {
                child.NativeElement.RemoveFromHierarchy();
                nativeParent.Add(child.NativeElement);
            }
        }

        private void UpdateNativeParentForSharedRoot(VisualElement nativeParent, VisualElement sharedRoot) {
            NativeParent = nativeParent;
            foreach (var child in _children) {
                if (ReferenceEquals(child.NativeElement, sharedRoot)) {
                    child.UpdateNativeParentForSharedRoot(nativeParent, sharedRoot);
                }
            }
        }

        internal virtual bool TryUpdate(Widget nextWidget) => false;

        internal void RegisterInheritedDependency(IInheritedScope scope) {
            if (scope is null) throw new ArgumentNullException(nameof(scope));
            if (_state is not (WidgetNodeState.Mounting or WidgetNodeState.Mounted)) return;
            if (_inheritedDependencies.Add(scope)) scope.AddDependent(this);
        }

        internal void NotifyInheritedChanged(IInheritedScope scope) {
            if (_state != WidgetNodeState.Mounted || !_inheritedDependencies.Contains(scope)) return;
            var previousDependencies = new IInheritedScope[_inheritedDependencies.Count];
            _inheritedDependencies.CopyTo(previousDependencies);
            ResetInheritedDependencies();
            try {
                OnInheritedChanged(scope.Aspect);
            } catch {
                ResetInheritedDependencies();
                foreach (var dependency in previousDependencies) {
                    if (_inheritedDependencies.Add(dependency)) dependency.AddDependent(this);
                }
                throw;
            }
        }

        protected void ResetInheritedDependencies() {
            foreach (var dependency in _inheritedDependencies) {
                dependency.RemoveDependent(this);
            }
            _inheritedDependencies.Clear();
        }

        protected T ReevaluateInheritedDependencies<T>(Func<T> evaluation) {
            if (evaluation is null) throw new ArgumentNullException(nameof(evaluation));
            var previousDependencies = new IInheritedScope[_inheritedDependencies.Count];
            _inheritedDependencies.CopyTo(previousDependencies);
            ResetInheritedDependencies();
            try {
                return evaluation();
            } catch {
                ResetInheritedDependencies();
                foreach (var dependency in previousDependencies) {
                    if (_inheritedDependencies.Add(dependency)) dependency.AddDependent(this);
                }
                throw;
            }
        }

        protected virtual void OnInheritedChanged(InheritedAspect aspect) {
        }

        protected bool CanUpdateWith(Widget nextWidget) {
            return nextWidget is not null
                && nextWidget.GetType() == Widget.GetType()
                && Nullable.Equals(nextWidget.Key, Widget.Key);
        }

        protected void UpdateWidget(Widget nextWidget) {
            Widget = nextWidget ?? throw new ArgumentNullException(nameof(nextWidget));
            RefreshSemantics();
        }

        /// <summary>Returns this node's current assistive-technology annotation.</summary>
        protected virtual SemanticsProperties? DescribeSemantics() => null;

        /// <summary>Whether semantic nodes below this node must be omitted.</summary>
        protected virtual bool SuppressesDescendantSemantics => false;

        /// <summary>Re-applies semantic state after externally owned values change.</summary>
        protected void RefreshSemantics() {
            if (_state is not (WidgetNodeState.Mounting or WidgetNodeState.Mounted)) return;
            if (TryResolveSemanticParent(out var parentNode, out var suppressed) && suppressed) {
                _semanticsRegistration?.Dispose();
                _semanticsRegistration = null;
                return;
            }

            var properties = DescribeSemantics();
            if (properties is null) {
                _semanticsRegistration?.Dispose();
                _semanticsRegistration = null;
                return;
            }

            if (_semanticsRegistration is null) {
                _semanticsRegistration = Context.Semantics.Register(this, properties, parentNode);
            } else {
                _semanticsRegistration.Update(properties);
            }
        }

        private bool TryResolveSemanticParent(
            out UnityEngine.Accessibility.AccessibilityNode? parentNode,
            out bool suppressed) {
            var ancestor = _parent;
            while (ancestor is not null) {
                if (ancestor.SuppressesDescendantSemantics) {
                    parentNode = null;
                    suppressed = true;
                    return true;
                }

                if (ancestor._semanticsRegistration is { } registration) {
                    parentNode = registration.Node;
                    suppressed = false;
                    return true;
                }

                ancestor = ancestor._parent;
            }

            parentNode = null;
            suppressed = false;
            return false;
        }

        /// <summary>
        /// Updates one owned child in place when its type and key are compatible,
        /// otherwise mounts the replacement before releasing the previous subtree.
        /// </summary>
        protected void ReconcileSingleChild(
            ref WidgetNode? currentChild,
            Widget nextWidget,
            VisualElement nativeParent,
            BuildContext? context = null) {
            if (currentChild is not null && ReferenceEquals(currentChild.Configuration, nextWidget)) {
                return;
            }

            if (currentChild is not null) {
                try {
                    if (currentChild.TryUpdate(nextWidget)) {
                        RefreshTransparentNativeElement(currentChild);
                        return;
                    }
                } catch {
                    RefreshTransparentNativeElement(currentChild);
                    throw;
                }
            }

            var previous = currentChild;
            var previousNativeIndex = previous is null ? -1 : nativeParent.IndexOf(previous.NativeElement);
            var nextChild = MountChild(nextWidget, nativeParent, context ?? Context);
            try {
                if (previousNativeIndex >= 0) {
                    nextChild.NativeElement.RemoveFromHierarchy();
                    nativeParent.Insert(previousNativeIndex, nextChild.NativeElement);
                }
            } catch (Exception placementFailure) {
                try {
                    UnmountChild(nextChild);
                } catch (Exception cleanupFailure) {
                    throw new InvalidOperationException(
                        "Single-child replacement failed while placing and cleaning its new child.",
                        new AggregateException(placementFailure, cleanupFailure));
                }

                throw;
            }

            currentChild = nextChild;
            RefreshTransparentNativeElement(nextChild);
            if (previous is not null) UnmountChild(previous);
        }

        protected void ReconcileSingleChild<TNode>(
            ref TNode? currentChild,
            Widget nextWidget,
            VisualElement nativeParent,
            BuildContext? context = null)
            where TNode : WidgetNode {
            WidgetNode? untyped = currentChild;
            try {
                ReconcileSingleChild(ref untyped, nextWidget, nativeParent, context);
            } finally {
                currentChild = (TNode?)untyped;
            }
        }

        /// <summary>
        /// Reconciles a direct multi-child hierarchy. Unkeyed children match their
        /// existing position; keyed children match by key and can move without losing
        /// a compatible mounted node.
        /// </summary>
        protected void ReconcileChildren(IReadOnlyList<Widget> nextWidgets, VisualElement nativeParent) {
            if (nextWidgets is null) throw new ArgumentNullException(nameof(nextWidgets));
            if (nativeParent is null) throw new ArgumentNullException(nameof(nativeParent));

            ValidateUniqueKeys(nextWidgets);
            var previous = _children.ToArray();
            var keyedPrevious = new Dictionary<WidgetKey, WidgetNode>();
            foreach (var child in previous) {
                if (child.Configuration.Key is { } key) {
                    keyedPrevious.Add(key, child);
                }
            }

            var retained = new HashSet<WidgetNode>();
            var resolved = new List<WidgetNode>(nextWidgets.Count);
            var newlyMounted = new List<WidgetNode>();
            try {
                for (var index = 0; index < nextWidgets.Count; index++) {
                    var nextWidget = nextWidgets[index]
                        ?? throw new ArgumentException("Widget children cannot contain null.", nameof(nextWidgets));
                    WidgetNode? candidate = null;
                    if (nextWidget.Key is { } key) {
                        keyedPrevious.TryGetValue(key, out candidate);
                    } else if (index < previous.Length
                          && previous[index].Configuration.Key is null
                          && !retained.Contains(previous[index])) {
                        candidate = previous[index];
                    }

                    if (candidate is not null
                        && !retained.Contains(candidate)
                        && (ReferenceEquals(candidate.Configuration, nextWidget) || candidate.TryUpdate(nextWidget))) {
                        retained.Add(candidate);
                        resolved.Add(candidate);
                        continue;
                    }

                    var mounted = MountChild(nextWidget, nativeParent);
                    newlyMounted.Add(mounted);
                    resolved.Add(mounted);
                }
            } catch (Exception updateFailure) {
                Exception? cleanupFailure = null;
                for (var index = newlyMounted.Count - 1; index >= 0; index--) {
                    try {
                        UnmountChild(newlyMounted[index]);
                    } catch (Exception exception) {
                        cleanupFailure ??= exception;
                    }
                }

                if (cleanupFailure is not null) {
                    throw new InvalidOperationException(
                        "Multi-child reconciliation failed and could not fully clean its newly mounted children.",
                        new AggregateException(updateFailure, cleanupFailure));
                }

                throw;
            }

            List<Exception>? removalFailures = null;
            foreach (var child in previous) {
                if (!retained.Contains(child)) {
                    try {
                        UnmountChild(child);
                    } catch (Exception exception) {
                        removalFailures ??= new List<Exception>();
                        removalFailures.Add(exception);
                    }
                }
            }

            for (var index = 0; index < resolved.Count; index++) {
                MoveChildToIndex(resolved[index], index);
            }

            if (removalFailures is { Count: 1 }) {
                throw new InvalidOperationException(
                    "Multi-child reconciliation updated the hierarchy, but a removed child failed to clean up.",
                    removalFailures[0]);
            }

            if (removalFailures is { Count: > 1 }) {
                throw new InvalidOperationException(
                    "Multi-child reconciliation updated the hierarchy, but multiple removed children failed to clean up.",
                    new AggregateException(removalFailures));
            }
        }

        private void ValidateUniqueKeys(IReadOnlyList<Widget> widgets) {
            Dictionary<WidgetKey, int>? keyIndexes = null;
            for (var index = 0; index < widgets.Count; index++) {
                var widget = widgets[index]
                    ?? throw new ArgumentException("Widget children cannot contain null.", nameof(widgets));
                if (widget.Key is not { } key) continue;
                keyIndexes ??= new Dictionary<WidgetKey, int>();
                if (keyIndexes.TryGetValue(key, out var previousIndex)) {
                    throw new InvalidOperationException(
                        $"{Widget.GetType().Name} received duplicate sibling key '{key.Value}' "
                        + $"at indexes {previousIndex} and {index}. Assign a unique WidgetKey to each sibling.");
                }
                keyIndexes.Add(key, index);
            }
        }

        protected abstract VisualElement? CreateElement(BuildContext context);

        protected void AdoptNativeElement(VisualElement element) {
            Element = element ?? throw new ArgumentNullException(nameof(element));
        }

        protected virtual void OnMounted() {
        }

        private static WidgetNode? FindLayoutParent(WidgetNode? node) {
            while (node is ITransparentWidgetNode) {
                node = node.Parent;
            }

            return node;
        }

        private void RefreshTransparentNativeElement(WidgetNode child) {
            if (this is ITransparentWidgetNode) AdoptNativeElement(child.NativeElement);
        }

        private void RefreshLayoutParentSpacing() {
            if (FindLayoutParent(this) is IFlexParentNode flexParent) {
                flexParent.RefreshChildSpacing();
            }
        }

        private void UnmountCore() {
            _state = WidgetNodeState.Unmounting;
            List<Exception>? failures = null;
            try {
                ResetInheritedDependencies();
            } catch (Exception exception) {
                failures = new List<Exception> { exception };
            }

            for (var index = _children.Count - 1; index >= 0; index--) {
                try {
                    _children[index].UnmountAfterFailedMount();
                } catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            _children.Clear();

            try {
                _semanticsRegistration?.Dispose();
                _semanticsRegistration = null;
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                _bindingScope.Dispose();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try {
                Element?.RemoveFromHierarchy();
            } catch (Exception exception) {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            _parent = null;
            _state = WidgetNodeState.Disposed;

            if (failures is { Count: 1 }) {
                throw new InvalidOperationException($"{GetType().Name} failed while unmounting.", failures[0]);
            }

            if (failures is { Count: > 1 }) {
                throw new InvalidOperationException(
                    $"{GetType().Name} encountered multiple failures while unmounting.",
                    new AggregateException(failures));
            }
        }
    }

    /// <summary>
    /// Shared reconciliation contract for widgets that own exactly one child.
    /// Derived nodes keep their native wrapper and update the compatible child in
    /// place, so layout decoration does not interrupt state, focus, or bindings.
    /// </summary>
    internal abstract class SingleChildWidgetNode<TWidget> : WidgetNode
        where TWidget : Widget {
        private WidgetNode? _currentChild;

        protected SingleChildWidgetNode(TWidget widget)
            : base(widget) {
        }

        protected TWidget CurrentWidget => (TWidget)Configuration;

        protected virtual VisualElement ChildContainer => Element;

        protected abstract Widget GetChild(TWidget widget);

        protected override void OnMounted() {
            var widget = CurrentWidget;
            _currentChild = MountChild(GetChild(widget), ChildContainer);
            OnChildReconciled(widget, _currentChild.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TWidget typedWidget) {
                return false;
            }

            ReconcileConfiguredChild(typedWidget);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyConfiguration(typedWidget);
                OnChildReconciled(typedWidget, _currentChild!.NativeElement);
                return true;
            });
            UpdateWidget(typedWidget);
            return true;
        }

        /// <summary>Rebuilds this wrapper's child without replacing the wrapper configuration.</summary>
        protected void ReconcileConfiguredChild(TWidget widget) {
            ReconcileSingleChild(
                ref _currentChild,
                GetChild(widget),
                ChildContainer);
        }

        /// <summary>Applies mutable native wrapper properties from a new configuration.</summary>
        protected virtual void ApplyConfiguration(TWidget widget) {
        }

        /// <summary>Applies properties owned by the wrapper to its current native child.</summary>
        protected virtual void OnChildReconciled(TWidget widget, VisualElement childElement) {
        }
    }

    internal interface ITransparentWidgetNode {
    }

}
