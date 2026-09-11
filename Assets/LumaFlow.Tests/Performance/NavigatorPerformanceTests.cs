#nullable enable

using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.UIElements;

namespace LumaFlow.Performance.Tests {

    public sealed class NavigatorPerformanceTests {
        private const int OperationBatchSize = 16;
        private const int RetainedHistorySize = 32;
        private const int WarmupCount = 5;
        private const int MeasurementCount = 20;

        [Test, Performance, Version("1")]
        public void NavigatorHost_MountBatch() {
            VisualElement[] roots = null!;
            NavigatorHost[] hosts = null!;
            MountHandle[] mounts = null!;

            Measure.Method(() =>
                {
                    for (var index = 0; index < OperationBatchSize; index++) {
                        mounts[index] = global::LumaFlow.LumaFlow.Mount(hosts[index], roots[index]);
                    }
                })
                .SampleGroup("Navigator.Mount.16RoutesBatch")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    roots = new VisualElement[OperationBatchSize];
                    hosts = new NavigatorHost[OperationBatchSize];
                    mounts = new MountHandle[OperationBatchSize];
                    for (var index = 0; index < OperationBatchSize; index++) {
                        roots[index] = new VisualElement();
                        hosts[index] = new NavigatorHost(new Text($"Route {index}"));
                    }
                })
                .CleanUp(() =>
                {
                    for (var index = 0; index < OperationBatchSize; index++) {
                        Assert.That(roots[index].childCount, Is.EqualTo(1));
                        mounts[index].Dispose();
                        Assert.That(roots[index].childCount, Is.Zero);
                    }
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void Navigator_PushRetainedHistoryBatch() {
            var root = (VisualElement)null!;
            var host = (VisualElement)null!;
            var navigator = (Navigator)null!;
            var mount = (MountHandle)null!;
            Widget[] routes = null!;

            Measure.Method(() =>
                {
                    for (var index = 0; index < OperationBatchSize; index++) {
                        navigator.Push(routes[index]);
                    }
                })
                .SampleGroup("Navigator.Push.16RetainedRoutesBatch")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    root = new VisualElement();
                    navigator = new Navigator(new Text("Root"));
                    mount = global::LumaFlow.LumaFlow.Mount(new NavigatorHost(navigator), root);
                    host = root[0][0];
                    routes = CreateRoutes(OperationBatchSize);
                })
                .CleanUp(() =>
                {
                    Assert.That(navigator.Depth, Is.EqualTo(OperationBatchSize + 1));
                    Assert.That(host.childCount, Is.EqualTo(OperationBatchSize + 1));
                    mount.Dispose();
                    Assert.That(host.childCount, Is.Zero);
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void Navigator_PopRetainedHistoryBatch() {
            var root = (VisualElement)null!;
            var host = (VisualElement)null!;
            var navigator = (Navigator)null!;
            var mount = (MountHandle)null!;

            Measure.Method(() =>
                {
                    for (var index = 0; index < OperationBatchSize; index++) {
                        navigator.Pop();
                    }
                })
                .SampleGroup("Navigator.Pop.16RetainedRoutesBatch")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    root = new VisualElement();
                    navigator = new Navigator(new Text("Root"));
                    mount = global::LumaFlow.LumaFlow.Mount(new NavigatorHost(navigator), root);
                    host = root[0][0];
                    var routes = CreateRoutes(OperationBatchSize);
                    for (var index = 0; index < routes.Length; index++) navigator.Push(routes[index]);
                })
                .CleanUp(() =>
                {
                    Assert.That(navigator.Depth, Is.EqualTo(1));
                    Assert.That(host.childCount, Is.EqualTo(1));
                    mount.Dispose();
                    Assert.That(host.childCount, Is.Zero);
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void NavigatorHost_DisposeRetainedHistory() {
            var root = (VisualElement)null!;
            var host = (VisualElement)null!;
            var mount = (MountHandle)null!;

            Measure.Method(() => mount.Dispose())
                .SampleGroup("Navigator.Dispose.32RetainedRoutes")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    root = new VisualElement();
                    var navigator = new Navigator(new Text("Root"));
                    mount = global::LumaFlow.LumaFlow.Mount(new NavigatorHost(navigator), root);
                    host = root[0][0];
                    var routes = CreateRoutes(RetainedHistorySize - 1);
                    for (var index = 0; index < routes.Length; index++) navigator.Push(routes[index]);
                    Assert.That(host.childCount, Is.EqualTo(RetainedHistorySize));
                })
                .CleanUp(() =>
                {
                    Assert.That(mount.IsMounted, Is.False);
                    Assert.That(root.childCount, Is.Zero);
                    Assert.That(host.childCount, Is.Zero);
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void Navigator_RestoreSnapshotWithRetainedHistory() {
            var snapshot = (NavigationSnapshot)null!;
            var root = (VisualElement)null!;
            var mount = (MountHandle)null!;
            var restored = (Navigator)null!;

            Measure.Method(() => {
                    restored = new Navigator(snapshot);
                    mount = global::LumaFlow.LumaFlow.Mount(new NavigatorHost(restored), root);
                })
                .SampleGroup("Navigator.Restore.32RetainedRoutes")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() => {
                    var source = new Navigator(new Route(new WidgetKey("route-0"), new Text("Root")));
                    var sourceRoot = new VisualElement();
                    using (global::LumaFlow.LumaFlow.Mount(new NavigatorHost(source), sourceRoot)) {
                        for (var index = 1; index < RetainedHistorySize; index++) {
                            source.Push(new Route(new WidgetKey($"route-{index}"), new Text($"Route {index}")));
                        }
                        snapshot = source.CaptureSnapshot();
                    }
                    root = new VisualElement();
                })
                .CleanUp(() => {
                    Assert.That(restored.Depth, Is.EqualTo(RetainedHistorySize));
                    mount.Dispose();
                    Assert.That(root.childCount, Is.Zero);
                })
                .Run();
        }

        [Test, Performance, Version("1")]
        public void Overlay_OpenAndCloseModalBatch() {
            var root = (VisualElement)null!;
            var controller = (OverlayController)null!;
            var mount = (MountHandle)null!;
            OverlayHandle[] handles = null!;

            Measure.Method(() => {
                    for (var index = 0; index < OperationBatchSize; index++) {
                        handles[index] = controller.ShowModal(new Text($"Modal {index}"));
                    }
                    for (var index = OperationBatchSize - 1; index >= 0; index--) handles[index].Close();
                })
                .SampleGroup("Overlay.ModalOpenClose.16EntriesBatch")
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SetUp(() => {
                    root = new VisualElement();
                    controller = new OverlayController();
                    mount = global::LumaFlow.LumaFlow.Mount(new OverlayHost(new Text("Content"), controller), root);
                    handles = new OverlayHandle[OperationBatchSize];
                })
                .CleanUp(() => {
                    Assert.That(controller.HasOpenEntries, Is.False);
                    mount.Dispose();
                })
                .Run();
        }

        private static Widget[] CreateRoutes(int count) {
            var routes = new Widget[count];
            for (var index = 0; index < count; index++) routes[index] = new Text($"Route {index}");
            return routes;
        }
    }

}
