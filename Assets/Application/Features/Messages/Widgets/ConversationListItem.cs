using System;
using Assets.Application.Features.Messages.Models;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Messages.Widgets {
    /// <summary>Selectable row used exclusively by the Messages conversation list.</summary>
    public sealed class ConversationListItem : StatelessWidget {
        private readonly Conversation _conversation;
        private readonly string _preview;
        private readonly bool _selected;
        private readonly Action _onPressed;

        public ConversationListItem(Conversation conversation, string preview, bool selected, Action onPressed) {
            _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            _selected = selected;
            _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        }

        public override Widget Build(BuildContext context) {
            var foreground = _selected ? AppColors.Primary : AppColors.TextPrimary;
            return new Button(
                child: SizedBox.ExpandWidth(new Row(gap: AppSpacing.Sm, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
                    new CircleAvatar(
                        child: new Text(_conversation.Initials, style: new TextStyle(color: AppColors.White, fontSize: AppTypography.Caption, fontStyle: FontStyle.Bold)),
                        radius: AppSizes.AvatarMd * 0.5f,
                        backgroundColor: _selected ? AppColors.Primary : new Color(100f / 255f, 116f / 255f, 139f / 255f),
                        semanticsLabel: _conversation.ParticipantName),
                    new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                        new Row(children: new Widget[] {
                            new Expanded(new Text(_conversation.ParticipantName, style: new TextStyle(color: foreground, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold), softWrap: false, overflow: TextOverflow.Ellipsis)),
                            new Text(FormatTime(_conversation.UpdatedAt), style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption), softWrap: false)
                        }),
                        new Row(gap: AppSpacing.Xs, children: new Widget[] {
                            new Expanded(new Text(_preview, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption), softWrap: false, overflow: TextOverflow.Ellipsis)),
                            BuildUnreadBadge()
                        })
                    }))
                })),
                onPressed: _onPressed,
                buttonStyle: BuildStyle(),
                semanticsLabel: $"Open conversation with {_conversation.ParticipantName}");
        }

        private Widget BuildUnreadBadge() => _conversation.UnreadCount == 0
            ? new SizedBox(new Spacer(), width: 0f)
            : new SizedBox(new Card(
                child: new Center(new Text(_conversation.UnreadCount.ToString(), style: new TextStyle(color: AppColors.Primary, fontSize: 11f, fontStyle: FontStyle.Bold))),
                padding: EdgeInsets.All(0f),
                backgroundColor: AppColors.PrimarySoft,
                borderRadius: BorderRadius.All(AppRadius.Pill)), width: 22f, height: 20f);

        private ButtonStyle BuildStyle() => new(
            background: _selected ? AppColors.PrimarySoft : AppColors.Transparent,
            foreground: AppColors.TextPrimary,
            padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: AppSpacing.Md),
            shape: BorderRadius.All(AppRadius.Sm),
            minimumSize: new Vector2(0f, 72f),
            hovered: new ButtonStateStyle(background: _selected ? AppColors.PrimarySoft : AppColors.SurfaceHover));

        private static string FormatTime(DateTime dateTime) => dateTime.Date == DateTime.Today ? dateTime.ToString("HH:mm") : dateTime.ToString("MMM d");
    }
}
