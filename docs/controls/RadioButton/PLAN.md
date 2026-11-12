# RadioButton: plan

[#32](https://github.com/wdcossey/CornflowerUI/issues/32). The 8.1 RadioButton, Avalonia's `RadioButton` themed as
the phone's: its default style and template as an Avalonia `ControlTheme`, the circle and the dot as the template's
own ellipses, the tilt on the part the phone tilts, and the phone's colours in each state, dark and light.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `RadioButton`,
`R:\cornflower-reference\flash-update1\xbf-json\radiobutton.json`, and the values of its keys from
`themeresources.json` beside it. The runtime's templates and theme resources equal the SDK's design files
([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the checklist, generated from the SDK, names the same
keys.

The captures **check** the theme; they don't supply values. They are the reference app's, run
`20261108-1443-dev11` on the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/RadioButton/`](../../../ref/wp81/RadioButton/README.md). Run `20261108-1732-dev11` has the same PNGs,
but for the forced Pressed state's tilt.

## The phone's style

`radiobutton.json`: one unkeyed style for `RadioButton`, ten setters and a template.

| Setter | Value | Resource value (`themeresources.json`) | Dump, dark / light |
|---|---|---|---|
| `Background` | `{ThemeResource PhoneRadioCheckBoxBrush}` | `Transparent` | `#00FFFFFF` |
| `BorderBrush` | `{ThemeResource PhoneRadioCheckBoxBorderBrush}` | `PhoneForegroundColor`: `#FFFFFFFF` dark, `#FF000000` light | `#FFFFFFFF` / `#FF000000` |
| `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `18.14` | 18.14 |
| `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `Segoe WP` | `Segoe WP` |
| `HorizontalContentAlignment` | `Left` | | `Left` |
| `VerticalContentAlignment` | `Top` | | `Top` |
| `HorizontalAlignment` | `Left` | | `Left` |
| `VerticalAlignment` | `Top` | | `Top` |
| `Padding` | `{ThemeResource CheckBoxAndRadioButtonTextPaddingThickness}` | `8,0,0,0` | the same |
| `MinWidth` | `{ThemeResource CheckBoxAndRadioButtonMinWidthSize}` | `168` | 168 |

The style sets no `BorderThickness`: the ring's width is the template's `PhoneStrokeThickness`. Nor `Foreground`: the
dump reads `#FFFFFFFF` dark and `#FF000000` light, `PhoneForegroundColor`, what the RadioButton inherits from the
page. As the CheckBox theme does (PR #92, open), ours sets `Foreground` to `PhoneForegroundBrush` (see "Decided on the user's
behalf" below).

### The template

`radiobutton.json`, `template.tree`:

```
Grid                            Background Transparent
  Grid "Container"              Margin {ThemeResource PhoneTouchTargetLargeOverhang} (0,16); columns 25.5, *
    Grid                        Column 0; VerticalAlignment Top
      Ellipse "CheckBackground" 25.5 × 25.5, Left, Center; Fill: TemplateBinding Background; Stroke: TemplateBinding
                                BorderBrush; StrokeThickness {ThemeResource PhoneStrokeThickness} (2.5)
      Ellipse "CheckMark"       12.5 × 12.5, centred; Fill {ThemeResource PhoneRadioCheckBoxCheckBrush}; Collapsed
      TextBlock                 empty
    ContentPresenter "ContentPresenter"
                                Column 1; Margin: TemplateBinding Padding; H/VAlignment: TemplateBinding H/VContentAlignment;
                                Content, ContentTemplate, FontFamily, FontSize, Foreground: TemplateBinding
```

The two ellipses have `IsHitTestVisible="False"`. And ours, in
`src/Cornflower.Themes.Wp81/Controls/RadioButton.axaml`, written new:

```
Panel "Root"                    Background Transparent; untilted, takes the hits
  Grid "Container"              as the phone's; Tilt.IsTarget: the part the tilt draws on
    Panel                       the phone's unnamed Grid: one cell, which a Panel lays out the same; 25.5 wide, Left
      Ellipse "CheckBackground" as the phone's
      Ellipse "CheckMark"       as the phone's
      PhoneLineSpacer           the empty TextBlock: one of the phone's lines tall (below)
    ContentPresenter "PART_ContentPresenter"
                                as the phone's, its font and foreground inherited; the TextBlock it makes of data
                                gets the phone's line
```

The phone's template already has an untilted root around the tilted `Container`, the shape Button's theme adds: its
unnamed root `Grid` is transparent and takes the hits, overhang included, and `PointerDownThemeAnimation` targets
`Container` inside the overhang. So ours has the same shape, and `Container` is marked `Tilt.IsTarget`
([CLAUDE.md](../../../CLAUDE.md), `Tilt`), as the CheckBox theme (PR #92) marks its `Grid`.

**The circle and the dot are ellipses**, as the template's are: no glyph. Avalonia's `Ellipse`, like WinRT's, draws
its stroke inside its bounds, so the ring is 2.5 wide inside the 25.5 circle, 6 raw pixels, on both.

**The empty TextBlock** is the phone's, and kept, as a spacer. It is one line of the RadioButton's font tall
(24.1276 at 18.14, Segoe WP's 1.33 em), shorter than the 25.5 circle at the default size, so it changes nothing there.
With a larger `FontSize` its line is taller than the circle, the cell grows to it, and the circle, centred in the cell,
is centred on the text's first line: at 30, the line is 39.9 and the circle 7 down. Ours is `PhoneLineSpacer`
(internal to the theme, `src/Cornflower.Themes.Wp81/PhoneLineSpacer.cs`): nothing wide, `PhoneTextLine.FontLine`
(1.33 em) of its inherited `FontSize` tall, drawing nothing. Not a `TextBlock`: in Avalonia an app's
`<Style Selector="TextBlock">` reaches a `TextBlock` in a template, and its margin or size would move the circle (the
first review's finding).

**The cell is 25.5 wide and left-aligned**, where the phone's stretches across the 25.5 column. Avalonia rounds the
column to the nearest raw pixel and the circle up (below), so a cell as wide as the column centres the dot on the
column across and on the circle down: at 3.0 the column is 76 raw pixels, the circle 77, and the dot would be 19 in
and 20 down. The cell at the circle's width centres it on the circle both ways (the first review's finding). At 2.4
it changes no pixel.

**The content's font** is the RadioButton's: the phone's presenter binds `FontFamily`, `FontSize` and `Foreground` to
it (`TemplateBinding`), unlike the phone's CheckBox's, which sets its own font. In Avalonia the three are inherited, so ours sets
none of them on the presenter, as Button's theme doesn't, with the same result. So `<RadioButton FontSize="30">`
draws its text at 30, on the phone and here. The presenter makes data into a `TextBlock` with Segoe WP's line,
1.33 em: the dump's `ContentPresenter` is 24.1667 tall. Ours gets the same line through `PhoneContentLine`, as
Button's does (ADR 0003).

### The visual states

`radiobutton.json`, `visualStateGroups`: `CommonStates` and `CheckStates`, both on the root `Grid`. **No
transitions.**

| State | What it sets (`radiobutton.json`) | Resource value | Avalonia |
|---|---|---|---|
| `Normal` | nothing | | the plain setters |
| `MouseOver`, `PointerOver` | nothing | | no style for `:pointerover` |
| `Pressed` | `PointerDownThemeAnimation` on `Container`; `CheckBackground.Fill` = `ButtonPressedBackgroundThemeBrush`; `CheckMark.Fill` = `ButtonPressedForegroundThemeBrush` | `SystemColorControlAccentColor`, Cobalt `#FF3E65FF` by default; `PhoneTextOverAccentColor`, `#FFFFFFFF` in both | `^:pressed`, and `Tilt` |
| `Disabled` | `CheckBackground.Stroke` and `CheckMark.Fill` = `ButtonDisabledBorderThemeBrush`; `ContentPresenter.Foreground` = `ButtonDisabledForegroundThemeBrush` | `PhoneBaseLowColor`, `#73FFFFFF` dark, `#40000000` light, both | `^:disabled` |
| `Checked` | `CheckMark.Visibility` = `Visible` | | `^:checked` |
| `Unchecked` | nothing | | the dot hidden |
| `Indeterminate` | nothing | | the dot hidden: drawn as unchecked |

The states use the **Button's** brushes, not the `RadioButton*ThemeBrush` keys in `themeresources.json`
(`RadioButtonPressedBackgroundThemeBrush` is `PhoneForegroundColor`, not the accent): the template is what the phone
draws, and the Pressed PNGs are the accent round a white dot. `Pressed` leaves the ring and the text alone;
`Disabled` leaves the circle's fill alone, so an app's `Background` stays.

**The release.** The template has no transitions, so no `PointerUpThemeAnimation` runs on leaving `Pressed`, as
HyperlinkButton's has none (#29). In WinRT, leaving a state with no transition stops its storyboard, which would put
`Container` back at once. That is a reading of the template; no probe has pressed a RadioButton. So the theme keeps
`Tilt`'s release, the Button's measured hold and ease, as HyperlinkButton's does (see "Decided on the user's
behalf").

## Avalonia's RadioButton against the phone's

Avalonia 12.1.4's `RadioButton` is a `ToggleButton` with `GroupName`, as WinRT's is. It raises `:pressed`,
`:disabled`, `:pointerover`, `:checked`, `:unchecked` and `:indeterminate`; a click checks it and never unchecks it
(its `Toggle` sets `IsChecked` true only), as WinRT's `OnToggle`; checking one unchecks the others in its group, by
`GroupName` across the window or, with none, by its parent (`RadioButtonGroupManager`), as WinRT groups them. It
looks for `PART_ContentPresenter`. Measured with the theme against the captures:

| Difference | The phone | Avalonia's RadioButton | Rung |
|---|---|---|---|
| The style and template | Above | Simple's: another circle, dot, padding and colours | 1, theme |
| The states | Above | Simple's | 1, theme |
| The tilt | `PointerDownThemeAnimation` on `Container` | none | 2, the `Tilt` attached behaviour, set by the theme |
| The release | no transition: believed to snap back (not measured) | none | 2, `Tilt`'s eased release, kept until measured |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine` and `PhoneTextLine`, internal to the theme |
| `Foreground` | inherited from the page: `PhoneForegroundBrush` | inherited from the window, whose Simple foreground isn't the phone's | 1, a setter (decided below) |
| Focus | no `FocusStates` group | a focus adorner | 1, the theme clears `FocusAdorner` |
| Access keys | none | Avalonia's themes recognise them | 1, kept as Avalonia's, as in Button: `_radio` draws `radio` and Alt+R checks it |
| The group | one checked per `GroupName`, or per parent | the same | none: Avalonia's, tested under the theme |
| Rounding | the nearest raw pixel | up | ADR 0003's recorded deviation; the sizes below |

No type is substituted. Rungs 1 and 2 only.

### Where rounding up differs from the phone's

ADR 0003 asks each plan to list them. At 2.4:

| Size | Template | Raw pixels | The phone | Ours | What shows |
|---|---|---|---|---|---|
| `MinWidth` | 168 | 403.2 | 403, 167.9167 | 404, 168.3333 | Nothing drawn: the RadioButton's transparent bounds |
| The circle, `CheckBackground` | 25.5 × 25.5 | 61.2 | 61, 25.4167 | 62, 25.8333 | The ring's right and bottom sides one raw pixel further out |
| The circle's column | 25.5 | 61.2 | 61 | 61 | Nothing: Avalonia's `Grid` rounds a column to the nearest, so the content's left is the phone's, 33.3333 |
| The circle's cell | stretched across the column | | 61 | 62 | Nothing: ours is 25.5 wide, so the dot is centred on the circle both ways (above) |
| The dot's place | centred | | 16, 16: 6.6667 (15.5 across, in the 61 cell, rounded up) | 16, 16: 6.6667 (16 in the 62 cell) | Nothing: the dot is on the phone's pixels |
| The empty TextBlock, `PhoneLineSpacer` | 24.1276 | 57.9 | 24.1276 | 24.1667 | Nothing at the default size |

`SceneTests.Layout_Rounds_A_Size_Up_Where_The_Phone_Rounds_To_The_Nearest` holds the rounding.

## Other controls' templates

**No template of Simple's holds a plain `RadioButton`**, so `HoleParts.FallBack` (#30, PR #93) isn't needed and the
theme doesn't set it. Probed: every control type Simple themes in the Avalonia assemblies Simple refers to (75, every
one shown on its own with Simple alone; ManagedFileChooser's CheckBox is the only toggle of either kind), and read:
Simple's sources and `Avalonia.Dialogs`' at the `12.1.4` tag hold no `RadioButton`. The ColorPicker's Simple theme
(a separate package, not referenced here) holds two, each with a `Theme` of its own
(`ColorViewColorModelRadioButtonTheme`), so Wp81's wouldn't reach them.

## Extensions

None. The theme declares nothing; the tilt is `Tilt`'s, already public.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/RadioButtonTests.cs` unless named.

1. **The style's values**, dark and light: the ten setters, the foreground, the overhang, the ellipses' brushes and
   stroke (`radiobutton.json`, the keys' values, the dump). `The_Style_Sets_What_The_Phone_Read`.
2. **The text's font is the RadioButton's**: its `FontSize` and `FontFamily` reach the text.
   `The_Texts_Font_Is_The_RadioButtons`.
3. **The bounds**: the RadioButton at 19.1667, 12.0833, 57.5 tall; `Container` 15.8333 down; the circle at its top
   left; the dot 6.6667 in and down, 12.5 across; the empty TextBlock's line; the content at 33.3333, 15.8333,
   24.1667 tall (the dump), with the rounding above. `Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding`.
4. **A larger font centres the circle on the text's first line**, through the empty TextBlock, `PhoneLineSpacer`,
   and back at the default size the cell is the circle again; **an app's `TextBlock` style doesn't move the circle**;
   **the dot is centred on the circle across and down alike** at 1.75, 2.4 and 3.0.
   `A_Larger_Font_Centres_The_Circle_On_The_Texts_First_Line`, `An_Apps_TextBlock_Style_Doesnt_Move_The_Circle`,
   `The_Dot_Is_Centred_On_The_Circle_At_Any_Scale`.
5. **Data's line is the phone's**, 1.33 em; an app's `ContentTemplate` is used; access keys.
   `A_Strings_Line_Is_The_Phones`, `The_Apps_ContentTemplate_Is_Used`,
   `An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Checks_It`.
6. **The ring is drawn where the phone draws it**: every ring pixel on the middle row and column, and the page just
   outside and inside, against the PNGs, dark and light (right and bottom one raw pixel out).
   `The_Ring_Is_Drawn_Where_The_Phone_Draws_It`.
7. **Checked shows the dot, on the phone's pixels; unchecked none; indeterminate is drawn as unchecked**, pixel for
   pixel, as the phone's PNGs are. `Checked_Shows_The_Dot_And_Unchecked_None`, `Indeterminate_Is_Drawn_As_Unchecked`.
8. **Pressed** fills the circle with the accent, the dot white over it; the ring and the text stay; back on release,
   checked. `Pressed_Fills_The_Circle_With_The_Accent`, `Pressed_Draws_The_Accent_Round_The_Dot`,
   `The_Pressed_Circle_Is_The_Accent_Brush`.
9. **Disabled** dims the ring, the dot and the text to `PhoneBaseLowColor` (`#FF737373` and `#FFBFBFBF` on the page)
   and keeps an app's background. `Disabled_Dims_The_Ring_The_Dot_And_The_Text`, `Disabled_Keeps_The_Apps_Background`.
10. **The pointer over it changes nothing**: the phone's `PointerOver` and `MouseOver` PNGs equal `base`'s.
    `The_Pointer_Over_It_Changes_Nothing`.
11. **A tap checks it, and another leaves it checked.** `A_Tap_Checks_It_And_Another_Leaves_It_Checked`.
12. **A tap anywhere on it checks it**: the overhang above and below, and the text. `A_Tap_Anywhere_On_It_Checks_It`.
13. **A press tilts `Container`, inside an untilted root.** `A_Press_Tilts_The_Container_Inside_An_Untilted_Root`.
14. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
15. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
16. **One checked per group, under the theme**: a tap on another in the same parent moves the dot, and leaves another
    parent's alone; a `GroupName` makes a group across parents. `A_Tap_On_Another_In_Its_Parent_Moves_The_Dot`,
    `A_GroupName_Makes_A_Group_Across_Parents`.
17. **Holes**: `RadioButton` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
18. **No global rule**: `StyleBudgetTests`, unchanged.
19. **The sample's page**: in the catalogue, every RadioButton on it the theme's, its header from its title, its
    first group's choice shown, its named group across two panels (`Sample/RadioButtonPageTests.cs`).

## Decided on the user's behalf

- **The theme sets `Foreground` to `PhoneForegroundBrush`**, which the phone's style doesn't: there the RadioButton
  inherits it from the page. As the CheckBox theme (PR #92) does, for the same reason: Wp81 doesn't theme `Window`, and Simple's
  window foreground isn't the phone's. Cost: an app's ancestor `Foreground` doesn't reach the RadioButton; set it on
  the RadioButton. To reverse: once Wp81 themes the window with `PhoneForegroundBrush`, drop the setter.
- **The release eases back, as the Button's does**, though the template has no transition (above). As
  HyperlinkButton's theme decided (#29). To reverse, once a probe shows a snap: a way in `Tilt` to turn the return
  off, set by this theme.
- **The empty TextBlock is kept, as `PhoneLineSpacer`**, a control with no template that measures one of the
  phone's lines, rather than a `TextBlock` (which an app's `TextBlock` style would reach): it is what centres the
  circle on a larger font's first line. **A deliberate difference, unmeasured:** on the phone an app-wide implicit
  `TextBlock` style (in `App.xaml`) may reach the template's empty `TextBlock` and move the circle; here none does
  (`An_Apps_TextBlock_Style_Doesnt_Move_The_Circle` holds ours, not the phone's). And the spacer is 1.33 em of any
  font, where the phone's `TextBlock` takes the line of the RadioButton's own `FontFamily`: the same for Segoe WP, the
  default. To reverse: a `TextBlock` with `PhoneTextLine.MaxHeightLineHeight` at its smallest in its place; or delete
  it from the template, and the cell is the circle's height at any font size.
- **The circle's cell is 25.5 wide and left-aligned**, not stretched across the column as the phone's: Avalonia's
  rounding would otherwise put the dot half a raw pixel off the circle's centre on one axis at some scales. No pixel
  changes at 2.4. To reverse: drop the cell's `Width` and `HorizontalAlignment`.

## Dependencies and captures

- `Tilt` (#22, #65), tokens (#19), `PhoneContentLine` (#27) and `PhoneTextLine` (#26), the desktop sample (#9):
  merged. All 13 resources the style reads are in `Tokens/`.
- Captures used: `base`, `CommonStates.PointerOver`, `CommonStates.Pressed`, `CommonStates.Disabled`, for checked and
  unchecked, dark and light, run `20261108-1443-dev11`. The run's other RadioButton captures are byte for byte one of
  these.
- **The Pressed capture is a forced state with no finger**: its colours check the theme; its tilt doesn't.
- **Owed:** a tilt probe on a RadioButton (its target, `Container`, is 168 × 25.4, as flat as CheckBox's; the release
  with no transition; a press in the overhang, outside the target); and the device run on the Pixel 8.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.


> Generated by `dotnet run --project tools/winrt-api -- checklist RadioButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.RadioButton`; Avalonia `Avalonia.Controls.RadioButton`.

### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `GroupName` | `String` | `RadioButton.GroupName` | Avalonia's stands: one checked per name across the window, or per parent with none (Behaviour 16) |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ToggleButton` (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsThreeState` | `Boolean` | `ToggleButton.IsThreeState` | Avalonia's stands; a tap never makes a RadioButton indeterminate, on either |
| [x] | `IsChecked` | `Boolean?` | `ToggleButton.IsChecked` | Avalonia's stands; drives `:checked`; null is drawn as unchecked (Behaviour 7) |

### Events, from `ToggleButton` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Checked` | `RoutedEventHandler` | none | Avalonia's `IsCheckedChanged` (one event for the three); WinRT's three events aren't declared. Recorded as missing |
| [-] | `Indeterminate` | `RoutedEventHandler` | none | As `Checked` |
| [-] | `Unchecked` | `RoutedEventHandler` | none | As `Checked` |

### Methods, from `ToggleButton` (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnToggle()` (protected, overridable) | none | Avalonia's `ToggleButton.Toggle()` (protected virtual), which `RadioButton` overrides to check only, as WinRT's |

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands; without `CanExecute` the RadioButton is `:disabled` and drawn as Disabled |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's stands (`Release`, `Press`; WinRT's `Hover` has no Avalonia member) |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; nothing is drawn for it, as on the phone (Behaviour 10) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; drives `:pressed` (Behaviour 8) |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's stands; a tap anywhere on the RadioButton, the overhang included, checks it (Behaviour 12) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent (mapping rules, theme animations). The template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` in `ContentTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's (Behaviour 5) |
| [x] | `Content` | `Object` | `ContentControl.Content` | Template-bound to the presenter, as the phone's |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not declared: `Presenter.Child` is Avalonia's |

### Events, from `ContentControl` (0)

_none_

### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Not declared: Avalonia's `OnPropertyChanged` for `Content` |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | Not declared: as `OnContentChanged` |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Not declared: no `ContentTemplateSelector` |

### Default style: setters (10)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource PhoneRadioCheckBoxBrush}` | `TemplatedControl.Background` | Setter, `DynamicResource`; the circle's fill |
| [x] | `BorderBrush` | `{ThemeResource PhoneRadioCheckBoxBorderBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource`; the ring |
| [x] | `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource`; inherited by the presenter, as the phone's binds it (Behaviour 2) |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`: Selawik for Segoe WP; inherited by the presenter |
| [x] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Setter |
| [x] | `VerticalContentAlignment` | `Top` | `ContentControl.VerticalContentAlignment` | Setter |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Top` | `Layoutable.VerticalAlignment` | Setter |
| [x] | `Padding` | `{ThemeResource CheckBoxAndRadioButtonTextPaddingThickness}` | `TemplatedControl.Padding` | Setter, `DynamicResource`; the presenter's `Margin`, as the phone's |
| [x] | `MinWidth` | `{ThemeResource CheckBoxAndRadioButtonMinWidthSize}` | `Layoutable.MinWidth` | Setter, `DynamicResource`. 168 rounds up to 168.3333 (ADR 0003) |

### Template parts (4)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Container` | `Grid` | none | A `Grid` named `Container`, two columns (25.5, *), in the overhang, marked `Tilt.IsTarget`, inside the untilted `Root` that takes the hits |
| [x] | `CheckBackground` | `Ellipse` | none | An `Ellipse`, 25.5 across, left and centred; the RadioButton's `Background` inside a `PhoneStrokeThickness` ring in its `BorderBrush`; the states set its fill and stroke |
| [x] | `CheckMark` | `Ellipse` | none | An `Ellipse`, 12.5 across, centred, in `PhoneRadioCheckBoxCheckBrush`, shown by `:checked` |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, as Avalonia's ContentControl looks for it; its font and foreground the RadioButton's, as the phone's |

### Visual states (8)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `MouseOver` | nothing | no rule | No style, as the phone changes nothing (Behaviour 10) |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style, as the phone changes nothing (Behaviour 10) |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on Container`<br>`CheckBackground.Fill = {ThemeResource ButtonPressedBackgroundThemeBrush}`<br>`CheckMark.Fill = {ThemeResource ButtonPressedForegroundThemeBrush}` | `^:pressed` | `^:pressed` setters on `CheckBackground` and `CheckMark`; the tilt by `Tilt.IsEnabled` on `Container` |
| [x] | `CommonStates` | `Disabled` | `CheckBackground.Stroke = {ThemeResource ButtonDisabledBorderThemeBrush}`<br>`CheckMark.Fill = {ThemeResource ButtonDisabledBorderThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ButtonDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled` setters on `CheckBackground`, `CheckMark` and `PART_ContentPresenter` |
| [x] | `CheckStates` | `Checked` | `CheckMark.Visibility = Visible` | `^:checked` | `^:checked` shows `CheckMark` |
| [x] | `CheckStates` | `Unchecked` | nothing | `^:unchecked` | Nothing: the dot hidden |
| [x] | `CheckStates` | `Indeterminate` | nothing | `^:indeterminate` | Nothing: the dot hidden, drawn as unchecked (Behaviour 7) |

### Transitions (0)

_none_. `Tilt`'s release runs on leaving `Pressed` all the same (see "The release" and "Decided on the user's behalf").

### Resources (13)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `PhoneRadioCheckBoxBrush` | `SolidColorBrush Transparent` | same | yes | `Background` |
| [x] | `PhoneRadioCheckBoxBorderBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | `BorderBrush` |
| [x] | `TextStyleLargeFontSize` | `18.14` | same | yes | `FontSize` |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | `FontFamily` |
| [x] | `CheckBoxAndRadioButtonTextPaddingThickness` | `8,0,0,0` | same | yes | `Padding` |
| [x] | `CheckBoxAndRadioButtonMinWidthSize` | `168` | same | yes | `MinWidth` |
| [x] | `ButtonPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `:pressed`, the circle's fill; follows an app's accent |
| [x] | `ButtonPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | `:pressed`, the dot's fill |
| [x] | `ButtonDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the ring and the dot |
| [x] | `ButtonDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the presenter's `Foreground` |
| [x] | `PhoneTouchTargetLargeOverhang` | `0,16` | same | yes | `Container`'s `Margin` |
| [x] | `PhoneStrokeThickness` | `2.5` | same | yes | `CheckBackground`'s `StrokeThickness` |
| [x] | `PhoneRadioCheckBoxCheckBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | `CheckMark`'s fill |
