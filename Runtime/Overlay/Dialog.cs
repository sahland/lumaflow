#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>A temporary surface intended to be presented through <see cref="OverlayController.ShowModal"/>.</summary>
    public sealed class Dialog : Widget {
        public Dialog(Widget content, Widget? title = null, IReadOnlyList<Widget>? actions = null) {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Title = title;
            Actions = actions is null ? Array.Empty<Widget>() : CopyActions(actions);
        }

        public Widget Content { get; }
        public Widget? Title { get; }
        public IReadOnlyList<Widget> Actions { get; }
        internal override WidgetNode CreateNode() => new DialogNode(this);

        private static IReadOnlyList<Widget> CopyActions(IReadOnlyList<Widget> actions) {
            var copy = new List<Widget>(actions.Count);
            foreach (var action in actions) {
                if (action is null) throw new ArgumentException("Dialog actions cannot contain null.", nameof(actions));
                copy.Add(action);
            }
            return copy.AsReadOnly();
        }
    }

}
