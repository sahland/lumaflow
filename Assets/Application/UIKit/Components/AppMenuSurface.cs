#nullable enable

using System;
using System.Collections.Generic;
using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Components {
    /// <summary>Fixed-width menu surface intended for an anchored LumaFlow popover.</summary>
    public sealed class AppMenuSurface : StatelessWidget {
        private readonly IReadOnlyList<AppMenuAction> _actions;

        public AppMenuSurface(IReadOnlyList<AppMenuAction> actions) {
            if (actions is null || actions.Count == 0) throw new ArgumentException("Menu requires at least one action.", nameof(actions));
            var copy = new AppMenuAction[actions.Count];
            for (var index = 0; index < actions.Count; index++) copy[index] = actions[index] ?? throw new ArgumentException("Menu actions cannot contain null.", nameof(actions));
            _actions = copy;
        }

        public override Widget Build(BuildContext context) {
            var children = new Widget[_actions.Count];
            for (var index = 0; index < _actions.Count; index++) children[index] = BuildAction(_actions[index]);
            return new SizedBox(
                new Card(
                    new Column(children, gap: AppSpacing.Xxs),
                    padding: EdgeInsets.All(AppSpacing.Xs),
                    backgroundColor: AppColors.Surface,
                    borderRadius: BorderRadius.All(AppRadius.Md),
                    border: Border.All(AppColors.Border)),
                width: 168f);
        }

        private static Widget BuildAction(AppMenuAction action) {
            return new Button(
                action.Label,
                action.OnPressed,
                variant: action.Destructive ? ButtonVariant.Destructive : ButtonVariant.Secondary,
                style: new ButtonStyle(
                    background: AppColors.Transparent,
                    foreground: action.Destructive ? AppColors.Danger : AppColors.TextPrimary,
                    padding: EdgeInsets.Symmetric(horizontal: AppSpacing.Sm, vertical: AppSpacing.Xs),
                    shape: BorderRadius.All(AppRadius.Sm),
                    typography: new TextStyle(fontSize: AppTypography.BodySmall),
                    minimumSize: new Vector2(0f, AppSizes.ControlHeightSm),
                    hovered: new ButtonStateStyle(background: action.Destructive ? AppColors.DangerSoft : AppColors.SurfaceHover)));
        }
    }

    public sealed class AppMenuAction {
        public AppMenuAction(string label, Action onPressed, bool destructive = false) {
            Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Label is required.", nameof(label)) : label;
            OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
            Destructive = destructive;
        }

        public string Label { get; }
        public Action OnPressed { get; }
        public bool Destructive { get; }
    }
}
