#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    internal static class UxmlPreviewStatus {
        private static readonly Dictionary<string, string> Failures = new(StringComparer.Ordinal);
        private static readonly Type? GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

        internal static int FailureCount => Failures.Count;
        internal static string? CurrentMessage => Failures.Values.LastOrDefault();

        internal static void ReportFailure(string id, string label, Exception exception) {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A preview status id is required.", nameof(id));
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            var detail = exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (detail.Length > 180) detail = detail.Substring(0, 177) + "...";
            Failures[id] = $"LumaFlow preview failed: {label}\n{detail}\nShowing last successful UXML.";
            RefreshGameViewNotifications();
        }

        internal static void ReportSuccess(string id) {
            if (!Failures.Remove(id)) return;
            RefreshGameViewNotifications();
        }

        internal static void Clear() {
            Failures.Clear();
            RefreshGameViewNotifications();
        }

        private static void RefreshGameViewNotifications() {
            if (GameViewType == null) return;
            var message = CurrentMessage;
            foreach (var window in Resources.FindObjectsOfTypeAll(GameViewType).OfType<EditorWindow>()) {
                if (message == null) window.RemoveNotification();
                else window.ShowNotification(new GUIContent(message));
            }
        }
    }
}
