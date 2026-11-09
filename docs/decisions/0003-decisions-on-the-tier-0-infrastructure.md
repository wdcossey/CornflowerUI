# ADR 0003 — Decisions on the Tier 0 infrastructure

**Date:** 2026-11-08 · **Status:** Accepted

## Context

Four Tier 0 PRs each ended with questions for the maintainer:
[#44](https://github.com/wdcossey/CornflowerUI/pull/44), the test harness
([#11](https://github.com/wdcossey/CornflowerUI/issues/11));
[#45](https://github.com/wdcossey/CornflowerUI/pull/45), the tokens
([#19](https://github.com/wdcossey/CornflowerUI/issues/19));
[#46](https://github.com/wdcossey/CornflowerUI/pull/46), the fonts
([#18](https://github.com/wdcossey/CornflowerUI/issues/18)); and
[#47](https://github.com/wdcossey/CornflowerUI/pull/47), the reference app's popups
([#23](https://github.com/wdcossey/CornflowerUI/issues/23)). The maintainer answered all twelve on
2026-11-08 and took the recommendation each time. This records those answers and the measurements
behind them, so later work can rely on them. Two were carried out the same day:
[#48](https://github.com/wdcossey/CornflowerUI/pull/48) added the token scripts and
[#49](https://github.com/wdcossey/CornflowerUI/pull/49) added the popup screenshots. The docs PR
that adds this ADR also changes the licence expression.

## Decision

### Layout and time in tests (#44)

| Question | Decision | Why |
|---|---|---|
| Avalonia rounds a measured size **up** to the raw pixel grid. The phone rounds to the **nearest** | **A recorded deviation that Avalonia forces** ([ADR 0001](0001-what-faithful-means.md), item 5). More readings from device 11 are wanted, from a size whose raw pixels have a fraction under a half | On device 10 (scale 1.8), the phone's 109-wide Button is 108.89 wide: 196.2 raw pixels rounded to 196. Avalonia makes it 109.44, rounding up to 197. On the reference profile, a size whose raw pixels have a fraction under a half comes out one raw pixel (0.42 view px) wider than the phone's. Turning layout rounding off would cost crisp edges everywhere. So far there is one reading on the phone's side |
| Avalonia's `Transitions` run on real time, so `TestClock` can't drive them | **Theme transitions are tested at their end value** (`Transitions = []`). A motion that has to match the phone frame by frame, such as the tilt, is a **frame loop on `CornflowerClock`**, and its tests read it at any millisecond | `IClock`, `Clock` and `Animatable.Clock` are all internal in Avalonia 12, and `Avalonias_Animation_Clock_Cannot_Be_Replaced` holds that fact. Writing every state transition as a frame loop would rebuild Avalonia's transitions for no gain where only the end state is compared |
| `Scene`'s default variant | **Dark**, unless the test asks for `ThemeVariant.Light` | The phone's default theme is dark |
| A frame under `TestClock` | **16 ms**, as in Avonic, not 16.67 | A timeline reads `Now`, so the interval decides only when a frame lands, not what value it shows |

### Tokens (#45)

| Question | Decision | Why |
|---|---|---|
| Keys for the 20 named accents | **`CornflowerAccent<Name>Color`**, such as `CornflowerAccentCobaltColor` | WinRT has no keys for them. The prefix marks them as Cornflower's own, so they never shadow a WinRT name |
| `Transparent` under rule 5 (no literal colours outside `Tokens/`) | **Allowed.** `LiteralColourGuardTests` lets it through | The 8.1 templates use it to make an area hit-testable. It carries no colour, so a token for it, such as `PhoneTransparentBrush`, would be a key WinRT never had |
| The token generator and cross-check scripts | **Committed** to `tools/tokens/` ([#48](https://github.com/wdcossey/CornflowerUI/pull/48)) | So a new capture run can be checked against the tokens again. Like the other tools, they read Microsoft's files from outside the repo when they run, and write only Cornflower's |
| Should the theme force Dark? | **No. The sample apps request Dark, and the theme follows the app's `ThemeVariant`** | An app chooses its own variant, as with any Avalonia theme. The phone's default is dark, so the samples start there |

### Fonts (#46)

| Question | Decision | Why |
|---|---|---|
| Where Selawik Semilight lives | **In a folder of its own, `Fonts/SelawikSemilight/`, unpatched** | Avalonia's `Typeface.Normalize` turns a weight word in a family name into the request: "Selawik Semilight" becomes "Selawik" at 350. One `avares://` folder is one font collection, with one face per weight, and Semilight's file reports 300, the same as Light. In one folder with Light, one of the two keys draws the wrong file. Patching `usWeightClass` would make a Modified Version under the OFL, which couldn't be called Selawik. The cost is that `PhoneFontFamilyNormal` with `FontWeight="SemiLight"` draws Light. No 8.1 template uses that combination |
| Line height: Selawik's line is 1.200 em (`hhea`), the phone's Segoe WP line is 1.330 em | **`LineHeight` = 1.33 × `FontSize`, set per style** in the text styles and templates where WinRT sets none. Text then sits **0.025 em higher than on the phone**: 0.45 DIP at 18.14, 1.4 DIP at 57. That offset is a recorded deviation | This gives the phone's line exactly: 24.1262 at 18.14, against the phone's 24.1267. Avalonia splits the extra leading evenly, so the baseline lands at 1.0547 em, where the phone's is 1.0796 em. Setting it per style, and not as a global `TextBlock` rule, keeps it out of the style budget. The alternative was rewriting Selawik's `hhea` to its win metrics, which would also need a new font name under the OFL |
| The theme package's licence expression | **`Apache-2.0 AND OFL-1.1`**, set in `Cornflower.Themes.Wp81.csproj`. The other packages keep `Apache-2.0` from `Directory.Build.props` | The package carries Selawik's OFL-1.1 fonts, and only this package does. The legal opinion that [ADR 0002](0002-decisions-of-2026-11-08.md#reference-material) requires before the first NuGet release still stands |

### Reference captures (#47)

| Question | Decision | Why |
|---|---|---|
| How to get a popup's pixels | **The emulator's own Screenshot tool, driven in the VM session** (`run.ps1 -Grab`, [#49](https://github.com/wdcossey/CornflowerUI/pull/49)). The app bar uses the same method | On the phone, `RenderTargetBitmap` can't render a popup, and rendering the window leaves popups out. The emulator's tool saves the phone's own display at full resolution, 1080 × 1920 on the reference profile. Capturing the emulator window with `ffmpeg` would give scaled pixels |

## Consequences

- A control's plan lists each size where rounding up differs from the phone's rounding, with the
  measurement. The comparison marks it as a deliberate difference, not a gap.
  `Layout_Rounds_A_Size_Up_Where_The_Phone_Rounds_To_The_Nearest` holds the behaviour, so if an
  Avalonia upgrade changes the rounding, that test turns red.
- A `VisualTransition` maps to `Transitions` on the part, and its test checks the end value. A
  plan that needs a timeline checked by the millisecond says why, and writes the motion as a frame
  loop on `CornflowerClock`.
- `Wp81Theme` sets no `ThemeVariant`. The desktop and Android samples set `RequestedThemeVariant`
  to Dark.
- Every WinRT text style without a `LineHeight` gets 1.33 × its `FontSize` in its Cornflower
  style. Styles that set their own `LineHeight` with `BaselineToBaseline` are measured with the
  type ramp ([#26](https://github.com/wdcossey/CornflowerUI/issues/26)).
- `ProgressRingActiveBackgroundOpacity` keeps the SDK file's 0.8, unconfirmed: the capture run
  didn't apply the `Active` storyboard. The ring has to be captured again while it is running
  before the ProgressRing theme ([#28](https://github.com/wdcossey/CornflowerUI/issues/28)) relies
  on that value.
