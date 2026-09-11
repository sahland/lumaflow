#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Provides explicit panel environment metrics to a descendant widget subtree.
    /// </summary>
    /// <remarks>
    /// This widget is transparent in the native hierarchy. <see cref="LayoutBuilder"/>
    /// supplies a fresh scoped value automatically when its observed axis changes.
    /// </remarks>
    public sealed class MediaQuery : Widget {
        public MediaQuery(MediaQueryData data, Widget child) {
            Data = data;
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        /// <summary>Gets the environment data exposed to the child subtree.</summary>
        public MediaQueryData Data { get; }

        /// <summary>Gets the child mounted in this scope.</summary>
        public Widget Child { get; }

        internal override WidgetNode CreateNode() => new MediaQueryNode(this);
    }

    internal sealed class MediaQueryNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;

        public MediaQueryNode(MediaQuery widget)
            : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (MediaQuery)Widget;
            _childContext = Context.WithMediaQuery(widget.Data);
            _currentChild = MountChild(widget.Child, NativeParent, _childContext);
            AdoptNativeElement(_currentChild!.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not MediaQuery mediaQuery) return false;
            var previous = (MediaQuery)Widget;
            if (previous.Data != mediaQuery.Data) _childContext!.UpdateMediaQuery(mediaQuery.Data);
            ReconcileSingleChild(
                ref _currentChild,
                mediaQuery.Child,
                NativeParent,
                _childContext!);
            UpdateWidget(mediaQuery);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }
    }

}
