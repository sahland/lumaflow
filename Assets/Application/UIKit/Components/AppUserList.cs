#nullable enable

using System;
using System.Collections.Generic;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    public sealed class AppUserListItem {
        public AppUserListItem(string name, string role, string initials, Color statusColor, Image? avatar = null) {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("User name cannot be empty.", nameof(name)) : name;
            Role = string.IsNullOrWhiteSpace(role) ? throw new ArgumentException("User role cannot be empty.", nameof(role)) : role;
            Initials = string.IsNullOrWhiteSpace(initials) ? throw new ArgumentException("Initials cannot be empty.", nameof(initials)) : initials;
            StatusColor = statusColor;
            Avatar = avatar;
        }

        public string Name { get; }
        public string Role { get; }
        public string Initials { get; }
        public Color StatusColor { get; }
        public Image? Avatar { get; }
    }

    /// <summary>Reusable people list with avatar, secondary label, and presence indicator.</summary>
    public sealed class AppUserList : StatelessWidget {
        private readonly IReadOnlyList<AppUserListItem> _items;

        public AppUserList(IReadOnlyList<AppUserListItem> items) {
            if (items is null || items.Count == 0) throw new ArgumentException("User list requires at least one item.", nameof(items));
            var copy = new AppUserListItem[items.Count];
            for (var index = 0; index < items.Count; index++) {
                copy[index] = items[index] ?? throw new ArgumentException("User list cannot contain null.", nameof(items));
            }
            _items = Array.AsReadOnly(copy);
        }

        public override Widget Build(BuildContext context) {
            var rows = new Widget[_items.Count];
            for (var index = 0; index < _items.Count; index++) rows[index] = BuildRow(_items[index]);
            return new AppCard(
                new Column(gap: AppSpacing.Sm, children: rows),
                padding: EdgeInsets.All(AppSpacing.Md),
                borderRadius: BorderRadius.All(AppRadius.Md),
                border: Border.All(AppColors.Border));
        }

        private static Widget BuildRow(AppUserListItem item) {
            var avatar = new CircleAvatar(
                child: new Text(item.Initials, style: new TextStyle(color: AppColors.White, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)),
                backgroundImage: item.Avatar,
                radius: AppSizes.AvatarSm * 0.5f,
                backgroundColor: AppColors.Primary,
                semanticsLabel: item.Name);

            return new Row(
                gap: AppSpacing.Sm,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: new Widget[] {
                    avatar,
                    new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                        new Text(item.Name, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)),
                        new Text(item.Role, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption))
                    })),
                    new CircleAvatar(radius: 3f, backgroundColor: item.StatusColor)
                });
        }
    }
}
