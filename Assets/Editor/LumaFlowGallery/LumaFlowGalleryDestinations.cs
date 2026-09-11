#nullable enable

using LumaFlow;

internal sealed class GalleryLibraryRoute : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryLibraryRoute(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return new ScrollView(
            new Center(
                new ConstrainedBox(
                    constraints: new BoxConstraints(maxWidth: 760f),
                    child: new Column(
                        gap: theme.Spacing.Large,
                        children: new Widget[]
                        {
                            new Column(
                                gap: theme.Spacing.ExtraSmall,
                                children: new Widget[]
                                {
                                    new Text("Library", theme.Typography.Title),
                                    new Text("Virtualized activity from the same explicit app state.")
                                }),
                            new Card(
                                padding: EdgeInsets.All(theme.Spacing.Large),
                                borderRadius: theme.Radius.Medium,
                                child: new SizedBox(
                                    height: 320f,
                                    child: new ListView<string>(
                                        _app.RecentActivity,
                                        item => new ListTile(item, "Gallery activity"),
                                        itemHeight: 58f)))
                        }))),
            padding: EdgeInsets.All(theme.Spacing.Large));
    }
}

internal sealed class GalleryProfileRoute : StatelessWidget
{
    private readonly LumaFlowGalleryApp _app;

    public GalleryProfileRoute(LumaFlowGalleryApp app) => _app = app;

    public override Widget Build(BuildContext context)
    {
        var theme = GalleryWidgets.Theme(context);
        return new ScrollView(
            new Center(
                new ConstrainedBox(
                    constraints: new BoxConstraints(maxWidth: 760f),
                    child: new Column(
                        gap: theme.Spacing.Large,
                        children: new Widget[]
                        {
                            new Column(
                                gap: theme.Spacing.ExtraSmall,
                                children: new Widget[]
                                {
                                    new Text("Profile", theme.Typography.Title),
                                    new Text("Controlled state remains owned by the application, not this route.")
                                }),
                            new Card(
                                padding: EdgeInsets.All(theme.Spacing.Large),
                                borderRadius: theme.Radius.Medium,
                                child: new Column(
                                    gap: theme.Spacing.Medium,
                                    children: new Widget[]
                                    {
                                        new ListTile("Display name", _app.DisplayName.Value),
                                        new ListTile("Plan", _app.Plan.Value),
                                        new ListTile("Accent", _app.Accent.Value),
                                        new Button("Back to home", () => _app.SelectDestination(0), variant: ButtonVariant.Secondary)
                                    }))
                        }))),
            padding: EdgeInsets.All(theme.Spacing.Large));
    }
}
