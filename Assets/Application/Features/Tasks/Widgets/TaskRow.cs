using System;
using Assets.Application.Features.Tasks.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Tasks.Widgets {
    public sealed class TaskRow : StatelessWidget {
        private readonly ProjectTask _task;
        private readonly Action _onPressed;
        public TaskRow(ProjectTask task, Action onPressed) { _task = task ?? throw new ArgumentNullException(nameof(task)); _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed)); }
        public override Widget Build(BuildContext context) => new Button(
            SizedBox.ExpandWidth(new AppCard(new Row(gap: AppSpacing.Md, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
                StatusMark(),
                new Expanded(new Align(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                    Align.CenterLeft(new Text(_task.Title, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Body, fontStyle: FontStyle.Bold), softWrap: false, overflow: TextOverflow.Ellipsis)),
                    Align.CenterLeft(new Text($"{_task.Project}  ·  {_task.Assignee}", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption), softWrap: false, overflow: TextOverflow.Ellipsis))
                }), Alignment.CenterLeft)),
                new SizedBox(new Align(new Text(_task.DueLabel, style: new TextStyle(color: _task.DueLabel == "Today" ? AppColors.Warning : AppColors.TextSecondary, fontSize: AppTypography.Caption), softWrap: false), Alignment.CenterRight), width: 96f),
                new SizedBox(new Align(StatusBadge(), Alignment.CenterRight), width: 104f)
            }), padding: EdgeInsets.All(AppSpacing.Md), backgroundColor: AppColors.Surface, borderRadius: BorderRadius.All(AppRadius.Md), border: Border.All(AppColors.Border))),
            _onPressed, buttonStyle: new ButtonStyle(background: AppColors.Transparent, padding: EdgeInsets.All(0f), shape: BorderRadius.All(AppRadius.Md)), semanticsLabel: $"Change status for {_task.Title}");

        private Widget StatusMark() => new CircleAvatar(
            child: new Icon(_task.Status == ProjectTaskStatus.Done ? LumaIcons.Check : LumaIcons.Clock, size: AppSizes.IconSm, color: _task.Status == ProjectTaskStatus.Done ? AppColors.White : AppColors.Primary),
            radius: 14f,
            backgroundColor: _task.Status == ProjectTaskStatus.Done ? AppColors.Success : AppColors.PrimarySoft);

        private Widget StatusBadge() {
            var (text, color, background) = _task.Status switch {
                ProjectTaskStatus.Done => ("Done", AppColors.Success, AppColors.SuccessSoft),
                ProjectTaskStatus.InProgress => ("In Progress", AppColors.Success, AppColors.SuccessSoft),
                _ => ("To do", AppColors.TextSecondary, AppColors.Surface)
            };
            return new SizedBox(new Card(
                child: new Center(new Text(text, style: new TextStyle(color: color, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold), softWrap: false, overflow: TextOverflow.Ellipsis)),
                padding: EdgeInsets.All(0f),
                backgroundColor: background,
                borderRadius: BorderRadius.All(AppRadius.Sm),
                border: Border.All(_task.Status == ProjectTaskStatus.InProgress ? AppColors.Success : _task.Status == ProjectTaskStatus.Done ? AppColors.Success : AppColors.BorderStrong)), width: 96f, height: 28f);
        }
    }
}
