#nullable enable

using System;
using System.Reflection;
using UnityEngine;

namespace LumaFlow.Editor {
    internal static class UxmlPreviewViewport {
        private static readonly MethodInfo? MainGameViewSize = Type.GetType("UnityEditor.GameView,UnityEditor")
            ?.GetMethod("GetSizeOfMainGameView", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Vector2 FallbackSize = new(1280f, 720f);

        internal static Vector2 GetGameViewSize() {
            try {
                if (MainGameViewSize?.Invoke(null, null) is Vector2 size
                    && IsUsable(size.x) && IsUsable(size.y)) {
                    return size;
                }
            } catch (Exception) {
                // Headless editors and Unity versions without an initialized Game View use a stable fallback.
            }

            return FallbackSize;
        }

        private static bool IsUsable(float value) => !float.IsNaN(value)
            && !float.IsInfinity(value)
            && value > 0f;
    }
}
