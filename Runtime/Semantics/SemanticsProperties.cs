#nullable enable

using System;
using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Immutable assistive-technology metadata and actions for one semantic node.
    /// </summary>
    public sealed class SemanticsProperties {
        public SemanticsProperties(
            string? label = null,
            string? value = null,
            string? hint = null,
            SemanticsRole role = SemanticsRole.None,
            bool? enabled = null,
            bool? checkedValue = null,
            bool? selected = null,
            bool? expanded = null,
            bool hidden = false,
            bool allowsDirectInteraction = false,
            Action? onTap = null,
            Action? onSelect = null,
            Action? onIncrease = null,
            Action? onDecrease = null,
            Func<bool>? onDismiss = null,
            Action<bool>? onAccessibilityFocusChanged = null,
            Func<Rect>? frameGetter = null) {
            if (!Enum.IsDefined(typeof(SemanticsRole), role)) {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            Label = label;
            Value = value;
            Hint = hint;
            Role = role;
            Enabled = enabled;
            Checked = checkedValue;
            Selected = selected;
            Expanded = expanded;
            Hidden = hidden;
            AllowsDirectInteraction = allowsDirectInteraction;
            OnTap = onTap;
            OnSelect = onSelect;
            OnIncrease = onIncrease;
            OnDecrease = onDecrease;
            OnDismiss = onDismiss;
            OnAccessibilityFocusChanged = onAccessibilityFocusChanged;
            FrameGetter = frameGetter;
        }

        public string? Label { get; }
        public string? Value { get; }
        public string? Hint { get; }
        public SemanticsRole Role { get; }
        public bool? Enabled { get; }
        public bool? Checked { get; }
        public bool? Selected { get; }
        public bool? Expanded { get; }
        public bool Hidden { get; }
        public bool AllowsDirectInteraction { get; }
        public Action? OnTap { get; }
        public Action? OnSelect { get; }
        public Action? OnIncrease { get; }
        public Action? OnDecrease { get; }
        public Func<bool>? OnDismiss { get; }
        public Action<bool>? OnAccessibilityFocusChanged { get; }

        /// <summary>
        /// Optional screen-space frame provider. The mounted UI Toolkit world bounds
        /// are used when this is null.
        /// </summary>
        public Func<Rect>? FrameGetter { get; }
    }
}
