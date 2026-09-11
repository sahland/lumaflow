using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Theme {
    public static class AppInputTheme {
        public static TextFieldTheme CreateTextFieldTheme() {
            return new TextFieldTheme(
                new TextFieldStyle(
                    background: AppColors.Surface,
                    foreground: AppColors.TextPrimary,
                    border: AppColors.BorderStrong,
                    placeholder: AppColors.TextMuted,
                    typography: new TextStyle(
                        fontSize: AppTypography.Body
                    ),
                    labelStyle: new TextStyle(
                        color: AppColors.TextSecondary,
                        fontSize: AppTypography.BodySmall
                    ),
                    supportingStyle: new TextStyle(
                        color: AppColors.TextMuted,
                        fontSize: AppTypography.Caption
                    ),
                    padding: EdgeInsets.Symmetric(
                        horizontal: AppSpacing.Md,
                        vertical: AppSpacing.Xs
                    ),
                    shape: BorderRadius.All(AppRadius.Md),
                    focused: new TextFieldStateStyle(
                        background: AppColors.Surface,
                        foreground: AppColors.TextPrimary,
                        border: AppColors.Primary
                    ),
                    disabled: new TextFieldStateStyle(
                        background: AppColors.SurfaceSecondary,
                        foreground: AppColors.TextMuted,
                        border: AppColors.Border
                    ),
                    error: new TextFieldStateStyle(
                        background: AppColors.DangerSoft,
                        foreground: AppColors.TextPrimary,
                        border: AppColors.Danger
                    ),
                    borderColor: WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0)
                                return AppColors.Border;

                            if ((states & WidgetStates.Error) != 0)
                                return AppColors.Danger;

                            if ((states & WidgetStates.Focused) != 0)
                                return AppColors.Primary;

                            if ((states & WidgetStates.Hovered) != 0)
                                return AppColors.BorderStrong;

                            return AppColors.Border;
                        }
                    )
                )
            );
        }

        public static DropdownTheme CreateDropdownTheme() {
            return new DropdownTheme(
                new DropdownStyle(
                    background: AppColors.Surface,
                    foreground: AppColors.TextPrimary,
                    border: AppColors.Border,
                    padding: EdgeInsets.Symmetric(
                        horizontal: AppSpacing.Md,
                        vertical: AppSpacing.Xs
                    ),
                    shape: BorderRadius.All(AppRadius.Md),
                    typography: new TextStyle(
                        fontSize: AppTypography.Body
                    ),
                    backgroundColor:
                        WidgetStateProperty<Color?>.ResolveWith(
                            states => {
                                if ((states & WidgetStates.Disabled) != 0)
                                    return AppColors.SurfaceSecondary;

                                if ((states & WidgetStates.Pressed) != 0)
                                    return AppColors.SurfaceHover;

                                return AppColors.Surface;
                            }
                        ),
                    borderColor:
                        WidgetStateProperty<Color?>.ResolveWith(
                            states => {
                                if ((states & WidgetStates.Disabled) != 0)
                                    return AppColors.Border;

                                if ((states & WidgetStates.Error) != 0)
                                    return AppColors.Danger;

                                if ((states & WidgetStates.Focused) != 0)
                                    return AppColors.Primary;

                                return AppColors.Border;
                            }
                        )
                )
            );
        }
    }
}
