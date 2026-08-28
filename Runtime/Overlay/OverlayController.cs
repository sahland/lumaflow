#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Scoped entry point for showing temporary content in one <see cref="OverlayHost" />.</summary>
    public sealed class OverlayController {
        private OverlayHostNode? _host;

        public OverlayHandle Show(Widget content) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return _host?.Show(content) ?? throw new InvalidOperationException("The overlay controller is not mounted.");
        }

        /// <summary>Shows content above a pointer-blocking modal barrier.</summary>
        public OverlayHandle ShowModal(Widget content) => ShowModal(content, ModalOptions.Default);

        /// <summary>Shows modal content with explicit dismissal and focus behavior.</summary>
        public OverlayHandle ShowModal(Widget content, ModalOptions options) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (options == null) throw new ArgumentNullException(nameof(options));
            return _host?.ShowModal(content, options) ?? throw new InvalidOperationException("The overlay controller is not mounted.");
        }

        /// <summary>Shows a modal drawer aligned to one horizontal edge.</summary>
        public OverlayHandle ShowDrawer(
            Widget content,
            DrawerPlacement placement = DrawerPlacement.Left) =>
            ShowDrawer(content, placement, ModalOptions.Default);

        /// <summary>Shows a modal drawer with explicit dismissal and focus behavior.</summary>
        public OverlayHandle ShowDrawer(
            Widget content,
            DrawerPlacement placement,
            ModalOptions options) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (options == null) throw new ArgumentNullException(nameof(options));
            return _host?.ShowDrawer(content, placement, options)
                ?? throw new InvalidOperationException("The overlay controller is not mounted.");
        }

        /// <summary>Shows a confirmation dialog and completes with the user's decision.</summary>
        public Task<bool> ShowConfirm(
            Widget content,
            Widget? title = null,
            string confirmText = "Confirm",
            string cancelText = "Cancel",
            ButtonVariant confirmVariant = ButtonVariant.Primary,
            Func<CancellationToken, Task>? onConfirm = null,
            ModalOptions? options = null) {
            var dialog = new ConfirmDialog(content, title, confirmText, cancelText, confirmVariant, onConfirm);
            var handle = ShowModal(dialog, options ?? ModalOptions.Default);
            dialog.AttachHandle(handle);
            return dialog.Result;
        }

        /// <summary>
        /// Shows non-modal content positioned relative to a mounted native UI Toolkit element.
        /// The popover closes automatically when its anchor detaches from the panel.
        /// </summary>
        public OverlayHandle ShowPopover(VisualElement anchor, Widget content, PopoverPlacement placement = PopoverPlacement.BottomStart) {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            if (content == null) throw new ArgumentNullException(nameof(content));
            return _host?.ShowPopover(anchor, content, placement)
                ?? throw new InvalidOperationException("The overlay controller is not mounted.");
        }

        /// <summary>Shows non-modal content at the bottom of the host and closes it after <paramref name="duration" />.</summary>
        public OverlayHandle ShowToast(Widget content, TimeSpan duration) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration), "Toast duration must be positive.");
            return _host?.ShowToast(content, duration)
                ?? throw new InvalidOperationException("The overlay controller is not mounted.");
        }

        /// <summary>
        /// Closes the most recently shown overlay entry.
        /// Returns <see langword="false"/> when this scope has no open entry.
        /// </summary>
        public bool TryCloseTop() => _host?.TryCloseTop() ?? false;

        /// <summary>Gets whether this scope currently owns any overlay entry.</summary>
        public bool HasOpenEntries => _host?.HasOpenEntries ?? false;

        internal bool TryHandleBack() => _host?.TryHandleBack() ?? false;

        internal void Attach(OverlayHostNode host) {
            if (_host is not null && _host != host) throw new InvalidOperationException("An overlay controller can belong to only one mounted host.");
            _host = host;
        }

        internal void Detach(OverlayHostNode host) {
            if (_host == host) _host = null;
        }
    }

}
