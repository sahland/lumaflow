using Assets.Application.Features.Dashboard.Widgets;
using Assets.Application.UIKit.Tokens;
using LumaFlow;

#nullable enable

namespace Assets.Application.Features.Dashboard.View {
    public class DashboardPage : StatelessWidget {
        private readonly OverlayController _overlays;

        public DashboardPage(OverlayController overlays) {
            _overlays = overlays;
        }

        public override Widget Build(BuildContext context) {
            return new Column(
                gap: AppSpacing.Xl,
                children: new Widget[] {
                    new DashboardHeader(_overlays),
                    new MetricsGrid(),
                    new ProjectsOverview(_overlays)
                }
            );
        }
    }
}
