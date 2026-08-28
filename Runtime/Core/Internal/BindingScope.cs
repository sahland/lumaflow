#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    internal sealed class BindingScope : IDisposable {
        private readonly List<Action> _cleanupActions = new();
        private bool _isDisposed;

        public void Add(IDisposable disposable) {
            if (disposable is null) {
                throw new ArgumentNullException(nameof(disposable));
            }
            Add(disposable.Dispose);
        }

        public void Add(Action cleanup) {
            if (cleanup is null) {
                throw new ArgumentNullException(nameof(cleanup));
            }

            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BindingScope));
            }

            _cleanupActions.Add(cleanup);
        }

        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            List<Exception>? exceptions = null;

            for (var index = _cleanupActions.Count - 1; index >= 0; index--) {
                try {
                    _cleanupActions[index]();
                } catch (Exception exception) {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }

            _cleanupActions.Clear();

            if (exceptions is { Count: 1 }) {
                throw exceptions[0];
            }

            if (exceptions is { Count: > 1 }) {
                throw new AggregateException(exceptions);
            }
        }
    }

}
