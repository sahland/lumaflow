#nullable enable

using UnityEngine;

namespace LumaFlow.Editor {
    [CreateAssetMenu(menuName = "LumaFlow/UXML Preview Demo")]
    public sealed class UxmlPreviewDemo : UxmlPreviewDefinition {
        [SerializeField] private string _title = "LumaFlow preview";
        [SerializeField] private int _count = 3;
        private static readonly Color Ink = new Color32(0x4A, 0x51, 0x60, 255);
        private static readonly Color Paper = new Color32(0xF3, 0xF2, 0xEE, 255);
        private static readonly Color Lavender = new Color32(0xC6, 0xBE, 0xD8, 255);
        private static readonly Color Blue = new Color32(0xB8, 0xC8, 0xD6, 255);

        public override Widget CreateWidget() => new SizedBox(new Container(
            new Column(new Widget[] {
                new Text("LUMAFLOW / EDITOR PREVIEW", new TextStyle(fontSize: 11, color: Ink)),
                new Text(_title, new TextStyle(fontSize: 26, color: Ink)),
                new Text("Your C# layout, rendered as UXML.", new TextStyle(fontSize: 14, color: Ink)),
                new Container(new Column(new Widget[] {
                    new Text("CURRENT VALUE", new TextStyle(fontSize: 11, color: Ink)),
                    new Text(_count.ToString(), new TextStyle(fontSize: 42, color: Ink))
                }, gap: 8), new BoxDecoration(Lavender, BorderRadius.All(12)), EdgeInsets.All(20)),
                new Row(new Widget[] {
                    new Expanded(new Button("Decrease", () => { }, style: ActionStyle(Blue, Ink))),
                    new Expanded(new Button("Increase", () => { }, style: ActionStyle(Ink, Paper)))
                }, gap: 12),
                new Text("Visual snapshot · change Count in the Inspector", new TextStyle(fontSize: 11, color: Ink))
            }, gap: 16),
            new BoxDecoration(Paper, BorderRadius.All(16)),
            EdgeInsets.All(24)), width: 400);

        private static ButtonStyle ActionStyle(Color background, Color foreground) => new ButtonStyle(
            background: background, foreground: foreground,
            padding: EdgeInsets.Symmetric(horizontal: 16, vertical: 12),
            shape: BorderRadius.All(10), typography: new TextStyle(fontSize: 14),
            minimumSize: new Vector2(0, 44));
    }
}
