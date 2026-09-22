#nullable enable

using UnityEngine;

namespace LumaFlow {

    /// <summary>
    /// Defines optional visual decoration for a single container.
    /// </summary>
    public readonly struct BoxDecoration {
        public BoxDecoration(
            Color? backgroundColor = null,
            BorderRadius? borderRadius = null,
            Border? border = null) {
            BackgroundColor = backgroundColor;
            BackgroundGradient = null;
            BorderRadius = borderRadius;
            Border = border;
        }

        public BoxDecoration(
            LinearGradient backgroundGradient,
            Color? backgroundColor = null,
            BorderRadius? borderRadius = null,
            Border? border = null) {
            BackgroundColor = backgroundColor;
            BackgroundGradient = backgroundGradient;
            BorderRadius = borderRadius;
            Border = border;
        }

        /// <summary>
        /// Gets the optional background color.
        /// </summary>
        public Color? BackgroundColor { get; }

        /// <summary>Gets the optional linear background gradient.</summary>
        public LinearGradient? BackgroundGradient { get; }

        /// <summary>
        /// Gets the optional corner radii.
        /// </summary>
        public BorderRadius? BorderRadius { get; }

        /// <summary>
        /// Gets the optional border.
        /// </summary>
        public Border? Border { get; }
    }

}
