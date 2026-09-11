using System;
using System.Collections.Generic;
using Assets.Application.Features.Team.Models;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Team.View {
    public sealed class TeamPage : StatefulWidget<TeamPageState> { }

    public sealed class TeamPageState : WidgetState {
        private static readonly TeamMember[] Members = {
            new("olivia", "Olivia Martinez", "Project Manager", "OM", "olivia@lumaflow.app", AppColors.Success, 8),
            new("liam", "Liam Chen", "Lead Architect", "LC", "liam@lumaflow.app", AppColors.Success, 6),
            new("noah", "Noah Johnson", "Structural Engineer", "NJ", "noah@lumaflow.app", AppColors.Warning, 4),
            new("emma", "Emma Williams", "Product Designer", "EW", "emma@lumaflow.app", AppColors.Success, 5),
            new("william", "William Brown", "Cost Consultant", "WB", "william@lumaflow.app", AppColors.TextMuted, 2)
        };
        private readonly State<string> _search = new(string.Empty);
        private string _selectedId = "olivia";

        public override Widget Build(BuildContext context) => new Column(gap: AppSpacing.Xl, children: new Widget[] {
            BuildHeader(),
            new Row(gap: AppSpacing.Xl, crossAxisAlignment: CrossAxisAlignment.Start, children: new Widget[] {
                new Expanded(BuildMembers()),
                new SizedBox(BuildProfile(Selected), width: 300f)
            })
        });

        private Widget BuildHeader() => new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text("Team", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading2, fontStyle: FontStyle.Bold)),
                new Text("People collaborating across your projects.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))
            })),
            new Button(new Row(gap: AppSpacing.Xs, children: new Widget[] { new Icon(LumaIcons.Plus, size: AppSizes.IconSm, color: AppColors.White), new Text("Invite member", style: new TextStyle(color: AppColors.White, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)) }), () => { }, semanticsLabel: "Invite team member")
        });

        private Widget BuildMembers() => new AppCard(new Column(gap: AppSpacing.Md, children: new Widget[] {
            new TextField(_search, placeholder: "Search team...", style: new TextFieldStyle(background: AppColors.SurfaceSecondary, border: AppColors.Border, padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: 6f), shape: BorderRadius.All(AppRadius.Sm))),
            new ReactiveBuilder<string>(_search, BuildMemberRows)
        }), padding: EdgeInsets.All(AppSpacing.Md), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));

        private Widget BuildMemberRows(string query) {
            var rows = new List<Widget>();
            foreach (var member in Members) if (Matches(member, query)) rows.Add(MemberRow(member).WithKey(new WidgetKey(member.Id)));
            return new Column(gap: AppSpacing.Xs, children: rows);
        }

        private Widget MemberRow(TeamMember member) => new Button(SizedBox.ExpandWidth(new AppCard(new Row(gap: AppSpacing.Md, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new CircleAvatar(child: new Text(member.Initials, style: new TextStyle(color: AppColors.White, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)), radius: AppSizes.AvatarMd * .5f, backgroundColor: member.Id == _selectedId ? AppColors.Primary : AppColors.TextSecondary),
            new Expanded(new Align(new Column(gap: AppSpacing.Xxs, children: new Widget[] { Align.CenterLeft(new Text(member.Name, style: new TextStyle(color: member.Id == _selectedId ? AppColors.Primary : AppColors.TextPrimary, fontSize: AppTypography.Body, fontStyle: FontStyle.Bold))), Align.CenterLeft(new Text(member.Role, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption))) }), Alignment.CenterLeft)),
            new CircleAvatar(radius: 4f, backgroundColor: member.Presence)
        }), padding: EdgeInsets.All(AppSpacing.Sm), backgroundColor: member.Id == _selectedId ? AppColors.PrimarySoft : AppColors.Surface, borderRadius: BorderRadius.All(AppRadius.Md), border: Border.All(member.Id == _selectedId ? AppColors.Primary : AppColors.Border))), () => SetState(() => _selectedId = member.Id), buttonStyle: new ButtonStyle(background: AppColors.Transparent, padding: EdgeInsets.All(0f), shape: BorderRadius.All(AppRadius.Md)), semanticsLabel: $"View {member.Name}");

        private static Widget BuildProfile(TeamMember member) => new AppCard(new Column(gap: AppSpacing.Md, children: new Widget[] {
            new Center(new CircleAvatar(child: new Text(member.Initials, style: new TextStyle(color: AppColors.White, fontSize: AppTypography.Heading3, fontStyle: FontStyle.Bold)), radius: AppSizes.AvatarLg * .5f, backgroundColor: AppColors.Primary)),
            new Center(new Text(member.Name, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading3, fontStyle: FontStyle.Bold))),
            new Center(new Text(member.Role, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))),
            new Card(new Column(gap: AppSpacing.Xs, children: new Widget[] { new Text("Contact", style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption)), new Text(member.Email, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall)), new Text($"{member.ActiveTasks} active tasks", style: new TextStyle(color: AppColors.Primary, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)) }), padding: EdgeInsets.All(AppSpacing.Md), backgroundColor: AppColors.SurfaceSecondary, borderRadius: BorderRadius.All(AppRadius.Sm)),
            new Button("Message", () => { }, buttonStyle: new ButtonStyle(background: AppColors.Primary, foreground: AppColors.White, padding: EdgeInsets.All(AppSpacing.Sm), shape: BorderRadius.All(AppRadius.Sm)))
        }), padding: EdgeInsets.All(AppSpacing.Lg), borderRadius: BorderRadius.All(AppRadius.Lg), border: Border.All(AppColors.Border));

        private TeamMember Selected { get { foreach (var member in Members) if (member.Id == _selectedId) return member; throw new InvalidOperationException("Selected member does not exist."); } }
        private static bool Matches(TeamMember member, string query) => string.IsNullOrWhiteSpace(query) || member.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || member.Role.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
