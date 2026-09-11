#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LumaFlow {

    /// <summary>Provides locale-specific, strongly typed resource objects to a subtree.</summary>
    public sealed class Localizations : Widget {
        public Localizations(Locale locale, Widget child, params object[] resources) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            if (resources is null) throw new ArgumentNullException(nameof(resources));
            Data = new LocalizationData(locale, resources);
        }

        public Locale Locale => Data.Locale;
        public Widget Child { get; }
        internal LocalizationData Data { get; }

        /// <summary>Returns the nearest resource object of type <typeparamref name="T"/>.</summary>
        public static T Of<T>(BuildContext context) where T : class {
            if (context is null) throw new ArgumentNullException(nameof(context));
            return MaybeOf<T>(context) ?? throw new InvalidOperationException(
                $"No localization resource of type {typeof(T).FullName} is available in this BuildContext.");
        }

        /// <summary>Returns the nearest resource object, or null when it is unavailable.</summary>
        public static T? MaybeOf<T>(BuildContext context) where T : class {
            if (context is null) throw new ArgumentNullException(nameof(context));
            return context.ReadLocalizations()?.TryGet<T>();
        }

        /// <summary>Returns the nearest active locale.</summary>
        public static Locale LocaleOf(BuildContext context) {
            if (context is null) throw new ArgumentNullException(nameof(context));
            return context.ReadLocalizations()?.Locale
                ?? throw new InvalidOperationException("No Localizations widget is available in this BuildContext.");
        }

        internal override WidgetNode CreateNode() => new LocalizationsNode(this);
    }

    internal sealed class LocalizationData {
        private readonly Dictionary<Type, object> _resources = new();

        internal LocalizationData(Locale locale, IEnumerable<object> resources) {
            if (string.IsNullOrEmpty(locale.LanguageCode)) throw new ArgumentException("The locale must be initialized.", nameof(locale));
            Locale = locale;
            foreach (var resource in resources) {
                if (resource is null) throw new ArgumentException("Localization resources cannot contain null.", nameof(resources));
                var type = resource.GetType();
                if (!_resources.TryAdd(type, resource)) {
                    throw new ArgumentException($"A localization resource of type {type.FullName} was supplied more than once.", nameof(resources));
                }
            }
        }

        internal Locale Locale { get; }
        internal T? TryGet<T>() where T : class =>
            _resources.TryGetValue(typeof(T), out var resource) ? (T)resource : null;
    }

    internal sealed class LocalizationsNode : WidgetNode, ITransparentWidgetNode {
        private WidgetNode? _currentChild;
        private BuildContext? _childContext;

        internal LocalizationsNode(Localizations widget) : base(widget) { }

        protected override VisualElement? CreateElement(BuildContext context) => null;

        protected override void OnMounted() {
            var widget = (Localizations)Widget;
            _childContext = Context.WithLocalizations(widget.Data);
            _currentChild = MountChild(widget.Child, NativeParent, _childContext);
            AdoptNativeElement(_currentChild.NativeElement);
        }

        internal override bool TryUpdate(Widget nextWidget) {
            if (!CanUpdateWith(nextWidget) || nextWidget is not Localizations localizations) return false;
            var previous = (Localizations)Widget;
            if (!ReferenceEquals(previous.Data, localizations.Data)) {
                _childContext!.UpdateLocalizations(localizations.Data);
            }
            ReconcileSingleChild(ref _currentChild, localizations.Child, NativeParent, _childContext!);
            UpdateWidget(localizations);
            AdoptNativeElement(_currentChild!.NativeElement);
            return true;
        }
    }
}
