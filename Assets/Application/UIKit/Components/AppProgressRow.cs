using System;
using Assets.Application.UIKit.Theme;
using Assets.Application.UIKit.Tokens;
using LumaFlow;

namespace Assets.Application.UIKit.Components {
    /// <summary>Labelled linear progress composition used by dashboard rows and settings surfaces.</summary>
    public sealed class AppProgressRow : StatelessWidget {
        private readonly string _label;
        private readonly float _value;
        private readonly bool _showPercentage;

        public AppProgressRow(string label, float value, bool showPercentage = true) {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Progress label cannot be empty.", nameof(label));
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f) {
                throw new ArgumentOutOfRangeException(nameof(value), "Progress value must be between zero and one.");
            }
            _label = label;
            _value = value;
            _showPercentage = showPercentage;
        }

        public override Widget Build(BuildContext context) {
            var percentage = $"{Math.Round(_value * 100f):0}%";
            return new Column(gap: AppSpacing.Xs, children: new Widget[] {
                new Text(_label, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)),
                new Row(gap: AppSpacing.Sm, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
                    new Expanded(new LinearProgressIndicator(
                        _value,
                        new LinearProgressIndicatorStyle(
                            valueColor: AppColors.Primary,
                            trackColor: AppColors.Border,
                            minHeight: 4f,
                            borderRadius: BorderRadius.All(AppRadius.Pill)),
                        semanticsLabel: _label)),
                    _showPercentage
                        ? new SizedBox(new Text(percentage, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)), width: 34f)
                        : new SizedBox(new Text(string.Empty), width: 0f)
                })
            });
        }
    }

    /// <summary>Styled controlled slider with an optional trailing numeric value.</summary>
    public sealed class AppSliderRow : StatelessWidget {
        private readonly string _label;
        private readonly State<float> _value;
        private readonly float _min;
        private readonly float _max;

        public AppSliderRow(string label, State<float> value, float min, float max) {
            _label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Slider label cannot be empty.", nameof(label)) : label;
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _min = min;
            _max = max;
        }

        public override Widget Build(BuildContext context) => new Column(gap: AppSpacing.Xs, children: new Widget[] {
            new Text(_label, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)),
            new Slider(_value, _min, _max, style: AppControlStyles.Slider)
        });
    }
}
