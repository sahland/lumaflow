#nullable enable

using UnityEngine;

namespace LumaFlow.Editor {
    [CreateAssetMenu(menuName = "LumaFlow/UXML Preview Demo")]
    public sealed class UxmlPreviewDemo : UxmlPreviewDefinition {
        [SerializeField] private string _title = "LumaFlow preview";
        [SerializeField] private int _count = 3;

        public override Widget CreateWidget() => new SizedBox(new Container(
            new Column(new Widget[] {
                new Text(_title, new TextStyle(fontSize: 24, color: new Color(0.29f, 0.32f, 0.38f))),
                new Text($"Count: {_count}", new TextStyle(fontSize: 16)),
                new Row(new Widget[] {
                    new Button("Decrease", () => { }),
                    new Button("Increase", () => { })
                }, gap: 12)
            }, gap: 16),
            new BoxDecoration(new Color(0.95f, 0.95f, 0.93f), BorderRadius.All(16)),
            EdgeInsets.All(24)), width: 400);
    }
}
