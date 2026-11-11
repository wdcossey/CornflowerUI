# ProgressRing, from the emulator

The reference app's captures of `new ProgressRing { IsActive = true }` ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs),
variant `active`), dark and light, at rest (`base`) and in each state of its template's two groups, `ActiveStates` and
`SizeStates`, forced with `VisualStateManager.GoToState` on a fresh instance. Copied unchanged from refapp run
`20261108-1732-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 120 view pixels (1080 × 288 raw), the page's background; the ring in a slot 19 from the left and 12 from the top, which it stretches to: 412.08 × 107.92 at 19.17, 12.08 once laid out, so it is `Large` |
| Date | 2026-11-08, from 17:26 UTC |

The names are the reference app's: `<theme>__ProgressRing__active__<state>.png`. The template-tree dumps beside them stay
outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from
them are quoted in [`docs/controls/ProgressRing/PLAN.md`](../../../docs/controls/ProgressRing/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/ProgressRingReadings.cs`.

**They are moments of a running animation.** The ring is active, so its `Active` storyboard is running when each picture
is taken, and each picture shows wherever the dots were at that moment: one dot in `base` (0.126 s into the storyboard),
four in the two `SizeStates` pictures (about 0.61 s), none in the forced `ActiveStates.Active` (its storyboard had not
drawn yet) or in `ActiveStates.Inactive`. The comparison fits each picture's moment from its dots. The dump of each
was taken at another moment than its picture, so the two don't show the same dots.
