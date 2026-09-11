using Assets.Application.UIKit.Media;
using Assets.Application.UIKit.Tokens;
using LumaFlow;

namespace Assets.Application.Features.Navigation.Widgets {
    public sealed class CurrentProjectCard : StatelessWidget {
        public override Widget Build(BuildContext context) {
            return new Card(
                child: new Row(
                    gap: AppSpacing.Sm,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children: new Widget[] {
                        new SizedBox(
                            AppMedia.ProjectThumbnail("greenfield_tower", 32f, "Greenfield Tower"),
                            width: 32f,
                            height: 32f
                        ),
                        new Expanded(new Column(
                            gap: AppSpacing.Xxs,
                            children: new Widget[] {
                                new Text("Current Project", style: new TextStyle(color: AppColors.TextMuted, fontSize: 10f)),
                                new Text(
                                    "Greenfield Tower",
                                    style: new TextStyle(
                                        color: AppColors.TextPrimary,
                                        fontSize: AppTypography.BodySmall,
                                        fontStyle: UnityEngine.FontStyle.Bold
                                    ),
                                    softWrap: false,
                                    overflow: TextOverflow.Ellipsis
                                )
                            }
                        )),
                        new Icon(LumaIcons.ChevronDown, size: AppSizes.IconSm, color: AppColors.TextSecondary)
                    }
                ),
                padding: EdgeInsets.All(AppSpacing.Xs),
                backgroundColor: AppColors.Surface,
                borderRadius: BorderRadius.All(AppRadius.Md),
                border: Border.All(AppColors.Border)
            );
        }
    }
}
