#nullable enable

using System;

namespace LumaFlow {
    /// <summary>
    /// An externally owned controller for one mounted focusable widget.
    /// </summary>
    public sealed class FocusNode {
        private Action? _requestFocus;
        private Func<bool>? _canRequestFocus;
        private Attachment? _attachment;

        /// <summary>
        /// Reflects whether the attached native control currently owns focus.
        /// </summary>
        public State<bool> IsFocused { get; } = new(false);

        /// <summary>
        /// Requests focus for the currently mounted control.
        /// Returns false when this node is not attached or its control is unavailable.
        /// </summary>
        public bool RequestFocus() {
            var requestFocus = _requestFocus;
            if (requestFocus is null || (_canRequestFocus is { } canRequestFocus && !canRequestFocus())) {
                return false;
            }

            requestFocus();
            return true;
        }

        internal IDisposable Attach(
            Action requestFocus,
            Func<bool>? canRequestFocus = null,
            bool replaceExistingAttachment = false) {
            if (requestFocus is null) throw new ArgumentNullException(nameof(requestFocus));
            if (_attachment is not null && !replaceExistingAttachment) {
                throw new InvalidOperationException(
                    "A FocusNode can be attached to only one mounted control at a time.");
            }

            _attachment?.Dispose();
            _requestFocus = requestFocus;
            _canRequestFocus = canRequestFocus;
            var attachment = new Attachment(this);
            _attachment = attachment;
            return attachment;
        }

        internal void SetFocused(bool isFocused) {
            IsFocused.Value = isFocused;
        }

        private sealed class Attachment : IDisposable {
            private FocusNode? _owner;
            public Attachment(FocusNode owner) {
                _owner = owner;
            }

            public void Dispose() {
                if (_owner?._attachment == this) {
                    _owner._requestFocus = null;
                    _owner._canRequestFocus = null;
                    _owner._attachment = null;
                    _owner.IsFocused.Value = false;
                }

                _owner = null;
            }
        }
    }
}
