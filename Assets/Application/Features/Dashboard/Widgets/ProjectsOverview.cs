using System;
using System.Collections.Generic;
using Assets.Application.Demo;
using Assets.Application.Features.Dashboard.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Dashboard.Widgets {
    public sealed class ProjectsOverview : StatefulWidget<ProjectsOverviewState> {
        internal OverlayController Overlays { get; }

        public ProjectsOverview(OverlayController overlays) {
            Overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
        }
    }

    public sealed class ProjectsOverviewState : WidgetState {
        private static readonly ButtonStyle ViewAllStyle = new(
            background: AppColors.Transparent,
            foreground: AppColors.Primary,
            padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Xs, vertical: AppSpacing.Xs),
            shape: BorderRadius.All(AppRadius.Sm),
            typography: new TextStyle(fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(0f, AppSizes.ControlHeightSm),
            hovered: new ButtonStateStyle(background: AppColors.PrimarySoft));

        private readonly List<ProjectSummary> _projects = new();
        private int _copyCount;

        private ProjectsOverview Configuration => (ProjectsOverview)Widget;

        protected override void InitState() => _projects.AddRange(DemoProjects.Dashboard);

        public override Widget Build(BuildContext context) {
            return new AppCard(
                child: new Column(
                    gap: AppSpacing.Sm,
                    children: new Widget[] { BuildHeader(), BuildProjects() }),
                padding: EdgeInsets.All(AppSpacing.Md),
                borderRadius: BorderRadius.All(AppRadius.Lg),
                border: Border.All(AppColors.Border));
        }

        private Widget BuildHeader() {
            return new Row(children: new Widget[] {
                new Expanded(new Text(
                    "Projects Overview",
                    style: new TextStyle(
                        color: AppColors.TextPrimary,
                        fontSize: AppTypography.BodyLarge,
                        fontStyle: FontStyle.Bold))),
                new Button("View all", ShowProjectsToast, buttonStyle: ViewAllStyle)
            });
        }

        private Widget BuildProjects() {
            var rows = new Widget[_projects.Count];
            for (var index = 0; index < _projects.Count; index++) {
                var project = _projects[index];
                rows[index] = new ProjectRow(project, Configuration.Overlays, DuplicateProject, DeleteProject)
                    .WithKey(new WidgetKey(project.Name));
            }
            return new Column(gap: AppSpacing.Xs, children: rows);
        }

        private void DuplicateProject(ProjectSummary source) {
            SetState(() => {
                _copyCount++;
                _projects.Add(new ProjectSummary(
                    name: $"{source.Name} Copy {_copyCount}",
                    description: source.Description,
                    mediaId: source.MediaId,
                    progress: source.Progress,
                    status: source.Status));
            });
        }

        private void DeleteProject(ProjectSummary project) => SetState(() => _projects.Remove(project));

        private void ShowProjectsToast() {
            Configuration.Overlays.ShowToast(
                new Toast(new AppToast("Projects list", "Projects list is coming next")),
                TimeSpan.FromSeconds(3));
        }
    }
}
