#nullable enable
using UnityEngine;

namespace LumaFlow.Editor {
    internal static class EditorToolWidgets {
        internal static readonly Color Background = new Color32(28, 32, 43, 255);
        internal static readonly Color Surface = new Color32(39, 44, 59, 255);
        internal static readonly Color Ink = new Color32(238, 241, 248, 255);
        internal static readonly Color Muted = new Color32(174, 185, 205, 255);
        private static readonly Color Accent = new Color32(188, 170, 231, 255);
        internal static ThemeData Theme { get; } = new(
            new ColorScheme(Background, Surface, Background, Accent, Surface, Background, Ink, Muted, Muted),
            new TypographyTheme(new TextStyle(Ink, 22, FontStyle.Bold), new TextStyle(Ink, 16, FontStyle.Bold),
                new TextStyle(Ink, 12), new TextStyle(Ink, 12)),
            new SpacingTheme(4, 8, 12, 16, 24),
            new RadiusTheme(BorderRadius.All(6), BorderRadius.All(10), BorderRadius.All(14)),
            new ButtonTheme(
                new ButtonStyle(background: Accent, foreground: Background, padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                    shape: BorderRadius.All(8), minimumSize: new Vector2(0, 32), typography: new TextStyle(fontSize: 12)),
                new ButtonStyle(background: Surface, foreground: Ink, padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                    shape: BorderRadius.All(8), minimumSize: new Vector2(0, 32), typography: new TextStyle(fontSize: 12))),
            dropdownTheme: new DropdownTheme(new DropdownStyle(background: Surface, foreground: Ink, border: Muted,
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
