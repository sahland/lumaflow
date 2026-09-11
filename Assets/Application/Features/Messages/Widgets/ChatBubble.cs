using System;
using Assets.Application.Features.Messages.Models;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Messages.Widgets {
    /// <summary>Presentational bubble for one message. Alignment comes from message ownership.</summary>
    public sealed class ChatBubble : StatelessWidget {
        private readonly ChatMessage _message;

        public ChatBubble(ChatMessage message) => _message = message ?? throw new ArgumentNullException(nameof(message));

        public override Widget Build(BuildContext context) {
            var bubble = new SizedBox(
                new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                    new Text(_message.Text, style: new TextStyle(color: _message.IsMine ? AppColors.White : AppColors.TextPrimary, fontSize: AppTypography.BodySmall)),
                    new Text(_message.SentAt.ToString("HH:mm"), style: new TextStyle(color: _message.IsMine ? new Color(0.86f, 0.92f, 1f) : AppColors.TextMuted, fontSize: 11f))
                }),
                width: 420f);

            return _message.IsMine
                ? Align.CenterRight(new Card(bubble, padding: EdgeInsets.All(AppSpacing.Sm), backgroundColor: AppColors.Primary, borderRadius: BorderRadius.All(AppRadius.Md)))
                : Align.CenterLeft(new Card(bubble, padding: EdgeInsets.All(AppSpacing.Sm), backgroundColor: AppColors.Surface, borderRadius: BorderRadius.All(AppRadius.Md), border: Border.All(AppColors.Border)));
        }
    }
}
