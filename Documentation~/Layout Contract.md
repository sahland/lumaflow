# LumaFlow layout contract

LumaFlow maps declarative layout widgets to Unity UI Toolkit Flexbox. It does
not replace Yoga or add a second constraint solver.

## Flex

`Row` lays out direct children horizontally; `Column` lays them out vertically.
`gap` is applied to direct native child roots and never creates spacer widgets.
Use `mainAxisAlignment` and `crossAxisAlignment` for the corresponding Flexbox
alignment axes.

`Expanded` and `Flexible` must be immediate children of a flex parent.
`Expanded` and `Flexible(fit: FlexFit.Tight)` fill their proportional flex
allocation. `Flexible(fit: FlexFit.Loose)` receives a proportional allocation,
but its child remains intrinsic rather than being forced to fill it.

## Constraints and positioning

`SizedBox` supplies a finite explicit width and/or height and tightens that
axis for its child. `ConstrainedBox` supplies optional min/max bounds. `Align`
and `Center` position one child inside their wrapper. `Stack` is a relative
positioning context; use `Positioned` for explicit offsets.

## Scrolling and responsive composition

`ScrollView` owns one overflow boundary and one content child. A vertical
scroll content subtree is height-content-sized, so use `Expanded` there only
when another ancestor explicitly bounds height.

`LayoutBuilder` observes one resolved axis. Horizontal is the default for
width breakpoints; vertical requires an explicitly bounded height. It rebuilds
only when that observed dimension changes and reconciles a compatible returned
subtree in place.
