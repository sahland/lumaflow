using Assets.Application.UIKit.Tokens;
using LumaFlow;
using System;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    public enum AppBadgeTone {
        Success,
        Primary,
        Neutral
    }

    public sealed class AppBadge : StatelessWidget {
        private readonly string _text;
        private readonly AppBadgeTone _tone;

        public AppBadge(
            string text,
            AppBadgeTone tone
        ) {
            _text = text
                ?? throw new ArgumentNullException(nameof(text));
            _tone = tone;
        }

        public override Widget Build(BuildContext context) {
            return new Card(
                child: new Text(
                    _text,
                    style: new TextStyle(
                        color: GetForeground(),
                        fontSize: AppTypography.Caption,
                        fontStyle: FontStyle.Bold
                    )
                ),
                padding: EdgeInsets.Symmetric(
                    horizontal: AppSpacing.Xs,
                    vertical: 4f
                ),
                backgroundColor: GetBackground(),
                borderRadius: BorderRadius.All(AppRadius.Xs),
                border: Border.All(GetBorder())
            );
        }

        private Color GetForeground() {
            return _tone switch {
                AppBadgeTone.Success => AppColors.Success,
                AppBadgeTone.Primary => AppColors.Primary,
                AppBadgeTone.Neutral => AppColors.TextSecondary,
                _ => AppColors.TextSecondary
            };
        }

        private Color GetBackground() {
            return _tone switch {
                AppBadgeTone.Success => AppColors.SuccessSoft,
                AppBadgeTone.Primary => AppColors.PrimarySoft,
                AppBadgeTone.Neutral => AppColors.SurfaceSecondary,
                _ => AppColors.SurfaceSecondary
            };
        }

        private Color GetBorder() {
            return _tone switch {
                AppBadgeTone.Success => new Color32(187, 247, 208, 255),
                AppBadgeTone.Primary => new Color32(191, 219, 254, 255),
                _ => AppColors.BorderStrong
            };
        }
    }
}
