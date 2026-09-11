using System;
using System.Collections.Generic;

namespace Assets.Application.Features.Messages.Models {
    /// <summary>Stable conversation metadata. Its mutable timeline is owned by the Messages screen state.</summary>
    public sealed class Conversation {
        public Conversation(string id, string participantName, string participantRole, string initials, string preview, DateTime updatedAt, bool isOnline, int unreadCount, IReadOnlyList<ChatMessage> messages) {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Conversation id cannot be empty.", nameof(id)) : id;
            ParticipantName = string.IsNullOrWhiteSpace(participantName) ? throw new ArgumentException("Participant name cannot be empty.", nameof(participantName)) : participantName;
            ParticipantRole = string.IsNullOrWhiteSpace(participantRole) ? throw new ArgumentException("Participant role cannot be empty.", nameof(participantRole)) : participantRole;
            Initials = string.IsNullOrWhiteSpace(initials) ? throw new ArgumentException("Initials cannot be empty.", nameof(initials)) : initials;
            Preview = preview ?? throw new ArgumentNullException(nameof(preview));
            UpdatedAt = updatedAt;
            IsOnline = isOnline;
            UnreadCount = Math.Max(0, unreadCount);
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        }

        public string Id { get; }
        public string ParticipantName { get; }
        public string ParticipantRole { get; }
        public string Initials { get; }
        public string Preview { get; }
        public DateTime UpdatedAt { get; }
        public bool IsOnline { get; }
        public int UnreadCount { get; }
        public IReadOnlyList<ChatMessage> Messages { get; }
    }
}
