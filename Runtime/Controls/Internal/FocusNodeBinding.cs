#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {
    /// <summary>
    /// Mount-local bridge between an external <see cref="FocusNode"/> and one native
    /// focusable element.
    /// </summary>
    internal sealed class FocusNodeBinding : IDisposable {
        private FocusNode? _focusNode;
        private VisualElement? _element;
        private IDisposable? _attachment;
        private IDisposable? _traversalRegistration;

        private FocusNodeBinding(
            FocusNode focusNode,
            VisualElement element,
            FocusTraversalController? focusTraversal,
            bool replaceExistingAttachment) {
            _focusNode = focusNode;
            _element = element;
            _attachment = focusNode.Attach(
                element.Focus,
                () => element.enabledInHierarchy,
                replaceExistingAttachment);
            _traversalRegistration = focusTraversal?.Register(focusNode);
            element.RegisterCallback<FocusInEvent>(HandleFocusIn);
            element.RegisterCallback<FocusOutEvent>(HandleFocusOut);
        }

        public static FocusNodeBinding Attach(
            FocusNode focusNode,
            VisualElement element,
            FocusTraversalController? focusTraversal = null,
            bool replaceExistingAttachment = false) {
            if (focusNode is null) throw new ArgumentNullException(nameof(focusNode));
            if (element is null) throw new ArgumentNullException(nameof(element));
            return new FocusNodeBinding(focusNode, element, focusTraversal, replaceExistingAttachment);
        }

        public void Dispose() {
            var element = _element;
            if (element is not null) {
                element.UnregisterCallback<FocusInEvent>(HandleFocusIn);
                element.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
            }

            _traversalRegistration?.Dispose();
            _traversalRegistration = null;
            _attachment?.Dispose();
            _attachment = null;
            _element = null;
            _focusNode = null;
        }

        private void HandleFocusIn(FocusInEvent _) {
            _focusNode?.SetFocused(true);
        }

        private void HandleFocusOut(FocusOutEvent _) {
            _focusNode?.SetFocused(false);
        }
    }
}
