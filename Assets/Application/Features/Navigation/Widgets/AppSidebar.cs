using Assets.Application.Features.Navigation.Models;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using System;
using UnityEngine;

namespace Assets.Application.Features.Navigation.Widgets {
    public sealed class AppSidebar : StatelessWidget {
        private readonly State<AppSection> _selectedSection;

        public AppSidebar(State<AppSection> selectedSection) {
            _selectedSection = selectedSection
                ?? throw new ArgumentNullException(nameof(selectedSection));
        }

        public override Widget Build(BuildContext context) {
            return SizedBox.Expand(
                new Padding(
                    padding: EdgeInsets.All(AppSpacing.Md),
                    child: SizedBox.Expand(
                        new Column(
                            gap: AppSpacing.Xs,
                            children: new Widget[] {
                                BuildBrand(),
                                new ReactiveBuilder<AppSection>(
                                    _selectedSection,
                                    BuildNavigation
                                ),
                                new Spacer(),
                                new ReactiveBuilder<AppSection>(
                                    _selectedSection,
                                    BuildSettings
                                ),
                                new CurrentProjectCard(),
                                new SidebarProfile()
                            }
                        )
                    )
                )
            );
        }

        private static Widget BuildBrand() {
            return new Padding(
                padding: EdgeInsets.Symmetric(
                    horizontal: AppSpacing.Xs,
                    vertical: AppSpacing.Md
                ),
                child: new Row(
                    new Widget[] {
                        new Icon(
                            LumaIcons.Package,
                            size: 28f,
                            color: AppColors.Primary
                        ),
                        new Column(
                            new Widget[] {
                                new Text(
                                    "LumaFlow",
                                    style: new TextStyle(
                                        color: AppColors.TextPrimary,
                                        fontSize: AppTypography.Heading3,
                                        fontStyle: FontStyle.Bold
                                    )
                                ),
                                new Text(
                                    "UI Test App",
                                    style: new TextStyle(
                                        color: AppColors.TextMuted,
                                        fontSize: AppTypography.Caption
                                    )
                                )
                            },
                            gap: AppSpacing.Xxs
                        )
                    },
                    gap: AppSpacing.Sm,
                    crossAxisAlignment: CrossAxisAlignment.Center
                )
            );
        }

        private Widget BuildNavigation(AppSection selected) {
            return new Column(
                gap: AppSpacing.Xs,
                    children: new Widget[] {
                        BuildItem(
                            AppSection.Dashboard,
                            "Dashboard",
                            LumaIcons.Home,
                            selected
                        ),
                        BuildItem(
                            AppSection.Projects,
                            "Projects",
                            LumaIcons.Folder,
                            selected
                        ),
                        BuildItem(
                            AppSection.Map,
                            "Map",
                            LumaIcons.Globe,
                            selected
                        ),
                        BuildItem(
                            AppSection.Calendar,
                            "Calendar",
                            LumaIcons.Calendar,
                            selected
                        ),
                        BuildItem(
                            AppSection.Messages,
                            "Messages",
                            LumaIcons.Mail,
                            selected
                        ),
                        BuildItem(
                            AppSection.Tasks,
                            "Tasks",
                            LumaIcons.List,
                            selected
                        ),
                        BuildItem(
                            AppSection.Team,
                            "Team",
                            LumaIcons.Users,
                            selected
                        )
                    }
            );
        }

        private Widget BuildSettings(AppSection selected) {
            return BuildItem(
                AppSection.Settings,
                "Settings",
                LumaIcons.Settings,
                selected
            );
        }

        private Widget BuildItem(
            AppSection section,
            string label,
            IconData icon,
            AppSection selected
        ) {
            return new SidebarItem(
                label: label,
                icon: icon,
                selected: section == selected,
                onPressed: () => {
                    _selectedSection.Value = section;
                },
                badge: section == AppSection.Messages ? "12" : null
            );
        }
    }
}
