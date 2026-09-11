using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    public sealed class AppLinearProgress : StatelessWidget {
        private readonly float _value;
        private readonly float _width;
        private readonly float _height;

        public AppLinearProgress(
            float value,
            float width = 150f,
            float height = 4f
        ) {
            _value = Mathf.Clamp01(value);
            _width = width;
            _height = height;
        }
        public override Widget Build(BuildContext context) {
            var filledWidth = _width * _value;
            var remainingWidth = _width - filledWidth;

            return new Row(
                new Widget[] {
                    BuildSegment(
                        filledWidth,
                        AppColors.Primary
                    ),

                    BuildSegment(
                        remainingWidth,
                        AppColors.Border
                    )
                },
                gap: 0f,
                crossAxisAlignment: CrossAxisAlignment.Center
            );
        }

        private Widget BuildSegment(
            float width,
            Color color
        ) {
            return new SizedBox(
                new Card(
                    child: new Spacer(),
                    padding: EdgeInsets.All(0f),
                    backgroundColor: color,
                    borderRadius: BorderRadius.All(
                        _height * 0.5f
                    )
                ),
                width: width,
                height: _height
            );
        }
    }
}
