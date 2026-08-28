#nullable enable

using System;

namespace LumaFlow {

    /// <summary>A semantic surface intended to be displayed through <see cref="OverlayController.ShowToast" />.</summary>
    public sealed class Toast : Widget {
        public Toast(Widget content) {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public Widget Content { get; }

        internal override WidgetNode CreateNode() => new ToastNode(this);
    }

}
