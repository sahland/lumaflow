#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Attaches a semantic context menu to a native UI Toolkit element.</summary>
    public sealed class ContextMenu {
        private readonly IReadOnlyList<ContextMenuItem> _items;

        public ContextMenu(IReadOnlyList<ContextMenuItem> items) {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new ArgumentException("A context menu must contain at least one item.", nameof(items));

            var copy = new List<ContextMenuItem>(items.Count);
            foreach (var item in items) {
                if (item == null) throw new ArgumentException("Context menu items cannot contain null.", nameof(items));
                copy.Add(item);
            }

            _items = copy.AsReadOnly();
        }

        /// <summary>Attaches this menu to <paramref name="anchor" /> and returns a handle that detaches it.</summary>
        public IDisposable AttachTo(VisualElement anchor) {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));

            var manipulator = new ContextualMenuManipulator(BuildMenu);
            anchor.AddManipulator(manipulator);
            return new DetachHandle(anchor, manipulator);
        }

        private void BuildMenu(ContextualMenuPopulateEvent populateEvent) {
            foreach (var item in _items) {
                populateEvent.menu.AppendAction(
                    item.Label,
                    _ => item.OnSelected(),
                    _ => item.Enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        private sealed class DetachHandle : IDisposable {
            private VisualElement? _anchor;
            private ContextualMenuManipulator? _manipulator;

            public DetachHandle(VisualElement anchor, ContextualMenuManipulator manipulator) {
                _anchor = anchor;
                _manipulator = manipulator;
            }

            public void Dispose() {
                var anchor = _anchor;
                var manipulator = _manipulator;
                _anchor = null;
                _manipulator = null;
                if (anchor != null && manipulator != null) anchor.RemoveManipulator(manipulator);
            }
        }
    }

}
