#nullable enable
using UnityEngine;

namespace LumaFlow.Editor {
    internal static class EditorToolWidgets {
        // Shared LumaFlow website palette: canvas, paper, graphite, blue, lavender.
        internal static readonly Color Background = new Color32(243, 242, 238, 255);
        internal static readonly Color Surface = new Color32(250, 249, 245, 255);
        internal static readonly Color Ink = new Color32(74, 81, 96, 255);
        internal static readonly Color Muted = new Color32(102, 112, 125, 255);
        private static readonly Color Accent = new Color32(63, 70, 84, 255);
        private static readonly Color Blue = new Color32(184, 200, 214, 255);
        private static readonly Color Lavender = new Color32(198, 190, 216, 255);
        private static readonly Color Outline = new Color(74f / 255, 81f / 255, 96f / 255, .18f);
        internal static ThemeData Theme { get; } = new(
            new ColorScheme(Background, Surface, Blue, Accent, Lavender, Surface, Ink, Muted, Outline),
            new TypographyTheme(new TextStyle(Ink, 22, FontStyle.Bold), new TextStyle(Ink, 16, FontStyle.Bold),
                new TextStyle(Ink, 12), new TextStyle(Ink, 12)),
            new SpacingTheme(4, 8, 12, 16, 24),
            new RadiusTheme(BorderRadius.All(6), BorderRadius.All(10), BorderRadius.All(14)),
            new ButtonTheme(
                new ButtonStyle(background: Accent, foreground: Surface, padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                    shape: BorderRadius.All(8), minimumSize: new Vector2(0, 32), typography: new TextStyle(fontSize: 12)),
                new ButtonStyle(background: Blue, foreground: Ink, padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                    shape: BorderRadius.All(8), minimumSize: new Vector2(0, 32), typography: new TextStyle(fontSize: 12))),
            dropdownTheme: new DropdownTheme(new DropdownStyle(background: Surface, foreground: Ink, border: Outline,
                padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 7), shape: BorderRadius.All(6),
                typography: new TextStyle(Ink, 12))),
            checkboxTheme: new CheckboxTheme(new CheckboxStyle(size: 16, shape: BorderRadius.All(3))));

        internal static Widget Hint(string text) => new Text(text, new TextStyle(Muted, 12));
        // Editor BaseField labels have an Inspector-oriented minimum width. Keep
        // captions in our layout so narrow settings columns retain a usable input.
        internal static Widget Field(string label, Widget input) => new Column(new Widget[] {
            Hint(label), input
        }, gap: 6);
        internal static Widget Card(Widget child) => new Container(child,
            new BoxDecoration(Surface, BorderRadius.All(12)), EdgeInsets.All(16));
    }
}
