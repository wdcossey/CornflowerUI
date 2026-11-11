# TextBlock: the text styles, from the emulator

The reference app's captures of the 14 `TextBlock` styles it catalogues
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)), dark and light, each at rest (a
`TextBlock` has no visual states). Copied unchanged from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 120 view pixels (1080 × 288 raw), the page's background; the `TextBlock` 19 from the left and 12 from the top |
| Text | "Agjy 0123 cornflower" |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__TextBlock__<style>__base.png`. The template-tree dumps beside them
stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers
read from them are quoted in [`docs/controls/TextBlock/PLAN.md`](../../../docs/controls/TextBlock/PLAN.md).

Not captured: `MessageDialogTitleStyle` and `MessageDialogContentStyle`, which the catalogue doesn't list.
