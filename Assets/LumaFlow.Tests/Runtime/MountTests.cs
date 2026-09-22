using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Framework = LumaFlow.LumaFlow;

namespace LumaFlow.Runtime.Tests {

    public sealed class MountTests {
        [Test]
        public void Semantics_ProjectsExplicitHierarchyAndExcludesReplacedDescendants() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Semantics(
                    new Text("Decorative child"),
                    new SemanticsProperties(
                        label: "Account summary",
                        hint: "Opens details",
                        role: SemanticsRole.Button,
                        enabled: true),
                    excludeDescendantSemantics: true),
                root);

            var hierarchy = mount.SemanticsOwner!.Hierarchy;
            Assert.That(hierarchy.rootNodes, Has.Count.EqualTo(1));
            var node = hierarchy.rootNodes[0];
            Assert.That(node.label, Is.EqualTo("Account summary"));
            Assert.That(node.hint, Is.EqualTo("Opens details"));
            Assert.That(node.role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(node.children, Is.Empty);
        }

        [Test]
        public void ExcludeSemantics_RemovesAllDescendantAnnotations() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new ExcludeSemantics(new Text("Decorative")), root);

            Assert.That(mount.SemanticsOwner!.Hierarchy.rootNodes, Is.Empty);
        }

        [Test]
        public void BuiltInControls_ExposeRolesLabelsAndDisabledState() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Column(new Widget[] {
                    new Text("Heading"),
                    new Button("Save", () => { }, enabled: false),
                    new TextField(new State<string>("Alex"), label: "Name")
                }),
                root);

            var nodes = mount.SemanticsOwner!.Hierarchy.rootNodes;
            Assert.That(nodes, Has.Count.EqualTo(3));
            Assert.That(nodes[0].role, Is.EqualTo(AccessibilityRole.StaticText));
            Assert.That(nodes[0].label, Is.EqualTo("Heading"));
            Assert.That(nodes[1].role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(nodes[1].state.HasFlag(AccessibilityState.Disabled), Is.True);
            Assert.That(nodes[2].role, Is.EqualTo(AccessibilityRole.TextField));
            Assert.That(nodes[2].label, Is.EqualTo("Name"));
            Assert.That(nodes[2].value, Is.EqualTo("Alex"));
            Assert.That(nodes[2].allowsDirectInteraction, Is.True);
        }

        [Test]
        public void ControlledSemantics_UpdatesInPlaceAndUnmountClearsTheHierarchy() {
            var root = new VisualElement();
            var value = new State<bool>(false);
            var mount = Framework.Mount(new Checkbox(value, label: "Remember me"), root);
            var hierarchy = mount.SemanticsOwner!.Hierarchy;
            var node = hierarchy.rootNodes[0];

            value.Value = true;

            Assert.That(hierarchy.rootNodes[0], Is.SameAs(node));
            Assert.That(node.state.HasFlag(AccessibilityState.Selected), Is.True);
            mount.Dispose();
            Assert.That(hierarchy.rootNodes, Is.Empty);
        }

        [Test]
        public void Locale_NormalizesLanguageScriptAndRegionCodes() {
            var locale = new Locale(" RU ", countryCode: "ru", scriptCode: "cyrl");

            Assert.That(locale.LanguageCode, Is.EqualTo("ru"));
            Assert.That(locale.ScriptCode, Is.EqualTo("Cyrl"));
            Assert.That(locale.CountryCode, Is.EqualTo("RU"));
            Assert.That(locale.ToString(), Is.EqualTo("ru-Cyrl-RU"));
        }

        [Test]
        public void Localizations_ResolveNearestStronglyTypedResourcesAndLocale() {
            var root = new VisualElement();
            var outer = new LocalizationProbe();
            var inner = new LocalizationProbe();
            using var mount = Framework.Mount(
                new Localizations(
                    new Locale("en", "US"),
                    new Column(new Widget[] {
                        outer,
                        new Localizations(new Locale("ru", "RU"), inner, new AppStrings("Привет"))
                    }),
                    new AppStrings("Hello")),
                root);

            var labels = root.Query<Label>().ToList();
            Assert.That(labels[0].text, Is.EqualTo("en-US:Hello"));
            Assert.That(labels[1].text, Is.EqualTo("ru-RU:Привет"));
            Assert.That(outer.BuildCount, Is.EqualTo(1));
            Assert.That(inner.BuildCount, Is.EqualTo(1));
        }

        [Test]
        public void Localizations_UpdateOnlyDependentSubtrees() {
            var root = new VisualElement();
            var host = new LocalizationHostWidget();
            using var mount = Framework.Mount(host, root);

            var retainedState = host.MountedState.Retained.MountedState;
            retainedState.Increment();

            host.MountedState.UseRussian();

            Assert.That(host.MountedState.Dependent.BuildCount, Is.EqualTo(2));
            Assert.That(host.MountedState.Independent.BuildCount, Is.EqualTo(1));
            Assert.That(host.MountedState.Retained.MountedState, Is.SameAs(retainedState));
            Assert.That(retainedState.InitializationCount, Is.EqualTo(1));
            Assert.That(root.Q<Label>().text, Is.EqualTo("ru-RU:Привет"));
            Assert.That(root.Query<Label>().ToList()[2].text, Is.EqualTo("Привет:1"));
        }

        [Test]
        public void Localizations_RejectDuplicateResourceTypesAndMissingScopes() {
            Assert.Throws<ArgumentException>(() => new Localizations(
                new Locale("en"),
                new Text("Child"),
                new AppStrings("First"),
                new AppStrings("Second")));
            Assert.Throws<InvalidOperationException>(() => Framework.Mount(new LocalizationProbe(), new VisualElement()));
        }

        [Test]
        public void TextScale_ScalesTypographyAndPreservesNestedStateAcrossUpdates() {
            var root = new VisualElement();
            var scaled = new TextScale(
                new TextScaler(1.5f),
                new Theme(
                    CreateTestTheme(),
                    new Column(new Widget[] {
                        new Text("Readable", new TextStyle(fontSize: 12f)),
                        new Button(
                            "Action",
                            () => { },
                            style: new ButtonStyle(typography: new TextStyle(fontSize: 10f))),
                        new Checkbox(new State<bool>(false), label: "Choice")
                    })));
            using (var mount = Framework.Mount(scaled, root)) {
                Assert.That(root.Q<Label>().style.fontSize.value.value, Is.EqualTo(18f));
                Assert.That(root.Q<UnityEngine.UIElements.Button>().style.fontSize.value.value, Is.EqualTo(15f));
                Assert.That(root.Q<UnityEngine.UIElements.Toggle>().labelElement.style.fontSize.value.value, Is.EqualTo(21f));
            }

            var stateRoot = new VisualElement();
            var host = new TextScaleHostWidget();
            using var stateMount = Framework.Mount(host, stateRoot);
            var retainedState = host.MountedState.Counter.MountedState;
            retainedState.Increment();

            host.MountedState.Enlarge();

            Assert.That(host.MountedState.Counter.MountedState, Is.SameAs(retainedState));
            Assert.That(retainedState.InitializationCount, Is.EqualTo(1));
            Assert.That(stateRoot.Q<Label>().text, Is.EqualTo("1"));
        }

        [Test]
        public void Mount_CreatesLabelInsideDedicatedHost() {
            var root = new VisualElement();

            using var mount = Framework.Mount(new Text("Hello"), root);

            Assert.That(mount.IsMounted, Is.True);
            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(root[0].style.flexGrow.value, Is.EqualTo(1f));
            Assert.That(root[0].style.flexShrink.value, Is.EqualTo(1f));
            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0], Is.TypeOf<Label>());
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Hello"));
        }

        [Test]
        public void Mount_UsesTransitionalMediaQueryBeforeRootLayout() {
            var root = new VisualElement();
            var capture = new MediaQueryCaptureWidget();

            using var mount = Framework.Mount(capture, root);

            Assert.That(capture.MediaQuery, Is.EqualTo(default(MediaQueryData)));
        }

        [Test]
        public void MediaQuery_ScopesExplicitMetricsWithoutAddingANativeWrapper() {
            var root = new VisualElement();
            var capture = new MediaQueryCaptureWidget();

            using var mount = Framework.Mount(
                new MediaQuery(new MediaQueryData(390f, 844f), capture),
                root);

            Assert.That(capture.MediaQuery, Is.EqualTo(new MediaQueryData(390f, 844f)));
            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0], Is.TypeOf<Label>());
        }

        [Test]
        public void Dispose_RemovesOnlyOwnedMountHost() {
            var root = new VisualElement();
            var existingChild = new VisualElement();
            root.Add(existingChild);
            var mount = Framework.Mount(new Text("Hello"), root);

            mount.Dispose();

            Assert.That(mount.IsMounted, Is.False);
            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(root[0], Is.SameAs(existingChild));
        }

        [Test]
        public void Dispose_IsIdempotent() {
            var root = new VisualElement();
            var mount = Framework.Mount(new Text("Hello"), root);

            Assert.DoesNotThrow(mount.Dispose);
            Assert.DoesNotThrow(mount.Dispose);
        }

        [Test]
        public void MultipleMounts_AreIndependent() {
            var root = new VisualElement();
            var firstMount = Framework.Mount(new Text("First"), root);
            using var secondMount = Framework.Mount(new Text("Second"), root);

            firstMount.Dispose();

            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Second"));
        }

        [Test]
        public void Mount_WhenNodeCreationFails_RollsBackHost() {
            var root = new VisualElement();

            Assert.Throws<InvalidOperationException>(() => Framework.Mount(new ThrowingWidget(), root));

            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Mount_WhenMountAndRollbackFail_PreservesBothFailuresAndDetachesTheTree() {
            var root = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new MountCleanupFailureWidget(), root));

            Assert.That(root.childCount, Is.Zero);
            Assert.That(exception!.InnerException, Is.TypeOf<AggregateException>());
            var failures = ((AggregateException)exception.InnerException!).InnerExceptions;
            Assert.That(failures, Has.Count.EqualTo(2));
            Assert.That(failures[0].Message, Is.EqualTo("Expected mount failure."));
            Assert.That(failures[1].Message, Does.Contain("failed while unmounting"));
            Assert.That(failures[1].InnerException!.Message, Is.EqualTo("Expected cleanup failure."));
        }

        [Test]
        public void Mount_RejectsDoubleMountOfTheSameNode() {
            var node = new Text("Hello").CreateNode();
            var context = new BuildContext();
            var root = new VisualElement();

            node.Mount(parent: null, context, root);

            Assert.Throws<InvalidOperationException>(() => node.Mount(parent: null, context, root));

            node.Unmount();
        }

        [Test]
        public void Dispose_CleansNodeOwnedBindings() {
            var root = new VisualElement();
            var cleanupCount = 0;
            var mount = Framework.Mount(new CleanupWidget(() => cleanupCount++), root);

            mount.Dispose();

            Assert.That(cleanupCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_UnmountsChildNodesOwnedByTheirParent() {
            var root = new VisualElement();
            var mount = Framework.Mount(new ParentWidget(), root);

            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0][0], Is.TypeOf<Label>());

            mount.Dispose();

            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Native_ExistingElement_MountsElement() {
            var root = new VisualElement();
            var element = new VisualElement();

            using var mount = Framework.Mount(new Native(element), root);

            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0], Is.SameAs(element));
        }

        [Test]
        public void Native_ExistingElement_UnmountDetachesElement() {
            var root = new VisualElement();
            var element = new VisualElement();
            var mount = Framework.Mount(new Native(element), root);

            mount.Dispose();

            Assert.That(element.parent, Is.Null);
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Native_ExistingElement_DoesNotDestroyElement() {
            var firstRoot = new VisualElement();
            var secondRoot = new VisualElement();
            var child = new Label("Owned by consumer");
            var element = new VisualElement();
            element.Add(child);
            var mount = Framework.Mount(new Native(element), firstRoot);

            mount.Dispose();
            secondRoot.Add(element);

            Assert.That(secondRoot[0], Is.SameAs(element));
            Assert.That(element[0], Is.SameAs(child));
            Assert.That(child.text, Is.EqualTo("Owned by consumer"));
        }

        [Test]
        public void Native_ExistingElement_PreservesClasses() {
            var root = new VisualElement();
            var element = new VisualElement();
            element.AddToClassList("consumer-class");
            var mount = Framework.Mount(new Native(element), root);

            mount.Dispose();

            Assert.That(element.ClassListContains("consumer-class"), Is.True);
        }

        [Test]
        public void Native_ExistingElement_PreservesInlineStyles() {
            var root = new VisualElement();
            var element = new VisualElement();
            element.style.width = 144f;
            element.style.backgroundColor = Color.cyan;
            var mount = Framework.Mount(new Native(element), root);

            mount.Dispose();

            Assert.That(element.style.width.value.value, Is.EqualTo(144f));
            Assert.That(element.style.backgroundColor.value, Is.EqualTo(Color.cyan));
        }

        [Test]
        public void Native_ExistingElement_WithParent_Throws() {
            var externalParent = new VisualElement();
            var element = new VisualElement();
            externalParent.Add(element);
            var mountRoot = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new Native(element), mountRoot));

            Assert.That(exception!.Message, Does.Contain("already has a parent"));
            Assert.That(element.parent, Is.SameAs(externalParent));
            Assert.That(mountRoot.childCount, Is.Zero);
        }

        [Test]
        public void Native_ExistingElement_CannotBeMountedTwice() {
            var element = new VisualElement();
            var widget = new Native(element);
            using var firstMount = Framework.Mount(widget, new VisualElement());
            var secondRoot = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(widget, secondRoot));

            Assert.That(exception!.Message, Does.Contain("already has a parent"));
            Assert.That(secondRoot.childCount, Is.Zero);
            Assert.That(element.parent, Is.Not.Null);
        }

        [Test]
        public void Native_ExistingElement_CanBeMountedAgainAfterUnmount() {
            var firstRoot = new VisualElement();
            var secondRoot = new VisualElement();
            var element = new VisualElement();
            var widget = new Native(element);
            var firstMount = Framework.Mount(widget, firstRoot);

            firstMount.Dispose();
            using var secondMount = Framework.Mount(widget, secondRoot);

            Assert.That(secondRoot[0][0], Is.SameAs(element));
        }

        [Test]
        public void Native_Factory_CreatesElementPerMount() {
            var createdElements = new List<VisualElement>();
            var widget = new Native(() =>
            {
                var element = new VisualElement();
                createdElements.Add(element);
                return element;
            });

            using var firstMount = Framework.Mount(widget, new VisualElement());
            using var secondMount = Framework.Mount(widget, new VisualElement());

            Assert.That(createdElements, Has.Count.EqualTo(2));
            Assert.That(createdElements[0], Is.Not.SameAs(createdElements[1]));
        }

        [Test]
        public void Native_Factory_MultipleMountsAreIndependent() {
            var firstRoot = new VisualElement();
            var secondRoot = new VisualElement();
            var widget = new Native(() => new VisualElement());
            var firstMount = Framework.Mount(widget, firstRoot);
            using var secondMount = Framework.Mount(widget, secondRoot);
            var firstElement = firstRoot[0][0];
            var secondElement = secondRoot[0][0];

            firstMount.Dispose();

            Assert.That(firstElement.parent, Is.Null);
            Assert.That(secondRoot[0][0], Is.SameAs(secondElement));
            Assert.That(secondElement.parent, Is.Not.Null);
        }

        [Test]
        public void Native_CompatibleUpdateRetainsTheSameBorrowedElement() {
            var root = new VisualElement();
            var element = new VisualElement();
            var node = (NativeNode)new Native(element).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(node.TryUpdate(new Native(element)), Is.True);
            Assert.That(root[0], Is.SameAs(element));
            Assert.That(element.parent, Is.SameAs(root));
            Assert.That(node.TryUpdate(new Native(new VisualElement())), Is.False);

            node.Unmount();
            Assert.That(element.parent, Is.Null);
        }

        [Test]
        public void Native_CompatibleFactoryUpdateRetainsTheElementCreatedForTheMount() {
            var root = new VisualElement();
            Func<VisualElement> factory = () => new VisualElement();
            var node = (NativeNode)new Native(factory).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var element = root[0];

            Assert.That(node.TryUpdate(new Native(factory)), Is.True);
            Assert.That(root[0], Is.SameAs(element));
            Assert.That(node.TryUpdate(new Native(() => new VisualElement())), Is.False);

            node.Unmount();
            Assert.That(element.parent, Is.Null);
        }

        [Test]
        public void Native_InsideReactiveBuilder_CleansCorrectly() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            var first = new VisualElement();
            var second = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<bool>(state, value => new Native(value ? second : first)),
                root);

            Assert.That(root[0][0], Is.SameAs(first));

            state.Value = true;

            Assert.That(first.parent, Is.Null);
            Assert.That(root[0][0], Is.SameAs(second));

            mount.Dispose();
            Assert.That(second.parent, Is.Null);
        }

        [Test]
        public void Native_CanHostThirdPartyVisualElement() {
            var root = new VisualElement();
            var control = new ThirdPartyVisualElement();

            using var mount = Framework.Mount(new Native(control), root);

            Assert.That(root[0][0], Is.SameAs(control));
            Assert.That(control.Marker, Is.EqualTo("third-party"));
        }

        [Test]
        public void Native_RejectsNullInputsAndNullFactoryResult() {
            Assert.Throws<ArgumentNullException>(() => new Native((VisualElement)null));
            Assert.Throws<ArgumentNullException>(() => new Native((Func<VisualElement>)null));

            var root = new VisualElement();
            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new Native(() => null), root));

            Assert.That(exception!.Message, Does.Contain("factory returned null"));
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void State_NotifiesOnlyWhenItsValueChanges() {
            var state = new State<int>(1);
            var notificationCount = 0;
            var observedValue = 0;
            using var subscription = state.Subscribe(value =>
            {
                notificationCount++;
                observedValue = value;
            });

            state.Value = 1;
            state.Value = 2;

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(observedValue, Is.EqualTo(2));
        }

        [Test]
        public void State_DisposedSubscriptionIsNotNotified() {
            var state = new State<int>(1);
            var notificationCount = 0;
            var subscription = state.Subscribe(_ => notificationCount++);

            subscription.Dispose();
            state.Value = 2;

            Assert.That(notificationCount, Is.Zero);
        }

        [Test]
        public void State_ListenerCanUnsubscribeItselfDuringNotification() {
            var state = new State<int>(0);
            var notificationCount = 0;
            IDisposable subscription = null;
            subscription = state.Subscribe(_ =>
            {
                notificationCount++;
                subscription.Dispose();
            });

            state.Value = 1;
            state.Value = 2;

            Assert.That(notificationCount, Is.EqualTo(1));
        }

        [Test]
        public void State_ListenerCanPerformReentrantMutation() {
            var state = new State<int>(0);
            var observedValues = new List<int>();
            using var subscription = state.Subscribe(value =>
            {
                observedValues.Add(value);
                if (value == 1) {
                    state.Value = 2;
                }
            });

            state.Value = 1;

            Assert.That(observedValues, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(state.Value, Is.EqualTo(2));
        }

        [Test]
        public void State_SubscriberFailureCommitsValueAndDoesNotBlockRemainingSubscribers() {
            var state = new State<int>(0);
            var observedValues = new List<int>();
            using var failingSubscription = state.Subscribe(_ => throw new InvalidOperationException("Expected failure."));
            using var succeedingSubscription = state.Subscribe(value => observedValues.Add(value));

            var exception = Assert.Throws<InvalidOperationException>(() => state.Value = 1);

            Assert.That(exception!.Message, Is.EqualTo("Expected failure."));
            Assert.That(state.Value, Is.EqualTo(1));
            Assert.That(observedValues, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void State_MultipleSubscriberFailuresAreAggregatedAfterAllNotifications() {
            var state = new State<int>(0);
            var notificationOrder = new List<string>();
            using var first = state.Subscribe(_ =>
            {
                notificationOrder.Add("first");
                throw new InvalidOperationException("First failure.");
            });
            using var success = state.Subscribe(_ => notificationOrder.Add("success"));
            using var second = state.Subscribe(_ =>
            {
                notificationOrder.Add("second");
                throw new ArgumentException("Second failure.");
            });

            var exception = Assert.Throws<AggregateException>(() => state.Value = 1);

            Assert.That(notificationOrder, Is.EqualTo(new[] { "first", "success", "second" }));
            Assert.That(state.Value, Is.EqualTo(1));
            Assert.That(exception!.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(exception.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerExceptions[1], Is.TypeOf<ArgumentException>());
        }

        [Test]
        public void TextBoundToState_UpdatesExistingLabelWithoutRemounting() {
            var root = new VisualElement();
            var state = new State<string>("Before");
            using var mount = Framework.Mount(new Text(state), root);
            var label = (Label)root[0][0];

            state.Value = "After";

            Assert.That(root[0][0], Is.SameAs(label));
            Assert.That(label.text, Is.EqualTo("After"));
        }

        [Test]
        public void TextBoundToState_UnsubscribesWhenUnmounted() {
            var root = new VisualElement();
            var state = new State<string>("Before");
            var mount = Framework.Mount(new Text(state), root);
            var label = (Label)root[0][0];

            mount.Dispose();
            state.Value = "After";

            Assert.That(label.text, Is.EqualTo("Before"));
        }

        [Test]
        public void Text_AppliesExplicitTypedStyle() {
            var root = new VisualElement();
            var style = new TextStyle(Color.cyan, fontSize: 18f, fontStyle: FontStyle.Bold);
            using var mount = Framework.Mount(new Text("Styled", style), root);
            var label = (Label)root[0][0];

            Assert.That(label.style.color.value, Is.EqualTo(Color.cyan));
            Assert.That(label.style.fontSize.value.value, Is.EqualTo(18f));
            Assert.That(label.style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
        }

        [Test]
        public void TextStyle_RejectsInvalidFontSize() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyle(fontSize: 0f));
        }

        [Test]
        public void TextField_MapsControlledStateAndNativeConfiguration() {
            var root = new VisualElement();
            var state = new State<string>("Initial");
            using var mount = Framework.Mount(
                new TextField(
                    state,
                    label: "Password",
                    placeholder: "Enter password",
                    obscureText: true,
                    enabled: false),
                root);
            var textField = (UnityEngine.UIElements.TextField)root[0][0];

            Assert.That(textField.value, Is.EqualTo("Initial"));
            Assert.That(textField.label, Is.EqualTo("Password"));
            Assert.That(textField.textEdition.placeholder, Is.EqualTo("Enter password"));
            Assert.That(textField.isPasswordField, Is.True);
            Assert.That(textField.enabledSelf, Is.False);
        }

        [Test]
        public void TextField_UpdatesExistingNativeFieldWhenStateChanges() {
            var root = new VisualElement();
            var state = new State<string>("Before");
            using var mount = Framework.Mount(new TextField(state), root);
            var textField = (UnityEngine.UIElements.TextField)root[0][0];

            state.Value = "After";

            Assert.That(root[0][0], Is.SameAs(textField));
            Assert.That(textField.value, Is.EqualTo("After"));
        }

        [Test]
        public void Form_ValidatesMountedFieldsAndPublishesReactiveErrors() {
            var value = new State<string>("");
            var field = new FormField<string>(value, text => string.IsNullOrWhiteSpace(text) ? "Required" : null);
            var form = new FormState();
            var root = new VisualElement();
            using var mount = Framework.Mount(new Form(form, new TextField(field, label: "Name")), root);

            Assert.That(form.Validate(), Is.False);
            Assert.That(field.ErrorText.Value, Is.EqualTo("Required"));

            value.Value = "Alex";
            Assert.That(form.Validate(), Is.True);
            Assert.That(field.ErrorText.Value, Is.Null);
        }

        [Test]
        public void Form_SubmitCallsItsCallbackOnlyWhenAllMountedFieldsAreValid() {
            var value = new State<string>(string.Empty);
            var field = new FormField<string>(
                value,
                text => string.IsNullOrWhiteSpace(text) ? "Required" : null);
            var form = new FormState();
            var submitCount = 0;
            var root = new VisualElement();
            using var mount = Framework.Mount(new Form(form, new TextField(field)), root);

            Assert.That(form.Submit(() => submitCount++), Is.False);
            Assert.That(submitCount, Is.Zero);
            Assert.That(field.ErrorText.Value, Is.EqualTo("Required"));

            value.Value = "Alex";

            Assert.That(form.Submit(() => submitCount++), Is.True);
            Assert.That(submitCount, Is.EqualTo(1));
        }

        [Test]
        public void Form_ReturnKeySubmitsOnlyWhenItsFieldsAreValid() {
            var value = new State<string>(string.Empty);
            var field = new FormField<string>(
                value,
                text => string.IsNullOrWhiteSpace(text) ? "Required" : null);
            var form = new FormState();
            var submitCount = 0;
            var node = (FormNode)new Form(
                form,
                new TextField(field),
                onSubmit: () => submitCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            using (var invalidEvent = KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None)) {
                node.HandleKeyDown(invalidEvent);
            }

            Assert.That(submitCount, Is.Zero);
            Assert.That(field.ErrorText.Value, Is.EqualTo("Required"));

            value.Value = "Alex";
            using (var validEvent = KeyDownEvent.GetPooled('\0', KeyCode.KeypadEnter, EventModifiers.None)) {
                node.HandleKeyDown(validEvent);
            }

            Assert.That(submitCount, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void Form_ReturnKeyDoesNotSubmitAfterUnmount() {
            var form = new FormState();
            var submitCount = 0;
            var node = (FormNode)new Form(
                form,
                new TextField(new State<string>("Alex")),
                onSubmit: () => submitCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            node.Unmount();

            using var keyEvent = KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None);
            node.HandleKeyDown(keyEvent);

            Assert.That(submitCount, Is.Zero);
        }

        [Test]
        public void Form_ReturnKeyDoesNotSubmitFromAMultilineTextField() {
            var form = new FormState();
            var submitCount = 0;
            var node = (FormNode)new Form(
                form,
                new TextField(new State<string>("Notes"), multiline: true),
                onSubmit: () => submitCount++).CreateNode();
            var root = new VisualElement();
            node.Mount(parent: null, new BuildContext(), root);
            var nativeTextField = (UnityEngine.UIElements.TextField)root[0];

            Assert.That(nativeTextField.multiline, Is.True);
            Assert.That(node.TrySubmit(KeyCode.Return, nativeTextField), Is.False);
            Assert.That(submitCount, Is.Zero);
            node.Unmount();
        }

        [Test]
        public void Form_OnChangeModeTracksValidityAndPublishesErrorsWithoutSubmit() {
            var value = new State<string>("Ready");
            var field = new FormField<string>(value, text => string.IsNullOrWhiteSpace(text) ? "Required" : null);
            var form = new FormState(FormValidationMode.OnChange);
            var root = new VisualElement();
            using var mount = Framework.Mount(new Form(form, new TextField(field)), root);

            Assert.That(form.IsValid.Value, Is.True);
            value.Value = "";
            Assert.That(form.IsValid.Value, Is.False);
            Assert.That(field.ErrorText.Value, Is.EqualTo("Required"));
        }

        [Test]
        public void FormFieldMessage_RegistersDropdownAndCheckboxFieldsWithTheForm() {
            var consent = new State<bool>(false);
            var plan = new State<string>("Studio");
            var consentField = new FormField<bool>(consent, value => value ? null : "Consent is required");
            var planField = new FormField<string>(plan, value => string.IsNullOrWhiteSpace(value) ? "Plan is required" : null);
            var form = new FormState(FormValidationMode.OnChange);
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Form(
                    form,
                    new Column(new Widget[]
                    {
                    new FormFieldMessage<bool>(consentField, new Checkbox(consentField, "Consent")),
                    new FormFieldMessage<string>(planField, new Dropdown<string>(planField, new[] { "Studio", "Creator" }, value => value))
                    })),
                root);

            Assert.That(form.IsValid.Value, Is.False);
            consent.Value = true;
            Assert.That(form.IsValid.Value, Is.True);
        }

        [Test]
        public void Form_CompatibleUpdatePreservesNativeChildAndUsesLatestSubmitCallback() {
            var form = new FormState();
            var firstSubmitCount = 0;
            var secondSubmitCount = 0;
            var root = new VisualElement();
            var node = (FormNode)new Form(
                form,
                new Text("Before"),
                () => firstSubmitCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];

            Assert.That(node.TryUpdate(new Form(
                form,
                new Text("After"),
                () => secondSubmitCount++)), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(((Label)native).text, Is.EqualTo("After"));
            Assert.That(node.TrySubmit(KeyCode.Return, native), Is.True);
            Assert.That(firstSubmitCount, Is.Zero);
            Assert.That(secondSubmitCount, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void Form_DifferentStateIsAnExplicitScopeBoundaryAndRequiresRemount() {
            var root = new VisualElement();
            var node = (FormNode)new Form(new FormState(), new Text("Form")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(node.TryUpdate(new Form(new FormState(), new Text("Form"))), Is.False);
            node.Unmount();
        }

        [Test]
        public void FormFieldMessage_CompatibleUpdateSwitchesFieldAndChildBindingsInPlace() {
            var first = new FormField<bool>(new State<bool>(false));
            var second = new FormField<bool>(new State<bool>(true));
            var root = new VisualElement();
            var context = new BuildContext().WithForm(new FormState());
            var node = (FormFieldMessageNode<bool>)new FormFieldMessage<bool>(
                first,
                new Checkbox(first, "First")).CreateNode();
            node.Mount(parent: null, context, root);
            var wrapper = root[0];
            var nativeCheckbox = wrapper[0];
            var message = (Label)wrapper[1];

            Assert.That(node.TryUpdate(new FormFieldMessage<bool>(
                second,
                new Checkbox(second, "Second"))), Is.True);
            Assert.That(root[0], Is.SameAs(wrapper));
            Assert.That(wrapper[0], Is.SameAs(nativeCheckbox));
            first.ErrorText.Value = "Stale";
            Assert.That(message.text, Is.Empty);
            second.ErrorText.Value = "Current";
            Assert.That(message.text, Is.EqualTo("Current"));
            node.Unmount();
        }

        [Test]
        public void FocusTraversalGroup_CompatibleUpdatePreservesMountedStateAndNativeRoot() {
            var first = new CounterWidget();
            var root = new VisualElement();
            var node = (FocusTraversalGroupNode)new FocusTraversalGroup(first).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];
            var mountedState = first.MountedState;
            var second = new CounterWidget();

            Assert.That(node.TryUpdate(new FocusTraversalGroup(second)), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(second.MountedState, Is.SameAs(mountedState));
            node.Unmount();
        }

        [Test]
        public void LayoutBuilder_CompatibleConfigurationUpdatePreservesChildStateAndMediaScope() {
            var first = new CounterWidget();
            var root = new VisualElement();
            var node = (LayoutBuilderNode)new LayoutBuilder((_, _) => first).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0][0];
            var mountedState = first.MountedState;
            var second = new CounterWidget();

            Assert.That(node.TryUpdate(new LayoutBuilder((context, constraints) =>
            {
                var media = context.MediaQuery;
                Assert.That(media.Width, Is.EqualTo(constraints.MaxWidth));
                return second;
            })), Is.True);
            Assert.That(root[0][0], Is.SameAs(native));
            Assert.That(second.MountedState, Is.SameAs(mountedState));
            node.Unmount();
        }

        [Test]
        public void TextField_PropagatesItsNativeCallbackToStateAndStopsObservingAfterUnmount() {
            var root = new VisualElement();
            var state = new State<string>("Before");
            var node = (TextFieldNode)new TextField(state).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var textField = (UnityEngine.UIElements.TextField)root[0];

            node.HandleValueChanged("During mount");

            Assert.That(state.Value, Is.EqualTo("During mount"));

            node.Unmount();
            state.Value = "After unmount";
            node.HandleValueChanged("Ignored after unmount");

            Assert.That(textField.value, Is.EqualTo("During mount"));
            Assert.That(state.Value, Is.EqualTo("After unmount"));
        }

        [Test]
        public void FocusNode_AttachesToOneTextFieldAndDetachesWhenItUnmounts() {
            var focusNode = new FocusNode();
            var node = (TextFieldNode)new TextField(
                new State<string>("Alex"),
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            Assert.That(focusNode.RequestFocus(), Is.True);

            node.Unmount();

            Assert.That(focusNode.RequestFocus(), Is.False);
            Assert.That(focusNode.IsFocused.Value, Is.False);
        }

        [Test]
        public void TextField_CompatibleUpdatePreservesNativeElementFocusAndSwitchesValueBinding() {
            var root = new VisualElement();
            var firstValue = new State<string>("First");
            var secondValue = new State<string>("Second");
            var focusNode = new FocusNode();
            var node = (TextFieldNode)new TextField(
                firstValue,
                label: "Before",
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.TextField)root[0];

            var updated = node.TryUpdate(new TextField(
                secondValue,
                label: "After",
                placeholder: "Search",
                focusNode: focusNode));

            Assert.That(updated, Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(native.label, Is.EqualTo("After"));
            Assert.That(native.value, Is.EqualTo("Second"));
            Assert.That(focusNode.RequestFocus(), Is.True);
            firstValue.Value = "Ignored";
            Assert.That(native.value, Is.EqualTo("Second"));
            secondValue.Value = "Observed";
            Assert.That(native.value, Is.EqualTo("Observed"));
            node.HandleValueChanged("Native edit");
            Assert.That(secondValue.Value, Is.EqualTo("Native edit"));

            node.Unmount();
        }

        [Test]
        public void FocusNode_TracksTextFieldFocusEventsAndIgnoresLateEvents() {
            var focusNode = new FocusNode();
            var node = (TextFieldNode)new TextField(
                new State<string>("Alex"),
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            node.HandleFocusChanged(isFocused: true);
            Assert.That(focusNode.IsFocused.Value, Is.True);

            node.HandleFocusChanged(isFocused: false);
            Assert.That(focusNode.IsFocused.Value, Is.False);

            node.Unmount();
            node.HandleFocusChanged(isFocused: true);

            Assert.That(focusNode.IsFocused.Value, Is.False);
        }

        [Test]
        public void FocusNode_RejectsMultipleMountedControlAttachments() {
            var focusNode = new FocusNode();
            using var attachment = focusNode.Attach(() => { });

            Assert.Throws<InvalidOperationException>(() => focusNode.Attach(() => { }));
        }

        [Test]
        public void FocusNode_DoesNotRequestFocusWhenItsMountedControlIsDisabled() {
            var focusNode = new FocusNode();
            var node = (ButtonNode)new Button(
                "Save",
                () => { },
                enabled: false,
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            Assert.That(focusNode.RequestFocus(), Is.False);

            node.Unmount();
        }

        [Test]
        public void FocusNode_AttachesToButtonAndDetachesWhenItUnmounts() {
            var focusNode = new FocusNode();
            var node = (ButtonNode)new Button(
                "Save",
                () => { },
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            Assert.That(focusNode.RequestFocus(), Is.True);

            node.Unmount();

            Assert.That(focusNode.RequestFocus(), Is.False);
        }

        [Test]
        public void Button_CompatibleUpdatePreservesNativeElementFocusAndUsesLatestCallback() {
            var root = new VisualElement();
            var firstCalls = 0;
            var secondCalls = 0;
            var focusNode = new FocusNode();
            var node = (ButtonNode)new Button(
                "Before",
                () => firstCalls++,
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.Button)root[0];

            var updated = node.TryUpdate(new Button(
                "After",
                () => secondCalls++,
                focusNode: focusNode));
            node.HandleClicked();

            Assert.That(updated, Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(native.text, Is.EqualTo("After"));
            Assert.That(focusNode.RequestFocus(), Is.True);
            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.EqualTo(1));

            node.Unmount();
        }

        [Test]
        public void FocusNode_AttachesToIconButtonAndDetachesWhenItUnmounts() {
            var focusNode = new FocusNode();
            var node = (IconButtonNode)new IconButton(
                LumaIcons.Save,
                () => { },
                focusNode: focusNode).CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());

            Assert.That(focusNode.RequestFocus(), Is.True);

            node.Unmount();

            Assert.That(focusNode.RequestFocus(), Is.False);
        }

        [Test]
        public void FocusTraversalController_MovesForwardBackwardAndWrapsRegisteredNodes() {
            var controller = new FocusTraversalController();
            var first = new FocusNode();
            var second = new FocusNode();
            var third = new FocusNode();
            using var firstAttachment = first.Attach(() => SetFocused(first, second, third));
            using var secondAttachment = second.Attach(() => SetFocused(second, first, third));
            using var thirdAttachment = third.Attach(() => SetFocused(third, first, second));
            using var firstRegistration = controller.Register(first);
            using var secondRegistration = controller.Register(second);
            using var thirdRegistration = controller.Register(third);

            Assert.That(controller.Move(forward: true), Is.True);
            Assert.That(first.IsFocused.Value, Is.True);
            Assert.That(controller.Move(forward: true), Is.True);
            Assert.That(second.IsFocused.Value, Is.True);
            Assert.That(controller.Move(forward: false), Is.True);
            Assert.That(first.IsFocused.Value, Is.True);
            Assert.That(controller.Move(forward: false), Is.True);
            Assert.That(third.IsFocused.Value, Is.True);
        }

        [Test]
        public void FocusTraversalController_SkipsUnavailableFocusNodes() {
            var controller = new FocusTraversalController();
            var unavailable = new FocusNode();
            var available = new FocusNode();
            using var unavailableAttachment = unavailable.Attach(() => { }, () => false);
            using var availableAttachment = available.Attach(() => SetFocused(available, unavailable));
            using var unavailableRegistration = controller.Register(unavailable);
            using var availableRegistration = controller.Register(available);

            Assert.That(controller.Move(forward: true), Is.True);
            Assert.That(unavailable.IsFocused.Value, Is.False);
            Assert.That(available.IsFocused.Value, Is.True);
        }

        [Test]
        public void FocusNode_AttachesToEveryBuiltInFormControl() {
            var toggleFocus = new FocusNode();
#pragma warning disable CS0618 // Legacy Toggle remains contract-covered during its pre-1.0 migration window.
            var legacyToggle = new Toggle(new State<bool>(false), focusNode: toggleFocus);
#pragma warning restore CS0618
            AssertFocusNodeDetaches(legacyToggle, toggleFocus);

            var checkboxFocus = new FocusNode();
            AssertFocusNodeDetaches(new Checkbox(new State<bool>(false), focusNode: checkboxFocus), checkboxFocus);

            var sliderFocus = new FocusNode();
            AssertFocusNodeDetaches(new Slider(new State<float>(0.5f), 0f, 1f, focusNode: sliderFocus), sliderFocus);

            var dropdownFocus = new FocusNode();
            AssertFocusNodeDetaches(
                new Dropdown<string>(new State<string>("One"), new[] { "One", "Two" }, value => value, focusNode: dropdownFocus),
                dropdownFocus);

            var radioFocus = new FocusNode();
            AssertFocusNodeDetaches(new Radio<string>(new State<string>("One"), "One", focusNode: radioFocus), radioFocus);
        }

        [Test]
        public void TextField_StateUpdateIsSilentAndUpdatesEveryBoundNativeField() {
            var root = new VisualElement();
            var state = new State<string>("Alex");
            var notifications = 0;
            using var subscription = state.Subscribe(_ => notifications++);
            using var mount = Framework.Mount(
                new Column(new Widget[] { new TextField(state), new TextField(state) }),
                root);
            var first = (UnityEngine.UIElements.TextField)root[0][0][0];
            var second = (UnityEngine.UIElements.TextField)root[0][0][1];

            state.Value = "John";

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(first.value, Is.EqualTo("John"));
            Assert.That(second.value, Is.EqualTo("John"));
        }

        [Test]
        public void TextField_NativeCallbackCanUnmountItsCurrentTreeWithoutDisposingState() {
            var root = new VisualElement();
            var state = new State<string>("Before");
            var callbackCount = 0;
            TextFieldNode node = null;
            using var unmountOnChange = state.Subscribe(_ => node!.Unmount());
            node = (TextFieldNode)new TextField(state, onChanged: _ => callbackCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.DoesNotThrow(() => node.HandleValueChanged("After"));
            Assert.That(node.IsMounted, Is.False);
            Assert.That(state.Value, Is.EqualTo("After"));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(root.childCount, Is.Zero);

            state.Value = "Still owned by caller";
            Assert.That(state.Value, Is.EqualTo("Still owned by caller"));
        }

        [Test]
        public void ControlledInputs_OnChangedRunsAfterUserCommitButNotProgrammaticStateChanges() {
            var root = new VisualElement();
            var callbacks = new List<string>();

            var textState = new State<string>("Initial");
            var textNode = (TextFieldNode)new TextField(
                textState,
                onChanged: value => {
                    Assert.That(textState.Value, Is.EqualTo(value));
                    callbacks.Add($"text:{value}");
                }).CreateNode();
            textNode.Mount(parent: null, new BuildContext(), root);
            textState.Value = "Programmatic";
            textNode.HandleValueChanged("User");
            textNode.Unmount();

            var checkboxState = new State<bool>(false);
            var checkboxNode = (CheckboxNode)new Checkbox(
                checkboxState,
                onChanged: value => {
                    Assert.That(checkboxState.Value, Is.EqualTo(value));
                    callbacks.Add($"checkbox:{value}");
                }).CreateNode();
            checkboxNode.Mount(parent: null, new BuildContext(), root);
            checkboxState.Value = true;
            checkboxNode.HandleValueChanged(false);
            checkboxNode.Unmount();

            var switchState = new State<bool>(false);
            var switchNode = (SwitchNode)new Switch(
                switchState,
                onChanged: value => {
                    Assert.That(switchState.Value, Is.EqualTo(value));
                    callbacks.Add($"switch:{value}");
                }).CreateNode();
            switchNode.Mount(parent: null, new BuildContext(), root);
            switchState.Value = true;
            switchNode.HandleValueChanged(false);
            switchNode.Unmount();

            var sliderState = new State<float>(0.1f);
            var sliderNode = (SliderNode)new Slider(
                sliderState,
                0f,
                1f,
                onChanged: value => {
                    Assert.That(sliderState.Value, Is.EqualTo(value));
                    callbacks.Add("slider");
                }).CreateNode();
            sliderNode.Mount(parent: null, new BuildContext(), root);
            sliderState.Value = 0.5f;
            sliderNode.HandleValueChanged(0.75f);
            sliderNode.Unmount();

            var dropdownState = new State<string>("One");
            var dropdownNode = (DropdownNode<string>)new Dropdown<string>(
                dropdownState,
                new[] { "One", "Two" },
                value => value,
                onChanged: value => {
                    Assert.That(dropdownState.Value, Is.EqualTo(value));
                    callbacks.Add($"dropdown:{value}");
                }).CreateNode();
            dropdownNode.Mount(parent: null, new BuildContext(), root);
            dropdownState.Value = "Two";
            dropdownNode.HandleValueChanged("One");
            dropdownNode.Unmount();

            var radioState = new State<string>("One");
            var radioNode = (RadioNode<string>)new Radio<string>(
                radioState,
                "One",
                onChanged: value => {
                    Assert.That(radioState.Value, Is.EqualTo(value));
                    callbacks.Add($"radio:{value}");
                }).CreateNode();
            radioNode.Mount(parent: null, new BuildContext(), root);
            radioState.Value = "Two";
            radioNode.HandleValueChanged(isSelected: true);
            radioNode.Unmount();

            Assert.That(callbacks, Is.EqualTo(new[]
            {
                "text:User",
                "checkbox:False",
                "switch:False",
                "slider",
                "dropdown:One",
                "radio:One"
            }));
        }

        [Test]
        public void ControlledInput_OnChangedUsesLatestCompatibleConfiguration() {
            var root = new VisualElement();
            var state = new State<string>("Initial");
            var firstCalls = 0;
            var latestCalls = 0;
            var node = (TextFieldNode)new TextField(state, onChanged: _ => firstCalls++).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(node.TryUpdate(new TextField(state, onChanged: _ => latestCalls++)), Is.True);
            node.HandleValueChanged("Updated");

            Assert.That(firstCalls, Is.Zero);
            Assert.That(latestCalls, Is.EqualTo(1));
            Assert.That(state.Value, Is.EqualTo("Updated"));
            node.Unmount();
        }

        [Test]
        public void TextField_OnSubmittedUsesCurrentValueAndLatestCompatibleConfiguration() {
            var root = new VisualElement();
            var state = new State<string>("Initial");
            var submissions = new List<string>();
            var node = (TextFieldNode)new TextField(
                state,
                onSubmitted: value => submissions.Add($"first:{value}")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            state.Value = "Current";
            Assert.That(node.TryUpdate(new TextField(
                state,
                onSubmitted: value => submissions.Add($"latest:{value}"))), Is.True);

            Assert.That(node.HandleSubmitted(KeyCode.Space), Is.False);
            Assert.That(node.HandleSubmitted(KeyCode.Return), Is.True);
            Assert.That(submissions, Is.EqualTo(new[] { "latest:Current" }));
            node.Unmount();
        }

        [Test]
        public void TextField_OnSubmittedPrecedesFormSubmissionAndIgnoresMultilineDisabledOrUnmountedFields() {
            var order = new List<string>();
            var root = new VisualElement();
            var textNode = (TextFieldNode)new TextField(
                new State<string>("Ready"),
                onSubmitted: _ => order.Add("field")).CreateNode();
            textNode.Mount(parent: null, new BuildContext(), root);
            var formNode = (FormNode)new Form(
                new FormState(),
                new Text("Content"),
                onSubmit: () => order.Add("form")).CreateNode();
            formNode.Mount(parent: null, new BuildContext(), root);

            Assert.That(textNode.HandleSubmitted(
                KeyCode.Return,
                () => formNode.HandleTextInputSubmit()), Is.True);

            Assert.That(order, Is.EqualTo(new[] { "field", "form" }));
            formNode.Unmount();
            textNode.Unmount();

            var callbackCount = 0;
            var multiline = (TextFieldNode)new TextField(
                new State<string>("Notes"),
                multiline: true,
                onSubmitted: _ => callbackCount++).CreateNode();
            multiline.Mount(parent: null, new BuildContext(), root);
            Assert.That(multiline.HandleSubmitted(KeyCode.Return), Is.False);
            multiline.Unmount();

            var disabled = (TextFieldNode)new TextField(
                new State<string>("Disabled"),
                enabled: false,
                onSubmitted: _ => callbackCount++).CreateNode();
            disabled.Mount(parent: null, new BuildContext(), root);
            Assert.That(disabled.HandleSubmitted(KeyCode.Return), Is.False);
            disabled.Unmount();
            Assert.That(disabled.HandleSubmitted(KeyCode.Return), Is.False);
            Assert.That(callbackCount, Is.Zero);
        }

        [Test]
        public void Slider_InteractionCallbacksBracketChangesWithBoundaryValues() {
            var root = new VisualElement();
            var state = new State<float>(0.25f);
            var phases = new List<string>();
            var values = new List<float>();
            var node = (SliderNode)new Slider(
                state,
                0f,
                1f,
                onChanged: value => { phases.Add("change"); values.Add(value); },
                onChangeStart: value => { phases.Add("start"); values.Add(value); },
                onChangeEnd: value => { phases.Add("end"); values.Add(value); }).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(node.HandleInteractionStart(), Is.True);
            node.HandleValueChanged(0.75f);
            Assert.That(node.HandleInteractionEnd(), Is.True);

            Assert.That(state.Value, Is.EqualTo(0.75f));
            Assert.That(phases, Is.EqualTo(new[] { "start", "change", "end" }));
            Assert.That(values, Is.EqualTo(new[] { 0.25f, 0.75f, 0.75f }));
            node.Unmount();
        }

        [Test]
        public void Slider_NonPointerNativeChangeCreatesOneCompleteInteraction() {
            var root = new VisualElement();
            var state = new State<float>(0.2f);
            var phases = new List<string>();
            var values = new List<float>();
            var node = (SliderNode)new Slider(
                state,
                0f,
                1f,
                onChanged: value => { phases.Add("change"); values.Add(value); },
                onChangeStart: value => { phases.Add("start"); values.Add(value); },
                onChangeEnd: value => { phases.Add("end"); values.Add(value); }).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            node.HandleNativeValueChanged(0.6f);

            Assert.That(state.Value, Is.EqualTo(0.6f));
            Assert.That(phases, Is.EqualTo(new[] { "start", "change", "end" }));
            Assert.That(values, Is.EqualTo(new[] { 0.2f, 0.6f, 0.6f }));
            node.Unmount();
        }

        [Test]
        public void Slider_NonPointerLifecyclePreservesCommitAndReportsChangeAndEndFailures() {
            var root = new VisualElement();
            var state = new State<float>(0.2f);
            var node = (SliderNode)new Slider(
                state,
                0f,
                1f,
                onChanged: _ => throw new InvalidOperationException("change"),
                onChangeEnd: _ => throw new ArgumentException("end")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var exception = Assert.Throws<AggregateException>(() => node.HandleNativeValueChanged(0.8f));

            Assert.That(state.Value, Is.EqualTo(0.8f));
            Assert.That(exception!.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(exception.InnerExceptions[0].Message, Is.EqualTo("change"));
            Assert.That(exception.InnerExceptions[1].Message, Is.EqualTo("end"));
            node.Unmount();
        }

        [Test]
        public void Slider_LifecycleUsesLatestConfigurationAndIgnoresDisabledOrUnmountedNodes() {
            var root = new VisualElement();
            var state = new State<float>(0.25f);
            var firstCalls = 0;
            var latestValues = new List<float>();
            var node = (SliderNode)new Slider(
                state,
                0f,
                1f,
                onChangeStart: _ => firstCalls++,
                onChangeEnd: _ => firstCalls++).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(node.TryUpdate(new Slider(
                state,
                0f,
                1f,
                onChangeStart: latestValues.Add,
                onChangeEnd: latestValues.Add)), Is.True);
            Assert.That(node.HandleInteractionStart(), Is.True);
            node.HandleValueChanged(0.5f);
            Assert.That(node.HandleInteractionEnd(), Is.True);
            Assert.That(firstCalls, Is.Zero);
            Assert.That(latestValues, Is.EqualTo(new[] { 0.25f, 0.5f }));
            node.Unmount();
            Assert.That(node.HandleInteractionStart(), Is.False);
            Assert.That(node.HandleInteractionEnd(), Is.False);

            var disabledCalls = 0;
            var disabled = (SliderNode)new Slider(
                new State<float>(0.25f),
                0f,
                1f,
                enabled: false,
                onChangeStart: _ => disabledCalls++,
                onChangeEnd: _ => disabledCalls++).CreateNode();
            disabled.Mount(parent: null, new BuildContext(), root);
            Assert.That(disabled.HandleInteractionStart(), Is.False);
            Assert.That(disabled.HandleInteractionEnd(), Is.False);
            disabled.Unmount();
            Assert.That(disabledCalls, Is.Zero);
        }

        [Test]
        public void ControlledInputs_DisabledNodesIgnoreSyntheticUserChanges() {
            var root = new VisualElement();
            var callbackCount = 0;

            var textState = new State<string>("Initial");
            var textNode = (TextFieldNode)new TextField(textState, enabled: false, onChanged: _ => callbackCount++).CreateNode();
            textNode.Mount(parent: null, new BuildContext(), root);
            textNode.HandleValueChanged("Ignored");
            textNode.Unmount();

            var checkboxState = new State<bool>(false);
            var checkboxNode = (CheckboxNode)new Checkbox(checkboxState, enabled: false, onChanged: _ => callbackCount++).CreateNode();
            checkboxNode.Mount(parent: null, new BuildContext(), root);
            checkboxNode.HandleValueChanged(true);
            checkboxNode.Unmount();

            var switchState = new State<bool>(false);
            var switchNode = (SwitchNode)new Switch(switchState, enabled: false, onChanged: _ => callbackCount++).CreateNode();
            switchNode.Mount(parent: null, new BuildContext(), root);
            switchNode.HandleValueChanged(true);
            switchNode.Unmount();

            var sliderState = new State<float>(0.25f);
            var sliderNode = (SliderNode)new Slider(sliderState, 0f, 1f, enabled: false, onChanged: _ => callbackCount++).CreateNode();
            sliderNode.Mount(parent: null, new BuildContext(), root);
            sliderNode.HandleValueChanged(0.75f);
            sliderNode.Unmount();

            var dropdownState = new State<string>("One");
            var dropdownNode = (DropdownNode<string>)new Dropdown<string>(
                dropdownState,
                new[] { "One", "Two" },
                value => value,
                enabled: false,
                onChanged: _ => callbackCount++).CreateNode();
            dropdownNode.Mount(parent: null, new BuildContext(), root);
            dropdownNode.HandleValueChanged("Two");
            dropdownNode.Unmount();

            var radioState = new State<string>("One");
            var radioNode = (RadioNode<string>)new Radio<string>(
                radioState,
                "Two",
                enabled: false,
                onChanged: _ => callbackCount++).CreateNode();
            radioNode.Mount(parent: null, new BuildContext(), root);
            radioNode.HandleValueChanged(isSelected: true);
            radioNode.Unmount();

            Assert.That(textState.Value, Is.EqualTo("Initial"));
            Assert.That(checkboxState.Value, Is.False);
            Assert.That(switchState.Value, Is.False);
            Assert.That(sliderState.Value, Is.EqualTo(0.25f));
            Assert.That(dropdownState.Value, Is.EqualTo("One"));
            Assert.That(radioState.Value, Is.EqualTo("One"));
            Assert.That(callbackCount, Is.Zero);
        }

        [Test]
        public void ControlledInputChange_CommitsAndPreservesStateObserverAndCallbackFailures() {
            var state = new State<int>(0);
            var callbackRan = false;
            using var subscription = state.Subscribe(_ => throw new InvalidOperationException("observer"));

            var exception = Assert.Throws<AggregateException>(() => ControlledInputChange.Commit(
                state,
                1,
                _ => {
                    callbackRan = true;
                    throw new ArgumentException("callback");
                }));

            Assert.That(state.Value, Is.EqualTo(1));
            Assert.That(callbackRan, Is.True);
            Assert.That(exception!.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(exception.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerExceptions[0].Message, Is.EqualTo("observer"));
            Assert.That(exception.InnerExceptions[1], Is.TypeOf<ArgumentException>());
            Assert.That(exception.InnerExceptions[1].Message, Is.EqualTo("callback"));
        }

        [Test]
        public void TextField_ResolvesThemeStyleAndDisplaysSupportingOrErrorText() {
            var root = new VisualElement();
            var style = new TextFieldStyle(
                background: Color.cyan,
                foreground: Color.black,
                border: Color.magenta,
                shape: BorderRadius.All(10f),
                error: new TextFieldStateStyle(foreground: Color.red, border: Color.red));
            var theme = CreateTestThemeWithTextFieldStyle(style);
            using var mount = Framework.Mount(
                new Theme(theme, new TextField(new State<string>(""), label: "Email", errorText: "Required")),
                root);
            var fieldRoot = root[0][0];
            var textField = (UnityEngine.UIElements.TextField)fieldRoot[0];

            var input = textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);
            Assert.That(textField.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(Color.red));
            Assert.That(fieldRoot[1], Is.TypeOf<Label>());
            Assert.That(((Label)fieldRoot[1]).text, Is.EqualTo("Required"));
            Assert.That(((Label)fieldRoot[1]).style.color.value, Is.EqualTo(Color.red));
        }

        [Test]
        public void TextField_ExplicitStyleOverridesTheTheme() {
            var root = new VisualElement();
            var theme = CreateTestThemeWithTextFieldStyle(new TextFieldStyle(background: Color.cyan));
            var explicitStyle = new TextFieldStyle(background: Color.yellow, border: Color.black);
            using var mount = Framework.Mount(
                new Theme(theme, new TextField(new State<string>(""), style: explicitStyle)),
                root);
            var textField = (UnityEngine.UIElements.TextField)root[0][0];

            var input = textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.yellow));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(Color.black));
        }

        [Test]
        public void TextField_UsesTheActiveThemeColorSchemeWhenNoComponentOverrideIsProvided() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            using var mount = Framework.Mount(new Theme(theme, new TextField(new State<string>(""), placeholder: "Search")), root);
            var field = (UnityEngine.UIElements.TextField)root[0][0];
            var input = field.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);

            Assert.That(field.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(theme.Colors.Surface));
            Assert.That(input.style.color.value, Is.EqualTo(theme.Colors.OnSurface));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(theme.Colors.Outline));
        }

        [Test]
        public void TextField_ThemeStylesActualInputElementWithoutNativeNestedChrome() {
            var root = new VisualElement();
            var theme = CreateTestThemeWithTextFieldStyle(new TextFieldStyle(
                background: Color.cyan,
                foreground: Color.black,
                border: Color.magenta));
            using var mount = Framework.Mount(new Theme(theme, new TextField(new State<string>(""))), root);
            var field = (UnityEngine.UIElements.TextField)root[0][0];
            var input = field.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);

            Assert.That(input, Is.Not.Null);
            Assert.That(field.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(field.style.borderTopWidth.value, Is.EqualTo(0f));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(Color.magenta));
        }

        [Test]
        public void TextField_StatePropertiesReceiveCombinedStatesAndOverrideLegacyStyles() {
            var root = new VisualElement();
            var resolvedStates = WidgetStates.None;
            var style = new TextFieldStyle(
                background: Color.black,
                disabled: new TextFieldStateStyle(background: Color.gray),
                error: new TextFieldStateStyle(background: Color.red),
                backgroundColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    resolvedStates = states;
                    return (states & (WidgetStates.Disabled | WidgetStates.Error | WidgetStates.Hovered))
                        == (WidgetStates.Disabled | WidgetStates.Error | WidgetStates.Hovered)
                        ? Color.yellow
                        : Color.cyan;
                }));
            var node = (TextFieldNode)new TextField(
                new State<string>(string.Empty),
                enabled: false,
                errorText: "Required",
                style: style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var textField = (UnityEngine.UIElements.TextField)root[0][0];
            var input = textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);

            node.SetHovered(true);

            Assert.That(resolvedStates.HasFlag(WidgetStates.Disabled), Is.True);
            Assert.That(resolvedStates.HasFlag(WidgetStates.Error), Is.True);
            Assert.That(resolvedStates.HasFlag(WidgetStates.Hovered), Is.True);
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.yellow));
            node.Unmount();
        }

        [Test]
        public void TextField_StateStyleCompatibleUpdatePreservesNativeElement() {
            var root = new VisualElement();
            var version = new State<bool>(false);
            using var mount = Framework.Mount(
                new ReactiveBuilder<bool>(version, updated => new TextField(
                    new State<string>(string.Empty),
                    style: new TextFieldStyle(
                        backgroundColor: WidgetStateProperty<Color?>.All(updated ? Color.yellow : Color.cyan)))),
                root);
            var textField = (UnityEngine.UIElements.TextField)root[0][0];
            var input = textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName);

            version.Value = true;

            Assert.That(root[0][0], Is.SameAs(textField));
            Assert.That(textField.Q<VisualElement>(className: UnityEngine.UIElements.TextField.inputUssClassName), Is.SameAs(input));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.yellow));
        }

        [Test]
        public void ReactiveBuilder_UpdatesCompatibleLocalChildWithoutRemounting() {
            var root = new VisualElement();
            var advancedMode = new State<bool>(false);
            using var mount = Framework.Mount(
                new Column(new Widget[]
                {
                new Text("Header"),
                new ReactiveBuilder<bool>(advancedMode, enabled => new Text(enabled ? "Advanced" : "Basic")),
                new Text("Save")
                }),
                root);
            var column = root[0][0];
            var header = column[0];
            var initialChild = column[1];
            var save = column[2];

            Assert.That(((Label)initialChild).text, Is.EqualTo("Basic"));
            advancedMode.Value = true;

            Assert.That(column[0], Is.SameAs(header));
            Assert.That(column[1], Is.SameAs(initialChild));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Advanced"));
            Assert.That(column[2], Is.SameAs(save));
        }

        [Test]
        public void ReactiveBuilder_CompatibleConfigurationUpdatePreservesStateAndUsesLatestBuilder() {
            var state = new State<int>(0);
            var first = new CounterWidget();
            var root = new VisualElement();
            var node = (ReactiveBuilderNode<int>)new ReactiveBuilder<int>(state, _ => first).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];
            var mountedState = first.MountedState;
            var second = new CounterWidget();
            var latestValue = -1;

            Assert.That(node.TryUpdate(new ReactiveBuilder<int>(state, value =>
            {
                latestValue = value;
                return second;
            })), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(second.MountedState, Is.SameAs(mountedState));
            state.Value = 4;
            Assert.That(latestValue, Is.EqualTo(4));
            node.Unmount();
        }

        [Test]
        public void ReactiveBuilder_CompatibleUpdateSwitchesObservedStateWithoutRemountingLabel() {
            var first = new State<int>(1);
            var second = new State<int>(2);
            var root = new VisualElement();
            var node = (ReactiveBuilderNode<int>)new ReactiveBuilder<int>(first, value => new Text($"A{value}")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var label = (Label)root[0];

            Assert.That(node.TryUpdate(new ReactiveBuilder<int>(second, value => new Text($"B{value}"))), Is.True);
            Assert.That(root[0], Is.SameAs(label));
            Assert.That(label.text, Is.EqualTo("B2"));
            first.Value = 3;
            Assert.That(label.text, Is.EqualTo("B2"));
            second.Value = 5;
            Assert.That(label.text, Is.EqualTo("B5"));
            node.Unmount();
        }

        [Test]
        public void ReactiveBuilder_ReplacementPreservesColumnGapWithoutWrapperElements() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            using var mount = Framework.Mount(
                new Column(new Widget[]
                {
                new Text("Header"),
                new ReactiveBuilder<bool>(state, value => new Text(value ? "Enabled" : "Disabled")),
                new Text("Footer")
                }, gap: 12f),
                root);
            var column = root[0][0];

            state.Value = true;

            Assert.That(column.childCount, Is.EqualTo(3));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Enabled"));
            Assert.That(column[0].style.marginBottom.value.value, Is.EqualTo(12f));
            Assert.That(column[1].style.marginBottom.value.value, Is.EqualTo(12f));
            Assert.That(column[2].style.marginBottom.value.value, Is.EqualTo(0f));
        }

        [Test]
        public void ReactiveBuilder_ReplacementPreservesRowGapWithoutWrapperElements() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            using var mount = Framework.Mount(
                new Row(new Widget[]
                {
                new Text("Start"),
                new ReactiveBuilder<bool>(state, value => new Text(value ? "On" : "Off")),
                new Text("End")
                }, gap: 8f),
                root);
            var row = root[0][0];

            state.Value = true;

            Assert.That(row.childCount, Is.EqualTo(3));
            Assert.That(((Label)row[1]).text, Is.EqualTo("On"));
            Assert.That(row[0].style.marginRight.value.value, Is.EqualTo(8f));
            Assert.That(row[1].style.marginRight.value.value, Is.EqualTo(8f));
            Assert.That(row[2].style.marginRight.value.value, Is.EqualTo(0f));
        }

        [Test]
        public void ReactiveBuilder_ReplacesOldChildCleansBindingsAndUnsubscribesOnUnmount() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            var cleanupCount = 0;
            var node = (ReactiveBuilderNode<bool>)new ReactiveBuilder<bool>(
                state,
                visible => visible ? new Text("Visible") : new CleanupWidget(() => cleanupCount++)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            state.Value = true;
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(((Label)root[0]).text, Is.EqualTo("Visible"));

            node.Unmount();
            state.Value = false;

            Assert.That(root.childCount, Is.Zero);
            Assert.That(cleanupCount, Is.EqualTo(1));
        }

        [Test]
        public void ReactiveBuilder_SameStateDrivesIndependentSubtrees() {
            var root = new VisualElement();
            var state = new State<int>(1);
            using var mount = Framework.Mount(
                new Column(new Widget[]
                {
                new ReactiveBuilder<int>(state, value => new Text($"First {value}")),
                new ReactiveBuilder<int>(state, value => new Text($"Second {value}"))
                }),
                root);
            var column = root[0][0];

            state.Value = 2;

            Assert.That(((Label)column[0]).text, Is.EqualTo("First 2"));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Second 2"));
        }

        [Test]
        public void ReactiveBuilder_CallbackCanTriggerItsOwnReplacement() {
            var root = new VisualElement();
            var visible = new State<bool>(true);
            var node = (ReactiveBuilderNode<bool>)new ReactiveBuilder<bool>(
                visible,
                value => value
                    ? new Button("Close", () => visible.Value = false)
                    : new Text("Closed")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var button = (ButtonNode)node.CurrentChild;

            Assert.DoesNotThrow(button.HandleClicked);
            Assert.That(visible.Value, Is.False);
            Assert.That(root[0], Is.TypeOf<Label>());
            Assert.That(((Label)root[0]).text, Is.EqualTo("Closed"));
            node.Unmount();
        }

        [Test]
        public void ReactiveBuilder_BuilderFailureLeavesCurrentSubtreeMounted() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            using var mount = Framework.Mount(
                new ReactiveBuilder<bool>(
                    state,
                    value => value
                        ? throw new InvalidOperationException("Expected builder failure.")
                        : new Text("Basic")),
                root);
            var initial = root[0][0];

            Assert.Throws<InvalidOperationException>(() => state.Value = true);

            Assert.That(root[0][0], Is.SameAs(initial));
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Basic"));
        }

        [Test]
        public void ReactiveBuilder_FailedReplacementDoesNotBlockIndependentSubtrees() {
            var root = new VisualElement();
            var state = new State<bool>(false);
            using var mount = Framework.Mount(
                new Column(new Widget[]
                {
                new ReactiveBuilder<bool>(state, value => new Text(value ? "First new" : "First old")),
                new ReactiveBuilder<bool>(state, value => value
                    ? throw new InvalidOperationException("Expected builder failure.")
                    : new Text("Second old")),
                new ReactiveBuilder<bool>(state, value => new Text(value ? "Third new" : "Third old"))
                }),
                root);
            var column = root[0][0];
            var secondInitial = column[1];

            Assert.Throws<InvalidOperationException>(() => state.Value = true);

            Assert.That(state.Value, Is.True);
            Assert.That(((Label)column[0]).text, Is.EqualTo("First new"));
            Assert.That(column[1], Is.SameAs(secondInitial));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Second old"));
            Assert.That(((Label)column[2]).text, Is.EqualTo("Third new"));
        }

        [Test]
        public void ReactiveBuilder_OldChildCleanupFailureCommitsTheReplacementAndAllowsLaterUpdates() {
            var root = new VisualElement();
            var state = new State<int>(0);
            var node = (ReactiveBuilderNode<int>)new ReactiveBuilder<int>(
                state,
                value => value == 0
                    ? new CleanupWidget(() => throw new InvalidOperationException("Expected cleanup failure."))
                    : new Text(value.ToString())).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.Throws<InvalidOperationException>(() => state.Value = 1);

            var replacement = root[0];
            Assert.That(node.NativeElement, Is.SameAs(replacement));
            Assert.That(((Label)replacement).text, Is.EqualTo("1"));
            Assert.That(node.IsMounted, Is.True);

            Assert.DoesNotThrow(() => state.Value = 2);
            Assert.That(root[0], Is.SameAs(replacement));
            Assert.That(((Label)replacement).text, Is.EqualTo("2"));
            Assert.DoesNotThrow(node.Unmount);
        }

        [Test]
        public void FailedConfigurationEvaluationRemovesNewInheritedDependenciesAndRestoresPreviousOnes() {
            var context = new BuildContext(
                theme: CreateTestTheme(),
                mediaQuery: new MediaQueryData(100f, 100f));
            var initial = new InheritedDependencyProbeWidget(readTheme: true);
            var failing = new InheritedDependencyProbeWidget(readMediaQuery: true, throwOnBuild: true);
            var root = new VisualElement();
            var node = (StatelessWidgetNode)initial.CreateNode();
            node.Mount(parent: null, context, root);

            Assert.Throws<InvalidOperationException>(() => node.TryUpdate(failing));
            Assert.That(initial.BuildCount, Is.EqualTo(1));
            Assert.That(failing.BuildCount, Is.EqualTo(1));

            context.UpdateMediaQuery(new MediaQueryData(200f, 100f));
            Assert.That(initial.BuildCount, Is.EqualTo(1));

            context.UpdateTheme(CreateTestTheme());
            Assert.That(initial.BuildCount, Is.EqualTo(2));
            node.Unmount();
        }

        [Test]
        public void FailedInheritedNotificationDoesNotKeepDependenciesReadByTheFailedBuild() {
            var context = new BuildContext(
                theme: CreateTestTheme(),
                mediaQuery: new MediaQueryData(100f, 100f));
            var probe = new InheritedDependencyProbeWidget(readTheme: true);
            var root = new VisualElement();
            var node = (StatelessWidgetNode)probe.CreateNode();
            node.Mount(parent: null, context, root);
            probe.ReadMediaQuery = true;
            probe.ThrowOnBuild = true;

            Assert.Throws<InvalidOperationException>(() => context.UpdateTheme(CreateTestTheme()));
            Assert.That(probe.BuildCount, Is.EqualTo(2));
            probe.ThrowOnBuild = false;

            context.UpdateMediaQuery(new MediaQueryData(200f, 100f));
            Assert.That(probe.BuildCount, Is.EqualTo(2));

            context.UpdateTheme(CreateTestTheme());
            Assert.That(probe.BuildCount, Is.EqualTo(3));
            node.Unmount();
        }

        [Test]
        public void StatefulWidget_InitializesBuildsAndRebuildsOnlyItsLocalSubtree() {
            var root = new VisualElement();
            var node = (StatefulWidgetNode)new CounterWidget().CreateNode();

            node.Mount(parent: null, new BuildContext(), root);
            var state = (CounterState)node.State;

            Assert.That(state.InitializationCount, Is.EqualTo(1));
            Assert.That(((Label)root[0]).text, Is.EqualTo("0"));
            var initialLabel = root[0];

            state.Increment();

            Assert.That(((Label)root[0]).text, Is.EqualTo("1"));
            Assert.That(root[0], Is.SameAs(initialLabel));
            Assert.That(root.childCount, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void StatefulWidget_ReconcilesStatelessAndTextChildrenWithoutRemounting() {
            var root = new VisualElement();
            var widget = new StatelessHostWidget();
            using var mount = Framework.Mount(widget, root);
            var initialLabel = root[0][0];

            widget.MountedState.SetText("Updated");

            Assert.That(root[0][0], Is.SameAs(initialLabel));
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Updated"));
        }

        [Test]
        public void SingleChildLayoutWrappers_UpdateInPlaceAndPreserveNestedState() {
            var root = new VisualElement();
            var widget = new LayoutWrapperHostWidget();
            using var mount = Framework.Mount(widget, root);
            var host = root[0];
            var sizedBox = host[0];
            var padding = sizedBox[0];
            var align = padding[0];
            var container = align[0];
            var opacity = container[0];
            var label = opacity[0];
            var counterState = widget.MountedState.LatestCounter.MountedState;
            counterState.Increment();

            widget.MountedState.UpdateLayout();

            Assert.That(root[0], Is.SameAs(host));
            Assert.That(host[0], Is.SameAs(sizedBox));
            Assert.That(sizedBox[0], Is.SameAs(padding));
            Assert.That(padding[0], Is.SameAs(align));
            Assert.That(align[0], Is.SameAs(container));
            Assert.That(container[0], Is.SameAs(opacity));
            Assert.That(opacity[0], Is.SameAs(label));
            Assert.That(widget.MountedState.LatestCounter.MountedState, Is.SameAs(counterState));
            Assert.That(((Label)label).text, Is.EqualTo("1"));
            Assert.That(sizedBox.style.width.value.value, Is.EqualTo(240f));
            Assert.That(padding.style.paddingLeft.value.value, Is.EqualTo(16f));
            Assert.That(align.style.justifyContent.value, Is.EqualTo(Justify.FlexEnd));
            Assert.That(container.style.backgroundColor.value, Is.EqualTo(Color.blue));
            Assert.That(opacity.style.opacity.value, Is.EqualTo(0.5f));
        }

        [Test]
        public void Row_ReordersArbitraryKeyedSubtreesAndPreservesNestedState() {
            var root = new VisualElement();
            var widget = new ReorderableCountersWidget();
            using var mount = Framework.Mount(widget, root);
            var state = widget.MountedState;
            var firstState = state.LatestFirst.MountedState;
            var secondState = state.LatestSecond.MountedState;
            firstState.Increment();

            state.Reverse();

            Assert.That(state.LatestFirst.MountedState, Is.SameAs(firstState));
            Assert.That(state.LatestSecond.MountedState, Is.SameAs(secondState));
            Assert.That(((Label)root[0][0][0]).text, Is.EqualTo("0"));
            Assert.That(((Label)root[0][0][1]).text, Is.EqualTo("1"));
        }

        [Test]
        public void Row_RejectsDuplicateGeneralWidgetKeys() {
            var key = new WidgetKey("duplicate");
            var row = new Row(new Widget[]
            {
            new Text("First").WithKey(key),
            new Text("Second").WithKey(key)
            });

            var exception = Assert.Throws<InvalidOperationException>(() => Framework.Mount(row, new VisualElement()));
            Assert.That(exception!.Message, Does.Contain("duplicate"));
            Assert.That(exception.Message, Does.Contain("indexes 0 and 1"));
            Assert.That(exception.Message, Does.Contain("Row"));
        }

        [Test]
        public void Row_ChangingAKeyDisposesThePreviousStateExactlyOnce() {
            var root = new VisualElement();
            var widget = new RekeyedCounterHostWidget();
            using var mount = Framework.Mount(widget, root);
            var firstState = widget.MountedState.LatestCounter.MountedState;
            var firstLabel = root[0][0][0];

            widget.MountedState.ChangeKey();

            Assert.That(widget.MountedState.LatestCounter.MountedState, Is.Not.SameAs(firstState));
            Assert.That(root[0][0][0], Is.Not.SameAs(firstLabel));
            Assert.That(firstState.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Row_RemovingAKeyedChildRunsItsCleanupExactlyOnce() {
            var root = new VisualElement();
            var cleanupCount = 0;
            var visible = new State<bool>(true);
            using var mount = Framework.Mount(
                new ReactiveBuilder<bool>(visible, show => new Row(show
                    ? new Widget[] { new CleanupWidget(() => cleanupCount++).WithKey(new WidgetKey("temporary")) }
                    : Array.Empty<Widget>())),
                root);

            visible.Value = false;
            visible.Value = true;

            Assert.That(cleanupCount, Is.EqualTo(1));
        }

        [Test]
        public void Row_FailedReconciliationCleansNewChildrenAndKeepsExistingHierarchyValid() {
            var root = new VisualElement();
            var cleanupCount = 0;
            var widget = new FailingRowUpdateWidget(() => cleanupCount++);
            using var mount = Framework.Mount(widget, root);
            var row = root[0][0];
            var existingLabel = row[0];

            Assert.Throws<InvalidOperationException>(widget.MountedState.TriggerFailure);

            Assert.That(row.childCount, Is.EqualTo(1));
            Assert.That(row[0], Is.SameAs(existingLabel));
            Assert.That(((Label)row[0]).text, Is.EqualTo("Updated before failure"));
            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(mount.IsMounted, Is.True);
        }

        [Test]
        public void Row_MultipleRemovedChildCleanupFailuresStillCommitTheNewHierarchy() {
            var root = new VisualElement();
            var visible = new State<bool>(true);
            var mount = Framework.Mount(
                new ReactiveBuilder<bool>(visible, show => new Row(show
                    ? new Widget[]
                    {
                    new CleanupWidget(() => throw new InvalidOperationException("First cleanup failure.")),
                    new CleanupWidget(() => throw new ArgumentException("Second cleanup failure."))
                    }
                    : Array.Empty<Widget>())),
                root);
            var row = root[0][0];

            var exception = Assert.Throws<InvalidOperationException>(() => visible.Value = false);

            Assert.That(row.childCount, Is.Zero);
            Assert.That(mount.IsMounted, Is.True);
            Assert.That(exception!.InnerException, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)exception.InnerException!).InnerExceptions, Has.Count.EqualTo(2));
            Assert.DoesNotThrow(mount.Dispose);
        }

        [Test]
        public void StatefulWidget_DisposesStateAndRejectsSetStateAfterUnmount() {
            var root = new VisualElement();
            var node = (StatefulWidgetNode)new CounterWidget().CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var state = (CounterState)node.State;

            node.Unmount();

            Assert.That(state.DisposeCount, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => state.Increment());
        }

        [Test]
        public void StatefulWidget_FailedRebuildLeavesTheCurrentSubtreeMounted() {
            var root = new VisualElement();
            var node = (StatefulWidgetNode)new FailingStatefulWidget().CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var state = (FailingState)node.State;

            Assert.Throws<InvalidOperationException>(state.FailOnNextBuild);

            Assert.That(((Label)root[0]).text, Is.EqualTo("Stable"));
            node.Unmount();
        }

        [Test]
        public void KeyedColumn_ReordersStatefulChildrenAndPreservesTheirMountedState() {
            var firstWidget = new CounterWidget();
            var secondWidget = new CounterWidget();
            var children = new State<IReadOnlyList<KeyedChild>>(new KeyedChild[]
            {
            new(new WidgetKey("first"), firstWidget),
            new(new WidgetKey("second"), secondWidget)
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(new KeyedColumn(children, gap: 8f), root);
            var column = root[0][0];
            firstWidget.MountedState!.Increment();
            var firstState = firstWidget.MountedState;
            var secondState = secondWidget.MountedState;
            var nextFirstWidget = new CounterWidget();
            var nextSecondWidget = new CounterWidget();

            children.Value = new KeyedChild[]
            {
            new(new WidgetKey("second"), nextSecondWidget),
            new(new WidgetKey("first"), nextFirstWidget)
            };

            Assert.That(nextFirstWidget.MountedState, Is.SameAs(firstState));
            Assert.That(nextSecondWidget.MountedState, Is.SameAs(secondState));
            Assert.That(((Label)column[0]).text, Is.EqualTo("0"));
            Assert.That(((Label)column[1]).text, Is.EqualTo("1"));
            Assert.That(column[0].style.marginBottom.value.value, Is.EqualTo(8f));
        }

        [Test]
        public void KeyedColumn_RejectsDuplicateKeys() {
            var children = new State<IReadOnlyList<KeyedChild>>(new KeyedChild[]
            {
            new(new WidgetKey("same"), new Text("One")),
            new(new WidgetKey("same"), new Text("Two"))
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new KeyedColumn(children), new VisualElement()));
            Assert.That(exception!.Message, Does.Contain("same"));
            Assert.That(exception.Message, Does.Contain("indexes 0 and 1"));
        }

        [Test]
        public void KeyedColumn_FailedIncompatibleReplacementRetainsThePreviousKeyedSubtree() {
            var children = new State<IReadOnlyList<KeyedChild>>(new[]
            {
            new KeyedChild(new WidgetKey("content"), new Text("Stable"))
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(new KeyedColumn(children), root);
            var column = root[0][0];
            var stable = column[0];

            Assert.Throws<InvalidOperationException>(() => children.Value = new[]
            {
            new KeyedChild(new WidgetKey("content"), new ThrowingWidget())
            });

            Assert.That(column.childCount, Is.EqualTo(1));
            Assert.That(column[0], Is.SameAs(stable));
            Assert.That(((Label)stable).text, Is.EqualTo("Stable"));
            Assert.That(mount.IsMounted, Is.True);

            Assert.DoesNotThrow(() => children.Value = new[]
            {
            new KeyedChild(new WidgetKey("content"), new Text("Recovered"))
            });
            Assert.That(column[0], Is.SameAs(stable));
            Assert.That(((Label)stable).text, Is.EqualTo("Recovered"));
        }

        [Test]
        public void KeyedChild_RejectsTheDefaultWidgetKey() {
            Assert.That(default(WidgetKey).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new KeyedChild(default, new Text("Item")));
        }

        [Test]
        public void KeyedRow_ReordersChildrenAndAppliesHorizontalGap() {
            var children = new State<IReadOnlyList<KeyedChild>>(new KeyedChild[]
            {
            new(new WidgetKey("left"), new Text("Left")),
            new(new WidgetKey("right"), new Text("Right"))
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(new KeyedRow(children, gap: 6f), root);
            var row = root[0][0];

            children.Value = new KeyedChild[]
            {
            new(new WidgetKey("right"), new Text("Right")),
            new(new WidgetKey("left"), new Text("Left"))
            };

            Assert.That(((Label)row[0]).text, Is.EqualTo("Right"));
            Assert.That(((Label)row[1]).text, Is.EqualTo("Left"));
            Assert.That(row[0].style.marginRight.value.value, Is.EqualTo(6f));
        }

        [Test]
        public void KeyedRow_ReordersStatefulChildrenAndPreservesTheirMountedState() {
            var firstWidget = new CounterWidget();
            var secondWidget = new CounterWidget();
            var children = new State<IReadOnlyList<KeyedChild>>(new KeyedChild[]
            {
            new(new WidgetKey("first"), firstWidget),
            new(new WidgetKey("second"), secondWidget)
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(new KeyedRow(children), root);
            var row = root[0][0];
            firstWidget.MountedState!.Increment();
            var firstState = firstWidget.MountedState;
            var secondState = secondWidget.MountedState;
            var nextFirstWidget = new CounterWidget();
            var nextSecondWidget = new CounterWidget();

            children.Value = new KeyedChild[]
            {
            new(new WidgetKey("second"), nextSecondWidget),
            new(new WidgetKey("first"), nextFirstWidget)
            };

            Assert.That(nextFirstWidget.MountedState, Is.SameAs(firstState));
            Assert.That(nextSecondWidget.MountedState, Is.SameAs(secondState));
            Assert.That(((Label)row[0]).text, Is.EqualTo("0"));
            Assert.That(((Label)row[1]).text, Is.EqualTo("1"));
        }

        [Test]
        public void KeyedColumn_RemovingAChildDisposesItsMountedState() {
            var counter = new CounterWidget();
            var children = new State<IReadOnlyList<KeyedChild>>(new KeyedChild[]
            {
            new(new WidgetKey("counter"), counter)
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(new KeyedColumn(children), root);
            var state = counter.MountedState!;

            children.Value = Array.Empty<KeyedChild>();

            Assert.That(state.DisposeCount, Is.EqualTo(1));
            Assert.That(root[0][0].childCount, Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(state.Increment);
        }

        [Test]
        public void KeyedColumn_CompatibleUpdatePreservesChildrenAndSwitchesCollectionStateAndGap() {
            var firstCounter = new CounterWidget();
            var firstChildren = new State<IReadOnlyList<KeyedChild>>(new[]
            {
            new KeyedChild(new WidgetKey("counter"), firstCounter),
            new KeyedChild(new WidgetKey("footer"), new Text("Footer"))
        });
            var secondCounter = new CounterWidget();
            var secondChildren = new State<IReadOnlyList<KeyedChild>>(new[]
            {
            new KeyedChild(new WidgetKey("counter"), secondCounter),
            new KeyedChild(new WidgetKey("footer"), new Text("Updated"))
        });
            var root = new VisualElement();
            var node = (KeyedColumnNode)new KeyedColumn(firstChildren, gap: 4f).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var column = root[0];
            var counterNative = column[0];
            var counterState = firstCounter.MountedState;

            Assert.That(node.TryUpdate(new KeyedColumn(secondChildren, gap: 10f)), Is.True);
            Assert.That(column[0], Is.SameAs(counterNative));
            Assert.That(secondCounter.MountedState, Is.SameAs(counterState));
            Assert.That(column[0].style.marginBottom.value.value, Is.EqualTo(10f));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Updated"));
            firstChildren.Value = Array.Empty<KeyedChild>();
            Assert.That(column.childCount, Is.EqualTo(2));
            secondChildren.Value = new[] { new KeyedChild(new WidgetKey("counter"), secondCounter) };
            Assert.That(column.childCount, Is.EqualTo(1));
            Assert.That(column[0], Is.SameAs(counterNative));
            node.Unmount();
        }

        [Test]
        public void Navigator_PushPopReplaceAndPopToRootRetainHistoryAndCleanRemovedEntries() {
            var root = new VisualElement();
            var home = new CounterWidget();
            var navigator = new Navigator(home);
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            var host = root[0][0];
            var homeEntry = host[0];
            var homeElement = homeEntry[0];
            var homeState = home.MountedState;
            homeState.Increment();

            var settings = new CounterWidget();
            navigator.Push(settings);
            var settingsEntry = host[1];
            var settingsState = settings.MountedState;

            Assert.That(navigator.Depth, Is.EqualTo(2));
            Assert.That(navigator.CanPop, Is.True);
            Assert.That(navigator.DepthState.Value, Is.EqualTo(2));
            Assert.That(navigator.CanPopState.Value, Is.True);
            Assert.That(host.childCount, Is.EqualTo(2));
            Assert.That(homeEntry.parent, Is.SameAs(host));
            Assert.That(homeEntry.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(homeEntry.enabledSelf, Is.False);
            Assert.That(homeElement.parent, Is.SameAs(homeEntry));
            Assert.That(homeState.IsMounted, Is.True);
            Assert.That(((Label)homeElement).text, Is.EqualTo("1"));
            Assert.That(settingsEntry.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(settingsEntry.enabledSelf, Is.True);

            navigator.Replace(new Text("Audio"));
            var audioEntry = host[1];

            Assert.That(navigator.Depth, Is.EqualTo(2));
            Assert.That(host.childCount, Is.EqualTo(2));
            Assert.That(homeEntry.parent, Is.SameAs(host));
            Assert.That(settingsEntry.parent, Is.Null);
            Assert.That(settingsState.IsMounted, Is.False);
            Assert.That(settingsState.DisposeCount, Is.EqualTo(1));
            Assert.That(((Label)audioEntry[0]).text, Is.EqualTo("Audio"));

            navigator.Push(new Text("Advanced"));
            var advancedEntry = host[2];
            Assert.That(navigator.Pop(), Is.True);
            Assert.That(host.childCount, Is.EqualTo(2));
            Assert.That(advancedEntry.parent, Is.Null);
            Assert.That(host[1], Is.SameAs(audioEntry));
            Assert.That(((Label)audioEntry[0]).text, Is.EqualTo("Audio"));

            Assert.That(navigator.PopToRoot(), Is.True);
            Assert.That(navigator.Depth, Is.EqualTo(1));
            Assert.That(navigator.CanPop, Is.False);
            Assert.That(navigator.DepthState.Value, Is.EqualTo(1));
            Assert.That(navigator.CanPopState.Value, Is.False);
            Assert.That(host.childCount, Is.EqualTo(1));
            Assert.That(host[0], Is.SameAs(homeEntry));
            Assert.That(homeEntry.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(homeEntry.enabledSelf, Is.True);
            Assert.That(home.MountedState, Is.SameAs(homeState));
            Assert.That(((Label)homeEntry[0]).text, Is.EqualTo("1"));
            Assert.That(navigator.Pop(), Is.False);
        }

        [Test]
        public void Navigator_PushAndHostDisposalCleanUpRoutesAndDetachTheController() {
            var root = new VisualElement();
            var cleanupCount = 0;
            var navigator = new Navigator(new CleanupWidget(() => cleanupCount++));
            var mount = Framework.Mount(new NavigatorHost(navigator), root);
            var host = root[0][0];
            var rootEntry = host[0];

            navigator.Push(new CleanupWidget(() => cleanupCount++));

            Assert.That(cleanupCount, Is.Zero);

            Assert.That(navigator.Pop(), Is.True);
            Assert.That(cleanupCount, Is.EqualTo(1));

            navigator.Push(new CleanupWidget(() => cleanupCount++));
            var retainedEntry = host[1];
            Assert.That(cleanupCount, Is.EqualTo(1));

            mount.Dispose();

            Assert.That(cleanupCount, Is.EqualTo(3));
            Assert.That(host.childCount, Is.Zero);
            Assert.That(rootEntry.parent, Is.Null);
            Assert.That(retainedEntry.parent, Is.Null);
            Assert.Throws<InvalidOperationException>(() => navigator.Push(new Text("Detached")));
        }

        [Test]
        public void Navigator_KeyedRoutesRejectDuplicatesAndRestoreTheCompleteStack() {
            var navigator = new Navigator(new Route(new WidgetKey("home"), new Text("Home")));
            var root = new VisualElement();
            using (Framework.Mount(new NavigatorHost(navigator), root)) {
                navigator.Push(new Route(
                    new WidgetKey("details"),
                    new Text("Details"),
                    RouteTransition.Fade(TimeSpan.FromSeconds(1))));
                Assert.Throws<InvalidOperationException>(() => navigator.Push(
                    new Route(new WidgetKey("home"), new Text("Duplicate"))));
            }

            var restored = new Navigator(navigator.CaptureSnapshot());
            var restoredRoot = new VisualElement();
            using var restoredMount = Framework.Mount(new NavigatorHost(restored), restoredRoot);

            Assert.That(restored.Depth, Is.EqualTo(2));
            Assert.That(restored.CurrentRoute.Key, Is.EqualTo(new WidgetKey("details")));
            Assert.That(((Label)restoredRoot[0][0][1][0]).text, Is.EqualTo("Details"));
            Assert.That(restoredRoot[0][0][1].style.opacity.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(restored.Pop(), Is.True);
            Assert.That(restored.CurrentRoute.Key, Is.EqualTo(new WidgetKey("home")));
        }

        [Test]
        public void Navigator_ClearAndPushCommitsOneNewRootAndCleansEveryOldRoute() {
            var cleanupCount = 0;
            var navigator = new Navigator(new Route(
                new WidgetKey("home"),
                new CleanupWidget(() => cleanupCount++)));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            navigator.Push(new Route(
                new WidgetKey("details"),
                new CleanupWidget(() => cleanupCount++)));

            navigator.ClearAndPush(new Route(new WidgetKey("login"), new Text("Login")));

            Assert.That(navigator.Depth, Is.EqualTo(1));
            Assert.That(navigator.CanPop, Is.False);
            Assert.That(navigator.CurrentRoute.Key, Is.EqualTo(new WidgetKey("login")));
            Assert.That(cleanupCount, Is.EqualTo(2));
            Assert.That(root[0][0].childCount, Is.EqualTo(1));
            Assert.That(((Label)root[0][0][0][0]).text, Is.EqualTo("Login"));
        }

        [Test]
        public void Navigator_FadeTransitionStartsNewRouteTransparentWithoutChangingRetention() {
            var navigator = new Navigator(new Route(new WidgetKey("home"), new Text("Home")));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);

            navigator.Push(new Route(
                new WidgetKey("details"),
                new Text("Details"),
                RouteTransition.Fade(TimeSpan.FromSeconds(1))));

            Assert.That(root[0][0][0].style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root[0][0][1].style.opacity.value, Is.EqualTo(0f));
            Assert.That(navigator.Depth, Is.EqualTo(2));
        }

        [Test]
        public void Navigator_ReplaceObserverFailureStillCleansTheRemovedRoute() {
            var cleanupCount = 0;
            var navigator = new Navigator(new Route(
                new WidgetKey("home"),
                new CleanupWidget(() => cleanupCount++)));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            using var subscription = navigator.CurrentRouteState.Subscribe(_ =>
                throw new InvalidOperationException("Expected current-route observer failure."));

            Assert.Throws<InvalidOperationException>(() => navigator.Replace(
                new Route(new WidgetKey("settings"), new Text("Settings"))));

            Assert.That(cleanupCount, Is.EqualTo(1));
            Assert.That(navigator.CurrentRoute.Key, Is.EqualTo(new WidgetKey("settings")));
            Assert.That(root[0][0].childCount, Is.EqualTo(1));
            Assert.That(((Label)root[0][0][0][0]).text, Is.EqualTo("Settings"));
        }

        [Test]
        public void NavigatorHost_CompatibleUpdateWithSameControllerPreservesCurrentRoute() {
            var root = new VisualElement();
            var navigator = new Navigator(new Text("Home"));
            var node = (NavigatorHostNode)new NavigatorHost(navigator).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            navigator.Push(new Text("Details"));
            var host = root[0];
            var homeEntry = host[0];
            var route = host[1];

            Assert.That(node.TryUpdate(new NavigatorHost(navigator)), Is.True);
            Assert.That(root[0], Is.SameAs(host));
            Assert.That(host[0], Is.SameAs(homeEntry));
            Assert.That(host[1], Is.SameAs(route));
            Assert.That(((Label)route[0]).text, Is.EqualTo("Details"));
            Assert.That(node.TryUpdate(new NavigatorHost(new Navigator(new Text("Other")))), Is.False);
            node.Unmount();
        }

        [Test]
        public void Navigator_InactiveRouteRetainsFocusAttachmentButCannotRequestFocus() {
            var focusNode = new FocusNode();
            var navigator = new Navigator(new TextField(new State<string>("Home"), focusNode: focusNode));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            var host = root[0][0];
            var homeEntry = host[0];

            Assert.That(focusNode.RequestFocus(), Is.True);

            navigator.Push(new Text("Details"));

            Assert.That(homeEntry.enabledSelf, Is.False);
            Assert.That(focusNode.RequestFocus(), Is.False);

            Assert.That(navigator.Pop(), Is.True);
            Assert.That(host[0], Is.SameAs(homeEntry));
            Assert.That(homeEntry.enabledSelf, Is.True);
            Assert.That(focusNode.RequestFocus(), Is.True);
        }

        [Test]
        public void Navigator_FailedPushLeavesCurrentRouteAndStackUnchanged() {
            var navigator = new Navigator(new Text("Home"));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            var host = root[0][0];
            var homeEntry = host[0];

            Assert.Throws<InvalidOperationException>(() => navigator.Push(new ThrowingWidget()));

            Assert.That(navigator.Depth, Is.EqualTo(1));
            Assert.That(navigator.DepthState.Value, Is.EqualTo(1));
            Assert.That(navigator.CanPopState.Value, Is.False);
            Assert.That(host.childCount, Is.EqualTo(1));
            Assert.That(host[0], Is.SameAs(homeEntry));
            Assert.That(homeEntry.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(((Label)homeEntry[0]).text, Is.EqualTo("Home"));

            navigator.Push(new Text("Recovered"));
            Assert.That(navigator.Depth, Is.EqualTo(2));
            Assert.That(((Label)host[1][0]).text, Is.EqualTo("Recovered"));
        }

        [Test]
        public void Navigator_ObserverFailureDoesNotLeaveStackAndMountedEntriesOutOfSync() {
            var navigator = new Navigator(new Text("Home"));
            var root = new VisualElement();
            using var mount = Framework.Mount(new NavigatorHost(navigator), root);
            var host = root[0][0];
            using var subscription = navigator.DepthState.Subscribe(_ =>
                throw new InvalidOperationException("Expected observer failure."));

            Assert.Throws<InvalidOperationException>(() => navigator.Push(new Text("Details")));

            Assert.That(navigator.Depth, Is.EqualTo(2));
            Assert.That(navigator.DepthState.Value, Is.EqualTo(2));
            Assert.That(navigator.CanPopState.Value, Is.True);
            Assert.That(host.childCount, Is.EqualTo(2));
            Assert.That(host[0].style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(host[1].style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(((Label)host[1][0]).text, Is.EqualTo("Details"));

            subscription.Dispose();
            Assert.That(navigator.Pop(), Is.True);
            Assert.That(host.childCount, Is.EqualTo(1));
            Assert.That(((Label)host[0][0]).text, Is.EqualTo("Home"));
        }

        [Test]
        public void BackNavigation_ClosesTheTopOverlayBeforePoppingTheCurrentRoute() {
            var root = new VisualElement();
            var overlay = new OverlayController();
            var navigator = new Navigator(new Text("Home"));
            var node = (BackNavigationNode)new BackNavigation(
                new OverlayHost(new NavigatorHost(navigator), overlay),
                navigator,
                overlay).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            navigator.Push(new Text("Details"));
            var entry = overlay.ShowModal(new Text("Dialog"));

            Assert.That(node.TryHandleBack(), Is.True);
            Assert.That(entry.IsOpen, Is.False);
            Assert.That(navigator.Depth, Is.EqualTo(2));

            Assert.That(node.TryHandleBack(), Is.True);
            Assert.That(navigator.Depth, Is.EqualTo(1));
            Assert.That(node.TryHandleBack(), Is.False);
            node.Unmount();
        }

        [Test]
        public void BackNavigation_CompatibleUpdatePreservesChildStateAndUsesLatestControllers() {
            var firstNavigator = new Navigator(new Text("First"));
            var secondNavigator = new Navigator(new Text("Second"));
            var firstOverlay = new OverlayController();
            var secondOverlay = new OverlayController();
            var firstCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (BackNavigationNode)new BackNavigation(
                firstCounter,
                firstNavigator,
                firstOverlay).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];
            var state = firstCounter.MountedState;
            var secondCounter = new CounterWidget();

            Assert.That(node.TryUpdate(new BackNavigation(
                secondCounter,
                secondNavigator,
                secondOverlay)), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(secondCounter.MountedState, Is.SameAs(state));
            Assert.That(node.TryHandleBack(), Is.False);
            node.Unmount();
        }

        [Test]
        public void Navigator_ResolvesNearestScopedNavigatorAndPreservesTheme() {
            var root = new VisualElement();
            var outerCapture = new NavigatorCaptureWidget();
            var innerCapture = new NavigatorCaptureWidget();
            var outer = new Navigator(
                new Column(new Widget[]
                {
                outerCapture,
                new NavigatorHost(new Navigator(innerCapture))
                }));
            var theme = CreateTestTheme();

            using var mount = Framework.Mount(new Theme(theme, new NavigatorHost(outer)), root);

            Assert.That(outerCapture.Navigator, Is.SameAs(outer));
            Assert.That(innerCapture.Navigator, Is.Not.SameAs(outer));
            Assert.That(outerCapture.Theme, Is.SameAs(theme));
            Assert.That(innerCapture.Theme, Is.SameAs(theme));
        }

        [Test]
        public void Navigator_RejectsNavigationDuringRouteBuild() {
            var root = new VisualElement();
            var navigator = new Navigator(new NavigationDuringBuildWidget());

            Assert.Throws<InvalidOperationException>(() => Framework.Mount(new NavigatorHost(navigator), root));

            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Toggle_MapsControlledStateAndNativeConfiguration() {
            var root = new VisualElement();
            var state = new State<bool>(true);
#pragma warning disable CS0618 // Legacy compatibility contract.
            using var mount = Framework.Mount(new Toggle(state, label: "Enabled", enabled: false), root);
#pragma warning restore CS0618
            var toggle = (UnityEngine.UIElements.Toggle)root[0][0];

            Assert.That(toggle.value, Is.True);
            Assert.That(toggle.label, Is.EqualTo("Enabled"));
            Assert.That(toggle.enabledSelf, Is.False);
        }

        [Test]
        public void Toggle_UpdatesInBothDirectionsAndStopsObservingAfterUnmount() {
            var root = new VisualElement();
            var state = new State<bool>(false);
#pragma warning disable CS0618 // Legacy compatibility contract.
            var node = (ToggleNode)new Toggle(state).CreateNode();
#pragma warning restore CS0618
            node.Mount(parent: null, new BuildContext(), root);
            var toggle = (UnityEngine.UIElements.Toggle)root[0];

            node.HandleValueChanged(true);

            Assert.That(state.Value, Is.True);

            state.Value = false;

            Assert.That(toggle.value, Is.False);

            node.Unmount();
            state.Value = true;
            node.HandleValueChanged(false);

            Assert.That(toggle.value, Is.False);
            Assert.That(state.Value, Is.True);
        }

        [Test]
        public void Toggle_UsesThemeColorsForItsNativeInput() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
#pragma warning disable CS0618 // Legacy compatibility contract.
            using var mount = Framework.Mount(new Theme(theme, new Toggle(new State<bool>(true), "Enabled")), root);
#pragma warning restore CS0618
            var toggle = (UnityEngine.UIElements.Toggle)root[0][0];
            var input = toggle.Q<VisualElement>(className: "unity-toggle__input");

            Assert.That(input, Is.Not.Null);
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(theme.Colors.Primary));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(theme.Colors.Outline));
        }

        [Test]
        public void Slider_MapsControlledStateRangeAndNativeConfiguration() {
            var root = new VisualElement();
            var state = new State<float>(0.25f);
            using var mount = Framework.Mount(new Slider(state, min: 0f, max: 1f, label: "Volume", enabled: false), root);
            var slider = (UnityEngine.UIElements.Slider)root[0][0];

            Assert.That(slider.value, Is.EqualTo(0.25f));
            Assert.That(slider.lowValue, Is.EqualTo(0f));
            Assert.That(slider.highValue, Is.EqualTo(1f));
            Assert.That(slider.label, Is.EqualTo("Volume"));
            Assert.That(slider.enabledSelf, Is.False);
        }

        [Test]
        public void Slider_UpdatesInBothDirectionsAndStopsObservingAfterUnmount() {
            var root = new VisualElement();
            var state = new State<float>(0.25f);
            var node = (SliderNode)new Slider(state, min: 0f, max: 1f).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var slider = (UnityEngine.UIElements.Slider)root[0];

            node.HandleValueChanged(0.75f);

            Assert.That(state.Value, Is.EqualTo(0.75f));

            state.Value = 0.5f;

            Assert.That(slider.value, Is.EqualTo(0.5f));

            node.Unmount();
            state.Value = 0.25f;
            node.HandleValueChanged(0.75f);

            Assert.That(slider.value, Is.EqualTo(0.5f));
            Assert.That(state.Value, Is.EqualTo(0.25f));
        }

        [Test]
        public void Slider_RejectsInvalidRangeAndInitialValue() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Slider(new State<float>(0.5f), min: 1f, max: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Slider(new State<float>(2f), min: 0f, max: 1f));
        }

        [Test]
        public void Slider_StateStyleResolvesDisabledStateAndCustomGeometry() {
            var observed = WidgetStates.None;
            var style = new SliderStyle(
                thumbColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    observed = states;
                    return Color.magenta;
                }),
                trackHeight: WidgetStateProperty<float?>.All(3f),
                thumbSize: WidgetStateProperty<float?>.All(14f));
            var root = new VisualElement();
            using var mount = Framework.Mount(new Slider(new State<float>(0.5f), 0f, 1f, enabled: false, style: style), root);
            var slider = (UnityEngine.UIElements.Slider)root[0][0];
            var tracker = slider.Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.trackerUssClassName);
            var thumb = slider.Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.draggerUssClassName);

            Assert.That(observed, Is.EqualTo(WidgetStates.Disabled));
            Assert.That(tracker, Is.Not.Null);
            Assert.That(thumb, Is.Not.Null);
            Assert.That(tracker.style.height.value.value, Is.EqualTo(3f));
            Assert.That(thumb.style.width.value.value, Is.EqualTo(14f));
            Assert.That(thumb.style.backgroundColor.value, Is.EqualTo(Color.magenta));
        }

        [Test]
        public void Slider_FormFieldSuppliesErrorStateAndRebindsOnCompatibleUpdate() {
            var first = new FormField<float>(new State<float>(0.25f));
            var second = new FormField<float>(new State<float>(0.75f));
            first.ErrorText.Value = "First error";
            var observed = WidgetStates.None;
            var style = new SliderStyle(
                thumbColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    observed = states;
                    return Color.magenta;
                }));
            var root = new VisualElement();
            var node = (SliderNode)new Slider(first, 0f, 1f, style: style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(observed.HasFlag(WidgetStates.Error), Is.True);
            Assert.That(node.TryUpdate(new Slider(second, 0f, 1f, style: style)), Is.True);
            Assert.That(observed.HasFlag(WidgetStates.Error), Is.False);

            second.ErrorText.Value = "Second error";

            Assert.That(observed.HasFlag(WidgetStates.Error), Is.True);
            var firstFieldReuse = (SliderNode)new Slider(first, 0f, 1f).CreateNode();
            Assert.DoesNotThrow(() => firstFieldReuse.Mount(parent: null, new BuildContext(), root));
            firstFieldReuse.Unmount();
            node.Unmount();
        }

        [Test]
        public void Switch_StateStyleResolvesSelectedAndPreservesNativeElementOnUpdate() {
            var first = new State<bool>(true);
            var second = new State<bool>(false);
            var version = new State<int>(0);
            var observed = WidgetStates.None;
            var style = new SwitchStyle(
                trackColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    observed = states;
                    return (states & WidgetStates.Selected) != 0 ? Color.green : Color.gray;
                }),
                width: WidgetStateProperty<float?>.All(48f),
                height: WidgetStateProperty<float?>.All(28f));
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<int>(version, value => new Switch(value == 0 ? first : second, style: style)),
                root);
            var native = root[0][0];

            Assert.That(observed, Is.EqualTo(WidgetStates.Selected));
            Assert.That(native.style.width.value.value, Is.EqualTo(48f));
            Assert.That(native[0].style.backgroundColor.value, Is.EqualTo(Color.green));

            version.Value = 1;

            Assert.That(root[0][0], Is.SameAs(native));
            Assert.That(native[0].style.backgroundColor.value, Is.EqualTo(Color.gray));
            first.Value = false;
            Assert.That(native[0].style.backgroundColor.value, Is.EqualTo(Color.gray));
            second.Value = true;
            Assert.That(native[0].style.backgroundColor.value, Is.EqualTo(Color.green));
        }

        [Test]
        public void Button_UsesNativeButtonAndInvokesOnPressed() {
            var root = new VisualElement();
            var invocationCount = 0;
            var node = (ButtonNode)new Button("Save", () => invocationCount++).CreateNode();

            node.Mount(parent: null, new BuildContext(), root);
            var button = (UnityEngine.UIElements.Button)root[0];

            node.HandleClicked();

            Assert.That(button.text, Is.EqualTo("Save"));
            Assert.That(invocationCount, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void Button_CallbackCanUnmountItsCurrentMount() {
            var root = new VisualElement();
            ButtonNode node = null;
            var widget = new Button("Close", () => node.Unmount());
            node = (ButtonNode)widget.CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            Assert.DoesNotThrow(node.HandleClicked);

            Assert.That(node.IsMounted, Is.False);
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void ButtonStyle_PrimaryAppliesSemanticSurfaceTypographyAndMinimumSize() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Button("Upgrade", onPressed: () => { }, style: ButtonStyle.Primary),
                root);
            var button = (UnityEngine.UIElements.Button)root[0][0];

            Assert.That(button.style.backgroundColor.value, Is.EqualTo(new Color(0.56f, 0.47f, 1f)));
            Assert.That(button.style.color.value, Is.EqualTo(new Color(0.10f, 0.08f, 0.18f)));
            Assert.That(button.style.minWidth.value.value, Is.EqualTo(64f));
            Assert.That(button.style.minHeight.value.value, Is.EqualTo(40f));
            Assert.That(button.style.borderTopLeftRadius.value.value, Is.EqualTo(22f));
            Assert.That(button.style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
        }

        [Test]
        public void ButtonStyle_RejectsInvalidMinimumSize() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ButtonStyle(minimumSize: new Vector2(-1f, 40f)));
        }

        [Test]
        public void Button_DisabledStyleDisablesNativeInputAndUsesDisabledVisuals() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Button("Unavailable", onPressed: () => { }, style: ButtonStyle.Primary, enabled: false),
                root);
            var button = (UnityEngine.UIElements.Button)root[0][0];

            Assert.That(button.enabledSelf, Is.False);
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(new Color(0.26f, 0.24f, 0.32f)));
        }

        [Test]
        public void ButtonStyle_ExplicitValuesOverrideTheSemanticDefaultsAndCenterText() {
            var style = new ButtonStyle(
                background: Color.red,
                foreground: Color.yellow,
                padding: EdgeInsets.All(6f),
                shape: BorderRadius.All(8f),
                typography: new TextStyle(fontSize: 18f, fontStyle: FontStyle.Bold),
                minimumSize: new Vector2(120f, 48f));
            var root = new VisualElement();
            using var mount = Framework.Mount(new Button("Custom", () => { }, style), root);
            var button = (UnityEngine.UIElements.Button)root[0][0];

            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.red));
            Assert.That(button.style.color.value, Is.EqualTo(Color.yellow));
            Assert.That(button.style.paddingTop.value.value, Is.EqualTo(6f));
            Assert.That(button.style.borderTopLeftRadius.value.value, Is.EqualTo(8f));
            Assert.That(button.style.fontSize.value.value, Is.EqualTo(18f));
            Assert.That(button.style.minWidth.value.value, Is.EqualTo(120f));
            Assert.That(button.style.minHeight.value.value, Is.EqualTo(48f));
            Assert.That(button.style.unityTextAlign.value, Is.EqualTo(TextAnchor.MiddleCenter));
        }

        [Test]
        public void Button_HoveredAndPressedStatesOverrideTheNormalSurface() {
            var style = new ButtonStyle(
                background: Color.black,
                hovered: new ButtonStateStyle(background: Color.blue),
                pressed: new ButtonStateStyle(background: Color.red));
            var root = new VisualElement();
            var node = (ButtonNode)new Button("State", () => { }, style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var button = (UnityEngine.UIElements.Button)root[0];

            node.ApplyHoveredStyle();
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.blue));
            node.ApplyPressedStyle();
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.red));
            node.Unmount();
        }

        [Test]
        public void WidgetStateProperty_ResolvesCombinedStatesAndAllValuesHaveStableEquality() {
            var observed = WidgetStates.None;
            var property = WidgetStateProperty<Color?>.ResolveWith(states =>
            {
                observed = states;
                return (states & WidgetStates.Pressed) != 0 ? Color.red : Color.black;
            });

            Assert.That(property.Resolve(WidgetStates.Focused | WidgetStates.Pressed), Is.EqualTo(Color.red));
            Assert.That(observed, Is.EqualTo(WidgetStates.Focused | WidgetStates.Pressed));
            Assert.That(WidgetStateProperty<Color>.All(Color.green), Is.EqualTo(new WidgetStatePropertyAll<Color>(Color.green)));
        }

        [Test]
        public void Button_StatePropertiesReceiveAllActiveStatesAndOverrideLegacyStateStyles() {
            var observed = WidgetStates.None;
            var style = new ButtonStyle(
                background: Color.black,
                pressed: new ButtonStateStyle(background: Color.yellow),
                backgroundColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    observed = states;
                    return (states & WidgetStates.Pressed) != 0 ? Color.red : null;
                }));
            var root = new VisualElement();
            var node = (ButtonNode)new Button("State", () => { }, style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            node.SetFocused(true);
            node.SetPressed(true);

            Assert.That(observed, Is.EqualTo(WidgetStates.Focused | WidgetStates.Pressed));
            Assert.That(root[0].style.backgroundColor.value, Is.EqualTo(Color.red));
            node.Unmount();
        }

        [Test]
        public void Button_InteractionFlagsPreserveFocusAfterPointerLeaves() {
            var style = new ButtonStyle(
                background: Color.black,
                hovered: new ButtonStateStyle(background: Color.blue),
                pressed: new ButtonStateStyle(background: Color.red),
                focused: new ButtonStateStyle(background: Color.green));
            var root = new VisualElement();
            var node = (ButtonNode)new Button("State", () => { }, style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var button = (UnityEngine.UIElements.Button)root[0];

            node.SetFocused(true);
            node.SetHovered(true);
            node.SetPressed(true);
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.red));

            node.SetPressed(false);
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.blue));

            node.SetHovered(false);
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.green));
            node.Unmount();
        }

        [Test]
        public void IconButton_InteractionStateUpdatesIconTintAndPreservesFocus() {
            var style = new ButtonStyle(
                foreground: Color.white,
                hovered: new ButtonStateStyle(foreground: Color.blue),
                focused: new ButtonStateStyle(foreground: Color.green));
            var root = new VisualElement();
            var node = (IconButtonNode)new IconButton(LumaIcons.Home, () => { }, style: style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var button = (UnityEngine.UIElements.Button)root[0];
            var icon = (UnityEngine.UIElements.Image)button[0];

            node.SetFocused(true);
            node.SetHovered(true);
            Assert.That(icon.tintColor, Is.EqualTo(Color.blue));

            node.SetHovered(false);
            Assert.That(icon.tintColor, Is.EqualTo(Color.green));
            node.Unmount();
        }

        [Test]
        public void Button_CallbackDoesNotRunAfterUnmount() {
            var root = new VisualElement();
            var invocationCount = 0;
            var node = (ButtonNode)new Button("Safe", () => invocationCount++).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            node.Unmount();

            node.HandleClicked();

            Assert.That(invocationCount, Is.Zero);
        }

        [Test]
        public void Button_DisabledDoesNotInvokeItsCallback() {
            var root = new VisualElement();
            var invocationCount = 0;
            var node = (ButtonNode)new Button("Unavailable", () => invocationCount++, enabled: false).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            node.HandleClicked();

            Assert.That(invocationCount, Is.Zero);
            node.Unmount();
        }

        [Test]
        public async Task AsyncAction_TracksSuccessAndPreventsConcurrentRuns() {
            var completion = new TaskCompletionSource<bool>();
            var calls = 0;
            var succeeded = 0;
            var action = new AsyncAction(
                () => {
                    calls++;
                    return completion.Task;
                },
                onSucceeded: () => succeeded++);

            var first = action.Run();
            var duplicate = action.Run();

            Assert.That(action.Status.Value, Is.EqualTo(AsyncActionStatus.Running));
            Assert.That(action.IsRunning.Value, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(duplicate, Is.SameAs(first));

            completion.SetResult(true);
            await first;

            Assert.That(action.Status.Value, Is.EqualTo(AsyncActionStatus.Succeeded));
            Assert.That(action.IsRunning.Value, Is.False);
            Assert.That(action.Error.Value, Is.Null);
            Assert.That(succeeded, Is.EqualTo(1));
        }

        [Test]
        public async Task AsyncAction_ExposesFailureAndDeliversItToItsOwner() {
            var expected = new InvalidOperationException("Save failed.");
            Exception observed = null;
            var action = new AsyncAction(
                () => Task.FromException(expected),
                onFailed: exception => observed = exception);

            await action.Run();

            Assert.That(action.Status.Value, Is.EqualTo(AsyncActionStatus.Failed));
            Assert.That(action.IsRunning.Value, Is.False);
            Assert.That(action.Error.Value, Is.SameAs(expected));
            Assert.That(observed, Is.SameAs(expected));
        }

        [Test]
        public async Task AsyncAction_CancelPublishesCancelledWithoutAnError() {
            var cancellationObserved = new TaskCompletionSource<bool>();
            var action = new AsyncAction(token =>
            {
                token.Register(() => cancellationObserved.TrySetResult(true));
                return Task.Delay(Timeout.Infinite, token);
            });

            var running = action.Run();
            Assert.That(action.Cancel(), Is.True);
            await cancellationObserved.Task;
            await running;

            Assert.That(action.Status.Value, Is.EqualTo(AsyncActionStatus.Cancelled));
            Assert.That(action.IsRunning.Value, Is.False);
            Assert.That(action.Error.Value, Is.Null);
            Assert.That(action.Cancel(), Is.False);
        }

        [Test]
        public async Task AsyncButton_UsesLoadingLabelAndDisablesWhileItsActionRuns() {
            var completion = new TaskCompletionSource<bool>();
            var action = new AsyncAction(() => completion.Task);
            var root = new VisualElement();
            using var mount = Framework.Mount(new AsyncButton("Save", action, loadingText: "Saving…"), root);

            var running = action.Run();
            var loadingButton = (UnityEngine.UIElements.Button)root[0][0];
            Assert.That(loadingButton.text, Is.EqualTo("Saving…"));
            Assert.That(loadingButton.enabledSelf, Is.False);

            completion.SetResult(true);
            await running;

            var completedButton = (UnityEngine.UIElements.Button)root[0][0];
            Assert.That(completedButton.text, Is.EqualTo("Save"));
            Assert.That(completedButton.enabledSelf, Is.True);
        }

        [Test]
        public async Task AsyncButton_FocusNodeTracksItsInteractiveLifetime() {
            var completion = new TaskCompletionSource<bool>();
            var action = new AsyncAction(() => completion.Task);
            var focusNode = new FocusNode();
            var root = new VisualElement();
            using var mount = Framework.Mount(new AsyncButton("Save", action, focusNode: focusNode), root);

            Assert.That(focusNode.RequestFocus(), Is.True);

            var running = action.Run();
            Assert.That(focusNode.RequestFocus(), Is.False);

            completion.SetResult(true);
            await running;

            Assert.That(focusNode.RequestFocus(), Is.True);
        }

        [Test]
        public async Task AsyncButton_RendersRetryAndInlineFailureThenCancelsOnUnmount() {
            var failure = new InvalidOperationException("Connection lost.");
            var failedAction = new AsyncAction(() => Task.FromException(failure));
            var failedRoot = new VisualElement();
            using var failedMount = Framework.Mount(new AsyncButton("Save", failedAction), failedRoot);

            await failedAction.Run();

            var failureColumn = failedRoot[0][0];
            Assert.That(((UnityEngine.UIElements.Button)failureColumn[0]).text, Is.EqualTo("Retry"));
            Assert.That(((Label)failureColumn[1]).text, Is.EqualTo("Connection lost."));

            var cancelled = new TaskCompletionSource<bool>();
            var runningAction = new AsyncAction(token =>
            {
                token.Register(() => cancelled.TrySetResult(true));
                return Task.Delay(Timeout.Infinite, token);
            });
            var runningRoot = new VisualElement();
            var runningMount = Framework.Mount(new AsyncButton("Save", runningAction), runningRoot);
            var running = runningAction.Run();

            runningMount.Dispose();
            await cancelled.Task;
            await running;

            Assert.That(runningAction.Status.Value, Is.EqualTo(AsyncActionStatus.Cancelled));
        }

        [Test]
        public void Icon_MountsOneNativeImageAndExplicitValuesOverrideTheme() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Theme(CreateTestTheme(), new Icon(LumaIcons.Settings, size: 24f, color: Color.yellow)),
                root);
            var image = (UnityEngine.UIElements.Image)root[0][0];

            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(image.focusable, Is.False);
            Assert.That(image.style.width.value.value, Is.EqualTo(24f));
            Assert.That(image.tintColor, Is.EqualTo(Color.yellow));
            Assert.That(image.vectorImage, Is.Not.Null);
        }

        [Test]
        public void Icon_UsesNearestThemeDefaults() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            using var mount = Framework.Mount(new Theme(theme, new Icon(LumaIcons.Home)), root);
            var image = (UnityEngine.UIElements.Image)root[0][0];

            Assert.That(image.style.width.value.value, Is.EqualTo(theme.IconTheme.Size));
            Assert.That(image.tintColor, Is.EqualTo(theme.IconTheme.Color));
        }

        [Test]
        public void Image_MapsOwnedTextureGeometryFitTintAndCompatibleUpdates() {
            var root = new VisualElement();
            var node = (ImageNode)new global::LumaFlow.Image(
                Texture2D.whiteTexture,
                width: 96f,
                height: 64f,
                fit: ImageFit.Cover,
                tint: Color.cyan,
                semanticsLabel: "Project preview",
                borderRadius: BorderRadius.All(8f)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.Image)root[0];

            Assert.That(native.image, Is.SameAs(Texture2D.whiteTexture));
            Assert.That(native.scaleMode, Is.EqualTo(ScaleMode.ScaleAndCrop));
            Assert.That(native.style.width.value.value, Is.EqualTo(96f));
            Assert.That(native.style.height.value.value, Is.EqualTo(64f));
            Assert.That(native.tintColor, Is.EqualTo(Color.cyan));
            Assert.That(native.style.overflow.value, Is.EqualTo(Overflow.Hidden));
            Assert.That(native.style.borderTopLeftRadius.value.value, Is.EqualTo(8f));

            Assert.That(node.TryUpdate(new global::LumaFlow.Image(
                Texture2D.blackTexture,
                fit: ImageFit.Contain)), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(native.image, Is.SameAs(Texture2D.blackTexture));
            Assert.That(native.scaleMode, Is.EqualTo(ScaleMode.ScaleToFit));
            Assert.That(native.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(native.style.overflow.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(native.style.borderTopLeftRadius.keyword, Is.EqualTo(StyleKeyword.Null));
            node.Unmount();
        }

        [Test]
        public void CircleAvatar_ReconcilesMediaAndForegroundWithoutLosingState() {
            var firstChild = new CounterWidget();
            var secondChild = new CounterWidget();
            var root = new VisualElement();
            var node = (CircleAvatarNode)new CircleAvatar(
                firstChild,
                radius: 24f,
                backgroundColor: Color.gray).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var avatar = root[0];
            var foreground = avatar[0];
            var foregroundState = firstChild.MountedState;

            Assert.That(node.TryUpdate(new CircleAvatar(
                secondChild,
                new global::LumaFlow.Image(Texture2D.whiteTexture, fit: ImageFit.Cover),
                radius: 28f,
                semanticsLabel: "Alex")), Is.True);

            Assert.That(root[0], Is.SameAs(avatar));
            Assert.That(avatar[1], Is.SameAs(foreground));
            Assert.That(secondChild.MountedState, Is.SameAs(foregroundState));
            Assert.That(avatar.style.width.value.value, Is.EqualTo(56f));
            Assert.That(avatar.style.overflow.value, Is.EqualTo(Overflow.Hidden));
            Assert.That(avatar[0], Is.TypeOf<UnityEngine.UIElements.Image>());
            Assert.That(avatar[0].style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(avatar[0].style.right.value.value, Is.EqualTo(0f));
            Assert.That(avatar[0].style.bottom.value.value, Is.EqualTo(0f));
            node.Unmount();
        }

        [Test]
        public void ImageAndAvatar_RejectInvalidGeometryAndFit() {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new global::LumaFlow.Image(Texture2D.whiteTexture, width: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new global::LumaFlow.Image(Texture2D.whiteTexture, fit: (ImageFit)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircleAvatar(radius: 0f));
        }

        [Test]
        public void LumaIcons_ExposeTheCompleteCuratedTypedCatalog() {
            Assert.That(LumaIcons.All, Has.Count.EqualTo(96));
            var names = new HashSet<string>();
            var resourceIds = new HashSet<string>();
            foreach (var icon in LumaIcons.All) {
                Assert.That(names.Add(icon.Name), Is.True, $"Duplicate icon name: {icon.Name}");
                Assert.That(icon.ResourceId, Does.StartWith("LumaFlowIcons/"));
                Assert.That(resourceIds.Add(icon.ResourceId), Is.True, $"Duplicate icon resource: {icon.ResourceId}");
                Assert.That(Resources.Load<VectorImage>(icon.ResourceId), Is.Not.Null,
                    $"Missing or invalid vector asset: {icon.ResourceId}");
            }
            Assert.That(LumaIcons.Home.ResourceId, Is.EqualTo("LumaFlowIcons/house"));
            Assert.That(LumaIcons.LumaFlow.ResourceId, Is.EqualTo("LumaFlowIcons/lumaflow"));
            Assert.That(LumaIcons.MoreVertical.ResourceId, Is.EqualTo("LumaFlowIcons/ellipsis-vertical"));
            Assert.That(LumaIcons.Warning.ResourceId, Is.EqualTo("LumaFlowIcons/triangle-alert"));
        }

        [Test]
        public void IconContracts_RejectUndefinedDataAndInvalidSizes() {
            Assert.Throws<ArgumentException>(() => new Icon(default));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Icon(LumaIcons.Home, size: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IconButton(LumaIcons.Home, () => { }, hitSize: 0f));
        }

        [Test]
        public void Icon_AcceptsCustomIconDataWithoutUsingTheBuiltInCatalog() {
            var custom = new IconData("Custom settings", LumaIcons.Settings.ResourceId);
            var root = new VisualElement();

            using var mount = Framework.Mount(new Icon(custom), root);

            var image = (UnityEngine.UIElements.Image)root[0][0];
            Assert.That(image.vectorImage, Is.SameAs(Resources.Load<VectorImage>(custom.ResourceId)));
        }

        [Test]
        public void IconButton_UsesNativeButtonSemanticsAndCleansUpItsCallback() {
            var root = new VisualElement();
            var calls = 0;
            var node = (IconButtonNode)new IconButton(LumaIcons.Close, () => calls++, tooltip: "Close").CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var button = (UnityEngine.UIElements.Button)root[0];

            Assert.That(button[0], Is.TypeOf<UnityEngine.UIElements.Image>());
            Assert.That(button.tooltip, Is.EqualTo("Close"));
            Assert.That(button.style.width.value.value, Is.EqualTo(40f));
            node.HandleClicked();
            node.Unmount();
            node.HandleClicked();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void IconButton_DisabledDoesNotInvokeItsCallback() {
            var root = new VisualElement();
            var calls = 0;
            var node = (IconButtonNode)new IconButton(LumaIcons.Close, () => calls++, enabled: false).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            node.HandleClicked();

            Assert.That(root[0].enabledSelf, Is.False);
            Assert.That(calls, Is.Zero);
            node.Unmount();
        }

        [Test]
        public void IconButton_UsesThemedButtonStyleAndItsExplicitOverride() {
            var root = new VisualElement();
            var explicitStyle = new ButtonStyle(background: Color.cyan, foreground: Color.black, shape: BorderRadius.All(12f));
            using var mount = Framework.Mount(
                new Theme(
                    CreateTestTheme(),
                    new Row(new Widget[]
                    {
                    new IconButton(LumaIcons.Home, () => { }),
                    new IconButton(LumaIcons.Close, () => { }, style: explicitStyle)
                    })),
                root);
            var row = root[0][0];
            var themed = (UnityEngine.UIElements.Button)row[0];
            var explicitButton = (UnityEngine.UIElements.Button)row[1];

            Assert.That(themed.style.backgroundColor.value, Is.EqualTo(CreateTestTheme().ButtonTheme.Secondary.Background));
            Assert.That(explicitButton.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(explicitButton.style.borderTopLeftRadius.value.value, Is.EqualTo(12f));
        }

        [Test]
        public void Theme_ProvidesSemanticDefaultsWithoutAddingANativeWrapper() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            using var mount = Framework.Mount(
                new Theme(
                    theme,
                    new Column(new Widget[]
                    {
                    new Text("Themed body"),
                    new Button("Themed action", () => { })
                    })),
                root);
            var column = root[0][0];
            var label = (Label)column[0];
            var button = (UnityEngine.UIElements.Button)column[1];

            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(column.childCount, Is.EqualTo(2));
            Assert.That(label.style.color.value, Is.EqualTo(theme.Colors.OnSurfaceVariant));
            Assert.That(label.style.fontSize.value.value, Is.EqualTo(15f));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(theme.Colors.Primary));
            Assert.That(button.style.color.value, Is.EqualTo(theme.Colors.OnPrimary));
        }

        [Test]
        public void Theme_UpdateInvalidatesOnlyThemeDependentsAndPreservesNativeControls() {
            var firstTheme = CreateTestTheme();
            var secondTypography = new TypographyTheme(
                firstTheme.Typography.Title,
                firstTheme.Typography.Headline,
                new TextStyle(Color.green, 19f),
                firstTheme.Typography.Label);
            var secondTheme = new ThemeData(
                firstTheme.Colors,
                secondTypography,
                firstTheme.Spacing,
                firstTheme.Radius,
                new ButtonTheme(
                    new ButtonStyle(background: Color.yellow, foreground: Color.black),
                    firstTheme.ButtonTheme.Secondary),
                firstTheme.IconTheme,
                firstTheme.TextFieldTheme);
            var theme = new State<ThemeData>(firstTheme);
            var dependent = new ThemeDependentProbe();
            var independent = new ContextIndependentProbe();
            var button = new Button("Action", () => { });
            var content = new Row(new Widget[] { dependent, independent, button });
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<ThemeData>(theme, data => new Theme(data, content)),
                root);
            var row = root[0][0];
            var dependentLabel = row[0];
            var independentLabel = row[1];
            var nativeButton = row[2];

            theme.Value = secondTheme;

            Assert.That(row[0], Is.SameAs(dependentLabel));
            Assert.That(row[1], Is.SameAs(independentLabel));
            Assert.That(row[2], Is.SameAs(nativeButton));
            Assert.That(dependent.BuildCount, Is.EqualTo(2));
            Assert.That(independent.BuildCount, Is.EqualTo(1));
            Assert.That(((Label)dependentLabel).style.color.value, Is.EqualTo(Color.green));
            Assert.That(((Label)dependentLabel).style.fontSize.value.value, Is.EqualTo(19f));
            Assert.That(((UnityEngine.UIElements.Button)nativeButton).style.backgroundColor.value, Is.EqualTo(Color.yellow));
        }

        [Test]
        public void MediaQuery_UpdateInvalidatesOnlyMediaDependents() {
            var media = new State<MediaQueryData>(new MediaQueryData(320f, 640f));
            var dependent = new MediaQueryDependentProbe();
            var independent = new ContextIndependentProbe();
            var content = new Row(new Widget[] { dependent, independent });
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<MediaQueryData>(media, data => new MediaQuery(data, content)),
                root);
            var row = root[0][0];
            var dependentLabel = row[0];
            var independentLabel = row[1];

            media.Value = new MediaQueryData(768f, 640f);

            Assert.That(row[0], Is.SameAs(dependentLabel));
            Assert.That(row[1], Is.SameAs(independentLabel));
            Assert.That(((Label)dependentLabel).text, Is.EqualTo("768"));
            Assert.That(dependent.BuildCount, Is.EqualTo(2));
            Assert.That(independent.BuildCount, Is.EqualTo(1));
        }

        [Test]
        public void NestedTheme_ShieldsItsDependentsFromOuterThemeUpdates() {
            var outerTheme = new State<ThemeData>(CreateTestTheme());
            var innerTheme = CreateTestThemeWithBodyStyle(new TextStyle(Color.cyan, 17f));
            var replacementOuter = CreateTestThemeWithBodyStyle(new TextStyle(Color.green, 21f));
            var outerDependent = new ThemeDependentProbe();
            var innerDependent = new ThemeDependentProbe();
            var content = new Row(new Widget[]
            {
            outerDependent,
            new Theme(innerTheme, innerDependent)
            });
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<ThemeData>(outerTheme, data => new Theme(data, content)),
                root);
            var row = root[0][0];
            var innerLabel = row[1];

            outerTheme.Value = replacementOuter;

            Assert.That(outerDependent.BuildCount, Is.EqualTo(2));
            Assert.That(innerDependent.BuildCount, Is.EqualTo(1));
            Assert.That(row[1], Is.SameAs(innerLabel));
            Assert.That(((Label)row[0]).style.color.value, Is.EqualTo(Color.green));
            Assert.That(((Label)row[1]).style.color.value, Is.EqualTo(Color.cyan));
        }

        [Test]
        public void InheritedDependency_IsReleasedWhenItsConsumerUnmounts() {
            var theme = new State<ThemeData>(CreateTestTheme());
            var visible = new State<bool>(true);
            var dependent = new ThemeDependentProbe();
            var content = new ReactiveBuilder<bool>(visible, show =>
                show ? dependent : new Text("Removed", new TextStyle(Color.white)));
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<ThemeData>(theme, data => new Theme(data, content)),
                root);

            visible.Value = false;
            theme.Value = CreateTestThemeWithBodyStyle(new TextStyle(Color.green, 20f));

            Assert.That(dependent.BuildCount, Is.EqualTo(1));
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Removed"));
        }

        [Test]
        public void Theme_DoesNotOverrideExplicitTextOrButtonStyles() {
            var root = new VisualElement();
            var buttonStyle = new ButtonStyle(background: Color.cyan, foreground: Color.black);
            using var mount = Framework.Mount(
                new Theme(
                    CreateTestTheme(),
                    new Column(new Widget[]
                    {
                    new Text("Explicit", new TextStyle(Color.yellow, 18f)),
                    new Button("Explicit", () => { }, buttonStyle)
                    })),
                root);
            var column = root[0][0];
            var label = (Label)column[0];
            var button = (UnityEngine.UIElements.Button)column[1];

            Assert.That(label.style.color.value, Is.EqualTo(Color.yellow));
            Assert.That(label.style.fontSize.value.value, Is.EqualTo(18f));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(button.style.color.value, Is.EqualTo(Color.black));
        }

        [Test]
        public void Theme_IsTransparentToTheExpandedFlexContract() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(new Widget[]
                {
                new Theme(CreateTestTheme(), new Expanded(new Text("Fill")))
                }),
                root);
            var row = root[0][0];

            Assert.That(row.childCount, Is.EqualTo(1));
            Assert.That(row[0].style.flexGrow.value, Is.EqualTo(1f));
            Assert.That(((Label)row[0][0]).text, Is.EqualTo("Fill"));
        }

        [Test]
        public void StatelessWidget_UsesTheNearestThemeForSemanticTextAndButtonVariants() {
            var root = new VisualElement();
            var outerTheme = CreateTestTheme();
            var innerTypography = new TypographyTheme(
                new TextStyle(Color.cyan, 20f, FontStyle.Bold),
                new TextStyle(Color.yellow, 16f, FontStyle.Bold),
                new TextStyle(Color.green, 15f),
                new TextStyle(Color.magenta, 14f, FontStyle.Bold));
            var innerTheme = new ThemeData(
                new ColorScheme(Color.black, Color.gray, Color.white, Color.magenta, Color.blue, Color.yellow, Color.cyan, Color.green, Color.red),
                innerTypography,
                outerTheme.Spacing,
                outerTheme.Radius,
                new ButtonTheme(
                    outerTheme.ButtonTheme.Primary,
                    new ButtonStyle(background: Color.magenta, foreground: Color.yellow)));
            using var mount = Framework.Mount(
                new Theme(outerTheme, new Theme(innerTheme, new ThemedActionWidget())),
                root);
            var column = root[0][0];
            var label = (Label)column[0];
            var button = (UnityEngine.UIElements.Button)column[1];

            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(label.style.color.value, Is.EqualTo(Color.green));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.magenta));
            Assert.That(button.style.color.value, Is.EqualTo(Color.yellow));
        }

        [Test]
        public void Scaffold_ComposesThemedAppBarBodyAndNavigationBar() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            var selectedIndex = new State<int>(0);
            using var mount = Framework.Mount(
                new Theme(
                    theme,
                    new Scaffold(
                        body: new Text("Body"),
                        appBar: new AppBar("Luma", new Widget[] { new Icon(LumaIcons.MoreVertical) }),
                        navigationBar: new NavigationBar(
                            selectedIndex,
                            new[]
                            {
                            new NavigationDestination("Home", LumaIcons.Home),
                            new NavigationDestination("Library", LumaIcons.Library)
                            },
                            index => selectedIndex.Value = index))),
                root);
            var scaffold = root[0][0];

            Assert.That(scaffold.childCount, Is.EqualTo(3));
            Assert.That(scaffold.style.flexGrow.value, Is.EqualTo(1f));
            Assert.That(scaffold.style.flexShrink.value, Is.EqualTo(1f));
            Assert.That(scaffold.style.backgroundColor.value, Is.EqualTo(theme.Colors.Canvas));
            Assert.That(scaffold[0].style.height.value.value, Is.EqualTo(64f));
            Assert.That(scaffold[0].style.flexShrink.value, Is.Zero);
            Assert.That(scaffold[1].style.flexGrow.value, Is.EqualTo(1f));
            Assert.That(scaffold[2].style.height.value.value, Is.EqualTo(60f));
            Assert.That(scaffold[2].style.flexShrink.value, Is.Zero);
            Assert.That(((Label)scaffold[0][0]).text, Is.EqualTo("Luma"));
            Assert.That(((UnityEngine.UIElements.Image)scaffold[2][0][0][0]).tintColor, Is.EqualTo(theme.Colors.Primary));
        }

        [Test]
        public void Scaffold_CompatibleUpdatePreservesBodyWhenOptionalChromeChanges() {
            var selected = new State<int>(0);
            var destinations = new[] { new NavigationDestination("Home", LumaIcons.Home) };
            var firstBody = new CounterWidget();
            var root = new VisualElement();
            var node = (ScaffoldNode)new Scaffold(firstBody).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var scaffold = root[0];
            var bodyNative = scaffold[0];
            var bodyState = firstBody.MountedState;
            var secondBody = new CounterWidget();

            Assert.That(node.TryUpdate(new Scaffold(
                secondBody,
                new AppBar("Title"),
                new NavigationBar(selected, destinations, index => selected.Value = index))), Is.True);
            Assert.That(root[0], Is.SameAs(scaffold));
            Assert.That(scaffold.childCount, Is.EqualTo(3));
            Assert.That(scaffold[1], Is.SameAs(bodyNative));
            Assert.That(secondBody.MountedState, Is.SameAs(bodyState));
            Assert.That(((Label)scaffold[0][0]).text, Is.EqualTo("Title"));
            node.Unmount();
        }

        [Test]
        public void AppBar_CompatibleUpdatePreservesActionAndUpdatesTitle() {
            var firstAction = new CounterWidget();
            var root = new VisualElement();
            var node = (AppBarNode)new AppBar("Before", new Widget[] { firstAction }).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var appBar = root[0];
            var actionNative = appBar[2];
            var actionState = firstAction.MountedState;
            var secondAction = new CounterWidget();

            Assert.That(node.TryUpdate(new AppBar("After", new Widget[] { secondAction })), Is.True);
            Assert.That(root[0], Is.SameAs(appBar));
            Assert.That(((Label)appBar[0]).text, Is.EqualTo("After"));
            Assert.That(appBar[2], Is.SameAs(actionNative));
            Assert.That(secondAction.MountedState, Is.SameAs(actionState));
            node.Unmount();
        }

        [Test]
        public void NavigationBar_IsControlledAndUpdatesNativeSelectionFromState() {
            var root = new VisualElement();
            var selectedIndex = new State<int>(0);
            var callbackIndex = -1;
            var node = (NavigationBarNode)new NavigationBar(
                selectedIndex,
                new[]
                {
                new NavigationDestination("Home", LumaIcons.Home),
                new NavigationDestination("Library", LumaIcons.Library)
                },
                index => {
                    callbackIndex = index;
                    selectedIndex.Value = index;
                }).CreateNode();
            var context = new BuildContext(CreateTestTheme());
            node.Mount(parent: null, context, root);
            var bar = root[0];

            var tabBar = context.Semantics.Hierarchy.rootNodes[0];
            Assert.That(tabBar.role, Is.EqualTo(AccessibilityRole.TabBar));
            Assert.That(tabBar.children, Has.Count.EqualTo(2));
            Assert.That(tabBar.children[0].role, Is.EqualTo(AccessibilityRole.TabButton));
            Assert.That(tabBar.children[0].state.HasFlag(AccessibilityState.Selected), Is.True);

            node.HandleDestinationSelected(1);

            Assert.That(callbackIndex, Is.EqualTo(1));
            Assert.That(selectedIndex.Value, Is.EqualTo(1));
            Assert.That(((UnityEngine.UIElements.Image)bar[1][0][0]).tintColor, Is.EqualTo(CreateTestTheme().Colors.Primary));
            Assert.That(((Label)bar[0][0][1]).style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Normal));
            Assert.That(tabBar.children[0].state.HasFlag(AccessibilityState.Selected), Is.False);
            Assert.That(tabBar.children[1].state.HasFlag(AccessibilityState.Selected), Is.True);
            node.Unmount();
        }

        [Test]
        public void NavigationBar_RejectsInvalidControlledSelection() {
            var selectedIndex = new State<int>(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationBar(
                selectedIndex,
                new[] { new NavigationDestination("Home", LumaIcons.Home) },
                _ => { }));
        }

        [Test]
        public void NavigationBar_CompatibleUpdatePreservesButtonsAndSwitchesControlledState() {
            var firstSelection = new State<int>(0);
            var secondSelection = new State<int>(1);
            var firstCallback = -1;
            var secondCallback = -1;
            var root = new VisualElement();
            var node = (NavigationBarNode)new NavigationBar(
                firstSelection,
                new[]
                {
                new NavigationDestination("Home", LumaIcons.Home),
                new NavigationDestination("Library", LumaIcons.Library)
                },
                index => firstCallback = index).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var bar = root[0];
            var firstButton = bar[0];

            Assert.That(node.TryUpdate(new NavigationBar(
                secondSelection,
                new[]
                {
                new NavigationDestination("Dashboard", LumaIcons.Grid),
                new NavigationDestination("Files", LumaIcons.File)
                },
                index => secondCallback = index)), Is.True);
            Assert.That(bar[0], Is.SameAs(firstButton));
            Assert.That(((Label)bar[0][0][1]).text, Is.EqualTo("Dashboard"));
            node.HandleDestinationSelected(0);
            Assert.That(firstCallback, Is.EqualTo(-1));
            Assert.That(secondCallback, Is.EqualTo(0));
            firstSelection.Value = 1;
            Assert.That(((Label)bar[0][0][1]).style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Normal));
            secondSelection.Value = 0;
            Assert.That(((Label)bar[0][0][1]).style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
            node.Unmount();
        }

        [Test]
        public void NavigationRail_IsControlledAndUpdatesNativeSelectionFromState() {
            var root = new VisualElement();
            var selectedIndex = new State<int>(0);
            var callbackIndex = -1;
            var node = (NavigationRailNode)new NavigationRail(
                selectedIndex,
                new[]
                {
                new NavigationDestination("Home", LumaIcons.Home),
                new NavigationDestination("Library", LumaIcons.Library)
                },
                index => {
                    callbackIndex = index;
                    selectedIndex.Value = index;
                }).CreateNode();
            var context = new BuildContext(CreateTestTheme());
            node.Mount(parent: null, context, root);
            var rail = root[0];
            var tabBar = context.Semantics.Hierarchy.rootNodes[0];
            Assert.That(tabBar.role, Is.EqualTo(AccessibilityRole.TabBar));
            Assert.That(tabBar.children[0].label, Is.EqualTo("Home"));
            Assert.That(rail[0].style.justifyContent.value, Is.EqualTo(Justify.FlexStart));
            Assert.That(rail[0].style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexStart));

            node.HandleDestinationSelected(1);

            Assert.That(callbackIndex, Is.EqualTo(1));
            Assert.That(selectedIndex.Value, Is.EqualTo(1));
            Assert.That(rail.style.width.value.value, Is.EqualTo(208f));
            Assert.That(((UnityEngine.UIElements.Image)rail[1][0][0]).tintColor, Is.EqualTo(CreateTestTheme().Colors.Primary));
            Assert.That(((Label)rail[0][0][1]).style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Normal));
            Assert.That(tabBar.children[1].state.HasFlag(AccessibilityState.Selected), Is.True);

            node.SetMode(NavigationRailMode.Collapsed);

            Assert.That(rail.style.width.value.value, Is.EqualTo(72f));
            Assert.That(rail[0].style.justifyContent.value, Is.EqualTo(Justify.Center));
            Assert.That(rail[0].style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.Center));
            Assert.That(rail[0].tooltip, Is.EqualTo("Home"));
            Assert.That(rail[0][0][1].style.display.value, Is.EqualTo(DisplayStyle.None));
            node.Unmount();
        }

        [Test]
        public void AdaptiveScaffold_CompatibleUpdatePreservesBodyAndNativeChromeAcrossOptionalAppBar() {
            var selected = new State<int>(0);
            var destinations = new[] { new NavigationDestination("Home", LumaIcons.Home) };
            NavigationBar Bar() => new(selected, destinations, index => selected.Value = index);
            NavigationRail Rail() => new(selected, destinations, index => selected.Value = index);
            var firstBody = new CounterWidget();
            var root = new VisualElement();
            var node = (AdaptiveScaffoldNode)new AdaptiveScaffold(firstBody, Bar(), Rail()).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var scaffold = root[0];
            var content = scaffold[0];
            var rail = content[0];
            var bodyNative = content[1][0];
            var navigationBar = scaffold[1];
            var bodyState = firstBody.MountedState;
            var secondBody = new CounterWidget();

            Assert.That(node.TryUpdate(new AdaptiveScaffold(
                secondBody,
                Bar(),
                Rail(),
                appBar: new AppBar("Dashboard"))), Is.True);
            Assert.That(root[0], Is.SameAs(scaffold));
            Assert.That(scaffold[1], Is.SameAs(content));
            Assert.That(content[0], Is.SameAs(rail));
            Assert.That(content[1][0], Is.SameAs(bodyNative));
            Assert.That(scaffold[2], Is.SameAs(navigationBar));
            Assert.That(secondBody.MountedState, Is.SameAs(bodyState));
            node.Unmount();
        }

        [Test]
        public void CardAndListTile_UseThemedSurfaceAndComposeTrailingContent() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            using var mount = Framework.Mount(
                new Theme(
                    theme,
                    new Card(
                        new ListTile(
                            "Playback",
                            "Lossless audio",
                            trailing: new Button("Change", () => { })))),
                root);
            var card = root[0][0];
            var tile = card[0];

            Assert.That(card.style.backgroundColor.value, Is.EqualTo(theme.Colors.Surface));
            Assert.That(card.style.paddingLeft.value.value, Is.EqualTo(theme.Spacing.Medium));
            Assert.That(tile.style.flexDirection.value, Is.EqualTo(FlexDirection.Row));
            Assert.That(((Label)tile[0][0][0]).text, Is.EqualTo("Playback"));
            Assert.That(((Label)tile[0][0][1]).text, Is.EqualTo("Lossless audio"));
            Assert.That(((UnityEngine.UIElements.Button)tile[1]).text, Is.EqualTo("Change"));
        }

        [Test]
        public void ListTile_CompatibleUpdatePreservesTrailingStateAndUsesLatestInteraction() {
            var firstTrailing = new CounterWidget();
            var firstCalls = 0;
            var secondCalls = 0;
            var root = new VisualElement();
            var node = (ListTileNode)new ListTile(
                "Before",
                trailing: firstTrailing,
                onPressed: () => firstCalls++).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var tile = root[0];
            var textRoot = tile[0];
            var trailingNative = tile[1];
            var trailingState = firstTrailing.MountedState;
            var secondTrailing = new CounterWidget();

            Assert.That(node.TryUpdate(new ListTile(
                "After",
                "Supporting",
                secondTrailing,
                () => secondCalls++)), Is.True);
            Assert.That(root[0], Is.SameAs(tile));
            Assert.That(tile[0], Is.SameAs(textRoot));
            Assert.That(tile[1], Is.SameAs(trailingNative));
            Assert.That(secondTrailing.MountedState, Is.SameAs(trailingState));
            Assert.That(((Label)tile[0][0][0]).text, Is.EqualTo("After"));
            Assert.That(((Label)tile[0][0][1]).text, Is.EqualTo("Supporting"));
            using (var click = ClickEvent.GetPooled()) node.HandleClicked(click);
            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void ListTile_FlexibleSlotsAndLeadingChangesPreserveNestedState() {
            var firstTitle = new CounterWidget();
            var secondTitle = new CounterWidget();
            var root = new VisualElement();
            var node = (ListTileNode)new ListTile(firstTitle).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            var tile = root[0];
            var contentRoot = tile[0];
            var titleState = firstTitle.MountedState;

            Assert.That(node.TryUpdate(new ListTile(
                secondTitle,
                subtitle: new Text("Supporting"),
                leading: new Text("Leading"),
                trailing: new Text("Trailing"))), Is.True);

            Assert.That(root[0], Is.SameAs(tile));
            Assert.That(tile[1], Is.SameAs(contentRoot));
            Assert.That(secondTitle.MountedState, Is.SameAs(titleState));
            Assert.That(((Label)tile[0]).text, Is.EqualTo("Leading"));
            Assert.That(((Label)tile[2]).text, Is.EqualTo("Trailing"));

            Assert.That(node.TryUpdate(new ListTile(secondTitle)), Is.True);
            Assert.That(tile.childCount, Is.EqualTo(1));
            Assert.That(tile[0], Is.SameAs(contentRoot));
            Assert.That(secondTitle.MountedState, Is.SameAs(titleState));
            node.Unmount();
        }

        [Test]
        public void ListTile_StringConstructorRetainsLeadingAndTextStyles() {
            var titleStyle = new TextStyle(Color.red, 19f);
            var subtitleStyle = new TextStyle(Color.green, 13f);
            var leading = new Text("L");
            var tile = new ListTile(
                "Title",
                "Subtitle",
                leading: leading,
                titleStyle: titleStyle,
                subtitleStyle: subtitleStyle);

            Assert.That(tile.Leading, Is.SameAs(leading));
            Assert.That(tile.TitleStyle, Is.SameAs(titleStyle));
            Assert.That(tile.SubtitleStyle, Is.SameAs(subtitleStyle));
        }

        private static ThemeData CreateTestTheme() {
            var colors = new ColorScheme(
                canvas: Color.black,
                surface: Color.gray,
                surfaceVariant: Color.white,
                primary: Color.magenta,
                primaryContainer: Color.blue,
                onPrimary: Color.black,
                onSurface: Color.white,
                onSurfaceVariant: new Color(0.7f, 0.8f, 0.9f),
                outline: Color.gray);
            var typography = new TypographyTheme(
                title: new TextStyle(Color.white, 22f, FontStyle.Bold),
                headline: new TextStyle(Color.white, 18f, FontStyle.Bold),
                body: new TextStyle(colors.OnSurfaceVariant, 15f),
                label: new TextStyle(Color.white, 14f, FontStyle.Bold));
            var buttonTheme = new ButtonTheme(
                primary: new ButtonStyle(background: colors.Primary, foreground: colors.OnPrimary),
                secondary: new ButtonStyle(background: colors.SurfaceVariant, foreground: colors.OnSurface));
            return new ThemeData(
                colors,
                typography,
                new SpacingTheme(4f, 8f, 12f, 16f, 24f),
                new RadiusTheme(BorderRadius.All(4f), BorderRadius.All(8f), BorderRadius.All(16f)),
                buttonTheme);
        }

        private static ThemeData CreateTestThemeWithTextFieldStyle(TextFieldStyle style) {
            var theme = CreateTestTheme();
            return new ThemeData(
                theme.Colors,
                theme.Typography,
                theme.Spacing,
                theme.Radius,
                theme.ButtonTheme,
                theme.IconTheme,
                new TextFieldTheme(style));
        }

        private static ThemeData CreateTestThemeWithBodyStyle(TextStyle bodyStyle) {
            var theme = CreateTestTheme();
            return new ThemeData(
                theme.Colors,
                new TypographyTheme(
                    theme.Typography.Title,
                    theme.Typography.Headline,
                    bodyStyle,
                    theme.Typography.Label),
                theme.Spacing,
                theme.Radius,
                theme.ButtonTheme,
                theme.IconTheme,
                theme.TextFieldTheme);
        }

        [Test]
        public void Column_UsesNativeVerticalFlexLayoutAndMountsAllChildren() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Column(
                    new Widget[]
                    {
                    new Text("First"),
                    new Text("Second")
                    },
                    gap: 12f),
                root);
            var column = root[0][0];

            Assert.That(column.style.flexDirection.value, Is.EqualTo(FlexDirection.Column));
            Assert.That(column[0].style.marginBottom.value.value, Is.EqualTo(12f));
            Assert.That(column.childCount, Is.EqualTo(2));
            Assert.That(((Label)column[0]).text, Is.EqualTo("First"));
            Assert.That(((Label)column[1]).text, Is.EqualTo("Second"));
        }

        [Test]
        public void Column_UnmountsItsOwnedChildren() {
            var root = new VisualElement();
            var mount = Framework.Mount(
                new Column(
                    new Widget[]
                    {
                    new Text("First"),
                    new Text("Second")
                    }),
                root);

            mount.Dispose();

            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Row_UsesNativeHorizontalFlexLayoutAndMountsAllChildren() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(
                    new Widget[]
                    {
                    new Text("First"),
                    new Text("Second")
                    },
                    gap: 12f),
                root);
            var row = root[0][0];

            Assert.That(row.style.flexDirection.value, Is.EqualTo(FlexDirection.Row));
            Assert.That(row[0].style.marginRight.value.value, Is.EqualTo(12f));
            Assert.That(row.childCount, Is.EqualTo(2));
            Assert.That(((Label)row[0]).text, Is.EqualTo("First"));
            Assert.That(((Label)row[1]).text, Is.EqualTo("Second"));
        }

        [Test]
        public void Column_GapPreservesDirectExpandedChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Column(new Widget[] { new Expanded(new Text("Fill")), new Text("End") }, gap: 8f),
                root);
            var column = root[0][0];

            Assert.That(column.childCount, Is.EqualTo(2));
            Assert.That(column[0].style.flexGrow.value, Is.EqualTo(1f));
            Assert.That(column[0].style.marginBottom.value.value, Is.EqualTo(8f));
            Assert.That(((Label)column[0][0]).text, Is.EqualTo("Fill"));
        }

        [Test]
        public void Row_RejectsNonFiniteGap() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Row(new Widget[0], float.NaN));
        }

        [Test]
        public void ColumnAndKeyedFlex_RejectNonFiniteGaps() {
            var children = new State<IReadOnlyList<KeyedChild>>(Array.Empty<KeyedChild>());

            Assert.Throws<ArgumentOutOfRangeException>(() => new Column(new Widget[0], float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KeyedColumn(children, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KeyedRow(children, float.NegativeInfinity));
        }

        [Test]
        public void Row_MapsSemanticMainAndCrossAxisAlignment() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(
                    new Widget[] { new Text("Aligned") },
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    crossAxisAlignment: CrossAxisAlignment.End),
                root);
            var row = root[0][0];

            Assert.That(row.style.justifyContent.value, Is.EqualTo(Justify.SpaceBetween));
            Assert.That(row.style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexEnd));
        }

        [Test]
        public void Column_MapsSemanticMainAndCrossAxisAlignment() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Column(
                    new Widget[] { new Text("Aligned") },
                    mainAxisAlignment: MainAxisAlignment.Center,
                    crossAxisAlignment: CrossAxisAlignment.Start),
                root);
            var column = root[0][0];

            Assert.That(column.style.justifyContent.value, Is.EqualTo(Justify.Center));
            Assert.That(column.style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexStart));
        }

        [Test]
        public void Expanded_MapsFlexAndMountsItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Column(new Widget[] { new Expanded(new Text("Fill"), flex: 2) }),
                root);
            var expanded = root[0][0][0];

            Assert.That(expanded.style.flexGrow.value, Is.EqualTo(2f));
            Assert.That(((Label)expanded[0]).text, Is.EqualTo("Fill"));
        }

        [Test]
        public void Expanded_RejectsNonPositiveFlex() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Expanded(new Text("Fill"), flex: 0));
        }

        [Test]
        public void Expanded_RejectsContainerParentWithActionableDiagnostic() {
            var root = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new Container(new Expanded(new Text("Fill"))), root));

            Assert.That(exception!.Message, Does.Contain("Expanded must be an immediate child of Row or Column"));
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Expanded_RejectsRootPlacementWithActionableDiagnostic() {
            var root = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new Expanded(new Text("Fill")), root));

            Assert.That(exception!.Message, Does.Contain("Expanded must be an immediate child of Row or Column"));
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Flexible_RejectsPlacementOutsideAnImmediateFlexParent() {
            var root = new VisualElement();

            var exception = Assert.Throws<InvalidOperationException>(
                () => Framework.Mount(new Flexible(new Text("Fill")), root));

            Assert.That(exception!.Message, Does.Contain("Flexible must be an immediate child of Row or Column"));
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Center_UsesNativeCenterAlignmentAndMountsItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Center(new Text("Centered")), root);
            var center = root[0][0];

            Assert.That(center.style.justifyContent.value, Is.EqualTo(Justify.Center));
            Assert.That(center.style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.Center));
            Assert.That(((Label)center[0]).text, Is.EqualTo("Centered"));
        }

        [Test]
        public void Align_MapsTopRightAndMountsItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Align(new Text("Aligned"), Alignment.TopRight), root);
            var align = root[0][0];

            Assert.That(align.style.justifyContent.value, Is.EqualTo(Justify.FlexStart));
            Assert.That(align.style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexEnd));
            Assert.That(((Label)align[0]).text, Is.EqualTo("Aligned"));
        }

        [Test]
        public void Align_RejectsUnknownAlignment() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Align(new Text("Aligned"), (Alignment)99));
        }

        [Test]
        public void Align_NamedFactoriesMapToTypedAlignments() {
            var child = new Text("Aligned");

            Assert.That(Align.TopLeft(child).Alignment, Is.EqualTo(Alignment.TopLeft));
            Assert.That(Align.CenterRight(child).Alignment, Is.EqualTo(Alignment.CenterRight));
            Assert.That(Align.BottomCenter(child).Alignment, Is.EqualTo(Alignment.BottomCenter));
        }

        [Test]
        public void Spacer_MapsFlexToAnEmptyNativeElement() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Spacer(flex: 3), root);
            var spacer = root[0][0];

            Assert.That(spacer.style.flexGrow.value, Is.EqualTo(3f));
            Assert.That(spacer.childCount, Is.Zero);
        }

        [Test]
        public void Spacer_RejectsNonPositiveFlex() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Spacer(flex: 0));
        }

        [Test]
        public void Padding_AppliesTypedInsetsAndMountsItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Padding(
                    new Text("Padded"),
                    EdgeInsets.Only(left: 4f, top: 8f, right: 12f, bottom: 16f)),
                root);
            var padding = root[0][0];

            Assert.That(padding.style.paddingLeft.value.value, Is.EqualTo(4f));
            Assert.That(padding.style.paddingTop.value.value, Is.EqualTo(8f));
            Assert.That(padding.style.paddingRight.value.value, Is.EqualTo(12f));
            Assert.That(padding.style.paddingBottom.value.value, Is.EqualTo(16f));
            Assert.That(((Label)padding[0]).text, Is.EqualTo("Padded"));
        }

        [Test]
        public void Margin_AppliesTypedInsetsAroundItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Margin(
                    new Text("Margined"),
                    EdgeInsets.Only(left: 4f, top: 8f, right: 12f, bottom: 16f)),
                root);
            var margin = root[0][0];

            Assert.That(margin.style.marginLeft.value.value, Is.EqualTo(4f));
            Assert.That(margin.style.marginTop.value.value, Is.EqualTo(8f));
            Assert.That(margin.style.marginRight.value.value, Is.EqualTo(12f));
            Assert.That(margin.style.marginBottom.value.value, Is.EqualTo(16f));
            Assert.That(((Label)margin[0]).text, Is.EqualTo("Margined"));
        }

        [Test]
        public void Opacity_AppliesValueAndMountsItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Opacity(new Text("Faded"), 0.5f), root);
            var opacity = root[0][0];

            Assert.That(opacity.style.opacity.value, Is.EqualTo(0.5f));
            Assert.That(((Label)opacity[0]).text, Is.EqualTo("Faded"));
        }

        [Test]
        public void Opacity_RejectsValueOutsideItsSupportedRange() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Opacity(new Text("Faded"), 1.1f));
        }

        [Test]
        public void Stack_CreatesNativePositioningContextAndMountsAllChildren() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Stack(new Widget[] { new Text("Back"), new Text("Front") }),
                root);
            var stack = root[0][0];

            Assert.That(stack.style.position.value, Is.EqualTo(Position.Relative));
            Assert.That(stack.childCount, Is.EqualTo(2));
            Assert.That(((Label)stack[0]).text, Is.EqualTo("Back"));
            Assert.That(((Label)stack[1]).text, Is.EqualTo("Front"));
        }

        [Test]
        public void Stack_RejectsNullChildren() {
            Assert.Throws<ArgumentException>(() => new Stack(new Widget[] { null }));
        }

        [Test]
        public void Positioned_MapsExplicitNativeOffsetsAndSize() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Stack(
                    new Widget[]
                    {
                    new Positioned(new Text("Badge"), top: 8f, right: 12f, width: 48f, height: 20f)
                    }),
                root);
            var positioned = root[0][0][0];

            Assert.That(positioned.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(positioned.style.top.value.value, Is.EqualTo(8f));
            Assert.That(positioned.style.right.value.value, Is.EqualTo(12f));
            Assert.That(positioned.style.width.value.value, Is.EqualTo(48f));
            Assert.That(positioned.style.height.value.value, Is.EqualTo(20f));
            Assert.That(((Label)positioned[0]).text, Is.EqualTo("Badge"));
        }

        [Test]
        public void Positioned_RejectsNonFiniteValues() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Positioned(new Text("Badge"), left: float.PositiveInfinity));
        }

        [Test]
        public void ScrollView_MapsDirectionAndMountsChildIntoNativeContentContainer() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new ScrollView(new Text("Scrollable"), Axis.Horizontal), root);
            var scrollView = (UnityEngine.UIElements.ScrollView)root[0][0];

            Assert.That(scrollView.mode, Is.EqualTo(ScrollViewMode.Horizontal));
            Assert.That(scrollView.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(scrollView.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(scrollView.contentContainer.childCount, Is.EqualTo(1));
            Assert.That(((Label)scrollView.contentContainer[0]).text, Is.EqualTo("Scrollable"));
        }

        [Test]
        public void ScrollView_AppliesContentPaddingWithoutAddingAWidgetWrapper() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ScrollView(new Text("Scrollable"), padding: EdgeInsets.All(12f)),
                root);
            var scrollView = (UnityEngine.UIElements.ScrollView)root[0][0];

            Assert.That(scrollView.contentContainer.style.paddingLeft.value.value, Is.EqualTo(12f));
            Assert.That(scrollView.contentContainer.childCount, Is.EqualTo(1));
            Assert.That(scrollView.contentContainer[0], Is.TypeOf<Label>());
        }

        [Test]
        public void ScrollView_RejectsUnknownDirection() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ScrollView(new Text("Scrollable"), (Axis)99));
        }

        [Test]
        public void EdgeInsets_FactoriesCreateExpectedImmutableValues() {
            Assert.That(EdgeInsets.All(8f), Is.EqualTo(new EdgeInsets(8f, 8f, 8f, 8f)));
            Assert.That(EdgeInsets.Symmetric(horizontal: 12f, vertical: 4f),
                Is.EqualTo(new EdgeInsets(12f, 4f, 12f, 4f)));
            Assert.That(EdgeInsets.Zero, Is.EqualTo(new EdgeInsets(0f, 0f, 0f, 0f)));
        }

        [Test]
        public void SizedBox_AppliesExplicitDimensionsAndTightensItsChild() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new SizedBox(new Text("Fixed"), width: 320f, height: 44f), root);
            var box = root[0][0];

            Assert.That(box.style.width.value.value, Is.EqualTo(320f));
            Assert.That(box.style.height.value.value, Is.EqualTo(44f));
            Assert.That(((Label)box[0]).text, Is.EqualTo("Fixed"));
            Assert.That(box[0].style.width.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(box[0].style.width.value.value, Is.EqualTo(100f));
            Assert.That(box[0].style.height.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(box[0].style.height.value.value, Is.EqualTo(100f));
        }

        [Test]
        public void SizedBox_FactoriesExposeSquareFillAndShrinkContracts() {
            var child = new Text("Child");
            var square = SizedBox.Square(child, 32f);
            var shrink = SizedBox.Shrink(child);
            var expand = SizedBox.Expand(child);
            var root = new VisualElement();
            using var mount = Framework.Mount(expand, root);
            var box = root[0][0];

            Assert.That(square.Width, Is.EqualTo(32f));
            Assert.That(square.Height, Is.EqualTo(32f));
            Assert.That(shrink.Width, Is.Zero);
            Assert.That(shrink.Height, Is.Zero);
            Assert.That(expand.ExpandsWidth, Is.True);
            Assert.That(expand.ExpandsHeight, Is.True);
            Assert.That(box.style.width.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(box.style.height.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(box[0].style.width.value.value, Is.EqualTo(100f));
            Assert.That(box[0].style.height.value.value, Is.EqualTo(100f));
        }

        [Test]
        public void ConstrainedBox_AppliesOnlyExplicitNativeConstraints() {
            var root = new VisualElement();
            var constraints = new BoxConstraints(minWidth: 80f, maxWidth: 240f, minHeight: 24f);
            using var mount = Framework.Mount(new ConstrainedBox(new Text("Constrained"), constraints), root);
            var box = root[0][0];

            Assert.That(box.style.minWidth.value.value, Is.EqualTo(80f));
            Assert.That(box.style.maxWidth.value.value, Is.EqualTo(240f));
            Assert.That(box.style.minHeight.value.value, Is.EqualTo(24f));
            Assert.That(box.style.maxHeight.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(((Label)box[0]).text, Is.EqualTo("Constrained"));
        }

        [Test]
        public void ConstrainedBox_FactoriesCreateTightMinimumAndMaximumBounds() {
            var child = new Text("Bounded");
            var tight = ConstrainedBox.Tight(child, width: 120f, height: 40f);
            var atMost = ConstrainedBox.AtMost(child, width: 240f);
            var atLeast = ConstrainedBox.AtLeast(child, height: 24f);

            Assert.That(tight.Constraints, Is.EqualTo(new BoxConstraints(120f, 120f, 40f, 40f)));
            Assert.That(atMost.Constraints, Is.EqualTo(new BoxConstraints(maxWidth: 240f)));
            Assert.That(atLeast.Constraints, Is.EqualTo(new BoxConstraints(minHeight: 24f)));
        }

        [Test]
        public void BoxConstraints_RejectInvalidDimensionsAndInvertedRanges() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoxConstraints(minWidth: float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoxConstraints(maxHeight: -1f));
            Assert.Throws<ArgumentException>(() => new BoxConstraints(minWidth: 240f, maxWidth: 80f));
            Assert.Throws<ArgumentException>(() => new BoxConstraints(minHeight: 80f, maxHeight: 24f));
        }

        [Test]
        public void Flexible_MapsTightAndLooseFitsToNativeFlexSemantics() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(
                    new Widget[]
                    {
                    new Flexible(new Text("Tight"), flex: 2, fit: FlexFit.Tight),
                    new Flexible(new Text("Loose"), flex: 3, fit: FlexFit.Loose)
                    }),
                root);
            var row = root[0][0];

            Assert.That(row[0].style.flexGrow.value, Is.EqualTo(2f));
            Assert.That(row[0][0].style.width.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(row[1].style.flexGrow.value, Is.EqualTo(3f));
            Assert.That(row[1][0].style.width.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(((Label)row[0][0]).text, Is.EqualTo("Tight"));
            Assert.That(((Label)row[1][0]).text, Is.EqualTo("Loose"));
        }

        [Test]
        public void Flexible_RejectsInvalidFlexConfiguration() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Flexible(new Text("Child"), flex: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Flexible(new Text("Child"), fit: (FlexFit)99));
        }

        [Test]
        public void Dropdown_MapsTypedItemsControlledStateAndNativeConfiguration() {
            var root = new VisualElement();
            var selected = new State<int>(2);
            using var mount = Framework.Mount(
                new Dropdown<int>(
                    selected,
                    new[] { 1, 2, 3 },
                    item => $"Option {item}",
                    label: "Quality",
                    enabled: false),
                root);
            var dropdown = (PopupField<int>)root[0][0];

            Assert.That(dropdown.value, Is.EqualTo(2));
            Assert.That(dropdown.choices, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(dropdown.label, Is.EqualTo("Quality"));
            Assert.That(dropdown.enabledSelf, Is.False);
        }

        [Test]
        public void Dropdown_ResolvesThemeStyleOnTheNativeAnchorField() {
            var root = new VisualElement();
            var theme = CreateTestTheme();
            using var mount = Framework.Mount(
                new Theme(theme, new Dropdown<int>(
                    new State<int>(1),
                    new[] { 1, 2 },
                    item => item.ToString())),
                root);
            var dropdown = (PopupField<int>)root[0][0];
            var input = dropdown.Q<VisualElement>(className: "unity-base-field__input");

            Assert.That(input.style.backgroundColor.value, Is.EqualTo(theme.Colors.SurfaceVariant));
            Assert.That(input.style.color.value, Is.EqualTo(theme.Colors.OnSurface));
            Assert.That(input.style.borderTopColor.value, Is.EqualTo(theme.Colors.Outline));
        }

        [Test]
        public void Dropdown_StatePropertiesReceiveCombinedInteractionAndValidationStates() {
            var root = new VisualElement();
            var field = new FormField<string>(new State<string>("One"));
            field.ErrorText.Value = "Required";
            var resolvedStates = WidgetStates.None;
            var style = new DropdownStyle(
                background: Color.black,
                backgroundColor: WidgetStateProperty<Color?>.ResolveWith(states => {
                    resolvedStates = states;
                    return (states & (WidgetStates.Disabled | WidgetStates.Error | WidgetStates.Hovered))
                        == (WidgetStates.Disabled | WidgetStates.Error | WidgetStates.Hovered)
                        ? Color.yellow
                        : Color.cyan;
                }));
            var node = (DropdownNode<string>)new Dropdown<string>(
                field,
                new[] { "One", "Two" },
                item => item,
                enabled: false,
                style: style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var dropdown = (PopupField<string>)root[0];
            var input = dropdown.Q<VisualElement>(className: "unity-base-field__input");

            node.SetHovered(true);

            Assert.That(resolvedStates.HasFlag(WidgetStates.Disabled), Is.True);
            Assert.That(resolvedStates.HasFlag(WidgetStates.Error), Is.True);
            Assert.That(resolvedStates.HasFlag(WidgetStates.Hovered), Is.True);
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.yellow));
            node.Unmount();
        }

        [Test]
        public void Dropdown_PropagatesNativeChangesAndCleansUpStateSubscription() {
            var root = new VisualElement();
            var selected = new State<string>("Low");
            var node = (DropdownNode<string>)new Dropdown<string>(
                selected,
                new[] { "Low", "High" },
                item => item).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var dropdown = (PopupField<string>)root[0];

            node.HandleValueChanged("High");
            Assert.That(selected.Value, Is.EqualTo("High"));

            selected.Value = "Low";
            Assert.That(dropdown.value, Is.EqualTo("Low"));

            node.Unmount();
            selected.Value = "High";
            Assert.That(dropdown.value, Is.EqualTo("Low"));
        }

        [Test]
        public void Dropdown_RejectsEmptyItemsAndInitialValueOutsideItems() {
            Assert.Throws<ArgumentException>(() => new Dropdown<int>(new State<int>(1), new int[0], item => item.ToString()));
            Assert.Throws<ArgumentException>(() => new Dropdown<int>(new State<int>(3), new[] { 1, 2 }, item => item.ToString()));
        }

        [Test]
        public void Dropdown_CompatibleUpdatePreservesNativeElementAndReplacesChoicesAndBinding() {
            var first = new State<int>(1);
            var second = new State<int>(3);
            var version = new State<int>(0);
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<int>(version, value => value == 0
                    ? new Dropdown<int>(first, new[] { 1, 2 }, item => $"A{item}", "First",
                        style: new DropdownStyle(background: Color.cyan))
                    : new Dropdown<int>(second, new[] { 3, 4 }, item => $"B{item}", "Second",
                        style: new DropdownStyle(background: Color.yellow))),
                root);
            var native = (PopupField<int>)root[0][0];
            var input = native.Q<VisualElement>(className: "unity-base-field__input");

            version.Value = 1;

            Assert.That(root[0][0], Is.SameAs(native));
            Assert.That(native.label, Is.EqualTo("Second"));
            Assert.That(native.choices, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(native.value, Is.EqualTo(3));
            Assert.That(native.Q<VisualElement>(className: "unity-base-field__input"), Is.SameAs(input));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.yellow));
            first.Value = 2;
            Assert.That(native.value, Is.EqualTo(3));
            second.Value = 4;
            Assert.That(native.value, Is.EqualTo(4));
        }

        [Test]
        public void Checkbox_MapsControlledStateAndNativeConfiguration() {
            var root = new VisualElement();
            var isAccepted = new State<bool>(true);
            using var mount = Framework.Mount(new Checkbox(isAccepted, "Accept terms", enabled: false), root);
            var checkbox = (UnityEngine.UIElements.Toggle)root[0][0];

            Assert.That(checkbox.value, Is.True);
            Assert.That(checkbox.label, Is.EqualTo("Accept terms"));
            Assert.That(checkbox.ClassListContains("lumaflow-checkbox"), Is.True);
            Assert.That(checkbox.enabledSelf, Is.False);
        }

        [Test]
        public void Checkbox_PropagatesChangesAndStopsObservingWhenUnmounted() {
            var root = new VisualElement();
            var isAccepted = new State<bool>(false);
            var node = (CheckboxNode)new Checkbox(isAccepted).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var checkbox = (UnityEngine.UIElements.Toggle)root[0];

            node.HandleValueChanged(true);
            Assert.That(isAccepted.Value, Is.True);

            isAccepted.Value = false;
            Assert.That(checkbox.value, Is.False);

            node.Unmount();
            isAccepted.Value = true;
            Assert.That(checkbox.value, Is.False);
        }

        [Test]
        public void Checkbox_StateStyleResolvesSelectedAndDisabledTogether() {
            var observed = WidgetStates.None;
            var style = new CheckboxStyle(
                fillColorByState: WidgetStateProperty<Color?>.ResolveWith(states => {
                    observed = states;
                    return (states & WidgetStates.Selected) != 0 ? Color.green : Color.black;
                }),
                sizeByState: WidgetStateProperty<float?>.All(22f));
            var root = new VisualElement();
            using var mount = Framework.Mount(new Checkbox(new State<bool>(true), enabled: false, style: style), root);
            var checkbox = (UnityEngine.UIElements.Toggle)root[0][0];
            var input = checkbox.Q<VisualElement>(className: "unity-toggle__input")
                ?? checkbox.Q<VisualElement>(className: "unity-base-field__input");

            Assert.That(input, Is.Not.Null);
            Assert.That(observed, Is.EqualTo(WidgetStates.Selected | WidgetStates.Disabled));
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(input.style.width.value.value, Is.EqualTo(22f));
        }

        [Test]
        public void Checkbox_CompatibleUpdatePreservesNativeElementAndSwitchesStateBinding() {
            var first = new State<bool>(false);
            var second = new State<bool>(true);
            var version = new State<int>(0);
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<int>(version, value => new Checkbox(value == 0 ? first : second, value == 0 ? "First" : "Second")),
                root);
            var native = (UnityEngine.UIElements.Toggle)root[0][0];

            version.Value = 1;

            Assert.That(root[0][0], Is.SameAs(native));
            Assert.That(native.label, Is.EqualTo("Second"));
            Assert.That(native.value, Is.True);
            first.Value = true;
            Assert.That(native.value, Is.True);
            second.Value = false;
            Assert.That(native.value, Is.False);
        }

        [Test]
        public void Radio_MapsSharedSelectedStateToEachNativeOption() {
            var root = new VisualElement();
            var selected = new State<string>("Medium");
            using var mount = Framework.Mount(
                new Column(
                    new Widget[]
                    {
                    new Radio<string>(selected, "Low", "Low"),
                    new Radio<string>(selected, "Medium", "Medium", enabled: false)
                    }),
                root);
            var column = root[0][0];
            var low = (RadioButton)column[0];
            var medium = (RadioButton)column[1];

            Assert.That(low.value, Is.False);
            Assert.That(medium.value, Is.True);
            Assert.That(medium.enabledSelf, Is.False);
            Assert.That(medium.ClassListContains("lumaflow-radio"), Is.True);
        }

        [Test]
        public void Radio_SelectingOneOptionUpdatesEveryOptionAndCleansUpOnUnmount() {
            var root = new VisualElement();
            var selected = new State<int>(1);
            var first = (RadioNode<int>)new Radio<int>(selected, 1, "One").CreateNode();
            var second = (RadioNode<int>)new Radio<int>(selected, 2, "Two").CreateNode();
            first.Mount(parent: null, new BuildContext(), root);
            second.Mount(parent: null, new BuildContext(), root);
            var firstRadio = (RadioButton)root[0];
            var secondRadio = (RadioButton)root[1];

            second.HandleValueChanged(true);

            Assert.That(selected.Value, Is.EqualTo(2));
            Assert.That(firstRadio.value, Is.False);
            Assert.That(secondRadio.value, Is.True);

            second.Unmount();
            selected.Value = 1;
            Assert.That(secondRadio.value, Is.True);

            first.Unmount();
        }

        [Test]
        public void Radio_StateStyleResolvesSelectedState() {
            var style = new RadioStyle(
                fillColorByState: WidgetStateProperty<Color?>.ResolveWith(states =>
                    (states & WidgetStates.Selected) != 0 ? Color.cyan : Color.gray),
                borderColorByState: WidgetStateProperty<Color?>.ResolveWith(states =>
                    (states & WidgetStates.Focused) != 0 ? Color.yellow : null),
                sizeByState: WidgetStateProperty<float?>.All(24f));
            var root = new VisualElement();
            var node = (RadioNode<int>)new Radio<int>(new State<int>(2), 2, style: style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var radio = (RadioButton)root[0];
            var input = radio.Q<VisualElement>(className: "unity-radio-button__input");

            Assert.That(input, Is.Not.Null);
            Assert.That(input.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(input.style.width.value.value, Is.EqualTo(24f));
            node.Unmount();
        }

        [Test]
        public void ControlThemesSupplyDefaultsWhileExplicitFieldsRemainStronger() {
            var baseline = CreateTestTheme();
            var theme = new ThemeData(
                baseline.Colors,
                baseline.Typography,
                baseline.Spacing,
                baseline.Radius,
                baseline.ButtonTheme,
                checkboxTheme: new CheckboxTheme(new CheckboxStyle(
                    fillColor: Color.cyan,
                    inactiveColor: Color.black,
                    size: 26f)),
                radioTheme: new RadioTheme(new RadioStyle(
                    fillColor: Color.yellow,
                    inactiveColor: Color.gray,
                    size: 21f)),
                switchTheme: new SwitchTheme(new SwitchStyle(
                    trackColor: WidgetStateProperty<Color?>.All(Color.green),
                    width: WidgetStateProperty<float?>.All(50f),
                    height: WidgetStateProperty<float?>.All(30f))),
                sliderTheme: new SliderTheme(new SliderStyle(
                    inactiveTrackColor: WidgetStateProperty<Color?>.All(Color.red),
                    trackHeight: WidgetStateProperty<float?>.All(2f),
                    thumbSize: WidgetStateProperty<float?>.All(12f))));
            var context = new BuildContext(theme);
            var root = new VisualElement();
            var checkboxNode = (CheckboxNode)new Checkbox(
                new State<bool>(true),
                style: new CheckboxStyle(size: 18f)).CreateNode();
            var radioNode = (RadioNode<int>)new Radio<int>(new State<int>(1), 1).CreateNode();
            var switchNode = (SwitchNode)new Switch(new State<bool>(true)).CreateNode();
            var sliderNode = (SliderNode)new Slider(new State<float>(0.5f), 0f, 1f).CreateNode();
            checkboxNode.Mount(parent: null, context, root);
            radioNode.Mount(parent: null, context, root);
            switchNode.Mount(parent: null, context, root);
            sliderNode.Mount(parent: null, context, root);

            var checkboxInput = ((UnityEngine.UIElements.Toggle)root[0]).Q<VisualElement>(className: "unity-toggle__input")
                ?? ((UnityEngine.UIElements.Toggle)root[0]).Q<VisualElement>(className: "unity-base-field__input");
            var radioInput = ((RadioButton)root[1]).Q<VisualElement>(className: "unity-radio-button__input");
            var sliderTrack = ((UnityEngine.UIElements.Slider)root[3]).Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.trackerUssClassName);
            var sliderThumb = ((UnityEngine.UIElements.Slider)root[3]).Q<VisualElement>(
                className: UnityEngine.UIElements.Slider.draggerUssClassName);

            Assert.That(checkboxInput, Is.Not.Null);
            Assert.That(radioInput, Is.Not.Null);
            Assert.That(sliderTrack, Is.Not.Null);
            Assert.That(sliderThumb, Is.Not.Null);
            Assert.That(checkboxInput!.style.backgroundColor.value, Is.EqualTo(Color.cyan));
            Assert.That(checkboxInput.style.width.value.value, Is.EqualTo(18f));
            Assert.That(radioInput!.style.backgroundColor.value, Is.EqualTo(Color.yellow));
            Assert.That(radioInput.style.width.value.value, Is.EqualTo(21f));
            Assert.That(root[2].style.width.value.value, Is.EqualTo(50f));
            Assert.That(root[2][0].style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(sliderTrack!.style.backgroundColor.value, Is.EqualTo(Color.red));
            Assert.That(sliderTrack.style.height.value.value, Is.EqualTo(2f));
            Assert.That(sliderThumb!.style.width.value.value, Is.EqualTo(12f));

            sliderNode.Unmount();
            switchNode.Unmount();
            radioNode.Unmount();
            checkboxNode.Unmount();
        }

        [Test]
        public void ListView_UsesNativeVirtualizationAndPreservesItsTypedConfiguration() {
            var root = new VisualElement();
            var items = new[] { "First", "Second" };
            using var mount = Framework.Mount(
                new ListView<string>(items, item => new Text(item), itemHeight: 28f),
                root);
            var listView = (UnityEngine.UIElements.ListView)root[0][0];

            Assert.That(listView.itemsSource, Is.EqualTo(items));
            Assert.That(listView.makeItem, Is.Not.Null);
            Assert.That(listView.bindItem, Is.Not.Null);
            Assert.That(listView.unbindItem, Is.Not.Null);
            Assert.That(listView.destroyItem, Is.Not.Null);
            Assert.That(listView.fixedItemHeight, Is.EqualTo(28f));
        }

        [Test]
        public void ListView_CompatibleUpdatePreservesVirtualizedNativeElementAndReplacesItems() {
            var version = new State<int>(0);
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<int>(version, value => new ListView<int>(
                    value == 0 ? new[] { 1, 2 } : new[] { 3, 4, 5 },
                    item => new Text(item.ToString()),
                    itemHeight: value == 0 ? 24f : 32f)),
                root);
            var native = (UnityEngine.UIElements.ListView)root[0][0];

            version.Value = 1;

            Assert.That(root[0][0], Is.SameAs(native));
            Assert.That(native.itemsSource, Is.EqualTo(new[] { 3, 4, 5 }));
            Assert.That(native.fixedItemHeight, Is.EqualTo(32f));
        }

        [Test]
        public void ListView_ProvidesTheCurrentItemIndexToItsBuilder() {
            var root = new VisualElement();
            var node = (ListViewNode<string>)new ListView<string>(
                new[] { "First", "Second" },
                (item, index) => new Text($"{index}: {item}")).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var host = new VisualElement();

            node.BindItem(host, 1);

            Assert.That(((Label)host[0]).text, Is.EqualTo("1: Second"));
            node.Unmount();
        }

        [Test]
        public void ListView_RebindingARecycledHostUnmountsItsPreviousItemSubtree() {
            var root = new VisualElement();
            var firstText = new State<string>("First");
            var secondText = new State<string>("Second");
            var node = (ListViewNode<int>)new ListView<int>(
                new[] { 1, 2 },
                item => new Text(item == 1 ? firstText : secondText)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var host = new VisualElement();

            node.BindItem(host, 0);
            var firstLabel = (Label)host[0];
            firstText.Value = "Updated first";
            Assert.That(firstLabel.text, Is.EqualTo("Updated first"));

            node.BindItem(host, 1);
            var secondLabel = (Label)host[0];
            firstText.Value = "Stale";
            secondText.Value = "Updated second";

            Assert.That(firstLabel.parent, Is.Null);
            Assert.That(secondLabel.text, Is.EqualTo("Updated second"));

            node.Unmount();
            secondText.Value = "Stale after unmount";
            Assert.That(secondLabel.text, Is.EqualTo("Updated second"));
        }

        [Test]
        public void ListView_ReactiveSourceReplacementRefreshesNativeItemsAndUnbindsRows() {
            var root = new VisualElement();
            IReadOnlyList<int> initialItems = new[] { 1 };
            var items = new State<IReadOnlyList<int>>(initialItems);
            var itemText = new State<string>("Initial");
            var node = (ListViewNode<int>)new ListView<int>(items, _ => new Text(itemText)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var listView = (UnityEngine.UIElements.ListView)root[0];
            var host = new VisualElement();
            node.BindItem(host, 0);
            var oldLabel = (Label)host[0];

            items.Value = new[] { 2, 3 };
            itemText.Value = "Stale";

            Assert.That(listView.itemsSource, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(oldLabel.parent, Is.Null);
            Assert.That(oldLabel.text, Is.EqualTo("Initial"));

            node.Unmount();
        }

        [Test]
        public void ListView_ItemsUseParentThemeAndReactiveBindingsDoNotSurviveRecycle() {
            var root = new VisualElement();
            var firstExpanded = new State<bool>(false);
            var secondExpanded = new State<bool>(false);
            var theme = CreateTestTheme();
            var node = (ListViewNode<int>)new ListView<int>(
                new[] { 1, 2 },
                (item, _) => new ReactiveBuilder<bool>(
                    item == 1 ? firstExpanded : secondExpanded,
                    expanded => new Text(expanded ? $"Expanded {item}" : $"Compact {item}"))).CreateNode();
            node.Mount(parent: null, new BuildContext(theme), root);
            var host = new VisualElement();

            node.BindItem(host, 0);
            var firstLabel = (Label)host[0];
            Assert.That(firstLabel.style.color.value, Is.EqualTo(theme.Colors.OnSurfaceVariant));

            node.BindItem(host, 1);
            var secondLabel = (Label)host[0];
            firstExpanded.Value = true;
            secondExpanded.Value = true;

            Assert.That(firstLabel.parent, Is.Null);
            Assert.That(((Label)host[0]).text, Is.EqualTo("Expanded 2"));
            node.Unmount();
        }

        [Test]
        public void ListView_ControlledInputBindingDoesNotLeakAcrossRecycledHosts() {
            var root = new VisualElement();
            var first = new State<string>("First");
            var second = new State<string>("Second");
            var node = (ListViewNode<int>)new ListView<int>(
                new[] { 1, 2 },
                (item, _) => new TextField(item == 1 ? first : second)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var host = new VisualElement();

            node.BindItem(host, 0);
            node.BindItem(host, 1);
            var field = (UnityEngine.UIElements.TextField)host[0];
            first.Value = "Stale";
            second.Value = "Updated";

            Assert.That(field.value, Is.EqualTo("Updated"));
            node.Unmount();
        }

        [Test]
        public void ListView_LargeDataSourceDoesNotCreateOneWidgetPerItemOnMount() {
            var root = new VisualElement();
            var items = new List<int>();
            for (var index = 0; index < 10000; index++) items.Add(index);
            var buildCount = 0;
            var node = (ListViewNode<int>)new ListView<int>(
                items,
                (item, _) => {
                    buildCount++;
                    return new Text(item.ToString());
                }).CreateNode();

            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(buildCount, Is.LessThan(items.Count));
            node.Unmount();
        }

        [Test]
        public void ListView_KeyedRealizedRowsFollowItemsAcrossReorderWithoutLosingState() {
            var root = new VisualElement();
            IReadOnlyList<int> initial = new[] { 1, 2 };
            var items = new State<IReadOnlyList<int>>(initial);
            var latestWidgets = new Dictionary<int, CounterWidget>();
            var node = (ListViewNode<int>)new ListView<int>(
                items,
                item => {
                    var counter = new CounterWidget();
                    latestWidgets[item] = counter;
                    return counter;
                },
                itemKey: item => new WidgetKey(item.ToString())).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var firstHost = new VisualElement();
            var secondHost = new VisualElement();
            node.BindItem(firstHost, 0);
            node.BindItem(secondHost, 1);
            var firstState = latestWidgets[1].MountedState;
            var secondState = latestWidgets[2].MountedState;
            firstState.Increment();

            items.Value = new[] { 2, 1 };
            node.BindItem(firstHost, 0);
            node.BindItem(secondHost, 1);

            Assert.That(latestWidgets[1].MountedState, Is.SameAs(firstState));
            Assert.That(latestWidgets[2].MountedState, Is.SameAs(secondState));
            Assert.That(((Label)secondHost[0]).text, Is.EqualTo("1"));
            Assert.That(firstState.InitializationCount, Is.EqualTo(1));
            Assert.That(secondState.InitializationCount, Is.EqualTo(1));
            Assert.That(node.ActiveItemNodeCount, Is.EqualTo(2));
            node.Unmount();
            Assert.That(firstState.DisposeCount, Is.EqualTo(1));
            Assert.That(secondState.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void ListView_ControlledSelectedKeysSurviveMutationAndMapNativeSelection() {
            var root = new VisualElement();
            IReadOnlyList<int> initial = new[] { 1, 2, 3 };
            var items = new State<IReadOnlyList<int>>(initial);
            IReadOnlyList<WidgetKey> initialSelection = new[] { new WidgetKey("2") };
            var selection = new State<IReadOnlyList<WidgetKey>>(initialSelection);
            IReadOnlyList<WidgetKey> callbackValue = Array.Empty<WidgetKey>();
            var node = (ListViewNode<int>)new ListView<int>(
                items,
                item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString()),
                selectionMode: ListSelectionMode.Multiple,
                selectedKeys: selection,
                onSelectionChanged: value => callbackValue = value).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.ListView)root[0];

            Assert.That(native.selectedIndices, Is.EqualTo(new[] { 1 }));
            items.Value = new[] { 3, 1, 2 };
            Assert.That(selection.Value, Is.EqualTo(initialSelection));
            Assert.That(native.selectedIndices, Is.EqualTo(new[] { 2 }));

            node.HandleSelectedIndicesChanged(new[] { 0, 1 });

            Assert.That(selection.Value, Is.EqualTo(new[] { new WidgetKey("3"), new WidgetKey("1") }));
            Assert.That(callbackValue, Is.EqualTo(selection.Value));
            node.Unmount();
        }

        [Test]
        public void ListView_ControlledSelectionKeepsMissingKeysForFutureRestoration() {
            var root = new VisualElement();
            IReadOnlyList<int> initial = new[] { 1, 2 };
            var items = new State<IReadOnlyList<int>>(initial);
            IReadOnlyList<WidgetKey> selected = new[] { new WidgetKey("2") };
            var selection = new State<IReadOnlyList<WidgetKey>>(selected);
            var node = (ListViewNode<int>)new ListView<int>(
                items,
                item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString()),
                selectionMode: ListSelectionMode.Single,
                selectedKeys: selection).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.ListView)root[0];

            items.Value = new[] { 1 };
            Assert.That(selection.Value, Is.SameAs(selected));
            Assert.That(native.selectedIndices, Is.Empty);
            items.Value = new[] { 2, 1 };
            Assert.That(native.selectedIndices, Is.EqualTo(new[] { 0 }));
            node.Unmount();
        }

        [Test]
        public void ListView_CompatibleUpdateReleasesPreviousSelectionBinding() {
            var version = new State<int>(0);
            var firstSelection = new State<IReadOnlyList<WidgetKey>>(Array.Empty<WidgetKey>());
            var secondSelection = new State<IReadOnlyList<WidgetKey>>(Array.Empty<WidgetKey>());
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReactiveBuilder<int>(version, value => new ListView<int>(
                    new[] { 1, 2 },
                    item => new Text(item.ToString()),
                    itemKey: item => new WidgetKey(item.ToString()),
                    selectionMode: ListSelectionMode.Single,
                    selectedKeys: value == 0 ? firstSelection : secondSelection)),
                root);
            var native = (UnityEngine.UIElements.ListView)root[0][0];

            version.Value = 1;
            firstSelection.Value = new[] { new WidgetKey("1") };
            Assert.That(native.selectedIndices, Is.Empty);
            secondSelection.Value = new[] { new WidgetKey("2") };
            Assert.That(native.selectedIndices, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ListView_InvalidReactiveKeySnapshotDoesNotReplaceMountedSource() {
            var root = new VisualElement();
            IReadOnlyList<int> initial = new[] { 1, 2 };
            var items = new State<IReadOnlyList<int>>(initial);
            var node = (ListViewNode<int>)new ListView<int>(
                items,
                item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString())).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.ListView)root[0];

            Assert.Throws<ArgumentException>(() => items.Value = new[] { 1, 1 });
            Assert.That(native.itemsSource, Is.EqualTo(initial));
            node.Unmount();
        }

        [Test]
        public void ListViewController_RetainsOffsetAndDetachesDeterministically() {
            var controller = new ListViewController(initialOffset: 24f);
            var firstRoot = new VisualElement();
            var first = (ListViewNode<int>)new ListView<int>(
                new[] { 1, 2 },
                item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString()),
                controller: controller).CreateNode();
            first.Mount(parent: null, new BuildContext(), firstRoot);

            Assert.That(controller.Offset, Is.EqualTo(24f));
            var nativeScroll = ((UnityEngine.UIElements.ListView)firstRoot[0])
                .Q<UnityEngine.UIElements.ScrollView>();
            nativeScroll.verticalScroller.highValue = 200f;
            controller.JumpTo(48f);
            Assert.That(nativeScroll.scrollOffset.y, Is.EqualTo(48f).Within(0.01f));
            first.HandleNativeScrollOffset(64f);
            Assert.That(controller.Offset, Is.EqualTo(64f).Within(0.01f));
            Assert.That(controller.ScrollTo(new WidgetKey("2")), Is.True);
            Assert.That(controller.ScrollTo(new WidgetKey("missing")), Is.False);
            Assert.Throws<InvalidOperationException>(() => {
                var concurrent = new ListView<int>(new[] { 3 }, item => new Text(item.ToString()),
                    itemKey: item => new WidgetKey(item.ToString()), controller: controller).CreateNode();
                concurrent.Mount(parent: null, new BuildContext(), new VisualElement());
            });

            controller.JumpTo(72f);
            first.Unmount();
            Assert.That(controller.ScrollTo(new WidgetKey("2")), Is.False);
            Assert.That(controller.Offset, Is.EqualTo(72f));

            var second = (ListViewNode<int>)new ListView<int>(
                new[] { 2 },
                item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString()),
                controller: controller).CreateNode();
            second.Mount(parent: null, new BuildContext(), new VisualElement());
            Assert.That(controller.ScrollTo(new WidgetKey("2")), Is.True);
            second.Unmount();
        }

        [Test]
        public void ListView_RejectsInvalidKeyAndControlledSelectionContracts() {
            var selection = new State<IReadOnlyList<WidgetKey>>(Array.Empty<WidgetKey>());
            Assert.Throws<ArgumentException>(() => new ListView<int>(
                new[] { 1, 1 }, item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString())));
            Assert.Throws<ArgumentException>(() => new ListView<int>(
                new[] { 1 }, item => new Text(item.ToString()),
                selectionMode: ListSelectionMode.Single,
                selectedKeys: selection));
            Assert.Throws<ArgumentNullException>(() => new ListView<int>(
                new[] { 1 }, item => new Text(item.ToString()),
                itemKey: item => new WidgetKey(item.ToString()),
                selectionMode: ListSelectionMode.Single));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ListViewController(float.NaN));
        }

        [Test]
        public void ListView_RejectsInvalidItemHeight() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ListView<int>(new[] { 1 }, item => new Text(item.ToString()), itemHeight: 0f));
        }

        [Test]
        public void OverlayHost_ShowsEntriesAboveContentAndClosesThemIdempotently() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var host = root[0][0];

            var entry = controller.Show(new Text("Overlay"));

            Assert.That(((Label)host[0][0]).text, Is.EqualTo("Content"));
            Assert.That(((Label)host[1][0][0]).text, Is.EqualTo("Overlay"));
            Assert.That(entry.IsOpen, Is.True);
            entry.Close();
            entry.Close();
            Assert.That(host[1].childCount, Is.Zero);
            Assert.That(entry.IsOpen, Is.False);
        }

        [Test]
        public void OverlayHost_CloseDetachesOwnedHierarchyEvenWhenContentCleanupFails() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[0][0][1];
            var entry = controller.Show(
                new CleanupWidget(() => throw new InvalidOperationException("Expected overlay cleanup failure.")));

            var exception = Assert.Throws<InvalidOperationException>(entry.Close);

            Assert.That(exception!.Message, Does.Contain("Overlay closed"));
            Assert.That(entry.IsOpen, Is.False);
            Assert.That(overlayContainer.childCount, Is.Zero);
            Assert.That(controller.TryCloseTop(), Is.False);
            Assert.DoesNotThrow(entry.Close);
        }

        [Test]
        public void OverlayHost_CompatibleUpdatePreservesContentStateAndOpenEntries() {
            var controller = new OverlayController();
            var firstCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (OverlayHostNode)new OverlayHost(firstCounter, controller).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var host = root[0];
            var contentNative = host[0][0];
            var counterState = firstCounter.MountedState;
            var handle = controller.Show(new Text("Overlay"));
            var overlayNative = host[1][0];
            var secondCounter = new CounterWidget();

            Assert.That(node.TryUpdate(new OverlayHost(secondCounter, controller)), Is.True);
            Assert.That(root[0], Is.SameAs(host));
            Assert.That(host[0][0], Is.SameAs(contentNative));
            Assert.That(secondCounter.MountedState, Is.SameAs(counterState));
            Assert.That(host[1][0], Is.SameAs(overlayNative));
            Assert.That(handle.IsOpen, Is.True);
            node.Unmount();
            Assert.That(handle.IsOpen, Is.False);
        }

        [Test]
        public void Toast_CompatibleUpdatePreservesNativeRootAndChildState() {
            var firstCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (ToastNode)new Toast(firstCounter).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var toast = root[0];
            var counterNative = toast[0];
            var counterState = firstCounter.MountedState;
            var secondCounter = new CounterWidget();

            Assert.That(node.TryUpdate(new Toast(secondCounter)), Is.True);
            Assert.That(root[0], Is.SameAs(toast));
            Assert.That(toast[0], Is.SameAs(counterNative));
            Assert.That(secondCounter.MountedState, Is.SameAs(counterState));
            node.Unmount();
        }

        [Test]
        public void OverlayHost_TeardownClosesAllEntriesAndCleansTheirBindings() {
            var root = new VisualElement();
            var controller = new OverlayController();
            var text = new State<string>("Before");
            var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var entry = controller.Show(new Text(text));
            var label = (Label)root[0][0][1][0][0];

            mount.Dispose();
            text.Value = "After";

            Assert.That(entry.IsOpen, Is.False);
            Assert.That(label.text, Is.EqualTo("Before"));
            Assert.Throws<InvalidOperationException>(() => controller.Show(new Text("Closed")));
        }

        [Test]
        public void OverlayHost_ShowModalCreatesAndRemovesAPointerBlockingBarrier() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[0][0][1];

            var entry = controller.ShowModal(new Text("Modal"));

            Assert.That(overlayContainer.childCount, Is.EqualTo(2));
            Assert.That(overlayContainer[0].ClassListContains("lumaflow-modal-barrier"), Is.True);
            Assert.That(overlayContainer[0].pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(overlayContainer[1].style.justifyContent.value, Is.EqualTo(Justify.Center));
            Assert.That(overlayContainer[1].style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.Center));
            Assert.That(((Label)overlayContainer[1][0]).text, Is.EqualTo("Modal"));

            entry.Dispose();
            Assert.That(overlayContainer.childCount, Is.Zero);
        }

        [Test]
        public void OverlayHost_ShowModalAppliesTheConfiguredBarrierColor() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[0][0][1];
            var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            using var entry = controller.ShowModal(
                new Text("Modal"),
                new ModalOptions(barrierColor: color));

            Assert.That(overlayContainer[0].style.backgroundColor.value, Is.EqualTo(color));
        }

        [Test]
        public void OverlayHost_ShowPopoverMountsNonModalContentInAPositionedEntry() {
            var root = new VisualElement();
            var anchor = new VisualElement();
            root.Add(anchor);
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[1][0][1];

            var entry = controller.ShowPopover(anchor, new Text("Popover"), PopoverPlacement.BottomEnd);

            Assert.That(overlayContainer.childCount, Is.EqualTo(2));
            Assert.That(overlayContainer[0].pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(overlayContainer[1].pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(overlayContainer[1][0].ClassListContains("lumaflow-popover"), Is.True);
            Assert.That(((Label)overlayContainer[1][0][0]).text, Is.EqualTo("Popover"));
            Assert.That(entry.IsOpen, Is.True);
        }

        [Test]
        public void OverlayHost_ShowPopoverRejectsDetachedAnchor() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);

            Assert.Throws<InvalidOperationException>(() => controller.ShowPopover(new VisualElement(), new Text("Popover")));
        }

        [Test]
        public void ContextMenu_RejectsInvalidItems() {
            Assert.Throws<ArgumentException>(() => new ContextMenu(Array.Empty<ContextMenuItem>()));
            Assert.Throws<ArgumentException>(() => new ContextMenu(new ContextMenuItem[] { null }));
            Assert.Throws<ArgumentException>(() => new ContextMenuItem("", () => { }));
        }

        [Test]
        public void ContextMenu_CanBeAttachedAndDetachedIdempotently() {
            var menu = new ContextMenu(new[] { new ContextMenuItem("Copy", () => { }) });
            var anchor = new VisualElement();
            var handle = menu.AttachTo(anchor);

            Assert.DoesNotThrow(handle.Dispose);
            Assert.DoesNotThrow(handle.Dispose);
        }

        [Test]
        public void Tooltip_AppliesTextAndRestoresThePreviousTooltip() {
            var anchor = new VisualElement { tooltip = "Existing" };
            var tooltip = new Tooltip("Copy the selected value");

            using (tooltip.AttachTo(anchor)) {
                Assert.That(anchor.tooltip, Is.EqualTo("Copy the selected value"));
            }

            Assert.That(anchor.tooltip, Is.EqualTo("Existing"));
        }

        [Test]
        public void Tooltip_DoesNotOverwriteATooltipAppliedAfterIt() {
            var anchor = new VisualElement();
            var tooltip = new Tooltip("Original");
            var handle = tooltip.AttachTo(anchor);
            anchor.tooltip = "Replacement";

            handle.Dispose();

            Assert.That(anchor.tooltip, Is.EqualTo("Replacement"));
        }

        [Test]
        public void TooltipAnchor_MountsItsChildAndProvidesTheNativeTooltipFallback() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(
                new OverlayHost(
                    new TooltipAnchor(new Text("Copy"), new Tooltip("Copy value"), controller),
                    controller),
                root);

            var anchor = root[0][0][0][0];

            Assert.That(anchor.tooltip, Is.EqualTo("Copy value"));
            Assert.That(((Label)anchor[0]).text, Is.EqualTo("Copy"));
            Assert.That(controller.HasOpenEntries, Is.False);
        }

        [Test]
        public void TabView_RebuildsItsContentWhenTheSelectedValueChanges() {
            var root = new VisualElement();
            var selected = new State<string>("Overview");
            using var mount = Framework.Mount(
                new TabView<string>(selected, value => new Text(value)),
                root);

            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Overview"));
            selected.Value = "Activity";
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Activity"));
        }

        [Test]
        public void TabBar_UserSelectionCommitsAndPreservesObserverAndCallbackFailures() {
            var root = new VisualElement();
            var selected = new State<string>("Overview");
            var observerFailure = new InvalidOperationException("observer");
            var callbackFailure = new ArgumentException("callback");
            var node = (TabBarNode<string>)new TabBar<string>(
                selected,
                new[] { new TabItem<string>("Overview", "Overview"), new TabItem<string>("Activity", "Activity") },
                _ => throw callbackFailure).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            using var subscription = selected.Subscribe(_ => throw observerFailure);

            var failure = Assert.Throws<AggregateException>(() => node.HandleSelection("Activity"));

            Assert.That(selected.Value, Is.EqualTo("Activity"));
            Assert.That(failure!.InnerExceptions, Is.EquivalentTo(new Exception[] { observerFailure, callbackFailure }));
            node.Unmount();
        }

        [Test]
        public void TabBar_SelectionOnlyRebuildsThePreviousAndNextItems() {
            var root = new VisualElement();
            var selected = new State<int>(0);
            var probes = new[] {
                new ThemeDependentProbe(),
                new ThemeDependentProbe(),
                new ThemeDependentProbe(),
                new ThemeDependentProbe()
            };
            var items = new TabItem<int>[probes.Length];
            for (var index = 0; index < probes.Length; index++) {
                items[index] = new TabItem<int>(index, probes[index], $"Tab {index}");
            }
            using var mount = Framework.Mount(
                new Theme(CreateTestTheme(), new TabBar<int>(selected, items)),
                root);

            Assert.That(probes, Has.All.Matches<ThemeDependentProbe>(probe => probe.BuildCount == 1));

            selected.Value = 1;

            Assert.That(probes[0].BuildCount, Is.EqualTo(2));
            Assert.That(probes[1].BuildCount, Is.EqualTo(2));
            Assert.That(probes[2].BuildCount, Is.EqualTo(1));
            Assert.That(probes[3].BuildCount, Is.EqualTo(1));
        }

        [Test]
        public void SegmentedControl_UserSelectionCommitsAndPreservesObserverAndCallbackFailures() {
            var root = new VisualElement();
            var selected = new State<string>("Day");
            var observerFailure = new InvalidOperationException("observer");
            var callbackFailure = new ArgumentException("callback");
            var node = (SegmentedControlNode<string>)new SegmentedControl<string>(
                selected,
                new[] { new SegmentedControlItem<string>("Day", "Day"), new SegmentedControlItem<string>("Week", "Week") },
                _ => throw callbackFailure).CreateNode();
            node.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            using var subscription = selected.Subscribe(_ => throw observerFailure);

            var failure = Assert.Throws<AggregateException>(() => node.HandleSelection("Week"));

            Assert.That(selected.Value, Is.EqualTo("Week"));
            Assert.That(failure!.InnerExceptions, Is.EquivalentTo(new Exception[] { observerFailure, callbackFailure }));
            node.Unmount();
        }

        [Test]
        public void SegmentedControl_SelectionOnlyRebuildsThePreviousAndNextItems() {
            var root = new VisualElement();
            var selected = new State<int>(0);
            var probes = new[] {
                new ThemeDependentProbe(),
                new ThemeDependentProbe(),
                new ThemeDependentProbe(),
                new ThemeDependentProbe()
            };
            var items = new SegmentedControlItem<int>[probes.Length];
            for (var index = 0; index < probes.Length; index++) {
                items[index] = new SegmentedControlItem<int>(index, probes[index], $"Segment {index}");
            }
            using var mount = Framework.Mount(
                new Theme(CreateTestTheme(), new SegmentedControl<int>(selected, items)),
                root);

            Assert.That(probes, Has.All.Matches<ThemeDependentProbe>(probe => probe.BuildCount == 1));

            selected.Value = 1;

            Assert.That(probes[0].BuildCount, Is.EqualTo(2));
            Assert.That(probes[1].BuildCount, Is.EqualTo(2));
            Assert.That(probes[2].BuildCount, Is.EqualTo(1));
            Assert.That(probes[3].BuildCount, Is.EqualTo(1));
        }

        [Test]
        public void TabsAndSegments_RejectDuplicateControlledValues() {
            Assert.Throws<ArgumentException>(() => new TabBar<string>(
                new State<string>("same"),
                new[] { new TabItem<string>("same", "One"), new TabItem<string>("same", "Two") }));
            Assert.Throws<ArgumentException>(() => new SegmentedControl<string>(
                new State<string>("same"),
                new[] { new SegmentedControlItem<string>("same", "One"), new SegmentedControlItem<string>("same", "Two") }));
        }

        [Test]
        public void TabsAndSegments_MountArbitraryItemChildren() {
            var root = new VisualElement();
            var tabs = (TabBarNode<string>)new TabBar<string>(
                new State<string>("one"),
                new[] {
                    new TabItem<string>(
                        "one",
                        new Row(new Widget[] { new Icon(LumaIcons.Home), new Text("Home") }),
                        "Home")
                }).CreateNode();
            var segments = (SegmentedControlNode<string>)new SegmentedControl<string>(
                new State<string>("day"),
                new[] {
                    new SegmentedControlItem<string>(
                        "day",
                        new Row(new Widget[] { new Icon(LumaIcons.Calendar), new Text("Day") }),
                        "Day")
                }).CreateNode();

            tabs.Mount(parent: null, new BuildContext(CreateTestTheme()), root);
            segments.Mount(parent: null, new BuildContext(CreateTestTheme()), root);

            Assert.That(root[0][0][0].childCount, Is.EqualTo(2));
            Assert.That(root[1][0][0].childCount, Is.EqualTo(2));

            segments.Unmount();
            tabs.Unmount();
        }

        [Test]
        public void TabAndSegmentThemesSupplyDefaultsWhileExplicitFieldsRemainStronger() {
            var baseline = CreateTestTheme();
            var theme = new ThemeData(
                baseline.Colors,
                baseline.Typography,
                baseline.Spacing,
                baseline.Radius,
                baseline.ButtonTheme,
                tabBarTheme: new TabBarTheme(new TabBarStyle(dividerColor: Color.cyan)),
                segmentedControlTheme: new SegmentedControlTheme(new SegmentedControlStyle(background: Color.red)));
            var root = new VisualElement();
            var tabs = (TabBarNode<string>)new TabBar<string>(
                new State<string>("one"),
                new[] { new TabItem<string>("one", "One") },
                style: new TabBarStyle(dividerColor: Color.yellow)).CreateNode();
            var segments = (SegmentedControlNode<string>)new SegmentedControl<string>(
                new State<string>("one"),
                new[] { new SegmentedControlItem<string>("one", "One") },
                style: new SegmentedControlStyle(background: Color.green)).CreateNode();
            var themedTabs = (TabBarNode<string>)new TabBar<string>(
                new State<string>("one"),
                new[] { new TabItem<string>("one", "One") }).CreateNode();
            var themedSegments = (SegmentedControlNode<string>)new SegmentedControl<string>(
                new State<string>("one"),
                new[] { new SegmentedControlItem<string>("one", "One") }).CreateNode();

            tabs.Mount(parent: null, new BuildContext(theme), root);
            segments.Mount(parent: null, new BuildContext(theme), root);
            themedTabs.Mount(parent: null, new BuildContext(theme), root);
            themedSegments.Mount(parent: null, new BuildContext(theme), root);

            Assert.That(root[0].style.borderBottomColor.value, Is.EqualTo(Color.yellow));
            Assert.That(root[1].style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(root[2].style.borderBottomColor.value, Is.EqualTo(Color.cyan));
            Assert.That(root[3].style.backgroundColor.value, Is.EqualTo(Color.red));

            themedSegments.Unmount();
            themedTabs.Unmount();
            segments.Unmount();
            tabs.Unmount();
        }

        [Test]
        public void ThemeData_CopyWithReplacesSelectedValuesAndRetainsTheRest() {
            var original = CreateTestTheme();
            var tabs = new TabBarTheme(new TabBarStyle(indicatorColor: Color.magenta));
            var copy = original.CopyWith(tabBarTheme: tabs);

            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.TabBarTheme, Is.SameAs(tabs));
            Assert.That(copy.Colors, Is.SameAs(original.Colors));
            Assert.That(copy.Typography, Is.SameAs(original.Typography));
            Assert.That(copy.ButtonTheme, Is.SameAs(original.ButtonTheme));
            Assert.That(copy.SegmentedControlTheme, Is.SameAs(original.SegmentedControlTheme));
        }

        [Test]
        public void OverlayHost_ShowToastPlacesSemanticToastAtTheBottomAndCanCloseIt() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[0][0][1];

            var entry = controller.ShowToast(new Toast(new Text("Saved")), TimeSpan.FromSeconds(1));
            var host = overlayContainer[0];

            Assert.That(host.ClassListContains("lumaflow-toast-host"), Is.True);
            Assert.That(host.style.justifyContent.value, Is.EqualTo(Justify.FlexEnd));
            Assert.That(host.style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.Center));
            Assert.That(host.style.bottom.value.value, Is.EqualTo(24f));
            Assert.That(host[0].ClassListContains("lumaflow-toast"), Is.True);
            Assert.That(((Label)host[0][0]).text, Is.EqualTo("Saved"));

            var secondEntry = controller.ShowToast(new Toast(new Text("Synced")), TimeSpan.FromSeconds(1));
            Assert.That(overlayContainer[1].style.bottom.value.value, Is.EqualTo(100f));

            entry.Close();
            Assert.That(overlayContainer.childCount, Is.EqualTo(1));
            Assert.That(overlayContainer[0].style.bottom.value.value, Is.EqualTo(24f));
            secondEntry.Close();
            Assert.That(overlayContainer.childCount, Is.Zero);
        }

        [Test]
        public void OverlayController_ShowToastRejectsNonPositiveDuration() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);

            Assert.Throws<ArgumentOutOfRangeException>(() => controller.ShowToast(new Text("Saved"), TimeSpan.Zero));
        }

        [Test]
        public void AnimatedOpacity_UsesItsStateValueForTheInitialNativeOpacity() {
            var root = new VisualElement();
            var opacity = new State<float>(0.4f);
            using var mount = Framework.Mount(new AnimatedOpacity(new Text("Fading"), opacity, TimeSpan.FromMilliseconds(200)), root);

            Assert.That(root[0][0].style.opacity.value, Is.EqualTo(0.4f));
            Assert.That(((Label)root[0][0][0]).text, Is.EqualTo("Fading"));
        }

        [Test]
        public void AnimatedOpacity_CompatibleUpdatePreservesChildStateAndSwitchesValueBinding() {
            var firstOpacity = new State<float>(0.4f);
            var secondOpacity = new State<float>(0.4f);
            var firstCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (AnimatedOpacityNode)new AnimatedOpacity(
                firstCounter,
                firstOpacity,
                TimeSpan.FromMilliseconds(200)).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var wrapper = root[0];
            var childNative = wrapper[0];
            var childState = firstCounter.MountedState;
            var secondCounter = new CounterWidget();

            Assert.That(node.TryUpdate(new AnimatedOpacity(
                secondCounter,
                secondOpacity,
                TimeSpan.FromMilliseconds(400))), Is.True);
            Assert.That(root[0], Is.SameAs(wrapper));
            Assert.That(wrapper[0], Is.SameAs(childNative));
            Assert.That(secondCounter.MountedState, Is.SameAs(childState));
            firstOpacity.Value = 0.8f;
            Assert.That(wrapper.style.opacity.value, Is.EqualTo(0.4f));
            node.Unmount();
        }

        [Test]
        public void AsyncActionScope_CompatibleUpdatePreservesChildState() {
            var action = new AsyncAction(() => Task.CompletedTask);
            var firstCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (AsyncActionScopeNode)new AsyncActionScope(action, firstCounter).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];
            var state = firstCounter.MountedState;
            var secondCounter = new CounterWidget();

            Assert.That(node.TryUpdate(new AsyncActionScope(action, secondCounter)), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(secondCounter.MountedState, Is.SameAs(state));
            node.Unmount();
        }

        [Test]
        public void AnimatedOpacity_RejectsInvalidStateValueAndDuration() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(new Text("Fading"), new State<float>(-0.1f), TimeSpan.FromMilliseconds(200)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(new Text("Fading"), new State<float>(1f), TimeSpan.Zero));
        }

        [Test]
        public void Dialog_ComposesOptionalTitleContentAndActions() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Dialog(new Text("Body"), new Text("Title"), new Widget[] { new Button("Cancel", () => { }) }), root);
            var dialog = root[0][0];

            Assert.That(dialog.ClassListContains("lumaflow-dialog"), Is.True);
            Assert.That(((Label)dialog[0]).text, Is.EqualTo("Title"));
            Assert.That(((Label)dialog[1]).text, Is.EqualTo("Body"));
            Assert.That(dialog[2].ClassListContains("lumaflow-dialog__actions"), Is.True);
            Assert.That(dialog[2][0], Is.TypeOf<UnityEngine.UIElements.Button>());
        }

        [Test]
        public void Dialog_CompatibleUpdatePreservesContentAndKeyedActionsAcrossStructuralChanges() {
            var firstContent = new CounterWidget();
            var firstAction = new CounterWidget();
            var secondAction = new CounterWidget();
            var root = new VisualElement();
            var node = (DialogNode)new Dialog(
                firstContent,
                actions: new Widget[]
                {
                firstAction.WithKey(new WidgetKey("first")),
                secondAction.WithKey(new WidgetKey("second"))
                }).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var dialog = root[0];
            var contentNative = dialog[0];
            var actions = dialog[1];
            var firstActionNative = actions[0];
            var secondActionNative = actions[1];
            var contentState = firstContent.MountedState;
            var firstActionState = firstAction.MountedState;
            var secondActionState = secondAction.MountedState;
            var nextContent = new CounterWidget();
            var nextFirstAction = new CounterWidget();
            var nextSecondAction = new CounterWidget();

            Assert.That(node.TryUpdate(new Dialog(
                nextContent,
                new Text("Title"),
                new Widget[]
                {
                nextSecondAction.WithKey(new WidgetKey("second")),
                nextFirstAction.WithKey(new WidgetKey("first"))
                })), Is.True);
            Assert.That(root[0], Is.SameAs(dialog));
            Assert.That(dialog[1], Is.SameAs(contentNative));
            Assert.That(dialog[2], Is.SameAs(actions));
            Assert.That(actions[0], Is.SameAs(secondActionNative));
            Assert.That(actions[1], Is.SameAs(firstActionNative));
            Assert.That(nextContent.MountedState, Is.SameAs(contentState));
            Assert.That(nextFirstAction.MountedState, Is.SameAs(firstActionState));
            Assert.That(nextSecondAction.MountedState, Is.SameAs(secondActionState));
            node.Unmount();
        }

        [Test]
        public void OverlayHost_ModalStackDisablesUnderlyingContentAndRestoresOwnershipInOrder() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var host = root[0][0];
            var passive = controller.Show(new Text("Passive"));
            var modal = controller.ShowModal(new Text("Modal"));

            Assert.That(host[0].enabledSelf, Is.False);
            Assert.That(host[1][0].enabledSelf, Is.False);
            Assert.That(host[1][2].enabledSelf, Is.True);

            modal.Close();
            Assert.That(host[0].enabledSelf, Is.True);
            Assert.That(host[1][0].enabledSelf, Is.True);
            passive.Close();
            Assert.That(host[1].childCount, Is.Zero);
        }

        [Test]
        public void OverlayHost_FailedModalMountRollsBackBarrierAndInputOwnership() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var host = root[0][0];

            Assert.Throws<InvalidOperationException>(() => controller.ShowModal(new ThrowingWidget()));

            Assert.That(controller.HasOpenEntries, Is.False);
            Assert.That(host[0].enabledSelf, Is.True);
            Assert.That(host[1].childCount, Is.Zero);
            using var recovered = controller.ShowModal(new Text("Recovered"));
            Assert.That(controller.HasOpenEntries, Is.True);
        }

        [Test]
        public void BackNavigation_NonDismissibleModalConsumesBackWithoutPoppingRoute() {
            var root = new VisualElement();
            var overlay = new OverlayController();
            var navigator = new Navigator(new Text("Home"));
            var node = (BackNavigationNode)new BackNavigation(
                new OverlayHost(new NavigatorHost(navigator), overlay),
                navigator,
                overlay).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            navigator.Push(new Text("Details"));
            var modal = overlay.ShowModal(
                new Text("Required"),
                new ModalOptions(dismissOnBarrier: false, dismissOnBack: false));

            Assert.That(node.TryHandleBack(), Is.True);
            Assert.That(modal.IsOpen, Is.True);
            Assert.That(navigator.Depth, Is.EqualTo(2));

            modal.Close();
            Assert.That(node.TryHandleBack(), Is.True);
            Assert.That(navigator.Depth, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void OverlayHost_DrawerUsesModalBarrierAndRequestedEdge() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);
            var overlayContainer = root[0][0][1];

            var drawer = controller.ShowDrawer(new Text("Drawer"), DrawerPlacement.Right);

            Assert.That(overlayContainer[0].ClassListContains("lumaflow-modal-barrier"), Is.True);
            Assert.That(overlayContainer[1].ClassListContains("lumaflow-drawer-host"), Is.True);
            Assert.That(overlayContainer[1].style.alignItems.value, Is.EqualTo(UnityEngine.UIElements.Align.FlexEnd));
            Assert.That(((Label)overlayContainer[1][0]).text, Is.EqualTo("Drawer"));
            drawer.Close();
        }

        [Test]
        public void Dialog_RemovedActionCleanupFailuresStillCommitTheNewActionHierarchy() {
            var root = new VisualElement();
            var node = (DialogNode)new Dialog(
                new Text("Body"),
                actions: new Widget[]
                {
                new CleanupWidget(() => throw new InvalidOperationException("First action cleanup failure.")),
                new CleanupWidget(() => throw new ArgumentException("Second action cleanup failure."))
                }).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var dialog = root[0];

            var exception = Assert.Throws<InvalidOperationException>(
                () => node.TryUpdate(new Dialog(new Text("Updated"))));

            Assert.That(dialog.childCount, Is.EqualTo(1));
            Assert.That(((Label)dialog[0]).text, Is.EqualTo("Updated"));
            Assert.That(exception!.InnerException, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)exception.InnerException!).InnerExceptions, Has.Count.EqualTo(2));

            Assert.That(node.TryUpdate(new Dialog(
                new Text("Recovered"),
                actions: new Widget[] { new Text("Action") })), Is.True);
            Assert.That(dialog.childCount, Is.EqualTo(2));
            Assert.That(((Label)dialog[0]).text, Is.EqualTo("Recovered"));
            Assert.That(((Label)dialog[1][0]).text, Is.EqualTo("Action"));
            Assert.DoesNotThrow(node.Unmount);
        }

        [Test]
        public async Task ConfirmDialog_CompletesTrueOnlyAfterItsAsyncConfirmationSucceeds() {
            var completion = new TaskCompletionSource<bool>();
            var dialog = new ConfirmDialog(
                new Text("Delete this project?"),
                new Text("Delete project"),
                confirmText: "Delete",
                confirmVariant: ButtonVariant.Destructive,
                onConfirm: _ => completion.Task);
            var root = new VisualElement();
            using var mount = Framework.Mount(dialog, root);

            var running = dialog.ConfirmAction.Run();
            Assert.That(dialog.Result.IsCompleted, Is.False);
            Assert.That(dialog.ConfirmAction.Status.Value, Is.EqualTo(AsyncActionStatus.Running));

            completion.SetResult(true);
            await running;

            Assert.That(await dialog.Result, Is.True);
            Assert.That(dialog.ConfirmAction.Status.Value, Is.EqualTo(AsyncActionStatus.Succeeded));
        }

        [Test]
        public async Task ConfirmDialog_CancelCompletesFalseAndCancelsTheConfirmation() {
            var cancellationObserved = new TaskCompletionSource<bool>();
            var dialog = new ConfirmDialog(
                new Text("Discard changes?"),
                onConfirm: token => {
                    token.Register(() => cancellationObserved.TrySetResult(true));
                    return Task.Delay(Timeout.Infinite, token);
                });
            var root = new VisualElement();
            using var mount = Framework.Mount(dialog, root);

            var running = dialog.ConfirmAction.Run();
            dialog.Cancel();
            await cancellationObserved.Task;
            await running;

            Assert.That(await dialog.Result, Is.False);
            Assert.That(dialog.ConfirmAction.Status.Value, Is.EqualTo(AsyncActionStatus.Cancelled));
        }

        [Test]
        public async Task ConfirmDialog_CompatibleUpdatePreservesContentStateAndSwitchesResultOwner() {
            var firstContent = new CounterWidget();
            var firstDialog = new ConfirmDialog(
                firstContent,
                new Text("First title"),
                confirmText: "Save");
            var root = new VisualElement();
            var node = (ConfirmDialogNode)firstDialog.CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = root[0];
            var contentState = firstContent.MountedState;
            contentState.Increment();
            var nextContent = new CounterWidget();
            var nextDialog = new ConfirmDialog(
                nextContent,
                new Text("Next title"),
                confirmText: "Apply");

            Assert.That(node.TryUpdate(firstDialog), Is.True);
            Assert.That(firstDialog.Result.IsCompleted, Is.False);
            Assert.That(node.TryUpdate(nextDialog), Is.True);
            Assert.That(root[0], Is.SameAs(native));
            Assert.That(nextContent.MountedState, Is.SameAs(contentState));
            Assert.That(((Label)native[0]).text, Is.EqualTo("Next title"));
            Assert.That(((Label)native[1]).text, Is.EqualTo("1"));
            Assert.That(firstDialog.Result.IsCompleted, Is.True);
            Assert.That(await firstDialog.Result, Is.False);
            Assert.That(nextDialog.Result.IsCompleted, Is.False);

            node.Unmount();

            Assert.That(await nextDialog.Result, Is.False);
            Assert.That(root.childCount, Is.Zero);
        }

        [Test]
        public void Dispose_MultipleChildCleanupFailuresAreAggregatedAfterTheWholeTreeDetaches() {
            var root = new VisualElement();
            var mount = Framework.Mount(
                new Column(new Widget[]
                {
                new CleanupWidget(() => throw new InvalidOperationException("First cleanup failure.")),
                new CleanupWidget(() => throw new ArgumentException("Second cleanup failure."))
                }),
                root);

            var exception = Assert.Throws<InvalidOperationException>(mount.Dispose);

            Assert.That(root.childCount, Is.Zero);
            Assert.That(mount.IsMounted, Is.False);
            Assert.That(exception!.InnerException, Is.TypeOf<AggregateException>());
            Assert.That(((AggregateException)exception.InnerException!).InnerExceptions, Has.Count.EqualTo(2));
            Assert.DoesNotThrow(mount.Dispose);
        }

        [Test]
        public async Task OverlayController_ShowConfirmCompletesFalseWhenTheOverlayCloses() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);

            var decision = controller.ShowConfirm(new Text("Leave this page?"), new Text("Unsaved changes"));

            Assert.That(controller.TryCloseTop(), Is.True);
            Assert.That(await decision, Is.False);
        }

        [Test]
        public async Task OverlayController_ShowConfirmHonorsCustomModalOptions() {
            var root = new VisualElement();
            var controller = new OverlayController();
            using var mount = Framework.Mount(new OverlayHost(new Text("Content"), controller), root);

            var decision = controller.ShowConfirm(
                new Text("Leave this page?"),
                options: new ModalOptions(dismissOnBack: false));

            Assert.That(controller.TryHandleBack(), Is.False);
            Assert.That(decision.IsCompleted, Is.False);
            Assert.That(controller.TryCloseTop(), Is.True);
            Assert.That(await decision, Is.False);
        }

        [Test]
        public void ButtonVariant_DestructiveResolvesFromTheTheme() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Theme(CreateTestTheme(), new Button("Delete", () => { }, variant: ButtonVariant.Destructive)),
                root);
            var button = (UnityEngine.UIElements.Button)root[0][0];

            Assert.That(button.style.backgroundColor.value, Is.EqualTo(ButtonStyle.Destructive.Background));
        }

        [Test]
        public void Container_AppliesExplicitDecorationAndMountsItsChild() {
            var root = new VisualElement();
            var backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            using var mount = Framework.Mount(
                new Container(
                    new Text("Decorated"),
                    new BoxDecoration(backgroundColor, BorderRadius.All(8f))),
                root);
            var container = root[0][0];

            Assert.That(container.style.backgroundColor.value, Is.EqualTo(backgroundColor));
            Assert.That(container.style.borderTopLeftRadius.value.value, Is.EqualTo(8f));
            Assert.That(container.style.borderTopRightRadius.value.value, Is.EqualTo(8f));
            Assert.That(container.style.borderBottomRightRadius.value.value, Is.EqualTo(8f));
            Assert.That(container.style.borderBottomLeftRadius.value.value, Is.EqualTo(8f));
            Assert.That(((Label)container[0]).text, Is.EqualTo("Decorated"));
        }

        [Test]
        public void Container_ComposesOptionalDecorationAndPadding() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Container(
                    new Text("Card"),
                    new BoxDecoration(backgroundColor: Color.gray),
                    EdgeInsets.Symmetric(horizontal: 12f, vertical: 8f)),
                root);
            var container = root[0][0];

            Assert.That(container.style.backgroundColor.value, Is.EqualTo(Color.gray));
            Assert.That(container.style.paddingLeft.value.value, Is.EqualTo(12f));
            Assert.That(container.style.paddingTop.value.value, Is.EqualTo(8f));
            Assert.That(((Label)container[0]).text, Is.EqualTo("Card"));
        }

        [Test]
        public void BorderRadius_AllCreatesExpectedImmutableValue() {
            Assert.That(BorderRadius.All(6f), Is.EqualTo(new BorderRadius(6f, 6f, 6f, 6f)));
        }

        [Test]
        public void Container_AppliesTypedBorderDecoration() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Container(
                    new Text("Bordered"),
                    new BoxDecoration(border: Border.All(Color.magenta, width: 2f))),
                root);
            var container = root[0][0];

            Assert.That(container.style.borderLeftColor.value, Is.EqualTo(Color.magenta));
            Assert.That(container.style.borderRightWidth.value, Is.EqualTo(2f));
            Assert.That(container.style.borderBottomColor.value, Is.EqualTo(Color.magenta));
            Assert.That(container.style.borderTopWidth.value, Is.EqualTo(2f));
        }

        [Test]
        public void Container_AppliesCachedLinearGradientAndClipping() {
            var root = new VisualElement();
            var gradient = new LinearGradient(Color.red, new Color(0f, 0f, 1f, 0.25f), 90f);
            using var mount = Framework.Mount(
                new Column(new Widget[] {
                    new Container(
                        new Text("First"),
                        ClipBehavior.HardEdge,
                        new BoxDecoration(gradient, borderRadius: BorderRadius.All(12f))),
                    new Container(new Text("Second"), new BoxDecoration(gradient))
                }),
                root);
            var first = root[0][0][0];
            var second = root[0][0][1];
            var firstTexture = first.style.backgroundImage.value.texture;

            Assert.That(first.style.overflow.value, Is.EqualTo(Overflow.Hidden));
            Assert.That(firstTexture, Is.Not.Null);
            Assert.That(second.style.backgroundImage.value.texture, Is.SameAs(firstTexture));
            Assert.That(firstTexture!.width, Is.EqualTo(64));
            Assert.That(firstTexture.height, Is.EqualTo(64));
        }

        [Test]
        public void LinearGradient_NormalizesAngleAndRejectsNonFiniteValues() {
            Assert.That(new LinearGradient(Color.black, Color.white, 450f).Angle, Is.EqualTo(90f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LinearGradient(Color.black, Color.white, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Container(new Text("Invalid"), (ClipBehavior)999));
        }

        [Test]
        public void Text_MapsWrappingOverflowAndMaximumLines() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Text(
                    "A long line",
                    new TextStyle(fontSize: 10f),
                    softWrap: false,
                    overflow: global::LumaFlow.TextOverflow.Ellipsis,
                    maxLines: 2),
                root);
            var label = (Label)root[0][0];

            Assert.That(label.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
            Assert.That(label.style.textOverflow.value, Is.EqualTo(UnityEngine.UIElements.TextOverflow.Ellipsis));
            Assert.That(label.style.overflow.value, Is.EqualTo(Overflow.Hidden));
            Assert.That(label.style.maxHeight.value.value, Is.EqualTo(24f).Within(0.001f));
        }

        [Test]
        public void Text_RejectsNonPositiveMaximumLines() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Text("Invalid", maxLines: 0));
        }

        [Test]
        public void Button_ArbitraryChildReconcilesInPlaceAndInvokesLatestCallback() {
            var root = new VisualElement();
            var firstCalls = 0;
            var secondCalls = 0;
            var node = (ButtonNode)new Button(
                new Row(new Widget[] { new Icon(LumaIcons.Save), new Text("Save") }),
                () => firstCalls++,
                semanticsLabel: "Save project").CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.Button)root[0];
            var row = native[0];

            Assert.That(native.text, Is.Empty);
            Assert.That(row.childCount, Is.EqualTo(2));
            Assert.That(node.TryUpdate(new Button(
                new Row(new Widget[] { new Icon(LumaIcons.Save), new Text("Save now") }),
                () => secondCalls++,
                semanticsLabel: "Save project")), Is.True);
            node.HandleClicked();

            Assert.That(root[0], Is.SameAs(native));
            Assert.That(native[0], Is.SameAs(row));
            Assert.That(((Label)row[1]).text, Is.EqualTo("Save now"));
            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void Pressable_AddsNativeActivationWithoutOwningTheChildSurface() {
            var root = new VisualElement();
            var calls = 0;
            var node = (PressableNode)new Pressable(
                new Container(new Text("Open"), new BoxDecoration(backgroundColor: Color.cyan)),
                () => calls++,
                semanticsLabel: "Open card").CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.Button)root[0];

            Assert.That(native.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(native[0].style.backgroundColor.value, Is.EqualTo(Color.cyan));
            node.HandleClicked();
            Assert.That(calls, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void Pressable_StateBuilderCombinesStatesAndPreservesNestedState() {
            var observed = new List<WidgetStates>();
            var built = new List<CounterWidget>();
            var cursor = new PointerCursor(Texture2D.whiteTexture, new Vector2(1f, 2f));
            Widget Build(WidgetStates states) {
                observed.Add(states);
                var child = new CounterWidget();
                built.Add(child);
                return child;
            }
            var root = new VisualElement();
            var node = (PressableNode)new Pressable(Build, () => { }, cursor: cursor).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);
            var native = (UnityEngine.UIElements.Button)root[0];
            var originalState = built[0].MountedState;

            node.SetHovered(true);
            node.SetFocused(true);
            node.SetPressed(true);

            Assert.That(observed[^1], Is.EqualTo(
                WidgetStates.Hovered | WidgetStates.Focused | WidgetStates.Pressed));
            Assert.That(built[^1].MountedState, Is.SameAs(originalState));
            Assert.That(native.style.cursor.value.texture, Is.SameAs(Texture2D.whiteTexture));
            Assert.That(native.style.cursor.value.hotspot, Is.EqualTo(new Vector2(1f, 2f)));

            Assert.That(node.TryUpdate(new Pressable(Build, () => { }, enabled: false, cursor: cursor)), Is.True);
            Assert.That(observed[^1].HasFlag(WidgetStates.Disabled), Is.True);
            Assert.That(native.style.cursor.keyword, Is.EqualTo(StyleKeyword.Null));
            node.Unmount();
        }

        [Test]
        public void PointerCursor_RejectsInvalidConfiguration() {
            Assert.Throws<ArgumentNullException>(() => new PointerCursor(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PointerCursor(Texture2D.whiteTexture, new Vector2(float.NaN, 0f)));
        }

        [Test]
        public void Button_ChildUsesResolvedForegroundAsDefaultTextAndIconTheme() {
            var root = new VisualElement();
            var style = new ButtonStyle(foreground: Color.yellow);
            using var mount = Framework.Mount(
                new Theme(
                    CreateTestTheme(),
                    new Button(
                        new Row(new Widget[] { new Icon(LumaIcons.Save), new Text("Save") }),
                        () => { },
                        style)),
                root);
            var row = root[0][0][0];

            Assert.That(((UnityEngine.UIElements.Image)row[0]).tintColor, Is.EqualTo(Color.yellow));
            Assert.That(((Label)row[1]).style.color.value, Is.EqualTo(Color.yellow));
        }

        [Test]
        public void Button_ChildThemeOverridesPreserveUnrelatedComponentThemes() {
            var baseline = CreateTestTheme();
            var checkboxTheme = new CheckboxTheme(new CheckboxStyle(size: 27f));
            var theme = baseline.CopyWith(checkboxTheme: checkboxTheme);
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Theme(
                    theme,
                    new Button(new Checkbox(new State<bool>(true)), () => { })),
                root);
            var button = (UnityEngine.UIElements.Button)root[0][0];
            var checkbox = (UnityEngine.UIElements.Toggle)button[0];
            var input = checkbox.Q<VisualElement>(className: "unity-toggle__input")
                ?? checkbox.Q<VisualElement>(className: "unity-base-field__input");

            Assert.That(input, Is.Not.Null);
            Assert.That(input!.style.width.value.value, Is.EqualTo(27f));
        }

        [Test]
        public void Button_BorderResolvesForTheCurrentInteractionState() {
            var style = new ButtonStyle(
                border: Border.All(Color.gray),
                borderByState: WidgetStateProperty<Border?>.ResolveWith(states =>
                    (states & WidgetStates.Focused) != 0 ? Border.All(Color.blue, 2f) : null));
            var root = new VisualElement();
            var node = (ButtonNode)new Button("Outlined", () => { }, style).CreateNode();
            node.Mount(parent: null, new BuildContext(), root);

            Assert.That(root[0].style.borderTopColor.value, Is.EqualTo(Color.gray));
            node.SetFocused(true);
            Assert.That(root[0].style.borderTopColor.value, Is.EqualTo(Color.blue));
            Assert.That(root[0].style.borderTopWidth.value, Is.EqualTo(2f));
            node.Unmount();
        }

        [Test]
        public void Card_UsesSharedBoxDecorationIncludingBorder() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Card(
                    new Text("Surface"),
                    new BoxDecoration(
                        backgroundColor: Color.white,
                        borderRadius: BorderRadius.All(9f),
                        border: Border.All(Color.black, 3f)),
                    padding: EdgeInsets.All(7f)),
                root);
            var card = root[0][0];

            Assert.That(card.style.backgroundColor.value, Is.EqualTo(Color.white));
            Assert.That(card.style.borderTopLeftRadius.value.value, Is.EqualTo(9f));
            Assert.That(card.style.borderLeftColor.value, Is.EqualTo(Color.black));
            Assert.That(card.style.borderBottomWidth.value, Is.EqualTo(3f));
            Assert.That(card.style.paddingTop.value.value, Is.EqualTo(7f));
        }

        [Test]
        public void LinearProgressIndicator_MapsStyleAndTracksControlledValue() {
            var value = new State<float>(0.25f);
            var style = new LinearProgressIndicatorStyle(
                valueColor: Color.green,
                trackColor: Color.gray,
                minHeight: 3f,
                borderRadius: BorderRadius.All(1.5f));
            var root = new VisualElement();
            using var mount = Framework.Mount(new LinearProgressIndicator(value, style, "Upload"), root);
            var track = root[0][0];
            var fill = track[0];

            Assert.That(track.style.backgroundColor.value, Is.EqualTo(Color.gray));
            Assert.That(track.style.height.value.value, Is.EqualTo(3f));
            Assert.That(track.style.borderTopLeftRadius.value.value, Is.EqualTo(1.5f));
            Assert.That(fill.style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(fill.style.width.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(fill.style.width.value.value, Is.EqualTo(25f));
            Assert.That(fill.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(fill.style.left.value.value, Is.EqualTo(0f));
            Assert.That(fill.style.bottom.value.value, Is.EqualTo(0f));

            value.Value = 0.8f;
            Assert.That(fill.style.width.value.value, Is.EqualTo(80f));
        }

        [Test]
        public void LinearProgressIndicator_ValidatesValueAndGeometry() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(-0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicatorStyle(minHeight: 0f));
        }

        [Test]
        public void LinearProgressIndicator_ClampsOversizedRadiiToItsHeight() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new LinearProgressIndicator(
                    0.5f,
                    new LinearProgressIndicatorStyle(minHeight: 4f, borderRadius: BorderRadius.All(999f))),
                root);

            Assert.That(root[0][0].style.borderTopLeftRadius.value.value, Is.EqualTo(2f));
            Assert.That(root[0][0][0].style.borderTopRightRadius.value.value, Is.EqualTo(2f));
        }

        [Test]
        public void BorderSide_RejectsInvalidWidth() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BorderSide(Color.black, float.NaN));
        }

        [Test]
        public void AnimationPrimitives_ValidateAndInterpolateTypedValues() {
            Assert.That(Curves.Linear.Transform(0.35f), Is.EqualTo(0.35f));
            Assert.That(Curves.EaseIn.Transform(0.5f), Is.LessThan(0.5f));
            Assert.That(Curves.EaseOut.Transform(0.5f), Is.GreaterThan(0.5f));
            Assert.That(new FloatTween(2f, 6f).Transform(0.25f), Is.EqualTo(3f));

            var color = new ColorTween(Color.black, Color.white).Transform(0.5f);
            Assert.That(color.r, Is.EqualTo(0.5f).Within(0.0001f));
            var insets = new EdgeInsetsTween(EdgeInsets.Zero, EdgeInsets.All(8f)).Transform(0.5f);
            Assert.That(insets, Is.EqualTo(EdgeInsets.All(4f)));
            var radius = new BorderRadiusTween(BorderRadius.All(2f), BorderRadius.All(10f)).Transform(0.5f);
            Assert.That(radius, Is.EqualTo(BorderRadius.All(6f)));

            Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Linear.Transform(-0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cubic(-0.1f, 0f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FloatTween(float.NaN, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationSpec(TimeSpan.Zero));
        }

        [Test]
        public void ImplicitAnimation_RetargetsFromDisplayedValueAndCompletesExactly() {
            var animation = new ImplicitAnimation<float>();
            var spec = new AnimationSpec(TimeSpan.FromSeconds(1));

            Assert.That(animation.Start(0f, 1f, new FloatTween(0f, 1f), spec, 10d, false), Is.False);
            Assert.That(animation.Sample(9d).Value, Is.EqualTo(0f));
            Assert.That(animation.Sample(10.4d).Value, Is.EqualTo(0.4f).Within(0.0001f));
            var displayed = animation.Current;
            animation.Start(displayed, 2f, new FloatTween(displayed, 2f), spec, 10.4d, false);
            Assert.That(animation.Sample(10.9d).Value, Is.EqualTo(1.2f).Within(0.0001f));
            var completed = animation.Sample(11.4d);
            Assert.That(completed.Completed, Is.True);
            Assert.That(completed.Value, Is.EqualTo(2f));
            Assert.That(animation.IsRunning, Is.False);
        }

        [Test]
        public void ImplicitAnimation_HasDeterministicCancellationAndReducedMotionPolicy() {
            var animation = new ImplicitAnimation<float>();
            var normal = new AnimationSpec(TimeSpan.FromSeconds(1));
            var preserve = new AnimationSpec(TimeSpan.FromSeconds(1), behavior: AnimationBehavior.Preserve);

            Assert.That(animation.Start(0f, 1f, new FloatTween(0f, 1f), normal, 2d, true), Is.True);
            Assert.That(animation.Current, Is.EqualTo(1f));
            Assert.That(animation.IsRunning, Is.False);
            Assert.That(animation.Start(0f, 1f, new FloatTween(0f, 1f), preserve, 3d, true), Is.False);
            Assert.That(animation.IsRunning, Is.True);
            animation.Cancel();
            Assert.That(animation.Sample(4d).Completed, Is.False);
            Assert.That(animation.Current, Is.EqualTo(0f));
        }

        [Test]
        public void MediaQueryData_WithSizePreservesAnimationPolicyThroughLayoutBuilder() {
            var data = new MediaQueryData(320f, 640f, disableAnimations: true);
            Assert.That(data.WithSize(800f, 600f), Is.EqualTo(new MediaQueryData(800f, 600f, true)));
            Assert.That(data, Is.Not.EqualTo(new MediaQueryData(320f, 640f, false)));

            var capture = new MediaQueryCaptureWidget();
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new MediaQuery(
                    data,
                    new LayoutBuilder((_, _) => capture)),
                root);

            Assert.That(capture.MediaQuery.DisableAnimations, Is.True);
        }

        [Test]
        public void TweenAnimationBuilder_AnimatesLocallyAndInvokesLatestOnEndOnce() {
            var values = new List<float>();
            var firstEnds = 0;
            var latestEnds = 0;
            var root = new VisualElement();
            var node = (TweenAnimationBuilderNode<float>)new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f),
                TimeSpan.FromSeconds(1),
                value => {
                    values.Add(value);
                    return new Text("value");
                },
                onEnd: () => firstEnds++).CreateNode();
            node.Mount(null, new BuildContext(), root);
            var startedAt = node.AnimationStartedAt;

            node.TickAt(startedAt + 0.25d);
            Assert.That(node.CurrentValue, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(node.TryUpdate(new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f),
                TimeSpan.FromSeconds(1),
                value => new Text("latest"),
                onEnd: () => latestEnds++)), Is.True);
            node.TickAt(startedAt + 1d);

            Assert.That(node.CurrentValue, Is.EqualTo(1f));
            Assert.That(firstEnds, Is.Zero);
            Assert.That(latestEnds, Is.EqualTo(1));
            Assert.That(node.IsAnimating, Is.False);
            node.Unmount();
        }

        [Test]
        public void TweenAnimationBuilder_RetargetsAndPreservesCompatibleChildState() {
            var firstCounter = new CounterWidget();
            var secondCounter = new CounterWidget();
            var root = new VisualElement();
            var node = (TweenAnimationBuilderNode<float>)new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f),
                TimeSpan.FromSeconds(1),
                (value, child) => new SizedBox(child!, width: 100f + value),
                child: firstCounter).CreateNode();
            node.Mount(null, new BuildContext(), root);
            var originalState = firstCounter.MountedState;
            var firstStart = node.AnimationStartedAt;
            node.TickAt(firstStart + 0.4d);

            Assert.That(node.TryUpdate(new TweenAnimationBuilder<float>(
                new FloatTween(0f, 2f),
                TimeSpan.FromSeconds(1),
                (value, child) => new SizedBox(child!, width: 100f + value),
                child: secondCounter)), Is.True);
            var retargetStart = node.AnimationStartedAt;
            Assert.That(node.CurrentValue, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(secondCounter.MountedState, Is.SameAs(originalState));
            node.TickAt(retargetStart + 0.5d);
            Assert.That(node.CurrentValue, Is.EqualTo(1.2f).Within(0.0001f));
            node.Unmount();
        }

        [Test]
        public void TweenAnimationBuilder_RespectsReducedMotionAndUnmountCancellation() {
            var completed = 0;
            var root = new VisualElement();
            var node = (TweenAnimationBuilderNode<float>)new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f),
                TimeSpan.FromSeconds(1),
                value => new Text("value"),
                onEnd: () => completed++).CreateNode();
            var context = new BuildContext(mediaQuery: new MediaQueryData(100f, 100f));
            node.Mount(null, context, root);

            context.UpdateMediaQuery(new MediaQueryData(100f, 100f, disableAnimations: true));
            Assert.That(node.CurrentValue, Is.EqualTo(1f));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(node.IsAnimating, Is.False);
            node.Unmount();
            node.TickAt(node.AnimationStartedAt + 2d);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void TweenAnimationBuilder_PreserveBehaviorIgnoresReducedMotionSnap() {
            var root = new VisualElement();
            var node = (TweenAnimationBuilderNode<float>)new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f),
                TimeSpan.FromSeconds(1),
                value => new Text("value"),
                behavior: AnimationBehavior.Preserve).CreateNode();
            node.Mount(null, new BuildContext(mediaQuery: new MediaQueryData(100f, 100f, true)), root);

            Assert.That(node.CurrentValue, Is.EqualTo(0f));
            Assert.That(node.IsAnimating, Is.True);
            node.Unmount();
        }

        [Test]
        public void AnimatedOpacity_RetargetsFromCurrentValueAndCompletesOnce() {
            var value = new State<float>(0f);
            var completed = 0;
            var root = new VisualElement();
            var node = (AnimatedOpacityNode)new AnimatedOpacity(
                new Text("fade"),
                value,
                TimeSpan.FromSeconds(1),
                onEnd: () => completed++).CreateNode();
            node.Mount(null, new BuildContext(), root);

            value.Value = 1f;
            var firstStart = node.AnimationStartedAt;
            node.TickAt(firstStart + 0.4d);
            Assert.That(node.CurrentOpacity, Is.EqualTo(0.4f).Within(0.0001f));
            value.Value = 0.8f;
            var secondStart = node.AnimationStartedAt;
            node.TickAt(secondStart + 0.5d);
            Assert.That(node.CurrentOpacity, Is.EqualTo(0.6f).Within(0.0001f));
            node.TickAt(secondStart + 1d);
            Assert.That(node.CurrentOpacity, Is.EqualTo(0.8f));
            Assert.That(completed, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void AnimatedOpacity_ReducedMotionSnapsAndCompletesOnce() {
            var value = new State<float>(0f);
            var completed = 0;
            var root = new VisualElement();
            var node = (AnimatedOpacityNode)new AnimatedOpacity(
                new Text("fade"),
                value,
                TimeSpan.FromSeconds(1),
                onEnd: () => completed++).CreateNode();
            var context = new BuildContext(mediaQuery: new MediaQueryData(100f, 100f, true));
            node.Mount(null, context, root);

            value.Value = 1f;
            Assert.That(node.CurrentOpacity, Is.EqualTo(1f));
            Assert.That(node.IsAnimating, Is.False);
            Assert.That(completed, Is.EqualTo(1));
            node.Unmount();
        }

        [Test]
        public void NavigatorFade_UsesSharedReducedMotionPolicy() {
            var navigator = new Navigator(new Route(new WidgetKey("root"), new Text("root")));
            var root = new VisualElement();
            var context = new BuildContext(mediaQuery: new MediaQueryData(100f, 100f));
            var node = (NavigatorHostNode)new NavigatorHost(navigator).CreateNode();
            node.Mount(null, context, root);

            navigator.Push(new Route(
                new WidgetKey("details"),
                new Text("details"),
                RouteTransition.Fade(TimeSpan.FromSeconds(1), Curves.EaseOut)));
            Assert.That(root[0][1].style.opacity.value, Is.EqualTo(0f));
            context.UpdateMediaQuery(new MediaQueryData(100f, 100f, disableAnimations: true));
            Assert.That(root[0][1].style.opacity.value, Is.EqualTo(1f));
            node.Unmount();
        }

        [Test]
        public void AnimationWidgets_RejectInvalidConfiguration() {
            Assert.Throws<ArgumentNullException>(() => new TweenAnimationBuilder<float>(
                null!, TimeSpan.FromSeconds(1), value => new Text("value")));
            Assert.Throws<ArgumentNullException>(() => new TweenAnimationBuilder<float>(
                new FloatTween(0f, 1f), TimeSpan.FromSeconds(1), (Func<float, Widget>)null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(
                new Text("fade"), new State<float>(1f), TimeSpan.Zero));
        }

        [Test]
        public void MountHandle_CapturesImmutableWidgetStateNativeAndInheritedDiagnostics() {
            var counter = new CounterWidget();
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Theme(
                    CreateTestTheme(),
                    new Column(new Widget[] {
                        counter.WithKey(new WidgetKey("counter")),
                        new InheritedDependencyProbeWidget(readTheme: true, readMediaQuery: true)
                    })),
                root);

            var diagnostics = mount.CaptureDiagnostics();
            var text = diagnostics.ToStringDeep();

            Assert.That(diagnostics.MountId, Is.EqualTo(mount.DiagnosticId));
            Assert.That(diagnostics.Root.WidgetType, Is.EqualTo(typeof(Theme).FullName));
            Assert.That(diagnostics.Root.LifecycleState, Is.EqualTo("Mounted"));
            Assert.That(text, Does.Contain("KeyedSubtree key='counter'"));
            Assert.That(text, Does.Contain(typeof(CounterState).FullName));
            Assert.That(text, Does.Contain("inherits=[MediaQuery,Theme]"));
            Assert.That(text, Does.Contain("theme.primary=#"));
            Assert.That(text, Does.Contain("media=0x0"));
            Assert.That(text, Does.Contain("native=UnityEngine.UIElements"));
            Assert.That(diagnostics.Findings, Is.Empty);
        }

        [Test]
        public void MountHandle_RebuildRefreshesDescriptionsAndPreservesMountedState() {
            var label = "Before";
            var counter = new CounterWidget();
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReassembleProbeWidget(() => label, counter),
                root);
            var originalState = counter.MountedState;
            originalState.Increment();

            label = "After";
            mount.Rebuild();

            Assert.That(counter.MountedState, Is.SameAs(originalState));
            Assert.That(originalState.InitializationCount, Is.EqualTo(1));
            Assert.That(root.Query<Label>().ToList().ConvertAll(item => item.text),
                Is.EquivalentTo(new[] { "After", "1" }));
        }

        [Test]
        public void MountHandle_RestartCreatesFreshStateAndKeepsOneHostChild() {
            var firstCounter = new CounterWidget();
            var secondCounter = new CounterWidget();
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new ReassembleProbeWidget(() => "First", firstCounter),
                root);
            var firstState = firstCounter.MountedState;

            mount.Restart(new ReassembleProbeWidget(() => "Second", secondCounter));

            Assert.That(secondCounter.MountedState, Is.Not.SameAs(firstState));
            Assert.That(secondCounter.MountedState.InitializationCount, Is.EqualTo(1));
            Assert.That(firstState.DisposeCount, Is.EqualTo(1));
            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root.Query<Label>().ToList().ConvertAll(item => item.text),
                Is.EquivalentTo(new[] { "Second", "0" }));
        }

        [Test]
        public void MountHandle_FailedRestartLeavesPreviousTreeActive() {
            var root = new VisualElement();
            using var mount = Framework.Mount(new Text("Stable"), root);
            var originalNative = root[0][0];

            Assert.Throws<InvalidOperationException>(() => mount.Restart(new ThrowingWidget()));

            Assert.That(mount.IsMounted, Is.True);
            Assert.That(root[0].childCount, Is.EqualTo(1));
            Assert.That(root[0][0], Is.SameAs(originalNative));
            Assert.That(((Label)root[0][0]).text, Is.EqualTo("Stable"));
        }

        [Test]
        public void MountHandle_DisposedHandleRejectsRebuildAndRestart() {
            var mount = Framework.Mount(new Text("Disposed"), new VisualElement());
            mount.Dispose();

            Assert.Throws<ObjectDisposedException>(() => mount.Rebuild());
            Assert.Throws<ObjectDisposedException>(() => mount.Restart(new Text("Replacement")));
        }

        [Test]
        public void Diagnostics_FlagsOnlyUnkeyedStatefulSiblingsWithActionablePaths() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(new Widget[] { new CounterWidget(), new CounterWidget() }),
                root);

            var findings = mount.CaptureDiagnostics().Findings;

            Assert.That(findings, Has.Count.EqualTo(2));
            Assert.That(findings[0].Code, Is.EqualTo("LF1001"));
            Assert.That(findings[0].Severity, Is.EqualTo(LumaFlowDiagnosticSeverity.Warning));
            Assert.That(findings[0].Message, Does.Contain("WidgetKey"));
            Assert.That(findings[0].WidgetPath, Does.Contain("[0]"));
            Assert.That(findings[1].WidgetPath, Does.Contain("[1]"));
        }

        [Test]
        public void Diagnostics_ReportsNativeOwnershipBoundaryWithoutRetainingTheElement() {
            var element = new VisualElement { name = "borrowed" };
            var root = new VisualElement();
            using var mount = Framework.Mount(new Native(element), root);

            var snapshot = mount.CaptureDiagnostics();

            Assert.That(snapshot.Findings, Has.Count.EqualTo(1));
            Assert.That(snapshot.Findings[0].Code, Is.EqualTo("LF1002"));
            Assert.That(snapshot.Findings[0].Severity, Is.EqualTo(LumaFlowDiagnosticSeverity.Info));
            Assert.That(snapshot.Root.NativeElementName, Is.EqualTo("borrowed"));
        }

        [Test]
        public void Diagnostics_ExposeFlexSizingAlignmentAndBuilderConstraints() {
            var root = new VisualElement();
            using var mount = Framework.Mount(
                new Row(
                    new Widget[] {
                        new Expanded(new SizedBox(new Text("Flexible"), width: 80f), flex: 2),
                        new LayoutBuilder((_, constraints) => new Text(constraints.MaxWidth.ToString()))
                    },
                    gap: 12f,
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    crossAxisAlignment: CrossAxisAlignment.Center),
                root);

            var snapshot = mount.CaptureDiagnostics();
            var expanded = snapshot.Root.Children[0];
            var sized = expanded.Children[0];
            var builder = snapshot.Root.Children[1];

            Assert.That(snapshot.Root.Properties["layout.axis"], Is.EqualTo("horizontal"));
            Assert.That(snapshot.Root.Properties["layout.gap"], Is.EqualTo("12"));
            Assert.That(snapshot.Root.Properties["layout.mainAxisAlignment"], Is.EqualTo("SpaceBetween"));
            Assert.That(expanded.Properties["layout.flex"], Is.EqualTo("2"));
            Assert.That(expanded.Properties["layout.fit"], Is.EqualTo("Tight"));
            Assert.That(sized.Properties["constraints.width"], Is.EqualTo("80"));
            Assert.That(builder.Properties["layout.observedAxis"], Is.EqualTo("Horizontal"));
            Assert.That(builder.Properties, Does.ContainKey("constraints.maxWidth"));
            Assert.That(snapshot.ToStringDeep(), Does.Contain("layout.flex=2"));
        }

        [Test]
        public void DiagnosticsRegistryTracksLiveMountsAndDisposedHandlesRejectCapture() {
            var first = Framework.Mount(new Text("first"), new VisualElement());
            var second = Framework.Mount(new Text("second"), new VisualElement());
            try {
                var activeIds = new HashSet<int>();
                foreach (var snapshot in LumaFlowDiagnostics.CaptureActiveTrees()) {
                    activeIds.Add(snapshot.MountId);
                }
                Assert.That(activeIds, Has.Member(first.DiagnosticId));
                Assert.That(activeIds, Has.Member(second.DiagnosticId));

                first.Dispose();
                Assert.Throws<ObjectDisposedException>(() => first.CaptureDiagnostics());
                activeIds.Clear();
                foreach (var snapshot in LumaFlowDiagnostics.CaptureActiveTrees()) {
                    activeIds.Add(snapshot.MountId);
                }
                Assert.That(activeIds, Has.No.Member(first.DiagnosticId));
                Assert.That(activeIds, Has.Member(second.DiagnosticId));
            } finally {
                first.Dispose();
                second.Dispose();
            }
        }

        private sealed class ThrowingWidget : Widget {
            internal override WidgetNode CreateNode() {
                return new ThrowingNode(this);
            }
        }

        private sealed class ThrowingNode : WidgetNode {
            public ThrowingNode(Widget widget)
                : base(widget) {
            }

            protected override VisualElement CreateElement(BuildContext context) {
                throw new InvalidOperationException("Expected mount failure.");
            }
        }

        private sealed class ReassembleProbeWidget : StatelessWidget {
            private readonly Func<string> _label;
            private readonly CounterWidget _counter;

            public ReassembleProbeWidget(Func<string> label, CounterWidget counter) {
                _label = label;
                _counter = counter;
            }

            public override Widget Build(BuildContext context) => new Column(new Widget[] {
                new Text(_label()),
                _counter
            });
        }

        private sealed class MountCleanupFailureWidget : Widget {
            internal override WidgetNode CreateNode() => new MountCleanupFailureNode(this);
        }

        private sealed class MountCleanupFailureNode : WidgetNode {
            public MountCleanupFailureNode(Widget widget)
                : base(widget) {
            }

            protected override VisualElement CreateElement(BuildContext context) => new();

            protected override void OnMounted() {
                Bindings.Add(() => throw new InvalidOperationException("Expected cleanup failure."));
                throw new InvalidOperationException("Expected mount failure.");
            }
        }

        private sealed class InheritedDependencyProbeWidget : StatelessWidget {
            public InheritedDependencyProbeWidget(
                bool readTheme = false,
                bool readMediaQuery = false,
                bool throwOnBuild = false) {
                ReadTheme = readTheme;
                ReadMediaQuery = readMediaQuery;
                ThrowOnBuild = throwOnBuild;
            }

            public bool ReadTheme { get; set; }
            public bool ReadMediaQuery { get; set; }
            public bool ThrowOnBuild { get; set; }
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context) {
                BuildCount++;
                if (ReadTheme) _ = context.Theme;
                if (ReadMediaQuery) _ = context.MediaQuery;
                if (ThrowOnBuild) throw new InvalidOperationException("Expected inherited build failure.");
                return new Text(BuildCount.ToString());
            }
        }

        private sealed class CleanupWidget : Widget {
            private readonly Action _cleanup;

            public CleanupWidget(Action cleanup) {
                _cleanup = cleanup;
            }

            internal override WidgetNode CreateNode() {
                return new CleanupNode(this, _cleanup);
            }
        }

        private sealed class CleanupNode : WidgetNode {
            private readonly Action _cleanup;

            public CleanupNode(Widget widget, Action cleanup)
                : base(widget) {
                _cleanup = cleanup;
            }

            protected override VisualElement CreateElement(BuildContext context) {
                return new VisualElement();
            }

            protected override void OnMounted() {
                Bindings.Add(_cleanup);
            }
        }

        private sealed class NavigatorCaptureWidget : StatelessWidget {
            public Navigator Navigator { get; private set; }
            public ThemeData Theme { get; private set; }

            public override Widget Build(BuildContext context) {
                Navigator = context.Navigator;
                Theme = context.Theme;
                return new Text("Captured");
            }
        }

        private sealed class MediaQueryCaptureWidget : StatelessWidget {
            public MediaQueryData MediaQuery { get; private set; }

            public override Widget Build(BuildContext context) {
                MediaQuery = context.MediaQuery;
                return new Text("Captured");
            }
        }

        private sealed class NavigationDuringBuildWidget : StatelessWidget {
            public override Widget Build(BuildContext context) {
                context.Navigator.Push(new Text("Invalid"));
                return new Text("Unreachable");
            }
        }

        private sealed class ParentWidget : Widget {
            internal override WidgetNode CreateNode() {
                return new ParentNode(this);
            }
        }

        private sealed class ParentNode : WidgetNode {
            public ParentNode(Widget widget)
                : base(widget) {
            }

            protected override VisualElement CreateElement(BuildContext context) {
                return new VisualElement();
            }

            protected override void OnMounted() {
                MountChild(new Text("Child"), Element);
            }
        }

        private sealed class ThemedActionWidget : StatelessWidget {
            public override Widget Build(BuildContext context) {
                return new Column(new Widget[]
                {
                new Text("Scoped"),
                new Button("Secondary", () => { }, variant: ButtonVariant.Secondary)
                });
            }
        }

        private sealed class ThirdPartyVisualElement : VisualElement {
            public string Marker { get; } = "third-party";
        }

        private static void SetFocused(FocusNode focused, params FocusNode[] others) {
            focused.SetFocused(true);
            foreach (var other in others) {
                other.SetFocused(false);
            }
        }

        private static void AssertFocusNodeDetaches(Widget widget, FocusNode focusNode) {
            var node = widget.CreateNode();
            node.Mount(parent: null, new BuildContext(), new VisualElement());
            Assert.That(focusNode.RequestFocus(), Is.True);
            node.Unmount();
            Assert.That(focusNode.RequestFocus(), Is.False);
        }

        private sealed class CounterWidget : StatefulWidget<CounterState> {
            public CounterState MountedState { get; set; }
        }

        private sealed class CounterState : WidgetState {
            private int _count;
            public int InitializationCount { get; private set; }
            public int DisposeCount { get; private set; }

            public CounterState() {
            }

            protected internal override void InitState() {
                InitializationCount++;
                ((CounterWidget)Widget).MountedState = this;
            }
            protected internal override void DidUpdateWidget(StatefulWidget oldWidget) => ((CounterWidget)Widget).MountedState = this;
            public override Widget Build(BuildContext context) => new Text(_count.ToString());
            protected internal override void Dispose() => DisposeCount++;
            public void Increment() => SetState(() => _count++);
        }

        private sealed class StatelessHostWidget : StatefulWidget<StatelessHostState> {
            public StatelessHostState MountedState { get; set; }
        }

        private sealed class StatelessHostState : WidgetState {
            private string _text = "Initial";

            protected internal override void InitState() => ((StatelessHostWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) => new ConfigurableLabel(_text);

            public void SetText(string text) => SetState(() => _text = text);
        }

        private sealed class ConfigurableLabel : StatelessWidget {
            private readonly string _text;

            public ConfigurableLabel(string text) => _text = text;

            public override Widget Build(BuildContext context) => new Text(_text);
        }

        private sealed class ThemeDependentProbe : StatelessWidget {
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context) {
                BuildCount++;
                return new Text("Theme dependent", context.Theme?.Typography.Body);
            }
        }

        private sealed class MediaQueryDependentProbe : StatelessWidget {
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context) {
                BuildCount++;
                return new Text(context.MediaQuery.Width.ToString("0"));
            }
        }

        private sealed class ContextIndependentProbe : StatelessWidget {
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context) {
                BuildCount++;
                return new Text("Independent", new TextStyle(Color.white));
            }
        }

        private sealed class AppStrings {
            public AppStrings(string greeting) => Greeting = greeting;
            public string Greeting { get; }
        }

        private sealed class LocalizationProbe : StatelessWidget {
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context) {
                BuildCount++;
                return new Text($"{Localizations.LocaleOf(context)}:{Localizations.Of<AppStrings>(context).Greeting}");
            }
        }

        private sealed class LocalizationHostWidget : StatefulWidget<LocalizationHostState> {
            public LocalizationHostState MountedState { get; set; }
        }

        private sealed class LocalizationHostState : WidgetState {
            private bool _russian;
            public LocalizationProbe Dependent { get; } = new();
            public ContextIndependentProbe Independent { get; } = new();
            public LocalizedCounterWidget Retained { get; } = new();

            protected internal override void InitState() =>
                ((LocalizationHostWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) => new Localizations(
                _russian ? new Locale("ru", "RU") : new Locale("en", "US"),
                new Column(new Widget[] { Dependent, Independent, Retained }),
                new AppStrings(_russian ? "Привет" : "Hello"));

            public void UseRussian() => SetState(() => _russian = true);
        }

        private sealed class LocalizedCounterWidget : StatefulWidget<LocalizedCounterState> {
            public LocalizedCounterState MountedState { get; set; }
        }

        private sealed class LocalizedCounterState : WidgetState {
            private int _count;
            public int InitializationCount { get; private set; }

            protected internal override void InitState() {
                InitializationCount++;
                ((LocalizedCounterWidget)Widget).MountedState = this;
            }

            public override Widget Build(BuildContext context) =>
                new Text($"{Localizations.Of<AppStrings>(context).Greeting}:{_count}");

            public void Increment() => SetState(() => _count++);
        }

        private sealed class TextScaleHostWidget : StatefulWidget<TextScaleHostState> {
            public TextScaleHostState MountedState { get; set; }
        }

        private sealed class TextScaleHostState : WidgetState {
            private bool _large;
            public CounterWidget Counter { get; } = new();

            protected internal override void InitState() =>
                ((TextScaleHostWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) => new TextScale(
                new TextScaler(_large ? 1.5f : 1f),
                Counter);

            public void Enlarge() => SetState(() => _large = true);
        }

        private sealed class LayoutWrapperHostWidget : StatefulWidget<LayoutWrapperHostState> {
            public LayoutWrapperHostState MountedState { get; set; }
        }

        private sealed class LayoutWrapperHostState : WidgetState {
            private bool _updated;

            public CounterWidget LatestCounter { get; private set; }

            protected internal override void InitState() =>
                ((LayoutWrapperHostWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) {
                LatestCounter = new CounterWidget();
                return new SizedBox(
                    new Padding(
                        new Align(
                            new Container(
                                new Opacity(LatestCounter, _updated ? 0.5f : 1f),
                                new BoxDecoration(backgroundColor: _updated ? Color.blue : Color.red)),
                            _updated ? Alignment.BottomRight : Alignment.TopLeft),
                        EdgeInsets.All(_updated ? 16f : 4f)),
                    width: _updated ? 240f : 120f,
                    height: 80f);
            }

            public void UpdateLayout() => SetState(() => _updated = true);
        }

        private sealed class RekeyedCounterHostWidget : StatefulWidget<RekeyedCounterHostState> {
            public RekeyedCounterHostState MountedState { get; set; }
        }

        private sealed class RekeyedCounterHostState : WidgetState {
            private int _generation;

            public CounterWidget LatestCounter { get; private set; }

            protected internal override void InitState() =>
                ((RekeyedCounterHostWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) {
                LatestCounter = new CounterWidget();
                return new Row(new[]
                {
                LatestCounter.WithKey(new WidgetKey($"counter-{_generation}"))
            });
            }

            public void ChangeKey() => SetState(() => _generation++);
        }

        private sealed class ReorderableCountersWidget : StatefulWidget<ReorderableCountersState> {
            public ReorderableCountersState MountedState { get; set; }
        }

        private sealed class ReorderableCountersState : WidgetState {
            private bool _reversed;

            public CounterWidget LatestFirst { get; private set; }
            public CounterWidget LatestSecond { get; private set; }

            protected internal override void InitState() => ((ReorderableCountersWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) {
                LatestFirst = new CounterWidget();
                LatestSecond = new CounterWidget();
                var first = LatestFirst.WithKey(new WidgetKey("first"));
                var second = LatestSecond.WithKey(new WidgetKey("second"));
                return new Row(_reversed
                    ? new Widget[] { second, first }
                    : new Widget[] { first, second });
            }

            public void Reverse() => SetState(() => _reversed = !_reversed);
        }

        private sealed class FailingRowUpdateWidget : StatefulWidget<FailingRowUpdateState> {
            public FailingRowUpdateWidget(Action cleanup) => Cleanup = cleanup;

            public Action Cleanup { get; }
            public FailingRowUpdateState MountedState { get; set; }
        }

        private sealed class FailingRowUpdateState : WidgetState {
            private bool _shouldFail;

            protected internal override void InitState() => ((FailingRowUpdateWidget)Widget).MountedState = this;

            public override Widget Build(BuildContext context) {
                if (!_shouldFail) {
                    return new Row(new Widget[] { new Text("Stable") });
                }

                return new Row(new Widget[]
                {
                new Text("Updated before failure"),
                new CleanupWidget(((FailingRowUpdateWidget)Widget).Cleanup),
                new ThrowingWidget()
                });
            }

            public void TriggerFailure() => SetState(() => _shouldFail = true);
        }

        private sealed class FailingStatefulWidget : StatefulWidget<FailingState> {
        }

        private sealed class FailingState : WidgetState {
            private bool _shouldFail;

            public FailingState() {
            }

            public override Widget Build(BuildContext context) {
                if (_shouldFail) throw new InvalidOperationException("Build failed.");
                return new Text("Stable");
            }

            public void FailOnNextBuild() => SetState(() => _shouldFail = true);
        }
    }

}
