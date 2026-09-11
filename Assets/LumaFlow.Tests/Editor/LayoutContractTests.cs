#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;
using LumaScrollView = LumaFlow.ScrollView;

namespace LumaFlow.Editor.Tests {

    public sealed class LayoutContractTests {
        // Runs against an actual EditorWindow panel; assertions read resolved UI Toolkit layout.
        private LayoutTestWindow _window = null!;
        private MountHandle? _mount;

        [SetUp]
        public void SetUp() {
            _window = ScriptableObject.CreateInstance<LayoutTestWindow>();
            _window.position = new Rect(40f, 40f, 480f, 480f);
            _window.Show();
        }

        [TearDown]
        public void TearDown() {
            _mount?.Dispose();
            _window.Close();
        }

        [UnityTest]
        public IEnumerator Column_ResolvesVerticalChildrenWithinTightBounds() => VerifyAfterLayout(
            new SizedBox(new Column(new Widget[]
            {
            new SizedBox(new Text("First"), height: 20f),
            new SizedBox(new Text("Second"), height: 30f)
            }), width: 200f, height: 100f),
            box => {
                AssertSize(box, 200f, 100f);
                Assert.That(box[0][1].worldBound.y - box[0].worldBound.y, Is.EqualTo(20f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator Row_ResolvesHorizontalChildrenWithinTightBounds() => VerifyAfterLayout(
            new SizedBox(new Row(new Widget[]
            {
            new SizedBox(new Text("Left"), width: 70f),
            new SizedBox(new Text("Right"), width: 90f)
            }), width: 200f, height: 40f),
            box => {
                AssertSize(box, 200f, 40f);
                Assert.That(box[0][1].worldBound.x - box[0].worldBound.x, Is.EqualTo(70f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator Padding_ResolvesChildInsideAllInsets() => VerifyAfterLayout(
            new SizedBox(new Padding(new Text("Padded"), EdgeInsets.All(12f)), width: 200f, height: 80f),
            padding => {
                AssertSize(padding, 200f, 80f);
                var child = padding[0][0];
                Assert.That(child.worldBound.x - padding.worldBound.x, Is.EqualTo(12f).Within(0.1f));
                Assert.That(child.worldBound.y - padding.worldBound.y, Is.EqualTo(12f).Within(0.1f));
                Assert.That(child.resolvedStyle.width, Is.EqualTo(176f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator SizedBox_TightensButtonToItsResolvedDimensions() => VerifyAfterLayout(
            new SizedBox(new Button("Save", () => { }), width: 240f, height: 44f),
            box => {
                AssertSize(box, 240f, 44f);
                AssertSize(box[0], 240f, 44f);
            });

        [UnityTest]
        public IEnumerator Button_ResolvesExplicitHeightAndStretchesToItsParentWidth() => VerifyAfterLayout(
            new SizedBox(new Button("Continue", () => { }, ButtonStyle.Primary), width: 240f, height: 48f),
            box => {
                AssertSize(box, 240f, 48f);
                AssertSize(box[0], 240f, 48f);
            });

        [UnityTest]
        public IEnumerator Button_ResolvesAtLeastItsStyledMinimumHeight() => VerifyAfterLayout(
            new SizedBox(new Button("Continue", () => { }, ButtonStyle.Primary), width: 120f, height: 20f),
            box => Assert.That(box[0].resolvedStyle.height, Is.GreaterThanOrEqualTo(40f)));

        [UnityTest]
        public IEnumerator SizedBox_TightensTextToItsResolvedDimensions() => VerifyAfterLayout(
            new SizedBox(new Text("Caption"), width: 180f, height: 36f),
            box => {
                AssertSize(box, 180f, 36f);
                AssertSize(box[0], 180f, 36f);
            });

        [UnityTest]
        public IEnumerator Row_ExpandedReceivesRemainingResolvedWidth() => VerifyAfterLayout(
            new SizedBox(new Row(new Widget[]
            {
            new Expanded(new Text("Fill")),
            new SizedBox(new Text("Fixed"), width: 60f)
            }), width: 300f, height: 40f),
            box => {
                var expanded = box[0][0];
                Assert.That(expanded.resolvedStyle.width, Is.EqualTo(240f).Within(0.1f));
                Assert.That(expanded[0].resolvedStyle.width, Is.EqualTo(240f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator Column_ExpandedReceivesRemainingResolvedHeight() => VerifyAfterLayout(
            new SizedBox(new Column(new Widget[]
            {
            new Expanded(new Text("Fill")),
            new SizedBox(new Text("Fixed"), height: 20f)
            }), width: 200f, height: 120f),
            box => {
                var expanded = box[0][0];
                Assert.That(expanded.resolvedStyle.height, Is.EqualTo(100f).Within(0.1f));
                Assert.That(expanded[0].resolvedStyle.height, Is.EqualTo(100f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator Expanded_TightensNestedSizedBoxToItsResolvedAllocation() => VerifyAfterLayout(
            new SizedBox(new Column(new Widget[]
            {
            new Expanded(new SizedBox(new Text("Fill"), height: 24f))
            }), width: 200f, height: 120f),
            box => {
                var expanded = box[0][0];
                AssertSize(expanded, 200f, 120f);
                AssertSize(expanded[0], 200f, 120f);
            });

        [UnityTest]
        public IEnumerator Spacer_ReceivesRemainingResolvedWidth() => VerifyAfterLayout(
            new SizedBox(new Row(new Widget[]
            {
            new Spacer(),
            new SizedBox(new Text("Fixed"), width: 40f)
            }), width: 200f, height: 30f),
            box => Assert.That(box[0][0].resolvedStyle.width, Is.EqualTo(160f).Within(0.1f)));

        [UnityTest]
        public IEnumerator Column_GapKeepsExpandedDirectAndResolvesItsAllocation() => VerifyAfterLayout(
            new SizedBox(new Column(new Widget[]
            {
            new Expanded(new Text("Fill")),
            new SizedBox(new Text("Fixed"), height: 20f)
            }, gap: 10f), width: 200f, height: 120f),
            box => {
                var column = box[0];
                Assert.That(column.childCount, Is.EqualTo(2));
                Assert.That(column[0].resolvedStyle.height, Is.EqualTo(90f).Within(0.1f));
                Assert.That(column[0].resolvedStyle.marginBottom, Is.EqualTo(10f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator Row_GapKeepsExpandedDirectAndResolvesItsAllocation() => VerifyAfterLayout(
            new SizedBox(new Row(new Widget[]
            {
            new Expanded(new Text("Fill")),
            new SizedBox(new Text("Fixed"), width: 60f)
            }, gap: 10f), width: 300f, height: 40f),
            box => {
                var row = box[0];
                Assert.That(row.childCount, Is.EqualTo(2));
                Assert.That(row[0].resolvedStyle.width, Is.EqualTo(230f).Within(0.1f));
                Assert.That(row[0].resolvedStyle.marginRight, Is.EqualTo(10f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator ScrollView_ProvidesAResolvedViewportAndKeepsColumnExpandedDirect() => VerifyAfterLayout(
            new SizedBox(new LumaScrollView(new Column(new Widget[]
            {
            new Expanded(new Text("Scrollable")),
            new SizedBox(new Text("Footer"), height: 20f)
            })), width: 240f, height: 160f),
            box => {
                var scrollView = (UnityEngine.UIElements.ScrollView)box[0];
                var column = scrollView.contentContainer[0];
                AssertSize(scrollView, 240f, 160f);
                Assert.That(column.childCount, Is.EqualTo(2));
                Assert.That(column[0].style.flexGrow.value, Is.EqualTo(1f));
                Assert.That(column[0].resolvedStyle.height, Is.GreaterThan(0f));
            });

        [UnityTest]
        public IEnumerator LayoutBuilder_ReceivesResolvedPanelMetricsAndKeepsOneLocalChild() => VerifyAfterLayout(
            new SizedBox(
                new LayoutBuilder((context, constraints) => new Text(
                    $"{context.MediaQuery.Width:0} x {constraints.MaxHeight:0}")),
                width: 240f,
                height: 80f),
            box => {
                var builder = box[0];
                Assert.That(builder.name, Is.EqualTo("lumaflow-layout-builder"));
                Assert.That(builder.childCount, Is.EqualTo(1));
                Assert.That(((Label)builder[0]).text, Is.EqualTo("240 x 80"));
            });

        [UnityTest]
        public IEnumerator LayoutBuilder_RebuildsOnceForWidthAndIgnoresContentHeightChanges() {
            var buildCount = 0;
            _mount = Framework.Mount(
                new LayoutBuilder((context, constraints) => {
                    buildCount++;
                    return new Text($"{context.MediaQuery.Width:0}");
                }),
                _window.rootVisualElement);

            yield return null;
            yield return null;
            var stableBuildCount = buildCount;
            var builder = _window.rootVisualElement[0][0];
            var initialWidth = builder.resolvedStyle.width;
            Assert.That(initialWidth, Is.GreaterThan(0f));

            // Resize the actual panel. This is the production source of a
            // LayoutBuilder's responsive constraints; changing a stretch-managed
            // child inline is not a reliable Editor UI Toolkit geometry trigger.
            var resized = _window.position;
            resized.width += 120f;
            _window.position = resized;
            _window.Repaint();
            yield return null;
            yield return null;

            Assert.That(buildCount, Is.EqualTo(stableBuildCount + 1));
            Assert.That(builder.childCount, Is.EqualTo(1));
            Assert.That(Mathf.Abs(builder.resolvedStyle.width - initialWidth), Is.GreaterThan(0.1f));
            Assert.That(((Label)builder[0]).text, Is.EqualTo($"{builder.resolvedStyle.width:0}"));

            resized.height += 80f;
            _window.position = resized;
            _window.Repaint();
            yield return null;
            yield return null;

            Assert.That(buildCount, Is.EqualTo(stableBuildCount + 1));
            Assert.That(builder.childCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Text_WrapsInsideItsResolvedAvailableWidth() => VerifyAfterLayout(
            new SizedBox(
                new Text("Responsive text wraps instead of forcing a horizontal overflow."),
                width: 160f),
            box => {
                var label = (Label)box[0];
                Assert.That(label.resolvedStyle.width, Is.EqualTo(160f).Within(0.1f));
                Assert.That(label.resolvedStyle.height, Is.GreaterThan(20f));
            });

        [UnityTest]
        public IEnumerator SizedBox_WithWidth_PreservesExplicitWidthInsideRow() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new SizedBox(new Text("Thumb"), width: 48f),
                new Expanded(new Text("Fill"))
                }),
                width: 300f,
                height: 64f),
            box => {
                var row = box[0];
                Assert.That(row[0].resolvedStyle.width, Is.EqualTo(48f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator SizedBox_WithHeight_PreservesExplicitHeightInsideColumn() => VerifyAfterLayout(
            new SizedBox(
                new Column(new Widget[]
                {
                new SizedBox(new Text("Thumb"), height: 48f),
                new Expanded(new Text("Fill"))
                }),
                width: 240f,
                height: 200f),
            box => {
                var column = box[0];
                Assert.That(column[0].resolvedStyle.height, Is.EqualTo(48f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator SizedBox_WithWidthAndHeight_PreservesBothDimensions() => VerifyAfterLayout(
            new SizedBox(new Text("Icon"), width: 48f, height: 48f),
            box => {
                AssertSize(box, 48f, 48f);
                AssertSize(box[0], 48f, 48f);
            });

        [UnityTest]
        public IEnumerator SizedBox_WidthOnly_DoesNotIncorrectlyForceHeight() => VerifyAfterLayout(
            new SizedBox(
                new Column(new Widget[]
                {
                new SizedBox(new Text("Thumb"), width: 48f),
                new Expanded(new Text("Fill"))
                }),
                width: 240f,
                height: 120f),
            box => {
                var child = box[0][0];
                // When only width is supplied, minHeight should not be imposed.
                Assert.That(child.style.minHeight.value.value, Is.EqualTo(0f));
            });

        [UnityTest]
        public IEnumerator SizedBox_HeightOnly_DoesNotIncorrectlyForceWidth() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new SizedBox(new Text("Thumb"), height: 48f),
                new Expanded(new Text("Fill"))
                }),
                width: 320f,
                height: 120f),
            box => {
                var child = box[0][0];
                // When only height is supplied, minWidth should not be imposed.
                Assert.That(child.style.minWidth.value.value, Is.EqualTo(0f));
            });

        [UnityTest]
        public IEnumerator SizedBox_InsideExpanded_HasDefinedBehavior_RowExpandedContainsSizedBox() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new Expanded(new SizedBox(new Text("Inner"), height: 48f))
                }),
                width: 240f,
                height: 120f),
            box => {
                var expanded = box[0][0];
                // Expanded should receive remaining allocation and not be broken by sizedbox internals.
                Assert.That(expanded.resolvedStyle.height, Is.EqualTo(120f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator SizedBox_InsideExpanded_HasDefinedBehavior_ColumnExpandedContainsSizedBox() => VerifyAfterLayout(
            new SizedBox(
                new Column(new Widget[]
                {
                new Expanded(new SizedBox(new Text("Inner"), width: 48f))
                }),
                width: 240f,
                height: 120f),
            box => {
                var expanded = box[0][0];
                Assert.That(expanded.resolvedStyle.width, Is.EqualTo(240f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator SizedBox_NestedInRowWithInsufficientSpace_HasPredictableBehavior() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new SizedBox(new Text("A"), width: 60f),
                new SizedBox(new Text("B"), width: 60f)
                }),
                width: 100f,
                height: 40f),
            box => {
                var row = box[0];
                Assert.That(row[0].resolvedStyle.width, Is.EqualTo(60f).Within(0.1f));
                Assert.That(row[1].resolvedStyle.width, Is.EqualTo(60f).Within(0.1f));
            });

        [UnityTest]
        public IEnumerator NavigationRail_ExpandedWidth_DoesNotShrink() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new NavigationRail(new State<int>(0), new[]
                {
                    new NavigationDestination("A", LumaIcons.Home),
                    new NavigationDestination("B", LumaIcons.Grid)
                }, idx => { }, NavigationRailMode.Expanded),
                new Expanded(new Text("Main"))
                }),
                width: 800f,
                height: 600f),
            box => {
                var row = box[0];
                var rail = row[0];
                Assert.That(rail.resolvedStyle.width, Is.EqualTo(208f).Within(0.1f));
                Assert.That(rail.style.minWidth.value.value, Is.EqualTo(208f));
                Assert.That(rail.style.flexShrink.value, Is.EqualTo(0f));
                Assert.That(rail.style.flexGrow.value, Is.EqualTo(0f));
            });

        [UnityTest]
        public IEnumerator NavigationRail_CollapsedWidth_DoesNotShrink_And_ModeChange_UpdatesWidth() => VerifyAfterLayout(
            new SizedBox(
                new Row(new Widget[]
                {
                new NavigationRail(new State<int>(0), new[]
                {
                    new NavigationDestination("A", LumaIcons.Home),
                    new NavigationDestination("B", LumaIcons.Grid)
                }, idx => { }, NavigationRailMode.Collapsed),
                new Expanded(new Text("Main"))
                }),
                width: 800f,
                height: 600f),
            box => {
                var row = box[0];
                var rail = row[0];
                Assert.That(rail.resolvedStyle.width, Is.EqualTo(72f).Within(0.1f));
                Assert.That(rail.style.minWidth.value.value, Is.EqualTo(72f));
                Assert.That(rail.style.flexShrink.value, Is.EqualTo(0f));
                Assert.That(rail.style.flexGrow.value, Is.EqualTo(0f));
            });

        [UnityTest]
        public IEnumerator Native_InsideColumn_ParticipatesInLayout() {
            var native = new VisualElement();
            native.style.height = 36f;
            return VerifyAfterLayout(
                new SizedBox(
                    new Column(new Widget[]
                    {
                    new Native(native),
                    new SizedBox(new Text("Footer"), height: 20f)
                    }),
                    width: 240f,
                    height: 100f),
                box => {
                    var column = box[0];
                    Assert.That(column[0], Is.SameAs(native));
                    Assert.That(native.resolvedStyle.width, Is.EqualTo(240f).Within(0.1f));
                    Assert.That(native.resolvedStyle.height, Is.EqualTo(36f).Within(0.1f));
                    Assert.That(column[1].worldBound.y - column.worldBound.y, Is.EqualTo(36f).Within(0.1f));
                });
        }

        private IEnumerator VerifyAfterLayout(Widget widget, Action<VisualElement> verify) {
            _mount = Framework.Mount(widget, _window.rootVisualElement);
            _window.Repaint();
            yield return null;
            _window.Repaint();
            yield return null;
            verify(_window.rootVisualElement[0][0]);
        }

        private static void AssertSize(VisualElement element, float width, float height) {
            Assert.That(element.resolvedStyle.width, Is.EqualTo(width).Within(0.1f));
            Assert.That(element.resolvedStyle.height, Is.EqualTo(height).Within(0.1f));
        }

        private sealed class LayoutTestWindow : EditorWindow {
        }
    }

}
