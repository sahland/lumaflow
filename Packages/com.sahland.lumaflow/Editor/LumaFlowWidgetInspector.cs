#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow.Editor {

    /// <summary>Editor-only inspector for immutable snapshots of active LumaFlow mounts.</summary>
    public sealed class LumaFlowWidgetInspector : EditorWindow {
        internal const string EmptyStateName = "lumaflow-widget-inspector-empty";
        internal const string MountFoldoutName = "lumaflow-widget-inspector-mount";
        internal const string WidgetRowName = "lumaflow-widget-inspector-widget";
        internal const string FindingName = "lumaflow-widget-inspector-finding";

        [MenuItem("Window/LumaFlow/Widget Inspector")]
        public static void Open() {
            var window = GetWindow<LumaFlowWidgetInspector>();
            window.titleContent = new GUIContent("LumaFlow Inspector");
            window.minSize = new Vector2(420f, 260f);
            window.Show();
        }

        public void CreateGUI() {
            rootVisualElement.style.flexGrow = 1f;
            RefreshNow();
        }

        internal void RefreshNow() => Render(LumaFlowDiagnostics.CaptureActiveTrees());

        internal void Render(IReadOnlyList<WidgetTreeDiagnostics> snapshots) {
            if (snapshots is null) throw new ArgumentNullException(nameof(snapshots));
            rootVisualElement.Clear();

            var header = new VisualElement { name = "lumaflow-widget-inspector-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = UnityEngine.UIElements.Align.Center;
            header.style.paddingLeft = 8f;
            header.style.paddingRight = 8f;
            header.style.paddingTop = 6f;
            header.style.paddingBottom = 6f;
            header.style.borderBottomWidth = 1f;
            header.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            var title = new Label($"Active mounts: {snapshots.Count}");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1f;
            header.Add(title);
            header.Add(new UnityEngine.UIElements.Button(RefreshNow) { text = "Refresh" });
            rootVisualElement.Add(header);

            if (snapshots.Count == 0) {
                var empty = new Label(
                    "No active LumaFlow mounts. Enter Play Mode or open a mounted Editor UI, then refresh.") {
                    name = EmptyStateName
                };
                empty.style.whiteSpace = WhiteSpace.Normal;
                empty.style.marginLeft = 12f;
                empty.style.marginRight = 12f;
                empty.style.marginTop = 12f;
                rootVisualElement.Add(empty);
                return;
            }

            var scroll = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            for (var index = 0; index < snapshots.Count; index++) {
                scroll.Add(BuildMount(snapshots[index]));
            }
            rootVisualElement.Add(scroll);
        }

        private static VisualElement BuildMount(WidgetTreeDiagnostics snapshot) {
            var foldout = new Foldout {
                name = MountFoldoutName,
                text = $"Mount #{snapshot.MountId} · {ShortName(snapshot.Root.WidgetType)}",
                value = true
            };
            foldout.style.marginLeft = 4f;
            foldout.style.marginRight = 4f;

            var summary = new VisualElement();
            summary.style.flexDirection = FlexDirection.Row;
            summary.style.alignItems = UnityEngine.UIElements.Align.Center;
            summary.style.marginLeft = 12f;
            var count = new Label($"{CountNodes(snapshot.Root)} nodes · {snapshot.Findings.Count} findings");
            count.style.flexGrow = 1f;
            summary.Add(count);
            summary.Add(new UnityEngine.UIElements.Button(() =>
                EditorGUIUtility.systemCopyBuffer = snapshot.ToStringDeep()) { text = "Copy tree" });
            foldout.Add(summary);

            if (snapshot.Findings.Count > 0) {
                var findings = new Foldout { text = "Findings", value = true };
                findings.style.marginLeft = 12f;
                for (var index = 0; index < snapshot.Findings.Count; index++) {
                    var finding = snapshot.Findings[index];
                    var label = new Label($"{finding.Severity} {finding.Code}: {finding.Message}\n{finding.WidgetPath}") {
                        name = FindingName,
                        tooltip = finding.ToString()
                    };
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.marginBottom = 4f;
                    label.style.color = finding.Severity switch {
                        LumaFlowDiagnosticSeverity.Error => new Color(1f, 0.4f, 0.4f),
                        LumaFlowDiagnosticSeverity.Warning => new Color(1f, 0.75f, 0.25f),
                        _ => new Color(0.55f, 0.75f, 1f)
                    };
                    findings.Add(label);
                }
                foldout.Add(findings);
            }

            var tree = new VisualElement();
            tree.style.marginTop = 4f;
            AppendNode(tree, snapshot.Root, depth: 0);
            foldout.Add(tree);
            return foldout;
        }

        private static void AppendNode(VisualElement parent, WidgetDiagnosticsNode node, int depth) {
            var key = node.Key is null ? string.Empty : $"  key='{node.Key}'";
            var state = node.StateType is null ? string.Empty : $"  state={ShortName(node.StateType)}";
            var label = new Label($"{ShortName(node.WidgetType)}{key}{state}") {
                name = WidgetRowName,
                tooltip = BuildTooltip(node)
            };
            label.style.marginLeft = 12f + depth * 14f;
            label.style.height = 20f;
            if (node.Focused) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (!node.Enabled) label.style.opacity = 0.55f;
            parent.Add(label);
            for (var index = 0; index < node.Children.Count; index++) {
                AppendNode(parent, node.Children[index], depth + 1);
            }
        }

        private static string BuildTooltip(WidgetDiagnosticsNode node) {
            var inherited = node.InheritedAspects.Count == 0
                ? "none"
                : string.Join(", ", node.InheritedAspects);
            var properties = node.Properties.Count == 0
                ? "none"
                : string.Join("\n", node.Properties.Select(property => $"  {property.Key}: {property.Value}"));
            return $"Widget: {node.WidgetType}\n"
                + $"Node: {node.NodeType}\n"
                + $"Native: {node.NativeElementType} '{node.NativeElementName}'\n"
                + $"Layout: {node.Layout.width:0.##} × {node.Layout.height:0.##}\n"
                + $"Lifecycle: {node.LifecycleState}\n"
                + $"Layout properties:\n{properties}\n"
                + $"Inherited dependencies: {inherited}\n"
                + $"Theme: {(node.HasTheme ? $"primary #{ColorUtility.ToHtmlStringRGBA(node.ThemePrimaryColor!.Value)}" : "none")}\n"
                + $"MediaQuery: {node.MediaQuery.Width:0.##} × {node.MediaQuery.Height:0.##}, motion disabled={node.MediaQuery.DisableAnimations}\n"
                + $"Locale: {node.Locale ?? "none"}\n"
                + $"Text scale: {node.TextScaleFactor:0.##}";
        }

        private static int CountNodes(WidgetDiagnosticsNode node) =>
            1 + node.Children.Sum(CountNodes);

        private static string ShortName(string fullName) {
            var separator = fullName.LastIndexOf('.');
            return separator < 0 ? fullName : fullName.Substring(separator + 1);
        }
    }
}
