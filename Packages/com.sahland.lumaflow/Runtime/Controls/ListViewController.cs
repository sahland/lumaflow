#nullable enable

using System;

namespace LumaFlow {
    /// <summary>
    /// An externally owned controller that restores and changes one virtualized list's scroll position.
    /// Retain the controller across remounts to retain its offset.
    /// </summary>
    public sealed class ListViewController {
        private Attachment? _attachment;
        private Func<WidgetKey, bool>? _scrollToItem;
        private readonly State<float> _offset;

        public ListViewController(float initialOffset = 0f) {
            _offset = new State<float>(ValidateOffset(initialOffset));
        }

        /// <summary>Gets the latest logical scroll offset.</summary>
        public float Offset => _offset.Value;

        /// <summary>Moves the attached list to a non-negative logical offset.</summary>
        public void JumpTo(float offset) => _offset.Value = ValidateOffset(offset);

        /// <summary>Scrolls a keyed item into view. Returns false if detached or the key is absent.</summary>
        public bool ScrollTo(WidgetKey key) {
            if (!key.IsValid) throw new ArgumentException("A valid item key is required.", nameof(key));
            return _scrollToItem?.Invoke(key) ?? false;
        }

        internal IDisposable Attach(Func<WidgetKey, bool> scrollToItem) {
            if (scrollToItem is null) throw new ArgumentNullException(nameof(scrollToItem));
            if (_attachment is not null) {
                throw new InvalidOperationException(
                    "A ListViewController can be attached to only one mounted list at a time.");
            }

            _scrollToItem = scrollToItem;
            _attachment = new Attachment(this);
            return _attachment;
        }

        internal IDisposable SubscribeOffset(Action<float> listener) => _offset.Subscribe(listener);

        internal void SetOffsetFromNative(float offset) => _offset.Value = ValidateOffset(offset);

        private static float ValidateOffset(float offset) =>
            float.IsNaN(offset) || float.IsInfinity(offset) || offset < 0f
                ? throw new ArgumentOutOfRangeException(nameof(offset), "Scroll offset must be finite and non-negative.")
                : offset;

        private sealed class Attachment : IDisposable {
            private ListViewController? _owner;

            public Attachment(ListViewController owner) => _owner = owner;

            public void Dispose() {
                if (_owner?._attachment == this) {
                    _owner._attachment = null;
                    _owner._scrollToItem = null;
                }
                _owner = null;
            }
        }
    }
}
