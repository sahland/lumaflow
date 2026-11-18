#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;
using NativeButton = UnityEngine.UIElements.Button;

namespace LumaFlow.Editor.Tests {
    public sealed class NativeViewTests {
        [Test]
        public void IdNamesTheActualControlWithoutAnotherNativeWrapper() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Button("Parking", () => { }).WithId("parking"), root);
            using var view = new NativeView(root);
            Assert.That(root.Q<NativeButton>("parking"), Is.Not.Null);
            Assert.That(view.Require<NativeButton>("parking"), Is.SameAs(root.Q<NativeButton>("parking")));
            Assert.That(root[0].childCount, Is.EqualTo(1));
            var iconData = new IconData("test", "native-view-test-icon");
            var iconState = new State<Widget>(new Icon(iconData));
            using var iconMount = Framework.Mount(new ReactiveBuilder<Widget>(iconState, widget => widget).WithId("icon"), root);
            var icon = view.Require<UnityEngine.UIElements.Image>("icon");
            iconState.Value = new Icon(iconData, size: 32);
            Assert.That(view.Require<UnityEngine.UIElements.Image>("icon"), Is.SameAs(icon));
        }

        [Test]
        public void QueriesAreTypedScopedAndFailClearlyForMissingAndDuplicateIds() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Column(new Widget[] {
                new Button("A", () => { }).WithId("parking"),
                new Container(new Button("B", () => { }).WithId("parking")).WithId("card")
            }), root);
            using var view = new NativeView(root);
            Assert.Throws<InvalidOperationException>(() => view.Q<NativeButton>("parking"));
            using var scoped = new NativeView(view.Require<VisualElement>("card"));
            Assert.That(scoped.Require<NativeButton>("parking"), Is.Not.Null);
            Assert.That(scoped.Q<Label>("parking"), Is.Null);
            Assert.Throws<InvalidOperationException>(() => scoped.Require<Label>("parking"));
            Assert.Throws<InvalidOperationException>(() => scoped.Require<Label>("missing"));
            Assert.Throws<ArgumentException>(() => new Text("bad").WithId("bad id"));
        }

        [Test]
        public void PublicIdTracksReactiveNativeRootReplacementThroughTransparentParents() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            using var view = new NativeView(root);
            using var mount = Framework.Mount(new Composite(new ReactiveBuilder<bool>(state,
                flag => flag ? new Button("Now", () => { }) : new Text("Before"))).WithId("dynamic"), root);
            Assert.That(view.Require<Label>("dynamic").text, Is.EqualTo("Before"));
            state.Value = true;
            Assert.That(view.Require<NativeButton>("dynamic"), Is.Not.Null);
            Assert.That(root.Query<VisualElement>("dynamic").ToList().Count, Is.EqualTo(1));
        }

        [Test]
        public void ChangingPublicIdRestoresBorrowedNativeNameAndKeepsState() {
            var root = new VisualElement();
            var control = new Label("borrowed") {name = "original"};
            var native = new Native(control);
            var state = new State<Widget>(native.WithId("first"));
            var mount = Framework.Mount(new ReactiveBuilder<Widget>(state, widget => widget), root);
            state.Value = native.WithId("second");
            Assert.That(root.Q<Label>("first"), Is.Null);
            Assert.That(root.Q<Label>("second"), Is.SameAs(control));
            mount.Dispose();
            Assert.That(control.name, Is.EqualTo("original"));
        }

        [Test]
        public void BindingsWaitForMountAndReleaseReplacedRemovedAndDisposedElements() {
            var root = new VisualElement();
            var state = new State<Widget>(new Text("empty"));
            using var mount = Framework.Mount(new ReactiveBuilder<Widget>(state, value => value), root);
            var bound = 0; var unbound = 0;
            using var view = new NativeView(root);
            var subscription = view.Bind<NativeButton>("parking", _ => bound++, _ => unbound++);
            Assert.That(bound, Is.Zero);
            state.Value = new Button("Parking", () => { }).WithId("parking");
            view.Refresh();
            view.Refresh();
            Assert.That(bound, Is.EqualTo(1));
            state.Value = new Button("Updated", () => { }).WithId("parking");
            view.Refresh();
            Assert.That(bound, Is.EqualTo(1));
            state.Value = new Text("empty");
            view.Refresh();
            Assert.That(unbound, Is.EqualTo(1));
            state.Value = new Button("Again", () => { }).WithId("parking");
            view.Refresh();
            subscription.Dispose(); subscription.Dispose();
            Assert.That(bound, Is.EqualTo(2));
            Assert.That(unbound, Is.EqualTo(2));
        }

        [Test]
        public void BindingFailuresRollbackAndDoNotLeaveHiddenSubscriptions() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Button("Parking", () => { }).WithId("parking"), root);
            using var view = new NativeView(root);
            var released = 0;
            Assert.Throws<InvalidOperationException>(() => view.Bind<NativeButton>("parking",
                _ => throw new InvalidOperationException("probe"), _ => released++));
            Assert.That(released, Is.EqualTo(1));
            view.Refresh();
            view.Dispose(); view.Dispose();
            Assert.That(released, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => view.Q<NativeButton>("parking"));
        }

        [Test]
        public void DisposeNotifiesControllerLifetimeEvenWhenCleanupFails() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Text("x").WithId("value"), root);
            var view = new NativeView(root);
            var released = false; var notified = 0;
            view.Bind<Label>("value", _ => { }, _ => { released = true; throw new InvalidOperationException("cleanup"); });
            view.Disposed += () => { Assert.That(released, Is.True); notified++; };
            view.Disposed += () => notified++;
            Assert.Throws<AggregateException>(() => view.Dispose());
            view.Dispose();
            Assert.That(notified, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateNativeOwnershipFailsWithoutPoisoningFutureMounts() {
            var root = new VisualElement();
            Assert.Throws<InvalidOperationException>(() => Framework.Mount(new Composite(new Text("x").WithId("inner")).WithId("outer"), root));
            using var mount = Framework.Mount(new Text("ok").WithId("outer"), root);
            Assert.That(root.Q<Label>("outer"), Is.Not.Null);
        }

        [Test]
        public void CustomNativeControlsAndReactiveStateRemainUsable() {
            var root = new VisualElement();
            var state = new State<string>("initial");
            using var mount = Framework.Mount(new Column(new Widget[] {
                new TextField(state).WithId("path"), new Native(() => new CustomElement()).WithId("custom")
            }), root);
            using var view = new NativeView(root);
            Assert.That(view.Require<CustomElement>("custom"), Is.Not.Null);
            state.Value = "updated";
            Assert.That(view.Require<UnityEngine.UIElements.TextField>("path").value, Is.EqualTo("updated"));
        }

        [Test]
        public void IdDoesNotDiscardExistingReconciliationKeys() {
            var root = new VisualElement();
            var firstState = new State<string>("first"); var secondState = new State<string>("second");
            Widget First() => new TextField(firstState).WithKey(new WidgetKey("first-key")).WithId("first");
            Widget Second() => new TextField(secondState).WithKey(new WidgetKey("second-key")).WithId("second");
            var state = new State<Widget>(new Column(new[] {First(), Second()}));
            using var mount = Framework.Mount(new ReactiveBuilder<Widget>(state, value => value), root);
            var first = root.Q<UnityEngine.UIElements.TextField>("first");
            var second = root.Q<UnityEngine.UIElements.TextField>("second");
            state.Value = new Column(new[] {Second(), First()});
            Assert.That(root.Q<UnityEngine.UIElements.TextField>("first"), Is.SameAs(first));
            Assert.That(root.Q<UnityEngine.UIElements.TextField>("second"), Is.SameAs(second));
            Assert.That(first.value, Is.EqualTo("first"));
        }

        [UnityTest]
        public IEnumerator ScheduledBindingsAndClicksSurviveReplacementWithoutDuplicateCallbacks() {
            var window = ScriptableObject.CreateInstance<TestWindow>();
            MountHandle? mount = null; NativeView? view = null;
            try {
                window.Show();
                view = new NativeView(window.rootVisualElement);
                var clicked = 0;
                using var token = view.OnClick("parking", () => clicked++);
                var state = new State<Widget>(new Text("empty"));
                mount = Framework.Mount(new ReactiveBuilder<Widget>(state, value => value), window.rootVisualElement);
                state.Value = new Button("First", () => { }).WithId("parking");
                yield return null; yield return null; yield return null;
                var first = view.Require<NativeButton>("parking");
                Submit(first);
                Assert.That(clicked, Is.EqualTo(1));
                state.Value = new Text("empty");
                yield return null; yield return null;
                window.rootVisualElement.Add(first);
                Submit(first);
                Assert.That(clicked, Is.EqualTo(1));
                first.RemoveFromHierarchy();
                state.Value = new Button("Second", () => { }).WithId("parking");
                yield return null; yield return null;
                var second = view.Require<NativeButton>("parking");
                Submit(second);
                Assert.That(clicked, Is.EqualTo(2));
                state.Value = new Button("Renamed", () => { }).WithId("parking");
                yield return null; yield return null;
                Submit(second);
                Assert.That(clicked, Is.EqualTo(3));
                view.Dispose();
                Submit(second);
                Assert.That(clicked, Is.EqualTo(3));
            } finally { view?.Dispose(); mount?.Dispose(); window.Close(); }
        }

        [UnityTest]
        public IEnumerator ValueBindingsObserveControlsAndSetValueWithoutNotifyDoesNotEcho() {
            var window = ScriptableObject.CreateInstance<TestWindow>();
            MountHandle? mount = null; NativeView? view = null;
            try {
                window.Show();
                var state = new State<string>("initial");
                var caption = new State<string>("Initial caption");
                mount = Framework.Mount(new ReactiveBuilder<string>(caption, text => new TextField(state,label:text).WithId("path")), window.rootVisualElement);
                view = new NativeView(window.rootVisualElement);
                var changes = 0;
                using var token = view.OnValueChanged<string>("path", _ => changes++);
                yield return null;
                var field = view.Require<UnityEngine.UIElements.TextField>("path");
                field.value = "typed";
                Assert.That(changes, Is.EqualTo(1));
                Assert.That(state.Value, Is.EqualTo("typed"));
                caption.Value = "New caption";
                Assert.That(state.Value, Is.EqualTo("typed"));
                Assert.That(field.value, Is.EqualTo("typed"));
                Assert.That(changes, Is.EqualTo(1));
                field.SetValueWithoutNotify("display only");
                Assert.That(changes, Is.EqualTo(1));
                Assert.That(state.Value, Is.EqualTo("typed"));
            } finally { view?.Dispose(); mount?.Dispose(); window.Close(); }
        }

        private static void Submit(NativeButton button) {
            using var evt = NavigationSubmitEvent.GetPooled(); evt.target = button; button.SendEvent(evt);
        }
        private sealed class Composite : StatelessWidget {
            private readonly Widget _child;
            internal Composite(Widget child) => _child = child;
            public override Widget Build(BuildContext context) => _child;
        }
        private sealed class CustomElement : VisualElement { }
        private sealed class TestWindow : EditorWindow { }
    }
}
