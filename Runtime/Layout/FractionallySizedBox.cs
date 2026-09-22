#nullable enable

using System;

namespace LumaFlow {
    /// <summary>Sizes and aligns a child as a fraction of the available parent bounds.</summary>
    public sealed class FractionallySizedBox : Widget {
        public FractionallySizedBox(
            Widget child,
            float? widthFactor = null,
            float? heightFactor = null,
            Alignment alignment = Alignment.Center) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (widthFactor is null && heightFactor is null) {
                throw new ArgumentException("At least one size factor is required.");
            }
            ValidateFactor(widthFactor, nameof(widthFactor));
            ValidateFactor(heightFactor, nameof(heightFactor));
            if (!Enum.IsDefined(typeof(Alignment), alignment)) throw new ArgumentOutOfRangeException(nameof(alignment));
            WidthFactor = widthFactor;
            HeightFactor = heightFactor;
            Alignment = alignment;
        }

        public Widget Child { get; }
        public float? WidthFactor { get; }
        public float? HeightFactor { get; }
        public Alignment Alignment { get; }

        internal override WidgetNode CreateNode() => new FractionallySizedBoxNode(this);

        private static void ValidateFactor(float? factor, string parameterName) {
            if (factor is { } value && (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)) {
                throw new ArgumentOutOfRangeException(parameterName, "Size factors must be finite and non-negative.");
            }
        }
    }
}
