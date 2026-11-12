# CheckBox: plan

[#31](https://github.com/wdcossey/CornflowerUI/issues/31). The 8.1 CheckBox, Avalonia's `CheckBox` themed as the
phone's: its default style and template as an Avalonia `ControlTheme`, the tick as Cornflower's own glyph, the tilt
on the part the phone tilts, and the phone's colours in each state, dark and light.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `CheckBox`,
`R:\cornflower-reference\flash-update1\xbf-json\checkbox.json`, and the values of its keys from
`themeresources.json` beside it. The runtime's templates and theme resources equal the SDK's design files
([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the checklist, generated from the SDK, names the same
keys.

The captures **check** the theme; they don't supply values. They are the reference app's, run
`20261108-1443-dev11` on the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/CheckBox/`](../../../ref/wp81/CheckBox/README.md). The tick's geometry, `Wp81.Glyph.CheckMark`, was
measured from the same run's checked CheckBox in #20 and is tested against it in `tests/Cornflower.Tests/Glyphs/`.

## The phone's style

`checkbox.json`: one unkeyed style for `CheckBox`, eleven setters and a template.

| Setter | Value | Resource value (`themeresources.json`) | Dump, dark / light |
|---|---|---|---|
| `Background` | `{ThemeResource CheckBoxBackgroundThemeBrush}` | `#00FFFFFF` | `#00FFFFFF` |
| `BorderBrush` | `{ThemeResource CheckBoxBorderThemeBrush}` | `PhoneForegroundColor`: `#FFFFFFFF` dark, `#FF000000` light | `#FFFFFFFF` / `#FF000000` |
| `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `2.5` | `2.5` on each side |
| `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `18.14` | 18.14 |
| `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `Segoe WP` | `Segoe WP` |
| `HorizontalContentAlignment` | `Left` | | `Left` |
| `VerticalContentAlignment` | `Top` | | `Top` |
| `HorizontalAlignment` | `Left` | | `Left` |
| `VerticalAlignment` | `Top` | | `Top` |
| `Padding` | `{ThemeResource CheckBoxAndRadioButtonTextPaddingThickness}` | `8,0,0,0` | the same |
| `MinWidth` | `{ThemeResource CheckBoxAndRadioButtonMinWidthSize}` | `168` | 168 |

The style sets no `Foreground`. The dump reads `#FFFFFFFF` dark and `#FF000000` light, `PhoneForegroundColor`:
what the CheckBox inherits from the page. Wp81 doesn't theme Avalonia's `Window`, and under Simple a window's
`Foreground` isn't the phone's, so the theme sets `Foreground` to `PhoneForegroundBrush` (see "Decided on the
user's behalf" below).

### The template

`checkbox.json`, `template.tree`:

```
Grid                            Background Transparent
  Grid "Grid"                   Margin {ThemeResource PhoneTouchTargetLargeOverhang} (0,16); columns 25.5, *
    Grid                        Column 0; VerticalAlignment Top
      Border "CheckBackground"  25.5 × 25.5, Left, Center; Background, BorderBrush, BorderThickness: TemplateBinding
      Rectangle "NormalRectangle"  13 × 13, centred; Fill {ThemeResource CheckBoxBackgroundThemeBrush}; Collapsed
      Path "CheckGlyph"         18.5 × 17, Stretch Fill, centred; Fill {ThemeResource CheckBoxForegroundThemeBrush}; Collapsed
    ContentPresenter "ContentPresenter"
                                Column 1; Margin: TemplateBinding Padding; H/VAlignment: TemplateBinding H/VContentAlignment;
                                Foreground: TemplateBinding; FontFamily {ThemeResource PhoneFontFamilyNormal};
                                FontSize {ThemeResource TextStyleLargeFontSize}; FontWeight Normal
```

The box, the rectangle and the tick have `IsHitTestVisible="False"`. And ours, in
`src/Cornflower.Themes.Wp81/Controls/CheckBox.axaml`, written new:

```
Panel "Root"                    Background Transparent; untilted, takes the hits
  Grid "Grid"                   as the phone's; Tilt.IsTarget: the part the tilt draws on
    Panel                       the phone's unnamed Grid: one cell, which a Panel lays out the same
      Border "CheckBackground"  as the phone's
      Rectangle "NormalRectangle"  as the phone's
      Path "CheckGlyph"         Data {DynamicResource Wp81.Glyph.CheckMark}, at the glyph's own size, centred
    ContentPresenter "PART_ContentPresenter"
                                as the phone's; the TextBlock it makes of data gets the phone's line
```

The phone's template already has an untilted root around the tilted `Grid`, the shape Button's theme adds: its
unnamed root `Grid` is transparent and takes the hits, overhang included, and `PointerDownThemeAnimation` targets
`Grid` inside the overhang. So ours has the same shape, and `Grid` is marked `Tilt.IsTarget`
([CLAUDE.md](../../../CLAUDE.md), `Tilt`). The tilt's geometry is `Grid`'s bounds: the box and the text, not the
overhang.

**The tick** is `Wp81.Glyph.CheckMark`, Cornflower's own outline measured from the checked capture (#20); the
phone's path data isn't used (rule 6). The phone's `CheckGlyph` is 18.5 × 17 with `Stretch="Fill"`, and the phone
arranges it 18.33 × 17.08, rounding to the nearest raw pixel; the glyph's own size is 18.31 × 17.08, that arranged
shape. Avalonia would round 18.5 up to 18.75 (45 raw pixels to the phone's 44) and stretch the tick 2% wider. So
ours has no size and no stretch: the glyph is drawn at its own size, 18.33 × 17.08 once rounded, the phone's.

**The rectangle** for `Indeterminate` is filled with `CheckBoxBackgroundThemeBrush`, transparent. So on the phone
an indeterminate CheckBox at rest is drawn as an unchecked one, pixel for pixel (the captures' hashes are equal),
and pressed it is the accent on the accent. Ours is the same. An app that sets `CheckBoxBackgroundThemeBrush` sees
it, as on the phone.

**The content's font** is set on the presenter, not bound: `PhoneFontFamilyNormal` at `TextStyleLargeFontSize`
(the type ramp's base size, as `BaseTextBlockStyle` has), `Normal`. So the CheckBox's own `FontSize`,
`FontFamily` and `FontWeight` don't reach the text, on the phone or here (see "Decided on the user's behalf"). The
presenter makes data into a `TextBlock` with Segoe WP's line, 1.33 em: the dump's `ContentPresenter` is 24.1667
tall. Ours gets the same line through `PhoneContentLine`, as Button's does (ADR 0003).

### The visual states

`checkbox.json`, `visualStateGroups`: `CommonStates` and `CheckStates`, both on `Grid`.

| State | What it sets (`checkbox.json`) | Resource value | Avalonia |
|---|---|---|---|
| `Normal` | nothing | | the plain setters |
| `MouseOver`, `PointerOver` | nothing | | no style for `:pointerover` |
| `Pressed` | `PointerDownThemeAnimation` on `Grid`; `CheckBackground.Background` and `NormalRectangle.Fill` = `CheckBoxPressedBackgroundThemeBrush`; `CheckGlyph.Fill` = `CheckBoxPressedForegroundThemeBrush` | `SystemColorControlAccentColor`, Cobalt `#FF3E65FF` by default; `PhoneTextOverAccentColor`, `#FFFFFFFF` in both | `^:pressed`, and `Tilt` |
| `Disabled` | `CheckBackground.BorderBrush` = `CheckBoxDisabledBorderThemeBrush`; `CheckGlyph.Fill` and `ContentPresenter.Foreground` = `CheckBoxDisabledForegroundThemeBrush`; `NormalRectangle.Fill` = `CheckBoxDisabledBackgroundThemeBrush` | `PhoneBaseLowColor`, `#73FFFFFF` dark, `#40000000` light; `#00FFFFFF` | `^:disabled` |
| `Checked` | `CheckGlyph.Visibility` = `Visible` | | `^:checked` |
| `Unchecked` | nothing | | both marks hidden |
| `Indeterminate` | `NormalRectangle.Visibility` = `Visible` | | `^:indeterminate` |

`Pressed` leaves the text and the border alone; `Disabled` leaves the box's background alone, unlike Button's, so an
app's `Background` stays. The transitions, `Pressed` → `PointerOver`, `PointerOver` → `Normal` and `Pressed` →
`Normal`, each run `PointerUpThemeAnimation` on `Grid`: the tilt's return, which `Tilt` runs on release.

## Avalonia's CheckBox against the phone's

Avalonia 12.1.4's `CheckBox` is a `ToggleButton`, so a `Button`: it raises `:pressed`, `:disabled`,
`:pointerover`, `:checked`, `:unchecked` and `:indeterminate`, toggles on click, cycles checked, indeterminate,
unchecked when `IsThreeState` (WinRT's order), and looks for `PART_ContentPresenter`. Measured with the theme against
the captures:

| Difference | The phone | Avalonia's CheckBox | Rung |
|---|---|---|---|
| The style and template | Above | Simple's: another box, tick, padding and colours | 1, theme |
| The states | Above | Simple's | 1, theme |
| The tilt | `PointerDownThemeAnimation` and `PointerUpThemeAnimation` on `Grid` | none | 2, the `Tilt` attached behaviour, set by the theme |
| The tick | the template's path | Simple's | 1, `Wp81.Glyph.CheckMark` (#20) |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine`, internal to the theme |
| `Foreground` | inherited from the page: `PhoneForegroundBrush` | inherited from the window, whose Simple foreground isn't the phone's | 1, a setter (decided below) |
| Focus | no `FocusStates` group | a focus adorner | 1, the theme clears `FocusAdorner` |
| Access keys | none | Avalonia's themes recognise them | 1, kept as Avalonia's, as in Button: `_check` draws `check` and Alt+C toggles |
| Rounding | the nearest raw pixel | up | ADR 0003's recorded deviation; the sizes below |

No type is substituted. Rungs 1 and 2 only.

### Where rounding up differs from the phone's

ADR 0003 asks each plan to list them. At 2.4:

| Size | Template | Raw pixels | The phone | Ours | What shows |
|---|---|---|---|---|---|
| `MinWidth` | 168 | 403.2 | 403, 167.9167 | 404, 168.3333 | Nothing drawn: the CheckBox's transparent bounds |
| The box, `CheckBackground` | 25.5 × 25.5 | 61.2 | 61, 25.4167 | 62, 25.8333 | The border's right and bottom sides one raw pixel further out |
| The box's column | 25.5 | 61.2 | 61 | 61 | Nothing: Avalonia's `Grid` rounds a column to the nearest, so the content's left is the phone's, 33.3333 |
| The tick's left in the box | centred: 8.5 raw | | 9, 3.75 | 8, 3.3333 | The tick one raw pixel left of the phone's: Avalonia rounds the half down |
| `NormalRectangle` | 13 × 13 | 31.2 | 31, 12.9167 | 32, 13.3333 | Nothing at the default resources: it is transparent, or the accent on the accent |

`SceneTests.Layout_Rounds_A_Size_Up_Where_The_Phone_Rounds_To_The_Nearest` holds the rounding.

## Extensions

None. The theme declares nothing; the tilt is `Tilt`'s, already public.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/CheckBoxTests.cs` unless named.

1. **The style's values**, dark and light: the eleven setters, the foreground, the overhang, the parts' brushes and
   the glyph (`checkbox.json`, the keys' values, the dump). `The_Style_Sets_What_The_Phone_Read`.
2. **The bounds**: the CheckBox at 19.1667, 12.0833, 57.5 tall; `Grid` 15.8333 down; the box at its top left; the
   tick 4.1667 down in it, 18.3333 × 17.0833; the content at 33.3333, 15.8333, 24.1667 tall (the dump), with the
   rounding above. `Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding`.
3. **The text's font is the presenter's own**: the CheckBox's `FontSize`, `FontFamily` and `FontWeight` don't reach
   it. `The_Texts_Font_Is_The_Presenters_Own_As_On_The_Phone`.
4. **Data's line is the phone's**, 1.33 em; an app's `ContentTemplate` is used; access keys.
   `A_Strings_Line_Is_The_Phones`, `The_Apps_ContentTemplate_Is_Used`,
   `An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Toggles_It`.
5. **The box is drawn where the phone draws it**: every border pixel on the read row and column, and the page just
   outside and inside, against the PNGs, dark and light (right and bottom one raw pixel out).
   `The_Box_Is_Drawn_Where_The_Phone_Draws_It`.
6. **Checked shows the tick, unchecked none; indeterminate shows a transparent rectangle**, so its frame is the
   unchecked one's, pixel for pixel, as the phone's PNGs are; an app's `CheckBoxBackgroundThemeBrush` fills it.
   `Checked_Shows_The_Tick_And_Unchecked_None`, `Indeterminate_Shows_A_Transparent_Rectangle_So_It_Looks_Unchecked`,
   `An_Apps_Background_Key_Fills_The_Rectangle`.
7. **Pressed** fills the box with the accent, the tick white over it, the rectangle the accent; the border and the
   text stay; back on release, toggled. `Pressed_Fills_The_Box_With_The_Accent`,
   `Pressed_Draws_The_Accent_Round_The_Tick`, `The_Pressed_Box_Is_The_Accent_Brush`.
8. **Disabled** dims the border, the tick and the text to `PhoneBaseLowColor` (`#FF737373` and `#FFBFBFBF` on the
   page) and keeps an app's background. `Disabled_Dims_The_Border_The_Tick_And_The_Text`,
   `Disabled_Keeps_The_Apps_Background`.
9. **The pointer over it changes nothing**: the phone's `PointerOver` and `MouseOver` PNGs equal `base`'s.
   `The_Pointer_Over_It_Changes_Nothing`.
10. **A tap toggles, and the marks follow**, through indeterminate when three-state.
    `A_Tap_Toggles_And_The_Marks_Follow`.
11. **A press tilts `Grid`, inside an untilted root.** `A_Press_Tilts_The_Grid_Inside_An_Untilted_Root`.
12. **A tap anywhere on it toggles**: the overhang above and below, and the text. `A_Tap_Anywhere_On_It_Toggles`.
13. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
14. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
15. **Holes**: `CheckBox` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
16. **No global rule**: `StyleBudgetTests`, unchanged.
17. **The sample's page**: in the catalogue, every CheckBox on it the theme's, its header from its title, its state
    shown (`Sample/CheckBoxPageTests.cs`).
18. **A CheckBox in a hole's template keeps the theme under Wp81**: every hole shown with Simple then Wp81 has as many
    plain CheckBoxes as with Simple alone, each in Simple's CheckBox theme (today only `ManagedFileChooser`'s), not
    the phone's size and no tilt; in an app's own template, or an app's theme for a hole, it is the phone's
    (`CheckBoxHolePartsTests.cs`).

## Decided on the user's behalf

- **The theme sets `Foreground` to `PhoneForegroundBrush`**, which the phone's style doesn't: there the CheckBox
  inherits it from the page. Wp81 doesn't theme `Window`, and Simple's window foreground isn't the phone's, so
  inheriting would draw the text in Simple's colour. Cost: an app's ancestor `Foreground` doesn't reach the CheckBox;
  set it on the CheckBox. To reverse: once Wp81 themes the window with `PhoneForegroundBrush`, drop the setter.
- **The text's font is the presenter's own, as on the phone**, so `<CheckBox FontSize="30">` doesn't change the
  text. Faithful, and surprising to an Avalonia developer. To reverse: bind the presenter's `FontFamily` and
  `FontSize` to the CheckBox's (`TemplateBinding`), whose setters hold the same keys.
- **The tick is drawn at the glyph's own size** rather than the template's 18.5 × 17 stretched (above). To reverse:
  give `CheckGlyph` `Width="18.5" Height="17" Stretch="Fill"`.

## Dependencies and captures

- `Tilt` (#22, #65), tokens (#19), `PhoneContentLine` (#27), the template glyphs (#20), the desktop sample (#9):
  merged. All 14 resources the style reads are in `Tokens/`.
- Captures used: `base`, `CommonStates.PointerOver`, `CommonStates.Pressed`, `CommonStates.Disabled`, for checked,
  unchecked and indeterminate, dark and light, run `20261108-1443-dev11`. The run's other CheckBox captures are
  byte for byte one of these.
- **The Pressed capture is a forced state with no finger**: its colours check the theme; its tilt doesn't.
- **Owed:** a tilt probe on a CheckBox (its target, `Grid`, is 168 × 25.4, flatter than any Button probed; and a
  press in the overhang, outside the target, takes the tilt of the nearest edge here, unmeasured on the phone); the
  pressed colours while a finger slides off; and the device run on the Pixel 8.
- **Other controls' templates**: of Simple's templates, only `ManagedFileChooser` (the file dialogs' "show hidden
  files") holds a plain `CheckBox` (probed: every control type Simple themes in Avalonia.Controls and
  Avalonia.Dialogs, built with Simple alone). The theme sets `HoleParts.FallBack`, so since #93, which falls back by
  the control's own type, it takes Simple's CheckBox theme there (Behaviour 18).

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.


> Generated by `dotnet run --project tools/winrt-api -- checklist CheckBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.CheckBox`; Avalonia `Avalonia.Controls.CheckBox`.

### Properties (0)

_none_

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ToggleButton` (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsThreeState` | `Boolean` | `ToggleButton.IsThreeState` | Avalonia's stands; with it a tap goes checked, indeterminate, unchecked, WinRT's order (Behaviour 10) |
| [x] | `IsChecked` | `Boolean?` | `ToggleButton.IsChecked` | Avalonia's stands; drives `:checked`, `:unchecked`, `:indeterminate` (Behaviours 5, 6) |

### Events, from `ToggleButton` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Checked` | `RoutedEventHandler` | none | Avalonia's `IsCheckedChanged` (one event for the three); WinRT's three events aren't declared. Recorded as missing |
| [-] | `Indeterminate` | `RoutedEventHandler` | none | As `Checked` |
| [-] | `Unchecked` | `RoutedEventHandler` | none | As `Checked` |

### Methods, from `ToggleButton` (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnToggle()` (protected, overridable) | none | Avalonia's `ToggleButton.Toggle()` (protected virtual), the same role under its own name |

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands; without `CanExecute` the CheckBox is `:disabled` and drawn as Disabled |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's stands (`Release`, `Press`; WinRT's `Hover` has no Avalonia member) |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; nothing is drawn for it, as on the phone (Behaviour 9) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; drives `:pressed` (Behaviour 7) |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's stands; a tap anywhere on the CheckBox, the overhang included, toggles it (Behaviour 12) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent (mapping rules, theme animations). The template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` in `ContentTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's (Behaviour 4) |
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

### Default style: setters (11)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource CheckBoxBackgroundThemeBrush}` | `TemplatedControl.Background` | Setter, `DynamicResource`; the box's background |
| [x] | `BorderBrush` | `{ThemeResource CheckBoxBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource`; the box's border |
| [x] | `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource`; the box's border |
| [x] | `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource`. The phone's presenter sets its own, so it doesn't reach the text (Behaviour 3) |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`: Selawik for Segoe WP. As `FontSize`, the presenter sets its own |
| [x] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Setter |
| [x] | `VerticalContentAlignment` | `Top` | `ContentControl.VerticalContentAlignment` | Setter |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Top` | `Layoutable.VerticalAlignment` | Setter |
| [x] | `Padding` | `{ThemeResource CheckBoxAndRadioButtonTextPaddingThickness}` | `TemplatedControl.Padding` | Setter, `DynamicResource`; the presenter's `Margin`, as the phone's |
| [x] | `MinWidth` | `{ThemeResource CheckBoxAndRadioButtonMinWidthSize}` | `Layoutable.MinWidth` | Setter, `DynamicResource`. 168 rounds up to 168.3333 (ADR 0003) |

### Template parts (5)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Grid` | `Grid` | none | A `Grid` named `Grid`, two columns (25.5, *), in the overhang, marked `Tilt.IsTarget`, inside the untilted `Root` that takes the hits |
| [x] | `CheckBackground` | `Border` | none | A `Border`, 25.5 square, left and centred; the states set its background and border |
| [x] | `NormalRectangle` | `Rectangle` | none | A `Rectangle`, 13 square, centred, shown by `:indeterminate`; filled with `CheckBoxBackgroundThemeBrush` |
| [x] | `CheckGlyph` | `Path` | none | A `Path` of `Wp81.Glyph.CheckMark`, centred, at the glyph's own size (the phone's arranged 18.33 × 17.08), shown by `:checked` |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, as Avalonia's ContentControl looks for it; its own font as the phone's |

### Visual states (8)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `MouseOver` | nothing | no rule | No style, as the phone changes nothing (Behaviour 9) |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style, as the phone changes nothing (Behaviour 9) |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on Grid`<br>`CheckBackground.Background = {ThemeResource CheckBoxPressedBackgroundThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxPressedForegroundThemeBrush}`<br>`NormalRectangle.Fill = {ThemeResource CheckBoxPressedBackgroundThemeBrush}` | `^:pressed` | `^:pressed` setters on `CheckBackground`, `CheckGlyph` and `NormalRectangle`; the tilt by `Tilt.IsEnabled` on `Grid` |
| [x] | `CommonStates` | `Disabled` | `CheckBackground.BorderBrush = {ThemeResource CheckBoxDisabledBorderThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxDisabledForegroundThemeBrush}`<br>`NormalRectangle.Fill = {ThemeResource CheckBoxDisabledBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource CheckBoxDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled` setters on `CheckBackground`, `CheckGlyph`, `NormalRectangle` and `PART_ContentPresenter` |
| [x] | `CheckStates` | `Checked` | `CheckGlyph.Visibility = Visible` | `^:checked` | `^:checked` shows `CheckGlyph` |
| [x] | `CheckStates` | `Unchecked` | nothing | `^:unchecked` | Nothing: both marks hidden |
| [x] | `CheckStates` | `Indeterminate` | `NormalRectangle.Visibility = Visible` | `^:indeterminate` | `^:indeterminate` shows `NormalRectangle` |

### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on Grid` | `Tilt`'s return on release: hold 182.6 ms, ease back over 167 ms |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on Grid` | The same; with nothing tilted it does nothing |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on Grid` | The same |

### Resources (14)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `CheckBoxBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | `Background`; `NormalRectangle.Fill` |
| [x] | `CheckBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | `BorderBrush` |
| [x] | `PhoneBorderThickness` | `2.5` | same | yes | `BorderThickness` |
| [x] | `TextStyleLargeFontSize` | `18.14` | same | yes | `FontSize`, and the presenter's |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | `FontFamily`, and the presenter's |
| [x] | `CheckBoxAndRadioButtonTextPaddingThickness` | `8,0,0,0` | same | yes | `Padding` |
| [x] | `CheckBoxAndRadioButtonMinWidthSize` | `168` | same | yes | `MinWidth` |
| [x] | `CheckBoxPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `:pressed`, the box's background and the rectangle's fill; follows an app's accent |
| [x] | `CheckBoxPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | `:pressed`, the tick's fill |
| [x] | `CheckBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the box's border |
| [x] | `CheckBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the tick's fill and the presenter's `Foreground` |
| [x] | `CheckBoxDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | `:disabled`, the rectangle's fill |
| [x] | `PhoneTouchTargetLargeOverhang` | `0,16` | same | yes | `Grid`'s `Margin` |
| [x] | `CheckBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | The tick's fill |

