# Slider, from the emulator

The reference app's captures of one Slider ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)):
`value-30`, `new Slider { Header = "slider", Value = 30 }`. Dark and light, at rest (`base`) and in `CommonStates`
`Disabled`, each forced with `VisualStateManager.GoToState` on a fresh instance. Copied unchanged from refapp run
`20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 140 view pixels (1080 × 336 raw), the page's background; the Slider stretched in a slot 19 from the left and right and 12 from the top, so at 19.1667, 12.0833, 412.0833 × 127.9167 once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__Slider__<variant>__<state>.png`. The template-tree dumps beside them
stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read
from them are quoted in [`docs/controls/Slider/PLAN.md`](../../../docs/controls/Slider/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/SliderReadings.cs`.

The Slider's template has only two states, `Normal` and `Disabled`, so these are all of them: the run's
`CommonStates.Normal` capture is `base`, byte for byte, in each theme, and isn't copied. Run `20261108-1732-dev11`'s
Slider captures are byte for byte these.

Not captured: a vertical Slider, ticks, and a Slider with no header. The plan takes those from the template.
