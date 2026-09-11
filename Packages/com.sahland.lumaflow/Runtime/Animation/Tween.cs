#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {
    /// <summary>Interpolates values of one type between immutable begin and end values.</summary>
    public abstract class Tween<T> {
        protected Tween(T begin, T end) {
            Begin = begin;
            End = end;
        }

        public T Begin { get; }
        public T End { get; }

        public T Transform(float progress) => LerpRange(Begin, End, ValidateProgress(progress));

        protected abstract T Lerp(T begin, T end, float progress);

        internal T LerpRange(T begin, T end, float progress) => Lerp(begin, end, progress);

        private static float ValidateProgress(float progress) {
            if (float.IsNaN(progress) || float.IsInfinity(progress) || progress < 0f || progress > 1f) {
                throw new ArgumentOutOfRangeException(nameof(progress), "Tween progress must be finite and between zero and one.");
            }
            return progress;
        }
    }

    public sealed class FloatTween : Tween<float> {
        public FloatTween(float begin, float end) : base(Validate(begin, nameof(begin)), Validate(end, nameof(end))) { }

        protected override float Lerp(float begin, float end, float progress) =>
            begin + (end - begin) * progress;

        private static float Validate(float value, string parameterName) =>
            float.IsNaN(value) || float.IsInfinity(value)
                ? throw new ArgumentOutOfRangeException(parameterName, "Float tween values must be finite.")
                : value;
    }

    public sealed class ColorTween : Tween<Color> {
        public ColorTween(Color begin, Color end) : base(begin, end) { }

        protected override Color Lerp(Color begin, Color end, float progress) =>
            Color.LerpUnclamped(begin, end, progress);
    }

    public sealed class EdgeInsetsTween : Tween<EdgeInsets> {
        public EdgeInsetsTween(EdgeInsets begin, EdgeInsets end) : base(begin, end) { }

        protected override EdgeInsets Lerp(EdgeInsets begin, EdgeInsets end, float progress) => new(
            LerpFloat(begin.Left, end.Left, progress),
            LerpFloat(begin.Top, end.Top, progress),
            LerpFloat(begin.Right, end.Right, progress),
            LerpFloat(begin.Bottom, end.Bottom, progress));

        private static float LerpFloat(float begin, float end, float progress) => begin + (end - begin) * progress;
    }

    public sealed class BorderRadiusTween : Tween<BorderRadius> {
        public BorderRadiusTween(BorderRadius begin, BorderRadius end) : base(begin, end) { }

        protected override BorderRadius Lerp(BorderRadius begin, BorderRadius end, float progress) => new(
            LerpFloat(begin.TopLeft, end.TopLeft, progress),
            LerpFloat(begin.TopRight, end.TopRight, progress),
            LerpFloat(begin.BottomRight, end.BottomRight, progress),
            LerpFloat(begin.BottomLeft, end.BottomLeft, progress));

        private static float LerpFloat(float begin, float end, float progress) => begin + (end - begin) * progress;
    }
}
