#nullable enable

using System;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.UIElements;

namespace LumaFlow.Performance.Tests {

    public sealed class AnimationPerformanceTests {
        private const int DriverOperationCount = 10_000;
        private const int BuilderCount = 16;
        private const int FrameCount = 60;
        private const int WarmupCount = 5;
        private const int MeasurementCount = 20;

        [Test, Performance, Version("1")]
        public void ImplicitAnimation_SampleAndRetargetBatch() {
            var animation = new ImplicitAnimation<float>();
            var tween = new FloatTween(0f, 1f);
            var spec = new AnimationSpec(TimeSpan.FromMilliseconds(250), Curves.EaseInOut);

            Measure.Method(() => {
                    for (var index = 0; index < DriverOperationCount; index++) {
                        var start = index;
                        animation.Start(0f, 1f, tween, spec, start, disableAnimations: false);
                        animation.Sample(start + 0.125d);
                        animation.Sample(start + 0.25d);
                    }
                })
                .SampleGroup("Animation.Driver.10000StartAndTwoSamples")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .Run();

            Assert.That(animation.Current, Is.EqualTo(1f));
            Assert.That(animation.IsRunning, Is.False);
        }

        [Test, Performance, Version("1")]
        public void TweenAnimationBuilder_RenderSixteenWidgetsForSixtyFrames() {
            VisualElement[] roots = null!;
            TweenAnimationBuilderNode<float>[] nodes = null!;
            double[] starts = null!;
            TweenAnimationBuilder<float>[] widgets = null!;

            Measure.Method(() => {
                    for (var index = 0; index < BuilderCount; index++) {
                        nodes[index] = (TweenAnimationBuilderNode<float>)widgets[index].CreateNode();
                        nodes[index].Mount(null, new BuildContext(), roots[index]);
                        starts[index] = nodes[index].AnimationStartedAt;
                    }
                    for (var frame = 1; frame <= FrameCount; frame++) {
                        var offset = frame / (double)FrameCount;
                        for (var index = 0; index < BuilderCount; index++) {
                            nodes[index].TickAt(starts[index] + offset);
                        }
                    }
                    for (var index = 0; index < BuilderCount; index++) nodes[index].Unmount();
                })
                .SampleGroup("Animation.TweenBuilder.16MountSchedule60FramesAndUnmount")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() => {
                    roots = new VisualElement[BuilderCount];
                    nodes = new TweenAnimationBuilderNode<float>[BuilderCount];
                    starts = new double[BuilderCount];
                    widgets = new TweenAnimationBuilder<float>[BuilderCount];
                    for (var index = 0; index < BuilderCount; index++) {
                        roots[index] = new VisualElement();
                        widgets[index] = new TweenAnimationBuilder<float>(
                            new FloatTween(0f, 1f),
                            TimeSpan.FromSeconds(1),
                            value => new SizedBox(new Text("Animated"), width: 100f + value));
                    }
                })
                .CleanUp(() => {
                    for (var index = 0; index < BuilderCount; index++) {
                        Assert.That(nodes[index].CurrentValue, Is.EqualTo(1f));
                        Assert.That(roots[index].childCount, Is.Zero);
                    }
                })
                .Run();
        }
    }
}
