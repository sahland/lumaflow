using System;
using Assets.Application.Features.Dashboard.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Dashboard.Widgets {
    public sealed class MetricCard : StatelessWidget {
        private readonly DashboardMetric _metric;

        public MetricCard(DashboardMetric metric) {
            _metric = metric ?? throw new ArgumentNullException(nameof(metric));
        }

        public override Widget Build(BuildContext context) {
            return new AppCard(
                child: new Column(
                    gap: AppSpacing.Xs,
                    children: new Widget[] {
                        new Row(
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children: new Widget[] {
                                new Expanded(new Text(
                                    _metric.Title,
                                    style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption),
                                    softWrap: false,
                                    overflow: TextOverflow.Ellipsis
                                )),
                                new Card(
                                    child: new Icon(_metric.Icon, size: AppSizes.IconSm, color: _metric.IconColor),
                                    padding: EdgeInsets.All(AppSpacing.Xs),
                                    backgroundColor: _metric.IconSurfaceColor,
                                    borderRadius: BorderRadius.All(AppRadius.Pill)
                                )
                            }
                        ),
                        new Text(
                            _metric.Value,
                            style: new TextStyle(
                                color: AppColors.TextPrimary,
                                fontSize: AppTypography.Heading2,
                                fontStyle: FontStyle.Bold
                            )
                        ),
                        new Row(
                            gap: AppSpacing.Xxs,
                            children: new Widget[] {
                                new Text("↗", style: new TextStyle(color: AppColors.Success, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)),
                                new Text(_metric.Change, style: new TextStyle(color: AppColors.Success, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold))
                            }
                        ),
                        new Text(_metric.Subtitle, style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption))
                    }
                ),
                padding: EdgeInsets.All(AppSpacing.Md),
                borderRadius: BorderRadius.All(AppRadius.Lg),
                border: Border.All(AppColors.Border)
            );
        }
    }
}
