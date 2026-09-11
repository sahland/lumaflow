using Assets.Application.UIKit.Media;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.Features.Navigation.Widgets {
    public sealed class SidebarProfile : StatelessWidget {
        public override Widget Build(BuildContext context) {
            return new Padding(
                padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Xs, vertical: AppSpacing.Sm),
                child: new Row(
                    gap: AppSpacing.Sm,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children: new Widget[] {
                        new CircleAvatar(
                            backgroundImage: new Image(AppMedia.Avatar, fit: ImageFit.Cover),
                            radius: AppSizes.AvatarSm * 0.5f,
                            semanticsLabel: "Alex Johnson"
                        ),
                        new Expanded(new Column(
                            gap: AppSpacing.Xxs,
                            children: new Widget[] {
                                new Text(
                                    "Alex Johnson",
                                    style: new TextStyle(
                                        color: AppColors.TextPrimary,
                                        fontSize: AppTypography.BodySmall,
                                        fontStyle: FontStyle.Bold
                                    ),
                                    softWrap: false,
                                    overflow: TextOverflow.Ellipsis
                                ),
                                new Text(
                                    "alex@example.com",
                                    style: new TextStyle(color: AppColors.TextMuted, fontSize: 11f),
                                    softWrap: false,
                                    overflow: TextOverflow.Ellipsis
                                )
                            }
                        )),
                        new IconButton(
                            LumaIcons.MoreVertical,
                            onPressed: () => Debug.Log("Profile menu"),
                            hitSize: AppSizes.IconButtonSm,
                            tooltip: "Profile actions"
                        )
                    }
                )
            );
        }
    }
}
