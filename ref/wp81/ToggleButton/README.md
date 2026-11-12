# ToggleButton, from the emulator

The reference app's captures of `new ToggleButton { Content = "toggle button" }`, variant `unchecked`, and the same with
`IsChecked = true`, variant `checked` ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)), dark and
light, at rest (`base`) and in states of its template's one group, `CommonStates`, forced with
`VisualStateManager.GoToState` on a fresh instance. Copied unchanged from refapp run `20261108-1443-dev11` (run
`20261108-1732-dev11` has the same PNGs, byte for byte).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 100 view pixels (1080 × 240 raw), the page's background; the ToggleButton in a slot 19 from the left and 12 from the top, so at 19.1667, 27.5 once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__ToggleButton__<variant>__<state>.png`. The run has all twelve states for
both variants, 52 PNGs; they come to four looks per theme, so only the ones the comparison uses are here. A forced
state draws the same whatever `IsChecked` is, so `checked__…` and `unchecked__…` of one state are the same PNG.

| Look | The run's PNGs that are it, byte for byte | Here |
|---|---|---|
| Unchecked | `unchecked__base`; `Normal`, `PointerOver`, `Pressed`, `CheckedPressed`, `Indeterminate`, `IndeterminatePointerOver`, `IndeterminatePressed` | `unchecked__base`, `unchecked__CommonStates.Pressed`, `checked__CommonStates.CheckedPressed`, `unchecked__CommonStates.Indeterminate` |
| Checked | `checked__base`; `Checked`, `CheckedPointerOver` | `checked__base`, `checked__CommonStates.CheckedPointerOver` |
| Disabled | `Disabled`, `IndeterminateDisabled` | `unchecked__CommonStates.Disabled`, `unchecked__CommonStates.IndeterminateDisabled` |
| CheckedDisabled | `CheckedDisabled` | `checked__CommonStates.CheckedDisabled` |

The template-tree dumps beside them stay outside the repository
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from them are quoted in
[`docs/controls/ToggleButton/PLAN.md`](../../../docs/controls/ToggleButton/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/ToggleButtonReadings.cs`. The template runs no theme animation, so no state is tilted.
