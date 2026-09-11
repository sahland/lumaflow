using System;

namespace Assets.Application.Features.Tasks.Models {
    public enum ProjectTaskStatus { Todo, InProgress, Done }

    /// <summary>Immutable task data; the Tasks screen owns status changes for the dogfood experience.</summary>
    public sealed class ProjectTask {
        public ProjectTask(string id, string title, string project, string assignee, string dueLabel, ProjectTaskStatus status) {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Task id is required.", nameof(id)) : id;
            Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Task title is required.", nameof(title)) : title;
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Assignee = assignee ?? throw new ArgumentNullException(nameof(assignee));
            DueLabel = dueLabel ?? throw new ArgumentNullException(nameof(dueLabel));
            Status = status;
        }
        public string Id { get; }
        public string Title { get; }
        public string Project { get; }
        public string Assignee { get; }
        public string DueLabel { get; }
        public ProjectTaskStatus Status { get; }
        public ProjectTask WithStatus(ProjectTaskStatus status) => new(Id, Title, Project, Assignee, DueLabel, status);
    }
}
