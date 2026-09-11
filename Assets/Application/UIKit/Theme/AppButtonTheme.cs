using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Theme {
    public static class AppButtonTheme {
        public static ButtonTheme Create() {
            return new ButtonTheme(
                primary: CreatePrimary(),
                secondary: CreateSecondary(),
                destructive: CreateDestructive()
            );
        }

        private static ButtonStyle CreatePrimary() {
            return new ButtonStyle(
                background: AppColors.Primary,
                foreground: AppColors.TextOnPrimary,
                padding: EdgeInsets.Symmetric(
                    horizontal: AppSpacing.Md,
                    vertical: AppSpacing.Xs
                ),
                shape: BorderRadius.All(AppRadius.Md),
                typography: new TextStyle(
                    fontSize: AppTypography.Body,
                    fontStyle: FontStyle.Bold
                ),
                minimumSize: new Vector2(
                    AppSizes.TopBarHeight,
                    AppSizes.ControlHeightMd
                ),
                hovered: new ButtonStateStyle(
                    background: AppColors.PrimaryHover
                ),
                pressed: new ButtonStateStyle(
                    background: AppColors.PrimaryHover
                ),
                focused: new ButtonStateStyle(
                    background: AppColors.PrimaryHover
                ),
                disabled: new ButtonStateStyle(
                    background: AppColors.Border,
                    foreground: AppColors.TextMuted
                )
            );
        }

        private static ButtonStyle CreateSecondary() {
            return new ButtonStyle(
                background: AppColors.SurfaceSecondary,
                foreground: AppColors.TextPrimary,
                padding: EdgeInsets.Symmetric(
                    horizontal: AppSpacing.Md,
                    vertical: AppSpacing.Xs
                ),
                shape: BorderRadius.All(AppRadius.Md),
                typography: new TextStyle(
                    fontSize: AppTypography.Body,
                    fontStyle: FontStyle.Bold
                ),
                minimumSize: new Vector2(
                    AppSizes.TopBarHeight,
                    AppSizes.ControlHeightMd
                ),
                hovered: new ButtonStateStyle(
                    background: AppColors.SurfaceHover
                ),
                pressed: new ButtonStateStyle(
                    background: AppColors.Border
                ),
                focused: new ButtonStateStyle(
                    background: AppColors.PrimarySoft
                ),
                disabled: new ButtonStateStyle(
                    background: AppColors.SurfaceSecondary,
                    foreground: AppColors.TextMuted
                )
            );
        }

        private static ButtonStyle CreateDestructive() {
            return new ButtonStyle(
                background: AppColors.Danger,
                foreground: AppColors.White,
                padding: EdgeInsets.Symmetric(
                    horizontal: AppSpacing.Md,
                    vertical: AppSpacing.Xs
                ),
                shape: BorderRadius.All(AppRadius.Md),
                typography: new TextStyle(
                    fontSize: AppTypography.Body,
                    fontStyle: FontStyle.Bold
                ),
                minimumSize: new Vector2(
                    AppSizes.TopBarHeight,
                    AppSizes.ControlHeightMd
                ),
                hovered: new ButtonStateStyle(
                    background: new Color32(185, 28, 28, 255)
                ),
                pressed: new ButtonStateStyle(
                    background: new Color32(153, 27, 27, 255)
                ),
                focused: new ButtonStateStyle(
                    background: new Color32(185, 28, 28, 255)
                ),
                disabled: new ButtonStateStyle(
                    background: AppColors.Border,
                    foreground: AppColors.TextMuted
                )
            );
        }
    }
}
