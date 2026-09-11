using UnityEngine;

namespace Assets.Application.UIKit.Tokens {
    public static class AppColors {
        #region Brand
        public static readonly Color32 Primary = new(38, 99, 235, 255);
        public static readonly Color32 PrimaryHover = new(29, 78, 216, 255);
        public static readonly Color32 PrimaryPressed = new(30, 64, 175, 255);
        public static readonly Color32 PrimarySoft = new(239, 246, 255, 255);
        #endregion

        #region Surfaces
        public static readonly Color32 Background = new(247, 248, 252, 255);
        public static readonly Color32 Surface = new(255, 255, 255, 255);
        public static readonly Color32 SurfaceSecondary = new(249, 250, 251, 255);
        public static readonly Color32 SurfaceHover = new(243, 244, 246, 255);
        #endregion

        #region Text
        public static readonly Color32 TextPrimary = new(17, 24, 39, 255);
        public static readonly Color32 TextSecondary = new(107, 114, 128, 255);
        public static readonly Color32 TextMuted = new(156, 163, 175, 255);
        public static readonly Color32 TextOnPrimary = new(255, 255, 255, 255);
        #endregion

        #region Borders
        public static readonly Color32 Border = new(229, 231, 235, 255);
        public static readonly Color32 BorderStrong = new(209, 213, 219, 255);
        #endregion

        #region Semantic
        public static readonly Color32 Success = new(22, 163, 74, 255);
        public static readonly Color32 SuccessSoft = new(240, 253, 244, 255);

        public static readonly Color32 Warning = new(217, 119, 6, 255);
        public static readonly Color32 WarningSoft = new(255, 251, 235, 255);

        public static readonly Color32 Danger = new(220, 38, 38, 255);
        public static readonly Color32 DangerSoft = new(254, 242, 242, 255);

        public static readonly Color32 Info = new(37, 99, 235, 255);
        public static readonly Color32 InfoSoft = new(239, 246, 255, 255);
        #endregion

        #region Misc
        public static readonly Color32 Transparent = new(0, 0, 0, 0);
        public static readonly Color32 White = new(255, 255, 255, 255);
        #endregion
    }
}
