#nullable enable

using System;
using UnityEngine.UIElements;
using NativeTextOverflow = UnityEngine.UIElements.TextOverflow;

namespace LumaFlow {

    internal sealed class TextNode : WidgetNode {
        private Label? _label;
        private IDisposable? _valueSubscription;
        public TextNode(Text widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            var widget = (Text)Widget;
            _label = new Label(widget.Value);
            // Text is content that participates in its parent's available space.
            // Without these defaults a native Label keeps its intrinsic width and
            // forces responsive flex layouts to overflow instead of wrapping.
            _label.style.flexShrink = 1f;
            _label.style.minWidth = 0f;
            ApplyTextStyle(widget);
            return _label;
        }

        protected override void OnMounted() {
            BindValueState(((Text)Widget).ValueState);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not Text next || !CanUpdateWith(next)) return false;
            var previous = (Text)Widget;
            if (!ReferenceEquals(previous.ValueState, next.ValueState)) {
                _valueSubscription?.Dispose();
                _valueSubscription = null;
                BindValueState(next.ValueState);
            }

            _label!.text = next.ValueState?.Value ?? next.Value;
            ReevaluateInheritedDependencies(() =>
            {
                ApplyTextStyle(next);
                return true;
            });
            UpdateWidget(next);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => ApplyTextStyle((Text)Widget);

        protected override SemanticsProperties DescribeSemantics() => new(
            label: ((Text)Widget).ValueState?.Value ?? ((Text)Widget).Value,
            role: SemanticsRole.StaticText);

        private void UpdateText(string value) {
            _label!.text = value;
            RefreshSemantics();
        }

        private void BindValueState(State<string>? state) {
            if (state is null) return;
            _valueSubscription = state.Subscribe(UpdateText);
            Bindings.Add(_valueSubscription);
        }

        private void ApplyTextStyle(Text widget) {
            _label!.style.color = StyleKeyword.Null;
            _label.style.fontSize = StyleKeyword.Null;
            _label.style.unityFontStyleAndWeight = StyleKeyword.Null;
            _label.style.maxHeight = StyleKeyword.Null;
            _label.style.whiteSpace = widget.SoftWrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            _label.style.textOverflow = widget.Overflow == TextOverflow.Ellipsis
                ? NativeTextOverflow.Ellipsis
                : NativeTextOverflow.Clip;
            _label.style.overflow = widget.Overflow == TextOverflow.Visible
                ? Overflow.Visible
                : Overflow.Hidden;
            var style = widget.Style ?? Context.Theme?.Typography.Body;
            if (style is not null) TextStyleMapper.Apply(_label, style, Context.TextScaler);
            if (widget.MaxLines is { } maxLines) {
                var lineHeight = ResolveLineHeight(style);
                _label.style.maxHeight = lineHeight * maxLines;
            }
        }

        private float ResolveLineHeight(TextStyle? style) {
            var fontSize = style?.FontSize is { } size
                ? Context.TextScaler.OrDefault().Scale(size)
                : 14f;
            // UI Toolkit does not expose its font-metric line height publicly.
            // The 1.2 em fallback matches its default label leading closely and,
            // unlike a fixed pixel cap, remains correct under TextScaler.
            return fontSize * 1.2f;
        }
    }

}
