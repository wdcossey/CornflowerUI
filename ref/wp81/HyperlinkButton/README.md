# HyperlinkButton, from the emulator

The reference app's captures of `new HyperlinkButton { Content = "hyperlink button" }`
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs), variant `default`), dark and light, at rest
(`base`) and in each state of its template's `CommonStates` group, forced with `VisualStateManager.GoToState` on a
fresh instance. Copied unchanged from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 100 view pixels (1080 × 240 raw), the page's background; the HyperlinkButton in a slot 19 from the left and 12 from the top, so at 19.1667, 32.5 once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__HyperlinkButton__default__<state>.png`. The template-tree dumps beside
them stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers
read from them are quoted in [`docs/controls/HyperlinkButton/PLAN.md`](../../../docs/controls/HyperlinkButton/PLAN.md)
and written out in `tests/Cornflower.Tests/Controls/HyperlinkButtonReadings.cs`.

`CommonStates.Pressed` is the state forced with no finger: its `PointerDownThemeAnimation` ran with no press point and
left the template's `ContentPresenter` (not the whole control, as on a Button) tilted by −17.19°, 17.19°, which no
touch gives. Its colours are the pressed state's: the text keeps its colour. `CommonStates.Normal` and
`CommonStates.PointerOver` are pixel for pixel the same as `base`. Run `20261108-1732-dev11` has the same PNGs byte
for byte, but for `Pressed`, whose PNG differs while its dump reads the same tilt.
