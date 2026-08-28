# Animations

LumaFlow provides local implicit animations. A widget owns its transition,
compatible updates retarget it from the value currently displayed, and
unmounting cancels its scheduler.

## Timing and interpolation

`AnimationSpec` combines a positive duration, a `Curve`, and an
`AnimationBehavior`. Built-in curves are `Curves.Linear`, `Ease`, `EaseIn`,
`EaseOut`, and `EaseInOut`; `Cubic` defines an additional cubic Bézier curve.

Typed immutable tweens currently cover `FloatTween`, `ColorTween`,
`EdgeInsetsTween`, and `BorderRadiusTween`. Calling `Transform(0)` returns the
begin value and `Transform(1)` returns the end value.

## TweenAnimationBuilder

`TweenAnimationBuilder<T>` is the general local implicit transition boundary:

```csharp
new TweenAnimationBuilder<float>(
    new FloatTween(0f, 1f),
    TimeSpan.FromMilliseconds(240),
    (opacity, stableChild) => new Opacity(stableChild!, opacity),
    curve: Curves.EaseOut,
    child: expensiveContent,
    onEnd: OnIntroComplete);
```

The first configuration starts at `Tween.Begin` and transitions to
`Tween.End`. A compatible update with a different end value starts from the
currently rendered value, not from the previous tween's begin value. The
optional `child` is passed back unchanged so builders can keep an expensive or
stateful subtree outside the value-dependent portion.

`onEnd` runs once after a non-empty transition reaches its target, including a
reduced-motion snap. Cancellation and unmount do not report completion. Updating
the widget while an animation is active uses the latest builder and callback.

## AnimatedOpacity

`AnimatedOpacity` remains the concise state-bound form:

```csharp
var visibility = new State<float>(1f);

new AnimatedOpacity(
    panel,
    visibility,
    TimeSpan.FromMilliseconds(180),
    curve: Curves.EaseInOut,
    onEnd: OnVisibilitySettled);
```

The initial native opacity equals the state's current value and is not replayed
as an entrance animation. Later state changes use the same driver and
retargeting rules as `TweenAnimationBuilder<T>`.

## Reduced motion

Reduced motion is a tree-scoped application policy:

```csharp
new MediaQuery(
    new MediaQueryData(width, height, disableAnimations: true),
    application);
```

`AnimationBehavior.Normal` snaps to the target and completes synchronously when
animations are disabled. `AnimationBehavior.Preserve` continues the configured
animation for cases where motion is semantically required. `LayoutBuilder`
changes local width and height without discarding this policy.

Unity UI Toolkit does not currently expose a portable OS reduced-motion flag,
so the application or platform adapter must supply `DisableAnimations`.

## Route fades and lifecycle

Route fades use the same curve, behavior, clock, and cancellation semantics:

```csharp
new Route(
    new WidgetKey("project-details"),
    details,
    RouteTransition.Fade(
        TimeSpan.FromMilliseconds(200),
        Curves.EaseOut));
```

Schedulers tick only while their UI Toolkit element is attached to a panel.
Every LumaFlow animation establishes its initial value immediately and cancels
its scheduled item on retarget, route deactivation, or unmount.

The current API does not include explicit controllers, animation graphs,
physics-based animation, shared-element transitions or general transforms.
