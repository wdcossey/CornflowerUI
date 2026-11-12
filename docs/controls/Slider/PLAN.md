# Slider: plan

[#34](https://github.com/wdcossey/CornflowerUI/issues/34). The 8.1 Slider, Avalonia's `Slider` themed as the
phone's: its default style and its horizontal and vertical templates as an Avalonia `ControlTheme`, the phone's
colours at rest and disabled, dark and light, the header by an attached property, and the whole area below the header
taking the finger, as the phone's does.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `Slider`,
`R:\cornflower-reference\flash-update1\xbf-json\slider.json`, and the values of its keys from `themeresources.json`
beside it. The runtime's templates and theme resources equal the SDK's design files ([CLAUDE.md](../../../CLAUDE.md),
"Reference material"), so the checklist, generated from the SDK, names the same keys.

The captures **check** the theme; they don't supply values. They are the reference app's, run `20261108-1443-dev11`
on the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/Slider/`](../../../ref/wp81/Slider/README.md): `new Slider { Header = "slider", Value = 30 }`, at rest and
disabled, dark and light. Run `20261108-1732-dev11`'s are byte for byte the same.

**What no capture has**: a vertical Slider, ticks, a Slider with no header, and any input. No probe has touched a
Slider (none of the refapp's runs). So the vertical template and the ticks are the template's, and the interaction is
WinUI's source (`dxaml/xcp/dxaml/lib/Slider_Partial.cpp` and `TickBar_Partial.cpp`, microsoft-ui-xaml), as
[PROCESS.md](../../PROCESS.md) allows for a guide, and said so wherever it is used.

## The phone's style

`slider.json`: one unkeyed style for `Slider`, eight setters and a template.

| Setter | Value | Resource value (`themeresources.json`) | Dump, dark / light |
|---|---|---|---|
| `Background` | `{ThemeResource SliderTrackBackgroundThemeBrush}` | `#FF1F1F1F` dark, `#FFDEDEDE` light | the same |
| `BorderBrush` | `{ThemeResource SliderBorderThemeBrush}` | `Transparent` | `#00FFFFFF` |
| `BorderThickness` | `{ThemeResource SliderBorderThemeThickness}` | `0` | |
| `Foreground` | `{ThemeResource SliderTrackDecreaseBackgroundThemeBrush}` | `SystemColorControlAccentColor`, Cobalt `#FF3E65FF` | `#FF3E65FF` |
| `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `Segoe WP` | `Segoe WP` |
| `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `12` | 12 |
| `ManipulationMode` | `None` | | |
| `IsThumbToolTipEnabled` | `False` | | |

### The template

`slider.json`, `template.tree`, in short:

```
Grid                                  Margin: TemplateBinding Padding; rows Auto, *
  Grid.Resources: SliderThumbStyle    Thumb: BorderThickness 0.8, SliderThumbBorderThemeBrush, SliderThumbBackgroundThemeBrush;
                                      template a Border of the three
  ContentPresenter "HeaderContentPresenter"
                                      HeaderContentPresenterStyle; Content Header, ContentTemplate HeaderTemplate
  Grid "SliderContainer"              Row 1; Background Transparent
    Grid "HorizontalTemplate"         columns Auto, Auto, *; rows 17.5, Auto, 30.5
      Rectangle "HorizontalTrackRect"   Row 1, ColumnSpan 3; Fill: TemplateBinding Background; Stroke SliderTrackBorderThemeBrush
      Rectangle "HorizontalDecreaseRect" Row 1, column 0; Fill: TemplateBinding Foreground (its Width set by WinRT's code)
      TickBar "TopTickBar"            Collapsed; Row 0; Height SliderOutsideTickBarThemeHeight (5); Margin -4.5,0;
                                      Fill SliderTickmarkOutsideBackgroundThemeBrush
      TickBar "HorizontalInlineTickBar" Collapsed; Row 1; Height SliderTrackThemeHeight (9); Margin -4.5,0;
                                      Fill SliderTickMarkInlineBackgroundThemeBrush
      TickBar "BottomTickBar"         as TopTickBar, Row 2
      Thumb "HorizontalThumb"         SliderThumbStyle; 9.5 × 19; VerticalAlignment Top; Margin 0,13,0,0; RowSpan 3; column 1
      Rectangle "HorizontalBorder"    RowSpan 3, ColumnSpan 3; Stroke: TemplateBinding BorderBrush;
                                      StrokeThickness: TemplateBinding BorderThickness
    Grid "VerticalTemplate"           Collapsed; rows *, Auto, Auto; columns 13.5, Auto, 13.5
      Rectangle "VerticalTrackRect"   column 1, RowSpan 3
      Rectangle "VerticalDecreaseRect" column 1, row 2
      TickBar "LeftTickBar"           Width 5, Right, Margin 0,0,1.5,0, column 0
      TickBar "VerticalInlineTickBar" Width 9, column 1
      TickBar "RightTickBar"          Width 5, Left, Margin 1.5,0,0,0, column 2
      Thumb "VerticalThumb"           9 × 9 (SliderTrackThemeHeight); row 1, column 1
      Rectangle "VerticalBorder"      column 1, RowSpan 3
```

`HeaderContentPresenterStyle` (`themeresources.json`): `FontFamily` `PhoneFontFamilyNormal`, `FontSize`
`TextStyleMediumFontSize` (16), `FontWeight` Normal, `Foreground` `PhoneMidBrush` (`#99FFFFFF` dark, `#73000000`
light).

The dump (`dark__Slider__value-30__base.json`), in the Slider: the header 412.0833 × 21.6667, its text 21.28 tall
(Segoe WP's 1.33 em of 16); `SliderContainer` at 21.6667, 106.25 tall (the Slider is stretched to 127.9167 by the
stage); `HorizontalTrackRect` at 39.1667, 412.0833 × 9.1667; `HorizontalDecreaseRect` 120.8333 wide (its `Width` set to
120.75, three tenths of the track less the thumb); `HorizontalThumb` at 120.8333, 34.5833, 9.5833 × 19.1667;
`HorizontalBorder` 57.0833 tall, `strokeThickness` 1. The inline tick bar is the one shown, with nothing drawn (no
`TickFrequency`): WinRT's default `TickPlacement` is `Inline`.

**The border is 1 wide whatever `BorderThickness` says.** The phone binds a `Thickness` to `StrokeThickness`, a
double; the binding fails and the stroke keeps its default, 1 (the dump: 1 under `SliderBorderThemeThickness` 0, as
`Tokens/Sizes.axaml` records). So ours sets 1.

And ours, in `src/Cornflower.Themes.Wp81/Controls/Slider.axaml`, written new. Avalonia's `Slider` looks for one
`PART_Track` (a `Track`, which lays out a decrease `RepeatButton`, the `Thumb` and an increase `RepeatButton` by the
value), so each orientation has its own template, set by `:horizontal` and `:vertical`, as Simple's are, where the
phone's has both and collapses one. Horizontal:

```
Grid "Root"                           Margin: TemplateBinding Padding; rows Auto, *
  ContentPresenter "HeaderContentPresenter"
                                      cf:ControlHeader.Header and HeaderTemplate; HeaderContentPresenterStyle's four
                                      values; the phone's line (PhoneContentLine)
  Grid "SliderContainer"              Row 1; Background Transparent
    Grid "HorizontalTemplate"         rows 17.5, Auto, 30.5, and * for the rest
      Rectangle "HorizontalTrackRect"   Row 1; Height SliderTrackThemeHeight; as the phone's
      TickBar "TopTickBar", "BottomTickBar"   as the phone's; shown by TickPlacement
      Track "PART_Track"              RowSpan 4: the whole container
        RepeatButton "PART_DecreaseButton"   Theme Wp81SliderHorizontalDecreaseTheme: transparent, with
                                      Rectangle "HorizontalDecreaseRect" 17.5 down, the track's height, in its Foreground
        RepeatButton "PART_IncreaseButton"   Theme Wp81SliderIncreaseTheme: transparent, nothing drawn
        Thumb "HorizontalThumb"       Width 9.5; Theme Wp81SliderHorizontalThumbTheme (SliderThumbStyle's values): the
                                      container's height, transparent, with Border "ThumbBorder" 19 tall, 13 down
      Rectangle "HorizontalBorder"    RowSpan 3; Stroke: TemplateBinding BorderBrush; StrokeThickness 1; no hits
```

Vertical, the same turned: columns 13.5, Auto, 13.5 and `*`; `VerticalTrackRect` 9 wide in column 1; the decrease
button's `VerticalDecreaseRect` 13.5 in and 9 wide; the thumb 9 tall (`SliderTrackThemeHeight`), its `ThumbBorder` 9
wide, 13.5 in; `LeftTickBar` and `RightTickBar` as the phone's; `VerticalBorder` in column 1.

**Where WinRT's code and Avalonia's `Track` differ.** WinRT's Slider sets `HorizontalDecreaseRect.Width` and moves the
thumb's column in code; Avalonia's `Track` arranges its three parts instead. So the decrease rect is drawn by the decrease
button, which the `Track` makes exactly as wide: (track − thumb) × (value − min) / range, as WinRT's code computes. At
30 that is 120.75, drawn 120.8333 on both.

**The hit area.** The phone's `SliderContainer` is transparent and WinRT's Slider handles the press on it
(`Slider_Partial.cpp`: `OnPointerPressed` on `m_tpSliderContainer`), so the whole area below the header takes the
finger, however tall the Slider is (the capture's is 106.25 tall below the header, of which the template's rows are
57). Avalonia's Slider moves the thumb only for a press on its decrease or increase button. So ours makes the `Track`
fill the container, its two buttons transparent through it, and its `Thumb` the container's height, drawing the
phone's 9.5 × 19 border inside: a press anywhere below the header lands on one of the three. That is why the fourth,
star row is there (the phone's grid leaves the rest empty, but its container still takes the hits).

**The parts' themes.** The track's `RepeatButton`s and the `Thumb` set `Theme` to the Slider's own `ControlTheme`s,
keyed `Wp81Slider…Theme` in the same file. Wp81's `RepeatButton` theme (#30) is keyed by type and has its 57.5 minimum
height, its border and the accent pressed; Simple's `Thumb` theme likewise. An explicit `Theme` stops both from
reaching the parts, and `HoleParts` never acts on them: it is set only by Wp81's own `Button`, `ToggleButton` and
`RepeatButton` themes, which these parts don't take. (They are not left to `HoleParts`' fallback to Simple's either:
Simple's track theme is the wrong look.)

### The visual states

`slider.json`, `visualStateGroups`: `CommonStates`, on the root `Grid`. **No transitions.** The refapp README:
"The Slider's template really has only Normal and Disabled."

| State | What it sets (`slider.json`) | Resource value, dark / light | Avalonia |
|---|---|---|---|
| `Normal` | nothing | | the plain setters |
| `Disabled` | `HorizontalBorder`/`VerticalBorder.Stroke` = `SliderDisabledBorderThemeBrush`; the decrease rects' `Fill` = `SliderTrackDecreaseDisabledBackgroundThemeBrush`; the track rects' `Fill` = `SliderTrackDisabledBackgroundThemeBrush` and `Stroke` = `SliderTrackDisabledBorderThemeBrush`; both thumbs' `Background` = `SliderThumbDisabledBackgroundThemeBrush` and `BorderBrush` = `SliderThumbDisabledBorderThemeBrush`; the outside tick bars' `Fill` = `SliderTickMarkOutsideDisabledForegroundThemeBrush`, the inline ones' `SliderTickMarkInlineDisabledForegroundThemeBrush` | Transparent; `#FF666666` / `#FFBDBDBD`; `#FF1F1F1F` / `#FFDEDEDE` both; `#FF999999` / `#FF808080` both; `#80FFFFFF` / `#80000000` | `^:disabled` on the Slider for the track, border and tick bars; `^:disabled` in the part themes for the decrease rect and the thumb (a part is `:disabled` when the Slider is) |

The header isn't named by `Disabled`: it keeps `PhoneMidBrush` (the dumps). The `Pressed`, `PointerOver` and focus
brushes in `themeresources.json` (`SliderThumbPressedBackgroundThemeBrush` and the rest) are used by no state of this
template, so nothing changes under a finger, over a pointer or with focus.

## Avalonia's Slider against the phone's

Avalonia 12.1.4's `Slider` is a `RangeBase` (read at the `12.1.4` tag: `Slider.cs`, `Track.cs`, `TickBar.cs`, and
Simple's `Slider.xaml`). It raises `:horizontal`, `:vertical`, `:pressed` and `:disabled`; a press on either track
button moves the thumb's middle to the finger (`MoveToPoint`) and starts a drag, and while dragging every move moves
the thumb's middle to the finger, clamped at the ends; a press on the `Thumb` starts a drag too. The arrow keys move by
`SmallChange`, Page Up and Down by `LargeChange`, Home and End to the ends. It has `IsSnapToTickEnabled`,
`TickFrequency`, `Ticks`, `TickPlacement` (`None`, `TopLeft`, `BottomRight`, `Outside`) and `IsDirectionReversed`.
WinRT's Slider (WinUI's source): a press on the container moves the thumb's middle to the finger
(`MoveThumbToPoint`) and captures the pointer, every move does the same until release; a press on the thumb drags it by
the finger's travel (`OnThumbDragDelta`); a value from input goes to the nearest `StepFrequency` (1 by default).
Measured with the theme against the captures:

| Difference | The phone | Avalonia's Slider | Rung |
|---|---|---|---|
| The style and template | Above | Simple's: a 4-wide track, a 12 dot, another hit area | 1, theme |
| The decrease rect | sized by WinRT's code | none; the `Track` sizes its decrease button | 1, the rect drawn by the decrease button |
| The hit area | the whole container below the header | the two track buttons and the thumb | 1, the `Track` and its parts fill the container |
| `Header`, `HeaderTemplate` | on the Slider | none | 2, `cf:ControlHeader.Header` and `HeaderTemplate`, attached (decided below) |
| The header's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine`, internal to the theme |
| A tap on the track | the thumb's middle to the finger, then follows it | the same | none: Avalonia's, tested under the theme |
| A drag from the thumb | the thumb moves by the finger's travel | the thumb's middle goes to the finger on the first move | 6, recorded: at most half the thumb, 4.79 across, 4.5 up and down |
| The value from input | to the nearest `StepFrequency`, 1 | wherever the finger is, unless `IsSnapToTickEnabled` | 6, recorded: `StepFrequency` missing (decided below) |
| `TickPlacement` | `Inline` by default, and `TopLeft`, `BottomRight`, `Outside` | `None` by default; no `Inline` | 1 for the outside bars; `Inline` recorded as missing. With no `TickFrequency` the phone draws no tick, so the default looks the same |
| The ticks | WinRT's `TickBar`: one raw pixel wide, the bar's height, every one | Avalonia's `TickBar`: 1 wide, the ends the bar's height and the rest three quarters | 6, recorded, not measured (no capture) |
| Focus | no `FocusStates` | a focus adorner | 1, the theme clears `FocusAdorner` |
| The thumb's tooltip | off (`IsThumbToolTipEnabled` False) | none | none |
| `ManipulationMode` None | WinRT's: no pan or zoom manipulation from the Slider | Avalonia has no manipulation mode; its gestures are recognisers on a `ScrollViewer` | not applicable |
| Rounding | the nearest raw pixel | up | ADR 0003's recorded deviation; the sizes below |

No type is substituted. Rungs 1, 2 and 6.

### Where rounding up differs from the phone's

ADR 0003 asks each plan to list them. At 2.4:

| Size | Template | Raw pixels | The phone | Ours | What shows |
|---|---|---|---|---|---|
| The stage's slot | 450 less 19 each side | 1080 − 45.6 − 45.6 | 989, 412.0833 | 988, 411.6667 (each margin rounded to 46) | The track ends one raw pixel sooner: 1033, not 1034 |
| The decrease rect | 120.75 | 289.8 | 290, 120.8333 | 290, 120.8333 | Nothing |
| The thumb | 9.5 × 19 | 22.8 × 45.6 | 23 × 46 | 23 × 46 | Nothing: on the phone's pixels |
| The track | 9 | 21.6 | 22 | 22 | Nothing |
| The 30.5 row | 30.5 | 73.2 | 73 | 74 | Nothing drawn; an unstretched Slider is 57.5 below its header, the phone's 57.0833 |
| The header | 21.28 | 51.07 | 52, 21.6667 | 52, 21.6667 | Nothing |

`SceneTests.Layout_Rounds_A_Size_Up_Where_The_Phone_Rounds_To_The_Nearest` holds the rounding.

## Other controls' templates

**Wp81's `RepeatButton` theme can't reach the track's buttons**: they set their own `Theme` (above), and a test holds
it with Wp81's `RepeatButton` theme present (merged in #93) and with an app's implicit `RepeatButton` and `Thumb`
themes added. No template of Simple's holds a plain `Slider` (GitHub's code search of Simple's and `Avalonia.Dialogs`'
sources finds only Simple's own `Slider.xaml`), so no fallback is needed for it.

## Extensions

**`Cornflower.Controls.ControlHeader`**, rung 2: two attached properties, `Header` (`object?`) and `HeaderTemplate`
(`IDataTemplate?`), WinRT's names. Data only; the Slider's template shows them. Not a WinRT or Avalonia type name. Any
later control that needs WinRT's header (ToggleSwitch, TextBox, ComboBox, the pickers) can show the same two.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/SliderTests.cs` unless named.

1. **The style's values**, dark and light: the six setters Avalonia has, the parts' brushes, the thumb's 0.8 border,
   the header's font, size, weight and foreground (`slider.json`, the keys' values, the dump).
   `The_Style_Sets_What_The_Phone_Read`.
2. **The bounds**: the Slider at 19.1667, 12.0833; the header 21.6667; the container and its 106.25; the track at
   39.1667, 9.1667 tall; the decrease rect 120.8333; the thumb at 120.8333, 34.5833, 9.5833 × 19.1667; the border
   57.0833 and 1 wide (the dump), with the rounding above. `Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding`.
3. **The pixels**: the track row and the track and thumb columns of the PNGs, dark and light.
   `The_Track_And_Thumb_Are_Drawn_Where_The_Phone_Draws_Them`.
4. **Disabled** dims the decrease rect and the thumb, keeps the track and the header (dumps and PNGs), and sets the
   track and border by key over an app's brushes. `Disabled_Dims_The_Decrease_Rect_And_The_Thumb`,
   `Disabled_Sets_The_Track_And_Border_Over_An_Apps_Brushes`, `Vertical_Disabled_Dims_The_Decrease_Rect_And_The_Thumb`.
5. **No other state**: the pointer over it, a press on the thumb and keyboard focus change no pixel.
   `The_Pointer_Over_It_And_A_Press_Change_Nothing`, `Keyboard_Focus_Draws_Nothing`.
6. **The header**: shown in the phone's line; empty, it takes no height; its template is used.
   `The_Header_Is_Shown_Above_The_Track`, `With_No_Header_The_Track_Is_At_The_Top`, `The_HeaderTemplate_Is_Used`.
7. **A tap moves the thumb's middle to the finger**, either side of the thumb and at the ends; **anywhere below the
   header**, under the track and below its rows; not on the header.
   `A_Tap_On_The_Track_Moves_The_Thumbs_Middle_To_The_Finger`, `A_Tap_Anywhere_Below_The_Header_Moves_The_Thumb`.
8. **A drag follows the finger**, off the Slider and past the ends, and stops at release; by touch too; from the thumb
   as well. `A_Drag_From_The_Track_Follows_The_Finger`, `A_Drag_From_The_Thumb_Follows_The_Finger`.
9. **The Slider's brushes reach the track**: `Foreground` the decrease rect, `Background` the track; the border is 1
   wide whatever `BorderThickness`; `Padding` insets the template. `The_Sliders_Brushes_Reach_The_Track`,
   `The_Border_Is_One_Wide_Whatever_BorderThickness_Says`, `Padding_Insets_The_Template`.
10. **Reversed**, the value fills from the right and a tap counts from it. `Reversed_The_Value_Fills_From_The_Right`.
11. **Vertical**: the template's columns, thumb and decrease rect, from the bottom; a tap in the side column moves it.
    `Vertical_It_Is_The_Phones_Vertical_Template`.
12. **Ticks**: each `TickPlacement` shows the phone's bars, at their sizes and places, bound to the Slider's range and
    frequency; they are drawn from end to end of the track. `TickPlacement_Shows_The_Phones_Tick_Bars`,
    `The_Ticks_Are_Drawn_From_End_To_End_Of_The_Track`.
13. **The parts keep the Slider's themes**, with Wp81's `RepeatButton` theme and an app's implicit ones present.
    `The_Tracks_Parts_Keep_The_Sliders_Themes`.
14. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
15. **Holes**: `Slider` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
16. **No global rule**: `StyleBudgetTests`, unchanged.
17. **The sample's page**: in the catalogue, every Slider on it the theme's, its header from its title, the first
    Slider's header shown and its value following it (`Sample/SliderPageTests.cs`).

## Decided on the user's behalf

- **WinRT's `Header` and `HeaderTemplate` are attached properties, `cf:ControlHeader.Header` and
  `cf:ControlHeader.HeaderTemplate`**, in `Cornflower.Controls` (rung 2, as ARCHITECTURE.md names `Header` and
  ROADMAP.md plans for ToggleSwitch), rather than recorded as missing: the phone's capture has a header, and a page
  without one can't be written as an 8.1 page writes it. Named for WinRT's two properties so a port is a search and
  replace (`Header=` → `cf:ControlHeader.Header=`). To reverse: rename the class (no other control uses it yet), or
  delete `ControlHeader.cs` and the template's `HeaderContentPresenter` and record `Header` as missing.
- **The header's presenter recognises access keys**, so `PhoneContentLine` can give its text the phone's line (it
  acts on the `AccessText` Avalonia's presenter makes). A cost the phone doesn't have: an underscore in a string header
  marks an access key and isn't drawn. To reverse: a `PhoneContentLine` that also acts on a plain `TextBlock`, a
  change to a shared file.
- **`StepFrequency` isn't emulated.** On the phone a value from a tap or a drag goes to the nearest whole number;
  here it is wherever the finger is (30.47, say). The theme could set `IsSnapToTickEnabled` and `TickFrequency` 1,
  but then an app's `TickFrequency="10"` for its ticks would snap the value to tens, which the phone's doesn't
  (WinRT snaps to ticks only with `SnapsTo="Ticks"`), and `TickPlacement` alone would draw a hundred ticks where the
  phone draws none. An app that wants whole steps sets `IsSnapToTickEnabled="True" TickFrequency="1"`. To reverse: the
  two setters in the theme, or an attached `StepFrequency` (rung 2).
- **A drag from the thumb keeps Avalonia's way**: the thumb's middle goes to the finger on the first move, at most
  half the thumb (4.79) from where WinRT's would be, then follows. Changing it needs a `Slider` subclass or a part
  class (rungs 3 and 4) for a few pixels. To reverse: a part class `Thumb` whose drag moves by the travel.
- **The tick bars are the template's, drawn by Avalonia's `TickBar`**, with the thumb as its `ReservedSpace`: with
  the template's −4.5 margins that puts the first and last ticks at the track's ends, where WinUI's `TickBar` puts
  them (`TickBar_Partial.cpp`: half a thumb in from the bar's ends). No capture shows ticks, so their look (Avalonia's
  are 1 wide and three quarters tall but at the ends, WinUI's one raw pixel and full height) is unmeasured.
  `Inline`, WinRT's default placement, has no Avalonia member and is recorded as missing: with no `TickFrequency` it
  draws nothing on the phone, so the default looks the same.
- **The hit area is the whole container**, through a `Track` that fills it and a thumb as tall as it (above). No
  probe has pressed a Slider; WinUI's source has the container take the press. To reverse: put the `Track` in rows
  0 to 2 only.
- **The border's `StrokeThickness` is 1**, not bound to `BorderThickness`, as the phone's binding fails to 1 (above).
  To reverse: `StrokeThickness="{Binding $parent[Slider].BorderThickness.Left}"`.

## Dependencies and captures

- Tokens (#19), `PhoneContentLine` (#27) and `PhoneTextLine` (#26), the desktop sample (#9), Wp81's `RepeatButton`
  theme and `HoleParts` by style key (#30, PR #93): merged. All 23 resources the style reads are in `Tokens/`, but
  `HeaderContentPresenterStyle`, a `Style` whose four values the template sets on the presenter.
- Captures used: `base` and `CommonStates.Disabled`, dark and light, run `20261108-1443-dev11`. `CommonStates.Normal`
  is `base`, byte for byte.
- **Owed:** captures of a vertical Slider, of each `TickPlacement` with a `TickFrequency`, and of a Slider with no
  header; a probe of a tap, a drag from the track and a drag from the thumb (the interaction here is WinUI's source,
  not measured); and the device run on the Pixel 8.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist Slider <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.Slider`; Avalonia `Avalonia.Controls.Slider`.

### Properties (11)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `TickPlacement` | `TickPlacement` | `Slider.TickPlacement` | Avalonia's stands; `TopLeft`, `BottomRight` and `Outside` show the phone's bars (Behaviour 12). WinRT's `Inline`, its default, has no Avalonia member: recorded as missing |
| [x] | `TickFrequency` | `Double` | `Slider.TickFrequency` | Avalonia's stands; bound to the tick bars |
| [-] | `ThumbToolTipValueConverter` | `IValueConverter` | none | Not declared: the phone's style turns the thumb's tooltip off |
| [-] | `StepFrequency` | `Double` | none | Recorded as missing: a value from input isn't rounded to a step (decided above); `IsSnapToTickEnabled` with `TickFrequency` is Avalonia's nearest |
| [-] | `SnapsTo` | `SliderSnapsTo` | none | Recorded as missing: Avalonia's `IsSnapToTickEnabled` snaps to ticks |
| [x] | `Orientation` | `Orientation` | `Slider.Orientation` | Avalonia's stands; `:horizontal` and `:vertical` set the phone's two templates (Behaviour 11) |
| [-] | `IsThumbToolTipEnabled` | `Boolean` | none | Not declared: the phone's style sets it false, and Avalonia's Slider has no thumb tooltip, so both show none |
| [x] | `IsDirectionReversed` | `Boolean` | `Slider.IsDirectionReversed` | Avalonia's stands; bound to the `Track` and the tick bars (Behaviour 10) |
| [-] | `IntermediateValue` | `Double` | none | Recorded as missing: with no steps, `Value` is the value under the finger |
| [x] | `HeaderTemplate` | `DataTemplate` | none | Extension: `cf:ControlHeader.HeaderTemplate`, attached (Behaviour 6) |
| [x] | `Header` | `Object` | none | Extension: `cf:ControlHeader.Header`, attached, shown in `HeaderContentPresenter` (Behaviour 6) |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `RangeBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Value` | `Double` | `RangeBase.Value` | Avalonia's stands; bound two ways to the `Track` |
| [x] | `SmallChange` | `Double` | `RangeBase.SmallChange` | Avalonia's stands: the arrow keys |
| [x] | `Minimum` | `Double` | `RangeBase.Minimum` | Avalonia's stands; bound to the `Track` and the tick bars |
| [x] | `Maximum` | `Double` | `RangeBase.Maximum` | Avalonia's stands; bound to the `Track` and the tick bars |
| [x] | `LargeChange` | `Double` | `RangeBase.LargeChange` | Avalonia's stands: Page Up and Page Down |

### Events, from `RangeBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ValueChanged` | `RangeBaseValueChangedEventHandler` | `RangeBase.ValueChanged` | Avalonia's stands (the sample's page uses it) |

### Methods, from `RangeBase` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnMinimumChanged(Double oldMinimum, Double newMinimum)` (protected, overridable) | none | Not declared: Avalonia's `OnPropertyChanged` |
| [-] | `void OnMaximumChanged(Double oldMaximum, Double newMaximum)` (protected, overridable) | none | Not declared: as `OnMinimumChanged` |
| [-] | `void OnValueChanged(Double oldValue, Double newValue)` (protected, overridable) | none | Not declared: as `OnMinimumChanged` |

### Default style: setters (8)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource SliderTrackBackgroundThemeBrush}` | `TemplatedControl.Background` | Setter, `DynamicResource`; the track rect's fill |
| [x] | `BorderBrush` | `{ThemeResource SliderBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource`; the border rect's stroke |
| [x] | `BorderThickness` | `{ThemeResource SliderBorderThemeThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource`; draws nothing, as on the phone: the border is 1 wide whatever it says (Behaviour 9) |
| [x] | `Foreground` | `{ThemeResource SliderTrackDecreaseBackgroundThemeBrush}` | `TemplatedControl.Foreground` | Setter, `DynamicResource`; the decrease rect's fill, through the decrease button, which inherits it |
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`; the header sets its own, as the phone's |
| [x] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource`; as `FontFamily` |
| [-] | `ManipulationMode` | `None` | none | Not applicable: Avalonia has no manipulation mode |
| [-] | `IsThumbToolTipEnabled` | `False` | none | Not applicable: no thumb tooltip in Avalonia, so none, as on the phone |

### Template parts (18)

Avalonia's control looks for: `PART_DecreaseButton`, `PART_IncreaseButton`, `PART_Track`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HeaderContentPresenter` | `ContentPresenter` | none | A `ContentPresenter` of `cf:ControlHeader.Header` and `HeaderTemplate`, in `HeaderContentPresenterStyle`'s values, with the phone's line |
| [x] | `SliderContainer` | `Grid` | none | A transparent `Grid`; the `Track` fills it, so it all takes the finger |
| [x] | `HorizontalTemplate` | `Grid` | none | The `:horizontal` template's `Grid`, the phone's rows and a star row |
| [x] | `HorizontalTrackRect` | `Rectangle` | none | As the phone's, `SliderTrackThemeHeight` tall |
| [x] | `HorizontalDecreaseRect` | `Rectangle` | none | In `PART_DecreaseButton`'s template, the button's width, 17.5 down |
| [x] | `TopTickBar` | `TickBar` | none | Avalonia's `TickBar`, as the phone's; shown by `TopLeft` and `Outside` |
| [-] | `HorizontalInlineTickBar` | `TickBar` | none | Left out: no `Inline` in Avalonia's `TickPlacement`; the track rect's height holds the row |
| [x] | `BottomTickBar` | `TickBar` | none | As `TopTickBar`; shown by `BottomRight` and `Outside` |
| [x] | `HorizontalThumb` | `Thumb` | none | The `Track`'s `Thumb`, 9.5 wide, the container's height, drawing the phone's 9.5 × 19 border 13 down |
| [x] | `HorizontalBorder` | `Rectangle` | none | As the phone's, 1 wide, last, no hits |
| [x] | `VerticalTemplate` | `Grid` | none | The `:vertical` template's `Grid`, the phone's columns and a star column |
| [x] | `VerticalTrackRect` | `Rectangle` | none | As the phone's, `SliderTrackThemeHeight` wide |
| [x] | `VerticalDecreaseRect` | `Rectangle` | none | In `PART_DecreaseButton`'s template, the button's height, 13.5 in |
| [x] | `LeftTickBar` | `TickBar` | none | As the phone's; shown by `TopLeft` and `Outside` |
| [-] | `VerticalInlineTickBar` | `TickBar` | none | Left out: as `HorizontalInlineTickBar` |
| [x] | `RightTickBar` | `TickBar` | none | As the phone's; shown by `BottomRight` and `Outside` |
| [x] | `VerticalThumb` | `Thumb` | none | The `Track`'s `Thumb`, 9 tall, the container's width, drawing the phone's 9 × 9 border 13.5 in |
| [x] | `VerticalBorder` | `Rectangle` | none | As the phone's, column 1 |

And Avalonia's own: `PART_Track` (a `Track` filling the container), `PART_DecreaseButton` and `PART_IncreaseButton`
(`RepeatButton`s with the Slider's own themes).

### Visual states (2)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `Disabled` | `HorizontalBorder.Stroke = {ThemeResource SliderDisabledBorderThemeBrush}`<br>`VerticalBorder.Stroke = {ThemeResource SliderDisabledBorderThemeBrush}`<br>`HorizontalDecreaseRect.Fill = {ThemeResource SliderTrackDecreaseDisabledBackgroundThemeBrush}`<br>`HorizontalTrackRect.Fill = {ThemeResource SliderTrackDisabledBackgroundThemeBrush}`<br>`HorizontalTrackRect.Stroke = {ThemeResource SliderTrackDisabledBorderThemeBrush}`<br>`VerticalDecreaseRect.Fill = {ThemeResource SliderTrackDecreaseDisabledBackgroundThemeBrush}`<br>`VerticalTrackRect.Fill = {ThemeResource SliderTrackDisabledBackgroundThemeBrush}`<br>`VerticalTrackRect.Stroke = {ThemeResource SliderTrackDisabledBorderThemeBrush}`<br>`HorizontalThumb.Background = {ThemeResource SliderThumbDisabledBackgroundThemeBrush}`<br>`HorizontalThumb.BorderBrush = {ThemeResource SliderThumbDisabledBorderThemeBrush}`<br>`VerticalThumb.Background = {ThemeResource SliderThumbDisabledBackgroundThemeBrush}`<br>`VerticalThumb.BorderBrush = {ThemeResource SliderThumbDisabledBorderThemeBrush}`<br>`TopTickBar.Fill = {ThemeResource SliderTickMarkOutsideDisabledForegroundThemeBrush}`<br>`HorizontalInlineTickBar.Fill = {ThemeResource SliderTickMarkInlineDisabledForegroundThemeBrush}`<br>`BottomTickBar.Fill = {ThemeResource SliderTickMarkOutsideDisabledForegroundThemeBrush}`<br>`LeftTickBar.Fill = {ThemeResource SliderTickMarkOutsideDisabledForegroundThemeBrush}`<br>`VerticalInlineTickBar.Fill = {ThemeResource SliderTickMarkInlineDisabledForegroundThemeBrush}`<br>`RightTickBar.Fill = {ThemeResource SliderTickMarkOutsideDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled` setters on the borders, the track rects and the tick bars; the part themes' `^:disabled` on the decrease rects and the thumbs. The inline bars are left out (above) |

### Transitions (0)

_none_

### Resources (23)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `SliderTrackBackgroundThemeBrush` | `SolidColorBrush #FF1F1F1F` | `SolidColorBrush #FFDEDEDE` | yes | `Background` |
| [x] | `SliderBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | `BorderBrush` |
| [x] | `SliderBorderThemeThickness` | `0` | same | yes | `BorderThickness` |
| [x] | `SliderTrackDecreaseBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `Foreground`; follows an app's accent |
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | `FontFamily` |
| [x] | `ControlContentThemeFontSize` | `12` | same | yes | `FontSize` |
| [x] | `SliderThumbBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | The thumb themes' `BorderBrush` |
| [x] | `SliderThumbBackgroundThemeBrush` | `SolidColorBrush #FFFFFFFF` | `SolidColorBrush #FF000000` | yes | The thumb themes' `Background` |
| [x] | `SliderDisabledBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | `:disabled`, the border rects |
| [x] | `SliderTrackDecreaseDisabledBackgroundThemeBrush` | `SolidColorBrush #FF666666` | `SolidColorBrush #FFBDBDBD` | yes | `:disabled`, the decrease rects |
| [x] | `SliderTrackDisabledBackgroundThemeBrush` | `SolidColorBrush #FF1F1F1F` | `SolidColorBrush #FFDEDEDE` | yes | `:disabled`, the track rects' fill |
| [x] | `SliderTrackDisabledBorderThemeBrush` | `SolidColorBrush #FF1F1F1F` | `SolidColorBrush #FFDEDEDE` | yes | `:disabled`, the track rects' stroke |
| [x] | `SliderThumbDisabledBackgroundThemeBrush` | `SolidColorBrush #FF999999` | `SolidColorBrush #FF808080` | yes | `:disabled`, the thumbs' `Background` |
| [x] | `SliderThumbDisabledBorderThemeBrush` | `SolidColorBrush #FF999999` | `SolidColorBrush #FF808080` | yes | `:disabled`, the thumbs' `BorderBrush` |
| [x] | `SliderTickMarkOutsideDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80000000` | yes | `:disabled`, the tick bars |
| [-] | `SliderTickMarkInlineDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | Not used: no inline bars |
| [x] | `HeaderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | Its four values set on `HeaderContentPresenter` by their keys: `PhoneFontFamilyNormal`, `TextStyleMediumFontSize`, Normal, `PhoneMidBrush` |
| [x] | `SliderTrackBorderThemeBrush` | `SolidColorBrush #FF1F1F1F` | `SolidColorBrush #FFDEDEDE` | yes | The track rects' stroke |
| [x] | `SliderTickmarkOutsideBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80000000` | yes | The tick bars' `Fill` |
| [x] | `SliderOutsideTickBarThemeHeight` | `5` | same | yes | The tick bars' height (width, vertical) |
| [-] | `SliderTickMarkInlineBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | Not used: no inline bars |
| [x] | `SliderTrackThemeHeight` | `9` | same | yes | The track's height (width, vertical), the decrease rects', and the vertical thumb's size |
| [x] | `SliderThumbStyle` | not in themeresources.xaml |  | no | The template's own: `Wp81SliderHorizontalThumbTheme` and `Wp81SliderVerticalThumbTheme` in `Slider.axaml` |
