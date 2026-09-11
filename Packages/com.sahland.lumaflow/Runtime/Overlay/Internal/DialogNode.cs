#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    internal sealed class DialogNode : WidgetNode {
        private VisualElement? _actions;
        private WidgetNode? _titleNode;
        private WidgetNode? _contentNode;
        private readonly List<WidgetNode> _actionNodes = new();
        public DialogNode(Dialog widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var dialog = new VisualElement();
            dialog.AddToClassList("lumaflow-dialog");
            dialog.style.flexDirection = FlexDirection.Column;
            ApplySurface(dialog, context.Theme);
            _actions = new VisualElement();
            _actions.AddToClassList("lumaflow-dialog__actions");
            _actions.style.flexDirection = FlexDirection.Row;
            _actions.style.justifyContent = Justify.FlexEnd;
            _actions.style.marginTop = 16f;
            return dialog;
        }

        protected override void OnMounted() {
            var widget = (Dialog)Widget;
            if (widget.Title is { } title) _titleNode = MountChild(title, Element);
            _contentNode = MountChild(widget.Content, Element);
            ReconcileActions(widget.Actions);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Dialog dialog) return false;
            ReconcileTitle(ref _titleNode, dialog.Title);
            ReconcileSingleChild(ref _contentNode, dialog.Content, Element);
            ReconcileActions(dialog.Actions);
            UpdateWidget(dialog);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) => ApplySurface(Element, Context.Theme);

        private static void ApplySurface(VisualElement dialog, ThemeData? theme) {
            var colors = theme?.Colors;
            var radius = theme?.Radius.Medium ?? BorderRadius.All(12f);
            dialog.style.width = 320f;
            dialog.style.maxWidth = Length.Percent(90f);
            dialog.style.backgroundColor = StyleKeyword.Null;
            if (colors is not null) dialog.style.backgroundColor = colors.Surface;
            dialog.style.paddingLeft = theme?.Spacing.Medium ?? 16f;
            dialog.style.paddingRight = theme?.Spacing.Medium ?? 16f;
            dialog.style.paddingTop = theme?.Spacing.Medium ?? 16f;
            dialog.style.paddingBottom = theme?.Spacing.Medium ?? 16f;
            dialog.style.borderTopLeftRadius = radius.TopLeft;
            dialog.style.borderTopRightRadius = radius.TopRight;
            dialog.style.borderBottomRightRadius = radius.BottomRight;
            dialog.style.borderBottomLeftRadius = radius.BottomLeft;
            dialog.style.borderTopColor = StyleKeyword.Null;
            dialog.style.borderRightColor = StyleKeyword.Null;
            dialog.style.borderBottomColor = StyleKeyword.Null;
            dialog.style.borderLeftColor = StyleKeyword.Null;
            if (colors is not null) {
                dialog.style.borderTopColor = colors.Outline;
                dialog.style.borderRightColor = colors.Outline;
                dialog.style.borderBottomColor = colors.Outline;
                dialog.style.borderLeftColor = colors.Outline;
            }
            dialog.style.borderTopWidth = 1f;
            dialog.style.borderRightWidth = 1f;
            dialog.style.borderBottomWidth = 1f;
            dialog.style.borderLeftWidth = 1f;
        }

        private void ReconcileTitle(ref WidgetNode? current, Widget? title) {
            if (title is null) {
                var previous = current;
                current = null;
                if (previous is not null) UnmountChild(previous);
                return;
            }

            ReconcileSingleChild(ref current, title, Element);
            if (Element.IndexOf(current!.NativeElement) != 0) {
                current.NativeElement.RemoveFromHierarchy();
                Element.Insert(0, current.NativeElement);
            }
        }

        private void ReconcileActions(IReadOnlyList<Widget> actions) {
            ValidateUniqueActionKeys(actions);
            var previous = _actionNodes.ToArray();
            var keyedPrevious = new Dictionary<WidgetKey, WidgetNode>();
            foreach (var node in previous) {
                if (node.Configuration.Key is { } key) keyedPrevious.Add(key, node);
            }

            var retained = new HashSet<WidgetNode>();
            var resolved = new List<WidgetNode>(actions.Count);
            var mounted = new List<WidgetNode>();
            try {
                for (var index = 0; index < actions.Count; index++) {
                    var action = actions[index] ?? throw new ArgumentException("Dialog actions cannot contain null.", nameof(actions));
                    WidgetNode? candidate = null;
                    if (action.Key is { } key) {
                        keyedPrevious.TryGetValue(key, out candidate);
                    } else if (index < previous.Length && previous[index].Configuration.Key is null) {
                        candidate = previous[index];
                    }

                    if (candidate is not null
                        && !retained.Contains(candidate)
                        && (ReferenceEquals(candidate.Configuration, action) || candidate.TryUpdate(action))) {
                        retained.Add(candidate);
                        resolved.Add(candidate);
                        continue;
                    }

                    var node = MountChild(action, _actions!);
                    mounted.Add(node);
                    resolved.Add(node);
                }
            } catch (Exception reconciliationFailure) {
                List<Exception>? cleanupFailures = null;
                for (var index = mounted.Count - 1; index >= 0; index--) {
                    try {
                        UnmountChild(mounted[index]);
                    } catch (Exception exception) {
                        cleanupFailures ??= new List<Exception>();
                        cleanupFailures.Add(exception);
                    }
                }

                if (cleanupFailures is not null) {
                    cleanupFailures.Insert(0, reconciliationFailure);
                    throw new InvalidOperationException(
                        "Dialog action reconciliation failed and could not fully clean its newly mounted actions.",
                        new AggregateException(cleanupFailures));
                }

                throw;
            }

            _actionNodes.Clear();
            _actionNodes.AddRange(resolved);
            List<Exception>? removalFailures = null;
            foreach (var node in previous) {
                if (retained.Contains(node)) continue;
                try {
                    UnmountChild(node);
                } catch (Exception exception) {
                    removalFailures ??= new List<Exception>();
                    removalFailures.Add(exception);
                }
            }

            for (var index = 0; index < resolved.Count; index++) {
                var native = resolved[index].NativeElement;
                if (_actions!.IndexOf(native) == index) continue;
                native.RemoveFromHierarchy();
                _actions.Insert(index, native);
            }

            if (resolved.Count == 0) {
                _actions!.RemoveFromHierarchy();
            } else if (_actions!.parent != Element) {
                Element.Add(_actions);
            } else if (Element.IndexOf(_actions) != Element.childCount - 1) {
                _actions.RemoveFromHierarchy();
                Element.Add(_actions);
            }

            if (removalFailures is { Count: 1 }) {
                throw new InvalidOperationException(
                    "Dialog actions updated, but a removed action failed to clean up.",
                    removalFailures[0]);
            }

            if (removalFailures is { Count: > 1 }) {
                throw new InvalidOperationException(
                    "Dialog actions updated, but multiple removed actions failed to clean up.",
                    new AggregateException(removalFailures));
            }
        }

        private static void ValidateUniqueActionKeys(IReadOnlyList<Widget> actions) {
            Dictionary<WidgetKey, int>? keyIndexes = null;
            for (var index = 0; index < actions.Count; index++) {
                var action = actions[index];
                if (action is null) throw new ArgumentException("Dialog actions cannot contain null.", nameof(actions));
                if (action.Key is not { } key) continue;
                keyIndexes ??= new Dictionary<WidgetKey, int>();
                if (keyIndexes.TryGetValue(key, out var previousIndex)) {
                    throw new InvalidOperationException(
                        $"Dialog received duplicate action key '{key.Value}' at indexes "
                        + $"{previousIndex} and {index}. Assign a unique WidgetKey to each action.");
                }
                keyIndexes.Add(key, index);
            }
        }
    }

}
