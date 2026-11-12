# RadioButton, from the emulator

The reference app's captures of two RadioButtons ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)):
`checked`, `new RadioButton { Content = "radio button", IsChecked = true }`; and `unchecked`, the same with no
`IsChecked`. Dark and light, at rest (`base`) and in `CommonStates` `Pressed` and `Disabled`, each forced with
`VisualStateManager.GoToState` on a fresh instance, and the checked one with the pointer over it. Copied unchanged
from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 100 view pixels (1080 × 240 raw), the page's background; the RadioButton in a slot 19 from the left and 12 from the top, so at 19.1667, 12.0833 once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__RadioButton__<variant>__<state>.png`. The template-tree dumps beside
them stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers
read from them are quoted in [`docs/controls/RadioButton/PLAN.md`](../../../docs/controls/RadioButton/PLAN.md) and
written out in `tests/Cornflower.Tests/Controls/RadioButtonReadings.cs`.

The run has more of each variant than are copied here, and they add nothing: in each theme, every capture of a
variant at rest (`base`, `CommonStates.Normal`, `PointerOver`, `MouseOver`) is byte for byte the same PNG, and so is
each `CheckStates` capture with the variant at rest that matches it: `CheckStates.Checked` is `checked` at rest, and
`CheckStates.Unchecked` and `CheckStates.Indeterminate` are `unchecked` at rest. The template's `Indeterminate` state
sets nothing.

`CommonStates.Pressed` is the state forced with no finger on the RadioButton: its `PointerDownThemeAnimation` ran with
no press point and left the template's `Container` tilted, which no touch gives. The colours in it are the pressed
state's; the tilt isn't. Run `20261108-1732-dev11` caught the same state at another moment of that animation; its
other RadioButton captures are byte for byte these.
