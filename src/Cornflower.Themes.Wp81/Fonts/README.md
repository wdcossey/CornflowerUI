# Fonts

The Windows Phone 8.1 text font is Segoe WP. It is Microsoft's and never ships. The theme bundles
**Selawik** instead, Microsoft's open font, under the SIL Open Font License 1.1 ([`OFL.txt`](OFL.txt)).
That substitution is a recorded deviation ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md), item 3).

## What is here

Selawik release 1.01, unchanged, from
<https://github.com/microsoft/Selawik/releases/download/1.01/Selawik_Release.zip> (530,652 bytes,
SHA-256 `3f62c51e05e3b5a1e6241cf92a371f0be2ea1183aa87b30718bbd40832a8d423`). The zip holds no
licence text. `OFL.txt` is `LICENSE.txt` from the repository at tag `1.01` (commit `b65ff9c`).

| File | Family (name ID 1) | `usWeightClass` | '0' advance | SHA-256 |
|---|---|---|---|---|
| `Selawik/selawkl.ttf` | Selawik Light | 300 | 1055 | `8e19d073091a1e869b5b0d48b925e605c0a4c6ece7df7b22a364fb065314c4c5` |
| `SelawikSemilight/selawksl.ttf` | Selawik Semilight | **300** | 1080 | `8620960344f12093482cded984a1aafa5a57d24cbf7e9299da125ccf0e9d4102` |
| `Selawik/selawk.ttf` | Selawik | 400 | 1104 | `e9d98518d8ac2817782a9a382430463a2e0793ea68350b695bb727d9a830ee1c` |
| `Selawik/selawksb.ttf` | Selawik Semibold | 600 | 1153 | `0a9e9d0549a10f24bef9b3a29e06fe6e0b5c21e7a784c503b048a307841a7783` |
| `Selawik/selawkb.ttf` | Selawik | 700 | 1178 | `f0db5e174a90e0956ad7d2844bdca1d5e6da92ec65b2c04e57ba9b180668c904` |

The same hashes are in `MicrosoftFileGuard.AllowedFonts`
([`tests/Cornflower.Tests/Repository`](../../../tests/Cornflower.Tests/Repository/MicrosoftFileGuard.cs)),
which fails on any other font, or on other bytes at one of these paths. Advances are in font units, 2048
to the em. The release also has `.eot`, `.svg`, `.woff` and `.woff2` copies. They aren't bundled, since
Avalonia loads only `.ttf`, `.otf` and `.ttc`.

## The keys

[`Tokens/Fonts.axaml`](../Tokens/Fonts.axaml) holds the six `FontFamily` resources of the phone's
theme resources, under their WinRT names:

| Key | On the phone | Here |
|---|---|---|
| `PhoneFontFamilyNormal` | Segoe WP | `Fonts/Selawik#Selawik` |
| `PhoneFontFamilyLight` | Segoe WP Light | `Fonts/Selawik#Selawik Light` |
| `PhoneFontFamilySemiLight` | Segoe WP SemiLight | `Fonts/SelawikSemilight#Selawik Semilight` |
| `PhoneFontFamilySemiBold` | Segoe WP Semibold | `Fonts/Selawik#Selawik Semibold` |
| `ContentControlThemeFontFamily` | Segoe WP | `Fonts/Selawik#Selawik` |
| `PivotHeaderItemFontFamily` | Segoe WP SemiLight | `Fonts/SelawikSemilight#Selawik Semilight` |

There is no Bold key in 8.1. `PhoneFontFamilyNormal` with `FontWeight="Bold"` draws `selawkb.ttf`.

## Why Semilight has a folder of its own

Selawik Semilight's file reports `usWeightClass` 300, the same as Light. In Avalonia 12.1.4:

- `Typeface.Normalize` reads any word of a family name that parses as a weight, style or stretch
  as the request. "Selawik Semilight" becomes family "Selawik" at weight 350, and "Selawik Light"
  is Light whatever `FontWeight` says.
- One `avares://` folder is one font collection, read recursively. It registers each face under its
  typographic family name, "Selawik", by weight, and the first face loaded at a weight keeps that slot.

With all five files in one folder, "Selawik Semilight" falls back from 350 to the 300 slot, which
Light holds, and **draws Light**. Addressing Semilight by its own name, as ADR 0002 decided, works
only if Semilight is alone in its collection. So it is, and the font isn't patched.

The one thing this doesn't reach: `PhoneFontFamilyNormal` with `FontWeight="SemiLight"` draws
Light, where the phone would draw Segoe WP Semilight. No 8.1 template asks for that. They set
`SemiLight` only with `PhoneFontFamilySemiLight`.

Held by [`SemilightTests`](../../../tests/Cornflower.Tests/Fonts/SemilightTests.cs): each key draws
the file it names, proved by the width of ten zeros against the '0' advance read from that file,
and by pixels: Semilight's frame differs from Light's, with more ink than Light and less than Regular.

## Line height

Avalonia's line is the font's `hhea` line, unless `OS/2` sets `USE_TYPO_METRICS`. No Selawik file
does. So a line of Selawik is 1.2002 em (2027 + 431 units), with the baseline 0.9897 em down. Its
`OS/2` win line, 1.330 em, isn't used.

On the phone, a Segoe WP `TextBlock` with no `LineHeight` is **1.330 em** tall at every size
measured, and its baseline is **1.0796 em** down. Read by the reference app on the reference
profile on 2026-11-08 (`refapp/20261108-1443-dev11`, outside the repo):

| Dump | Font | `FontSize` | Height | ÷ `FontSize` |
|---|---|---|---|---|
| `dark__Button__default__base.json` | Segoe WP, 600 | 18.14 | 24.1267 | 1.3300 |
| `dark__ComboBox__closed__base.json` (header) | Segoe WP | 16 | 21.28 | 1.3300 |
| `dark__ToggleSwitch__no-header__base.json` | Segoe WP | 25.5 | 33.9167 | 1.3301 |
| `dark__HyperlinkButton__default__base.json` | Segoe WP SemiLight | 29.86 | 39.7167 | 1.3301 |
| `dark__Pivot__two-items__base.json` (header) | Segoe WP SemiLight | 57 | 75.8133 | 1.3301 |

The baseline is the bottom of the ink of "button" and "check box" in the Button and CheckBox
captures at 18.14, to within one pixel of the capture (0.023 em).

So Selawik's line is **9.8% short**: 21.77 against 24.13 at 18.14. `LineHeight = 1.33 × FontSize`
gives the phone's line. Avalonia splits the extra evenly above and below, which leaves the baseline
at 1.0547 em, **0.025 em above the phone's**: 0.45 DIP at 18.14, 1.4 DIP at 57.

Held by [`LineMetricTests`](../../../tests/Cornflower.Tests/Fonts/LineMetricTests.cs). Where the
WinRT styles set their own `LineHeight` (`BodyTextBlockStyle` and others, with
`LineStackingStrategy="BaselineToBaseline"`), the mapping is the type ramp's to measure.

## Letter spacing

`LetterSpacing` is in DIPs, so WinRT's `CharacterSpacing` n is `n × FontSize / 1000`. That is
[spike 5](../../../docs/spikes/README.md#5-the-unit-of-textblockletterspacing), not re-measured here.

## Other scripts

Selawik covers 348 code points, Latin-1 and Latin Extended-A. The Noto Sans fallback package,
`Cornflower.Fonts.Noto`, is decided ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md#fonts-and-colour))
and not built yet.
