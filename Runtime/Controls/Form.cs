#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumaFlow {

    public enum FormValidationMode {
        OnSubmit,
        OnChange
    }

    /// <summary>Owns validation for the fields mounted below one <see cref="Form"/> scope.</summary>
    public sealed class FormState {
        private readonly List<IFormField> _fields = new();

        public FormState(FormValidationMode validationMode = FormValidationMode.OnSubmit) {
            if (!Enum.IsDefined(typeof(FormValidationMode), validationMode)) {
                throw new ArgumentOutOfRangeException(nameof(validationMode));
            }

            ValidationMode = validationMode;
            IsValid = new State<bool>(true);
        }

        public FormValidationMode ValidationMode { get; }

        /// <summary>Tracks current validity without requiring a submit attempt.</summary>
        public State<bool> IsValid { get; }

        /// <summary>Validates mounted fields and focuses the first invalid input.</summary>
        public bool Validate() {
            return Validate(showErrors: true, focusFirstInvalid: true);
        }

        /// <summary>
        /// Validates all mounted fields and invokes <paramref name="onValid"/> only when
        /// the form is valid. Invalid submission focuses the first invalid input.
        /// </summary>
        public bool Submit(Action onValid) {
            if (onValid is null) throw new ArgumentNullException(nameof(onValid));
            if (!Validate()) return false;
            onValid();
            return true;
        }

        private bool Validate(bool showErrors, bool focusFirstInvalid) {
            IFormField? firstInvalid = null;
            foreach (var field in _fields) {
                var valid = showErrors ? field.Validate() : field.IsValid;
                if (!valid && firstInvalid is null) firstInvalid = field;
            }

            var isValid = firstInvalid is null;
            IsValid.Value = isValid;
            if (!isValid && focusFirstInvalid) firstInvalid!.Focus();
            return isValid;
        }

        /// <summary>Clears validation messages without changing externally owned field values.</summary>
        public void ClearValidation() {
            foreach (var field in _fields) field.ClearError();
        }

        internal IDisposable Register(IFormField field) {
            if (field is null) throw new ArgumentNullException(nameof(field));
            if (_fields.Contains(field)) throw new InvalidOperationException("A form field can be mounted only once in the same Form.");
            _fields.Add(field);
            Validate(showErrors: false, focusFirstInvalid: false);
            var changes = field.SubscribeValueChanged(HandleFieldValueChanged);
            return new Registration(this, field, changes);
        }

        private void HandleFieldValueChanged() {
            Validate(showErrors: ValidationMode == FormValidationMode.OnChange, focusFirstInvalid: false);
        }

        private void Unregister(IFormField field) {
            _fields.Remove(field);
            Validate(showErrors: false, focusFirstInvalid: false);
        }

        private sealed class Registration : IDisposable {
            private FormState? _owner;
            private IFormField? _field;
            private IDisposable? _changes;

            public Registration(FormState owner, IFormField field, IDisposable changes) {
                _owner = owner;
                _field = field;
                _changes = changes;
            }

            public void Dispose() {
                var owner = _owner;
                if (owner is null) return;
                _changes!.Dispose();
                owner.Unregister(_field!);
                _owner = null;
                _field = null;
                _changes = null;
            }
        }
    }

    /// <summary>External value, validator and validation message for one controlled form input.</summary>
    public sealed class FormField<T> : IFormField {
        private Action? _focus;

        public FormField(State<T> value, Func<T, string?>? validator = null) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Validator = validator;
            ErrorText = new State<string?>(null);
        }

        public State<T> Value { get; }
        public Func<T, string?>? Validator { get; }
        public State<string?> ErrorText { get; }

        public bool Validate() {
            ErrorText.Value = Validator?.Invoke(Value.Value);
            return string.IsNullOrEmpty(ErrorText.Value);
        }

        public void ClearError() => ErrorText.Value = null;

        bool IFormField.IsValid => string.IsNullOrEmpty(Validator?.Invoke(Value.Value));
        IDisposable IFormField.SubscribeValueChanged(Action listener) => Value.Subscribe(_ => listener());

        internal IDisposable AttachFocus(Action focus) {
            if (_focus is not null) throw new InvalidOperationException("A FormField can be mounted by only one input at a time.");
            _focus = focus ?? throw new ArgumentNullException(nameof(focus));
            return new FocusRegistration(this, focus);
        }

        void IFormField.Focus() => _focus?.Invoke();
        void IFormField.ClearError() => ClearError();

        private sealed class FocusRegistration : IDisposable {
            private FormField<T>? _owner;
            private readonly Action _focus;

            public FocusRegistration(FormField<T> owner, Action focus) { _owner = owner; _focus = focus; }

            public void Dispose() {
                if (_owner?._focus == _focus) _owner._focus = null;
                _owner = null;
            }
        }
    }

    internal interface IFormField {
        bool Validate();
        bool IsValid { get; }
        IDisposable SubscribeValueChanged(Action listener);
        void Focus();
        void ClearError();
    }

    /// <summary>Scopes a <see cref="FormState"/> to its declarative child without adding a native wrapper.</summary>
    public sealed class Form : Widget {
        public Form(FormState state, Widget child, Action? onSubmit = null) {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Child = child ?? throw new ArgumentNullException(nameof(child));
            OnSubmit = onSubmit;
        }

        public FormState State { get; }
        public Widget Child { get; }
        public Action? OnSubmit { get; }
        internal override WidgetNode CreateNode() => new FormNode(this);
    }

    internal sealed class FormNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;
        private VisualElement? _submitElement;

        public FormNode(Form widget) : base(widget) { }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (Form)Widget;
            _childContext = Context.WithForm(widget.State);
            _currentChild = MountChild(widget.Child, NativeParent, _childContext);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindSubmitHandler(widget);
            Bindings.Add(ReleaseSubmitHandler);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Form form) return false;
            if (!ReferenceEquals(((Form)Widget).State, form.State)) return false;

            ReconcileSingleChild(ref _currentChild, form.Child, NativeParent, _childContext);
            ReleaseSubmitHandler();
            UpdateWidget(form);
            AdoptNativeElement(_currentChild!.NativeElement);
            BindSubmitHandler(form);
            return true;
        }

        internal void HandleKeyDown(KeyDownEvent evt) {
            if (!TrySubmit(evt.keyCode, evt.target as VisualElement)) return;
            evt.StopPropagation();
        }

        private void HandleTextInputSubmit(TextInputSubmitEvent evt) {
            if (!HandleTextInputSubmit()) return;
            evt.StopPropagation();
        }

        internal bool HandleTextInputSubmit() {
            if (!IsMounted) return false;
            var widget = (Form)Widget;
            if (widget.OnSubmit is null) return false;
            widget.State.Submit(widget.OnSubmit);
            return true;
        }

        internal bool TrySubmit(KeyCode keyCode, VisualElement? eventTarget) {
            if (!IsMounted || IsMultilineTextInput(eventTarget)) return false;
            if (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter) return false;

            var widget = (Form)Widget;
            if (widget.OnSubmit is null) return false;
            widget.State.Submit(widget.OnSubmit);
            return true;
        }

        private static bool IsMultilineTextInput(VisualElement? element) {
            while (element is not null) {
                if (element is UnityEngine.UIElements.TextField textField && textField.multiline) {
                    return true;
                }

                element = element.parent;
            }

            return false;
        }

        private void BindSubmitHandler(Form widget) {
            if (widget.OnSubmit is null) return;
            _submitElement = Element;
            // TextField converts the native key into a semantic event before its
            // internal editor can consume the original KeyDownEvent.
            _submitElement.RegisterCallback<TextInputSubmitEvent>(HandleTextInputSubmit);
        }

        private void ReleaseSubmitHandler() {
            _submitElement?.UnregisterCallback<TextInputSubmitEvent>(HandleTextInputSubmit);
            _submitElement = null;
        }
    }

    internal sealed class TextInputSubmitEvent : EventBase<TextInputSubmitEvent> {
        public TextInputSubmitEvent() {
            Init();
        }

        protected override void Init() {
            base.Init();
            bubbles = true;
            tricklesDown = false;
        }
    }

}
