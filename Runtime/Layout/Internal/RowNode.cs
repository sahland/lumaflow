#nullable enable

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class RowNode : WidgetNode, IFlexParentNode {
        private readonly Dictionary<VisualElement, float> _baseRightMargins = new();

        public RowNode(Row widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var widget = (Row)Widget;
            LayoutAlignmentStyleMapper.ApplyMainAxis(row, widget.MainAxisAlignment);
            LayoutAlignmentStyleMapper.ApplyCrossAxis(row, widget.CrossAxisAlignment);
            return row;
        }

        protected override void OnMounted() {
            ReconcileChildren(((Row)Widget).Children, Element);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not Row next || !CanUpdateWith(next)) return false;
            ReconcileChildren(next.Children, Element);
            UpdateWidget(next);
            LayoutAlignmentStyleMapper.ApplyMainAxis(Element, next.MainAxisAlignment);
            LayoutAlignmentStyleMapper.ApplyCrossAxis(Element, next.CrossAxisAlignment);
            RefreshChildSpacing();
            return true;
        }

        public void RefreshChildSpacing() {
            var row = (Row)Widget;
            for (var index = 0; index < Element.childCount; index++) {
                var child = Element[index];
                if (!_baseRightMargins.TryGetValue(child, out var baseMargin)) {
                    baseMargin = GetPixelMargin(child.style.marginRight);
                    _baseRightMargins.Add(child, baseMargin);
                }

                child.style.marginRight = baseMargin + (index < Element.childCount - 1 ? row.Gap : 0f);
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
            foreach (var element in _baseRightMargins.Keys) {
                if (element.parent != Element) {
                    detached ??= new List<VisualElement>();
                    detached.Add(element);
                }
            }

            if (detached is null) return;
            foreach (var element in detached) _baseRightMargins.Remove(element);
        }
    }

}
