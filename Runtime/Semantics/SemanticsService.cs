#nullable enable

using System;
using UnityEngine;
using UnityEngine.Accessibility;

namespace LumaFlow {

    /// <summary>Provides platform assistive-technology announcements.</summary>
    public static class SemanticsService {
        public static bool IsScreenReaderSupported =>
            Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer;

        public static bool IsScreenReaderEnabled =>
            IsScreenReaderSupported && AssistiveSupport.isScreenReaderEnabled;

        /// <summary>Sends an announcement when the current platform screen reader is active.</summary>
        public static bool Announce(string message) {
            if (string.IsNullOrWhiteSpace(message)) {
                throw new ArgumentException("An accessibility announcement cannot be empty.", nameof(message));
            }

            if (!IsScreenReaderEnabled) return false;
            AssistiveSupport.notificationDispatcher.SendAnnouncement(message);
            return true;
        }
    }
}
