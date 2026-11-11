# TextBlock and the text styles

Status: device run owed · [#26](https://github.com/wdcossey/CornflowerUI/issues/26)

(The loop is done but for the sample's run on the Android device, as the phone was locked on 2026-11-10. The
line becomes `Status: done` with that run.)

The Windows Phone 8.1 type ramp: the 16 `TextBlock` styles of the phone's `generic.xaml`, as `ControlTheme`s
under the same keys, in `src/Cornflower.Themes.Wp81/Controls/TextBlock.axaml`. The plan, with the measurements,
is [PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

Where an 8.1 page sets `Style`, an Avalonia one sets `Theme`, with the same key:

```xml
<!-- 8.1 -->
<TextBlock Style="{StaticResource HeaderTextBlockStyle}" Text="cornflower" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<TextBlock Theme="{StaticResource HeaderTextBlockStyle}" Text="cornflower" />
```

`{DynamicResource}` works too, where the theme isn't in `Application.Styles` when the XAML loads (the sample's
page uses it, so its tests can load it). A `TextBlock` with no `Theme` is Avalonia's own, as the phone's has no
default style.

| Key | Size | Family, weight | Line height | Colour | Wraps |
|---|---|---|---|---|---|
| `BaseTextBlockStyle` | `TextStyleLargeFontSize`, 18.14 | Normal | 21.5 | `PhoneForegroundBrush` | between words |
| `BodyTextBlockStyle` | 18.14 | Normal | 24 | foreground | between words |
| `TitleTextBlockStyle` | 18.14 | Normal, SemiBold | 21.5 | foreground | no |
| `FlyoutPickerTitleTextBlockStyle` | 18.14, spacing 15 | Normal, SemiBold | 21.5 | foreground | no |
| `HeaderTextBlockStyle` | 57.5 | SemiLight | 57.5 | foreground | no |
| `SubheaderTextBlockStyle` | 30 | SemiLight | 33 | foreground | between words |
| `GroupHeaderTextBlockStyle` | 23.73, spacing −3 | SemiLight | 23 | `PhoneMidBrush` | between words |
| `ControlHeaderTextBlockStyle` | `TextStyleMediumFontSize`, 16 | Normal | 17.5 | mid | between words |
| `ControlContextualInfoTextBlockStyle` | 16 | Normal | 17.5 | mid | between words |
| `ListViewItemTextBlockStyle` | `TextStyleExtraLargePlusFontSize`, 29.86 | SemiLight | 33.5 | foreground | no |
| `ListViewEmptyStaticTextBlockStyle` | 29.86 | SemiLight | 33.5 | mid | between words |
| `ListViewItemSubheaderTextBlockStyle` | 16 | SemiLight | the font's, 21.28 | mid | no |
| `ListViewItemContentTextBlockStyle` | 16 | Normal | the font's, 21.28 | foreground | no |
| `ComboBoxPlaceholderTextBlockStyle` | `ContentControlFontSize`, 20.26 | Normal | 30 | inherited, at 0.6 dark and 0.45 light | no |
| `MessageDialogTitleStyle` | `TextStyleExtraLargeFontSize`, 25.5, spacing 3 | Normal, SemiBold | 31.5 | foreground | between words |
| `MessageDialogContentStyle` | 18.14 | Normal | 21.5 | foreground | between words |

"Normal" and "SemiLight" are `PhoneFontFamilyNormal` and `PhoneFontFamilySemiLight`. A line height is the
distance between baselines, as on the phone.

## Behaviours

- **The phone's line.** The first baseline is where the phone puts it, 1.0791 em below the top, each later one a
  line height below the one before, and the height is the phone's, from a `Padding` the theme works out from the
  `TextBlock`'s own font and sizes. Descenders draw below a line too short for them, as on the phone.
- **A `TextBlock`'s own values.** A local `FontSize` keeps the phone's line and letter spacing at the new size. A
  `Padding`, `LineHeight` or `LetterSpacing` the app sets, on the `TextBlock`, in a style or in a `ControlTheme`
  based on a text style, wins over the style's.
- **Dark and light.** The brushes follow the theme variant, as the phone's `ThemeResource`s do.
- **No global rule.** The styles add nothing to the style budget: a `ControlTheme`'s setters cost only the
  `TextBlock`s that use it.

## Resource keys: the hooks

Every value a style reads is a resource reached by `DynamicResource`, so a key defined in a window's or the
application's `Resources` overrides it ([ADR 0004](../../decisions/0004-larger-screens-as-an-extension-pack.md)):

- the WinRT keys in `Tokens/Typography.axaml`: `TextStyleMediumFontSize`, `TextStyleLargeFontSize`,
  `ContentControlFontSize`, `TextStyleExtraLargeFontSize`, `TextStyleExtraLargePlusFontSize`;
- the font families in `Tokens/Fonts.axaml`, the brushes and ComboBoxPlaceholder's opacity in
  `Tokens/Colors.axaml`, its margin in `Tokens/Sizes.axaml`;
- for the values the phone's styles wrote as literals, Cornflower's keys in `Tokens/TextStyles.axaml`, named for
  the style: `HeaderTextBlockStyleFontSize`, `SubheaderTextBlockStyleFontSize`,
  `GroupHeaderTextBlockStyleFontSize`; `<Style>LineHeight` for each of the 13 styles that set a line height;
  `<Style>CharacterSpacing` for GroupHeader, MessageDialogTitle and FlyoutPickerTitle, in thousandths of an em.

No style sets a width. What the hooks cost is in [PLAN.md](PLAN.md#the-hooks-adr-0004): the `DynamicResource`s
that the variant needs anyway cost most of it, and the size hooks 1 to 2 µs more per styled `TextBlock`.

## The ladder

Rung 1 (theme) for the setters; rung 2 (attached behaviour) for the phone's line stacking and character
spacing: `PhoneTextLine`, internal to `Cornflower.Themes.Wp81`, as its numbers are Segoe WP's. No type is
substituted.

## Known gaps

- **Optical margin**: no `TrimSideBearings` in Avalonia. Header's "A" starts 1.25 DIP right of the phone's;
  most others 0 or 0.42.
- **A `NoWrap` line past its slot isn't clipped**, as the styles turn `ClipToBounds` off to draw descenders as the
  phone does. The container's own clip, `ClipToBounds` or `TextTrimming` on the `TextBlock` stop it.
- **Rounding**: laid-out heights within −0.15 and +0.47 DIP of the phone's (ADR 0003).
- **MaxHeight styles' baseline** is 0.025 em high (ADR 0003).
- **The phone's text-size setting** (`IsTextScaleFactorEnabled`) has no equivalent.
- **Not captured on the phone**: the two MessageDialog styles, and two-line text.
- **Not run on the Android device** yet: the phone was locked on 2026-11-10.
- **Fallback fonts and larger runs**: the `Padding` reads the `TextBlock`'s own face and size, so a first line
  whose fallback font (a script Selawik lacks) or a larger `Run` has a taller ascent sits lower than the phone's
  line by the difference.
- **A local `LineHeight` on a MaxHeight style** is taken as it is, where the phone would still keep the font's
  line if it were shorter.

## The checklist

In [PLAN.md](PLAN.md#checklist): 79 rows done or kept as Avalonia's, 55 left out with their reasons.
