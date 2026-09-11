using System;
using System.Collections.Generic;
using Assets.Application.Features.Messages.Data;
using Assets.Application.Features.Messages.Models;
using Assets.Application.Features.Messages.Widgets;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Messages.View {
    /// <summary>Interactive dogfood messaging feature with locally owned mock conversation state.</summary>
    public sealed class MessagesPage : StatefulWidget<MessagesPageState> { }

    public sealed class MessagesPageState : WidgetState {
        private readonly List<Conversation> _conversations = new();
        private readonly Dictionary<string, List<ChatMessage>> _timelines = new();
        private readonly State<string> _search = new(string.Empty);
        private readonly State<string> _draft = new(string.Empty);
        private string _selectedConversationId = "olivia";
        private int _nextMessageNumber = 1;

        protected override void InitState() {
            foreach (var conversation in DemoConversations.All) {
                _conversations.Add(conversation);
                _timelines.Add(conversation.Id, new List<ChatMessage>(conversation.Messages));
            }
        }

        public override Widget Build(BuildContext context) => SizedBox.Expand(BuildMessenger());

        private Widget BuildMessenger() => new AppCard(
            SizedBox.Expand(new Row(children: new Widget[] {
                new SizedBox(SizedBox.ExpandHeight(BuildConversationPane()), width: 312f),
                new SizedBox(new Card(new Spacer(), padding: EdgeInsets.All(0f), backgroundColor: AppColors.Border, borderRadius: BorderRadius.All(AppRadius.None)), width: 1f),
                new Expanded(SizedBox.Expand(new Card(BuildChatPane(), padding: EdgeInsets.All(0f), backgroundColor: AppColors.Background, borderRadius: BorderRadius.All(AppRadius.None))))
            })),
            padding: EdgeInsets.All(0f),
            borderRadius: BorderRadius.All(AppRadius.Lg),
            border: Border.All(AppColors.Border));

        private Widget BuildConversationPane() => new Padding(
            padding: EdgeInsets.All(AppSpacing.Md),
            child: new Column(gap: AppSpacing.Md, children: new Widget[] {
                new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
                    new Expanded(new Text("Messages", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading3, fontStyle: FontStyle.Bold))),
                    new IconButton(LumaIcons.Edit, () => SetState(() => _draft.Value = string.Empty), tooltip: "New message", hitSize: AppSizes.IconButtonSm)
                }),
                new SizedBox(new TextField(
                    _search,
                    placeholder: "Search messages...",
                    style: new TextFieldStyle(
                        background: AppColors.SurfaceSecondary,
                        border: AppColors.Border,
                        padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: 6f),
                        shape: BorderRadius.All(AppRadius.Sm))), height: AppSizes.ControlHeightSm),
                new Expanded(new ReactiveBuilder<string>(_search, BuildFilteredConversationList))
            }));

        private Widget BuildFilteredConversationList(string query) {
            var rows = new List<Widget>();
            foreach (var conversation in _conversations) {
                if (!Matches(conversation, query)) continue;
                rows.Add(new ConversationListItem(
                    conversation,
                    PreviewFor(conversation),
                    conversation.Id == _selectedConversationId,
                    () => SelectConversation(conversation.Id)).WithKey(new WidgetKey(conversation.Id)));
            }

            return rows.Count == 0
                ? new Center(new Text("No conversations found.", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall)))
                : new ScrollView(new Column(gap: AppSpacing.Xs, children: rows));
        }

        private Widget BuildChatPane() {
            var conversation = SelectedConversation;
            return SizedBox.Expand(new Stack(new Widget[] {
                new Positioned(
                    SizedBox.Expand(BuildChatHeader(conversation)),
                    left: AppSpacing.Lg,
                    top: AppSpacing.Lg,
                    right: AppSpacing.Lg,
                    height: AppSizes.ControlHeightLg),
                new Positioned(
                    SizedBox.Expand(BuildTimeline(conversation)),
                    left: AppSpacing.Lg,
                    top: 84f,
                    right: AppSpacing.Lg,
                    bottom: 84f),
                new Positioned(
                    SizedBox.Expand(new MessageComposer(_draft, SendMessage)),
                    left: AppSpacing.Lg,
                    right: AppSpacing.Lg,
                    bottom: AppSpacing.Lg,
                    height: AppSizes.ControlHeightLg)
            }));
        }

        private static Widget BuildChatHeader(Conversation conversation) => new Row(gap: AppSpacing.Sm, crossAxisAlignment: CrossAxisAlignment.Center, children: new Widget[] {
            new CircleAvatar(
                child: new Text(conversation.Initials, style: new TextStyle(color: AppColors.White, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)),
                radius: AppSizes.AvatarMd * 0.5f,
                backgroundColor: AppColors.Primary,
                semanticsLabel: conversation.ParticipantName),
            new Expanded(new Column(gap: AppSpacing.Xxs, children: new Widget[] {
                new Text(conversation.ParticipantName, style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodyLarge, fontStyle: FontStyle.Bold)),
                new Row(gap: AppSpacing.Xxs, children: new Widget[] {
                    new CircleAvatar(radius: 3f, backgroundColor: conversation.IsOnline ? AppColors.Success : AppColors.TextMuted),
                    new Text(conversation.IsOnline ? "online" : conversation.ParticipantRole, style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.Caption))
                })
            })),
            new IconButton(LumaIcons.MoreHorizontal, () => { }, tooltip: "Conversation actions", hitSize: AppSizes.IconButtonMd)
        });

        private Widget BuildTimeline(Conversation conversation) {
            var items = new List<Widget> {
                new Center(new Text("Today", style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption)))
            };
            foreach (var message in _timelines[conversation.Id]) items.Add(new ChatBubble(message).WithKey(new WidgetKey(message.Id)));
            return new ScrollView(new Column(gap: AppSpacing.Sm, children: items));
        }

        private Conversation SelectedConversation {
            get {
                foreach (var conversation in _conversations) if (conversation.Id == _selectedConversationId) return conversation;
                throw new InvalidOperationException("Selected conversation does not exist.");
            }
        }

        private void SelectConversation(string id) {
            if (_selectedConversationId == id) return;
            SetState(() => {
                _selectedConversationId = id;
                _draft.Value = string.Empty;
            });
        }

        private void SendMessage() {
            var text = _draft.Value.Trim();
            if (text.Length == 0) return;
            SetState(() => {
                _timelines[_selectedConversationId].Add(new ChatMessage($"local-{_nextMessageNumber++}", "alex", text, DateTime.Now, true));
                _draft.Value = string.Empty;
            });
        }

        private string PreviewFor(Conversation conversation) {
            var timeline = _timelines[conversation.Id];
            return timeline.Count == 0 ? conversation.Preview : timeline[timeline.Count - 1].Text;
        }

        private static bool Matches(Conversation conversation, string query) => string.IsNullOrWhiteSpace(query)
            || conversation.ParticipantName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
            || conversation.ParticipantRole.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
