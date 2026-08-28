#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {
    internal readonly struct AnimationSample<T> {
        public AnimationSample(T value, bool completed) {
            Value = value;
            Completed = completed;
        }

        public T Value { get; }
        public bool Completed { get; }
    }

    /// <summary>Deterministic, scheduler-independent state machine for an implicit tween.</summary>
    internal sealed class ImplicitAnimation<T> {
        private Tween<T>? _tween;
        private AnimationSpec _spec;
        private T _from = default!;
        private T _target = default!;
        private T _current = default!;
        private double _startedAt;

        public T Current => _current;
        public T Target => _target;
        public bool IsRunning { get; private set; }
        internal double StartedAt => _startedAt;

        public bool Start(
            T from,
            T target,
            Tween<T> tween,
            AnimationSpec spec,
            double nowSeconds,
            bool disableAnimations) {
            if (tween is null) throw new ArgumentNullException(nameof(tween));
            ValidateTime(nowSeconds);
            _from = from;
            _target = target;
            _current = from;
            _tween = tween;
            _spec = spec;
            _startedAt = nowSeconds;
            if (EqualityComparer<T>.Default.Equals(from, target)
                || (disableAnimations && spec.Behavior == AnimationBehavior.Normal)) {
                _current = target;
                IsRunning = false;
                return true;
            }
            IsRunning = true;
            return false;
        }

        public AnimationSample<T> Sample(double nowSeconds) {
            ValidateTime(nowSeconds);
            if (!IsRunning) return new AnimationSample<T>(_current, completed: false);
            var elapsed = Math.Max(0d, nowSeconds - _startedAt);
            var linear = Math.Min(1d, elapsed / _spec.Duration.TotalSeconds);
            var progress = _spec.Curve.Transform((float)linear);
            _current = _tween!.LerpRange(_from, _target, progress);
            if (linear < 1d) return new AnimationSample<T>(_current, completed: false);
            _current = _target;
            IsRunning = false;
            return new AnimationSample<T>(_current, completed: true);
        }

        public bool SnapToEnd() {
            if (!IsRunning) return false;
            _current = _target;
            IsRunning = false;
            return true;
        }

        public void Cancel() => IsRunning = false;

        private static void ValidateTime(double time) {
            if (double.IsNaN(time) || double.IsInfinity(time)) {
                throw new ArgumentOutOfRangeException(nameof(time), "Animation time must be finite.");
            }
        }
    }
}
