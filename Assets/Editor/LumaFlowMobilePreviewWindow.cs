#nullable enable

using System;
using LumaFlow;
using UnityEditor;
using UnityEngine;
using Framework = LumaFlow.LumaFlow;

public sealed class LumaFlowMobilePreviewWindow : EditorWindow
{
    private static readonly ThemeData MobileTheme = CreateMobileTheme();

    private readonly OverlayController _overlay = new();
    private MountHandle? _mount;

    [MenuItem("Window/LumaFlow/Mobile Preview")]
    public static void Open() => GetWindow<LumaFlowMobilePreviewWindow>("LumaFlow Mobile");

    public void CreateGUI()
    {
        _mount?.Dispose();
        rootVisualElement.Clear();
        _mount = Framework.Mount(
            new Theme(MobileTheme, new OverlayHost(new MobileScreen(_overlay), _overlay)),
            rootVisualElement);
    }

    public void OnDisable() => _mount?.Dispose();

    private sealed class MobileScreen : StatelessWidget
    {
        private readonly OverlayController _overlay;
        private readonly State<int> _selectedTab = new(0);
        private readonly Navigator _navigator;

        public MobileScreen(OverlayController overlay)
        {
            _overlay = overlay;
            _navigator = new Navigator(new HomeRoute(this));
        }

        public override Widget Build(BuildContext context)
        {
            var theme = context.Theme
                ?? throw new InvalidOperationException("MobileScreen requires a Theme.");

            return new Container(
                decoration: new BoxDecoration(backgroundColor: theme.Colors.Canvas),
                child: new Center(
                    child: new SizedBox(
                        width: 390f,
                        height: 844f,
                        child: new Scaffold(
                            appBar: CreateAppBar(theme),
                            body: new NavigatorHost(_navigator),
                            navigationBar: new NavigationBar(
                                _selectedTab,
                                new[]
                                {
                                new NavigationDestination("Home", LumaIcons.Home),
                                new NavigationDestination("Library", LumaIcons.Library),
                                new NavigationDestination("Profile", LumaIcons.User)
                                },
                                index => _selectedTab.Value = index)))));
        }

        private AppBar CreateAppBar(ThemeData theme) => new(
            title: new Row(new Widget[]
            {
                new Text("Luma", new TextStyle(theme.Colors.Primary, 18f, FontStyle.Bold)),
                new Text("Flow", new TextStyle(theme.Colors.OnSurface, 18f, FontStyle.Bold))
            }),
            actions: new Widget[]
            {
                new IconButton(LumaIcons.MoreVertical, () => { }, tooltip: "More options")
            });

        private Widget CreateHome(BuildContext context)
        {
            var theme = context.Theme
                ?? throw new InvalidOperationException("Home route requires a Theme.");
            return new ScrollView(
                new Column(
                    new Widget[]
                    {
                        CreateWelcomeCard(theme, () => context.Navigator.Push(new PremiumDetailsRoute())),
                        CreateSettingsSection(theme),
                        CreateAccountSection(theme)
                    },
                    gap: theme.Spacing.ExtraLarge),
                padding: EdgeInsets.Only(
                    left: theme.Spacing.Large,
                    top: theme.Spacing.Large,
                    right: theme.Spacing.Large,
                    bottom: theme.Spacing.Large));
        }

        private Widget CreateWelcomeCard(ThemeData theme, Action openPremium) => new Card(
            padding: EdgeInsets.All(18f),
            backgroundColor: theme.Colors.PrimaryContainer,
            borderRadius: theme.Radius.Large,
            child: new Column(
                gap: theme.Spacing.ExtraSmall,
                children: new Widget[]
                {
                    new Text("Good evening, Alex", theme.Typography.Title),
                    new Text("Your listening space is ready for today."),
                    new SizedBox(
                        height: 44f,
                        child: new Button("Upgrade to Premium", openPremium))
                }));

        private Widget CreateSettingsSection(ThemeData theme) => new Column(
            gap: theme.Spacing.Small,
            children: new Widget[]
            {
                new Text("Quick settings", theme.Typography.Headline),
                CreateSettingTile(theme, "Playback", "Lossless audio · Wi‑Fi streaming", "Change quality", ShowDialog),
                CreateSettingTile(theme, "Downloads", "18 tracks · 1.4 GB on this device", "Manage storage", ShowToast)
            });

        private Widget CreateAccountSection(ThemeData theme) => new Column(
            gap: theme.Spacing.Small,
            children: new Widget[]
            {
                new Text("Account", theme.Typography.Headline),
                CreateSettingTile(theme, "Alex Pavlov", "Premium · Renews 12 September", "View subscription", ShowDialog)
            });

        private Widget CreateSettingTile(ThemeData theme, string title, string subtitle, string action, Action onPressed) => new Card(
            child: new Column(
                gap: theme.Spacing.ExtraSmall,
                children: new Widget[]
                {
                    new Text(title, theme.Typography.Headline),
                    new Text(subtitle),
                    new SizedBox(
                        height: 40f,
                        child: new Button(action, onPressed, variant: ButtonVariant.Secondary))
                }));

        private void ShowToast() => _overlay.ShowToast(
            new Toast(new Text("Saved to your preferences.")),
            TimeSpan.FromSeconds(2));

        private void ShowDialog()
        {
            OverlayHandle? handle = null;
            handle = _overlay.ShowModal(new Dialog(
                new Text("Premium audio"),
                new Text("This action is available in the full mobile flow."),
                new Widget[] { new Button("Close", () => handle?.Close()) }));
        }

        private sealed class HomeRoute : StatelessWidget
        {
            private readonly MobileScreen _screen;

            public HomeRoute(MobileScreen screen) => _screen = screen;

            public override Widget Build(BuildContext context) => _screen.CreateHome(context);
        }

        private sealed class PremiumDetailsRoute : StatelessWidget
        {
            public override Widget Build(BuildContext context)
            {
                var theme = context.Theme
                    ?? throw new InvalidOperationException("Premium details route requires a Theme.");

                return new ScrollView(
                    new Column(
                        gap: theme.Spacing.Large,
                        children: new Widget[]
                        {
                            new SizedBox(
                                height: 40f,
                                child: new Button(
                                    "Back to Home",
                                    () => context.Navigator.Pop(),
                                    variant: ButtonVariant.Secondary)),
                            new Card(
                                padding: EdgeInsets.All(20f),
                                backgroundColor: theme.Colors.PrimaryContainer,
                                borderRadius: theme.Radius.Large,
                                child: new Column(
                                    gap: theme.Spacing.Small,
                                    children: new Widget[]
                                    {
                                        new Text("LumaFlow Premium", theme.Typography.Title),
                                        new Text("A focused listening space with no interruptions."),
                                        new Text("7-day free trial · Cancel anytime", theme.Typography.Label)
                                    })),
                            new Text("Included with Premium", theme.Typography.Headline),
                            CreateBenefit(theme, "Lossless audio", "Hear every detail in studio-quality sound."),
                            CreateBenefit(theme, "Offline library", "Take your favourite tracks anywhere."),
                            CreateBenefit(theme, "Personal sessions", "Keep your listening space distraction-free.")
                        }),
                    padding: EdgeInsets.All(theme.Spacing.Large));
            }

            private static Widget CreateBenefit(ThemeData theme, string title, string subtitle) => new Card(
                child: new Column(
                    gap: theme.Spacing.ExtraSmall,
                    children: new Widget[]
                    {
                        new Text(title, theme.Typography.Headline),
                        new Text(subtitle)
                    }));
        }
    }

    private static ThemeData CreateMobileTheme()
    {
        var colors = new ColorScheme(
            canvas: new Color(0.06f, 0.07f, 0.10f),
            surface: new Color(0.11f, 0.13f, 0.19f),
            surfaceVariant: new Color(0.15f, 0.17f, 0.24f),
            primary: new Color(0.56f, 0.47f, 1f),
            primaryContainer: new Color(0.22f, 0.16f, 0.40f),
            onPrimary: new Color(0.10f, 0.08f, 0.18f),
            onSurface: Color.white,
            onSurfaceVariant: new Color(0.79f, 0.81f, 0.88f),
            outline: new Color(0.20f, 0.22f, 0.30f));
        var typography = new TypographyTheme(
            title: new TextStyle(colors.OnSurface, 21f, FontStyle.Bold),
            headline: new TextStyle(colors.OnSurface, 16f, FontStyle.Bold),
            body: new TextStyle(colors.OnSurfaceVariant, 14f),
            label: new TextStyle(colors.OnSurface, 14f, FontStyle.Bold));
        var spacing = new SpacingTheme(10f, 12f, 16f, 20f, 24f);
        var radius = new RadiusTheme(BorderRadius.All(20f), BorderRadius.All(22f), BorderRadius.All(24f));
        var buttons = new ButtonTheme(
            primary: new ButtonStyle(
                background: colors.Primary,
                foreground: colors.OnPrimary,
                padding: EdgeInsets.Symmetric(horizontal: 20f, vertical: 10f),
                shape: radius.Medium,
                typography: typography.Label,
                minimumSize: new Vector2(64f, 40f),
                hovered: new ButtonStateStyle(background: new Color(0.63f, 0.55f, 1f)),
                pressed: new ButtonStateStyle(background: new Color(0.46f, 0.37f, 0.84f))),
            secondary: new ButtonStyle(
                background: colors.SurfaceVariant,
                foreground: colors.OnSurface,
                padding: EdgeInsets.Symmetric(horizontal: 14f, vertical: 10f),
                shape: radius.Small,
                typography: typography.Label,
                minimumSize: new Vector2(64f, 40f),
                hovered: new ButtonStateStyle(background: new Color(0.20f, 0.23f, 0.32f)),
                pressed: new ButtonStateStyle(background: new Color(0.10f, 0.12f, 0.18f))));
        return new ThemeData(
            colors,
            typography,
            spacing,
            radius,
            buttons,
            new IconThemeData(size: 20f, color: colors.OnSurfaceVariant));
    }
}
