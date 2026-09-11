using Assets.Application.Features.Navigation.Models;
using Assets.Application.UIKit.Theme;
using LumaFlow;

#nullable enable

namespace Assets.Application.App {
    public sealed class AppRoot : StatelessWidget {
        private readonly State<AppSection> _selectedSection = new(AppSection.Dashboard);
        private readonly OverlayController _overlays = new();

        public override Widget Build(BuildContext context) {
            return new Theme(
                AppTheme.Data,
                new OverlayHost(
                    new AppShell(_selectedSection, _overlays),
                    _overlays)
            );
        }
    }
}
