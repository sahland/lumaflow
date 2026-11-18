#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace LumaFlow {
    public sealed class NativeView : IDisposable {
        private static readonly ConditionalWeakTable<VisualElement, List<WeakReference<NativeView>>> Views = new();
        private readonly List<IBinding> _bindings = new();
        private IVisualElementScheduledItem? _pending;
        private bool _refreshing;
        public VisualElement Root { get; }
        public bool IsDisposed { get; private set; }
        public event Action? Disposed;

        public NativeView(VisualElement root) {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Views.GetOrCreateValue(root).Add(new WeakReference<NativeView>(this));
            root.RegisterCallback<AttachToPanelEvent>(OnAttached);
        }

        public static bool IsValidId(string? id) {
            if (id is null || id.Length == 0 || id.Length > 128) return false;
            foreach (var character in id)
                if (!char.IsLetterOrDigit(character) && character is not '_' and not '-' and not '.' and not ':' and not '/') return false;
            return true;
        }

        public T? Q<T>(string id) where T : VisualElement => Find(id) as T;

        public T Require<T>(string id) where T : VisualElement {
            var element = Find(id);
            if (element is T typed) return typed;
            throw new InvalidOperationException(element is null
                ? $"UI element '{id}' is not mounted in this view."
                : $"UI element '{id}' is {element.GetType().Name}, not {typeof(T).Name}.");
        }

        public IDisposable Bind<T>(string id, Action<T> onBind, Action<T> onUnbind) where T : VisualElement {
            ThrowIfDisposed();
            if (!IsValidId(id)) throw new ArgumentException("Invalid element ID.", nameof(id));
            if (onBind is null) throw new ArgumentNullException(nameof(onBind));
            if (onUnbind is null) throw new ArgumentNullException(nameof(onUnbind));
            var binding = new Binding<T>(this, id, onBind, onUnbind);
            _bindings.Add(binding);
            try { binding.Refresh(); }
            catch { binding.Dispose(); throw; }
            return binding;
        }

        public IDisposable OnClick(string id, Action clicked) {
            if (clicked is null) throw new ArgumentNullException(nameof(clicked));
            return Bind<UnityEngine.UIElements.Button>(id, button => button.clicked += clicked, button => button.clicked -= clicked);
        }

        public IDisposable OnValueChanged<TValue>(string id, EventCallback<ChangeEvent<TValue>> changed) {
            if (changed is null) throw new ArgumentNullException(nameof(changed));
            EventCallback<ChangeEvent<TValue>>? listener = null;
            return Bind<VisualElement>(id, element => {
                if (element is not INotifyValueChanged<TValue>) throw new InvalidOperationException($"UI element '{id}' does not expose values of type {typeof(TValue).Name}.");
                listener = evt => { if (ReferenceEquals(evt.target, element)) changed(evt); };
                element.RegisterCallback(listener);
            }, element => {
                if (listener is not null) element.UnregisterCallback(listener);
                listener = null;
            });
        }

        public void Refresh() {
            ThrowIfDisposed();
            if (_refreshing) throw new InvalidOperationException("NativeView.Refresh cannot run recursively from a binding callback.");
            _pending?.Pause(); _pending = null;
            _refreshing = true;
            List<Exception>? failures = null;
            try {
                foreach (var binding in _bindings.ToArray()) {
                    try { binding.Refresh(); }
                    catch (Exception error) { (failures ??= new List<Exception>()).Add(error); }
                }
            } finally { _refreshing = false; }
            if (failures is not null) throw new AggregateException("UI bindings failed to refresh.", failures);
        }

        public void Dispose() {
            if (IsDisposed) return;
            IsDisposed = true;
            _pending?.Pause(); _pending = null;
            Root.UnregisterCallback<AttachToPanelEvent>(OnAttached);
            if (Views.TryGetValue(Root, out var views))
                views.RemoveAll(reference => !reference.TryGetTarget(out var view) || ReferenceEquals(view, this));
            List<Exception>? failures = null;
            foreach (var binding in _bindings.ToArray()) {
                try { binding.Dispose(); }
                catch (Exception error) { (failures ??= new List<Exception>()).Add(error); }
            }
            _bindings.Clear();
            var disposed = Disposed;
            Disposed = null;
            if (disposed is not null) foreach (Action callback in disposed.GetInvocationList()) {
                try { callback(); }
                catch (Exception error) { (failures ??= new List<Exception>()).Add(error); }
            }
            if (failures is not null) throw new AggregateException("UI bindings failed to dispose.", failures);
        }

        internal static void NotifyChanged(VisualElement element) {
            for (var current = element; current is not null; current = current.parent) {
                if (!Views.TryGetValue(current, out var views)) continue;
                for (var i = views.Count - 1; i >= 0; i--) {
                    if (views[i].TryGetTarget(out var view)) view.ScheduleRefresh();
                    else views.RemoveAt(i);
                }
            }
        }

        private VisualElement? Find(string id) {
            ThrowIfDisposed();
            if (!IsValidId(id)) throw new ArgumentException("Invalid element ID.", nameof(id));
            var matches = Root.Query<VisualElement>(name: id).ToList();
            if (matches.Count > 1) throw new InvalidOperationException($"Duplicate UI ID '{id}' in this view. Use unique IDs or query a narrower component root.");
            return matches.Count == 0 ? null : matches[0];
        }
        private void ScheduleRefresh() {
            if (IsDisposed || _bindings.Count == 0 || _pending is not null) return;
            _pending = Root.schedule.Execute(() => { _pending = null; if (!IsDisposed) Refresh(); });
        }
        private void OnAttached(AttachToPanelEvent _) => ScheduleRefresh();
        private void ThrowIfDisposed() { if (IsDisposed) throw new ObjectDisposedException(nameof(NativeView)); }
        private interface IBinding : IDisposable { void Refresh(); }
        private sealed class Binding<T> : IBinding where T : VisualElement {
            private readonly NativeView _view;
            private readonly string _id;
            private readonly Action<T> _bind, _unbind;
            private T? _element;
            private bool _disposed;
            internal Binding(NativeView view, string id, Action<T> bind, Action<T> unbind) {
                _view = view; _id = id; _bind = bind; _unbind = unbind;
            }
            public void Refresh() {
                if (_disposed) return;
                VisualElement? found;
                try { found = _view.Find(_id); }
                catch { Unbind(); throw; }
                if (found is not null && found is not T) {
                    Unbind();
                    throw new InvalidOperationException($"UI element '{_id}' is {found.GetType().Name}, not {typeof(T).Name}.");
                }
                var next = found as T;
                if (ReferenceEquals(next, _element)) return;
                Unbind();
                if (next is null) return;
                _element = next;
                try { _bind(next); }
                catch { Unbind(); throw; }
            }
            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                _view._bindings.Remove(this);
                Unbind();
            }
            private void Unbind() {
                var previous = _element;
                _element = null;
                if (previous is not null) _unbind(previous);
            }
        }
    }
}
