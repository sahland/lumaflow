#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Attaches a native UI Toolkit text tooltip to an element.</summary>
    public sealed class Tooltip {
        public Tooltip(string text) {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Tooltip text cannot be empty.", nameof(text));
            Text = text;
        }

        public string Text { get; }

        /// <summary>Shows a themed visual tooltip in the supplied overlay scope.</summary>
        public OverlayHandle Show(
            OverlayController controller,
            VisualElement anchor,
            PopoverPlacement placement = PopoverPlacement.Top) {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            return controller.ShowPopover(
                anchor,
                new Card(
                    new Text(
                        Text,
                        new TextStyle(color: Color.white, fontSize: 12f),
                        softWrap: false,
                        overflow: global::LumaFlow.TextOverflow.Clip),
                    padding: EdgeInsets.Symmetric(horizontal: 10f, vertical: 6f),
                    backgroundColor: new Color(0.10f, 0.12f, 0.16f, 1f),
                    borderRadius: BorderRadius.All(6f)),
                placement);
        }

        /// <summary>Applies this tooltip to <paramref name="anchor" /> and restores its previous value when disposed.</summary>
        public IDisposable AttachTo(VisualElement anchor) {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            var previousTooltip = anchor.tooltip;
            anchor.tooltip = Text;
            return new RestoreHandle(anchor, previousTooltip, Text);
        }

        private sealed class RestoreHandle : IDisposable {
            private VisualElement? _anchor;
            private readonly string? _previousTooltip;
            private readonly string _appliedTooltip;

            public RestoreHandle(VisualElement anchor, string? previousTooltip, string appliedTooltip) {
                _anchor = anchor;
                _previousTooltip = previousTooltip;
                _appliedTooltip = appliedTooltip;
            }

            public void Dispose() {
                var anchor = _anchor;
                _anchor = null;
                if (anchor != null && anchor.tooltip == _appliedTooltip) anchor.tooltip = _previousTooltip;
            }
        }
    }

}
