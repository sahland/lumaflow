#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Builds a local subtree from its resolved UI Toolkit layout size.
    /// </summary>
    /// <remarks>
    /// A layout builder is an explicit structural boundary. It observes one parent-
    /// constrained axis and replaces only the subtree returned by <see cref="Builder"/>.
    /// Width is the default because it is stable for responsive composition inside
    /// vertical scrolling content.
    /// </remarks>
    public sealed class LayoutBuilder : Widget {
        public LayoutBuilder(
            Func<BuildContext, LayoutConstraints, Widget> builder,
            Axis axis = Axis.Horizontal) {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            if (!Enum.IsDefined(typeof(Axis), axis)) {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            Axis = axis;
        }

        /// <summary>Builds the responsive subtree for the current resolved size.</summary>
        public Func<BuildContext, LayoutConstraints, Widget> Builder { get; }

        /// <summary>
        /// Gets the constrained axis that triggers local subtree replacement.
        /// </summary>
        /// <remarks>
        /// Use <see cref="LumaFlow.Axis.Vertical"/> only below a parent that
        /// explicitly constrains height, such as <see cref="SizedBox"/> or
        /// <see cref="Expanded"/>. A vertical scroll content area is content-sized.
        /// </remarks>
        public Axis Axis { get; }

        internal override WidgetNode CreateNode() => new LayoutBuilderNode(this);
    }

    internal sealed class LayoutBuilderNode : WidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;
        private LayoutConstraints _constraints;
        private bool _hasConstraints;
        private bool _isRebuildScheduled;
        private IVisualElementScheduledItem? _scheduledRebuild;

        public LayoutBuilderNode(LayoutBuilder widget)
            : base(widget) {
        }

        protected override VisualElement CreateElement(BuildContext context) {
            return new VisualElement { name = "lumaflow-layout-builder" };
        }

        protected override void OnMounted() {
            Element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Bindings.Add(() => Element.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged));
            Bindings.Add(() => _scheduledRebuild?.Pause());
            ReplaceChild(ReadConstraints());
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not LayoutBuilder builder) return false;
            ReplaceChild(ReadConstraints(), builder, force: true);
            UpdateWidget(builder);
            return true;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            if (aspect == InheritedAspect.MediaQuery) {
                ReplaceChild(ReadConstraints(), force: true);
            }
        }

        internal override void Reassemble() => ReplaceChild(ReadConstraints(), force: true);

        private void OnGeometryChanged(GeometryChangedEvent change) {
            // During GeometryChangedEvent, contentRect can still expose the
            // previous layout pass. The event's newRect is the authoritative
            // resolved geometry for deciding whether a rebuild is necessary.
            if (change.target != Element || !HasObservedAxisChanged(ReadConstraints(change.newRect))) {
                return;
            }

            if (_isRebuildScheduled) {
                return;
            }

            _isRebuildScheduled = true;
            // GeometryChangedEvent runs during UI Toolkit's layout pass. The scheduler
            // applies one local replacement after that pass, coalescing resize events.
            // ExecuteLater explicitly schedules the work for the next panel update.
            // StartingIn(0) can be treated as an already-due item during an
            // EditMode geometry pass and never receive a subsequent tick.
            _scheduledRebuild = Element.schedule.Execute(FlushScheduledRebuild);
            _scheduledRebuild.ExecuteLater(0L);
        }

        private void FlushScheduledRebuild() {
            _isRebuildScheduled = false;
            _scheduledRebuild = null;
            ReplaceChild(ReadConstraints());
        }

        private void ReplaceChild(
            LayoutConstraints constraints,
            LayoutBuilder? configuration = null,
            bool force = false) {
            var widgetConfiguration = configuration ?? (LayoutBuilder)Widget;
            if (!force && _hasConstraints && !HasObservedAxisChanged(constraints, widgetConfiguration.Axis)) {
                return;
            }

            var mediaQuery = Context.MediaQuery.WithSize(constraints.MaxWidth, constraints.MaxHeight);
            if (_childContext is null) {
                _childContext = Context.WithMediaQuery(mediaQuery);
            } else {
                _childContext.UpdateMediaQuery(mediaQuery);
            }

            var widget = widgetConfiguration.Builder(_childContext, constraints)
                ?? throw new InvalidOperationException("LayoutBuilder builder cannot return null.");

            ReconcileSingleChild(ref _currentChild, widget, Element, _childContext);
            _constraints = constraints;
            _hasConstraints = true;
        }

        private static float NormalizeDimension(float value) {
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f
                ? 0f
                : value;
        }

        private bool HasObservedAxisChanged(LayoutConstraints constraints) =>
            HasObservedAxisChanged(constraints, ((LayoutBuilder)Widget).Axis);

        private bool HasObservedAxisChanged(LayoutConstraints constraints, Axis axis) {
            if (!_hasConstraints) {
                return true;
            }

            return axis == Axis.Horizontal
                ? !Mathf.Approximately(_constraints.MaxWidth, constraints.MaxWidth)
                : !Mathf.Approximately(_constraints.MaxHeight, constraints.MaxHeight);
        }

        private LayoutConstraints ReadConstraints() {
            var constraints = ReadConstraints(Element.contentRect);
            var mediaQuery = Context.MediaQuery;
            return ((LayoutBuilder)Widget).Axis == Axis.Horizontal
                ? new LayoutConstraints(
                    constraints.MaxWidth > 0f ? constraints.MaxWidth : mediaQuery.Width,
                    constraints.MaxHeight)
                : new LayoutConstraints(
                    constraints.MaxWidth,
                    constraints.MaxHeight > 0f ? constraints.MaxHeight : mediaQuery.Height);
        }

        private static LayoutConstraints ReadConstraints(Rect rect) {
            return new LayoutConstraints(
                NormalizeDimension(rect.width),
                NormalizeDimension(rect.height));
        }

        protected override void AppendDiagnosticProperties(System.Collections.Generic.IDictionary<string, string> properties) {
            if (!_hasConstraints) return;
            properties["constraints.maxWidth"] = LayoutDiagnosticProperties.Format(_constraints.MaxWidth);
            properties["constraints.maxHeight"] = LayoutDiagnosticProperties.Format(_constraints.MaxHeight);
        }
    }

}
