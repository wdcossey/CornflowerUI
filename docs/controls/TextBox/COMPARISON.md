# TextBox: comparison

**The phone:** the reference app's captures, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, in
[`ref/wp81/TextBox/`](../../../ref/wp81/TextBox/README.md): `empty` (`Header = "text box"`,
`PlaceholderText = "placeholder"`) and `text` (`Header = "text box"`, `Text = "Agjy 0123"`), 2026-11-08; `no-header`
(`Text = "Agjy 0123"`) and `wrap` (two wrapped lines), 2026-11-12. `Focused` is forced, so no caret. Segoe WP.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/TextBoxFrames.cs`, Avalonia 12.1.4 with Skia, on the
same stage: 450 × 140 at 2.4 (110 for `no-header`, 220 for `wrap`), the TextBox stretched in a slot 19 in, 12 down and
19 from the right, with the same values (the header by `cf:ControlHeader.Header`), in [`frames/`](frames). `Focused`
is real focus, its caret drawn transparent to match. Selawik.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
Sizes, positions and colours are compared by measurement, here and in the tests.

## Dark

| Specimen, state | Phone | Ours |
|---|---|---|
| `empty`, `base` | ![](../../../ref/wp81/TextBox/dark__TextBox__empty__base.png) | ![](frames/dark__TextBox__empty__base.png) |
| `empty`, `Focused` | ![](../../../ref/wp81/TextBox/dark__TextBox__empty__CommonStates.Focused.png) | ![](frames/dark__TextBox__empty__CommonStates.Focused.png) |
| `empty`, `Disabled` | ![](../../../ref/wp81/TextBox/dark__TextBox__empty__CommonStates.Disabled.png) | ![](frames/dark__TextBox__empty__CommonStates.Disabled.png) |
| `text`, `base` | ![](../../../ref/wp81/TextBox/dark__TextBox__text__base.png) | ![](frames/dark__TextBox__text__base.png) |
| `text`, `Focused` | ![](../../../ref/wp81/TextBox/dark__TextBox__text__CommonStates.Focused.png) | ![](frames/dark__TextBox__text__CommonStates.Focused.png) |
| `text`, `Disabled` | ![](../../../ref/wp81/TextBox/dark__TextBox__text__CommonStates.Disabled.png) | ![](frames/dark__TextBox__text__CommonStates.Disabled.png) |
| `no-header`, `base` | ![](../../../ref/wp81/TextBox/dark__TextBox__no-header__base.png) | ![](frames/dark__TextBox__no-header__base.png) |
| `no-header`, `Focused` | ![](../../../ref/wp81/TextBox/dark__TextBox__no-header__CommonStates.Focused.png) | ![](frames/dark__TextBox__no-header__CommonStates.Focused.png) |
| `no-header`, `Disabled` | ![](../../../ref/wp81/TextBox/dark__TextBox__no-header__CommonStates.Disabled.png) | ![](frames/dark__TextBox__no-header__CommonStates.Disabled.png) |
| `wrap`, `base` | ![](../../../ref/wp81/TextBox/dark__TextBox__wrap__base.png) | ![](frames/dark__TextBox__wrap__base.png) |
| `wrap`, `Focused` | ![](../../../ref/wp81/TextBox/dark__TextBox__wrap__CommonStates.Focused.png) | ![](frames/dark__TextBox__wrap__CommonStates.Focused.png) |
| `wrap`, `Disabled` | ![](../../../ref/wp81/TextBox/dark__TextBox__wrap__CommonStates.Disabled.png) | ![](frames/dark__TextBox__wrap__CommonStates.Disabled.png) |

## Light

| Specimen, state | Phone | Ours |
|---|---|---|
| `empty`, `base` | ![](../../../ref/wp81/TextBox/light__TextBox__empty__base.png) | ![](frames/light__TextBox__empty__base.png) |
| `empty`, `Focused` | ![](../../../ref/wp81/TextBox/light__TextBox__empty__CommonStates.Focused.png) | ![](frames/light__TextBox__empty__CommonStates.Focused.png) |
| `empty`, `Disabled` | ![](../../../ref/wp81/TextBox/light__TextBox__empty__CommonStates.Disabled.png) | ![](frames/light__TextBox__empty__CommonStates.Disabled.png) |
| `text`, `base` | ![](../../../ref/wp81/TextBox/light__TextBox__text__base.png) | ![](frames/light__TextBox__text__base.png) |
| `text`, `Focused` | ![](../../../ref/wp81/TextBox/light__TextBox__text__CommonStates.Focused.png) | ![](frames/light__TextBox__text__CommonStates.Focused.png) |
| `text`, `Disabled` | ![](../../../ref/wp81/TextBox/light__TextBox__text__CommonStates.Disabled.png) | ![](frames/light__TextBox__text__CommonStates.Disabled.png) |
| `no-header`, `base` | ![](../../../ref/wp81/TextBox/light__TextBox__no-header__base.png) | ![](frames/light__TextBox__no-header__base.png) |
| `no-header`, `Focused` | ![](../../../ref/wp81/TextBox/light__TextBox__no-header__CommonStates.Focused.png) | ![](frames/light__TextBox__no-header__CommonStates.Focused.png) |
| `no-header`, `Disabled` | ![](../../../ref/wp81/TextBox/light__TextBox__no-header__CommonStates.Disabled.png) | ![](frames/light__TextBox__no-header__CommonStates.Disabled.png) |
| `wrap`, `base` | ![](../../../ref/wp81/TextBox/light__TextBox__wrap__base.png) | ![](frames/light__TextBox__wrap__base.png) |
| `wrap`, `Focused` | ![](../../../ref/wp81/TextBox/light__TextBox__wrap__CommonStates.Focused.png) | ![](frames/light__TextBox__wrap__CommonStates.Focused.png) |
| `wrap`, `Disabled` | ![](../../../ref/wp81/TextBox/light__TextBox__wrap__CommonStates.Disabled.png) | ![](frames/light__TextBox__wrap__CommonStates.Disabled.png) |

`CommonStates.Normal` is byte for byte the phone's `base` in each theme, so it isn't shown. The template has no other
state.

## Measurements

In view pixels (DIP) unless raw pixels (px) are said. The phone's numbers are the dumps'; ours are `Bounds` and the
frames. The box's pixels are read down column 500 and along each edge; the text's baseline from the bottom edge of
the first "A" (the coverage of its last row).

| | Phone | Ours | |
|---|---|---|---|
| The TextBox on the stage | 19.1667, 21.6667, 412.0833 × 65.8333 | 19.1667, 21.6667, 411.6667 × 65.8333 | **deliberate difference**, ADR 0003: the slot's margins, 45.6 px each, round to 46 here, so the box is 988 px wide, not 989 |
| `BorderElement` | 0, 26.6667, 39.1667 tall; px rows 116–209, columns 46–1034 | the same; columns 46–1033 | **match**, the right edge 1 px short, as above |
| The border | 2.5, 6 px on every edge | the same | **match** |
| No header | the box 39.1667, `BorderElement` at its top | the same | **match** |
| Two wrapped lines | the text view 54.1667, the box 86.6667 | the same | **match** |
| The header | 21.6667 tall; its text 21.28; 4.8 above the box | the same | **match** (`PhoneHeaderLine`) |
| The header's baseline | px 93.64 | px 93.40 | **match**, 0.1 DIP high (ADR 0003's 0.025 em at 16 is 0.4) |
| The text view and the placeholder | at 9.5833, 30; the view 33.3333, the placeholder's text 26.9467 | the same; the placeholder's line 26.9458 (to the grid 27.0833) | **match** |
| The text's first baseline | px 177.23 | px 174.95 | **deliberate difference**: 0.95 DIP high. ADR 0003's 0.025 em is 0.51 at 20.26; the phone's text view draws its baseline 0.32 DIP below its TextBlock formula (176.47 px), so 0.44 more |
| The line pitch | 65.24 px, 27.18 | 65.00 px, 27.08 | **match**, a quarter of a raw pixel a line |
| At rest | 204 grey on every edge and inside, dark and light | 204 dark, 203 light | **match**, within one step a channel: Skia blends `#BFBFBF` at 0.8 over white to 203, the phone to 204 (203.8) |
| Focused | `#FF3E65FF` round `#FFFFFFFF` | the same | **match** |
| Disabled | 115 grey dark, 191 light, round the page | the same | **match** |
| The text's colour | `#FF000000`; disabled `#FF747474` dark, `#FFBFBFBF` light | the same | **match** |
| The placeholder's colour | `#FF909090` dark, `#40000000` light; disabled as the text | the same | **match** |
| The header's colour | `#99FFFFFF` dark, `#73000000` light; disabled `#73FFFFFF`, `#40000000` | the same | **match** |

## Behaviours

| | Behaviour | Result | Measurement |
|---|---|---|---|
| 1 | Templated, a `MaskedTextBox` too | **Match** | 39.1667 tall with no header |
| 2 | The style's values | **Match** | Every setter, dark and light |
| 3 | The bounds | **Match**, but the width (ADR 0003) | Above |
| 4 | No header, no room; an empty string a line | **Match** for null (measured); empty a line (decided) | 39.1667; 65.8333 |
| 5 | The header's style and line, an underscore kept | **Match** | 16, `PhoneMidBrush`, 21.28 |
| 6 | The phone's line | **Match** | 1.33 em at any size; two lines 54.1667 |
| 7 | At rest, focused, disabled | **Match** | The pixels above |
| 8 | The placeholder while empty, hidden while focused | **Match** | At 9.5833, 30, 20.26 whatever the box's size |
| 9 | No pointer-over look | **Match** | No pixel changes |
| 10 | Scrolling: clipped inside the padding, a touch drag pans | **Match** by the template (no capture of a scroll) | The padding's 7 stays the box's white |
| 11 | The selection in the accent | **Match** by the style (no capture of a selection) | `#FF3E65FF` behind the text |
| 12 | The caret, the selection's text colour, the keyboard | **Gap**, unmeasured: Avalonia's and the platform's | A 1-wide black caret |
| 13 | `MaxLines` | Avalonia's, counting the phone's lines; WinRT 8.1 has no `MaxLines` | Two lines show 53.33 of 53.89: Avalonia's measure leaves 0.56 of the last line's bottom outside |
| 14 | Simple's templates keep Simple's TextBox | **Match** of the intent (`HoleParts`) | Five templates |

## Known gaps

- **Rounding** (ADR 0003): stretched in the reference app's slot, the box ends one raw pixel short of the phone's.
- **The text's baseline** is 0.95 DIP above the phone's: ADR 0003's 0.025 em, and 0.44 DIP more, as the phone's
  text view sits a little lower than its own TextBlocks do. The header's is 0.1 DIP high.
- **At rest in light**, the box is 203 grey where the phone's is 204: Skia's blend.
- **`MaxLines`** leaves 0.56 DIP of the last line below the viewport (Avalonia's measure). The phone has no `MaxLines`.
- **Unmeasured**: the caret, the selection's text colour, the selection handles and the keyboard (no capture has
  them), and scrolling.
- **The text is Selawik** (ADR 0001).

## The checklist

In [PLAN.md](PLAN.md#checklist): 63 rows done or kept as Avalonia's, 7 left out with their reasons.
