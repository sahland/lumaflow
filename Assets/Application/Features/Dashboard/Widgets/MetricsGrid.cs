using Assets.Application.Features.Dashboard.Models;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Dashboard.Widgets {
    public sealed class MetricsGrid : StatelessWidget {
        private static readonly DashboardMetric[] Metrics = {
            new("Total Projects", "24", "+12%", "vs last month", LumaIcons.Package, AppColors.Primary, AppColors.PrimarySoft),
            new("Active Users", "1,248", "+8%", "vs last month", LumaIcons.Users, new Color32(124, 58, 237, 255), new Color32(245, 243, 255, 255)),
            new("Tasks Completed", "86%", "+8%", "vs last month", LumaIcons.Success, AppColors.Success, AppColors.SuccessSoft),
            new("Revenue", "$120,430", "+15%", "vs last month", LumaIcons.Archive, new Color32(217, 119, 6, 255), AppColors.WarningSoft)
        };

        public override Widget Build(BuildContext context) {
            return new LayoutBuilder((_, constraints) => {
                if (constraints.MaxWidth >= AppBreakpoints.DashboardWide) return BuildRow(0, 4);
                if (constraints.MaxWidth >= AppBreakpoints.DashboardCompact) {
                    return new Column(
                        gap: AppSpacing.Md,
                        children: new Widget[] { BuildRow(0, 2), BuildRow(2, 2) }
                    );
                }

                return new Column(
                    gap: AppSpacing.Md,
                    children: new Widget[] {
                        new MetricCard(Metrics[0]), new MetricCard(Metrics[1]),
                        new MetricCard(Metrics[2]), new MetricCard(Metrics[3])
                    }
                );
            });
        }

        private static Widget BuildRow(int start, int count) {
            var children = new Widget[count];
            for (var index = 0; index < count; index++) children[index] = new Expanded(new MetricCard(Metrics[start + index]));
            return new Row(children, gap: AppSpacing.Md);
        }
    }
}
