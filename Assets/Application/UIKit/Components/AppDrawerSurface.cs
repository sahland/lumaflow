#nullable enable

using System;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    /// <summary>Full-height fixed-width drawer surface for <see cref="OverlayController.ShowDrawer"/>.</summary>
    public sealed class AppDrawerSurface : StatelessWidget {
        private readonly Widget _content;
        private readonly string _title;
        private readonly Action _onClose;

        public AppDrawerSurface(string title, Widget content, Action onClose) {
            _title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
        }

        public override Widget Build(BuildContext context) {
            return new SizedBox(
                SizedBox.ExpandHeight(
                    new Card(
                        SizedBox.ExpandHeight(
                            new Column(
                                gap: AppSpacing.Md,
                                children: new Widget[] {
                                    new Row(new Widget[] {
                                        new Expanded(new Text(_title, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold))),
                                        new IconButton(LumaIcons.Close, _onClose, tooltip: "Close", hitSize: AppSizes.IconButtonMd)
                                    }, crossAxisAlignment: CrossAxisAlignment.Center),
                                    _content
                                })),
                        padding: EdgeInsets.All(AppSpacing.Lg),
                        backgroundColor: AppColors.Surface,
                        borderRadius: BorderRadius.All(0f),
                        border: Border.All(AppColors.Border))),
                width: 360f);
        }
    }
}
