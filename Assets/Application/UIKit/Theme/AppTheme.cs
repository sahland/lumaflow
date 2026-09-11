using Assets.Application.UIKit.Tokens;
using LumaFlow;

namespace Assets.Application.UIKit.Theme {
    public static class AppTheme {
        public static ThemeData Data { get; } = Create();

        private static ThemeData Create() {
            return new ThemeData(
                colors: CreateColors(),
                typography: CreateTypography(),
                spacing: CreateSpacing(),
                radius: CreateRadius(),
                buttonTheme: AppButtonTheme.Create(),
                iconTheme: CreateIconTheme(),
                textFieldTheme: AppInputTheme.CreateTextFieldTheme(),
                dropdownTheme: AppInputTheme.CreateDropdownTheme()
            );
        }

        private static ColorScheme CreateColors() {
            return new ColorScheme(
                canvas: AppColors.Background,
                surface: AppColors.Surface,
                surfaceVariant: AppColors.SurfaceSecondary,
                primary: AppColors.Primary,
                primaryContainer: AppColors.PrimarySoft,
                onPrimary: AppColors.TextOnPrimary,
                onSurface: AppColors.TextPrimary,
                onSurfaceVariant: AppColors.TextSecondary,
                outline: AppColors.Border
            );
        }

        private static TypographyTheme CreateTypography() {
            return new TypographyTheme(
                title: new TextStyle(
                    color: AppColors.TextPrimary,
                    fontSize: AppTypography.Heading1,
                    fontStyle: UnityEngine.FontStyle.Bold
                ),
                headline: new TextStyle(
                    color: AppColors.TextPrimary,
                    fontSize: AppTypography.Heading2,
                    fontStyle: UnityEngine.FontStyle.Bold
                ),
                body: new TextStyle(
                    color: AppColors.TextPrimary,
                    fontSize: AppTypography.Body
                ),
                label: new TextStyle(
                    color: AppColors.TextSecondary,
                    fontSize: AppTypography.BodySmall
                )
            );
        }

        private static SpacingTheme CreateSpacing() {
            return new SpacingTheme(
                extraSmall: AppSpacing.Xxs,
                small: AppSpacing.Xs,
                medium: AppSpacing.Md,
                large: AppSpacing.Xl,
                extraLarge: AppSpacing.Xxl
            );
        }

        private static RadiusTheme CreateRadius() {
            return new RadiusTheme(
                small: BorderRadius.All(AppRadius.Sm),
                medium: BorderRadius.All(AppRadius.Md),
                large: BorderRadius.All(AppRadius.Lg)
            );
        }

        private static IconThemeData CreateIconTheme() {
            return new IconThemeData(
                size: AppSizes.IconMd,
                color: AppColors.TextSecondary,
                disabledColor: AppColors.TextMuted
            );
        }
    }
}
