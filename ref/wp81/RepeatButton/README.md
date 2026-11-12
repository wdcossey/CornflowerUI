# RepeatButton, from the emulator

The reference app's captures of `new RepeatButton { Content = "repeat button" }`
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs), variant `default`), dark and light, at rest
(`base`) and in each state of its template's `CommonStates` group, forced with `VisualStateManager.GoToState` on a fresh
instance. Copied unchanged from refapp run `20261108-1443-dev11` (run `20261108-1732-dev11` has the same PNGs, byte
for byte).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 100 view pixels (1080 × 240 raw), the page's background; the RepeatButton in a slot 19 from the left and 12 from the top, so at 19.1667, 27.5 once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__RepeatButton__default__<state>.png`. `CommonStates.Normal` and
`CommonStates.PointerOver` are pixel for pixel the same as `base`, so `Normal` isn't copied. `CommonStates.Pressed` is
flat: the template runs no theme animation, where Button's forced Pressed is tilted.

The template-tree dumps beside them stay outside the repository
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from them are quoted in
[`docs/controls/RepeatButton/PLAN.md`](../../../docs/controls/RepeatButton/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/ToggleButtonReadings.cs`. The timing (the defaults and every click under a held touch)
comes from a probe, run `20261112-0755-dev11`, not from these.
