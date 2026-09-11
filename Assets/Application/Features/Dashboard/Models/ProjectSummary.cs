namespace Assets.Application.Features.Dashboard.Models {
    public sealed class ProjectSummary {
        public string Name { get; }
        public string Description { get; }
        public string MediaId { get; }
        public int Progress { get; }
        public string Status { get; }

        public ProjectSummary(
            string name,
            string description,
            string mediaId,
            int progress,
            string status
            ) {
            Name = name;
            Description = description;
            MediaId = mediaId;
            Progress = progress;
            Status = status;
        }
    }
}
