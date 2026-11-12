# TextBox: plan

[#36](https://github.com/wdcossey/CornflowerUI/issues/36). The 8.1 TextBox, Avalonia's `TextBox` themed as the
phone's: its default style and template as an Avalonia `ControlTheme`, the phone's box at rest, focused and disabled,
dark and light, the header by `ControlHeader` (#96), the placeholder, and the text in the phone's line.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `TextBox`,
`R:\cornflower-reference\flash-update1\xbf-json\textbox.json` (and its XAML beside it), and the values of its keys from
`themeresources.json`. The runtime's templates and theme resources equal the SDK's design files
([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the checklist, generated from the SDK, names the same keys.

The captures **check** the theme; they don't supply values. They are the reference app's, on the reference profile
(device 11, 1080 × 1920 at 2.4), committed in [`ref/wp81/TextBox/`](../../../ref/wp81/TextBox/README.md):

- run `20261108-1443-dev11`: `new TextBox { Header = "text box", PlaceholderText = "placeholder" }` (`empty`) and
  `new TextBox { Header = "text box", Text = "Agjy 0123" }` (`text`), at rest and in `Normal`, `Focused` and
  `Disabled`, dark and light. Run `20261108-1732-dev11`'s are byte for byte the same; `Normal` is `base`.
- run `20261112-1319-dev11`, made for this plan from a copy of `tools/refapp` with three more specimens: no header
  (`new TextBox { Text = "Agjy 0123" }`), no header and empty, and two wrapped lines (`TextWrapping = Wrap`,
  `AcceptsReturn = true`). What a TextBox with no header does, and the line pitch, no earlier run showed.

**What no capture has**: a caret, a selection, the keyboard, or any input: a forced `Focused` state draws the focus
look and nothing else. So the caret, the selection's text colour and the keyboard are Avalonia's and the platform's,
said so below.

## The phone's style

`textbox.json`: one unkeyed style for `TextBox`, 18 setters and a template.

| Setter | Value | Resource value | Dump, dark / light |
|---|---|---|---|
| `MinWidth` | `{ThemeResource TextControlThemeMinWidth}` | `51` | 51 |
| `MinHeight` | `{ThemeResource TextControlThemeMinHeight}` | `34` | 34 |
| `Foreground` | `{ThemeResource TextBoxForegroundThemeBrush}` | `#FF000000`, both | `#FF000000` |
| `SelectionHighlightColor` | `{ThemeResource TextSelectionHighlightColorThemeBrush}` | the accent, Cobalt `#FF3E65FF` | |
| `Background` | `{ThemeResource TextBoxBackgroundThemeBrush}` | `#FFFFFFFF` / `#FFBFBFBF` | the same |
| `BorderBrush` | `{ThemeResource TextBoxBorderThemeBrush}` | `#FFFFFFFF` / `#FFBFBFBF` | the same |
| `BorderThickness` | `{ThemeResource TextControlBorderThemeThickness}` | `2.5` | 2.5 |
| `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `Segoe WP` | `Segoe WP` |
| `FontSize` | `{ThemeResource ContentControlFontSize}` | `20.26` | 20.26 |
| `TextWrapping` | `NoWrap` | | |
| `ScrollViewer.HorizontalScrollMode`, `VerticalScrollMode` | `Auto` | | |
| `ScrollViewer.HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility` | `Hidden` | | |
| `ScrollViewer.IsDeferredScrollingEnabled` | `False` | | |
| `Padding` | `{ThemeResource TextControlThemePadding}` | `7,0.8,8,0` | the same |
| `Margin` | `{ThemeResource TextControlMarginThemeThickness}` | `0,9.5,0,9.5` | the same |
| `VerticalAlignment` | `Top` | | `Top` |

### The template

```
Grid                                   Background Transparent; rows Auto, *
  Border "BorderElement"               Row 1; Background, BorderBrush, BorderThickness: TemplateBinding
  ContentPresenter "HeaderContentPresenter"
                                       Row 0; HeaderContentPresenterStyle; Margin TextControlHeaderMarginThemeThickness
                                       (0,0,0,4.8); Content Header, ContentTemplate HeaderTemplate
  ScrollViewer "ContentElement"        Row 1; MinHeight TextControlThemeMinHeight; the box's scroll settings;
                                       Margin: TemplateBinding BorderThickness; Padding: TemplateBinding Padding;
                                       IsTabStop False; ZoomMode Disabled
  ContentControl "PlaceholderTextContentPresenter"
                                       Row 1; Foreground TextBoxPlaceholderTextThemeBrush; FontSize ContentControlFontSize;
                                       Margin BorderThickness; Padding Padding; Content PlaceholderText
```

`HeaderContentPresenterStyle` (generic.xaml): `PhoneFontFamilyNormal`, `TextStyleMediumFontSize` (16), `Normal`,
`PhoneMidBrush` (`#99FFFFFF` / `#73000000`), as the Slider's header (#96).

**There is no clear button.** The 8.1 *desktop* TextBox template (`Windows Kits\8.1\...\generic.xaml`) has a
`DeleteButton` with its own style and a `ButtonStates` group; the phone's has neither, and no capture shows one, in
any state. The roadmap's "clear button glyph" came from the desktop template, so no glyph is added (decided below).

What WinRT's code does to the template, from the dumps: it **collapses `HeaderContentPresenter`**, margin and all,
when `Header` is null (`no-header`: the box 39.1667 tall, `BorderElement` at its top), and it **collapses the
placeholder** while there is text (`text`: `PlaceholderTextContentPresenter` `Collapsed`, 0 × 0).

### The visual states

`CommonStates`, three states; no `PointerOver`, no other group.

| State | Sets | Dark | Light |
|---|---|---|---|
| `Normal` | `BorderElement.Opacity` to `TextControlBorderThemeOpacity`, 0.8 | the box and border 204 grey over black (white at 0.8) | 204 grey over white (`#FFBFBFBF` at 0.8) |
| `Focused` | `BorderElement.BorderBrush` the accent, `Background` `TextBoxFocusedBackgroundThemeBrush` (`#FFFFFFFF`, both); the placeholder's `Opacity` 0. The Normal opacity no longer runs: 1 | the accent round white | the same |
| `Disabled` | `BorderElement.Background` `TextBoxDisabledBackgroundThemeBrush` (transparent), `BorderBrush` `TextBoxDisabledBorderThemeBrush` (`#73FFFFFF` / `#40000000`); the text and placeholder `TextBoxDisabledForegroundThemeBrush` (`#FF747474` / `#FFBFBFBF`); the header `TextBoxDisabledHeaderForegroundThemeBrush` (`#73FFFFFF` / `#40000000`); opacity 1 | the border alone, 115 grey | 191 grey |

The PNGs read those colours on every edge: rows 116 to 209 and columns 46 to 1034, the border 6 raw pixels.

### The text

The text view is the dump's `ScrollContentPresenter` child at `[9.5833, 30]` in the box, 33.3333 tall: the
`ScrollViewer`'s 34.1667 (its `MinHeight`, 34, to the grid) less the padding's 0.8. The wrapped specimen's view is
54.1667: two lines, 130 raw pixels. Read from the PNGs, the two baselines are 65 raw pixels apart, 27.08 DIP, so the
phone's text view draws each line 1.33 em below the last (26.9458 at 20.26, on the pixel grid); its first baseline is
the placeholder's (both "A" and "l" end on row 177, 0.24 into it). The placeholder's `TextBlock` is 26.9467 tall at
the same place: the phone's 1.33 em line (ADR 0003). So the text's line is the phone's own, 1.33 em of its size.

## Avalonia's TextBox against the phone's

Avalonia 12.1.4's `TextBox` (read at the `12.1.4` tag: `TextBox.cs`, `TextPresenter.cs`, and Simple's `TextBox.xaml`)
looks for `PART_TextPresenter` (required) and `PART_ScrollViewer`, raises `:empty` and `:touch-mode`, and Avalonia
adds `:focus`, `:pointerover`, `:disabled` and `:error` (data validation). It has `PlaceholderText` (`Watermark` is its
old name), `PlaceholderForeground`, `UseFloatingPlaceholder`, `InnerLeftContent`, `InnerRightContent`, `LineHeight`,
`MinLines`, `MaxLines`, `CaretBrush`, `SelectionBrush`, `SelectionForegroundBrush`, `PasswordChar` and `RevealPassword`.
Its `TextPresenter` draws its font's own line (Selawik's 1.2 em) unless given a `LineHeight`. In each measure it sets
its `ScrollViewer`'s `MinHeight` and `MaxHeight` from `MinLines` and `MaxLines`, in lines of its own `LineHeight`.
Simple's template: a `DataValidationErrors` round the content, a floating placeholder, an IBeam cursor, a Cut, Copy and
Paste flyout, and the `.clearButton` and `.revealPasswordButton` classes.

| Difference | The phone | Avalonia's TextBox | Rung |
|---|---|---|---|
| The style and template | Above | Simple's: a 1-wide border, 4 padding, no header | 1, theme |
| `Header`, `HeaderTemplate` | on the TextBox | none | 2, `cf:ControlHeader` (#96), shown in the phone's style and line (`PhoneHeaderLine`) |
| No header | the presenter collapsed, margin and all | | 1, its `IsVisible` bound to whether `Header` is set |
| The placeholder | shown while empty, hidden while focused, at `ContentControlFontSize` whatever the box's size | Simple's: a `TextBlock` in the box's size | 1, a `ContentPresenter` by `:empty` and `:focus`, its line the phone's (`PhoneHeaderLine`) |
| `SelectionHighlightColor` | the accent | `SelectionBrush` | 1, the style sets `SelectionBrush` |
| The text's line | 1.33 em | 1.2 em | 1 and 2: the style binds the box's `LineHeight` to 1.33 × its `FontSize` (`PhoneTextBoxLine`, new and internal), and the template the presenter's to it. An app's `LineHeight` wins; `MinLines` and `MaxLines` count the phone's lines |
| `ContentElement`'s `MinHeight` | 34 | overwritten by the TextBox in each measure | 1, set on the `Grid` round it instead |
| The scroll bars | `Hidden`: none drawn | the `ScrollViewer`'s theme by type, a hole today (Wp81 alone would give it no template, and the text would vanish) | 1, the part's own theme, `Wp81TextBoxScrollViewerTheme`: the content presenter inset by the padding, with the gesture recogniser, no bars |
| The text clipped inside the padding | the `ScrollContentPresenter`'s margin is the padding | Simple's pads inside it | 1, the presenter's `Margin`, as the phone's |
| The box at 0.8 | the Border as one layer | Skia applies a visual's opacity to its background and its border each: 245 grey where they overlap | 1, `RenderOptions.RequiresFullOpacityHandling` on the template's root, as ToggleButton's (#93) |
| Focus | `Focused`, for any focus, a tap's too | `:focus` | 1, `:focus`, not `:focus-visible` |
| Pointer over | no state | `:pointerover` | 1, no style |
| `ScrollMode` | `Auto` | no member; scrolls where the content is larger | none |
| Data validation | none | Simple's `DataValidationErrors` | 1, left out (decided) |
| The context menu | none | Simple's Cut, Copy and Paste flyout | 1, none set (decided) |
| The clear button | none | Simple's `.clearButton` | 1, none (decided) |
| `InnerLeftContent`, `InnerRightContent` | no member | either side of the text | 1, inside the border either side of the text |
| `TextAlignment`, content alignment | the placeholder ignores `TextAlignment`; the template ignores the content alignments | | 1, the same |
| The caret, the selection's text colour, the keyboard | not captured | Avalonia's: a 1-wide caret in black (no `CaretBrush`, no presenter background); selected text in its own colour; the platform's keyboard (`TextInputOptions`) | none: recorded |
| Rounding | the nearest raw pixel | up | ADR 0003's recorded deviation; the sizes below |

No type is substituted. Rungs 1, 2 and 6.

### Where rounding up differs from the phone's

At 2.4:

| Size | Template | Raw pixels | The phone | Ours | What shows |
|---|---|---|---|---|---|
| The stage's slot | 450 less 19 each side | 1080 − 45.6 − 45.6 | 989, 412.0833 | 988, 411.6667 | The right edge one raw pixel sooner: 1033, not 1034 |
| The header and its gap | 21.28 + 4.8 | 51.07, 63.5 | 64, 26.6667 | 64, 26.6667 | Nothing |
| `ContentElement` | 34 | 81.6 | 82, 34.1667 | 82, 34.1667 | Nothing |
| The text view | 33.3333 | 80 | 80 | 80 | Nothing |
| Two lines | 2 × 26.9458 | 129.3 | 130, 54.1667 | 130, 54.1667 | Nothing |

## Other controls' templates

Simple 12.1.4's templates hold plain TextBoxes, which Wp81's theme, keyed by type, would reach: AutoCompleteBox's
`PART_TextBox`, CalendarDatePicker's `PART_TextBox`, ComboBox's `PART_EditableTextBox`, the managed file dialog's two,
and NumericUpDown's `PART_TextBox`. Each is checked:

| Template | What it passes the TextBox | With the phone's theme | So |
|---|---|---|---|
| AutoCompleteBox | Simple's padding, background, border | the phone's 9.5 margins and header row in Simple's chrome | Simple's |
| CalendarDatePicker | Simple's background and border; no foreground | the phone's black text on Simple's dark background | Simple's |
| ComboBox (editable) | its foreground, a transparent background, no border | the phone's white box when focused, under Simple's white text | Simple's |
| ManagedFileChooser | nothing | the phone's boxes in a desktop dialog laid out for Simple's (its CheckBox is Simple's already, #92) | Simple's |
| NumericUpDown | Simple's background, no border, 4 margins | the phone's black text on Simple's dark spinner | Simple's |

None should take the phone's: each is chrome another theme laid out. So `HoleParts` (#93) keeps them Simple's, as it
keeps those templates' Buttons, ToggleButtons, RepeatButtons and CheckBox. It took a `Button` (and its subclasses);
it now takes any templated control (`AttachedProperty` on `TemplatedControl`, its class handler and `Check` the same
code for any of them), and the TextBox theme sets it. A shared file, changed with the coordinator's approval. An app's
own control, and an app's theme for a hole, keep the phone's TextBox. `MaskedTextBox` is a TextBox to a theme (its
style key), so it takes the phone's.

## Extensions

None new. WinRT's `Header` and `HeaderTemplate` are `cf:ControlHeader` (#96).

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/TextBoxTests.cs` unless named.

1. **Templated by the theme**, a `MaskedTextBox` too. `A_TextBox_And_A_MaskedTextBox_Have_The_Phones_Template`.
2. **The style's values**, dark and light: the setters Avalonia has, `SelectionBrush` the accent, the placeholder's
   brush, the parts' values, no focus adorner. `The_Style_Sets_What_The_Phone_Read`; its font and wrapping its own,
   not a window's or an ancestor's. `Its_Font_And_Wrapping_Are_The_Styles`.
3. **The bounds**: the box at 19.1667, 21.6667, 65.8333 tall; the header 21.6667; `BorderElement` 26.6667 down,
   39.1667 tall; the text at 9.5833, 30, 33.3333 tall (the dump), 411.6667 wide (rounding).
   `The_Bounds_Are_The_Dumps`.
4. **No header, no room**: 39.1667, the box at the top; an empty string a line; null again, none.
   `With_No_Header_The_Header_Takes_No_Room`, `An_Empty_Header_Is_A_Line_And_Null_Is_None`.
5. **The header** in the phone's style and line, 4.8 above the box, not the box's font; an underscore drawn; a
   `HeaderTemplate` used. `The_Header_Is_The_Phones`, `The_Header_Keeps_An_Underscore_And_Takes_A_HeaderTemplate`.
6. **The line**: two wrapped lines 54.1667, the box 86.6667; 1.33 em of any `FontSize`; an app's `LineHeight` wins;
   `MaxLines` counts the phone's lines. `Wrapped_Text_Takes_The_Phones_Lines`,
   `The_Line_Follows_The_FontSize_And_An_Apps_LineHeight_Wins`, `MaxLines_Counts_The_Phones_Lines`.
7. **At rest**: 204 grey on every edge and inside, the page outside, as one layer. `At_Rest_The_Box_Is_Drawn_At_Eight_Tenths`.
8. **Focused**: the accent, 6 raw pixels, round white, at full opacity; the placeholder hidden; back at rest when focus
   goes. `Focused_Is_The_Accent_Round_White`.
9. **Disabled**: the border alone (115 or 191 grey), no background, the text, placeholder and header dimmed.
   `Disabled_Is_The_Border_Alone_And_Dim_Text`.
10. **The placeholder**: where the text starts, in its brush, at 20.26 in the phone's line, only while empty; its size
    the phone's whatever the box's; an app's `PlaceholderForeground` used, but not when disabled.
    `The_Placeholder_Shows_While_The_Box_Is_Empty`, `The_Placeholder_Keeps_Its_Size_And_Takes_An_Apps_Brush`.
11. **Typing** goes into the box and hides the placeholder. `Typing_Puts_Text_In_The_Box`.
12. **The selection** is the accent. `The_Selection_Is_The_Accent`.
13. **Scrolling**: text scrolled sideways is clipped inside the padding; a touch drag pans it.
    `Scrolled_Text_Is_Clipped_Inside_The_Padding`, `A_Touch_Drag_Pans_Long_Text`.
14. **No pointer-over look, and no focus adorner** (the Focused state is the focus look). `The_Pointer_Over_It_Changes_Nothing`,
    `Keyboard_Focus_Draws_No_Adorner`.
15. **The part's own ScrollViewer theme**, with an app's implicit one present; no bars. `The_ScrollViewer_Is_The_Themes_Own`.
16. **What the box passes its parts** (Avalonia's scroll settings, caret, selection, password and alignment values).
    `The_Boxs_Values_Reach_Its_Parts`; `InnerLeftContent` and `InnerRightContent` either side, their text not in the
    box's line. `Inner_Content_Sits_Either_Side_Of_The_Text`.
17. **The colours follow the variant.** `The_Colours_Follow_The_Variant`.
18. **Holes**: every hole's plain TextBoxes keep Simple's (five templates); NumericUpDown's keeps Simple's look; an
    app's template and an app's theme for a hole keep the phone's (`TextBoxHolePartsTests.cs`). `TextBox` is deleted
    from `Completeness/Holes.txt`.
19. **No global rule**: `StyleBudgetTests`, unchanged.
20. **The sample's page**: in the catalogue, every TextBox on it the theme's, its header from its title, the first
    TextBox's header shown and what is typed following it (`Sample/TextBoxPageTests.cs`).

## Decided on the user's behalf

- **No clear button, and no glyph for one.** The phone's TextBox has none (above); the `DeleteButton` the issue and
  the roadmap name is the Windows 8.1 desktop template's. An app that sets Simple's `.clearButton` class gets nothing
  from Wp81. To reverse: a `.clearButton` style setting `InnerRightContent` to a button, and its glyph in `Glyphs/`.
- **The text's line is the box's `LineHeight`, bound to its `FontSize` in the style** (`PhoneTextBoxLine`, an
  internal converter), not a value on the presenter alone: Avalonia's `MinLines` and `MaxLines` read the box's, so
  they count the phone's lines, and an app's `LineHeight` still wins. It changes what `TextBox.LineHeight` reads
  (26.9474, not NaN). `LineHeight` inherits, so the template's other presenters (the header, the placeholder, the
  inner content) set it back to NaN: what an app's template or content makes keeps its own line. To reverse: drop the
  setter; the presenter then draws Selawik's line, 24.31.
- **The `ScrollViewer` part has its own theme**, `Wp81TextBoxScrollViewerTheme`, rather than the `ScrollViewer` theme
  by type: no bars, as the phone's `Hidden` draws none, and the text doesn't depend on a hole (Wp81 has no
  `ScrollViewer` theme yet; alone, the box would show nothing). Prefixed so it doesn't shadow Simple's keys. To
  reverse: delete the `Theme` attribute.
- **`HoleParts` takes any templated control**, so the five templates' TextBoxes keep Simple's. To reverse: list the
  TextBox under "Proposed" and let the templates take the phone's.
- **The header shows for any non-null `Header`**, an empty string as one empty line (as #96 decided for the Slider);
  null collapses it, as measured. With a `HeaderTemplate` and no `Header` it stays collapsed (the phone's code, by
  WinUI's, shows it; 8.1's isn't captured). To reverse: bind `IsVisible` to either being set.
- **No context menu.** Simple's TextBox sets a Cut, Copy and Paste flyout; the phone's has no context menu (a long
  press selects; the keyboard offers the clipboard). An app sets `ContextFlyout` for one.
- **No data-validation display.** Simple wraps the content in `DataValidationErrors`; the phone has nothing like it.
  An app's validation errors show nothing. To reverse: wrap `ContentRow` in a `DataValidationErrors`.
- **`UseFloatingPlaceholder`, the content alignments and the IBeam cursor are left out**, as the phone's template has
  none of them; the placeholder ignores `TextAlignment`, as the phone's `ContentControl` does.
- **The caret and the selection's text colour are Avalonia's** (`CaretBrush` and `SelectionForegroundBrush` unset):
  no capture shows the phone's, and the forced `Focused` state draws no caret.
- **The placeholder is a `ContentPresenter`, not a `ContentControl`**, as a `ContentControl` would need a theme of its
  own (it is a hole). It reuses `PhoneHeaderLine` for its text's line: a presenter that doesn't recognise access keys,
  as the phone's placeholder doesn't.
- **`InnerLeftContent` and `InnerRightContent` sit inside the border, either side of the text**, as in Simple's
  template; the phone has no such members.
- **`:focus`, not `:focus-visible`**, for `Focused`: the phone goes to `Focused` on a tap too.
- **A new capture run**, `20261112-1319-dev11`, from a copy of `tools/refapp` with three specimens (`tools/` is
  shared this batch). The specimens are proposed for `Catalog.cs` in the PR.

## Dependencies and captures

- Tokens (#19), `PhoneTextLine` (#26), the desktop sample (#9), `HoleParts` by style key (#93), `ControlHeader` and
  `PhoneHeaderLine` (#96): merged. All 19 resources the style reads are in `Tokens/`, but `HeaderContentPresenterStyle`,
  whose four values the template sets on the presenter.
- Captures used: `empty` and `text` at `base`, `Focused` and `Disabled` (run `20261108-1443-dev11`), and `no-header`
  and `wrap` the same (run `20261112-1319-dev11`), dark and light.
- **Owed:** a capture of a real focus (the caret), of a selection, and of the keyboard; the device run on the Pixel 8.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist TextBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.TextBox`; Avalonia `Avalonia.Controls.TextBox`.

### Properties (18)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionHighlightColor` | `SolidColorBrush` | none | Avalonia's `SelectionBrush`; the style sets it to the accent |
| [-] | `PreventKeyboardDisplayOnProgrammaticFocus` | `Boolean` | none | Not applicable: the platform shows the keyboard; Avalonia has no such member |
| [x] | `PlaceholderText` | `String` | `TextBox.PlaceholderText` | Shown by `PlaceholderTextContentPresenter` |
| [-] | `IsColorFontEnabled` | `Boolean` | none | No Avalonia member: Avalonia draws a colour font's glyphs as the font has them |
| [x] | `HeaderTemplate` | `DataTemplate` | none | `cf:ControlHeader.HeaderTemplate` (#96) |
| [x] | `Header` | `Object` | none | `cf:ControlHeader.Header` (#96) |
| [x] | `SelectionStart` (not a dependency property) | `Int32` | `TextBox.SelectionStart` | Avalonia's |
| [x] | `SelectionLength` (not a dependency property) | `Int32` | none | Avalonia's `SelectionEnd` less `SelectionStart` |
| [x] | `SelectedText` (not a dependency property) | `String` | `TextBox.SelectedText` | Avalonia's |
| [x] | `MaxLength` | `Int32` | `TextBox.MaxLength` | Avalonia's |
| [x] | `IsTextPredictionEnabled` | `Boolean` | none | Avalonia's `TextInputOptions.ShowSuggestions`, attached: the platform keyboard's |
| [-] | `IsSpellCheckEnabled` | `Boolean` | none | No Avalonia member; the platform keyboard's own |
| [x] | `IsReadOnly` | `Boolean` | `TextBox.IsReadOnly` | Avalonia's; the phone's template has no state for it, so no look |
| [x] | `InputScope` | `InputScope` | none | Avalonia's `TextInputOptions.ContentType`, attached: the platform keyboard's |
| [x] | `Text` | `String` | `TextBox.Text` | Avalonia's, two-way to the presenter |
| [x] | `AcceptsReturn` | `Boolean` | `TextBox.AcceptsReturn` | Avalonia's |
| [x] | `TextWrapping` | `TextWrapping` | `TextBox.TextWrapping` | Avalonia's; the style sets `NoWrap` |
| [x] | `TextAlignment` | `TextAlignment` | `TextBox.TextAlignment` | Avalonia's, to the presenter; the placeholder ignores it, as the phone's |

### Events (4)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ContextMenuOpening` | `ContextMenuOpeningEventHandler` | none | Avalonia's `ContextRequested`; the theme sets no context menu (decided) |
| [-] | `SelectionChanged` | `RoutedEventHandler` | none | Avalonia's TextBox raises none; `SelectionStart` and `SelectionEnd` raise property changes. Recorded as missing |
| [x] | `TextChanged` | `TextChangedEventHandler` | `TextBox.TextChanged` | Avalonia's |
| [x] | `Paste` | `TextControlPasteEventHandler` | none | Avalonia's `PastingFromClipboard` |

### Methods (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void Select(Int32 start, Int32 length)` | none | Avalonia's `SelectionStart` and `SelectionEnd` |
| [x] | `void SelectAll()` | `TextBox.SelectAll()` | Avalonia's |
| [-] | `Rect GetRectFromCharacterIndex(Int32 charIndex, Boolean trailingEdge)` | none | No public member on Avalonia's TextBox. Recorded as missing |

### Default style: setters (18)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `MinWidth` | `{ThemeResource TextControlThemeMinWidth}` | `Layoutable.MinWidth` | Set |
| [x] | `MinHeight` | `{ThemeResource TextControlThemeMinHeight}` | `Layoutable.MinHeight` | Set |
| [x] | `Foreground` | `{ThemeResource TextBoxForegroundThemeBrush}` | `TemplatedControl.Foreground` | Set |
| [x] | `SelectionHighlightColor` | `{ThemeResource TextSelectionHighlightColorThemeBrush}` | none | `SelectionBrush` |
| [x] | `Background` | `{ThemeResource TextBoxBackgroundThemeBrush}` | `TemplatedControl.Background` | Set |
| [x] | `BorderBrush` | `{ThemeResource TextBoxBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Set |
| [x] | `BorderThickness` | `{ThemeResource TextControlBorderThemeThickness}` | `TemplatedControl.BorderThickness` | Set |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Set |
| [x] | `FontSize` | `{ThemeResource ContentControlFontSize}` | `TemplatedControl.FontSize` | Set |
| [x] | `TextWrapping` | `NoWrap` | `TextBox.TextWrapping` | Set (it inherits in Avalonia, so the setter stops an ancestor's) |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Auto` | none | No Avalonia member: its ScrollViewer scrolls where the content is larger, which is `Auto` |
| [-] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | The same |
| [x] | `ScrollViewer.HorizontalScrollBarVisibility` | `Hidden` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | Avalonia's TextBox sets it itself, at style priority: `Hidden`, but `Auto` with `AcceptsReturn` and `Disabled` wrapping. No setter (it would lose); bound to the part, whose theme draws no bars, so each looks the phone's |
| [x] | `ScrollViewer.VerticalScrollBarVisibility` | `Hidden` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | Set, and bound to the part |
| [x] | `ScrollViewer.IsDeferredScrollingEnabled` | `False` | `ScrollViewer.IsDeferredScrollingEnabled (attached)` | Avalonia's default, not inherited, so no setter; bound to the part |
| [x] | `Padding` | `{ThemeResource TextControlThemePadding}` | `TemplatedControl.Padding` | Set |
| [x] | `Margin` | `{ThemeResource TextControlMarginThemeThickness}` | `Layoutable.Margin` | Set |
| [x] | `VerticalAlignment` | `Top` | `Layoutable.VerticalAlignment` | Set |

### Template parts (4)

Avalonia's control looks for: `PART_TextPresenter`, `PART_ScrollViewer`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `BorderElement` | `Border` | none | The same, at 0.8 at rest, as one layer |
| [x] | `HeaderContentPresenter` | `ContentPresenter` | none | The same, from `ControlHeader`, collapsed with no header |
| [x] | `ContentElement` | `ScrollViewer` | none | `PART_ScrollViewer`, Avalonia's name, in its own theme; its `MinHeight` on the `Grid` round it; `PART_TextPresenter` in it |
| [x] | `PlaceholderTextContentPresenter` | `ContentControl` | none | A `ContentPresenter` of that name (decided) |

### Visual states (3)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Disabled` | `BorderElement.Background = {ThemeResource TextBoxDisabledBackgroundThemeBrush}`<br>`BorderElement.BorderBrush = {ThemeResource TextBoxDisabledBorderThemeBrush}`<br>`ContentElement.Foreground = {ThemeResource TextBoxDisabledForegroundThemeBrush}`<br>`PlaceholderTextContentPresenter.Foreground = {ThemeResource TextBoxDisabledForegroundThemeBrush}`<br>`HeaderContentPresenter.Foreground = {ThemeResource TextBoxDisabledHeaderForegroundThemeBrush}` | `^:disabled` | Themed, with the opacity 1 |
| [x] | `CommonStates` | `Normal` | `BorderElement.Opacity: DoubleAnimation Duration=0 To={ThemeResource TextControlBorderThemeOpacity}` | the theme's plain setters | The Border's `Opacity`, 0.8 |
| [x] | `CommonStates` | `Focused` | `BorderElement.BorderBrush = {ThemeResource TextSelectionHighlightColorThemeBrush}`<br>`PlaceholderTextContentPresenter.Opacity: DoubleAnimation Duration=0 To=0`<br>`BorderElement.Background = {ThemeResource TextBoxFocusedBackgroundThemeBrush}` | `^:focus-visible` | `^:focus`, as a tap focuses it too; the opacity 1 |

### Transitions (0)

_none_

### Resources (20)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `TextControlThemeMinWidth` | `51` | same | yes | By key |
| [x] | `TextControlThemeMinHeight` | `34` | same | yes | By key, the box's and `ContentRow`'s |
| [x] | `TextBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource TextBoxForegroundColor} → #FF000000` | same | yes | By key |
| [x] | `TextSelectionHighlightColorThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | By key: the selection and the focused border |
| [x] | `TextBoxBackgroundThemeBrush` | `SolidColorBrush {ThemeResource TextBoxBackgroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource TextBoxBackgroundColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource TextBoxBorderColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource TextBoxBorderColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextControlBorderThemeThickness` | `2.5` | same | yes | By key |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | By key, the box's and the header's |
| [x] | `ContentControlFontSize` | `20.26` | same | yes | By key, the box's and the placeholder's |
| [x] | `TextControlThemePadding` | `7,0.8,8,0` | same | yes | By key |
| [x] | `TextControlMarginThemeThickness` | `0,9.5,0,9.5` | same | yes | By key |
| [x] | `TextBoxDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | By key |
| [x] | `TextBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `TextBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowSolidColor} → #FF747474` | `SolidColorBrush {ThemeResource PhoneBaseLowSolidColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextBoxDisabledHeaderForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `TextControlBorderThemeOpacity` | `0.8` | same | yes | By key |
| [x] | `TextBoxFocusedBackgroundThemeBrush` | `SolidColorBrush #FFFFFFFF` | same | yes | By key |
| [x] | `HeaderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | Its four values on the presenter, by their keys, as the Slider's |
| [x] | `TextControlHeaderMarginThemeThickness` | `0,0,0,4.8` | same | yes | By key |
| [x] | `TextBoxPlaceholderTextThemeBrush` | `SolidColorBrush {ThemeResource TextBoxPlaceholderColor} → #FF909090` | `SolidColorBrush {ThemeResource TextBoxPlaceholderColor} → #40000000` | yes | By key, through `PlaceholderForeground` |
