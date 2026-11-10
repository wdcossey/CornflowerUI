# TextBlock and the text styles: plan

[#26](https://github.com/wdcossey/CornflowerUI/issues/26). The 8.1 type ramp: the `TextBlock` styles of the
phone's `generic.xaml`, as Avalonia `ControlTheme`s under the same keys, with resources an extension pack can
override ([ADR 0004](../../decisions/0004-larger-screens-as-an-extension-pack.md)).

`TextBlock` has no template and no default style, on the phone or in Avalonia, so this plan has no parts or
visual states. What it has instead is 16 keyed styles, and the phone's line, which Avalonia's `TextBlock`
doesn't draw the same way.

## Scope

| 8.1 `generic.xaml` | Here |
|---|---|
| The 16 `TextBlock` styles: `BaseTextBlockStyle`, `HeaderTextBlockStyle`, `SubheaderTextBlockStyle`, `TitleTextBlockStyle`, `BodyTextBlockStyle`, `GroupHeaderTextBlockStyle`, `ControlHeaderTextBlockStyle`, `ControlContextualInfoTextBlockStyle`, `ListViewItemTextBlockStyle`, `ListViewItemSubheaderTextBlockStyle`, `ListViewItemContentTextBlockStyle`, `ListViewEmptyStaticTextBlockStyle`, `FlyoutPickerTitleTextBlockStyle`, `ComboBoxPlaceholderTextBlockStyle`, `MessageDialogTitleStyle`, `MessageDialogContentStyle` | All 16 |
| `BaseRichTextBlockStyle`, `BodyRichTextBlockStyle` | Out: Avalonia has no `RichTextBlock` in the free packages ([ROADMAP](../../ROADMAP.md)) |
| `ContentPresenter` and `ContentControl` styles: `FlyoutPickerListViewItemContentPresenterStyle`, `ControlHeaderContentPresenterStyle`, `PlaceholderContentPresenterStyle`, `HeaderContentPresenterStyle`, `PivotTitleContentControlStyle` | Later, with the controls whose templates use them (pickers, the `Header` of the form controls, ComboBox, Pivot) |
| A plain `TextBlock`, with no style | Avalonia's, unchanged. The phone's `TextBlock` has no default style either, and a library added to an app shouldn't change its default text (Avonic's ADR 0002) |

The 14 styles the reference app catalogues are captured; the two MessageDialog styles aren't (see
[Dependencies](#dependencies-and-captures)).

## Style classes or `ControlTheme`s

The roadmap's row says "the type ramp, as style classes and resources". It is `ControlTheme`s and resources,
for the style budget (`CLAUDE.md`, rule 8; [docs/performance](../../performance/README.md)):

| | A class per style (`TextBlock.Header`) | A `ControlTheme` per style (`Theme="{StaticResource HeaderTextBlockStyle}"`) |
|---|---|---|
| Global rules | 16 alternatives, every one reaching every `TextBlock`: the limits are 10 in all and 5 reaching a `TextBlock`, so both raised past three times | None: a `ControlTheme`'s setters cost only the `TextBlock`s that use it. The budget stays at 0, 0 and 0 |
| Cost to an app that uses none | 16 checks on every `TextBlock`, about 0.5 µs each at Avonic's 30 ns | Nothing |
| As the phone writes it | `Classes="Header"` | `Theme="{StaticResource HeaderTextBlockStyle}"` for `Style="{StaticResource HeaderTextBlockStyle}"`, the same key, with `BasedOn` the same as the phone's |

Avonic's answer to the same question was an opt-in file of classes ([Avonic #70](https://github.com/wdcossey/Avonic/issues/70),
its ADR 0002), because Ionic's typography is classes. The phone's is keyed styles, so a keyed `ControlTheme`
is the closer translation and costs nothing. An opt-in classes file could be added later on top, if wanted.

## The phone's line

Every style but two sets `LineHeight` with `LineStackingStrategy="BaselineToBaseline"`, which Avalonia doesn't
have. ADR 0003 left these styles to be measured here.

**On the phone** (the dumps' heights; the baselines read from the PNGs, to the pixel, 0.417 DIP):

- the first baseline is Segoe WP's ascent below the top, 2210/2048 = 1.0791 em, whatever `LineHeight` is:
  `BaseTextBlockStyle` and `BodyTextBlockStyle` (21.5 and 24 at 18.14) both have it at 19.583;
- one line is that plus Segoe WP's share of descent in a line, 514/2724 = 0.18869, times `LineHeight`.
  `BaseTextBlockStyle`: 19.575 + 4.057 = 23.632, read 23.631. All 12 captured BaselineToBaseline styles fit
  to within 0.006 DIP;
- each later baseline is `LineHeight` below the one before (WinRT's definition of BaselineToBaseline; no
  capture has two lines);
- the two `MaxHeight` styles (`ListViewItemSubheader`, `ListViewItemContent`, `LineHeight` 20 at 16) get the
  font's own line, 1.33 em, 21.28, as their `LineHeight` is shorter;
- descenders draw below the `TextBlock` where `LineHeight` leaves them no room: Header's "gjy" reach 75.417,
  2.5 DIP past its 72.899.

**In Avalonia** (`tests/Cornflower.Tests/Fonts/LineMetricTests.cs`): each line is `LineHeight` tall, with the
baseline Selawik's ascent (0.9897 em) below its top, plus half of any height past Selawik's line (1.2002 em).
A shorter `LineHeight` keeps the baseline at the ascent and cuts the line from below. And a `TextBlock` clips
to its bounds.

**So:** `LineHeight` is WinRT's, which gives the distance between baselines, and the difference is all above
the first line and below the last. `Padding` takes it: top = 1.0791 × `FontSize` − Avalonia's baseline,
bottom = Avalonia's baseline − (1 − 0.18869) × `LineHeight`. Either edge can be negative (`BodyTextBlockStyle`'s
bottom is −0.40, `ComboBoxPlaceholderTextBlockStyle`'s both are), which Avalonia's `TextBlock` takes from its
size. `ClipToBounds` is `False`, so descenders draw past the bottom as on the phone. The `MaxHeight` styles
follow ADR 0003: `LineHeight` = max(WinRT's, 1.33 × `FontSize`), with the baseline 0.025 em higher than the
phone's, a recorded deviation.

| Style | `FontSize` | `LineHeight` | Phone height | Ours, unrounded | Ours, laid out | `Padding` top, bottom | Phone "A" bottom | Ours |
|---|---|---|---|---|---|---|---|---|
| `BaseTextBlockStyle` | 18.14 | 21.5 | 23.631 | 23.632 | 23.750 | 1.621, 0.511 | 19.583 | 19.583 |
| `BodyTextBlockStyle` | 18.14 | 24 | 24.103 | 24.104 | 24.167 | 0.507, −0.403 | 19.583 | 19.583 |
| `TitleTextBlockStyle` | 18.14 | 21.5 | 23.631 | 23.632 | 23.750 | 1.621, 0.511 | 19.583 | 19.583 |
| `FlyoutPickerTitleTextBlockStyle` | 18.14 | 21.5 | 23.631 | 23.632 | 23.750 | 1.621, 0.511 | 19.583 | 19.583 |
| `HeaderTextBlockStyle` | 57.5 | 57.5 | 72.899 | 72.898 | 72.917 | 5.138, 10.260 | 62.083 | 62.083 |
| `SubheaderTextBlockStyle` | 30 | 33 | 38.601 | 38.600 | 38.750 | 2.681, 2.919 | 32.500 | 32.083 |
| `GroupHeaderTextBlockStyle` | 23.73 | 23 | 29.948 | 29.947 | 30.417 | 2.120, 4.827 | 25.833 | 25.417 |
| `ControlHeaderTextBlockStyle` | 16 | 17.5 | 20.567 | 20.568 | 20.417 | 1.430, 1.638 | 17.500 | 17.083 |
| `ControlContextualInfoTextBlockStyle` | 16 | 17.5 | 20.567 | 20.568 | 20.417 | 1.430, 1.638 | 17.500 | 17.083 |
| `ListViewItemTextBlockStyle` | 29.86 | 33.5 | 38.544 | 38.543 | 38.750 | 2.668, 2.375 | 32.500 | 32.083 |
| `ListViewEmptyStaticTextBlockStyle` | 29.86 | 33.5 | 38.544 | 38.543 | 38.750 | 2.668, 2.375 | 32.500 | 32.083 |
| `ListViewItemSubheaderTextBlockStyle` | 16 | 20, MaxHeight | 21.280 | 21.281 | 21.667 | 0, 0 | 17.083 | 17.083 |
| `ListViewItemContentTextBlockStyle` | 16 | 20, MaxHeight | 21.280 | 21.281 | 21.667 | 0, 0 | 17.083 | 17.083 |
| `ComboBoxPlaceholderTextBlockStyle` | 20.26 | 30 | 27.523 | 27.523 | 27.917 | −1.032, −1.445 | 21.667 | 22.083 |
| `MessageDialogTitleStyle` | 25.5 | 31.5 | not captured | 33.461 | 33.333 | 1.831, 0.130 | | |
| `MessageDialogContentStyle` | 18.14 | 21.5 | not captured | 23.632 | 23.750 | 1.621, 0.511 | | |

"Laid out" is `Bounds.Height`: Avalonia rounds each `Padding` edge to the nearest raw pixel and the text's
height up, where the phone rounds the whole to the nearest (ADR 0003). The "A" bottoms are within one raw pixel everywhere; where they
differ, the computed baselines are the phone's to 0.001 DIP and the difference is where each rasteriser puts
the edge pixel.

## How a style maps

| WinRT setter | Avalonia | Note |
|---|---|---|
| `FontFamily`, `FontSize`, `Foreground` | The same, by `DynamicResource` | A hook each. Brushes follow the variant |
| `FontWeight` | The same | `SemiLight` with the Semilight family draws Selawik Semilight, as Segoe WP SemiLight's one face |
| `LineHeight`, `BaselineToBaseline` | `LineHeight` by `DynamicResource`, and `PhoneTextLine.BaselineToBaseline`, which sets `Padding` | See above |
| `LineHeight`, `MaxHeight` | `PhoneTextLine.MaxHeightLineHeight` by `DynamicResource`, which sets `LineHeight` | ADR 0003 |
| `CharacterSpacing` | `PhoneTextLine.CharacterSpacing` by `DynamicResource`, which sets `LetterSpacing` = n × `FontSize` / 1000 | [Spike 5](../../spikes/README.md#5-the-unit-of-textblockletterspacing) |
| `TextWrapping="WrapWholeWords"` | `WrapWithOverflow` | Breaks only between words; a longer word runs past the line |
| `TextTrimming="Clip"` | `None` | Set in every style, as Avalonia's inherits and WinRT's doesn't |
| `TextLineBounds="Full"` | nothing | What Avalonia's line is |
| `OpticalMarginAlignment="TrimSideBearings"` | nothing | Avalonia has none. A known gap: see COMPARISON.md |
| `Margin`, `Opacity` (ComboBoxPlaceholder) | The same, by `DynamicResource` | |

`PhoneTextLine` (`src/Cornflower.Themes.Wp81/PhoneTextLine.cs`) is internal to the theme. It works each value
out from the `TextBlock`'s own font and sizes whenever they change, at style priority: a local `FontSize`
keeps the phone's line, and a value the app sets itself wins. It is a class handler, not a binding in each
style: bindings were measured at about 4 µs each per `TextBlock` (below).

## The hooks (ADR 0004)

Every value a style reads is a resource reached by `DynamicResource`, so a key defined in a window's
`Resources`, or the application's, wins over the theme's. A pack changes a size by its key: the WinRT keys in
`Tokens/Typography.axaml` (`TextStyleLargeFontSize` and the rest), and, for the values the phone's styles
wrote as literals, the new keys in `Tokens/TextStyles.axaml`, named for the style:

| Key | Value | Style |
|---|---|---|
| `HeaderTextBlockStyleFontSize`, `SubheaderTextBlockStyleFontSize`, `GroupHeaderTextBlockStyleFontSize` | 57.5, 30, 23.73 | Those styles |
| `<Style>LineHeight`, 13 keys | WinRT's `LineHeight` | Each style that sets one |
| `GroupHeaderTextBlockStyleCharacterSpacing`, `MessageDialogTitleStyleCharacterSpacing`, `FlyoutPickerTitleTextBlockStyleCharacterSpacing` | −3, 3, 15 | Those styles |

No style sets a width, in any unit. Because `Padding`, `LineHeight` and `LetterSpacing` are worked out from the
`TextBlock`'s own values, a pack that changes a font size keeps the phone's line at the new size.

**What the hooks cost.** Measured with `tests/Cornflower.Tests/Performance/TypeRampCostBenchmark.cs`, the
benchmark of [docs/performance](../../performance/README.md) applied to the type ramp: 5,000 `TextBlock`s at a
depth of 20, Release, headless on the desktop PC, µs per `TextBlock` (the PR has the final run):

| Style | No style | `StaticResource` | Brushes and fonts dynamic | `DynamicResource` | The theme |
|---|---|---|---|---|---|
| `BodyTextBlockStyle` | 12.6 | 18.7 | 30.3 | 31.5 | 33.8 |
| `GroupHeaderTextBlockStyle` | 13.3 | 22.4 | 32.4 | 33.5 | 39.1 |

The columns: a plain `TextBlock`; every value fixed, as `StaticResource` would fix it (with `PhoneTextLine`'s
values worked out once); only the brushes and font families dynamic; every resource dynamic, `PhoneTextLine`'s
values fixed; and the theme as shipped. Three earlier runs the same morning, before the theme's own load was
taken out of the timing, were within 2.6 µs of this one in every cell. Bindings in place of `PhoneTextLine`, measured first, made the theme 66.6 and 85.6.

Most of the cost of `DynamicResource` is the first one on an element: a style whose brushes and fonts are
dynamic, which the variant and an app's own font need whatever the hooks do, costs nearly what one with every
resource dynamic does. So the size hooks themselves add about 1 µs per styled `TextBlock` (1.2 and 1.1;
0.5 to 2.6 over the earlier runs).
`PhoneTextLine` adds 2.3 (Body) and 5.6 (GroupHeader, which also has letter spacing). A `TextBlock`
without a text style pays a property read or two in a class handler when its font changes.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/TextBlockTests.cs` unless named.

1. **Each style sets what the phone read**: size, family, weight, wrapping, trimming, letter spacing, line
   height (the dumps). `Each_Style_Sets_What_The_Phone_Read`, `Each_Style_Draws_The_Face_The_Phone_Drew`.
2. **Each style is as tall as on the phone** (the dumps' heights). `Each_Style_Is_As_Tall_As_On_The_Phone`.
3. **The first baseline is the phone's** (the PNGs, the line model above). `Each_Styles_First_Baseline_Is_The_Phones`,
   `Each_Style_Draws_Its_Baseline_Where_The_Phone_Does` (pixels).
4. **Later lines are `LineHeight` apart** (BaselineToBaseline). `Wrapped_Lines_Are_LineHeight_Apart`.
5. **Character spacing** is n × `FontSize` / 1000 (spike 5), and follows a local `FontSize`.
   `A_Local_FontSize_Keeps_The_Letter_Spacing_And_The_MaxHeight_Line`.
6. **WrapWholeWords breaks only between words.** `A_Word_Longer_Than_The_Line_Is_Not_Broken`.
7. **Descenders draw below a short line** (the Header PNG). `Descenders_Draw_Below_A_Short_Line_As_On_The_Phone`.
8. **Colours, dark and light** (the PNGs' ink and the dumps' `foreground`), following a change of variant.
   `Each_Style_Draws_In_The_Phones_Colour`, `The_Colour_Follows_A_Change_Of_Variant`.
9. **A local `FontSize` keeps the phone's line; a value the app sets wins; taking the style away takes its
   values away.** `A_Local_FontSize_Keeps_The_Phones_Line`, `A_Value_The_App_Sets_Wins_Over_The_Styles`,
   `Taking_The_Style_Away_Takes_Its_Values_Away`.
10. **Every hook is a `DynamicResource`, and a key in a wider window's `Resources` changes the style** (ADR
    0004). `Every_Resource_A_Style_Reads_Is_Reached_By_DynamicResource`,
    `A_Key_In_A_Wider_Windows_Resources_Changes_The_Style` (each of the 68 keys the 16 styles read, overridden in a
    1280-wide window's `Resources` and taken out again),
    `Every_Key_In_TextStyles_Is_Read_By_A_Style`, `A_Wider_Windows_Sizes_Leave_Another_Window_Alone`.
11. **No style sets a width, and text takes a wider window's width.**
    `No_Style_Sets_A_Width_And_Text_Takes_A_Wider_Windows_Width` (1280 wide).
12. **`Theme="{StaticResource …}"` works as the phone's `Style` does**, with the theme in `Application.Styles`.
    `TextStyleStaticResourceTests`.
13. **No global rule**: `StyleBudgetTests`, unchanged at 0.

## Visual states

None: `TextBlock` has no template.

## The ladder

Rung 1, the theme, for everything a setter can say. Rung 2, an attached behaviour, for BaselineToBaseline,
MaxHeight and `CharacterSpacing`: `PhoneTextLine`. It lives in the theme, not in `Cornflower.Controls`, because
its numbers are Segoe WP's, which is the era's visual opinion. Nothing is subclassed.

## Extensions

None public. `PhoneTextLine`'s attached properties are internal to the theme; an app sets a text style, not
them.

## Dependencies and captures

- Tokens ([#19](https://github.com/wdcossey/CornflowerUI/issues/19)) and fonts ([#18](https://github.com/wdcossey/CornflowerUI/issues/18)): merged.
- Captures: the 14 catalogued styles, dark and light, from refapp run `20261108-1443-dev11` on the reference
  profile, committed under [`ref/wp81/TextBlock/`](../../../ref/wp81/TextBlock/README.md).
- **Owed:** `MessageDialogTitleStyle` and `MessageDialogContentStyle` aren't in the reference app's catalogue.
  Their values here are `generic.xaml`'s and their heights the line model's. Two lines in `Catalog.cs` and a
  run would capture them; and a two-line specimen would confirm Behaviour 4 on the phone.

## Checklist

Generated on 2026-11-10 from the phone SDK's `Windows.winmd` (`Windows Phone Kits\8.1\References\CommonConfiguration\Neutral`)
and its design folder (`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's;
`[-]` is left out, with its reason. The rows from `FrameworkElement` down are every control's; the type ramp
leaves Avalonia's in place.

> Generated by `dotnet run --project tools/winrt-api -- checklist TextBlock <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.TextBlock`; Avalonia `Avalonia.Controls.TextBlock`.

### Properties (29)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `TextWrapping` | `TextWrapping` | `TextBlock.TextWrapping` | Set by each style: WinRT's `WrapWholeWords` is `WrapWithOverflow`, `NoWrap` is `NoWrap`. Set even where it is the base's, because Avalonia's inherits and WinRT's doesn't |
| [x] | `TextTrimming` | `TextTrimming` | `TextBlock.TextTrimming` | Set by each style: WinRT's `Clip` is `None`. Set because Avalonia's inherits |
| [x] | `TextAlignment` | `TextAlignment` | `TextBlock.TextAlignment` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Text` | `String` | `TextBlock.Text` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Foreground` | `Brush` | `TextBlock.Foreground` | Set by each style that sets it, by `DynamicResource` (`PhoneForegroundBrush`, `PhoneMidBrush`), so it follows the variant |
| [x] | `FontWeight` | `FontWeight` | `TextBlock.FontWeight` | Set by the styles that set it (`SemiBold`, `SemiLight`) |
| [x] | `LineStackingStrategy` | `LineStackingStrategy` | none | Reproduced by the styles, not declared: `PhoneTextLine` sets `Padding` (BaselineToBaseline) or `LineHeight` (MaxHeight) from the style. See Behaviours 3 and 4 |
| [x] | `FontStretch` | `FontStretch` | `TextBlock.FontStretch` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `FontSize` | `Double` | `TextBlock.FontSize` | Set by every style, by `DynamicResource`: a hook (ADR 0004) |
| [x] | `FontFamily` | `FontFamily` | `TextBlock.FontFamily` | Set by every style, by `DynamicResource` to the 8.1 family keys |
| [x] | `LineHeight` | `Double` | `TextBlock.LineHeight` | Set by every BaselineToBaseline style, by `DynamicResource` to `<Style>LineHeight`: a hook. The distance between baselines, as on the phone |
| [-] | `IsTextSelectionEnabled` | `Boolean` | none | Not declared: Avalonia's `SelectableTextBlock` is the selectable text. Not in the type ramp |
| [x] | `CharacterSpacing` | `Int32` | none | Reproduced by the styles, not declared: `PhoneTextLine.CharacterSpacing` sets `LetterSpacing` = n × `FontSize` / 1000. Behaviour 5 |
| [x] | `Padding` | `Thickness` | `TextBlock.Padding` | Set by `PhoneTextLine` for BaselineToBaseline, at style priority, so the app's own wins. Either edge can be negative |
| [x] | `FontStyle` | `FontStyle` | `TextBlock.FontStyle` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `SelectedText` (read-only) | `String` | none | Not declared: selection is `SelectableTextBlock`'s |
| [-] | `SelectionEnd` (read-only) (not a dependency property) | `TextPointer` | none | Not declared: `TextPointer` has no Avalonia equivalent; selection is `SelectableTextBlock`'s |
| [-] | `SelectionStart` (read-only) (not a dependency property) | `TextPointer` | none | Not declared: as `SelectionEnd` |
| [-] | `ContentEnd` (read-only) (not a dependency property) | `TextPointer` | none | Not declared: `TextPointer` has no Avalonia equivalent |
| [-] | `ContentStart` (read-only) (not a dependency property) | `TextPointer` | none | Not declared: as `ContentEnd` |
| [x] | `BaselineOffset` (read-only) (not a dependency property) | `Double` | `TextBlock.BaselineOffset` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Inlines` (read-only) (not a dependency property) | `InlineCollection` | `TextBlock.Inlines` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `IsTextScaleFactorEnabled` | `Boolean` | none | Not declared: the phone's text-size setting (Ease of Access) has no Avalonia equivalent. Recorded |
| [-] | `TextReadingOrder` | `TextReadingOrder` | none | Not declared: Avalonia detects direction from the text and `FlowDirection` |
| [x] | `TextLineBounds` | `TextLineBounds` | none | The styles all set `Full`, which is what Avalonia's line is. Not declared |
| [-] | `SelectionHighlightColor` | `SolidColorBrush` | none | Not declared: selection is `SelectableTextBlock`'s |
| [x] | `OpticalMarginAlignment` | `OpticalMarginAlignment` | none | Every style based on `BaseTextBlockStyle` sets `TrimSideBearings`; Avalonia has none. Measured as a known gap: 0 to 1.25 DIP at the left (COMPARISON.md) |
| [x] | `MaxLines` | `Int32` | `TextBlock.MaxLines` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `IsColorFontEnabled` | `Boolean` | none | Not declared: Selawik has no colour glyphs. Avalonia draws colour fonts where a font has them |

### Events (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContextMenuOpening` | `ContextMenuOpeningEventHandler` | none | Not declared: Avalonia's `ContextRequested` |
| [-] | `SelectionChanged` | `RoutedEventHandler` | none | Not declared: selection is `SelectableTextBlock`'s |

### Methods (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void SelectAll()` | none | Not declared: `SelectableTextBlock.SelectAll` |
| [-] | `void Select(TextPointer start, TextPointer end)` | none | Not declared: `SelectableTextBlock`'s selection |
| [x] | `Boolean Focus(FocusState value)` | `InputElement.Focus()` | Avalonia's stands; the type ramp doesn't touch it |

### Properties, from `FrameworkElement` (22)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Margin` | `Thickness` | `Layoutable.Margin` | `ComboBoxPlaceholderTextBlockStyle` sets it (`ComboBoxFlyoutListPlaceholderTextThemeMargin`, 0,2.5). No other style does |
| [-] | `Language` | `String` | none | Not declared: Avalonia has no per-element language for shaping |
| [x] | `HorizontalAlignment` | `HorizontalAlignment` | `Layoutable.HorizontalAlignment` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `FlowDirection` | `FlowDirection` | `Visual.FlowDirection` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `DataContext` | `Object` | `StyledElement.DataContext` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `VerticalAlignment` | `VerticalAlignment` | `Layoutable.VerticalAlignment` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `MaxWidth` | `Double` | `Layoutable.MaxWidth` | Not set by any style |
| [x] | `MaxHeight` | `Double` | `Layoutable.MaxHeight` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Height` | `Double` | `Layoutable.Height` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `MinHeight` | `Double` | `Layoutable.MinHeight` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Width` | `Double` | `Layoutable.Width` | No style sets a width, in any unit (ADR 0004). Tested in a 1280-wide window |
| [x] | `Tag` | `Object` | `Control.Tag` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Style` | `Style` | none | WinRT's `Style="{StaticResource BodyTextBlockStyle}"` is Avalonia's `Theme="{StaticResource BodyTextBlockStyle}"`: each 8.1 text style is a `ControlTheme` under its key |
| [x] | `Resources` (not a dependency property) | `ResourceDictionary` | `StyledElement.Resources` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Name` | `String` | `StyledElement.Name` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `MinWidth` | `Double` | `Layoutable.MinWidth` | Not set by any style |
| [-] | `ActualWidth` (read-only) | `Double` | none | Not declared: `Bounds.Width` |
| [-] | `BaseUri` (read-only) (not a dependency property) | `Uri` | none | Not declared: WinRT's XAML loading |
| [x] | `Parent` (read-only) (not a dependency property) | `DependencyObject` | `StyledElement.Parent` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `Triggers` (read-only) (not a dependency property) | `TriggerCollection` | none | Not declared: WinRT's event triggers; Avalonia has styles and animations |
| [-] | `ActualHeight` (read-only) | `Double` | none | Not declared: `Bounds.Height` |
| [-] | `RequestedTheme` | `ElementTheme` | none | Not declared: Avalonia's `ThemeVariantScope` does this. The brushes follow it, by `DynamicResource` |

### Events, from `FrameworkElement` (5)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutUpdated` | `EventHandler<Object>` | `Layoutable.LayoutUpdated` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Loaded` | `RoutedEventHandler` | `Control.Loaded` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `SizeChanged` | `SizeChangedEventHandler` | `Control.SizeChanged` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Unloaded` | `RoutedEventHandler` | `Control.Unloaded` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `DataContextChanged` | `TypedEventHandler<FrameworkElement, DataContextChangedEventArgs>` | `StyledElement.DataContextChanged` | Avalonia's stands; the type ramp doesn't touch it |

### Methods, from `FrameworkElement` (7)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `Object FindName(String name)` | none | Not declared: `INameScope.Find` |
| [-] | `void SetBinding(DependencyProperty dp, BindingBase binding)` | none | Not declared: `Bind` |
| [-] | `BindingExpression GetBindingExpression(DependencyProperty dp)` | none | Not declared: Avalonia's bindings |
| [x] | `Size MeasureOverride(Size availableSize)` (protected, overridable) | `TextBlock.MeasureOverride()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Size ArrangeOverride(Size finalSize)` (protected, overridable) | `TextBlock.ArrangeOverride()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `void OnApplyTemplate()` (protected, overridable) | none | Not declared: `TextBlock` has no template |
| [-] | `Boolean GoToElementStateCore(String stateName, Boolean useTransitions)` (protected, overridable) | none | Not declared: pseudo-classes |

### Properties, from `UIElement` (20)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `CompositeMode` | `ElementCompositeMode` | none | Not declared: a WinRT composition setting |
| [-] | `ManipulationMode` | `ManipulationModes` | none | Not declared: WinRT manipulations; Avalonia has gestures |
| [-] | `IsTapEnabled` | `Boolean` | none | Not declared: Avalonia's gestures have no per-element switch |
| [-] | `IsRightTapEnabled` | `Boolean` | none | Not declared: as `IsTapEnabled` |
| [-] | `IsHoldingEnabled` | `Boolean` | none | Not declared: `Gestures.IsHoldingEnabled` is Avalonia's attached equivalent |
| [x] | `IsHitTestVisible` | `Boolean` | `InputElement.IsHitTestVisible` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `IsDoubleTapEnabled` | `Boolean` | none | Not declared: as `IsTapEnabled` |
| [x] | `Clip` | `RectangleGeometry` | `Visual.Clip` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `CacheMode` | `CacheMode` | `Visual.CacheMode` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Opacity` | `Double` | `Visual.Opacity` | `ComboBoxPlaceholderTextBlockStyle` sets it, by `DynamicResource` (0.6 dark, 0.45 light) |
| [-] | `AllowDrop` | `Boolean` | none | Not declared: `DragDrop.AllowDrop` is Avalonia's attached equivalent |
| [-] | `Visibility` | `Visibility` | none | Not declared: `IsVisible` |
| [x] | `UseLayoutRounding` | `Boolean` | `Layoutable.UseLayoutRounding` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Transitions` | `TransitionCollection` | `Animatable.Transitions` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `RenderTransformOrigin` | `Point` | `Visual.RenderTransformOrigin` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `RenderTransform` | `Transform` | `Visual.RenderTransform` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `Projection` | `Projection` | none | Not declared: the tilt (#22) uses Avalonia's 3D transform |
| [x] | `DesiredSize` (read-only) (not a dependency property) | `Size` | `Layoutable.DesiredSize` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `PointerCaptures` (read-only) | `IReadOnlyList<Pointer>` | none | Not declared: Avalonia captures per pointer |
| [-] | `RenderSize` (read-only) (not a dependency property) | `Size` | none | Not declared: `Bounds.Size` |

### Events, from `UIElement` (25)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `DoubleTapped` | `DoubleTappedEventHandler` | `InputElement.DoubleTapped` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `DragEnter` | `DragEventHandler` | none | Not declared: `DragDrop.DragEnter`, attached |
| [-] | `DragLeave` | `DragEventHandler` | none | Not declared: `DragDrop.DragLeave`, attached |
| [-] | `DragOver` | `DragEventHandler` | none | Not declared: `DragDrop.DragOver`, attached |
| [-] | `Drop` | `DragEventHandler` | none | Not declared: `DragDrop.Drop`, attached |
| [x] | `GotFocus` | `RoutedEventHandler` | `InputElement.GotFocus` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Holding` | `HoldingEventHandler` | `InputElement.Holding` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `KeyDown` | `KeyEventHandler` | `InputElement.KeyDown` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `KeyUp` | `KeyEventHandler` | `InputElement.KeyUp` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `LostFocus` | `RoutedEventHandler` | `InputElement.LostFocus` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `ManipulationCompleted` | `ManipulationCompletedEventHandler` | none | Not declared: WinRT manipulations; Avalonia has gestures |
| [-] | `ManipulationDelta` | `ManipulationDeltaEventHandler` | none | Not declared: as `ManipulationCompleted` |
| [-] | `ManipulationInertiaStarting` | `ManipulationInertiaStartingEventHandler` | none | Not declared: as `ManipulationCompleted` |
| [-] | `ManipulationStarted` | `ManipulationStartedEventHandler` | none | Not declared: as `ManipulationCompleted` |
| [-] | `ManipulationStarting` | `ManipulationStartingEventHandler` | none | Not declared: as `ManipulationCompleted` |
| [-] | `PointerCanceled` | `PointerEventHandler` | none | Not declared: Avalonia raises `PointerCaptureLost` |
| [x] | `PointerCaptureLost` | `PointerEventHandler` | `InputElement.PointerCaptureLost` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerEntered` | `PointerEventHandler` | `InputElement.PointerEntered` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerExited` | `PointerEventHandler` | `InputElement.PointerExited` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerMoved` | `PointerEventHandler` | `InputElement.PointerMoved` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerPressed` | `PointerEventHandler` | `InputElement.PointerPressed` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerReleased` | `PointerEventHandler` | `InputElement.PointerReleased` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `PointerWheelChanged` | `PointerEventHandler` | `InputElement.PointerWheelChanged` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `RightTapped` | `RightTappedEventHandler` | `InputElement.RightTapped` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `Tapped` | `TappedEventHandler` | `InputElement.Tapped` | Avalonia's stands; the type ramp doesn't touch it |

### Methods, from `UIElement` (15)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void Measure(Size availableSize)` | `Layoutable.Measure()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void Arrange(Rect finalRect)` | `Layoutable.Arrange()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `Boolean CapturePointer(Pointer value)` | none | Not declared: `IPointer.Capture` |
| [-] | `void ReleasePointerCapture(Pointer value)` | none | Not declared: `IPointer.Capture(null)` |
| [-] | `void ReleasePointerCaptures()` | none | Not declared: as `ReleasePointerCapture` |
| [x] | `void AddHandler(RoutedEvent routedEvent, Object handler, Boolean handledEventsToo)` | `Interactive.AddHandler()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void RemoveHandler(RoutedEvent routedEvent, Object handler)` | `Interactive.RemoveHandler()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `GeneralTransform TransformToVisual(UIElement visual)` | none | Not declared: `TransformToVisual` is an extension method in Avalonia |
| [x] | `void InvalidateMeasure()` | `Layoutable.InvalidateMeasure()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void InvalidateArrange()` | `Layoutable.InvalidateArrange()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void UpdateLayout()` | `Layoutable.UpdateLayout()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `Boolean CancelDirectManipulations()` | none | Not declared: WinRT manipulations |
| [x] | `AutomationPeer OnCreateAutomationPeer()` (protected, overridable) | `TextBlock.OnCreateAutomationPeer()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `void OnDisconnectVisualChildren()` (protected, overridable) | none | Not declared: WinRT's lifecycle |
| [-] | `IEnumerable<IEnumerable<Point>> FindSubElementsForTouchTargeting(Point point, Rect boundingRect)` (protected, overridable) | none | Not declared: WinRT's touch targeting |

### Properties, from `DependencyObject` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Dispatcher` (read-only) (not a dependency property) | `CoreDispatcher` | `AvaloniaObject.Dispatcher` | Avalonia's stands; the type ramp doesn't touch it |

### Events, from `DependencyObject` (0)

_none_

### Methods, from `DependencyObject` (5)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object GetValue(DependencyProperty dp)` | `AvaloniaObject.GetValue()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void SetValue(DependencyProperty dp, Object value)` | `AvaloniaObject.SetValue()` | Avalonia's stands; the type ramp doesn't touch it |
| [x] | `void ClearValue(DependencyProperty dp)` | `AvaloniaObject.ClearValue()` | Avalonia's stands; the type ramp doesn't touch it |
| [-] | `Object ReadLocalValue(DependencyProperty dp)` | none | Not declared: `GetValue` and `IsSet` |
| [-] | `Object GetAnimationBaseValue(DependencyProperty dp)` | none | Not declared: `GetBaseValue` |

### Default style

_`generic.xaml` has no default style for `TextBlock`._

