using System;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Messages.Widgets {
    /// <summary>Controlled message editor; submission authority remains with the screen state.</summary>
    public sealed class MessageComposer : StatelessWidget {
        private readonly State<string> _draft;
        private readonly Action _onSend;

        public MessageComposer(State<string> draft, Action onSend) {
            _draft = draft ?? throw new ArgumentNullException(nameof(draft));
            _onSend = onSend ?? throw new ArgumentNullException(nameof(onSend));
        }

        public override Widget Build(BuildContext context) => new Row(gap: AppSpacing.Sm, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new IconButton(LumaIcons.Plus, () => { }, tooltip: "Attach file", hitSize: AppSizes.IconButtonMd),
            new Expanded(new SizedBox(new TextField(
                _draft,
                placeholder: "Write a message...",
                style: new TextFieldStyle(
                    background: AppColors.Surface,
                    border: AppColors.Border,
                    padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Md, vertical: AppSpacing.Sm),
                    shape: BorderRadius.All(AppRadius.Md)),
                onSubmitted: _ => _onSend()), height: AppSizes.ControlHeightLg)),
            new IconButton(LumaIcons.Upload, _onSend, tooltip: "Send message", hitSize: AppSizes.ControlHeightLg, variant: ButtonVariant.Primary)
        });
    }
}
