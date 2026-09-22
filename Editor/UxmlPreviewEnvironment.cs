#nullable enable

using System;
using UnityEngine;

namespace LumaFlow.Editor {
    internal readonly struct UxmlPreviewEnvironment {
        internal UxmlPreviewEnvironment(int width, int height, string? locale, float textScale) {
            if ((width == 0) != (height == 0) || width < 0 || height < 0)
                throw new ArgumentException("Preview width and height must both be zero or both be positive.");
            if (float.IsNaN(textScale) || float.IsInfinity(textScale) || textScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(textScale), "Preview text scale must be finite and positive.");
            Width = width;
            Height = height;
            Locale = ParseLocale(locale);
            TextScale = textScale;
        }

        internal int Width { get; }
        internal int Height { get; }
        internal Locale? Locale { get; }
        internal float TextScale { get; }

        internal Vector2 ResolveViewport() => Width > 0 && Height > 0
            ? new Vector2(Width, Height)
            : UxmlPreviewViewport.GetGameViewSize();

        internal Widget Wrap(Widget widget) {
            if (widget == null) throw new ArgumentNullException(nameof(widget));
            if (Locale is { } locale) widget = new Localizations(locale, widget);
            if (!Mathf.Approximately(TextScale, 1f)) widget = new TextScale(new TextScaler(TextScale), widget);
            return widget;
        }

        private static Locale? ParseLocale(string? value) {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var parts = value.Trim().Replace('_', '-').Split('-');
            return parts.Length switch {
                1 => new Locale(parts[0]),
                2 when parts[1].Length == 4 => new Locale(parts[0], scriptCode: parts[1]),
                2 => new Locale(parts[0], countryCode: parts[1]),
                3 => new Locale(parts[0], countryCode: parts[2], scriptCode: parts[1]),
                _ => throw new ArgumentException($"Preview locale '{value}' must use language, language-region or language-script-region format.", nameof(value))
            };
        }
    }
}
