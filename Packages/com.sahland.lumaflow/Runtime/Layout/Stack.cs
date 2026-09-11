#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>
    /// Hosts multiple children in a shared native positioning context.
    /// </summary>
    public sealed class Stack : Widget {
        private readonly Widget[] _children;

        public Stack(IReadOnlyList<Widget> children) {
            if (children is null) {
                throw new ArgumentNullException(nameof(children));
            }

            _children = new Widget[children.Count];
            for (var index = 0; index < children.Count; index++) {
                _children[index] = children[index] ?? throw new ArgumentException(
                    "Stack children cannot contain null.",
                    nameof(children));
            }
        }

        public IReadOnlyList<Widget> Children => _children;

        internal override WidgetNode CreateNode() {
            return new StackNode(this);
        }
    }

}
