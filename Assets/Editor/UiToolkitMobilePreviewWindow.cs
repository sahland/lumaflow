#nullable enable

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class UiToolkitMobilePreviewWindow : EditorWindow
{
    private static readonly Color Canvas = new(0.06f, 0.07f, 0.10f);
    private static readonly Color Surface = new(0.11f, 0.13f, 0.19f);
    private static readonly Color SurfaceVariant = new(0.15f, 0.17f, 0.24f);
    private static readonly Color Primary = new(0.56f, 0.47f, 1f);
    private static readonly Color OnPrimary = new(0.10f, 0.08f, 0.18f);
    private static readonly Color Body = new(0.79f, 0.81f, 0.88f);

    [MenuItem("Window/LumaFlow/Mobile Preview — UI Toolkit")]
    public static void Open() => GetWindow<UiToolkitMobilePreviewWindow>("UI Toolkit Mobile");

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.style.backgroundColor = Canvas;
        rootVisualElement.style.alignItems = Align.Center;
        rootVisualElement.style.justifyContent = Justify.Center;
        rootVisualElement.Add(BuildScreen());
    }

    private VisualElement BuildScreen()
    {
        var screen = new VisualElement();
        screen.style.width = 390f;
        screen.style.height = 844f;
        screen.style.backgroundColor = Canvas;
        screen.style.flexDirection = FlexDirection.Column;
        screen.Add(AppBar());

        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1f;
        scrollView.contentContainer.style.paddingLeft = 20f;
        scrollView.contentContainer.style.paddingTop = 20f;
        scrollView.contentContainer.style.paddingRight = 20f;
        scrollView.contentContainer.style.paddingBottom = 20f;
        scrollView.Add(WelcomeCard());
        scrollView.Add(VerticalSpace(24f));
        scrollView.Add(SettingsSection());
        scrollView.Add(VerticalSpace(24f));
        scrollView.Add(AccountSection());

        screen.Add(scrollView);
        screen.Add(NavigationBar());
        return screen;
    }

    private static VisualElement AppBar()
    {
        var appBar = new VisualElement();
        appBar.style.height = 64f;
        appBar.style.paddingLeft = 20f;
        appBar.style.paddingRight = 20f;
        appBar.style.flexDirection = FlexDirection.Row;
        appBar.style.alignItems = Align.Center;
        appBar.style.borderBottomWidth = 1f;
        appBar.style.borderBottomColor = new Color(0.20f, 0.22f, 0.30f);
        appBar.Add(Label("Luma", Primary, 18f, FontStyle.Bold));
        appBar.Add(Label("Flow", Color.white, 18f, FontStyle.Bold));
        appBar.Add(new VisualElement { style = { flexGrow = 1f } });
        appBar.Add(Label("•••", Body, 14f));
        return appBar;
    }

    private VisualElement WelcomeCard()
    {
        var card = Card(new Color(0.22f, 0.16f, 0.40f), 18f, 10f, radius: 24f);
        card.Add(Label("Good evening, Alex", Color.white, 21f, FontStyle.Bold));
        card.Add(VerticalSpace(10f));
        card.Add(Label("Your listening space is ready for today.", Body, 14f));
        card.Add(VerticalSpace(10f));
        card.Add(Button("Upgrade to Premium", Primary, OnPrimary, ShowSaved, height: 44f));
        return card;
    }

    private VisualElement SettingsSection()
    {
        var section = Column(12f);
        section.Add(Label("Quick settings", Color.white, 16f, FontStyle.Bold));
        section.Add(VerticalSpace(12f));
        section.Add(SettingTile("Playback", "Lossless audio · Wi‑Fi streaming", "Change quality", ShowComingSoon));
        section.Add(VerticalSpace(12f));
        section.Add(SettingTile("Downloads", "18 tracks · 1.4 GB on this device", "Manage storage", ShowSaved));
        return section;
    }

    private VisualElement AccountSection()
    {
        var section = Column(12f);
        section.Add(Label("Account", Color.white, 16f, FontStyle.Bold));
        section.Add(VerticalSpace(12f));
        section.Add(SettingTile("Alex Pavlov", "Premium · Renews 12 September", "View subscription", ShowComingSoon));
        return section;
    }

    private VisualElement SettingTile(string title, string subtitle, string action, System.Action onClick)
    {
        var tile = Card(Surface, 16f, 10f);
        tile.Add(Label(title, Color.white, 16f, FontStyle.Bold));
        tile.Add(VerticalSpace(10f));
        tile.Add(Label(subtitle, Body, 14f));
        tile.Add(VerticalSpace(10f));
        tile.Add(Button(action, SurfaceVariant, Color.white, onClick));
        return tile;
    }

    private static VisualElement NavigationBar()
    {
        var nav = new VisualElement();
        nav.style.height = 60f;
        nav.style.paddingLeft = 26f;
        nav.style.paddingRight = 26f;
        nav.style.flexDirection = FlexDirection.Row;
        nav.style.justifyContent = Justify.SpaceBetween;
        nav.style.alignItems = Align.Center;
        nav.style.backgroundColor = Surface;
        nav.Add(Label("⌂  Home", Primary, 13f, FontStyle.Bold));
        nav.Add(Label("▤  Library", Body, 13f));
        nav.Add(Label("♙  Profile", Body, 13f));
        return nav;
    }

    private static VisualElement Card(Color color, float padding, float gap, float radius = 20f)
    {
        var card = Column(gap);
        card.style.paddingLeft = padding;
        card.style.paddingTop = padding;
        card.style.paddingRight = padding;
        card.style.paddingBottom = padding;
        card.style.backgroundColor = color;
        card.style.borderTopLeftRadius = radius;
        card.style.borderTopRightRadius = radius;
        card.style.borderBottomRightRadius = radius;
        card.style.borderBottomLeftRadius = radius;
        return card;
    }

    private static VisualElement Column(float gap)
    {
        var column = new VisualElement();
        column.style.flexDirection = FlexDirection.Column;
        return column;
    }

    private static VisualElement VerticalSpace(float height)
    {
        var space = new VisualElement();
        space.style.height = height;
        space.style.flexShrink = 0f;
        return space;
    }

    private static Label Label(string text, Color color, float size, FontStyle style = FontStyle.Normal) => new(text)
    {
        style =
        {
            color = color,
            fontSize = size,
            unityFontStyleAndWeight = style
        }
    };

    private static Button Button(string text, Color background, Color foreground, System.Action onClick, float height = 40f)
    {
        var button = new Button(onClick) { text = text };
        button.style.height = height;
        button.style.marginTop = 0f;
        button.style.marginRight = 0f;
        button.style.marginBottom = 0f;
        button.style.marginLeft = 0f;
        button.style.paddingLeft = 14f;
        button.style.paddingRight = 14f;
        button.style.backgroundImage = StyleKeyword.None;
        button.style.backgroundColor = background;
        button.style.color = foreground;
        button.style.borderTopWidth = 0f;
        button.style.borderRightWidth = 0f;
        button.style.borderBottomWidth = 0f;
        button.style.borderLeftWidth = 0f;
        button.style.borderTopLeftRadius = 20f;
        button.style.borderTopRightRadius = 20f;
        button.style.borderBottomRightRadius = 20f;
        button.style.borderBottomLeftRadius = 20f;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        return button;
    }

    private void ShowSaved() => ShowNotification(new GUIContent("Saved to your preferences."));

    private void ShowComingSoon() => ShowNotification(new GUIContent("Available in the full mobile flow."));
}
