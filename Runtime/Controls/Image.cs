#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    public enum ImageFit {
        Fill,
        Contain,
        Cover
    }

    /// <summary>Displays an application-owned Unity texture, sprite, or vector image.</summary>
    public sealed class Image : Widget {
        public Image(
            Texture texture,
            float? width = null,
            float? height = null,
            ImageFit fit = ImageFit.Contain,
            Color? tint = null,
            string? semanticsLabel = null,
            BorderRadius? borderRadius = null) {
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Initialize(width, height, fit, tint, semanticsLabel, borderRadius);
        }

        public Image(
            Sprite sprite,
            float? width = null,
            float? height = null,
            ImageFit fit = ImageFit.Contain,
            Color? tint = null,
            string? semanticsLabel = null,
            BorderRadius? borderRadius = null) {
            Sprite = sprite ?? throw new ArgumentNullException(nameof(sprite));
            Initialize(width, height, fit, tint, semanticsLabel, borderRadius);
        }

        public Image(
            VectorImage vectorImage,
            float? width = null,
            float? height = null,
            ImageFit fit = ImageFit.Contain,
            Color? tint = null,
            string? semanticsLabel = null,
            BorderRadius? borderRadius = null) {
            VectorImage = vectorImage ?? throw new ArgumentNullException(nameof(vectorImage));
            Initialize(width, height, fit, tint, semanticsLabel, borderRadius);
        }

        public Texture? Texture { get; private set; }
        public Sprite? Sprite { get; private set; }
        public VectorImage? VectorImage { get; private set; }
        public float? Width { get; private set; }
        public float? Height { get; private set; }
        public ImageFit Fit { get; private set; }
        public Color? Tint { get; private set; }
        public string? SemanticsLabel { get; private set; }
        /// <summary>Optional clipping radius applied to the rendered image bounds.</summary>
        public BorderRadius? BorderRadius { get; private set; }

        internal override WidgetNode CreateNode() => new ImageNode(this);

        private void Initialize(
            float? width,
            float? height,
            ImageFit fit,
            Color? tint,
            string? semanticsLabel,
            BorderRadius? borderRadius) {
            ValidateSize(width, nameof(width));
            ValidateSize(height, nameof(height));
            if (!Enum.IsDefined(typeof(ImageFit), fit)) throw new ArgumentOutOfRangeException(nameof(fit));
            Width = width;
            Height = height;
            Fit = fit;
            Tint = tint;
            SemanticsLabel = semanticsLabel;
            BorderRadius = borderRadius;
        }

        private static void ValidateSize(float? value, string parameterName) {
            if (value is { } size && (!float.IsFinite(size) || size < 0f)) {
                throw new ArgumentOutOfRangeException(parameterName, "Image size must be finite and non-negative.");
            }
        }
    }

    internal sealed class ImageNode : WidgetNode {
        public ImageNode(Image widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var image = new UnityEngine.UIElements.Image { focusable = false, pickingMode = PickingMode.Ignore };
            Apply(image, (Image)Widget);
            return image;
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Image image) return false;
            UpdateWidget(image);
            Apply((UnityEngine.UIElements.Image)Element, image);
            RefreshSemantics();
            return true;
        }

        protected override SemanticsProperties DescribeSemantics() {
            var widget = (Image)Widget;
            return new SemanticsProperties(
                label: widget.SemanticsLabel,
                role: SemanticsRole.Image,
                hidden: string.IsNullOrWhiteSpace(widget.SemanticsLabel));
        }

        private static void Apply(UnityEngine.UIElements.Image element, Image widget) {
            element.image = null;
            element.sprite = null;
            element.vectorImage = null;
            if (widget.Texture is not null) element.image = widget.Texture;
            else if (widget.Sprite is not null) element.sprite = widget.Sprite;
            else element.vectorImage = widget.VectorImage;
            element.scaleMode = widget.Fit switch {
                ImageFit.Fill => ScaleMode.StretchToFill,
                ImageFit.Contain => ScaleMode.ScaleToFit,
                ImageFit.Cover => ScaleMode.ScaleAndCrop,
                _ => throw new ArgumentOutOfRangeException(nameof(widget.Fit))
            };
            element.tintColor = widget.Tint ?? Color.white;
            element.style.width = widget.Width is { } width ? width : StyleKeyword.Null;
            element.style.height = widget.Height is { } height ? height : StyleKeyword.Null;
            element.style.flexShrink = 0f;
            element.style.overflow = StyleKeyword.Null;
            if (widget.BorderRadius is not null) element.style.overflow = Overflow.Hidden;
            element.style.borderTopLeftRadius = StyleKeyword.Null;
            element.style.borderTopRightRadius = StyleKeyword.Null;
            element.style.borderBottomRightRadius = StyleKeyword.Null;
            element.style.borderBottomLeftRadius = StyleKeyword.Null;
            if (widget.BorderRadius is not { } radius) return;
            element.style.borderTopLeftRadius = radius.TopLeft;
            element.style.borderTopRightRadius = radius.TopRight;
            element.style.borderBottomRightRadius = radius.BottomRight;
            element.style.borderBottomLeftRadius = radius.BottomLeft;
        }
    }
}
