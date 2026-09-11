#nullable enable

using System;
using LumaFlow;
using UnityEngine;
using VisualElement = UnityEngine.UIElements.VisualElement;
using Label = UnityEngine.UIElements.Label;

internal sealed class GalleryHero : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryHero(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return new Card(
            padding: EdgeInsets.All(theme.Spacing.ExtraLarge),
            backgroundColor: theme.Colors.PrimaryContainer,
            borderRadius: theme.Radius.Large,
            child: new Column(
                gap: theme.Spacing.Medium,
                children: new Widget[]
                {
                    new Text("A living component gallery", theme.Typography.Title),
                    new Text("Every control below is the real LumaFlow runtime — mounted into UI Toolkit, themed through BuildContext, and wired to explicit State."),
                    new Row(
                        gap: theme.Spacing.Small,
                        children: new Widget[]
                        {
                            new Button("Open a route", () => context.Navigator.Push(new GalleryDetailsRoute(_app))),
                            new Button("Show toast", _app.ShowToast, variant: ButtonVariant.Secondary),
                            new Spacer(),
                            new Text("LIVE", new TextStyle(theme.Colors.Primary, 13f, FontStyle.Bold))
                        })
                }));
    }
}

internal sealed class GalleryButtonSection : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryButtonSection(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return GalleryWidgets.Section(
            theme,
            "Buttons & overlays",
            "Semantic variants, state styles and scoped temporary UI.",
            new Column(
                gap: theme.Spacing.Medium,
                children: new Widget[]
                {
                    new Row(
                        gap: theme.Spacing.Small,
                        children: new Widget[]
                        {
                            new Expanded(new Button("Primary action", _app.ShowToast)),
                            new Expanded(new Button("Open dialog", _app.ShowDialog, variant: ButtonVariant.Secondary))
                        }),
                    new Row(
                        gap: theme.Spacing.Small,
                        children: new Widget[]
                        {
                            new IconButton(LumaIcons.Bell, _app.ShowToast, tooltip: "Toast"),
                            new IconButton(LumaIcons.MoreVertical, _app.ShowDialog, tooltip: "Dialog"),
                            new Text("Hover, press and focus these controls to inspect their themed states."),
                            new Spacer(),
                            new Button("Confirm delete", _app.ShowDeleteConfirmation, variant: ButtonVariant.Destructive)
                        })
                }));
    }
}

internal sealed class GalleryInputSection : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryInputSection(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return GalleryWidgets.Section(
            theme,
            "Controlled inputs",
            "State remains outside widgets; each control binds in both directions.",
            new Column(
                gap: theme.Spacing.Medium,
                children: new Widget[]
                {
                    new TextField(_app.DisplayNameField, label: "Display name", placeholder: "Choose a name", helperText: "Used only in this preview.", focusNode: _app.DisplayNameFocus),
                    new Slider(_app.Volume, 0f, 100f, label: "Listening volume", focusNode: _app.VolumeFocus),
                    new Row(
                        gap: theme.Spacing.Large,
                        children: new Widget[]
                        {
                            new Expanded(new FormFieldMessage<bool>(
                                _app.NotificationsField,
                                new Switch(_app.NotificationsField, "Product notifications", focusNode: _app.NotificationsFocus))),
                            new Expanded(new FormFieldMessage<bool>(
                                _app.LosslessField,
                                new Checkbox(_app.LosslessField, "Lossless audio", focusNode: _app.LosslessFocus)))
                        }),
                    new FormFieldMessage<string>(
                        _app.PlanField,
                        new Dropdown<string>(_app.PlanField, new[] { "Studio", "Creator", "Enterprise" }, value => value, label: "Plan", focusNode: _app.PlanFocus)),
                    new FormFieldMessage<string>(
                        _app.AccentField,
                        new Row(
                            gap: theme.Spacing.Medium,
                            children: new Widget[]
                            {
                                new Radio<string>(_app.AccentField, "Blue", "Blue", focusNode: _app.BlueAccentFocus),
                                new Radio<string>(_app.AccentField, "Violet", "Violet", focusNode: _app.VioletAccentFocus),
                                new Radio<string>(_app.AccentField, "Mint", "Mint", focusNode: _app.MintAccentFocus)
                            })),
                    new ReactiveBuilder<bool>(
                        _app.ProfileForm.IsValid,
                        isValid => new AsyncButton(
                            "Submit profile",
                            _app.ProfileSubmit,
                            loadingText: "Saving profile…",
                            enabled: isValid,
                            focusNode: _app.SubmitProfileFocus))
                }));
    }
}

internal sealed class GalleryLayoutSection : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryLayoutSection(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return GalleryWidgets.Section(
            theme,
            "Layout & virtualization",
            "Direct native hierarchy, `gap`, width-only responsive composition and a virtualized ListView.",
            new Column(
                gap: theme.Spacing.Medium,
                children: new Widget[]
                {
                    new LayoutBuilder((context, constraints) =>
                        constraints.MaxWidth < 640f
                            ? new Column(
                                gap: theme.Spacing.Small,
                                children: new Widget[]
                                {
                                    GalleryWidgets.MetricCard(theme, "12", "layout primitives"),
                                    GalleryWidgets.MetricCard(theme, "0", "gap wrappers"),
                                    GalleryWidgets.MetricCard(theme, "1", "native ListView")
                                })
                            : new Row(
                                gap: theme.Spacing.Small,
                                children: new Widget[]
                                {
                                    GalleryWidgets.Metric(theme, "12", "layout primitives"),
                                    GalleryWidgets.Metric(theme, "0", "gap wrappers"),
                                    GalleryWidgets.Metric(theme, "1", "native ListView")
                                })),
                    new SizedBox(
                        height: 176f,
                        child: new ListView<string>(
                            _app.RecentActivity,
                            item => new ListTile(item, "Mounted by native virtualization"),
                            itemHeight: 52f))
                }));
    }
}

internal sealed class GalleryInteropSection : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryInteropSection(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return GalleryWidgets.Section(
            theme,
            "Native interop & navigation",
            "An ordinary VisualElement can sit beside LumaFlow widgets; route transitions stay local.",
            new Column(
                gap: theme.Spacing.Medium,
                children: new Widget[]
                {
                    new Native(() => CreateNativeStatus(theme)),
                    new Row(
                        gap: theme.Spacing.Small,
                        children: new Widget[]
                        {
                            new Expanded(new Button("Open detail route", () => context.Navigator.Push(new GalleryDetailsRoute(_app)))),
                            new Expanded(new Button("Append activity", AddActivity, variant: ButtonVariant.Secondary))
                        })
                }));
    }

    private static VisualElement CreateNativeStatus(ThemeData theme)
    {
        var status = new VisualElement();
        status.style.paddingLeft = 14f;
        status.style.paddingRight = 14f;
        status.style.paddingTop = 12f;
        status.style.paddingBottom = 12f;
        status.style.backgroundColor = new Color(0.08f, 0.18f, 0.18f);
        status.style.borderTopLeftRadius = 12f;
        status.style.borderTopRightRadius = 12f;
        status.style.borderBottomLeftRadius = 12f;
        status.style.borderBottomRightRadius = 12f;
        status.Add(new Label("●  Native UI Toolkit status element — connected") { style = { color = theme.Colors.OnSurface } });
        return status;
    }

    private void AddActivity()
    {
        var updated = new System.Collections.Generic.List<string>(_app.RecentActivity.Value)
        {
            "Activity item appended through State<IReadOnlyList<T>>"
        };
        _app.RecentActivity.Value = updated;
    }
}

internal sealed class GalleryDetailsRoute : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryDetailsRoute(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return new ScrollView(
            new Center(
                new SizedBox(
                    width: 680f,
                    child: new Column(
                        gap: theme.Spacing.Large,
                        children: new Widget[]
                        {
                            new Button("← Back to gallery", () => context.Navigator.Pop(), variant: ButtonVariant.Secondary),
                            new Card(
                                padding: EdgeInsets.All(theme.Spacing.ExtraLarge),
                                backgroundColor: theme.Colors.PrimaryContainer,
                                borderRadius: theme.Radius.Large,
                                child: new Column(
                                    gap: theme.Spacing.Medium,
                                    children: new Widget[]
                                    {
                                        new Text("A local route boundary", theme.Typography.Title),
                                        new ReactiveBuilder<string>(
                                            _app.DisplayName,
                                            name => new Text($"Welcome, {name}. This route reads the same explicit State as the gallery.")),
                                        new Text("Only this screen subtree mounted and unmounted. The app shell, theme and OverlayHost stayed stable.")
                                    }))
                        }))),
            padding: EdgeInsets.All(theme.Spacing.ExtraLarge));
    }
}

internal static class GalleryWidgets
{
    public static ThemeData Theme(BuildContext context) => context.Theme
        ?? throw new InvalidOperationException("Gallery widgets require a Theme.");

    public static Widget Section(ThemeData theme, string title, string subtitle, Widget content) => new Column(
        gap: theme.Spacing.Small,
        children: new Widget[]
        {
            new Column(
                gap: theme.Spacing.ExtraSmall,
                children: new Widget[]
                {
                    new Text(title, theme.Typography.Headline),
                    new Text(subtitle)
                }),
            new Card(content, padding: EdgeInsets.All(theme.Spacing.Large), borderRadius: theme.Radius.Medium)
        });

    public static Widget Metric(ThemeData theme, string value, string label) => new Expanded(
        MetricCard(theme, value, label));

    public static Widget MetricCard(ThemeData theme, string value, string label) =>
        new Container(
            padding: EdgeInsets.All(theme.Spacing.Medium),
            decoration: new BoxDecoration(backgroundColor: theme.Colors.SurfaceVariant, borderRadius: theme.Radius.Small),
            child: new Column(
                gap: theme.Spacing.ExtraSmall,
                children: new Widget[]
                {
                    new Text(value, theme.Typography.Title),
                    new Text(label)
                }));
}
