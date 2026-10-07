#nullable enable

using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>Allocation-free native style appliers for <see cref="AnimatedStyle{T}"/>.</summary>
    public static class AnimatedStyleProperties {
        public static void Opacity(VisualElement element, float value) => element.style.opacity = value;

        public static void BackgroundColor(VisualElement element, Color value) => element.style.backgroundColor = value;

        public static void Width(VisualElement element, float value) => element.style.width = value;

        public static void Height(VisualElement element, float value) => element.style.height = value;

        public static void Translation(VisualElement element, Vector2 value) =>
            element.style.translate = new Translate(value.x, value.y);

        public static void Scale(VisualElement element, Vector2 value) => element.style.scale = new Scale(value);

        public static void RotationDegrees(VisualElement element, float value) =>
            element.style.rotate = new Rotate(Angle.Degrees(value));

        public static void BorderRadius(VisualElement element, BorderRadius value) {
            element.style.borderTopLeftRadius = value.TopLeft;
            element.style.borderTopRightRadius = value.TopRight;
            element.style.borderBottomRightRadius = value.BottomRight;
            element.style.borderBottomLeftRadius = value.BottomLeft;
        }
    }
}
