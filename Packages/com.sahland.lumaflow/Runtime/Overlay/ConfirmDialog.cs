#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Modal confirmation content with an explicit asynchronous decision result.</summary>
    public sealed class ConfirmDialog : Widget {
        private readonly TaskCompletionSource<bool> _result = new();
        private OverlayHandle? _handle;

        public ConfirmDialog(
            Widget content,
            Widget? title = null,
            string confirmText = "Confirm",
            string cancelText = "Cancel",
            ButtonVariant confirmVariant = ButtonVariant.Primary,
            Func<CancellationToken, Task>? onConfirm = null) {
            if (!Enum.IsDefined(typeof(ButtonVariant), confirmVariant)) throw new ArgumentOutOfRangeException(nameof(confirmVariant));
            if (string.IsNullOrWhiteSpace(confirmText)) throw new ArgumentException("Confirm text is required.", nameof(confirmText));
            if (string.IsNullOrWhiteSpace(cancelText)) throw new ArgumentException("Cancel text is required.", nameof(cancelText));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Title = title;
            ConfirmText = confirmText;
            CancelText = cancelText;
            ConfirmVariant = confirmVariant;
            ConfirmAction = new AsyncAction(
                onConfirm ?? (_ => Task.CompletedTask),
                onSucceeded: () => Complete(true));
        }

        public Widget Content { get; }
        public Widget? Title { get; }
        public string ConfirmText { get; }
        public string CancelText { get; }
        public ButtonVariant ConfirmVariant { get; }
        public AsyncAction ConfirmAction { get; }
        public Task<bool> Result => _result.Task;

        /// <summary>Completes the dialog as declined and cancels a running confirmation.</summary>
        public void Cancel() {
            ConfirmAction.Cancel();
            Complete(false);
        }

        internal void AttachHandle(OverlayHandle handle) {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            if (_result.Task.IsCompleted) _handle.Close();
        }

        internal void CompleteFromUnmount() {
            ConfirmAction.Cancel();
            _result.TrySetResult(false);
        }

        private void Complete(bool accepted) {
            _result.TrySetResult(accepted);
            _handle?.Close();
        }

        internal override WidgetNode CreateNode() => new ConfirmDialogNode(this);
    }

    internal sealed class ConfirmDialogNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _dialogNode;
        private ConfirmDialog _completionOwner;

        public ConfirmDialogNode(ConfirmDialog widget) : base(widget) {
            _completionOwner = widget;
        }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (ConfirmDialog)Widget;
            _dialogNode = MountChild(BuildDialog(widget), NativeParent);
            AdoptNativeElement(_dialogNode!.NativeElement);
            FocusSafeAction(_dialogNode.NativeElement);
            Bindings.Add(CompleteCurrentFromUnmount);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not ConfirmDialog dialog) return false;
            var previous = _completionOwner;
            ReconcileSingleChild(ref _dialogNode, BuildDialog(dialog), NativeParent);
            UpdateWidget(dialog);
            _completionOwner = dialog;
            AdoptNativeElement(_dialogNode!.NativeElement);
            if (!ReferenceEquals(previous, dialog)) previous.CompleteFromUnmount();
            return true;
        }

        private static Dialog BuildDialog(ConfirmDialog widget) {
            return new Dialog(
                widget.Content,
                widget.Title,
                new Widget[]
                {
                new Button(widget.CancelText, widget.Cancel, ButtonVariant.Secondary),
                new AsyncButton(
                    widget.ConfirmText,
                    widget.ConfirmAction,
                    loadingText: "Confirming…",
                    variant: widget.ConfirmVariant,
                    showError: true)
                });
        }

        private void CompleteCurrentFromUnmount() => _completionOwner.CompleteFromUnmount();

        private static void FocusSafeAction(VisualElement dialog) {
            if (dialog.childCount == 0) return;
            var actions = dialog[dialog.childCount - 1];
            if (actions.childCount > 0) actions[0].Focus();
        }
    }

}
