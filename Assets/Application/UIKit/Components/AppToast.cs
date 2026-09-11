#nullable enable

using System;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    /// <summary>Fixed-width application toast content for <see cref="OverlayController.ShowToast"/>.</summary>
    public sealed class AppToast : StatelessWidget {
        private readonly string _title;
        private readonly string? _message;
        private readonly IconData _icon;
        private readonly Color _iconColor;

        public AppToast(string title, string? message = null, IconData? icon = null, Color? iconColor = null) {
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _message = message;
            _icon = icon ?? LumaIcons.Success;
            _iconColor = iconColor ?? AppColors.Success;
        }

        public override Widget Build(BuildContext context) {
            var text = _message is null
                ? (Widget)new Text(_title, style: TitleStyle())
                : new Column(
                    gap: AppSpacing.Xxs,
                    children: new Widget[] {
                        new Text(_title, style: TitleStyle()),
                        new Text(_message, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption))
                    });

            return new SizedBox(
                new Card(
                    new Row(
                        gap: AppSpacing.Sm,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        children: new Widget[] {
                            new Icon(_icon, size: AppSizes.IconMd, color: _iconColor),
                            new Expanded(text)
                        }),
                    padding: EdgeInsets.All(AppSpacing.Md),
                    backgroundColor: AppColors.Surface,
                    borderRadius: BorderRadius.All(AppRadius.Md),
                    border: Border.All(AppColors.Border)),
                width: 280f);
        }

        private static TextStyle TitleStyle() => new(
            color: AppColors.TextPrimary,
            fontSize: AppTypography.BodySmall,
            fontStyle: FontStyle.Bold);
    }
}
