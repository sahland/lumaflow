using System;

namespace Assets.Application.Features.Messages.Models {
    /// <summary>One immutable message in a mocked conversation timeline.</summary>
    public sealed class ChatMessage {
        public ChatMessage(string id, string authorId, string text, DateTime sentAt, bool isMine) {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Message id cannot be empty.", nameof(id)) : id;
            AuthorId = string.IsNullOrWhiteSpace(authorId) ? throw new ArgumentException("Author id cannot be empty.", nameof(authorId)) : authorId;
            Text = string.IsNullOrWhiteSpace(text) ? throw new ArgumentException("Message text cannot be empty.", nameof(text)) : text;
            SentAt = sentAt;
            IsMine = isMine;
        }

        public string Id { get; }
        public string AuthorId { get; }
        public string Text { get; }
        public DateTime SentAt { get; }
        public bool IsMine { get; }
    }
}
