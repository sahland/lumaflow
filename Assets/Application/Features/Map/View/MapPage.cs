using Assets.Application.Demo;
using Assets.Application.Features.Dashboard.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;

namespace Assets.Application.Features.Map.View {
    /// <summary>Project location overview. The map surface is intentionally SDK-agnostic.</summary>
    public sealed class MapPage : StatelessWidget {
        public override Widget Build(BuildContext context) {
            return SizedBox.ExpandHeight(new Column(gap: AppSpacing.Xl, children: new Widget[] {
                new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                    new Text("Project Map", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: UnityEngine.FontStyle.Bold)),
                    new Text("Explore active project locations and their current progress.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))
                }),
                new Expanded(new Row(gap: AppSpacing.Xl, crossAxisAlignment: CrossAxisAlignment.Start, children: new Widget[] {
                    new Expanded(BuildMap()),
                    new SizedBox(BuildLocations(), width: 300f, height: 470f)
                }))
            }));
        }

        private static Widget BuildMap() => new AppCard(
            new SizedBox(
                new Stack(new Widget[] {
                    Marker(DemoProjects.Dashboard[0], 90f, 82f),
                    Marker(DemoProjects.Dashboard[1], 310f, 148f),
                    Marker(DemoProjects.Dashboard[2], 470f, 92f),
                    Marker(DemoProjects.Dashboard[3], 210f, 292f),
                    Marker(DemoProjects.Dashboard[4], 530f, 326f)
                }),
                height: 470f),
            padding: EdgeInsets.All(AppSpacing.Md),
            backgroundColor: AppColors.PrimarySoft,
            borderRadius: BorderRadius.All(AppRadius.Lg),
            border: Border.All(AppColors.Border));

        private static Widget Marker(ProjectSummary project, float left, float top) => new Positioned(
            new Card(
                new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                    new Text(project.Name, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Caption, fontStyle: UnityEngine.FontStyle.Bold)),
                    new Text($"{project.Progress}% · {project.Status}", style: new TextStyle(color: AppColors.TextSecondary, fontSize: 10f))
                }),
                padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: AppSpacing.Xs),
                backgroundColor: AppColors.Surface,
                borderRadius: BorderRadius.All(AppRadius.Sm),
                border: Border.All(AppColors.Border)),
            left: left,
            top: top);

        private static Widget BuildLocations() {
            var rows = new Widget[DemoProjects.Dashboard.Length];
            for (var index = 0; index < rows.Length; index++) {
                var project = DemoProjects.Dashboard[index];
                rows[index] = new AppCard(
                    new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                        new Text(project.Name, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall, fontStyle: UnityEngine.FontStyle.Bold)),
                        new Text(project.Description, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)),
                        new AppProgressRow("Progress", project.Progress / 100f)
                    }),
                    padding: EdgeInsets.All(AppSpacing.Sm),
                    backgroundColor: AppColors.SurfaceSecondary,
                    borderRadius: BorderRadius.All(AppRadius.Sm));
            }
            return new AppCard(new Column(gap: AppSpacing.Md, children: new Widget[] {
                new Text("Locations", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: UnityEngine.FontStyle.Bold)),
                new SizedBox(new ScrollView(new Column(gap: AppSpacing.Sm, children: rows)), height: 390f)
            }), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));
        }
    }
}
