#nullable enable

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class ColumnNode : WidgetNode, IFlexParentNode {
        private readonly Dictionary<VisualElement, float> _baseBottomMargins = new();

        public ColumnNode(Column widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            var widget = (Column)Widget;
            LayoutAlignmentStyleMapper.ApplyMainAxis(column, widget.MainAxisAlignment);
            LayoutAlignmentStyleMapper.ApplyCrossAxis(column, widget.CrossAxisAlignment);
            return column;
        }

        protected override void OnMounted() {
            ReconcileChildren(((Column)Widget).Children, Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not Column next || !CanUpdateWith(next)) return false;
            ReconcileChildren(next.Children, Element);
            UpdateWidget(next);
            LayoutAlignmentStyleMapper.ApplyMainAxis(Element, next.MainAxisAlignment);
            LayoutAlignmentStyleMapper.ApplyCrossAxis(Element, next.CrossAxisAlignment);
            RefreshChildSpacing();
            return true;
        }

        public void RefreshChildSpacing() {
            var column = (Column)Widget;
            for (var index = 0; index < Element.childCount; index++) {
                var child = Element[index];
                if (!_baseBottomMargins.TryGetValue(child, out var baseMargin)) {
                    baseMargin = GetPixelMargin(child.style.marginBottom);
                    _baseBottomMargins.Add(child, baseMargin);
                }

                child.style.marginBottom = baseMargin + (index < Element.childCount - 1 ? column.Gap : 0f);
            }

            RemoveDetachedMargins();
        }

        private static float GetPixelMargin(StyleLength margin) {
            return margin.keyword == StyleKeyword.Null || margin.value.unit != LengthUnit.Pixel
                ? 0f
                : margin.value.value;
        }

        private void RemoveDetachedMargins() {
            List<VisualElement>? detached = null;
            foreach (var element in _baseBottomMargins.Keys) {
                if (element.parent != Element) {
                    detached ??= new List<VisualElement>();
                    detached.Add(element);
                }
            }

            if (detached is null) return;
            foreach (var element in detached) _baseBottomMargins.Remove(element);
        }
    }

}
