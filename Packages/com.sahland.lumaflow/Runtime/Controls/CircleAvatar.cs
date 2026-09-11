#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>A circular application-media surface with optional background image and foreground child.</summary>
    public sealed class CircleAvatar : Widget {
        public CircleAvatar(
            Widget? child = null,
            Image? backgroundImage = null,
            float radius = 20f,
            Color? backgroundColor = null,
            string? semanticsLabel = null) {
            if (!float.IsFinite(radius) || radius <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(radius), "Avatar radius must be finite and greater than zero.");
            }
            Child = child;
            BackgroundImage = backgroundImage;
            Radius = radius;
            BackgroundColor = backgroundColor;
            SemanticsLabel = semanticsLabel;
        }

        public Widget? Child { get; }
        public Image? BackgroundImage { get; }
        public float Radius { get; }
        public Color? BackgroundColor { get; }
        public string? SemanticsLabel { get; }

        internal override WidgetNode CreateNode() => new CircleAvatarNode(this);
    }

    internal sealed class CircleAvatarNode : WidgetNode {
        private WidgetNode? _backgroundNode;
        private WidgetNode? _childNode;

        public CircleAvatarNode(CircleAvatar widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = UnityEngine.UIElements.Align.Center;
            root.style.overflow = Overflow.Hidden;
            ApplySurface(root, (CircleAvatar)Widget);
            return root;
        }

        protected override void OnMounted() => ReconcileSlots((CircleAvatar)Widget);

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not CircleAvatar avatar) return false;
            UpdateWidget(avatar);
            ApplySurface(Element, avatar);
            ReconcileSlots(avatar);
            RefreshSemantics();
            return true;
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (CircleAvatar)Widget;
            return new SemanticsProperties(
                label: widget.SemanticsLabel,
                role: string.IsNullOrWhiteSpace(widget.SemanticsLabel)
                    ? SemanticsRole.Container
                    : SemanticsRole.Image);
        }

        private void ReconcileSlots(CircleAvatar widget) {
            ReconcileOptional(ref _backgroundNode, widget.BackgroundImage);
            if (_backgroundNode is not null) {
                _backgroundNode.NativeElement.style.position = Position.Absolute;
                _backgroundNode.NativeElement.style.left = 0f;
                _backgroundNode.NativeElement.style.top = 0f;
                _backgroundNode.NativeElement.style.right = 0f;
                _backgroundNode.NativeElement.style.bottom = 0f;
                _backgroundNode.NativeElement.style.width = StyleKeyword.Null;
                _backgroundNode.NativeElement.style.height = StyleKeyword.Null;
                MoveChildToIndex(_backgroundNode, 0);
            }
            ReconcileOptional(ref _childNode, widget.Child);
            if (_childNode is not null) MoveChildToIndex(_childNode, _backgroundNode is null ? 0 : 1);
        }

        private void ReconcileOptional(ref WidgetNode? current, Widget? next) {
            if (next is null) {
                var previous = current;
                current = null;
                if (previous is not null) UnmountChild(previous);
                return;
            }
            ReconcileSingleChild(ref current, next, Element);
        }

        private static void ApplySurface(VisualElement root, CircleAvatar widget) {
            var diameter = widget.Radius * 2f;
            root.style.width = diameter;
            root.style.height = diameter;
            root.style.minWidth = diameter;
            root.style.minHeight = diameter;
            root.style.flexShrink = 0f;
            root.style.backgroundColor = widget.BackgroundColor is { } color ? color : StyleKeyword.Null;
            root.style.borderTopLeftRadius = widget.Radius;
            root.style.borderTopRightRadius = widget.Radius;
            root.style.borderBottomRightRadius = widget.Radius;
            root.style.borderBottomLeftRadius = widget.Radius;
        }
    }
}
