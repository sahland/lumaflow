#nullable enable

using Assets.Application.UIKit.Media;
using Assets.Application.UIKit.Components;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Dashboard.Widgets {
    public sealed class DashboardHeader : StatefulWidget<DashboardHeaderState> {
        internal OverlayController Overlays { get; }
        public DashboardHeader(OverlayController overlays) => Overlays = overlays ?? throw new System.ArgumentNullException(nameof(overlays));
    }

    public sealed class DashboardHeaderState : WidgetState {
        private readonly State<string> _search = new(string.Empty);
        private DashboardHeader Configuration => (DashboardHeader)Widget;

        public override Widget Build(BuildContext context) {
            return new LayoutBuilder((_, constraints) =>
                constraints.MaxWidth < AppBreakpoints.DashboardWide
                    ? new Column(
                        gap: AppSpacing.Md,
                        children: new Widget[] { BuildTitle(), BuildActions(expandSearch: true) }
                    )
                    : new Row(
                        gap: AppSpacing.Xl,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        children: new Widget[] {
                            new Expanded(BuildTitle()),
                            BuildActions(expandSearch: false)
                        }
                    ));
        }

        private static Widget BuildTitle() {
            return new Column(
                gap: AppSpacing.Xxs,
                children: new Widget[] {
                    new Text(
                        "Welcome back, Alex! 👋",
                        style: new TextStyle(
                            color: AppColors.TextPrimary,
                            fontSize: AppTypography.Heading2,
                            fontStyle: FontStyle.Bold
                        )
                    ),
                    new Text(
                        "Here's what's happening with your projects today.",
                        style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall)
                    )
                }
            );
        }

        private Widget BuildActions(bool expandSearch) {
            var search = new SizedBox(
                new Card(
                    child: new Row(
                        gap: AppSpacing.Xs,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        children: new Widget[] {
                            new Icon(LumaIcons.Search, size: AppSizes.IconSm, color: AppColors.TextMuted),
                            new Expanded(new TextField(
                                _search,
                                placeholder: "Search...",
                                style: new TextFieldStyle(
                                    background: AppColors.Transparent,
                                    border: AppColors.Transparent,
                                    padding: EdgeInsets.All(0f),
                                    shape: BorderRadius.All(0f)
                                )
                            )),
                            new Card(
                                child: new Text(
                                    "⌘ K",
                                    style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption)
                                ),
                                padding: EdgeInsets.Symmetric(horizontal: 6f, vertical: 2f),
                                backgroundColor: AppColors.Background,
                                borderRadius: BorderRadius.All(AppRadius.Xs)
                            )
                        }
                    ),
                    padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: 4f),
                    backgroundColor: AppColors.Surface,
                    borderRadius: BorderRadius.All(AppRadius.Sm),
                    border: Border.All(AppColors.Border)
                ),
                width: expandSearch ? null : AppSizes.HeaderSearchWidth,
                height: AppSizes.ControlHeightMd
            );

            return new Row(
                gap: AppSpacing.Xs,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: new Widget[] {
                    expandSearch ? new Expanded(search) : search,
                    new IconButton(LumaIcons.Bell, () => Debug.Log("Notifications"), tooltip: "Notifications", hitSize: AppSizes.IconButtonMd),
                    new Pressable(
                        new CircleAvatar(
                            backgroundImage: new Image(AppMedia.Avatar, fit: ImageFit.Cover),
                            radius: AppSizes.AvatarMd * 0.5f,
                            semanticsLabel: "Alex Johnson"),
                        ShowProfile,
                        semanticsLabel: "Open Alex Johnson profile")
                }
            );
        }

        private void ShowProfile() {
            OverlayHandle? drawer = null;
            drawer = Configuration.Overlays.ShowDrawer(
                new AppDrawerSurface("Profile", new Column(gap: AppSpacing.Md, children: new Widget[] {
                    new Center(new CircleAvatar(backgroundImage: new Image(AppMedia.Avatar, fit: ImageFit.Cover), radius: AppSizes.AvatarLg * 0.5f, semanticsLabel: "Alex Johnson")),
                    new Center(new Text("Alex Johnson", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.Heading3, fontStyle: FontStyle.Bold))),
                    new Center(new Text("Product owner · alex@example.com", style: new TextStyle(color: AppColors.TextSecondary, fontSize: AppTypography.BodySmall))),
                    new Card(new Column(gap: AppSpacing.Xs, children: new Widget[] {
                        new Text("Workspace", style: new TextStyle(color: AppColors.TextMuted, fontSize: AppTypography.Caption)),
                        new Text("LumaFlow UI Test App", style: new TextStyle(color: AppColors.TextPrimary, fontSize: AppTypography.BodySmall, fontStyle: FontStyle.Bold)),
                        new Text("12 active projects", style: new TextStyle(color: AppColors.Primary, fontSize: AppTypography.BodySmall))
                    }), padding: EdgeInsets.All(AppSpacing.Md), backgroundColor: AppColors.SurfaceSecondary, borderRadius: BorderRadius.All(AppRadius.Sm))
                }), () => drawer?.Close()),
                DrawerPlacement.Right);
        }
    }
}
