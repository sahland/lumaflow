#nullable enable

using System;

namespace LumaFlow {

    /// <summary>
    /// Constrains one child widget to optional pixel-like width and height values.
    /// </summary>
    public sealed class SizedBox : Widget {
        public SizedBox(Widget child, float? width = null, float? height = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Validate(width, nameof(width));
            Validate(height, nameof(height));
            Width = width;
            Height = height;
        }

        private SizedBox(Widget child, bool expandWidth, bool expandHeight) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            ExpandsWidth = expandWidth;
            ExpandsHeight = expandHeight;
        }

        /// <summary>Creates a box with equal finite width and height.</summary>
        public static SizedBox Square(Widget child, float dimension) =>
            new(child, dimension, dimension);

        /// <summary>Fills both axes offered by the native parent.</summary>
        public static SizedBox Expand(Widget child) => new(child, expandWidth: true, expandHeight: true);

        /// <summary>Fills the offered width while retaining intrinsic height.</summary>
        public static SizedBox ExpandWidth(Widget child) => new(child, expandWidth: true, expandHeight: false);

        /// <summary>Fills the offered height while retaining intrinsic width.</summary>
        public static SizedBox ExpandHeight(Widget child) => new(child, expandWidth: false, expandHeight: true);

        /// <summary>Creates a zero-sized box around the child.</summary>
        public static SizedBox Shrink(Widget child) => new(child, width: 0f, height: 0f);

        public Widget Child { get; }

        public float? Width { get; }

        public float? Height { get; }

        public bool ExpandsWidth { get; }

        public bool ExpandsHeight { get; }

        internal override WidgetNode CreateNode() {
            return new SizedBoxNode(this);
        }

        private static void Validate(float? value, string parameterName) {
            if (value is { } size && (float.IsNaN(size) || float.IsInfinity(size) || size < 0f)) {
                throw new ArgumentOutOfRangeException(parameterName, "Size must be finite and non-negative.");
            }
        }
    }

}
