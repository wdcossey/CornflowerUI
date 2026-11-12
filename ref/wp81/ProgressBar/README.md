# ProgressBar, from the emulator

The reference app's captures of the catalogue's two bars ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)):
`new ProgressBar { Value = 40 }` (variant `value-40`) and `new ProgressBar { IsIndeterminate = true }` (variant
`indeterminate`), dark and light, at rest (`base`) and in each state of its template's one group, `CommonStates`
(`Determinate`, `Updating`, `Indeterminate`, `Error`, `Paused`), forced with `VisualStateManager.GoToState` on a fresh
instance. Copied unchanged from refapp run `20261108-1732-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 60 view pixels (1080 × 144 raw), the page's background; the bar in a slot 19 from the left and 12 from the top, across which it stretches and down which it is centred: 412.08 × 3.33 at 19.17, 34.58 once laid out (46, 83; 989 × 8 raw) |
| Date | 2026-11-08, from 17:26 UTC |

The names are the reference app's: `<theme>__ProgressBar__<variant>__<state>.png`. The template-tree dumps beside them stay
outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from
them are quoted in [`docs/controls/ProgressBar/PLAN.md`](../../../docs/controls/ProgressBar/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/ProgressBarReadings.cs`.

What they show:

- **The 40% bar at rest**, and in `Determinate` and `Updating`, which draw the same: the indicator, 396 raw pixels, then
  the track, the accent at 0.16 over the page.
- **`Paused`**: the indicator at half opacity. **`Error`**: no indicator.
- **`Indeterminate`, forced on the 40% bar, is a moment of the running storyboard**: two dots, at 31/64 s into it (dark)
  and 29/64 s (light), fitted from the dots; no track. Each dump was taken a frame later than its picture.
- **The indeterminate bar at rest (`base`) shows nothing**: at its moment (9/64 or 10/64 s, from the dumps) the one dot
  shown is still left of the bar, and the bar clips it. Its forced states all show the track alone (its value is 0):
  forcing a state stopped its own storyboard, and the picture of `Indeterminate` was taken before that state's
  storyboard drew.
