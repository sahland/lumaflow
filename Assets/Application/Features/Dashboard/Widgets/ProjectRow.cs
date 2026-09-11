#nullable enable

using System;
using Assets.Application.Features.Dashboard.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Media;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;
using VisualElement = UnityEngine.UIElements.VisualElement;

namespace Assets.Application.Features.Dashboard.Widgets {
    public sealed class ProjectRow : StatefulWidget<ProjectRowState> {
        internal ProjectSummary Project { get; }
        internal OverlayController Overlays { get; }
        internal Action<ProjectSummary>? OnDuplicate { get; }
        internal Action<ProjectSummary>? OnDelete { get; }

        public ProjectRow(
            ProjectSummary project,
            OverlayController overlays,
            Action<ProjectSummary>? onDuplicate = null,
            Action<ProjectSummary>? onDelete = null) {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
            OnDuplicate = onDuplicate;
            OnDelete = onDelete;
        }
    }

    public sealed class ProjectRowState : WidgetState {
            private OverlayHandle? _menu;

            private ProjectRow Configuration => (ProjectRow)Widget;
            private ProjectSummary Project => Configuration.Project;
            private OverlayController Overlays => Configuration.Overlays;

            public override Widget Build(BuildContext context) {
            return new LayoutBuilder((_, constraints) =>
                constraints.MaxWidth < AppBreakpoints.ProjectRowCompact ? BuildCompact() : BuildWide());
            }

        private Widget BuildWide() {
            return new Padding(
                padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Xs, vertical: AppSpacing.Sm),
                child: new Row(
                    gap: AppSpacing.Md,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children: new Widget[] {
                        new SizedBox(BuildProjectInfo(), width: AppSizes.ProjectInfoWidth),
                        new Expanded(new Center(
                            child: new SizedBox(BuildProgress(), width: AppSizes.ProjectProgressBlockWidth)
                        )),
                        new SizedBox(BuildRightArea(), width: AppSizes.ProjectRightAreaWidth)
                    }
                )
            );
        }

        private Widget BuildCompact() {
            return new Padding(
                padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Xs, vertical: AppSpacing.Sm),
                child: new Column(
                    gap: AppSpacing.Sm,
                    children: new Widget[] {
                        BuildProjectInfo(),
                        BuildProgress(),
                        new Row(
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children: new Widget[] {
                                BuildStatus(), new Spacer(), BuildActions()
                            }
                        )
                    }
                )
            );
        }

        private Widget BuildProjectInfo() {
            return new Row(
                gap: AppSpacing.Sm,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: new Widget[] {
                    new SizedBox(
                        AppMedia.ProjectThumbnail(Project.MediaId, AppSizes.ProjectThumbnail, Project.Name),
                        width: AppSizes.ProjectThumbnail,
                        height: AppSizes.ProjectThumbnail
                    ),
                    new Expanded(new Column(
                        gap: AppSpacing.Xxs,
                        children: new Widget[] {
                            new Text(
                                Project.Name,
                                style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold),
                                softWrap: false,
                                overflow: TextOverflow.Ellipsis
                            ),
                            new Text(
                                Project.Description,
                                style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption),
                                softWrap: false,
                                overflow: TextOverflow.Ellipsis
                            )
                        }
                    ))
                }
            );
        }

        private Widget BuildProgress() {
            return new Row(
                gap: AppSpacing.Sm,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: new Widget[] {
                    new SizedBox(
                        new LinearProgressIndicator(
                            Project.Progress / 100f,
                            new LinearProgressIndicatorStyle(
                                valueColor: AppColors.Primary,
                                trackColor: AppColors.Border,
                                minHeight: 4f,
                                borderRadius: BorderRadius.All(AppRadius.Pill)
                            ),
                            semanticsLabel: $"{Project.Name} progress"
                        ),
                        width: AppSizes.ProjectProgressBarWidth,
                        height: 4f
                    ),
                    new SizedBox(
                        new Text($"{Project.Progress}%", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)),
                        width: 34f
                    )
                }
            );
        }

        private Widget BuildRightArea() {
            return new Row(
                gap: AppSpacing.Sm,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: new Widget[] {
                    new SizedBox(BuildStatus(), width: AppSizes.ProjectStatusWidth),
                    new Spacer(),
                    BuildActions()
                }
            );
        }

        private Widget BuildActions() => new IconButton(
            LumaIcons.MoreVertical,
            ShowProjectMenu,
            tooltip: $"Actions for {Project.Name}",
            hitSize: AppSizes.IconButtonSm);

        private void ShowProjectMenu(VisualElement anchor) {
            if (_menu?.IsOpen == true) {
                _menu.Close();
                _menu = null;
                return;
            }

            _menu = Overlays.ShowPopover(
                anchor,
                new AppMenuSurface(new[] {
                    new AppMenuAction("Edit project", () => { CloseMenu(); ShowProjectDrawer(); }),
                    new AppMenuAction("Duplicate", DuplicateProject),
                    new AppMenuAction("Delete", () => { CloseMenu(); ShowDeleteConfirmation(); }, destructive: true)
                }),
                PopoverPlacement.BottomEnd);
        }

        private void CloseMenu() {
            _menu?.Close();
            _menu = null;
        }

        private void DuplicateProject() {
            CloseMenu();
            ShowToast(
                "Project duplicated",
                $"{Project.Name} was added as a copy.",
                LumaIcons.Copy,
                AppColors.Primary);
            Configuration.OnDuplicate?.Invoke(Project);
        }

        private void ShowDeleteConfirmation() {
            Overlays.ShowConfirm(
                new Text($"Delete '{Project.Name}'? This action cannot be undone.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall)),
                new Text("Delete project", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold)),
                confirmText: "Delete",
                cancelText: "Cancel",
                confirmVariant: ButtonVariant.Destructive,
                onConfirm: _ => {
                    Debug.Log($"Deleted project: {Project.Name}");
                    ShowToast(
                        "Project deleted",
                        $"{Project.Name} was deleted.",
                        LumaIcons.Delete,
                        AppColors.Danger);
                    Configuration.OnDelete?.Invoke(Project);
                    return System.Threading.Tasks.Task.CompletedTask;
                });
        }

        private void ShowToast(string title, string message, IconData icon, Color iconColor) {
            Overlays.ShowToast(
                new Toast(new AppToast(title, message, icon, iconColor)),
                TimeSpan.FromSeconds(3));
        }

        private void ShowProjectDrawer() {
            OverlayHandle? drawer = null;
            drawer = Overlays.ShowDrawer(
                new AppDrawerSurface(
                    "Project details",
                    new Column(
                        gap: AppSpacing.Sm,
                        children: new Widget[] {
                            new Text(Project.Name, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading3, fontStyle: FontStyle.Bold)),
                            new Text(Project.Description, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall)),
                            new LinearProgressIndicator(Project.Progress / 100f, new LinearProgressIndicatorStyle(valueColor: AppColors.Primary, trackColor: AppColors.Border, minHeight: 4f)),
                            new Text($"{Project.Progress}% complete", style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption))
                        }),
                    () => drawer?.Close()),
                DrawerPlacement.Right);
        }

        private Widget BuildStatus() => new AppBadge(Project.Status, GetStatusTone());

        private AppBadgeTone GetStatusTone() {
            return Project.Status switch {
                "In Progress" => AppBadgeTone.Success,
                "Review" => AppBadgeTone.Primary,
                _ => AppBadgeTone.Neutral
            };
        }

            protected override void Dispose() => CloseMenu();
    }
}
