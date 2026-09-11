using System;
using Assets.Application.Features.Messages.Models;

namespace Assets.Application.Features.Messages.Data {
    /// <summary>Dogfood-only data source. Replace this at the repository boundary when messaging is connected.</summary>
    public static class DemoConversations {
        public static Conversation[] All { get; } = {
            Conversation("olivia", "Olivia Martinez", "Project Manager", "OM", "Please review the latest designs.", true, 2, 9, 20,
                Mine("Hi Olivia, the revised lobby plan is ready for review.", 9, 12),
                Theirs("Great, thank you! I will share it with the client this morning.", 9, 16),
                Theirs("Please review the latest designs when you have a moment.", 9, 20)),
            Conversation("liam", "Liam Chen", "Architect", "LC", "Can we schedule a meeting?", true, 0, 8, 42,
                Theirs("I have updated the Riverside floor plans.", 8, 30),
                Mine("Nice. Can we schedule a meeting to walk through them?", 8, 42)),
            Conversation("noah", "Noah Johnson", "Engineer", "NJ", "The structural report is attached.", false, 1, 17, 5,
                Theirs("The structural report is attached. The core looks good.", 17, 5)),
            Conversation("emma", "Emma Williams", "Designer", "EW", "I have added the new material options.", true, 0, 16, 18,
                Theirs("I have added the new material options to the board.", 16, 18)),
            Conversation("william", "William Brown", "Consultant", "WB", "Budget review is complete.", false, 0, 14, 2,
                Theirs("Budget review is complete. I left two notes for the team.", 14, 2))
        };

        private static Conversation Conversation(string id, string name, string role, string initials, string preview, bool online, int unread, int hour, int minute, params ChatMessage[] messages) =>
            new(id, name, role, initials, preview, Today(hour, minute), online, unread, messages);

        private static ChatMessage Mine(string text, int hour, int minute) => new(Guid.NewGuid().ToString("N"), "alex", text, Today(hour, minute), true);
        private static ChatMessage Theirs(string text, int hour, int minute) => new(Guid.NewGuid().ToString("N"), "participant", text, Today(hour, minute), false);
        private static DateTime Today(int hour, int minute) => DateTime.Today.AddHours(hour).AddMinutes(minute);
    }
}
