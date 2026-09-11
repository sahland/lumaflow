#nullable enable

using System;

namespace LumaFlow {
    /// <summary>Maps linear animation progress to eased progress.</summary>
    public abstract class Curve {
        public float Transform(float progress) {
            if (float.IsNaN(progress) || float.IsInfinity(progress) || progress < 0f || progress > 1f) {
                throw new ArgumentOutOfRangeException(nameof(progress), "Curve progress must be finite and between zero and one.");
            }
            if (progress is 0f or 1f) return progress;
            var transformed = TransformInternal(progress);
            if (float.IsNaN(transformed) || float.IsInfinity(transformed)) {
                throw new InvalidOperationException("A Curve must return a finite value.");
            }
            return transformed;
        }

        protected abstract float TransformInternal(float progress);
    }

    /// <summary>A cubic Bézier easing curve with monotonic time control points.</summary>
    public sealed class Cubic : Curve {
        public Cubic(float x1, float y1, float x2, float y2) {
            ValidateFinite(x1, nameof(x1));
            ValidateFinite(y1, nameof(y1));
            ValidateFinite(x2, nameof(x2));
            ValidateFinite(y2, nameof(y2));
            if (x1 < 0f || x1 > 1f) throw new ArgumentOutOfRangeException(nameof(x1), "Cubic time control points must be between zero and one.");
            if (x2 < 0f || x2 > 1f) throw new ArgumentOutOfRangeException(nameof(x2), "Cubic time control points must be between zero and one.");
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public float X1 { get; }
        public float Y1 { get; }
        public float X2 { get; }
        public float Y2 { get; }

        protected override float TransformInternal(float progress) {
            var lower = 0f;
            var upper = 1f;
            var parameter = progress;
            for (var iteration = 0; iteration < 16; iteration++) {
                parameter = (lower + upper) * 0.5f;
                var x = Sample(parameter, X1, X2);
                if (x < progress) lower = parameter;
                else upper = parameter;
            }
            return Sample(parameter, Y1, Y2);
        }

        private static float Sample(float t, float first, float second) {
            var inverse = 1f - t;
            return 3f * inverse * inverse * t * first
                + 3f * inverse * t * t * second
                + t * t * t;
        }

        private static void ValidateFinite(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                throw new ArgumentOutOfRangeException(parameterName, "Cubic control points must be finite.");
            }
        }
    }

    /// <summary>Common immutable easing curves.</summary>
    public static class Curves {
        public static Curve Linear { get; } = new LinearCurve();
        public static Curve Ease { get; } = new Cubic(0.25f, 0.1f, 0.25f, 1f);
        public static Curve EaseIn { get; } = new Cubic(0.42f, 0f, 1f, 1f);
        public static Curve EaseOut { get; } = new Cubic(0f, 0f, 0.58f, 1f);
        public static Curve EaseInOut { get; } = new Cubic(0.42f, 0f, 0.58f, 1f);

        private sealed class LinearCurve : Curve {
            protected override float TransformInternal(float progress) => progress;
        }
    }
}
