#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Handles the scoped Back/Escape contract for one application subtree.
    /// An open overlay is dismissed before the active navigator route is popped.
    /// </summary>
    public sealed class BackNavigation : Widget {
        public BackNavigation(Widget child, Navigator navigator, OverlayController overlay) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            Overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        }

        public Widget Child { get; }
        public Navigator Navigator { get; }
        public OverlayController Overlay { get; }

        internal override WidgetNode CreateNode() => new BackNavigationNode(this);
    }

    internal sealed class BackNavigationNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private VisualElement? _keyEventElement;

        public BackNavigationNode(BackNavigation widget) : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            _currentChild = MountChild(((BackNavigation)Widget).Child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindKeyHandler();
            Bindings.Add(ReleaseKeyHandler);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not BackNavigation navigation) return false;
            ReconcileSingleChild(ref _currentChild, navigation.Child, NativeParent);
            ReleaseKeyHandler();
            UpdateWidget(navigation);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindKeyHandler();
            return true;
        }

        internal bool TryHandleBack() {
            var widget = (BackNavigation)Widget;
            if (widget.Overlay.HasOpenEntries) {
                widget.Overlay.TryHandleBack();
                return true;
            }
            return widget.Navigator.CanPop && widget.Navigator.Pop();
        }

        private void OnKeyDown(KeyDownEvent evt) {
            if (evt.keyCode != KeyCode.Escape || !TryHandleBack()) return;
            evt.StopPropagation();
        }

        private void BindKeyHandler() {
            _keyEventElement = Element;
            _keyEventElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void ReleaseKeyHandler() {
            _keyEventElement?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _keyEventElement = null;
        }
    }

}
