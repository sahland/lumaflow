#nullable enable

using System;
using System.Runtime.ExceptionServices;

namespace LumaFlow {

    /// <summary>
    /// Commits one user-originated controlled-input value before notifying the
    /// configuration that produced the event. Both stages are best-effort so a
    /// failing state observer cannot silently suppress the component callback.
    /// </summary>
    internal static class ControlledInputChange {
        public static void Commit<T>(State<T> state, T value, Action<T>? onChanged) {
            Exception? stateFailure = null;
            Exception? callbackFailure = null;

            try {
                state.Value = value;
            } catch (Exception exception) {
                stateFailure = exception;
            }

            try {
                onChanged?.Invoke(value);
            } catch (Exception exception) {
                callbackFailure = exception;
            }

            if (stateFailure is not null && callbackFailure is not null) {
                throw new AggregateException(
                    "The controlled value was committed, but state observers and onChanged both failed.",
                    stateFailure,
                    callbackFailure);
            }

            if (stateFailure is not null) ExceptionDispatchInfo.Capture(stateFailure).Throw();
            if (callbackFailure is not null) ExceptionDispatchInfo.Capture(callbackFailure).Throw();
        }
    }

}
