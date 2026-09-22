#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Owns one LumaFlow tree mounted into an external UI Toolkit root.
    /// </summary>
    public sealed class MountHandle : IDisposable {
        private WidgetNode? _rootNode;
        private VisualElement? _host;
        private VisualElement? _root;
        private SemanticsOwner? _semantics;
        private BuildContext? _context;
        private readonly int _diagnosticId;

        internal MountHandle(
            WidgetNode rootNode,
            VisualElement host,
            VisualElement root,
            SemanticsOwner semantics,
            BuildContext context) {
            _rootNode = rootNode;
            _host = host;
            _root = root;
            _semantics = semantics;
            _context = context;
            root.RegisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
            _diagnosticId = LumaFlowDiagnostics.Register(this);
        }

        internal SemanticsOwner? SemanticsOwner => _semantics;

        /// <summary>
        /// Gets whether the mounted tree is still active.
        /// </summary>
        public bool IsMounted => _rootNode is { IsMounted: true };

        /// <summary>Gets the process-local identity used by LumaFlow diagnostic tooling.</summary>
        public int DiagnosticId => _diagnosticId;

        /// <summary>Captures an immutable, non-owning snapshot of this mounted tree.</summary>
        public WidgetTreeDiagnostics CaptureDiagnostics() {
            var rootNode = _rootNode
                ?? throw new ObjectDisposedException(nameof(MountHandle), "Cannot inspect a disposed LumaFlow mount.");
            return LumaFlowDiagnostics.Capture(_diagnosticId, rootNode);
        }

        /// <summary>
        /// Rebuilds declarative builder boundaries in the mounted tree while retaining
        /// compatible nodes and their mount-local state.
        /// </summary>
        public void Rebuild() {
            var rootNode = _rootNode
                ?? throw new ObjectDisposedException(nameof(MountHandle), "Cannot rebuild a disposed LumaFlow mount.");
            rootNode.Reassemble();
        }

        /// <summary>
        /// Replaces the mounted root with a fresh tree. The replacement is mounted
        /// before the previous tree is released, so a failed mount leaves the active
        /// tree untouched.
        /// </summary>
        public void Restart(Widget widget) {
            if (widget is null) throw new ArgumentNullException(nameof(widget));
            var previous = _rootNode
                ?? throw new ObjectDisposedException(nameof(MountHandle), "Cannot restart a disposed LumaFlow mount.");
            var host = _host!;
            var context = _context!;
            var previousNativeIndex = host.IndexOf(previous.NativeElement);
            var replacement = widget.CreateNode();
            try {
                replacement.Mount(parent: null, context, host);
                if (previousNativeIndex >= 0) {
                    replacement.NativeElement.RemoveFromHierarchy();
                    host.Insert(previousNativeIndex, replacement.NativeElement);
                }
            } catch (Exception mountFailure) {
                try {
                    replacement.UnmountAfterFailedMount();
                } catch (Exception cleanupFailure) {
                    throw new AggregateException(mountFailure, cleanupFailure);
                }

                throw;
            }

            _rootNode = replacement;
            previous.Unmount();
        }

        /// <summary>
        /// Unmounts the tree owned by this handle. Repeated calls are safe.
        /// </summary>
        public void Dispose() {
            var rootNode = _rootNode;
            if (rootNode is null) {
                return;
            }

            _rootNode = null;
            var host = _host;
            _host = null;
            var root = _root;
            _root = null;
            root?.UnregisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
            var semantics = _semantics;
            _semantics = null;
            _context = null;

            try {
                try {
                    rootNode.Unmount();
                } finally {
                    try {
                        host?.RemoveFromHierarchy();
                    } finally {
                        semantics?.Dispose();
                    }
                }
            } finally {
                LumaFlowDiagnostics.Unregister(_diagnosticId);
            }
        }

        private void HandleRootGeometryChanged(GeometryChangedEvent change) {
            if (change.target != _root || _context is null) return;
            _context.UpdateMediaQuery(_context.DiagnosticMediaQuery.WithSize(
                NormalizeDimension(change.newRect.width),
                NormalizeDimension(change.newRect.height)));
        }

        private static float NormalizeDimension(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value;
    }

}
