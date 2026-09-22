#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    /// <summary>Marks a parameterless static Widget factory for automatic edit-mode preview.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class LumaPreviewAttribute : Attribute {
        public LumaPreviewAttribute(string? name = null) {
            Name = name;
        }

        public string? Name { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Locale { get; set; }
        public float TextScale { get; set; } = 1f;
    }

    internal sealed class LumaPreviewFactory {
        internal LumaPreviewFactory(MethodInfo method, LumaPreviewAttribute attribute) {
            Method = method;
            Id = $"{method.DeclaringType!.Assembly.GetName().Name}:{method.DeclaringType.FullName}.{method.Name}";
            DisplayName = string.IsNullOrWhiteSpace(attribute.Name)
                ? $"{method.DeclaringType.Name}.{method.Name}"
                : attribute.Name!;
            Environment = new UxmlPreviewEnvironment(
                attribute.Width,
                attribute.Height,
                attribute.Locale,
                attribute.TextScale);
        }

        internal string Id { get; }
        internal string DisplayName { get; }
        internal MethodInfo Method { get; }
        internal UxmlPreviewEnvironment Environment { get; }
        internal Vector2 ViewportSize => Environment.ResolveViewport();

        internal Widget CreateWidget() {
            try {
                var widget = (Widget?)Method.Invoke(null, null)
                    ?? throw new InvalidOperationException($"Preview factory '{Id}' returned null.");
                return Environment.Wrap(widget);
            } catch (TargetInvocationException exception) when (exception.InnerException != null) {
                throw new InvalidOperationException($"Preview factory '{Id}' failed: {exception.InnerException.Message}", exception.InnerException);
            }
        }
    }

    internal static class LumaPreviewRegistry {
        private static IReadOnlyList<LumaPreviewFactory>? _factories;

        internal static IReadOnlyList<LumaPreviewFactory> Factories => _factories ??= Discover();

        internal static LumaPreviewFactory? Find(string? id) => string.IsNullOrEmpty(id)
            ? null
            : Factories.FirstOrDefault(factory => factory.Id == id);

        private static IReadOnlyList<LumaPreviewFactory> Discover() {
            var factories = new List<LumaPreviewFactory>();
            foreach (var method in TypeCache.GetMethodsWithAttribute<LumaPreviewAttribute>()) {
                if (!method.IsStatic || method.IsGenericMethod || method.GetParameters().Length != 0
                    || !typeof(Widget).IsAssignableFrom(method.ReturnType)) {
                    UnityEngine.Debug.LogWarning(
                        $"Ignoring [LumaPreview] method '{method.DeclaringType?.FullName}.{method.Name}'. "
                        + "It must be static, parameterless, non-generic and return Widget.");
                    continue;
                }

                try {
                    factories.Add(new LumaPreviewFactory(method, method.GetCustomAttribute<LumaPreviewAttribute>()!));
                } catch (Exception exception) {
                    UnityEngine.Debug.LogWarning(
                        $"Ignoring [LumaPreview] method '{method.DeclaringType?.FullName}.{method.Name}': {exception.Message}");
                }
            }

            return factories.OrderBy(factory => factory.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(factory => factory.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
