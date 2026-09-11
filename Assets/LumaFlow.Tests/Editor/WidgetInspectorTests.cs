#nullable enable

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;

namespace LumaFlow.Editor.Tests {

    public sealed class WidgetInspectorTests {
        [Test]
        public void WidgetInspector_RendersEmptyStateAndMountedSnapshot() {
            var window = ScriptableObject.CreateInstance<LumaFlowWidgetInspector>();
            try {
                window.Render(Array.Empty<WidgetTreeDiagnostics>());
                Assert.That(
                    window.rootVisualElement.Q<Label>(LumaFlowWidgetInspector.EmptyStateName),
                    Is.Not.Null);

                var native = new VisualElement { name = "diagnostic-native" };
                using var mount = Framework.Mount(new Native(native), new VisualElement());
                var snapshot = mount.CaptureDiagnostics();
                window.Render(new[] { snapshot });

                Assert.That(
                    window.rootVisualElement.Q<Foldout>(LumaFlowWidgetInspector.MountFoldoutName),
                    Is.Not.Null);
                Assert.That(
                    window.rootVisualElement.Q<Label>(LumaFlowWidgetInspector.WidgetRowName).text,
                    Does.Contain("Native"));
                var finding = window.rootVisualElement.Q<Label>(LumaFlowWidgetInspector.FindingName);
                Assert.That(finding, Is.Not.Null);
                Assert.That(finding.text, Does.Contain("LF1002"));
            } finally {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
