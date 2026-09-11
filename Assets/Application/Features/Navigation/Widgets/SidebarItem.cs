using Assets.Application.UIKit.Tokens;
using LumaFlow;
using System;
using UnityEngine;

#nullable enable

namespace Assets.Application.Features.Navigation.Widgets {
    public sealed class SidebarItem : StatelessWidget {
        private readonly string _label;
        private readonly IconData _icon;
        private readonly bool _selected;
        private readonly Action _onPressed;
        private readonly string? _badge;

        public SidebarItem(
            string label,
            IconData icon,
            bool selected,
            Action onPressed,
            string? badge = null) {
            _label = label
                ?? throw new ArgumentNullException(nameof(label));

            _selected = selected;
            _icon = icon;

            _onPressed = onPressed
                ?? throw new ArgumentNullException(nameof(onPressed));
            _badge = badge;
        }

        public override Widget Build(BuildContext context) {
            var foreground = _selected ? AppColors.Primary : AppColors.TextSecondary;
            return new Button(
                child: SizedBox.ExpandWidth(
                    new Align(
                        new Row(
                            gap: AppSpacing.Sm,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children: new Widget[] {
                                new Icon(_icon, size: AppSizes.IconSm, color: foreground),
                                new Expanded(new Text(
                                    _label,
                                    style: new TextStyle(
                                        color: foreground,
                                        fontSize: AppTypography.BodySmall,
                                        fontStyle: _selected ? FontStyle.Bold : FontStyle.Normal
                                    ),
                                    softWrap: false,
                                    overflow: TextOverflow.Ellipsis
                                )),
                                BuildBadge()
                            }
                        ),
                        Alignment.CenterLeft
                    )
                ),
                onPressed: _onPressed,
                buttonStyle: _selected ? Styles.Selected : Styles.Normal,
                semanticsLabel: _label);
        }

        private Widget BuildBadge() {
            if (string.IsNullOrWhiteSpace(_badge)) return new SizedBox(new Spacer(), width: 0f);
            return new Card(
                child: new Text(
                    _badge,
                    style: new TextStyle(color: AppColors.Primary, fontSize: 11f, fontStyle: FontStyle.Bold)
                ),
                padding: EdgeInsets.Symmetric(horizontal: 6f, vertical: 2f),
                backgroundColor: AppColors.PrimarySoft,
                borderRadius: BorderRadius.All(AppRadius.Pill)
            );
        }

        private static class Styles {
            public static ButtonStyle Normal { get; } =
                new(
                    background: AppColors.Transparent,
                    foreground: AppColors.TextSecondary,
                    padding: EdgeInsets.Symmetric(
                        horizontal: AppSpacing.Md,
                        vertical: AppSpacing.Xs
                    ),
                    shape: BorderRadius.All(AppRadius.Md),
                    typography: new TextStyle(fontSize: AppTypography.BodySmall),
                    minimumSize: new Vector2(
                        0f,
                        AppSizes.NavigationItemHeight
                    ),
                    hovered: new ButtonStateStyle(
                        background: AppColors.SurfaceHover,
                        foreground: AppColors.TextPrimary
                    )
                );

            public static ButtonStyle Selected { get; } =
                new(
                    background: AppColors.PrimarySoft,
                    foreground: AppColors.Primary,
                    padding: EdgeInsets.Symmetric(
                        horizontal: AppSpacing.Md,
                        vertical: AppSpacing.Xs
                    ),
                    shape: BorderRadius.All(AppRadius.Md),
                    typography: new TextStyle(
                        fontSize: AppTypography.BodySmall,
                        fontStyle: FontStyle.Bold
                    ),
                    minimumSize: new Vector2(
                        0f,
                        AppSizes.NavigationItemHeight
                    )
                );
        }
    }
}
