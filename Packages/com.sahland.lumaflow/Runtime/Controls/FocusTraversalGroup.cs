#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>
    /// Scopes Tab and Shift+Tab traversal to the explicitly connected
    /// <see cref="FocusNode"/> instances below its child.
    /// </summary>
    public sealed class FocusTraversalGroup : Widget {
        public FocusTraversalGroup(Widget child) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public Widget Child { get; }

        internal override WidgetNode CreateNode() => new FocusTraversalGroupNode(this);
    }

    internal sealed class FocusTraversalGroupNode : WidgetNode, ITransparentWidgetNode {
        private readonly FocusTraversalController _controller = new();
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;
        private VisualElement? _keyEventElement;

        public FocusTraversalGroupNode(FocusTraversalGroup widget)
            : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            _childContext = Context.WithFocusTraversal(_controller);
            _currentChild = MountChild(
                ((FocusTraversalGroup)Widget).Child,
                NativeParent,
                _childContext);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindKeyHandler();
            Bindings.Add(ReleaseKeyHandler);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not FocusTraversalGroup group) return false;
            ReconcileSingleChild(ref _currentChild, group.Child, NativeParent, _childContext);
            ReleaseKeyHandler();
            UpdateWidget(group);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindKeyHandler();
            return true;
        }

        internal FocusTraversalController Controller => _controller;

        internal void HandleKeyDown(KeyDownEvent evt) {
            if (evt.keyCode != KeyCode.Tab || !_controller.Move(forward: !evt.shiftKey)) {
                return;
            }

            evt.StopPropagation();
        }

        private void BindKeyHandler() {
            _keyEventElement = Element;
            _keyEventElement.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
        }

        private void ReleaseKeyHandler() {
            _keyEventElement?.UnregisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            _keyEventElement = null;
        }
    }

    /// <summary>
    /// Mount-local ordered registry. Registration follows native mount order and is
    /// released with the control binding, so the controller never owns widget state.
    /// </summary>
    internal sealed class FocusTraversalController {
        private readonly List<FocusNode> _nodes = new();

        public IDisposable Register(FocusNode node) {
            if (node is null) throw new ArgumentNullException(nameof(node));
            if (_nodes.Contains(node)) {
                throw new InvalidOperationException(
                    "A FocusNode can be registered only once in the same FocusTraversalGroup.");
            }

            _nodes.Add(node);
            return new Registration(this, node);
        }

        public bool Move(bool forward) {
            if (_nodes.Count == 0) {
                return false;
            }

            var activeIndex = _nodes.FindIndex(node => node.IsFocused.Value);
            var direction = forward ? 1 : -1;
            var startIndex = activeIndex < 0
                ? (forward ? 0 : _nodes.Count - 1)
                : activeIndex + direction;

            for (var offset = 0; offset < _nodes.Count; offset++) {
                var index = Modulo(startIndex + offset * direction, _nodes.Count);
                if (_nodes[index].RequestFocus()) {
                    return true;
                }
            }

            return false;
        }

        private void Unregister(FocusNode node) {
            _nodes.Remove(node);
        }

        private static int Modulo(int value, int divisor) {
            var remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        private sealed class Registration : IDisposable {
            private FocusTraversalController? _owner;
            private FocusNode? _node;

            public Registration(FocusTraversalController owner, FocusNode node) {
                _owner = owner;
                _node = node;
            }

            public void Dispose() {
                if (_owner is not null && _node is not null) {
                    _owner.Unregister(_node);
                }

                _owner = null;
                _node = null;
            }
        }
    }
}
