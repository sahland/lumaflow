#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LumaFlow {

    /// <summary>Describes the observable lifecycle of an <see cref="AsyncAction" />.</summary>
    public enum AsyncActionStatus {
        Idle,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Owns one explicit asynchronous operation and prevents it from running more than once at a time.
    /// </summary>
    /// <remarks>
    /// The owner supplies success and error presentation. This class has no dependency on overlays,
    /// navigation, or a particular control.
    /// </remarks>
    public sealed class AsyncAction {
        private readonly Func<CancellationToken, Task> _operation;
        private readonly Action? _onSucceeded;
        private readonly Action<Exception>? _onFailed;
        private Task? _runningTask;
        private CancellationTokenSource? _cancellation;

        public AsyncAction(
            Func<Task> operation,
            Action? onSucceeded = null,
            Action<Exception>? onFailed = null)
            : this(_ => operation?.Invoke() ?? throw new ArgumentNullException(nameof(operation)), onSucceeded, onFailed) {
        }

        public AsyncAction(
            Func<CancellationToken, Task> operation,
            Action? onSucceeded = null,
            Action<Exception>? onFailed = null) {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _onSucceeded = onSucceeded;
            _onFailed = onFailed;
        }

        /// <summary>Gets the current action lifecycle state.</summary>
        public State<AsyncActionStatus> Status { get; } = new(AsyncActionStatus.Idle);

        /// <summary>Gets whether an operation is in flight.</summary>
        public State<bool> IsRunning { get; } = new(false);

        /// <summary>Gets the most recent operation error, if any.</summary>
        public State<Exception?> Error { get; } = new(null);

        /// <summary>Requests cancellation for the current operation, if it supports cancellation.</summary>
        public bool Cancel() {
            if (!IsRunning.Value || _cancellation is null || _cancellation.IsCancellationRequested) {
                return false;
            }

            _cancellation.Cancel();
            return true;
        }

        /// <summary>
        /// Starts the operation or returns the in-flight task when it is already running.
        /// Operation failures become <see cref="Status"/> and <see cref="Error"/> state, and are
        /// delivered to <c>onFailed</c>; they are not raised from a UI click handler.
        /// </summary>
        public Task Run() {
            if (IsRunning.Value) {
                return _runningTask ?? Task.CompletedTask;
            }

            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            var task = RunCore(cancellation);
            _runningTask = task;
            return task;
        }

        private async Task RunCore(CancellationTokenSource cancellation) {
            IsRunning.Value = true;
            Error.Value = null;
            Status.Value = AsyncActionStatus.Running;

            try {
                await _operation(cancellation.Token);
                Status.Value = AsyncActionStatus.Succeeded;
                _onSucceeded?.Invoke();
            } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                Status.Value = AsyncActionStatus.Cancelled;
            } catch (Exception exception) {
                Error.Value = exception;
                Status.Value = AsyncActionStatus.Failed;
                _onFailed?.Invoke(exception);
            } finally {
                IsRunning.Value = false;
                if (_cancellation == cancellation) {
                    _cancellation = null;
                    _runningTask = null;
                }
                cancellation.Dispose();
            }
        }
    }

    /// <summary>A button that reflects and triggers one explicitly owned <see cref="AsyncAction" />.</summary>
    public sealed class AsyncButton : StatelessWidget {
        public AsyncButton(
            string text,
            AsyncAction action,
            string? loadingText = null,
            string? retryText = null,
            ButtonVariant variant = ButtonVariant.Primary,
            ButtonStyle? style = null,
            bool enabled = true,
            bool showError = true,
            bool cancelOnUnmount = true,
            FocusNode? focusNode = null) {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            LoadingText = loadingText ?? "Loading…";
            RetryText = retryText ?? "Retry";
            Variant = variant;
            Style = style;
            Enabled = enabled;
            ShowError = showError;
            CancelOnUnmount = cancelOnUnmount;
            FocusNode = focusNode;
        }

        public string Text { get; }
        public string LoadingText { get; }
        public string RetryText { get; }
        public AsyncAction Action { get; }
        public ButtonVariant Variant { get; }
        public ButtonStyle? Style { get; }
        public bool Enabled { get; }
        public bool ShowError { get; }
        public bool CancelOnUnmount { get; }
        public FocusNode? FocusNode { get; }

        public override Widget Build(BuildContext context) {
            return new AsyncActionScope(
                Action,
                new ReactiveBuilder<AsyncActionStatus>(Action.Status, BuildButton),
                CancelOnUnmount);
        }

        private Widget BuildButton(AsyncActionStatus status) {
            var label = status switch {
                AsyncActionStatus.Running => LoadingText,
                AsyncActionStatus.Failed => RetryText,
                _ => Text
            };
            var button = new Button(
                label,
                () => { _ = Action.Run(); },
                Variant,
                Style,
                Enabled && status != AsyncActionStatus.Running,
                FocusNode,
                replaceFocusNodeAttachment: true);
            if (!ShowError || status != AsyncActionStatus.Failed || Action.Error.Value is not { } error) {
                return button;
            }

            return new Column(
                new Widget[]
                {
                button,
                new Text(error.Message, new TextStyle(new UnityEngine.Color(1f, 0.40f, 0.45f), 13f))
                },
                gap: 4f);
        }
    }

    /// <summary>Associates cancellation of an <see cref="AsyncAction"/> with one mounted subtree.</summary>
    public sealed class AsyncActionScope : Widget {
        public AsyncActionScope(AsyncAction action, Widget child, bool cancelOnUnmount = true) {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Child = child ?? throw new ArgumentNullException(nameof(child));
            CancelOnUnmount = cancelOnUnmount;
        }

        public AsyncAction Action { get; }
        public Widget Child { get; }
        public bool CancelOnUnmount { get; }
        internal override WidgetNode CreateNode() => new AsyncActionScopeNode(this);
    }

    internal sealed class AsyncActionScopeNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;

        public AsyncActionScopeNode(AsyncActionScope widget) : base(widget) { }

        protected override UnityEngine.UIElements.VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (AsyncActionScope)Widget;
            _currentChild = MountChild(widget.Child, NativeParent);
            AdoptNativeElement(_currentChild!.NativeElement);
            Bindings.Add(CancelOwnedAction);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not AsyncActionScope scope) return false;
            var previous = (AsyncActionScope)Widget;
            ReconcileSingleChild(ref _currentChild, scope.Child, NativeParent);
            if (!ReferenceEquals(previous.Action, scope.Action) && previous.CancelOnUnmount) {
                previous.Action.Cancel();
            }
            UpdateWidget(scope);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }

        private void CancelOwnedAction() {
            var widget = (AsyncActionScope)Widget;
            if (widget.CancelOnUnmount) widget.Action.Cancel();
        }
    }

}
