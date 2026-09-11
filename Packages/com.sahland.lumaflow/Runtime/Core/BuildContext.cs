#nullable enable

using System;
using System.Collections.Generic;

namespace LumaFlow {

    /// <summary>
    /// Represents the tree-scoped environment of a mounted widget.
    /// </summary>
    public sealed class BuildContext {
        private readonly InheritedScope<ThemeData?> _theme;
        private readonly Navigator? _navigator;
        private readonly FormState? _form;
        private readonly FocusTraversalController? _focusTraversal;
        private readonly InheritedScope<MediaQueryData> _mediaQuery;
        private readonly InheritedScope<LocalizationData?> _localizations;
        private readonly InheritedScope<TextScaler> _textScaler;
        private readonly SemanticsOwner _semantics;
        private readonly WidgetNode? _owner;

        internal BuildContext(
            ThemeData? theme = null,
            Navigator? navigator = null,
            FormState? form = null,
            FocusTraversalController? focusTraversal = null,
            MediaQueryData mediaQuery = default,
            SemanticsOwner? semantics = null,
            LocalizationData? localizations = null,
            TextScaler textScaler = default) {
            _theme = new InheritedScope<ThemeData?>(InheritedAspect.Theme, theme);
            _navigator = navigator;
            _form = form;
            _focusTraversal = focusTraversal;
            _mediaQuery = new InheritedScope<MediaQueryData>(InheritedAspect.MediaQuery, mediaQuery);
            _localizations = new InheritedScope<LocalizationData?>(InheritedAspect.Localizations, localizations);
            _textScaler = new InheritedScope<TextScaler>(InheritedAspect.TextScaler, textScaler.OrDefault());
            _semantics = semantics ?? new SemanticsOwner();
        }

        private BuildContext(
            InheritedScope<ThemeData?> theme,
            Navigator? navigator,
            FormState? form,
            FocusTraversalController? focusTraversal,
            InheritedScope<MediaQueryData> mediaQuery,
            InheritedScope<LocalizationData?> localizations,
            InheritedScope<TextScaler> textScaler,
            SemanticsOwner semantics,
            WidgetNode? owner = null) {
            _theme = theme;
            _navigator = navigator;
            _form = form;
            _focusTraversal = focusTraversal;
            _mediaQuery = mediaQuery;
            _localizations = localizations;
            _textScaler = textScaler;
            _semantics = semantics;
            _owner = owner;
        }

        /// <summary>
        /// Gets the theme inherited by the widget currently being mounted.
        /// </summary>
        public ThemeData? Theme {
            get {
                _owner?.RegisterInheritedDependency(_theme);
                return _theme.Value;
            }
        }

        /// <summary>
        /// Gets the available dimensions of the nearest UI panel layout scope.
        /// </summary>
        /// <remarks>
        /// At initial mount the root values can be transitional. Use
        /// <see cref="LayoutBuilder"/> when a subtree must react to resolved size changes.
        /// </remarks>
        public MediaQueryData MediaQuery {
            get {
                _owner?.RegisterInheritedDependency(_mediaQuery);
                return _mediaQuery.Value;
            }
        }

        /// <summary>Gets the scalable-text policy inherited by the current widget.</summary>
        public TextScaler TextScaler {
            get {
                _owner?.RegisterInheritedDependency(_textScaler);
                return _textScaler.Value;
            }
        }

        /// <summary>
        /// Gets the nearest scoped navigator.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when this subtree is not mounted below a <see cref="NavigatorHost" />.</exception>
        public Navigator Navigator => _navigator
            ?? throw new System.InvalidOperationException(
                "No Navigator is available in this BuildContext. Mount the subtree below NavigatorHost.");

        /// <summary>Gets the nearest optional form scope.</summary>
        public FormState? Form => _form;

        internal FocusTraversalController? FocusTraversal => _focusTraversal;

        internal SemanticsOwner Semantics => _semantics;

        // Diagnostic reads must not subscribe the node to inherited updates.
        // Capturing a tree must never change which nodes rebuild later.
        internal ThemeData? DiagnosticTheme => _theme.Value;
        internal MediaQueryData DiagnosticMediaQuery => _mediaQuery.Value;
        internal Locale? DiagnosticLocale => _localizations.Value?.Locale;
        internal TextScaler DiagnosticTextScaler => _textScaler.Value;

        internal BuildContext WithTheme(ThemeData theme) {
            if (theme is null) {
                throw new System.ArgumentNullException(nameof(theme));
            }

            return new BuildContext(
                new InheritedScope<ThemeData?>(InheritedAspect.Theme, theme),
                _navigator,
                _form,
                _focusTraversal,
                _mediaQuery,
                _localizations,
                _textScaler,
                _semantics);
        }

        internal BuildContext WithNavigator(Navigator navigator) {
            if (navigator is null) {
                throw new System.ArgumentNullException(nameof(navigator));
            }

            return new BuildContext(_theme, navigator, _form, _focusTraversal, _mediaQuery, _localizations, _textScaler, _semantics);
        }

        internal BuildContext WithForm(FormState form) {
            if (form is null) throw new System.ArgumentNullException(nameof(form));
            return new BuildContext(_theme, _navigator, form, _focusTraversal, _mediaQuery, _localizations, _textScaler, _semantics);
        }

        internal BuildContext WithFocusTraversal(FocusTraversalController focusTraversal) {
            if (focusTraversal is null) throw new System.ArgumentNullException(nameof(focusTraversal));
            return new BuildContext(_theme, _navigator, _form, focusTraversal, _mediaQuery, _localizations, _textScaler, _semantics);
        }

        internal BuildContext WithMediaQuery(MediaQueryData mediaQuery) {
            return new BuildContext(
                _theme,
                _navigator,
                _form,
                _focusTraversal,
                new InheritedScope<MediaQueryData>(InheritedAspect.MediaQuery, mediaQuery),
                _localizations,
                _textScaler,
                _semantics);
        }

        internal BuildContext WithLocalizations(LocalizationData localizations) {
            if (localizations is null) throw new ArgumentNullException(nameof(localizations));
            return new BuildContext(
                _theme,
                _navigator,
                _form,
                _focusTraversal,
                _mediaQuery,
                new InheritedScope<LocalizationData?>(InheritedAspect.Localizations, localizations),
                _textScaler,
                _semantics);
        }

        internal BuildContext WithTextScaler(TextScaler textScaler) => new(
            _theme,
            _navigator,
            _form,
            _focusTraversal,
            _mediaQuery,
            _localizations,
            new InheritedScope<TextScaler>(InheritedAspect.TextScaler, textScaler.OrDefault()),
            _semantics);

        internal BuildContext ForNode(WidgetNode owner) {
            if (owner is null) throw new ArgumentNullException(nameof(owner));
            return new BuildContext(_theme, _navigator, _form, _focusTraversal, _mediaQuery, _localizations, _textScaler, _semantics, owner);
        }

        internal void UpdateTheme(ThemeData theme) {
            if (theme is null) throw new ArgumentNullException(nameof(theme));
            _theme.Update(theme);
        }

        internal void UpdateMediaQuery(MediaQueryData mediaQuery) => _mediaQuery.Update(mediaQuery);

        internal LocalizationData? ReadLocalizations() {
            _owner?.RegisterInheritedDependency(_localizations);
            return _localizations.Value;
        }

        internal void UpdateLocalizations(LocalizationData localizations) => _localizations.Update(localizations);

        internal void UpdateTextScaler(TextScaler textScaler) => _textScaler.Update(textScaler.OrDefault());
    }

    internal enum InheritedAspect {
        Theme,
        MediaQuery,
        Localizations,
        TextScaler
    }

    internal interface IInheritedScope {
        InheritedAspect Aspect { get; }
        void AddDependent(WidgetNode node);
        void RemoveDependent(WidgetNode node);
    }

    internal sealed class InheritedScope<T> : IInheritedScope {
        private readonly HashSet<WidgetNode> _dependents = new();
        private T _value;

        public InheritedScope(InheritedAspect aspect, T value) {
            Aspect = aspect;
            _value = value;
        }

        public InheritedAspect Aspect { get; }
        public T Value => _value;

        public void AddDependent(WidgetNode node) => _dependents.Add(node);

        public void RemoveDependent(WidgetNode node) => _dependents.Remove(node);

        public void Update(T value) {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            var snapshot = new WidgetNode[_dependents.Count];
            _dependents.CopyTo(snapshot);
            List<Exception>? failures = null;
            foreach (var dependent in snapshot) {
                try {
                    dependent.NotifyInheritedChanged(this);
                } catch (Exception exception) {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures is { Count: 1 }) throw failures[0];
            if (failures is { Count: > 1 }) {
                throw new AggregateException("One or more inherited-context dependents failed during update.", failures);
            }
        }
    }

}
