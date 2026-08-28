#nullable enable

using System;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Mounts LumaFlow widgets into existing UI Toolkit hierarchies.
    /// </summary>
    public static class LumaFlow {
        /// <summary>
        /// Mounts <paramref name="widget" /> into a LumaFlow-owned host within <paramref name="root" />.
        /// </summary>
        /// <param name="widget">The root widget to mount.</param>
        /// <param name="root">The externally owned UI Toolkit root.</param>
        /// <returns>A handle that unmounts only this LumaFlow tree.</returns>
        public static MountHandle Mount(Widget widget, VisualElement root) {
            if (widget is null) {
                throw new ArgumentNullException(nameof(widget));
            }

            if (root is null) {
                throw new ArgumentNullException(nameof(root));
            }

            var host = new VisualElement {
                name = "lumaflow-mount-host"
            };
            // A mounted application is a viewport-owned subtree. Without this the
            // host shrinks to the intrinsic height of its route, leaving the native
            // parent visible below a Scaffold or AdaptiveScaffold.
            host.style.flexGrow = 1f;
            host.style.flexShrink = 1f;
            host.style.minWidth = 0f;
            host.style.minHeight = 0f;

            var rootNode = widget.CreateNode();
            var semantics = new SemanticsOwner();
            try {
                var rootRect = root.contentRect;
                var context = new BuildContext(mediaQuery: new MediaQueryData(
                    NormalizeInitialDimension(rootRect.width),
                    NormalizeInitialDimension(rootRect.height)),
                    semantics: semantics);
                rootNode.Mount(parent: null, context, host);
                root.Add(host);
                return new MountHandle(rootNode, host, semantics, context);
            } catch (Exception mountFailure) {
                Exception? cleanupFailure = null;
                try {
                    rootNode.UnmountAfterFailedMount();
                } catch (Exception exception) {
                    cleanupFailure = exception;
                }

                try {
                    host.RemoveFromHierarchy();
                } catch (Exception exception) {
                    cleanupFailure = cleanupFailure is null
                        ? exception
                        : new AggregateException(cleanupFailure, exception);
                } finally {
                    try {
                        semantics.Dispose();
                    } catch (Exception exception) {
                        cleanupFailure = cleanupFailure is null
                            ? exception
                            : new AggregateException(cleanupFailure, exception);
                    }
                }

                if (cleanupFailure is not null) {
                    throw new AggregateException(mountFailure, cleanupFailure);
                }

                throw;
            }
        }

        private static float NormalizeInitialDimension(float value) {
            // A VisualElement that has not reached its first layout pass can report
            // NaN dimensions. This is a valid transitional panel state, represented
            // by zero until an explicit LayoutBuilder receives resolved geometry.
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f
                ? 0f
                : value;
        }
    }

}
