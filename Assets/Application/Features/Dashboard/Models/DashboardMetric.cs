using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Dashboard.Models {
    public sealed class DashboardMetric {
        public string Title { get; }
        public string Value { get; }
        public string Change { get; }
        public string Subtitle { get; }
        public IconData Icon { get; }
        public Color IconColor { get; }
        public Color IconSurfaceColor { get; }

        public DashboardMetric(
            string title,
            string value,
            string change,
            string subtitle,
            IconData icon,
            Color iconColor,
            Color iconSurfaceColor) {
            Title = title;
            Value = value;
            Change = change;
            Subtitle = subtitle;
            Icon = icon;
            IconColor = iconColor;
            IconSurfaceColor = iconSurfaceColor;
        }
    }
}
