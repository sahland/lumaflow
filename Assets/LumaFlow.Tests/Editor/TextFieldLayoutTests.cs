#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;

namespace LumaFlow.Editor.Tests {
    public sealed class TextFieldLayoutTests {
        private SimpleTestWindow _window = null!;
        private MountHandle? _mount;

        [SetUp]
        public void SetUp() {
            _window = ScriptableObject.CreateInstance<SimpleTestWindow>();
            _window.position = new Rect(10f, 10f, 800f, 400f);
            _window.Show();
        }

        [TearDown]
        public void TearDown() {
            _mount?.Dispose();
            _window.Close();
        }

        [UnityTest]
        public IEnumerator TextField_Inside_SizedBox_Maintains_Width_And_Height() {
            var widget = new global::LumaFlow.SizedBox(
                new global::LumaFlow.Row(new global::LumaFlow.Widget[]
                {
                    new global::LumaFlow.SizedBox(new global::LumaFlow.TextField(new global::LumaFlow.State<string>(""), placeholder: "Search projects..."), width: 300f, height: 42f)
                }),
                width: 400f,
                height: 80f);

            _mount = Framework.Mount(widget, _window.rootVisualElement);
            _window.Repaint();
            yield return null;
            _window.Repaint();
            yield return null;

            var host = _window.rootVisualElement[0][0];
            var row = host[0];
            var sized = row[0];

            // Verify sizedbox allocated size
            Assert.That(sized.resolvedStyle.width, Is.EqualTo(300f).Within(0.1f));
            Assert.That(sized.resolvedStyle.height, Is.EqualTo(42f).Within(0.1f));

            // The native TextField element should occupy the same resolved rect
            var native = sized[0];
            Assert.That(native.resolvedStyle.width, Is.EqualTo(300f).Within(0.1f));
            Assert.That(native.resolvedStyle.height, Is.EqualTo(42f).Within(0.1f));

            // Inspect flex and min constraints
            Assert.That(sized.style.flexGrow.value, Is.EqualTo(0f));
            Assert.That(sized.style.flexShrink.value, Is.EqualTo(0f));
            Assert.That(sized.style.minWidth.value.value, Is.EqualTo(300f));
            Assert.That(sized.style.minHeight.value.value, Is.EqualTo(42f));

            // Row properties
            Assert.That(row.style.flexDirection.value, Is.EqualTo(FlexDirection.Row));
            Assert.That(row.resolvedStyle.height, Is.EqualTo(80f).Within(0.1f));

            // UI Toolkit's default Auto inherits Row.alignItems (Stretch),
            // which is why the resolved dimensions above fill the SizedBox.
            Assert.That(native.style.alignSelf.value, Is.EqualTo(global::UnityEngine.UIElements.Align.Auto));
        }
    }

    // Local test window used by this test class.
    internal sealed class SimpleTestWindow : EditorWindow { }
}
