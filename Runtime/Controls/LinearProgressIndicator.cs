#nullable enable

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Visual configuration for a determinate linear progress indicator.</summary>
    public sealed class LinearProgressIndicatorStyle {
        public LinearProgressIndicatorStyle(
            Color? valueColor = null,
            Color? trackColor = null,
            float? minHeight = null,
            BorderRadius? borderRadius = null) {
            if (minHeight is { } height && (!float.IsFinite(height) || height <= 0f)) {
                throw new ArgumentOutOfRangeException(nameof(minHeight), "Indicator height must be finite and greater than zero.");
            }
            ValueColor = valueColor;
            TrackColor = trackColor;
            MinHeight = minHeight;
            BorderRadius = borderRadius;
        }

        public Color? ValueColor { get; }
        public Color? TrackColor { get; }
        public float? MinHeight { get; }
        public BorderRadius? BorderRadius { get; }
    }

    /// <summary>Theme default for linear progress indicators.</summary>
    public sealed class ProgressIndicatorTheme {
        public ProgressIndicatorTheme(LinearProgressIndicatorStyle linearStyle) {
            LinearStyle = linearStyle ?? throw new ArgumentNullException(nameof(linearStyle));
        }

        public LinearProgressIndicatorStyle LinearStyle { get; }
    }

    /// <summary>Displays a determinate value in the inclusive 0..1 range.</summary>
    public sealed class LinearProgressIndicator : Widget {
        private readonly float _value;

        public LinearProgressIndicator(
            float value,
            LinearProgressIndicatorStyle? style = null,
            string? semanticsLabel = null) {
            ValidateValue(value);
            _value = value;
            Style = style;
            SemanticsLabel = semanticsLabel;
        }

        public LinearProgressIndicator(
            State<float> value,
            LinearProgressIndicatorStyle? style = null,
            string? semanticsLabel = null) {
            ValueState = value ?? throw new ArgumentNullException(nameof(value));
            ValidateValue(value.Value);
            _value = value.Value;
            Style = style;
            SemanticsLabel = semanticsLabel;
        }

        public float Value => ValueState?.Value ?? _value;
        public LinearProgressIndicatorStyle? Style { get; }
        public string? SemanticsLabel { get; }
        internal State<float>? ValueState { get; }
        internal float CurrentValue => Value;
        internal override WidgetNode CreateNode() => new LinearProgressIndicatorNode(this);

        internal static void ValidateValue(float value) {
            if (!float.IsFinite(value) || value < 0f || value > 1f) {
                throw new ArgumentOutOfRangeException(nameof(value), "Progress must be finite and within 0..1.");
            }
        }
    }

    internal sealed class LinearProgressIndicatorNode : WidgetNode {
        private VisualElement? _track;
        private VisualElement? _fill;
        private IDisposable? _subscription;

        public LinearProgressIndicatorNode(LinearProgressIndicator widget) : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            _track = new VisualElement { name = "lumaflow-linear-progress-track" };
            _fill = new VisualElement { name = "lumaflow-linear-progress-fill", pickingMode = PickingMode.Ignore };
            _track.Add(_fill);
            _track.style.flexShrink = 0f;
            _track.style.position = Position.Relative;
            _track.style.overflow = Overflow.Hidden;
            ApplyStyleAndValue();
            return _track;
        }

        protected override void OnMounted() => BindValue((LinearProgressIndicator)Widget);

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not LinearProgressIndicator indicator) return false;
            var previous = (LinearProgressIndicator)Widget;
            if (!ReferenceEquals(previous.ValueState, indicator.ValueState)) ReleaseValue();
            UpdateWidget(indicator);
            if (!ReferenceEquals(previous.ValueState, indicator.ValueState)) BindValue(indicator);
            ReevaluateInheritedDependencies(() => { ApplyStyleAndValue(); return true; });
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => ApplyStyleAndValue();

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (LinearProgressIndicator)Widget;
            return new SemanticsProperties(
                label: widget.SemanticsLabel,
                value: widget.CurrentValue.ToString("P0", CultureInfo.InvariantCulture),
                role: SemanticsRole.ProgressIndicator);
        }

        private void BindValue(LinearProgressIndicator widget) {
            if (widget.ValueState is not { } state) return;
            _subscription = state.Subscribe(UpdateValue);
            Bindings.Add(_subscription);
        }

        private void UpdateValue(float value) {
            LinearProgressIndicator.ValidateValue(value);
            ApplyValue(value);
            RefreshSemantics();
        }

        private void ApplyStyleAndValue() {
            var widget = (LinearProgressIndicator)Widget;
            var explicitStyle = widget.Style;
            var themed = Context.Theme?.ProgressIndicatorTheme.LinearStyle;
            var valueColor = explicitStyle?.ValueColor ?? themed?.ValueColor ?? new Color(0.56f, 0.47f, 1f);
            var trackColor = explicitStyle?.TrackColor ?? themed?.TrackColor ?? new Color(0.22f, 0.23f, 0.28f);
            var height = explicitStyle?.MinHeight ?? themed?.MinHeight ?? 4f;
            var radius = ClampRadius(
                explicitStyle?.BorderRadius ?? themed?.BorderRadius ?? BorderRadius.All(height * 0.5f),
                height);

            _track!.style.backgroundColor = trackColor;
            _track.style.height = height;
            _track.style.minHeight = height;
            ApplyRadius(_track, radius);
            _fill!.style.backgroundColor = valueColor;
            // A percentage-sized flex child participates in the row/column solver and
            // can be shrunk into a wedge by an ancestor. The fill is visual chrome,
            // not layout content: pin it to the track instead.
            _fill.style.position = Position.Absolute;
            _fill.style.left = 0f;
            _fill.style.top = 0f;
            _fill.style.bottom = 0f;
            _fill.style.height = StyleKeyword.Null;
            _fill.style.flexShrink = 0f;
            ApplyRadius(_fill, radius);
            ApplyValue(widget.CurrentValue);
        }

        private void ApplyValue(float value) {
            LinearProgressIndicator.ValidateValue(value);
            _fill!.style.width = Length.Percent(value * 100f);
        }

        private void ReleaseValue() {
            _subscription?.Dispose();
            _subscription = null;
        }

        private static void ApplyRadius(VisualElement element, BorderRadius radius) {
            element.style.borderTopLeftRadius = radius.TopLeft;
            element.style.borderTopRightRadius = radius.TopRight;
            element.style.borderBottomRightRadius = radius.BottomRight;
            element.style.borderBottomLeftRadius = radius.BottomLeft;
        }

        private static BorderRadius ClampRadius(BorderRadius radius, float height) {
            var maximum = height * 0.5f;
            return new BorderRadius(
                Mathf.Min(radius.TopLeft, maximum),
                Mathf.Min(radius.TopRight, maximum),
                Mathf.Min(radius.BottomRight, maximum),
                Mathf.Min(radius.BottomLeft, maximum));
        }
    }
}
