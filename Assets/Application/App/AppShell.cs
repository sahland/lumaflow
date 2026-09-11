using Assets.Application.Features.Dashboard.View;
using Assets.Application.Features.Navigation.Models;
using Assets.Application.Features.Calendar.View;
using Assets.Application.Features.Map.View;
using Assets.Application.Features.Messages.View;
using Assets.Application.Features.Projects.View;
using Assets.Application.Features.Tasks.View;
using Assets.Application.Features.Team.View;
using Assets.Application.Features.Navigation.Widgets;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using System;

#nullable enable

namespace Assets.Application.App {
    public sealed class AppShell : StatelessWidget {
        private readonly State<AppSection> _selectedSection;
        private readonly OverlayController _overlays;

        public AppShell(State<AppSection> selectedSection, OverlayController overlays) {
            _selectedSection = selectedSection 
                ?? throw new ArgumentNullException(nameof(selectedSection));
            _overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
        }

        public override Widget Build(BuildContext context) {
            return new Scaffold(
                body: new Row(
                    children: new Widget[] {
                        new SizedBox(
                            width: AppSizes.SidebarWidth,
                            child: SizedBox.Expand(
                                new Padding(
                                    padding: EdgeInsets.All(AppSpacing.Xs),
                                    child: SizedBox.Expand(
                                        new Card(
                                            child: SizedBox.Expand(new AppSidebar(_selectedSection)),
                                            padding: EdgeInsets.All(0f),
                                            backgroundColor: AppColors.Surface,
                                            borderRadius: BorderRadius.All(AppRadius.Lg),
                                            border: Border.All(AppColors.Border)
                                        )
                                    )
                                )
                            )
                        ),
                        new Expanded(BuildContent())
                    }
                )
            );
        }

        private Widget BuildContent() {
            return new ReactiveBuilder<AppSection>(
                _selectedSection,
                selected => selected == AppSection.Map || selected == AppSection.Messages
                    ? SizedBox.Expand(new Padding(
                        padding: EdgeInsets.All(AppSpacing.Page),
                        child: BuildPage(selected)))
                    : new ScrollView(
                        child: new Padding(
                            padding: EdgeInsets.All(AppSpacing.Page),
                            child: BuildPage(selected)))
            );
        }

        private Widget BuildPage(AppSection section) {
            return section switch {
                AppSection.Dashboard => new DashboardPage(_overlays),
                AppSection.Projects => new ProjectsPage(_overlays),
                AppSection.Map => new MapPage(),
                AppSection.Calendar => new CalendarPage(),
                AppSection.Messages => new MessagesPage(),
                AppSection.Tasks => new TasksPage(),
                AppSection.Team => new TeamPage(),
                AppSection.Settings => new Text("Settings"),
                _ => throw new ArgumentOutOfRangeException(nameof(section))
            };
        }
    }
}
