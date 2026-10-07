using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LumaFlow.Runtime.Tests.Controls {

    public sealed class DiscreteSliderTests {
        [Test]
        public void DragUsesInsetsAndMagnetizesNearStops() {
            var value = new State<string>("early");
            var starts = new List<string>();
            var changes = new List<string>();
            var ends = new List<string>();
            var thumbStates = new List<WidgetStates>();
            var node = (DiscreteSliderNode<string>)new DiscreteSlider<string>(
                value,
                new[] {
                    new DiscreteSliderItem<string>("early", "03:00"),
                    new DiscreteSliderItem<string>("noon", "12:00"),
                    new DiscreteSliderItem<string>("late", "23:00")
                },
                (selected, states) => {
                    thumbStates.Add(states);
                    return new Text(selected);
                },
                startInset: 20f,
                endInset: 20f,
                snapThreshold: 0.025f,
                onChanged: changes.Add,
                onChangeStart: starts.Add,
                onChangeEnd: ends.Add).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            node.SetAvailableWidth(200f);

            Assert.That(node.HandlePointerDown(4, 20f), Is.True);
            Assert.That(node.HandlePointerMove(4, 90f), Is.True);
            Assert.That(value.Value, Is.EqualTo("noon"));
            var thumb = node.NativeElement[node.NativeElement.childCount - 1];
            Assert.That(thumb.style.left.value.value, Is.EqualTo(90f).Within(0.001f));

            node.HandlePointerMove(4, 99f);
            Assert.That(thumb.style.left.value.value, Is.EqualTo(100f).Within(0.001f));
            node.HandlePointerUp(4, 99f);

            Assert.That(starts, Is.EqualTo(new[] { "early" }));
            Assert.That(changes, Is.EqualTo(new[] { "noon" }));
            Assert.That(ends, Is.EqualTo(new[] { "noon" }));
            Assert.That(thumbStates.Exists(states => (states & WidgetStates.Dragged) != 0), Is.True);
            node.Unmount();
        }

        [Test]
        public void ProgrammaticValueAndKeyboardSelectionStayControlled() {
            var value = new State<int>(1);
            var changed = new List<int>();
            var node = (DiscreteSliderNode<int>)new DiscreteSlider<int>(
                value,
                new[] {
                    new DiscreteSliderItem<int>(1, "One"),
                    new DiscreteSliderItem<int>(2, "Two"),
                    new DiscreteSliderItem<int>(3, "Three")
                },
                (selected, _) => new Text(selected.ToString()),
                onChanged: changed.Add).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            node.SetAvailableWidth(100f);
            Assert.That(node.NativeElement.style.height.value.value, Is.EqualTo(48f));

            value.Value = 3;
            var thumb = node.NativeElement[node.NativeElement.childCount - 1];
            Assert.That(thumb.style.left.value.value, Is.EqualTo(100f).Within(0.001f));
            Assert.That(changed, Is.Empty);

            Assert.That(node.HandleStep(-1), Is.True);
            Assert.That(value.Value, Is.EqualTo(2));
            Assert.That(changed, Is.EqualTo(new[] { 2 }));
            node.Unmount();
        }

        [Test]
        public void CompatibleRebuildPreservesActiveDragAndUsesLatestCallbacks() {
            var value = new State<int>(1);
            var oldChanges = new List<int>();
            var latestChanges = new List<int>();
            var latestEnds = new List<int>();
            var items = new[] {
                new DiscreteSliderItem<int>(1, "One"),
                new DiscreteSliderItem<int>(2, "Two"),
                new DiscreteSliderItem<int>(3, "Three")
            };
            var node = (DiscreteSliderNode<int>)new DiscreteSlider<int>(
                value,
                items,
                (selected, _) => new Text(selected.ToString()),
                onChanged: oldChanges.Add).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            node.SetAvailableWidth(100f);

            Assert.That(node.HandlePointerDown(12, 0f), Is.True);
            Assert.That(node.TryUpdate(new DiscreteSlider<int>(
                value,
                new[] {
                    new DiscreteSliderItem<int>(1, "First"),
                    new DiscreteSliderItem<int>(2, "Second"),
                    new DiscreteSliderItem<int>(3, "Third")
                },
                (selected, _) => new Text(selected.ToString()),
                onChanged: latestChanges.Add,
                onChangeEnd: latestEnds.Add)), Is.True);

            Assert.That(node.HandlePointerMove(12, 100f), Is.True);
            Assert.That(value.Value, Is.EqualTo(3));
            Assert.That(oldChanges, Is.Empty);
            Assert.That(latestChanges, Is.EqualTo(new[] { 3 }));
            Assert.That(node.HandlePointerUp(12, 100f), Is.True);
            Assert.That(latestEnds, Is.EqualTo(new[] { 3 }));
            node.Unmount();
        }

        [Test]
        public void RejectsInvalidItemsAndUnmountStopsInteractionSilently() {
            var value = new State<int>(1);
            Assert.Throws<ArgumentException>(() => new DiscreteSlider<int>(
                value,
                new[] { new DiscreteSliderItem<int>(1, "Only") },
                (selected, _) => new Text(selected.ToString())));
            Assert.Throws<ArgumentException>(() => new DiscreteSlider<int>(
                value,
                new[] {
                    new DiscreteSliderItem<int>(1, "First"),
                    new DiscreteSliderItem<int>(1, "Duplicate")
                },
                (selected, _) => new Text(selected.ToString())));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiscreteSlider<int>(
                new State<int>(1),
                new[] {
                    new DiscreteSliderItem<int>(1, "One"),
                    new DiscreteSliderItem<int>(2, "Two")
                },
                (selected, _) => new Text(selected.ToString()),
                height: 0f));

            var endCount = 0;
            var node = (DiscreteSliderNode<int>)new DiscreteSlider<int>(
                value,
                new[] {
                    new DiscreteSliderItem<int>(1, "One"),
                    new DiscreteSliderItem<int>(2, "Two")
                },
                (selected, _) => new Text(selected.ToString()),
                onChangeEnd: _ => endCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            node.SetAvailableWidth(100f);
            node.HandlePointerDown(9, 0f);

            node.Unmount();

            Assert.That(endCount, Is.Zero);
        }
    }
}
