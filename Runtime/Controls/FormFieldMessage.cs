#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>
    /// Registers a non-text input with the nearest <see cref="Form"/> and renders its
    /// validation message below the supplied control.
    /// </summary>
    public sealed class FormFieldMessage<T> : Widget {
        public FormFieldMessage(FormField<T> field, Widget child) {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public FormField<T> Field { get; }
        public Widget Child { get; }
        internal override WidgetNode CreateNode() => new FormFieldMessageNode<T>(this);
    }

    internal sealed class FormFieldMessageNode<T> : WidgetNode {
        private Label? _message;
        private WidgetNode? _currentChild;
        private IDisposable? _errorSubscription;
        private IDisposable? _formRegistration;

        public FormFieldMessageNode(FormFieldMessage<T> widget) : base(widget) { }

        protected override VisualElement CreateElement(BuildContext context) {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexShrink = 1f;
            root.style.minWidth = 0f;
            _message = new Label();
            _message.AddToClassList("lumaflow-form-error");
            _message.style.marginTop = 4f;
            _message.style.display = DisplayStyle.None;
            if (context.Theme?.TextFieldTheme.Style.Error?.Foreground is { } errorColor) {
                _message.style.color = errorColor;
            } else {
                _message.style.color = new Color(1f, 0.40f, 0.45f);
            }
            return root;
        }

        protected override void OnMounted() {
            var widget = (FormFieldMessage<T>)Widget;
            _currentChild = MountChild(widget.Child, Element);
            Element.Add(_message!);
            BindField(widget);
            Bindings.Add(ReleaseField);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not FormFieldMessage<T> message) return false;
            var fieldChanged = !ReferenceEquals(((FormFieldMessage<T>)Widget).Field, message.Field);
            ReconcileSingleChild(ref _currentChild, message.Child, Element);
            if (fieldChanged) ReleaseField();
            UpdateWidget(message);
            if (fieldChanged) BindField(message);
            ReevaluateInheritedDependencies(() =>
            {
                ApplyMessageColor();
                UpdateMessage(message.Field.ErrorText.Value);
                return true;
            });
            return true;
        }

        private void UpdateMessage(string? message) {
            _message!.text = message ?? string.Empty;
            _message.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected override void OnInheritedChanged(InheritedAspect aspect) {
            ApplyMessageColor();
        }

        private void BindField(FormFieldMessage<T> widget) {
            UpdateMessage(widget.Field.ErrorText.Value);
            _errorSubscription = widget.Field.ErrorText.Subscribe(UpdateMessage);
            if (Context.Form is { } form) _formRegistration = form.Register(widget.Field);
        }

        private void ReleaseField() {
            _formRegistration?.Dispose();
            _formRegistration = null;
            _errorSubscription?.Dispose();
            _errorSubscription = null;
        }

        private void ApplyMessageColor() {
            _message!.style.color = Context.Theme?.TextFieldTheme.Style.Error?.Foreground
                ?? new Color(1f, 0.40f, 0.45f);
        }
    }

}
