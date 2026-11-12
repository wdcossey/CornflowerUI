# PasswordBox: comparison

**The phone:** the reference app's captures, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, in
[`ref/wp81/PasswordBox/`](../../../ref/wp81/PasswordBox/README.md): `password` (`Header = "password box"`,
`Password = "secret"`, the reveal button on), 2026-11-08; `empty` (a placeholder), `no-header`, `reveal-no-header` and
`reveal-empty`, 2026-11-12. Each state forced: `Focused` draws no caret, and `Disabled` leaves the CheckBox enabled.
Segoe WP.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/PasswordBoxFrames.cs`, Avalonia 12.1.4 with Skia, on
the same stages (450 × 140 at 2.4, 110 with no header), each a `TextBox` with `Theme="{DynamicResource PasswordBox}"`,
the same values, and the `revealPasswordButton` class where the phone's has `IsPasswordRevealButtonEnabled`, in
[`frames/`](frames). `Focused` is real focus, the caret drawn transparent; `PointerOver` a mouse over it; `Disabled`
really disabled, so our CheckBox is too. Selawik.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
Sizes, positions and colours are compared by measurement, here and in the tests.

## Dark

| Specimen, state | Phone | Ours |
|---|---|---|
| `password`, `base` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__password__base.png) | ![](frames/dark__PasswordBox__password__base.png) |
| `password`, `PointerOver` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__password__CommonStates.PointerOver.png) | ![](frames/dark__PasswordBox__password__CommonStates.PointerOver.png) |
| `password`, `Focused` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__password__CommonStates.Focused.png) | ![](frames/dark__PasswordBox__password__CommonStates.Focused.png) |
| `password`, `Disabled` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__password__CommonStates.Disabled.png) | ![](frames/dark__PasswordBox__password__CommonStates.Disabled.png) |
| `password`, `ButtonCollapsed` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__password__ButtonStates.ButtonCollapsed.png) | ![](frames/dark__PasswordBox__password__ButtonStates.ButtonCollapsed.png) |
| `empty`, `base` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__empty__base.png) | ![](frames/dark__PasswordBox__empty__base.png) |
| `empty`, `Focused` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__empty__CommonStates.Focused.png) | ![](frames/dark__PasswordBox__empty__CommonStates.Focused.png) |
| `empty`, `Disabled` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__empty__CommonStates.Disabled.png) | ![](frames/dark__PasswordBox__empty__CommonStates.Disabled.png) |
| `no-header`, `base` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__no-header__base.png) | ![](frames/dark__PasswordBox__no-header__base.png) |
| `reveal-no-header`, `base` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__reveal-no-header__base.png) | ![](frames/dark__PasswordBox__reveal-no-header__base.png) |
| `reveal-empty`, `base` | ![](../../../ref/wp81/PasswordBox/dark__PasswordBox__reveal-empty__base.png) | ![](frames/dark__PasswordBox__reveal-empty__base.png) |

## Light

| Specimen, state | Phone | Ours |
|---|---|---|
| `password`, `base` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__password__base.png) | ![](frames/light__PasswordBox__password__base.png) |
| `password`, `PointerOver` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__password__CommonStates.PointerOver.png) | ![](frames/light__PasswordBox__password__CommonStates.PointerOver.png) |
| `password`, `Focused` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__password__CommonStates.Focused.png) | ![](frames/light__PasswordBox__password__CommonStates.Focused.png) |
| `password`, `Disabled` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__password__CommonStates.Disabled.png) | ![](frames/light__PasswordBox__password__CommonStates.Disabled.png) |
| `password`, `ButtonCollapsed` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__password__ButtonStates.ButtonCollapsed.png) | ![](frames/light__PasswordBox__password__ButtonStates.ButtonCollapsed.png) |
| `empty`, `base` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__empty__base.png) | ![](frames/light__PasswordBox__empty__base.png) |
| `empty`, `Focused` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__empty__CommonStates.Focused.png) | ![](frames/light__PasswordBox__empty__CommonStates.Focused.png) |
| `empty`, `Disabled` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__empty__CommonStates.Disabled.png) | ![](frames/light__PasswordBox__empty__CommonStates.Disabled.png) |
| `no-header`, `base` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__no-header__base.png) | ![](frames/light__PasswordBox__no-header__base.png) |
| `reveal-no-header`, `base` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__reveal-no-header__base.png) | ![](frames/light__PasswordBox__reveal-no-header__base.png) |
| `reveal-empty`, `base` | ![](../../../ref/wp81/PasswordBox/light__PasswordBox__reveal-empty__base.png) | ![](frames/light__PasswordBox__reveal-empty__base.png) |

`CommonStates.Normal` is `base`, and `ButtonStates.ButtonVisible` is `base` where the button is on, byte for byte, so
neither is shown.

## Measurements

In view pixels (DIP) unless raw pixels (px) are said. The phone's numbers are the dumps'; ours are `Bounds` and the
frames. The box's pixels are read down column 500 and along each edge, the CheckBox's down column 48.

| | Phone | Ours | |
|---|---|---|---|
| The box, no reveal button | 412.0833 × 65.8333; 39.1667 with no header | 411.6667 × the same | **match** but the width, ADR 0003's rounding, as the TextBox's |
| The box with it, on the 140 stage | 108.75; `BorderElement` 26.6667 down, 31.25 tall, px rows 116–190 | the same | **match**: the Grid's star row gets what the stage leaves, on both |
| With it and no header, on the 110 stage | 78.75; `BorderElement` 27.9167 | the same | **match** |
| `ContentElement` with it | at 29.1667, 34.1667 tall, past the border's bottom | the same | **match** (its row's top, as the phone's) |
| The CheckBox | at 0, 51.25, 167.9167 × 57.5; its square px rows 213–273, columns 46–106 | at 0, 51.25, 168.3333 × 57.5; rows 213–274, columns 46–107 | **match**, within the CheckBox's own rounding (#31: its 25.4167 box is 25.8333 here) |
| The CheckBox's text | "Show Password", 18.14, white (black light) | the same | **match** |
| The circles | px rows 153–174, columns 73–240 | rows 152–172, columns 73–241 (the fallback font's `●`, below) | **match** in size; 1.5 px higher, the TextBox's baseline (0.95 DIP, #36) |
| The edges | at rest 204 grey dark and light; under a pointer the box's own (255 dark, 191 light); focused the accent round white; disabled 115 or 191 | the same; at rest in light 203 | **match**, within one step a channel (Skia's blend, as the TextBox's) |
| Disabled, the CheckBox | enabled-looking (the state forced alone) | its Disabled look: the square 115 grey dark | **deliberate difference**: a disabled box disables its parts, in WinRT and Avalonia |

## Behaviours

| | Behaviour | Result | Measurement |
|---|---|---|---|
| 1 | The key gives a TextBox the phone's PasswordBox | **Match** of the intent | A plain TextBox keeps the TextBox's |
| 2 | The style's values, `PasswordChar` `●` | **Match** | Every setter, dark and light |
| 3 | No reveal button: the TextBox's sizes | **Match** | 65.8333, 39.1667 |
| 4 | The reveal button's layout | **Match** on the phone's stages | 108.75, 31.25, 51.25; 78.75 |
| 5 | Ticking "Show Password" shows the password | **Match** by the template (no capture of a shown password) | `RevealPassword`, both ways |
| 6 | Unticked when the button shows or hides | **Gap**, recorded | The CheckBox follows `RevealPassword` alone |
| 7 | Under a pointer, drawn whole | **Match** | 255 inside, dark |
| 8 | The placeholder at the box's size | **Match** by the template | 30 at 30 |
| 9 | Disabled, the CheckBox too | **Deliberate difference** from the forced capture | Above |

## Known gaps

- **Rounding** (ADR 0003): the box ends one raw pixel short of the phone's; the CheckBox's square one raw pixel larger
  (#31's).
- **The text's baseline**, and so the circles, sits 0.95 DIP above the phone's (#36's).
- **At rest in light**, 203 grey where the phone's is 204: Skia's blend.
- **The `ButtonStates`' unticking** isn't emulated: an app that takes the class away while the password shows leaves it
  shown.
- **Unmeasured**: a shown password (the template unticks a forced one; a probe with a real tap is owed), the caret, the
  selection and the keyboard.
- **The text is Selawik** (ADR 0001), and **Selawik has no `●`** (U+25CF; it has `•`, U+2022): Avalonia draws the
  circles from the platform's fallback font. Headless on Windows they are the phone's size within a pixel; another
  platform's fallback draws its own (Android's Roboto and Noto have one). Unmeasured on the Pixel 8 until the device run.

## The checklist

In [PLAN.md](PLAN.md#checklist): 60 rows done or mapped to Avalonia's TextBox, 3 left out with their reasons.
