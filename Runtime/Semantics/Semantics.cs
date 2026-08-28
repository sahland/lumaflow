#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Adds an explicit accessibility annotation around one child.</summary>
    public sealed class Semantics : Widget {
        public Semantics(
            Widget child,
            SemanticsProperties properties,
            bool excludeDescendantSemantics = false) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            ExcludeDescendantSemantics = excludeDescendantSemantics;
        }

        public Widget Child { get; }
        public SemanticsProperties Properties { get; }

        /// <summary>
        /// Whether this node replaces, rather than contains, semantic annotations
        /// supplied by its descendants.
        /// </summary>
        public bool ExcludeDescendantSemantics { get; }

        internal override WidgetNode CreateNode() => new SemanticsNode(this);
    }

    /// <summary>Removes all semantic annotations below this transparent boundary.</summary>
    public sealed class ExcludeSemantics : Widget {
        public ExcludeSemantics(Widget child) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public Widget Child { get; }

        internal override WidgetNode CreateNode() => new ExcludeSemanticsNode(this);
    }

    internal sealed class SemanticsNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;

        public SemanticsNode(Semantics widget)
            : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override SemanticsProperties DescribeSemantics() =>
            ((Semantics)Configuration).Properties;

        protected override bool SuppressesDescendantSemantics =>
            ((Semantics)Configuration).ExcludeDescendantSemantics;

        protected override void OnMounted() {
            _currentChild = MountChild(((Semantics)Configuration).Child, NativeParent);
            AdoptNativeElement(_currentChild.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Semantics semantics) return false;
            var previous = (Semantics)Configuration;
            if (previous.ExcludeDescendantSemantics != semantics.ExcludeDescendantSemantics) return false;
            ReconcileSingleChild(ref _currentChild, semantics.Child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
            UpdateWidget(semantics);
            return true;
        }
    }

    internal sealed class ExcludeSemanticsNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;

        public ExcludeSemanticsNode(ExcludeSemantics widget)
            : base(widget) {
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override bool SuppressesDescendantSemantics => true;

        protected override void OnMounted() {
            _currentChild = MountChild(((ExcludeSemantics)Configuration).Child, NativeParent);
            AdoptNativeElement(_currentChild.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not ExcludeSemantics excluded) return false;
            ReconcileSingleChild(ref _currentChild, excluded.Child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
            UpdateWidget(excluded);
            return true;
        }
    }
}
