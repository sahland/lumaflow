using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Theme {
    public static class AppControlStyles {
        public static CheckboxStyle Checkbox { get; } =
            new CheckboxStyle(
                fillColor: AppColors.Primary,
                inactiveColor: AppColors.Surface,
                checkColor: AppColors.White,
                borderColor: AppColors.BorderStrong,
                size: 20f,
                shape: BorderRadius.All(AppRadius.Xs),
                fillColorByState:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;
                            if ((states & WidgetStates.Selected) != 0) return AppColors.Primary;
                            if ((states & WidgetStates.Hovered) != 0) return AppColors.PrimarySoft;

                            return AppColors.Surface;
                        }
                    ),
                borderColorByState:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;
                            if ((states & WidgetStates.Selected) != 0) return AppColors.Primary;
                            if ((states & WidgetStates.Focused) != 0) return AppColors.Primary;

                            return AppColors.BorderStrong;
                        }
                    )
            );

        public static RadioStyle Radio { get; } =
            new RadioStyle(
                fillColor: AppColors.Primary,
                inactiveColor: AppColors.Surface,
                borderColor: AppColors.BorderStrong,
                size: 20f,
                fillColorByState:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;
                            if ((states & WidgetStates.Selected) != 0) return AppColors.Primary;

                            return AppColors.Surface;
                        }
                    ),
                borderColorByState:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;
                            if ((states & WidgetStates.Focused) != 0) return AppColors.Primary;

                            return AppColors.BorderStrong;
                        }
                    )
            );

        public static SwitchStyle Switch { get; } =
            new SwitchStyle(
                trackColor:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;
                            if ((states & WidgetStates.Selected) != 0) return AppColors.Primary;

                            return AppColors.BorderStrong;
                        }
                    ),
                thumbColor:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.TextMuted;

                            return AppColors.White;
                        }
                    ),
                width: WidgetStateProperty<float?>.All(40f),
                height: WidgetStateProperty<float?>.All(22f)
            );

        public static SliderStyle Slider { get; } =
            new SliderStyle(
                activeTrackColor:
                    WidgetStateProperty<Color?>.ResolveWith(
                        states => {
                            if ((states & WidgetStates.Disabled) != 0) return AppColors.Border;

                            return AppColors.Primary;
                        }
                    ),
                trackHeight: WidgetStateProperty<float?>.All(4f),
                thumbSize: WidgetStateProperty<float?>.All(16f)
            );

        public static SegmentedControlStyle SegmentedControl { get; } =
            new SegmentedControlStyle(
                background: AppColors.SurfaceSecondary,
                selectedBackground: AppColors.Primary,
                selectedForeground: AppColors.White,
                foreground: AppColors.TextSecondary,
                shape: BorderRadius.All(AppRadius.Sm));

        public static TabBarStyle Tabs { get; } =
            new TabBarStyle(
                selectedForeground: AppColors.Primary,
                foreground: AppColors.TextSecondary,
                indicatorColor: AppColors.Primary,
                dividerColor: AppColors.Border);
    }
}
