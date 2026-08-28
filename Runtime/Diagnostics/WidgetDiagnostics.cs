#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LumaFlow {

    public enum LumaFlowDiagnosticSeverity {
        Info,
        Warning,
        Error
    }

    /// <summary>One actionable finding produced while inspecting a mounted widget tree.</summary>
    public sealed class LumaFlowDiagnostic {
        internal LumaFlowDiagnostic(
            string code,
            LumaFlowDiagnosticSeverity severity,
            string message,
            string widgetPath) {
            Code = code;
            Severity = severity;
            Message = message;
            WidgetPath = widgetPath;
        }

        public string Code { get; }
        public LumaFlowDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string WidgetPath { get; }

        public override string ToString() => $"{Severity} {Code} at {WidgetPath}: {Message}";
    }

    /// <summary>An immutable diagnostic projection of one mounted widget node.</summary>
    public sealed class WidgetDiagnosticsNode {
        internal WidgetDiagnosticsNode(
            string widgetType,
            string nodeType,
            string? key,
            string lifecycleState,
            string? stateType,
            string nativeElementType,
            string nativeElementName,
            Rect layout,
            bool enabled,
            bool focused,
            bool transparent,
            bool hasTheme,
            Color? themePrimaryColor,
            Color? themeSurfaceColor,
            MediaQueryData mediaQuery,
            string? locale,
            float textScaleFactor,
            IReadOnlyList<string> classes,
            IReadOnlyList<string> inheritedAspects,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyList<WidgetDiagnosticsNode> children) {
            WidgetType = widgetType;
            NodeType = nodeType;
            Key = key;
            LifecycleState = lifecycleState;
            StateType = stateType;
            NativeElementType = nativeElementType;
            NativeElementName = nativeElementName;
            Layout = layout;
            Enabled = enabled;
            Focused = focused;
            Transparent = transparent;
            HasTheme = hasTheme;
            ThemePrimaryColor = themePrimaryColor;
            ThemeSurfaceColor = themeSurfaceColor;
            MediaQuery = mediaQuery;
            Locale = locale;
            TextScaleFactor = textScaleFactor;
            Classes = classes;
            InheritedAspects = inheritedAspects;
            Properties = properties;
            Children = children;
        }

        public string WidgetType { get; }
        public string NodeType { get; }
        public string? Key { get; }
        public string LifecycleState { get; }
        public string? StateType { get; }
        public string NativeElementType { get; }
        public string NativeElementName { get; }
        public Rect Layout { get; }
        public bool Enabled { get; }
        public bool Focused { get; }
        public bool Transparent { get; }
        public bool HasTheme { get; }
        public Color? ThemePrimaryColor { get; }
        public Color? ThemeSurfaceColor { get; }
        public MediaQueryData MediaQuery { get; }
        public string? Locale { get; }
        public float TextScaleFactor { get; }
        public IReadOnlyList<string> Classes { get; }
        public IReadOnlyList<string> InheritedAspects { get; }
        public IReadOnlyDictionary<string, string> Properties { get; }
        public IReadOnlyList<WidgetDiagnosticsNode> Children { get; }
    }

    /// <summary>A point-in-time, non-owning snapshot of one active LumaFlow mount.</summary>
    public sealed class WidgetTreeDiagnostics {
        internal WidgetTreeDiagnostics(
            int mountId,
            DateTime capturedAtUtc,
            WidgetDiagnosticsNode root,
            IReadOnlyList<LumaFlowDiagnostic> findings) {
            MountId = mountId;
            CapturedAtUtc = capturedAtUtc;
            Root = root;
            Findings = findings;
        }

        public int MountId { get; }
        public DateTime CapturedAtUtc { get; }
        public WidgetDiagnosticsNode Root { get; }
        public IReadOnlyList<LumaFlowDiagnostic> Findings { get; }

        /// <summary>Formats the captured declarative tree without retaining native elements.</summary>
        public string ToStringDeep() => LumaFlowDiagnostics.FormatTree(this);

        public override string ToString() =>
            $"LumaFlow mount #{MountId}: {Root.WidgetType}, {Findings.Count} finding(s)";
    }

    /// <summary>Captures active trees and formats diagnostic snapshots on the Unity main thread.</summary>
    public static class LumaFlowDiagnostics {
        private static readonly object Sync = new();
        private static readonly List<MountRegistration> Registrations = new();
        private static int _nextMountId;

        /// <summary>Captures every currently live mount without extending its lifetime.</summary>
        public static IReadOnlyList<WidgetTreeDiagnostics> CaptureActiveTrees() {
            List<MountHandle> handles;
            lock (Sync) {
                handles = new List<MountHandle>(Registrations.Count);
                for (var index = Registrations.Count - 1; index >= 0; index--) {
                    if (!Registrations[index].Handle.TryGetTarget(out var handle) || !handle.IsMounted) {
                        Registrations.RemoveAt(index);
                        continue;
                    }
                    handles.Add(handle);
                }
            }

            var snapshots = new List<WidgetTreeDiagnostics>(handles.Count);
            for (var index = handles.Count - 1; index >= 0; index--) {
                if (!handles[index].IsMounted) continue;
                try {
                    snapshots.Add(handles[index].CaptureDiagnostics());
                } catch (ObjectDisposedException) {
                    // A caller outside the documented main-thread contract may race disposal.
                    // Disposed mounts are omitted from the active snapshot.
                }
            }
            return snapshots.AsReadOnly();
        }

        /// <summary>Formats widget, state, key, native element, layout, and inherited-aspect data.</summary>
        public static string FormatTree(WidgetTreeDiagnostics diagnostics) {
            if (diagnostics is null) throw new ArgumentNullException(nameof(diagnostics));
            var builder = new StringBuilder();
            builder.Append("LumaFlow mount #").Append(diagnostics.MountId).AppendLine();
            AppendNode(builder, diagnostics.Root, depth: 0);
            if (diagnostics.Findings.Count > 0) {
                builder.AppendLine("Findings:");
                for (var index = 0; index < diagnostics.Findings.Count; index++) {
                    builder.Append("  ").AppendLine(diagnostics.Findings[index].ToString());
                }
            }
            return builder.ToString();
        }

        internal static int Register(MountHandle handle) {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            lock (Sync) {
                var id = ++_nextMountId;
                Registrations.Add(new MountRegistration(id, new WeakReference<MountHandle>(handle)));
                return id;
            }
        }

        internal static void Unregister(int mountId) {
            lock (Sync) {
                for (var index = Registrations.Count - 1; index >= 0; index--) {
                    if (Registrations[index].Id == mountId
                        || !Registrations[index].Handle.TryGetTarget(out _)) {
                        Registrations.RemoveAt(index);
                    }
                }
            }
        }

        internal static WidgetTreeDiagnostics Capture(int mountId, WidgetNode rootNode) {
            var root = rootNode.CaptureDiagnostics();
            var findings = new List<LumaFlowDiagnostic>();
            Analyze(root, root.WidgetType, findings);
            return new WidgetTreeDiagnostics(
                mountId,
                DateTime.UtcNow,
                root,
                findings.AsReadOnly());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() {
            lock (Sync) {
                Registrations.Clear();
                _nextMountId = 0;
            }
        }

        private static void Analyze(
            WidgetDiagnosticsNode node,
            string path,
            ICollection<LumaFlowDiagnostic> findings) {
            if (node.Children.Count > 1) {
                for (var index = 0; index < node.Children.Count; index++) {
                    var child = node.Children[index];
                    if (child.StateType is null || child.Key is not null) continue;
                    findings.Add(new LumaFlowDiagnostic(
                        "LF1001",
                        LumaFlowDiagnosticSeverity.Warning,
                        "This stateful sibling has positional identity. Assign a WidgetKey before allowing insertion, removal, or reordering.",
                        ChildPath(path, child, index)));
                }
            }

            if (node.WidgetType == typeof(Native).FullName) {
                findings.Add(new LumaFlowDiagnostic(
                    "LF1002",
                    LumaFlowDiagnosticSeverity.Info,
                    "Native is an explicit UI Toolkit ownership boundary. Keep the borrowed element detached before mount and do not mutate its parent externally.",
                    path));
            }

            for (var index = 0; index < node.Children.Count; index++) {
                var child = node.Children[index];
                Analyze(child, ChildPath(path, child, index), findings);
            }
        }

        private static string ChildPath(string parent, WidgetDiagnosticsNode child, int index) =>
            $"{parent}/{child.WidgetType}[{index}]"
            + (child.Key is null ? string.Empty : $" key='{child.Key}'");

        private static void AppendNode(StringBuilder builder, WidgetDiagnosticsNode node, int depth) {
            builder.Append(' ', depth * 2)
                .Append(node.WidgetType);
            if (node.Key is not null) builder.Append(" key='").Append(node.Key).Append('\'');
            if (node.StateType is not null) builder.Append(" state=").Append(node.StateType);
            builder.Append(" native=").Append(node.NativeElementType);
            if (!string.IsNullOrEmpty(node.NativeElementName)) {
                builder.Append(" name='").Append(node.NativeElementName).Append('\'');
            }
            builder.Append(" layout=")
                .Append(node.Layout.width.ToString("0.##", CultureInfo.InvariantCulture))
                .Append('x')
                .Append(node.Layout.height.ToString("0.##", CultureInfo.InvariantCulture));
            if (node.Transparent) builder.Append(" transparent");
            if (node.Focused) builder.Append(" focused");
            if (!node.Enabled) builder.Append(" disabled");
            if (node.InheritedAspects.Count > 0) {
                builder.Append(" inherits=[").Append(string.Join(",", node.InheritedAspects)).Append(']');
            }
            if (node.InheritedAspects.Contains(nameof(InheritedAspect.Theme)) && node.HasTheme) {
                builder.Append(" theme.primary=#")
                    .Append(ColorUtility.ToHtmlStringRGBA(node.ThemePrimaryColor!.Value));
            }
            if (node.InheritedAspects.Contains(nameof(InheritedAspect.MediaQuery))) {
                builder.Append(" media=")
                    .Append(node.MediaQuery.Width.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append('x')
                    .Append(node.MediaQuery.Height.ToString("0.##", CultureInfo.InvariantCulture));
                if (node.MediaQuery.DisableAnimations) builder.Append(" motion=disabled");
            }
            if (node.InheritedAspects.Contains(nameof(InheritedAspect.Localizations)) && node.Locale is not null) {
                builder.Append(" locale=").Append(node.Locale);
            }
            if (node.InheritedAspects.Contains(nameof(InheritedAspect.TextScaler))) {
                builder.Append(" textScale=")
                    .Append(node.TextScaleFactor.ToString("0.##", CultureInfo.InvariantCulture));
            }
            if (node.Properties.Count > 0) {
                builder.Append(" properties=[");
                var first = true;
                foreach (var property in node.Properties) {
                    if (!first) builder.Append(", ");
                    builder.Append(property.Key).Append('=').Append(property.Value);
                    first = false;
                }
                builder.Append(']');
            }
            builder.AppendLine();
            for (var index = 0; index < node.Children.Count; index++) {
                AppendNode(builder, node.Children[index], depth + 1);
            }
        }

        private readonly struct MountRegistration {
            public MountRegistration(int id, WeakReference<MountHandle> handle) {
                Id = id;
                Handle = handle;
            }

            public int Id { get; }
            public WeakReference<MountHandle> Handle { get; }
        }
    }
}
