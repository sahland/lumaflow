using System;
using System.Collections.Generic;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Calendar.View {
    public sealed class CalendarPage : StatefulWidget<CalendarPageState> { }

    public sealed class CalendarPageState : WidgetState {
        private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _selectedDay = DateTime.Today;

        public override Widget Build(BuildContext context) => new Column(gap: AppSpacing.Xl, children: new Widget[] {
            BuildHeader(),
            new Row(gap: AppSpacing.Xl, crossAxisAlignment: CrossAxisAlignment.Start, children: new Widget[] {
                new Expanded(BuildCalendar()),
                new SizedBox(BuildAgenda(), width: 300f)
            })
        });

        private Widget BuildHeader() => new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text("Calendar", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: FontStyle.Bold)),
                new Text("Keep track of project milestones and upcoming work.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))
            })),
            new IconButton(LumaIcons.ChevronLeft, () => ChangeMonth(-1), tooltip: "Previous month"),
            new Text(_month.ToString("MMMM yyyy"), style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold)),
            new IconButton(LumaIcons.ChevronRight, () => ChangeMonth(1), tooltip: "Next month")
        });

        private Widget BuildCalendar() {
            var children = new List<Widget>();
            foreach (var day in new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }) {
                children.Add(new Expanded(new Center(new Text(day, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)))));
            }
            var rows = new List<Widget> { new Row(children: children) };
            var start = _month.AddDays(-(((int)_month.DayOfWeek + 6) % 7));
            for (var week = 0; week < 6; week++) {
                var days = new Widget[7];
                for (var day = 0; day < 7; day++) days[day] = BuildDay(start.AddDays(week * 7 + day));
                rows.Add(new Row(gap: AppSpacing.Xs, children: days));
            }
            return new AppCard(new Column(gap: AppSpacing.Xs, children: rows), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));
        }

        private Widget BuildDay(DateTime day) {
            var selected = day.Date == _selectedDay.Date;
            var inMonth = day.Month == _month.Month;
            var style = new ButtonStyle(
                background: selected ? AppColors.Primary : AppColors.Transparent,
                foreground: selected ? AppColors.White : inMonth ? AppColors.TextPrimary : AppColors.TextMuted,
                padding: EdgeInsets.All(AppSpacing.Sm),
                shape: BorderRadius.All(AppRadius.Sm),
                minimumSize: new Vector2(0f, 44f));
            return new Expanded(new Button(day.Day.ToString(), () => SetState(() => _selectedDay = day), buttonStyle: style));
        }

        private Widget BuildAgenda() => new AppCard(new Column(gap: AppSpacing.Md, children: new Widget[] {
            new Text("Upcoming", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold)),
            Event("09:30", "Greenfield design review", "Greenfield Tower"),
            Event("13:00", "Team planning", "Riverside Apartments"),
            Event("16:30", "Milestone check-in", "Sunset Plaza")
        }), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));

        private static Widget Event(string time, string title, string project) => new AppCard(new Row(gap: AppSpacing.Sm, children: new Widget[] {
            new SizedBox(
                new Text(time, style: new TextStyle(color: AppColors.Primary, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold), softWrap: false),
                width: 46f),
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text(title, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)),
                new Text(project, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption))
            }))
        }), padding: EdgeInsets.All(AppSpacing.Sm), backgroundColor: AppColors.SurfaceSecondary, borderRadius: BorderRadius.All(AppRadius.Sm));

        private void ChangeMonth(int delta) => SetState(() => _month = _month.AddMonths(delta));
    }
}
