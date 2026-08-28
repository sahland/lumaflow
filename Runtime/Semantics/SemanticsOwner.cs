#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;

namespace LumaFlow {

    /// <summary>Owns the native Unity accessibility hierarchy for one mounted tree.</summary>
    internal sealed class SemanticsOwner : IDisposable {
        private readonly HashSet<Registration> _registrations = new();
        private bool _disposed;

        public SemanticsOwner() {
            Hierarchy = new AccessibilityHierarchy();
            SemanticsActivation.Register(this);
        }

        internal AccessibilityHierarchy Hierarchy { get; }

        internal Registration Register(
            WidgetNode owner,
            SemanticsProperties properties,
            AccessibilityNode? parent) {
            if (_disposed) throw new ObjectDisposedException(nameof(SemanticsOwner));
            var registration = new Registration(this, owner, properties, parent);
            _registrations.Add(registration);
            NotifyLayoutChanged(registration.Node);
            return registration;
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            foreach (var registration in new List<Registration>(_registrations)) {
                registration.Dispose();
            }
            _registrations.Clear();
            SemanticsActivation.Unregister(this);
        }

        private void Remove(Registration registration) {
            if (!_registrations.Remove(registration)) return;
            if (Hierarchy.ContainsNode(registration.Node)) {
                Hierarchy.RemoveNode(registration.Node, removeChildren: false);
            }
            NotifyLayoutChanged(null);
        }

        private void NotifyLayoutChanged(AccessibilityNode? node) {
            if (!SemanticsService.IsScreenReaderSupported
                || !ReferenceEquals(AssistiveSupport.activeHierarchy, Hierarchy)) {
                return;
            }

            AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
        }

        internal sealed class Registration : IDisposable {
            private SemanticsOwner? _owner;
            private WidgetNode? _widgetNode;
            private SemanticsProperties _properties;
            private bool _invokedBound;
            private bool _incrementedBound;
            private bool _decrementedBound;
            private bool _dismissedBound;
            private bool _focusBound;

            internal Registration(
                SemanticsOwner owner,
                WidgetNode widgetNode,
                SemanticsProperties properties,
                AccessibilityNode? parent) {
                _owner = owner;
                _widgetNode = widgetNode;
                _properties = properties;
                Node = owner.Hierarchy.AddNode(properties.Label ?? string.Empty, parent);
                Apply(properties);
            }

            internal AccessibilityNode Node { get; }

            internal void Update(SemanticsProperties properties) {
                if (_owner is null) return;
                Apply(properties);
                _owner.NotifyLayoutChanged(Node);
            }

            public void Dispose() {
                var owner = _owner;
                if (owner is null) return;
                UnbindActions();
                _owner = null;
                _widgetNode = null;
                owner.Remove(this);
            }

            private void Apply(SemanticsProperties properties) {
                _properties = properties;
                Node.label = properties.Label ?? string.Empty;
                Node.value = properties.Value ?? string.Empty;
                Node.hint = properties.Hint ?? string.Empty;
                Node.role = MapRole(properties.Role);
                Node.state = ResolveState(properties);
                Node.isActive = !properties.Hidden;
                Node.allowsDirectInteraction = properties.AllowsDirectInteraction;
                Node.frameGetter = properties.FrameGetter ?? ResolveFrame;
                UpdateActionBindings();
            }

            private Rect ResolveFrame() {
                var widgetNode = _widgetNode;
                if (widgetNode is null || !widgetNode.IsMounted) return Rect.zero;
                var bounds = widgetNode.NativeElement.worldBound;
                return new Rect(bounds.x, bounds.y, bounds.width, bounds.height);
            }

            private bool HandleInvoked() {
                var callback = _properties.OnTap ?? _properties.OnSelect;
                if (callback is null) return false;
                callback();
                return true;
            }

            private void HandleIncremented() => _properties.OnIncrease?.Invoke();
            private void HandleDecremented() => _properties.OnDecrease?.Invoke();
            private bool HandleDismissed() => _properties.OnDismiss?.Invoke() ?? false;
            private void HandleFocusChanged(AccessibilityNode _, bool focused) =>
                _properties.OnAccessibilityFocusChanged?.Invoke(focused);

            private void UpdateActionBindings() {
                Bind(ref _invokedBound, _properties.OnTap is not null || _properties.OnSelect is not null,
                    () => Node.invoked += HandleInvoked,
                    () => Node.invoked -= HandleInvoked);
                Bind(ref _incrementedBound, _properties.OnIncrease is not null,
                    () => Node.incremented += HandleIncremented,
                    () => Node.incremented -= HandleIncremented);
                Bind(ref _decrementedBound, _properties.OnDecrease is not null,
                    () => Node.decremented += HandleDecremented,
                    () => Node.decremented -= HandleDecremented);
                Bind(ref _dismissedBound, _properties.OnDismiss is not null,
                    () => Node.dismissed += HandleDismissed,
                    () => Node.dismissed -= HandleDismissed);
                Bind(ref _focusBound, _properties.OnAccessibilityFocusChanged is not null,
                    () => Node.focusChanged += HandleFocusChanged,
                    () => Node.focusChanged -= HandleFocusChanged);
            }

            private void UnbindActions() {
                if (_invokedBound) Node.invoked -= HandleInvoked;
                if (_incrementedBound) Node.incremented -= HandleIncremented;
                if (_decrementedBound) Node.decremented -= HandleDecremented;
                if (_dismissedBound) Node.dismissed -= HandleDismissed;
                if (_focusBound) Node.focusChanged -= HandleFocusChanged;
                _invokedBound = _incrementedBound = false;
                _decrementedBound = _dismissedBound = _focusBound = false;
            }

            private static void Bind(
                ref bool bound,
                bool shouldBind,
                Action bind,
                Action unbind) {
                if (bound == shouldBind) return;
                if (shouldBind) bind();
                else unbind();
                bound = shouldBind;
            }

            private static AccessibilityState ResolveState(SemanticsProperties properties) {
                var state = AccessibilityState.None;
                if (properties.Enabled == false) state |= AccessibilityState.Disabled;
                if (properties.Checked == true || properties.Selected == true) state |= AccessibilityState.Selected;
                if (properties.Expanded == true) state |= AccessibilityState.Expanded;
                return state;
            }

            private static AccessibilityRole MapRole(SemanticsRole role) {
                return role switch {
                    SemanticsRole.Button => AccessibilityRole.Button,
                    SemanticsRole.Toggle => AccessibilityRole.Toggle,
                    SemanticsRole.TextField => AccessibilityRole.TextField,
                    SemanticsRole.SearchField => AccessibilityRole.SearchField,
                    SemanticsRole.Slider => AccessibilityRole.Slider,
                    SemanticsRole.Dropdown => AccessibilityRole.Dropdown,
                    SemanticsRole.Image => AccessibilityRole.Image,
                    SemanticsRole.Header => AccessibilityRole.Header,
                    SemanticsRole.StaticText => AccessibilityRole.StaticText,
                    SemanticsRole.ScrollView => AccessibilityRole.ScrollView,
                    SemanticsRole.Container => AccessibilityRole.Container,
                    SemanticsRole.TabBar => AccessibilityRole.TabBar,
                    SemanticsRole.Tab => AccessibilityRole.TabButton,
                    SemanticsRole.KeyboardKey => AccessibilityRole.KeyboardKey,
                    _ => AccessibilityRole.None
                };
            }
        }

        private static class SemanticsActivation {
            private static readonly List<SemanticsOwner> Owners = new();
            private static AccessibilityHierarchy? _previousHierarchy;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void ResetForPlayMode() {
                var ownsActiveHierarchy = false;
                foreach (var owner in Owners) {
                    if (ReferenceEquals(AssistiveSupport.activeHierarchy, owner.Hierarchy)) {
                        ownsActiveHierarchy = true;
                        break;
                    }
                }

                if (ownsActiveHierarchy) AssistiveSupport.activeHierarchy = _previousHierarchy;
                Owners.Clear();
                _previousHierarchy = null;
            }

            internal static void Register(SemanticsOwner owner) {
                if (!SemanticsService.IsScreenReaderSupported) return;
                if (Owners.Count == 0) _previousHierarchy = AssistiveSupport.activeHierarchy;
                Owners.Add(owner);
                AssistiveSupport.activeHierarchy = owner.Hierarchy;
            }

            internal static void Unregister(SemanticsOwner owner) {
                if (!SemanticsService.IsScreenReaderSupported) return;
                var ownedActiveHierarchy = ReferenceEquals(AssistiveSupport.activeHierarchy, owner.Hierarchy);
                Owners.Remove(owner);
                if (ownedActiveHierarchy) {
                    AssistiveSupport.activeHierarchy = Owners.Count > 0
                        ? Owners[Owners.Count - 1].Hierarchy
                        : _previousHierarchy;
                }
                if (Owners.Count == 0) _previousHierarchy = null;
            }
        }
    }
}
