#nullable enable

using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace LumaFlow {
    internal sealed class IdentifiedWidget : Widget {
        internal readonly string Id;
        internal readonly Widget Child;
        internal IdentifiedWidget(string id, Widget child) : base(child.Key) {
            if (!NativeView.IsValidId(id)) throw new ArgumentException("An element ID must contain 1–128 letters, digits, '_', '-', '.', ':' or '/'.", nameof(id));
            Id = id; Child = child;
        }
        internal override WidgetNode CreateNode() => new IdentifiedWidgetNode(this);
    }

    internal sealed class IdentifiedWidgetNode : WidgetNode, ITransparentWidgetNode {
        private static readonly ConditionalWeakTable<VisualElement, IdentifiedWidgetNode> Owners = new();
        private WidgetNode? _child;
        private VisualElement? _named;
        private string? _originalName;
        internal static void RestorePublicName(VisualElement? element) {
            if (element != null && Owners.TryGetValue(element, out var owner))
                element.name = ((IdentifiedWidget)owner.Widget).Id;
        }
        internal IdentifiedWidgetNode(IdentifiedWidget widget) : base(widget) { }
        protected override VisualElement? CreateElement(BuildContext context) => null;
        protected override void OnMounted() {
            Bindings.Add(ReleaseName);
            _child = MountChild(((IdentifiedWidget)Widget).Child, NativeParent);
            AdoptNativeElement(_child.NativeElement);
        }
        internal override bool TryUpdate(Widget nextWidget) {
            if (nextWidget is not IdentifiedWidget next || !CanUpdateWith(next)) return false;
            var previous = (IdentifiedWidget)Widget;
            ReconcileSingleChild(ref _child, next.Child, NativeParent);
            AdoptNativeElement(_child!.NativeElement);
            if (previous.Id != next.Id) ReleaseName();
            UpdateWidget(next);
            if (previous.Id != next.Id) ApplyName(NativeElement);
            return true;
        }
        protected override void OnNativeElementChanged(VisualElement? previous, VisualElement current) {
            ReleaseName();
            ApplyName(current);
        }
        private void ApplyName(VisualElement element) {
            if (Owners.TryGetValue(element, out var owner) && !ReferenceEquals(owner, this))
                throw new InvalidOperationException("A native element already has a public ID. Give a composite its own container instead of assigning two IDs to the same native root.");
            Owners.Add(element, this);
            _named = element;
            _originalName = element.name;
            element.name = ((IdentifiedWidget)Widget).Id;
            NativeView.NotifyChanged(NativeParent);
        }
        private void ReleaseName() {
            if (_named is null) return;
            var element = _named;
            _named = null;
            Owners.Remove(element);
            if (element.name == ((IdentifiedWidget)Widget).Id) element.name = _originalName ?? string.Empty;
            _originalName = null;
            NativeView.NotifyChanged(NativeParent);
        }
    }
}
