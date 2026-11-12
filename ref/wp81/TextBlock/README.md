# TextBlock: the text styles, from the emulator

The reference app's captures of the 16 `TextBlock` styles it catalogues
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)), dark and light, each at rest (a
`TextBlock` has no visual states): each style with one line, with two, and, for the nine that wrap, with a sentence
wrapped by the stage's width.

| | One line, 14 styles | One line, the two MessageDialog styles; two lines; wrapped |
|---|---|---|
| Run | `20261108-1443-dev11` | `20261111-2351-dev11` (B18), which also captured the 14 one-line PNGs again, byte for byte the same |
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) | the same |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 | the same |
| Stage | 450 × 120 view pixels (1080 × 288 raw), the page's background; the `TextBlock` 19 from the left and 12 from the top | one line 450 × 120; two lines and wrapped 450 × 200 (1080 × 480 raw); the same slot |
| Text | "Agjy 0123 cornflower" (`Text`) | one line, as before; two lines, that line twice with a `LineBreak` between; wrapped, "Agjy 0123 cornflower, a sentence long enough to wrap onto the next line" |
| Date | 2026-11-08, 14:37 to 14:42 UTC | 2026-11-11, 23:50 to 23:51 UTC |

The names are the reference app's: `<theme>__TextBlock__<variant>__base.png`, where the variant is the style's key,
or the key and `-two-lines` or `-wrapped`. The template-tree dumps beside them stay outside the repository
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from them are quoted in
[`docs/controls/TextBlock/PLAN.md`](../../../docs/controls/TextBlock/PLAN.md) and, for two lines and more, in
[`docs/controls/TextBlock/README.md`](../../../docs/controls/TextBlock/README.md#known-gaps), which
`tools/refapp/lines.py` reproduces.

Not here: the system's `MessageDialog` on the screen (runs `20261111-2357-dev11` and `20261111-2358-dev11`, `-Popups
only -Grab`), which the shell draws, with its status bar; those grabs stay in the reference folder.
