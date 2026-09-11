#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LumaFlow;

internal sealed class LumaFlowGalleryApp : StatelessWidget
{
    private readonly OverlayController _overlay;
    private readonly Navigator _navigator;
    private readonly FormState _profileForm = new(FormValidationMode.OnChange);
    private readonly AsyncAction _profileSubmit;
    private int _activeDestination;

    public LumaFlowGalleryApp(OverlayController overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _navigator = new Navigator(new GalleryHomeRoute(this));
        DisplayNameField = new FormField<string>(
            DisplayName,
            value => string.IsNullOrWhiteSpace(value) ? "Display name is required." : null);
        NotificationsField = new FormField<bool>(
            Notifications,
            value => value ? null : "Product notifications must be acknowledged.");
        LosslessField = new FormField<bool>(Lossless);
        PlanField = new FormField<string>(Plan, value => string.IsNullOrWhiteSpace(value) ? "Choose a plan." : null);
        AccentField = new FormField<string>(Accent, value => string.IsNullOrWhiteSpace(value) ? "Choose an accent." : null);
        _profileSubmit = new AsyncAction(
            cancellationToken => Task.Delay(650, cancellationToken),
            onSucceeded: () => _overlay.ShowToast(
                new Toast(new Text("Profile saved successfully.")),
                TimeSpan.FromSeconds(2)),
            onFailed: exception => _overlay.ShowToast(
                new Toast(new Text($"Could not save profile: {exception.Message}")),
                TimeSpan.FromSeconds(3)));
    }

    public State<string> DisplayName { get; } = new("Alex");
    public State<bool> Notifications { get; } = new(true);
    public State<bool> Lossless { get; } = new(true);
    public State<float> Volume { get; } = new(72f);
    public State<string> Plan { get; } = new("Studio");
    public State<string> Accent { get; } = new("Blue");
    public State<int> SelectedDestination { get; } = new(0);
    internal FocusNode DisplayNameFocus { get; } = new();
    internal FocusNode VolumeFocus { get; } = new();
    internal FocusNode NotificationsFocus { get; } = new();
    internal FocusNode LosslessFocus { get; } = new();
    internal FocusNode PlanFocus { get; } = new();
    internal FocusNode BlueAccentFocus { get; } = new();
    internal FocusNode VioletAccentFocus { get; } = new();
    internal FocusNode MintAccentFocus { get; } = new();
    internal FocusNode SubmitProfileFocus { get; } = new();
    public FormField<string> DisplayNameField { get; }
    public FormField<bool> NotificationsField { get; }
    public FormField<bool> LosslessField { get; }
    public FormField<string> PlanField { get; }
    public FormField<string> AccentField { get; }
    public FormState ProfileForm => _profileForm;
    public AsyncAction ProfileSubmit => _profileSubmit;
    public Navigator Navigator => _navigator;
    public State<IReadOnlyList<string>> RecentActivity { get; } = new(new[]
    {
        "Button state styles applied",
        "Theme resolved through BuildContext",
        "Navigator route mounted locally",
        "Native UI Toolkit element embedded"
    });

    public override Widget Build(BuildContext context)
    {
        var theme = context.Theme ?? throw new InvalidOperationException("Component Gallery requires a Theme.");
        var destinations = new[]
        {
            new NavigationDestination("Home", LumaIcons.Home),
            new NavigationDestination("Library", LumaIcons.Library),
            new NavigationDestination("Profile", LumaIcons.User)
        };
        return new AdaptiveScaffold(
            appBar: new AppBar(
                title: new Row(new Widget[]
                {
                    new Text("Luma", new TextStyle(theme.Colors.Primary, 19f, UnityEngine.FontStyle.Bold)),
                    new Text("Gallery", new TextStyle(theme.Colors.OnSurface, 19f, UnityEngine.FontStyle.Bold))
                }),
                actions: new Widget[]
                {
                    new ReactiveBuilder<bool>(
                        _navigator.CanPopState,
                        canPop => canPop
                            ? new IconButton(LumaIcons.ChevronLeft, () => { _navigator.Pop(); }, tooltip: "Back")
                            : new SizedBox(new Text(""), width: 0f, height: 0f)),
                    new IconButton(LumaIcons.Bell, ShowToast, tooltip: "Show toast"),
                    new IconButton(LumaIcons.MoreVertical, ShowDialog, tooltip: "Open dialog")
                }),
            body: new NavigatorHost(_navigator),
            navigationBar: new NavigationBar(
                SelectedDestination,
                destinations,
                SelectDestination),
            navigationRail: new NavigationRail(
                SelectedDestination,
                destinations,
                SelectDestination));
    }

    public Widget BuildHome(BuildContext context)
    {
        var theme = context.Theme ?? throw new InvalidOperationException("Gallery home requires a Theme.");
        return new ScrollView(
            new Center(
                new ConstrainedBox(
                    constraints: new BoxConstraints(maxWidth: 980f),
                    child: new Column(
                        gap: theme.Spacing.ExtraLarge,
                        children: new Widget[]
                        {
                            new GalleryHero(this),
                            new GalleryButtonSection(this),
                            new Form(
                                _profileForm,
                                new FocusTraversalGroup(new GalleryInputSection(this)),
                                onSubmit: SubmitProfile),
                            new GalleryLayoutSection(this),
                            new GalleryInteropSection(this)
                        }))),
            padding: EdgeInsets.All(theme.Spacing.Large));
    }

    internal void SelectDestination(int index)
    {
        if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));

        _navigator.PopToRoot();
        if (_activeDestination == index) return;

        _activeDestination = index;
        SelectedDestination.Value = index;
        _navigator.Replace(index switch
        {
            0 => new GalleryHomeRoute(this),
            1 => new GalleryLibraryRoute(this),
            _ => new GalleryProfileRoute(this)
        });
    }

    public void ShowToast() => _overlay.ShowToast(
        new Toast(new Text("Toast delivered through OverlayHost.")),
        TimeSpan.FromSeconds(2));

    public void ShowDialog()
    {
        OverlayHandle? handle = null;
        handle = _overlay.ShowModal(new Dialog(
            new Text("OverlayHost is scoped"),
            new Text("This dialog is owned by the gallery overlay scope and cleans itself up when closed."),
            new Widget[] { new Button("Close", () => handle?.Close()) }));
    }

    public void ShowDeleteConfirmation()
    {
        _ = ShowDeleteConfirmationAsync();
    }

    private async Task ShowDeleteConfirmationAsync()
    {
        var accepted = await _overlay.ShowConfirm(
            new Text("This removes the current gallery activity. This action cannot be undone."),
            new Text("Delete activity?"),
            confirmText: "Delete",
            confirmVariant: ButtonVariant.Destructive,
            onConfirm: cancellationToken => Task.Delay(650, cancellationToken));
        if (accepted)
        {
            _overlay.ShowToast(new Toast(new Text("Activity deleted.")), TimeSpan.FromSeconds(2));
        }
    }

    public void SubmitProfile()
    {
        _ = _profileSubmit.Run();
    }

    private sealed class GalleryHomeRoute : StatelessWidget
    {
        private readonly LumaFlowGalleryApp _app;

        public GalleryHomeRoute(LumaFlowGalleryApp app) => _app = app;

        public override Widget Build(BuildContext context) => _app.BuildHome(context);
    }
}
