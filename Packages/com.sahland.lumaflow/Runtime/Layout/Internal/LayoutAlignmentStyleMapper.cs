using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal static class LayoutAlignmentStyleMapper {
        public static void ApplyMainAxis(VisualElement element, MainAxisAlignment alignment) {
            element.style.justifyContent = alignment switch {
                MainAxisAlignment.Start => Justify.FlexStart,
                MainAxisAlignment.Center => Justify.Center,
                MainAxisAlignment.End => Justify.FlexEnd,
                MainAxisAlignment.SpaceBetween => Justify.SpaceBetween,
                MainAxisAlignment.SpaceAround => Justify.SpaceAround,
                MainAxisAlignment.SpaceEvenly => Justify.SpaceEvenly,
                _ => throw new ArgumentOutOfRangeException(nameof(alignment))
            };
        }

        public static void ApplyCrossAxis(VisualElement element, CrossAxisAlignment alignment) {
            element.style.alignItems = alignment switch {
                CrossAxisAlignment.Start => UnityEngine.UIElements.Align.FlexStart,
                CrossAxisAlignment.Center => UnityEngine.UIElements.Align.Center,
                CrossAxisAlignment.End => UnityEngine.UIElements.Align.FlexEnd,
                CrossAxisAlignment.Stretch => UnityEngine.UIElements.Align.Stretch,
                _ => throw new ArgumentOutOfRangeException(nameof(alignment))
            };
        }
    }

}
