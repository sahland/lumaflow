using System;

namespace LumaFlow {

    /// <summary>
    /// Applies an opacity value to one child while preserving its layout space.
    /// </summary>
    public sealed class Opacity : Widget {
        public Opacity(Widget child, float value) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f) {
                throw new ArgumentOutOfRangeException(nameof(value), "Opacity must be finite and between zero and one.");
            }

            Value = value;
        }

        public Widget Child { get; }

        public float Value { get; }

        internal override WidgetNode CreateNode() {
            return new OpacityNode(this);
        }
    }

}
