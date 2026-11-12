# TextBlock and the text styles

Status: done · [#26](https://github.com/wdcossey/CornflowerUI/issues/26)

(The device run passed on 2026-11-11 on the Pixel 8, a Release build over wireless adb: a cold start of
671 ms, and all 16 text styles drawn. `tools/android/run.ps1`; its frames are git-ignored.)

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
- **When the font or sizes change**, the `Padding`, `LineHeight` and `LetterSpacing` are worked out again at once;
  when a value goes (the style is taken away, a value cleared), from the dispatcher, before the next layout. A value
  worked out again changes twice, to the value below it and then to the new one, as only with its own value gone
  can `PhoneTextLine` see whether something else sets the property.
- **Dark and light.** The brushes follow the theme variant, as the phone's `ThemeResource`s do.
- **No global rule.** The styles add nothing to the style budget: a `ControlTheme`'s setters cost only the
  `TextBlock`s that use it.

## Resource keys: the hooks

Every size, line height, character spacing, font family, brush, margin and opacity a style sets is a resource
reached by `DynamicResource`, so a key defined in a window's or the application's `Resources` overrides it
([ADR 0004](../../decisions/0004-larger-screens-as-an-extension-pack.md)). The rest are written in the style, as the
phone's are: `FontWeight`, `TextWrapping` and `TextTrimming`, Cornflower's `ClipToBounds`, and
`PhoneTextLine.BaselineToBaseline`. An app changes those with a `ControlTheme` based on the style. The resources:

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
- **A `NoWrap` line past its slot isn't clipped**, where the phone clips it to the slot: the styles turn
  `ClipToBounds` off to draw descenders below a short line as the phone does, and Avalonia's clip is the bounds,
  both ways or neither, with no layout clip of its own. The container's own clip, `ClipToBounds` or `TextTrimming`
  on the `TextBlock` stop it. A clip that followed the bounds across and not down would be a geometry per
  `TextBlock`, made again on every resize, so it isn't done.
- **Rounding**: laid-out heights within −0.15 and +0.47 DIP of the phone's (ADR 0003).
- **MaxHeight styles' baseline** is 0.025 em high (ADR 0003).
- **The phone's text-size setting** (`IsTextScaleFactorEnabled`) has no equivalent.
- **Two lines or more are short or tall at the bottom** (B18, measured; the theme isn't changed). Refapp run
  `20261111-2351-dev11` captured every style with two lines and the wrapping ones with a wrapped sentence
  ([`ref/wp81/TextBlock/`](../../../ref/wp81/TextBlock/README.md); `python -I tools/refapp/lines.py <run>` prints
  the numbers). On the phone, as the theme assumes, the first baseline stays at 1.0791 em and each later one is
  `LineHeight` below the one before: the two lines' "A" are `LineHeight` apart in the PNGs to within 1.15 raw pixels
  (0.48 DIP, Base and Body; the phone places each line's ink on the pixel grid its own way), the first line's "A" ends within a raw
  pixel of where it ends alone, and the wrapped specimens' heights, two and three lines, fit
  1.0791 em + (n − 1) × `LineHeight` + the bottom below with n whole to 0.001. But the bottom isn't the theme's: one
  line ends 0.18869 × `LineHeight` below its baseline, as PLAN.md fitted, and two or more end Segoe WP's descent,
  514/2048 = 0.25098 em, below the last, in all 14 BaselineToBaseline styles to 0.002 DIP (Base: 4.552 where one
  line has 4.056; Header: 14.432 where one line has 10.850). `PhoneTextLine` keeps
  0.18869 × `LineHeight` whatever the count, so by its model (not rendered here) a block of two or more lines is
  0.18869 × `LineHeight` − 0.25098 em off the phone's height: 0.50 DIP short for Base, Title, FlyoutPickerTitle and
  MessageDialogContent, 0.02 for Body, 0.71 for ControlHeader and ControlContextualInfo, 1.17 for ListViewItem and
  ListViewEmptyStatic, 1.30 for Subheader, 1.62 for GroupHeader, 3.58 for Header, 0.46 for MessageDialogTitle,
  and 0.58 tall for ComboBoxPlaceholder. The two MaxHeight styles are n × 21.28 on the phone, as in the theme.
- **The MessageDialog styles, captured** as `TextBlock`s in the same run: `MessageDialogTitleStyle` 33.461 and
  `MessageDialogContentStyle` 23.631 tall, the line model's heights. The system's `MessageDialog` itself
  (`Windows.UI.Popups`, grabbed from the screen in runs `20261111-2357-dev11` and `-2358-dev11`, kept outside the
  repository as the shell draws it) doesn't draw with them as an app's `TextBlock` does: its glyphs are 1.077 to
  1.087 times as wide as the captures' and its lines 57 and 82 raw pixels apart (23.75 and 34.17 DIP, against 21.5
  and 31.5), and its text starts 50 raw pixels in, not 46. That fits the shell drawing at about 2.6 raw pixels a DIP
  rather than the app's 2.4, which isn't confirmed.
- **Fallback fonts and larger runs**: the `Padding` reads the `TextBlock`'s own face and size, so a first line
  whose fallback font (a script Selawik lacks) or a larger `Run` has a taller ascent sits lower than the phone's
  line by the difference.
- **Letter spacing on a `Run` of another size**: `LetterSpacing` is worked out from the `TextBlock`'s `FontSize`,
  in DIPs, where WinRT's `CharacterSpacing` is in ems of each run's own size. A `Run` of another `FontSize` in a
  spaced style (GroupHeader, FlyoutPickerTitle, MessageDialogTitle) is spaced for the `TextBlock`'s size.
- **No `LineHeight`**: a BaselineToBaseline style whose `LineHeight` is taken away (a local `NaN`) gets no
  `Padding`, so Avalonia's own line, as there is no line height for the phone's line to keep.
- **Stale until the dispatcher runs**: when a value goes (the style taken away, a value cleared), `PhoneTextLine`
  takes its own values back in a pass posted to the dispatcher, not at once, as acting inside Avalonia's unsetting
  can stop it (spike 6). Read in the same job, such as a `Measure` straight after `Theme = null`, they are the old
  style's. The pass is posted at `Normal`, above every priority layout and render run at, so the next layout sees it
  done.
- **A value worked out again passes through the value below**: `PhoneTextLine` takes its old value back before
  setting the new one, to see whether anything else sets the property, so for a moment the property has the value
  below it (often 0 or none). A transition on `Padding`, `LineHeight` or `LetterSpacing` starts again from there.
- **A local `LineHeight` on a MaxHeight style** is taken as it is, where the phone would still keep the font's
  line if it were shorter.

## The checklist

In [PLAN.md](PLAN.md#checklist): 79 rows done or kept as Avalonia's, 55 left out with their reasons.
