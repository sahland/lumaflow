#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Scales logical font sizes while preserving widget-owned typography.</summary>
    public readonly struct TextScaler : IEquatable<TextScaler> {
        private readonly float _scaleFactor;

        public TextScaler(float scaleFactor) {
            if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor) || scaleFactor <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Text scale must be finite and greater than zero.");
            }
            _scaleFactor = scaleFactor;
        }

        public static TextScaler NoScaling => new(1f);
        public float ScaleFactor => _scaleFactor == 0f ? 1f : _scaleFactor;

        public float Scale(float fontSize) {
            if (float.IsNaN(fontSize) || float.IsInfinity(fontSize) || fontSize <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be finite and greater than zero.");
            }
            return fontSize * ScaleFactor;
        }

        internal TextScaler OrDefault() => _scaleFactor == 0f ? NoScaling : this;
        public bool Equals(TextScaler other) => ScaleFactor.Equals(other.ScaleFactor);
        public override bool Equals(object? obj) => obj is TextScaler other && Equals(other);
        public override int GetHashCode() => ScaleFactor.GetHashCode();
        public static bool operator ==(TextScaler left, TextScaler right) => left.Equals(right);
        public static bool operator !=(TextScaler left, TextScaler right) => !left.Equals(right);
    }

    /// <summary>Overrides scalable-text policy for one subtree.</summary>
    public sealed class TextScale : Widget {
        public TextScale(TextScaler scaler, Widget child) {
            Scaler = scaler.OrDefault();
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public TextScaler Scaler { get; }
        public Widget Child { get; }
        internal override WidgetNode CreateNode() => new TextScaleNode(this);
    }

    internal sealed class TextScaleNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;

        internal TextScaleNode(TextScale widget) : base(widget) { }
        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (TextScale)Widget;
            _childContext = Context.WithTextScaler(widget.Scaler);
            _currentChild = MountChild(widget.Child, NativeParent, _childContext);
            AdoptNativeElement(_currentChild.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not TextScale textScale) return false;
            var previous = (TextScale)Widget;
            if (previous.Scaler != textScale.Scaler) _childContext!.UpdateTextScaler(textScale.Scaler);
            ReconcileSingleChild(ref _currentChild, textScale.Child, NativeParent, _childContext!);
            UpdateWidget(textScale);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }
    }
}
