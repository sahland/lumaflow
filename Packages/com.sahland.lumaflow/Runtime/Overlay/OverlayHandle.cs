#nullable enable

using System;

namespace LumaFlow {

    /// <summary>Owns one temporary overlay entry.</summary>
    public sealed class OverlayHandle : IDisposable {
        private Action? _close;

        internal OverlayHandle(Action close) { _close = close; }

        public bool IsOpen => _close is not null;

        public void Close() {
            var close = _close;
            _close = null;
            close?.Invoke();
        }

        public void Dispose() { Close(); }

        internal void CloseFromHost() { _close = null; }
    }

}
