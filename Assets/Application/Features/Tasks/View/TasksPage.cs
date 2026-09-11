using System;
using System.Collections.Generic;
using Assets.Application.Features.Tasks.Data;
using Assets.Application.Features.Tasks.Models;
using Assets.Application.Features.Tasks.Widgets;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Tasks.View {
    public sealed class TasksPage : StatefulWidget<TasksPageState> { }

    public sealed class TasksPageState : WidgetState {
        private readonly List<ProjectTask> _tasks = new();
        private ProjectTaskStatus? _filter;
        private int _nextTask = 1;

        protected override void InitState() => _tasks.AddRange(DemoTasks.All);

        public override Widget Build(BuildContext context) => new Column(gap: AppSpacing.Xl, children: new Widget[] {
            BuildHeader(),
            BuildOverview(),
            BuildTaskList()
        });

        private Widget BuildHeader() => new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text("Tasks", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: FontStyle.Bold)),
                new Text("Plan, assign, and track your project work.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))
            })),
            new Button(new Row(gap: AppSpacing.Xs, children: new Widget[] { new Icon(LumaIcons.Plus, size: AppSizes.IconSm, color: AppColors.White), new Text("New task", style: new TextStyle(color: AppColors.White, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)) }), AddTask, semanticsLabel: "Create task")
        });

        private Widget BuildOverview() => new Row(gap: AppSpacing.Md, children: new Widget[] {
            new Expanded(SummaryCard("To do", Count(ProjectTaskStatus.Todo), AppColors.TextSecondary, AppColors.SurfaceSecondary)),
            new Expanded(SummaryCard("In progress", Count(ProjectTaskStatus.InProgress), AppColors.Primary, AppColors.PrimarySoft)),
            new Expanded(SummaryCard("Completed", Count(ProjectTaskStatus.Done), AppColors.Success, AppColors.SuccessSoft))
        });

        private static Widget SummaryCard(string label, int count, Color color, Color background) => new AppCard(new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text(label, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption)),
                new Text(count.ToString(), style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: FontStyle.Bold))
            })),
            new CircleAvatar(child: new Text(count.ToString(), style: new TextStyle(color: color, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)), radius: 16f, backgroundColor: background)
        }), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Md), border: Border.All(AppColors.Border));

        private Widget BuildTaskList() {
            var rows = new List<Widget>();
            foreach (var task in _tasks) if (_filter is null || task.Status == _filter) rows.Add(new TaskRow(task, () => Advance(task.Id)).WithKey(new WidgetKey(task.Id)));
            return new AppCard(new Column(gap: AppSpacing.Md, children: new Widget[] {
                new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
                    new Expanded(new Text("My tasks", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold))),
                    FilterButton("All", null), FilterButton("To do", ProjectTaskStatus.Todo), FilterButton("In progress", ProjectTaskStatus.InProgress), FilterButton("Done", ProjectTaskStatus.Done)
                }),
                new Column(gap: AppSpacing.Xs, children: rows)
            }), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));
        }

        private Widget FilterButton(string label, ProjectTaskStatus? status) => new Button(label, () => SetState(() => _filter = status), buttonStyle: new ButtonStyle(background: _filter == status ? AppColors.PrimarySoft : AppColors.Transparent, foreground: _filter == status ? AppColors.Primary : AppColors.TextSecondary, padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: AppSpacing.Xs), shape: BorderRadius.All(AppRadius.Pill), typography: new TextStyle(fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)));
        private int Count(ProjectTaskStatus status) { var count = 0; foreach (var task in _tasks) if (task.Status == status) count++; return count; }
        private void Advance(string id) => SetState(() => { for (var i = 0; i < _tasks.Count; i++) if (_tasks[i].Id == id) { var next = _tasks[i].Status == ProjectTaskStatus.Todo ? ProjectTaskStatus.InProgress : _tasks[i].Status == ProjectTaskStatus.InProgress ? ProjectTaskStatus.Done : ProjectTaskStatus.Todo; _tasks[i] = _tasks[i].WithStatus(next); return; } });
        private void AddTask() => SetState(() => _tasks.Insert(0, new ProjectTask($"task-local-{_nextTask}", $"New task {_nextTask++}", "Greenfield Tower", "Alex Johnson", "Today", ProjectTaskStatus.Todo)));
    }
}
