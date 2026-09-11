using Assets.Application.Features.Tasks.Models;

namespace Assets.Application.Features.Tasks.Data {
    public static class DemoTasks {
        public static ProjectTask[] All { get; } = {
            new("task-1", "Review lobby material options", "Greenfield Tower", "Emma Williams", "Today", ProjectTaskStatus.InProgress),
            new("task-2", "Approve structural report", "Metro Station", "Noah Johnson", "Today", ProjectTaskStatus.Todo),
            new("task-3", "Prepare client presentation", "Riverside Apartments", "Olivia Martinez", "Tomorrow", ProjectTaskStatus.InProgress),
            new("task-4", "Update construction schedule", "Sunset Plaza", "Liam Chen", "Thu, 24 Oct", ProjectTaskStatus.Todo),
            new("task-5", "Confirm landscape budget", "Oakwood Villas", "William Brown", "Fri, 25 Oct", ProjectTaskStatus.Done)
        };
    }
}
