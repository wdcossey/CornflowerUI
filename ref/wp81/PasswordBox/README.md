# PasswordBox, from the emulator

The reference app's captures of five PasswordBoxes, dark and light, each forced into a state with
`VisualStateManager.GoToState` on a fresh instance: a forced `Focused` draws no caret, and a forced `Disabled` leaves
the box's parts enabled (the RevealButton keeps its enabled look). Copied unchanged from two runs:

| Variant | The specimen | States copied | Run |
|---|---|---|---|
| `password` | `new PasswordBox { Header = "password box", Password = "secret", IsPasswordRevealButtonEnabled = true }` | `base`, `PointerOver`, `Focused`, `Disabled`, `ButtonCollapsed` | `20261108-1443-dev11` ([`Catalog.cs`](../../../tools/refapp/app/Catalog.cs)) |
| `empty` | `new PasswordBox { Header = "password box", PlaceholderText = "placeholder" }` | `base`, `Focused`, `Disabled` | `20261112-1429-dev11` |
| `no-header` | `new PasswordBox { Password = "secret" }` | `base` | `20261112-1429-dev11` |
| `reveal-no-header` | `new PasswordBox { Password = "secret", IsPasswordRevealButtonEnabled = true }` | `base` | `20261112-1429-dev11` |
| `reveal-empty` | `new PasswordBox { Header = "password box", IsPasswordRevealButtonEnabled = true }` | `base` | `20261112-1429-dev11` |

Run `20261112-1429-dev11` was made for #37 from a copy of `tools/refapp` with the four specimens above added to
`Catalog.cs`, and a fifth, `revealed`, which ticked the RevealButton once loaded; the template's `ButtonVisible` state
unticks it as the box loads, so its captures show the password hidden and aren't copied. The repository's
`Catalog.cs` has had the four since the batch-3 roll-up, so a full run makes them. The same run's `password` captures are the
2026-11-08 ones, byte for byte, as are run `20261108-1732-dev11`'s.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 140 view pixels (1080 × 336 raw; 110 with no header), the page's background; the PasswordBox stretched in a slot 19 from the left and right and 12 from the top, at 19.1667, 21.6667, 412.0833 wide once laid out. The stage leaves the box 109 (79 with no header) of height, which a box with the RevealButton fills |
| Dates | 2026-11-08, 14:37 to 14:42 UTC (`password`); 2026-11-12, 14:27 to 14:28 UTC (the rest) |

The names are the reference app's: `<theme>__PasswordBox__<variant>__<state>.png`. The template-tree dumps beside them
stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read
from them are quoted in [`docs/controls/PasswordBox/PLAN.md`](../../../docs/controls/PasswordBox/PLAN.md) and written
out in `tests/Cornflower.Tests/Controls/PasswordBoxReadings.cs`.

The template's states: `CommonStates` `Normal`, `PointerOver`, `Focused` and `Disabled`, and `ButtonStates`
`ButtonVisible` and `ButtonCollapsed`. The runs' `CommonStates.Normal` captures are `base`, byte for byte, and aren't
copied; nor is `ButtonVisible`, which is `base` where the button is on.
