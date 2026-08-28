#nullable enable

using UnityEngine;
using UnityEngine.UIElements;
using Framework = global::LumaFlow.LumaFlow;

namespace LumaFlow.Samples.GettingStarted {

    [RequireComponent(typeof(UIDocument))]
    public sealed class LumaFlowCounterSample : MonoBehaviour {
        private static readonly Color Mist = new Color32(0xB8, 0xC8, 0xD6, 0xFF);
        private static readonly Color Porcelain = new Color32(0xF3, 0xF2, 0xEE, 0xFF);
        private static readonly Color Steel = new Color32(0x96, 0xAF, 0xC4, 0xFF);
        private static readonly Color Lavender = new Color32(0xC6, 0xBE, 0xD8, 0xFF);
        private static readonly Color Ink = new Color32(0x4A, 0x51, 0x60, 0xFF);

        private static readonly ButtonStyle PrimaryAction = new(
            background: Ink,
            foreground: Porcelain,
            padding: EdgeInsets.Symmetric(horizontal: 22f, vertical: 12f),
            shape: BorderRadius.All(12f),
            typography: new TextStyle(fontSize: 14f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(96f, 44f),
            hovered: new ButtonStateStyle(background: new Color(0.36f, 0.40f, 0.47f)),
            pressed: new ButtonStateStyle(background: new Color(0.22f, 0.24f, 0.29f)));

        private static readonly ButtonStyle SecondaryAction = new(
            background: Mist,
            foreground: Ink,
            padding: EdgeInsets.Symmetric(horizontal: 18f, vertical: 12f),
            shape: BorderRadius.All(12f),
            typography: new TextStyle(fontSize: 14f, fontStyle: FontStyle.Bold),
            minimumSize: new Vector2(82f, 44f),
            hovered: new ButtonStateStyle(background: Steel),
            pressed: new ButtonStateStyle(background: new Color(0.53f, 0.63f, 0.71f)));

        private static readonly ThemeData SampleTheme = new(
            colors: new ColorScheme(
                canvas: Porcelain,
                surface: new Color(0.98f, 0.98f, 0.97f),
                surfaceVariant: new Color(0.91f, 0.93f, 0.94f),
                primary: Ink,
                primaryContainer: Lavender,
                onPrimary: Porcelain,
                onSurface: Ink,
                onSurfaceVariant: new Color(0.42f, 0.46f, 0.53f),
                outline: Mist),
            typography: new TypographyTheme(
                title: new TextStyle(Ink, 25f, FontStyle.Bold),
                headline: new TextStyle(Ink, 18f, FontStyle.Bold),
                body: new TextStyle(new Color(0.38f, 0.42f, 0.49f), 15f),
                label: new TextStyle(Porcelain, 14f, FontStyle.Bold)),
            spacing: new SpacingTheme(4f, 8f, 12f, 20f, 32f),
            radius: new RadiusTheme(
                BorderRadius.All(8f),
                BorderRadius.All(12f),
                BorderRadius.All(18f)),
            buttonTheme: new ButtonTheme(
                PrimaryAction,
                SecondaryAction),
            iconTheme: new IconThemeData(size: 24f, color: Color.white));

        private readonly State<int> _count = new(0);
        private MountHandle? _mount;

        private void OnEnable() {
            _mount?.Dispose();
            _mount = Framework.Mount(
                BuildApplication(),
                GetComponent<UIDocument>().rootVisualElement);
        }

        private void OnDisable() {
            _mount?.Dispose();
            _mount = null;
        }

        private Widget BuildApplication() => new Theme(
            SampleTheme,
            new Scaffold(
                new Center(
                    new Padding(
                        new SizedBox(
                            new Card(
                                new Column(
                                    new Widget[] {
                                        BuildHeader(),
                                        BuildCounter(),
                                        BuildActions(),
                                        new Text(
                                            "Built entirely with LumaFlow widgets",
                                            new TextStyle(SampleTheme.Colors.OnSurfaceVariant, 12f))
                                    },
                                    gap: SampleTheme.Spacing.Large),
                                padding: EdgeInsets.All(28f),
                                backgroundColor: SampleTheme.Colors.Surface,
                                borderRadius: SampleTheme.Radius.Large,
                                border: Border.All(Mist, 1f)),
                            width: 460f),
                        EdgeInsets.All(20f)))));

        private static Widget BuildHeader() => new Row(
            new Widget[] {
                new Card(
                    new Icon(LumaIcons.LumaFlow, size: 76f, color: Color.white),
                    padding: EdgeInsets.All(12f),
                    backgroundColor: Ink,
                    borderRadius: BorderRadius.All(18f)),
                new Expanded(
                    new Column(
                        new Widget[] {
                            new Text("Welcome to LumaFlow", SampleTheme.Typography.Title),
                            new Text(
                                "Reactive UI for Unity, written in C#.",
                                SampleTheme.Typography.Body)
                        },
                        gap: 6f,
                        mainAxisAlignment: MainAxisAlignment.Center))
            },
            gap: 18f,
            crossAxisAlignment: CrossAxisAlignment.Center);

        private Widget BuildCounter() => new Card(
            new ReactiveBuilder<int>(
                _count,
                value => new Column(
                    new Widget[] {
                        new Text("CURRENT VALUE", new TextStyle(Ink, 11f, FontStyle.Bold)),
                        new Text(value.ToString(), new TextStyle(Ink, 44f, FontStyle.Bold)),
                        new Text(
                            value == 0 ? "Ready when you are" : "State updates without rebuilding the scene",
                            SampleTheme.Typography.Body)
                    },
                    gap: 5f,
                    crossAxisAlignment: CrossAxisAlignment.Center)),
            padding: EdgeInsets.Symmetric(horizontal: 20f, vertical: 22f),
            backgroundColor: Lavender,
            borderRadius: BorderRadius.All(16f));

        private Widget BuildActions() => new Row(
            new Widget[] {
                new Expanded(new Button("Decrease", () => _count.Value--, ButtonVariant.Secondary)),
                new Button("Reset", () => _count.Value = 0, ButtonVariant.Secondary),
                new Expanded(new Button("Increase", () => _count.Value++))
            },
            gap: 10f,
            crossAxisAlignment: CrossAxisAlignment.Center);
    }
}
