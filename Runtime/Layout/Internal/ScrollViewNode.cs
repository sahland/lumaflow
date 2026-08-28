#nullable enable

using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ScrollViewNode : SingleChildWidgetNode<ScrollView> {
        public ScrollViewNode(ScrollView widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var direction = ((ScrollView)Widget).Direction;
            var scrollView = new UnityEngine.UIElements.ScrollView(
                direction == Axis.Horizontal ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical);
            // A ScrollView is the overflow boundary of a flex layout. It fills the
            // available region but is allowed to become smaller than its content.
            scrollView.style.flexGrow = 1f;
            scrollView.style.flexShrink = 1f;
            scrollView.style.minWidth = 0f;
            scrollView.style.minHeight = 0f;
            // UI Toolkit retains wheel and touch scrolling when its chrome is hidden.
            // LumaFlow follows mobile conventions: scrollbars do not occupy visual space.
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            return scrollView;
        }

        protected override void OnMounted() {
            ApplyPadding((ScrollView)Widget);
            base.OnMounted();
        }

        protected override VisualElement ChildContainer =>
            ((UnityEngine.UIElements.ScrollView)Element).contentContainer;

        protected override Widget GetChild(ScrollView widget) => widget.Child;

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not ScrollView next
                || !CanUpdateWith(next)
                || next.Direction != ((ScrollView)Widget).Direction) {
                return false;
            }

            return base.TryUpdate(next);
        }

        protected override void ApplyConfiguration(ScrollView widget) => ApplyPadding(widget);

        protected override SemanticsProperties DescribeSemantics() => new(
            role: SemanticsRole.ScrollView);

        private void ApplyPadding(ScrollView widget) {
            var content = ((UnityEngine.UIElements.ScrollView)Element).contentContainer;
            PaddingStyleMapper.Clear(content);
            if (widget.Padding is { } padding) PaddingStyleMapper.Apply(content, padding);
        }
    }

}
