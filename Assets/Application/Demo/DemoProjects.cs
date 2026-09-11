using Assets.Application.Features.Dashboard.Models;

namespace Assets.Application.Demo {
    public static class DemoProjects {
        public static ProjectSummary[] Dashboard { get; } = {
            new(
                name: "Greenfield Tower",
                description: "Mixed-use development",
                mediaId: "greenfield_tower",
                progress: 78,
                status: "In Progress"
            ),
            new(
                name: "Riverside Apartments",
                description: "Residential complex",
                mediaId: "riverside_apartments",
                progress: 45,
                status: "In Progress"
            ),
            new(
                name: "Sunset Plaza",
                description: "Commercial complex",
                mediaId: "sunset_plaza",
                progress: 90,
                status: "Review"
            ),
            new(
                name: "Metro Station",
                description: "Infrastructure",
                mediaId: "metro_station",
                progress: 32,
                status: "In Progress"
            ),
            new(
                name: "Oakwood Villas",
                description: "Luxury villas",
                mediaId: "oakwood_villas",
                progress: 12,
                status: "Planning"
            )
        };
    }
}
