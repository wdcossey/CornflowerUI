# TextBox, from the emulator

The reference app's captures of four TextBoxes, dark and light, at rest (`base`) and in `CommonStates` `Focused` and
`Disabled`, each forced with `VisualStateManager.GoToState` on a fresh instance, so `Focused` draws no caret and
opens no keyboard. Copied unchanged from two runs:

| Variant | The specimen | Run |
|---|---|---|
| `empty` | `new TextBox { Header = "text box", PlaceholderText = "placeholder" }` | `20261108-1443-dev11` ([`Catalog.cs`](../../../tools/refapp/app/Catalog.cs)) |
| `text` | `new TextBox { Header = "text box", Text = "Agjy 0123" }` | `20261108-1443-dev11` |
| `no-header` | `new TextBox { Text = "Agjy 0123" }` | `20261112-1319-dev11` |
| `wrap` | `new TextBox { Header = "text box", Text = "Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123", TextWrapping = TextWrapping.Wrap, AcceptsReturn = true }` | `20261112-1319-dev11` |

Run `20261112-1319-dev11` was made for #36 from a copy of `tools/refapp` with three specimens added to `Catalog.cs`:
`no-header` and `wrap` above, and `no-header-empty` (`PlaceholderText = "placeholder"`, no header), whose captures
aren't copied (they show what `no-header` and `empty` show). The repository's `Catalog.cs` doesn't have them yet
(the TextBox PR proposes them). The same run's `empty` and `text` captures are the 2026-11-08 ones, byte for byte, as
are run `20261108-1732-dev11`'s.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 140 view pixels (1080 × 336 raw; 110 for `no-header`, 220 for `wrap`), the page's background; the TextBox stretched in a slot 19 from the left and right and 12 from the top, so at 19.1667, 21.6667, 412.0833 wide once laid out (its own `Margin` is 9.5 above) |
| Dates | 2026-11-08, 14:37 to 14:42 UTC (`empty`, `text`); 2026-11-12, 13:17 to 13:18 UTC (`no-header`, `wrap`) |

The names are the reference app's: `<theme>__TextBox__<variant>__<state>.png`. The template-tree dumps beside them
stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read
from them are quoted in [`docs/controls/TextBox/PLAN.md`](../../../docs/controls/TextBox/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/TextBoxReadings.cs`.

The TextBox's template has three states, `Normal`, `Focused` and `Disabled`, and no `PointerOver`; the runs'
`CommonStates.Normal` captures are `base`, byte for byte, in each theme, and aren't copied.
