using Assets.Application.UIKit.Tokens;
using LumaFlow;
using System;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    public sealed class AppCard : StatelessWidget {
        private readonly Widget _child;
        private readonly EdgeInsets _padding;
        private readonly Color _backgroundColor;
        private readonly BorderRadius _borderRadius;
        private readonly Border? _border;

        public AppCard(
            Widget child,
            EdgeInsets? padding = null,
            Color? backgroundColor = null,
            BorderRadius? borderRadius = null,
            Border? border = null
            ) {
            _child = child
                ?? throw new ArgumentNullException(nameof(child));

            _padding = padding
                ?? EdgeInsets.All(AppSpacing.Card);

            _backgroundColor = backgroundColor
                ?? AppColors.Surface;

            _borderRadius = borderRadius
                ?? BorderRadius.All(AppRadius.Lg);

            _border = border;
        }

        public override Widget Build(BuildContext context) {
            return new Card(
                child: _child,
                padding: _padding,
                backgroundColor: _backgroundColor,
                borderRadius: _borderRadius,
                border: _border
            );
        }
    }
}
