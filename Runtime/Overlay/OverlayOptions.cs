#nullable enable

using UnityEngine;

namespace LumaFlow {
    /// <summary>Controls dismissal and focus behavior for a modal overlay.</summary>
    public sealed class ModalOptions {
        public ModalOptions(
            bool dismissOnBarrier = true,
            bool dismissOnBack = true,
            bool requestFocus = true,
            bool restoreFocus = true,
            Color? barrierColor = null) {
            DismissOnBarrier = dismissOnBarrier;
            DismissOnBack = dismissOnBack;
            RequestFocus = requestFocus;
            RestoreFocus = restoreFocus;
            BarrierColor = barrierColor ?? new Color(0f, 0f, 0f, 0.32f);
        }

        public bool DismissOnBarrier { get; }
        public bool DismissOnBack { get; }
        public bool RequestFocus { get; }
        public bool RestoreFocus { get; }
        public Color BarrierColor { get; }

        public static ModalOptions Default { get; } = new();
    }

    public enum DrawerPlacement {
        Left,
        Right
    }
}
