using System;
using System.Collections.Generic;
using Assets.Application.Demo;
using Assets.Application.Features.Dashboard.Models;
using Assets.Application.Features.Dashboard.Widgets;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;

namespace Assets.Application.Features.Projects.View {
    public sealed class ProjectsPage : StatefulWidget<ProjectsPageState> {
        internal OverlayController Overlays { get; }
        public ProjectsPage(OverlayController overlays) => Overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
    }

    public sealed class ProjectsPageState : WidgetState {
        private readonly State<string> _search = new(string.Empty);
        private readonly List<ProjectSummary> _projects = new();
        private int _nextProjectNumber = 1;
        private ProjectsPage Configuration => (ProjectsPage)Widget;

        protected override void InitState() => _projects.AddRange(DemoProjects.Dashboard);

        public override Widget Build(BuildContext context) => new Column(gap: AppSpacing.Xl, children: new Widget[] {
            BuildHeader(),
            new TextField(_search, placeholder: "Search projects..."),
            new ReactiveBuilder<string>(_search, BuildProjectList)
        });

        private Widget BuildHeader() => new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text("Projects", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: UnityEngine.FontStyle.Bold)),
                new Text("Manage your active projects and their progress.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))
            })),
            new Button("+  New Project", CreateProject)
        });

        private Widget BuildProjectList(string query) {
            var rows = new List<Widget>();
            foreach (var project in _projects) {
                if (!Matches(project, query)) continue;
                rows.Add(new ProjectRow(project, Configuration.Overlays, DuplicateProject, DeleteProject).WithKey(new WidgetKey(project.Name)));
            }
            return new AppCard(
                rows.Count == 0
                    ? new Center(new Text("No projects found.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall)))
                    : new Column(gap: AppSpacing.Xs, children: rows),
                padding: EdgeInsets.All(AppSpacing.Md),
                borderRadius: BorderRadius.All(AppRadius.Lg),
                border: Border.All(AppColors.Border));
        }

        private void CreateProject() {
            SetState(() => _projects.Add(new ProjectSummary($"New Project {_nextProjectNumber++}", "New project workspace", "greenfield_tower", 0, "Planning")));
            Configuration.Overlays.ShowToast(new Toast(new AppToast("Project created", "Your new project is ready.")), TimeSpan.FromSeconds(3));
        }

        private void DuplicateProject(ProjectSummary source) => SetState(() => _projects.Add(new ProjectSummary($"{source.Name} Copy {_nextProjectNumber++}", source.Description, source.MediaId, source.Progress, source.Status)));
        private void DeleteProject(ProjectSummary project) => SetState(() => _projects.Remove(project));
        private static bool Matches(ProjectSummary project, string query) => string.IsNullOrWhiteSpace(query)
            || project.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
            || project.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
