# ToggleButton: plan

[#30](https://github.com/wdcossey/CornflowerUI/issues/30), with [RepeatButton](../RepeatButton/PLAN.md). The 8.1
ToggleButton, Avalonia's `ToggleButton` themed as the phone's: its default style and template as an Avalonia
`ControlTheme`, and the phone's look in each of its twelve states, dark and light. This is the plain ToggleButton's
theme only: CheckBox (#31), RadioButton and ToggleSwitch derive from it in both Avalonia and WinRT, but each has its
own style, and Avalonia finds a theme by the control's style key, its own type, so none of them takes this one.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `ToggleButton`,
`R:\cornflower-reference\flash-update1\xbf-json\togglebutton.json`, and the values of its keys from
`themeresources.json` beside it. These equal the SDK's design files, from which the checklist below is generated
([CLAUDE.md](../../../CLAUDE.md), "Reference material"). Each value is cited by its source: `togglebutton.json` for a
setter, a part or a state, and the resource key for a value.

The captures **check** the theme. They are the reference app's, run `20261108-1443-dev11` on the reference profile
(device 11, 1080 × 1920 at 2.4), committed in [`ref/wp81/ToggleButton/`](../../../ref/wp81/ToggleButton/README.md).
What a real touch does (the states it goes through, and what a slide off it does) is read from a probe of this PR's,
run `20261112-0755-dev11` (below, [Behaviour under a finger](#behaviour-under-a-finger)).

## The phone's style

`togglebutton.json`: one unkeyed style for `ToggleButton`, with the same twelve setters as Button's
([Button's plan](../Button/PLAN.md#the-phones-style)), and a template.

| Setter | Value | Dump, dark / light |
|---|---|---|
| `Background` | `Transparent` | `#00FFFFFF` |
| `BorderBrush`, `Foreground` | `{ThemeResource PhoneForegroundBrush}` | `#FFFFFFFF` / `#FF000000` |
| `BorderThickness` | `{ThemeResource PhoneBorderThickness}`, 2.5 | 2.5 on each side |
| `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `Segoe WP` |
| `FontWeight` | `{ThemeResource PhoneButtonFontWeight}`, Semibold | 600 |
| `FontSize` | `{ThemeResource TextStyleLargeFontSize}`, 18.14 | 18.14 |
| `Padding` | `{ThemeResource PhoneButtonContentPadding}`, 9.5,0,9.5,3.5 | the same |
| `MinHeight`, `MinWidth` | `{ThemeResource PhoneButtonMinHeight}` 57.5, `{ThemeResource PhoneButtonMinWidth}` 109 | the same |
| `HorizontalAlignment`, `VerticalAlignment` | `Left`, `Center` | the same |

As for Button, the dump reads the content alignment as `Center`, `Center`, WinRT's defaults; Avalonia's are `Stretch`,
so the theme sets `Center`. The probe read `ClickMode` `Release` and `IsThreeState` `False`, Avalonia's too.

### The template

`togglebutton.json`, `template.tree`:

```
Grid                          Background Transparent
  Border "EnabledBackground"  Background, BorderBrush, BorderThickness: TemplateBinding; Margin {ThemeResource PhoneTouchTargetOverhang}
    ContentPresenter "EnabledContent"
                              Foreground, Content, ContentTemplate: TemplateBinding; Margin: TemplateBinding Padding; H/V alignment: the content alignment
  Border "DisabledBackground" IsHitTestVisible False; Background Transparent; BorderBrush {ThemeResource ButtonDisabledBorderThemeBrush};
                              BorderThickness: TemplateBinding; Margin {ThemeResource PhoneTouchTargetOverhang}; Visibility Collapsed
    ContentPresenter "DisabledContent"
                              Foreground {ThemeResource ButtonDisabledForegroundThemeBrush}; the rest as EnabledContent
```

And ours, in `src/Cornflower.Themes.Wp81/Controls/ToggleButton.axaml`, written new:

```
Panel                         Background Transparent; RenderOptions.RequiresFullOpacityHandling (below)
  Border "EnabledBackground"  as the phone's
    ContentPresenter "PART_ContentPresenter"
                              the phone's EnabledContent; the TextBlock it makes of data gets the phone's line (Button's PhoneContentLine)
```

**One Border and one presenter, not two of each.** The phone shows the content twice, once in each presenter, and
collapses one. Avalonia can't put one element in two presenters (content that is a control has one parent), so the
second would throw for an app's own element content. The two Borders lie in the same place, with the same margin and
thickness, so ours draws the Disabled look by setting on the one Border and presenter what `DisabledBackground` and
`DisabledContent` draw: a transparent background, `ButtonDisabledBorderThemeBrush` and
`ButtonDisabledForegroundThemeBrush`. The pixels are the same (tested). The presenter is named `PART_ContentPresenter`,
the name Avalonia's `ContentControl` looks for.

No tilt: unlike Button's, the template runs no `PointerDownThemeAnimation` or `PointerUpThemeAnimation`, and no state
is tilted in the captures (Button's forced `Pressed` is, at −17.19°, 17.19°). The roadmap's "tilt" for this row
doesn't hold for the phone's ToggleButton.

### The visual states

`togglebutton.json`, `visualStateGroups`: one group, `CommonStates`, on the `Grid`, with twelve states. Every
animation is an `ObjectAnimationUsingKeyFrames` with one `DiscreteObjectKeyFrame` at `KeyTime` 0: a setter. No
transitions.

| State | What it sets | Resource value | Ours |
|---|---|---|---|
| `Normal`, `PointerOver`, `Pressed` | nothing | | the plain setters; no `:pointerover` or `:pressed` style |
| `Indeterminate`, `IndeterminatePointerOver`, `IndeterminatePressed` | nothing | | the plain setters: `:indeterminate` is never `:checked` |
| `Checked`, `CheckedPointerOver` | `EnabledBackground.Background` = `PhoneAccentBrush`; `EnabledContent.Foreground` = `PhoneButtonBasePressedForegroundBrush`; and three setters on the disabled parts | `SystemColorControlAccentColor`, Cobalt `#FF3E65FF`; `PhoneTextOverAccentColor`, `#FFFFFFFF` in both | `^:checked:not(:pressed), ^:checked[(wp81|PhonePress.IsOff)=True]` |
| `CheckedPressed` | nothing | | the plain setters: checked, pressed and the pressing pointer on it matches neither of the above |
| `Disabled`, `IndeterminateDisabled` | `EnabledBackground` collapsed, `DisabledBackground` visible | `ButtonDisabledBorderThemeBrush`, `ButtonDisabledForegroundThemeBrush`: `PhoneBaseLowColor`, `#73FFFFFF` dark, `#40000000` light | `^:disabled:not(:checked)`: the Border transparent, its border and the text in those brushes |
| `CheckedDisabled` | `EnabledBackground.Background` = `PhoneAccentBrush`, `EnabledBackground.Opacity` = 0.4, `EnabledContent.Foreground` = `PhoneButtonBasePressedForegroundBrush` | | `^:checked:disabled`: the opacity; the accent and the text come from the checked style, as Avalonia's Button is never pressed while disabled |

`Checked` and `CheckedPointerOver` also set `DisabledBackground.Background` and `BorderBrush` to `PhoneDisabledBrush` and
`DisabledContent.Foreground` to `PhoneBackgroundBrush`. They are never seen: `DisabledBackground` is collapsed in every
state but `Disabled` and `IndeterminateDisabled`, and only one state of the group runs at a time. So ours has nothing to
set for them (the dumps of `Checked` read them on a collapsed Border with no size).

**`CheckedPressed` sets nothing**, so a checked ToggleButton under a finger draws the unchecked look, and its release
unchecks it. **`Pressed` sets nothing**, so an unchecked one under a finger changes nothing drawn. This is the template,
and the probe saw the phone go through these states (below).

**The 0.4 of `CheckedDisabled`** is on the `EnabledBackground` Border, so the phone draws the Border and its content as
one layer at 0.4: the border `#FF666666` dark, `#FF999999` light, the fill `#FF192866` and `#FFB2C1FF`, the text the same
grey as the border. Avalonia multiplies a visual's opacity into each thing it draws, unless
`RenderOptions.RequiresFullOpacityHandling` is set where the opacity is pushed, its parent: then Skia draws it as a layer.
Without it, the border's inner half, drawn over the fill, comes out `#FF747EA3` (measured). So the root `Panel` sets it.

## Behaviour under a finger

Run `20261112-0755-dev11`, device 11, with `-Probe repeat`: a probe of this PR's, kept outside the repo beside the run
(`Probe.Repeat.cs`; "Proposed for shared files" in the PR). A ToggleButton (`Content = "toggle"`, 109.1667 × 57.5) on a
dark page, with its states, pointer events, `Click`, `Checked` and `Unchecked` logged, touched for real:

| Touch | What the phone did |
|---|---|
| A tap (150 ms), unchecked | `Normal` › `PointerOver` › `Pressed`; on the lift `Checked` and `Click` (`IsChecked` true), then `PointerOver` › `CheckedPointerOver` › `Checked` |
| Held 800 ms, checked | `Checked` › `CheckedPointerOver` › `CheckedPressed` for the whole hold; on the lift `Unchecked`, `Click`, then `Normal` |
| Held 800 ms, unchecked | `Pressed` for the hold, then `Checked` |
| Checked, pressed, slid 90 down and lifted there | `CheckedPressed`; **`CheckedPressed` › `Checked` as the finger crossed the bottom edge** (60.03 below the top, the control 57.5 tall); the lift off it neither clicked nor toggled |

## Avalonia's ToggleButton against the phone's

Avalonia 12.1.4's `ToggleButton` is a `Button` (so `:pressed`, `:pointerover`, `:disabled`, `ClickMode`, `Command`)
that toggles `IsChecked` on `Click` and raises `:checked`, `:unchecked` and `:indeterminate`. Measured with this
theme, and against the captures and the probe:

| Difference | The phone | Avalonia's ToggleButton | Rung |
|---|---|---|---|
| The style | the setters above | Simple's | 1, theme |
| The states | the table above | Simple's | 1, theme |
| Two parts per look | `EnabledBackground`/`EnabledContent` and `DisabledBackground`/`DisabledContent` | one element can't be in two presenters | 1: one Border and presenter, restyled for Disabled; the same pixels |
| A press on a checked one | the unchecked look while pressed | `:checked:pressed` | 1, theme: the accent only when not pressed, or pressed with the pointer off it |
| Sliding off while pressed | `CheckedPressed` › `Checked` at the edge | stays `:pressed`. Its `:pointerover` can't tell: it isn't set under a touch at all, nor for a press from the keyboard (tested) | 2, `PhonePress`, internal to the theme: `IsOff`, the pressing pointer off the control, judged as Avalonia's Button judges a release; `:checked[IsOff=True]` draws the accent |
| The lift off it | doesn't toggle | doesn't click (Avalonia's Button clicks on release only over itself) | nothing: the same |
| `CheckedDisabled`'s 0.4 | the Border and its content as one layer | each thing drawn at 0.4 | 1, theme: `RenderOptions.RequiresFullOpacityHandling` on the root |
| The tilt | none | none | nothing |
| Focus | no `FocusStates`: nothing drawn | the theme under Wp81 draws a focus rectangle when `FocusAdorner` is unset | 1, the theme clears `FocusAdorner` |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine`, as Button's |
| Content alignment | `Center`, `Center` | `Stretch`, `Stretch` | 1, theme setters |
| Access keys | none | recognised | 1, kept as Avalonia's, as Button's: `_Toggle` draws `Toggle` and Alt+T toggles it |
| ToggleButtons in templates written for the theme under Wp81 | not applicable | Simple's ComboBox (its drop-down toggle) and DrawerPage (three) hold plain ToggleButtons, which would take Wp81's theme by type: 109 × 57.5 at least, in a ComboBox | 2, `HoleParts`, extended in this PR from Button to any Button subclass by its style key (below) |
| CheckBox, RadioButton, ToggleSwitch, `CommandBarToggleButton` | their own styles | their own style keys | nothing: none takes this theme (tested, every subclass in Avalonia's assemblies and ours) |
| `Checked`, `Unchecked`, `Indeterminate` events | three events | `IsCheckedChanged` | not declared (no extension): `IsCheckedChanged` and `IsChecked` say the same |
| Layout rounding | the nearest raw pixel | up to the raw pixel | ADR 0003's recorded deviation; none shows here |

No type is substituted. Rung 1 and 2 only.

### PhonePress

`PhonePress` (`src/Cornflower.Themes.Wp81/PhonePress.cs`, internal, new in this PR, shared with RepeatButton) follows
the pointer that pressed the control, from the press to the release, and sets `IsOff` while that pointer is off it: no
part of the control under it, as Avalonia's Button decides a release doesn't click. Only that pointer counts. With no
pointer press (Space), nothing is off. The ToggleButton theme turns it on and reads `IsOff` in its checked selector.

### HoleParts, by style key

`HoleParts` (`src/Cornflower.Themes.Wp81/HoleParts.cs`, #27) gives a plain Button in a template written for the theme
under Wp81 the Button theme under Wp81. It looked that theme up by `typeof(Button)`, so a ToggleButton would have taken
Simple's *Button* theme. It now looks it up by the control's own style key, and takes it only if it targets that key:
a ToggleButton takes the ToggleButton theme under Wp81, a RepeatButton the RepeatButton theme, a Button the Button theme
as before. The attached property is registered on Button, so it reaches its subclasses; this theme and RepeatButton's set
it, as Button's does. A shared file, changed with the coordinator's approval. Button's tests and `HolePartsTests` are
unchanged and green; `Completeness/HoleTogglePartsTests.cs` holds the new cases.

## Extensions

None.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ToggleButtonTests.cs` unless named.

1. **The style's values**, dark and light: the setters, the content alignment, the overhang, the padding; `ClickMode`
   Release, `IsThreeState` false (the probe). `The_Style_Sets_What_The_Phone_Read`.
2. **The bounds**: at 19.1667, 27.5 on the stage, 57.5 tall; the Border 9.5833 down, 38.3333 tall; the content 15 down,
   24.1667 tall (the dumps). `Its_Bounds_Are_The_Phones`.
3. **Unchecked**: the border where the PNGs draw it, the page inside. `Unchecked_The_Border_Is_Drawn_Where_The_Phone_Draws_It`.
4. **Checked**: the accent inside, the text over it, the border unchanged; the app's accent. `Checked_Fills_With_The_Accent_And_Draws_The_Text_Over_It`, `Checked_Follows_The_Apps_Accent`.
5. **Pressed and CheckedPressed show the unchecked look, and the release toggles.** `A_Press_Shows_The_Unchecked_Look_And_The_Release_Toggles`.
6. **Sliding off a pressed checked one shows it checked; the lift off it keeps it**, by mouse and by touch; a held Space shows the unchecked look. `Sliding_Off_A_Pressed_Checked_One_Shows_It_Checked_And_The_Lift_Off_It_Keeps_It`, `Under_A_Finger_Checked_Looks_Unchecked_Until_It_Slides_Off`, `Held_Space_On_A_Checked_One_Shows_The_Unchecked_Look`.
7. **The pointer over it changes nothing**, checked or not. `The_Pointer_Over_It_Changes_Nothing`.
8. **Disabled** dims the border and the text, transparent inside whatever the app set (`#FF737373`, `#FFBFBFBF`).
   `Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does`.
9. **CheckedDisabled** is the checked look at 0.4 as one layer: the PNGs' border and fill within a step.
   `Checked_And_Disabled_Is_The_Checked_Look_At_Four_Tenths`.
10. **Indeterminate looks unchecked; IndeterminateDisabled looks disabled.** `Indeterminate_Looks_Unchecked_And_Disabled_Looks_Disabled`.
11. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
12. **No tilt.** `A_Press_Doesnt_Tilt`.
13. **The overhang takes taps.** `A_Tap_On_The_Overhang_Toggles`.
14. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
15. **Data's line is the phone's; access keys toggle.** `Datas_Line_Is_The_Phones_And_An_Access_Key_Toggles`.
16. **No subclass takes this theme.** `A_Subclass_Doesnt_Take_The_ToggleButton_Theme` (each concrete subclass),
    `The_Subclasses_Include_The_Ones_That_Matter`.
17. **ToggleButtons in templates written for the theme under Wp81 keep that theme's ToggleButton**, not its Button:
    every hole, ComboBox's toggle, an app's own template, no theme for the type under Wp81
    (`Completeness/HoleTogglePartsTests.cs`).
18. **Holes**: `ToggleButton` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
19. **No global rule**: `StyleBudgetTests`, unchanged.
20. **The sample's page**: in the catalogue, every ToggleButton on it the theme's, its header from its title, the
    default's state shown (`Sample/ToggleAndRepeatButtonPageTests.cs`).

## Dependencies and captures

- Button (#27) for `PhoneContentLine` and `HoleParts`; tokens (#19): merged. All 15 resources are in `Tokens/`.
- Captures used: every state, both variants, dark and light, run `20261108-1443-dev11`; the probe, run
  `20261112-0755-dev11`.
- **The captures are forced states**: no finger. The probe's real touches show the same states in the same order the
  template's look gives.
- **Not captured**: a frame of a real press (it would look like the unchecked capture), and a slide back onto the
  ToggleButton while still pressed. Ours goes back to the unchecked look, by the template's rule; the probe lifted off it.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist ToggleButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.Primitives.ToggleButton`; Avalonia `Avalonia.Controls.Primitives.ToggleButton`.

### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsThreeState` | `Boolean` | `ToggleButton.IsThreeState` | Avalonia's stands; false by default, as the probe read |
| [x] | `IsChecked` | `Boolean?` | `ToggleButton.IsChecked` | Avalonia's stands; drives `:checked`, `:unchecked`, `:indeterminate` (Behaviours 4, 10) |

### Events (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Checked` | `RoutedEventHandler` | none | Not declared: Avalonia 12's `IsCheckedChanged` with `IsChecked` says the same |
| [-] | `Indeterminate` | `RoutedEventHandler` | none | Not declared: as `Checked` |
| [-] | `Unchecked` | `RoutedEventHandler` | none | Not declared: as `Checked` |

### Methods (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnToggle()` (protected, overridable) | `ToggleButton.Toggle()` (protected virtual) | Avalonia's stands, under its own name |

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's stands: Release, as the probe read |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; draws nothing. Not set under a touch, so the slide off is `PhonePress.IsOff`'s (Behaviour 6) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; the unchecked look while pressed over it (Behaviour 5) |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's stands; toggles, and a lift off the control doesn't click (Behaviour 6) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent; the template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's |
| [x] | `Content` | `Object` | `ContentControl.Content` | Template-bound to the one presenter (the phone binds it to two) |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not declared: `Presenter.Child` is Avalonia's |

### Events, from `ContentControl` (0)

_none_

### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Not declared: Avalonia's `OnPropertyChanged` for `Content` |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | Not declared: as `OnContentChanged` |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Not declared: no `ContentTemplateSelector` |

### Default style: setters (12)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Setter, `Transparent` |
| [x] | `BorderBrush` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource` |
| [x] | `Foreground` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.Foreground` | Setter, `DynamicResource` |
| [x] | `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource` |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`: Selawik for Segoe WP |
| [x] | `FontWeight` | `{ThemeResource PhoneButtonFontWeight}` | `TemplatedControl.FontWeight` | Setter, `DynamicResource` |
| [x] | `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource` |
| [x] | `Padding` | `{ThemeResource PhoneButtonContentPadding}` | `TemplatedControl.Padding` | Setter, `DynamicResource`; the presenter's `Margin` |
| [x] | `MinHeight` | `{ThemeResource PhoneButtonMinHeight}` | `Layoutable.MinHeight` | Setter, `DynamicResource` |
| [x] | `MinWidth` | `{ThemeResource PhoneButtonMinWidth}` | `Layoutable.MinWidth` | Setter, `DynamicResource` |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | Setter |

### Template parts (4)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `EnabledBackground` | `Border` | none | A `Border` named `EnabledBackground`; the states set it, Disabled's look included |
| [x] | `EnabledContent` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, as Avalonia's `ContentControl` looks for it |
| [-] | `DisabledBackground` | `Border` | none | Not made: its look is drawn by `EnabledBackground` when disabled, in the same place (the content can't be in two presenters) |
| [-] | `DisabledContent` | `ContentPresenter` | none | Not made: as `DisabledBackground`; its foreground is set on `PART_ContentPresenter` when disabled |

### Visual states (12)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style |
| [x] | `CommonStates` | `Pressed` | nothing | `^:pressed` | No style (Behaviour 5) |
| [x] | `CommonStates` | `Disabled` | `EnabledBackground.Visibility = Collapsed`<br>`DisabledBackground.Visibility = Visible` | `^:disabled` | `^:disabled:not(:checked)`: `EnabledBackground` drawn as `DisabledBackground` (Behaviour 8) |
| [x] | `CommonStates` | `Checked` | `EnabledBackground.Background = {ThemeResource PhoneAccentBrush}`<br>`EnabledContent.Foreground = {ThemeResource PhoneButtonBasePressedForegroundBrush}`<br>`DisabledBackground.Background = {ThemeResource PhoneDisabledBrush}`<br>`DisabledBackground.BorderBrush = {ThemeResource PhoneDisabledBrush}`<br>`DisabledContent.Foreground = {ThemeResource PhoneBackgroundBrush}` | `^:checked` | `^:checked:not(:pressed), ^:checked[(wp81|PhonePress.IsOff)=True]`, the first two; the three on the collapsed disabled parts are never seen |
| [x] | `CommonStates` | `CheckedPointerOver` | as `Checked` | `^:checked:pointerover` | The same style |
| [x] | `CommonStates` | `CheckedPressed` | nothing | `^:checked:pressed` | No style: the plain setters while pressed over it (Behaviours 5, 6) |
| [x] | `CommonStates` | `CheckedDisabled` | `EnabledBackground.Background = {ThemeResource PhoneAccentBrush}`<br>`EnabledBackground.Opacity = 0.4`<br>`EnabledContent.Foreground = {ThemeResource PhoneButtonBasePressedForegroundBrush}` | `^:checked:disabled` | The opacity on `^:checked:disabled`; the accent and the text from the checked style (a disabled Button is never pressed); one layer (Behaviour 9) |
| [x] | `CommonStates` | `Indeterminate` | nothing | `^:indeterminate` | No style (Behaviour 10) |
| [x] | `CommonStates` | `IndeterminatePointerOver` | nothing | `^:indeterminate:pointerover` | No style |
| [x] | `CommonStates` | `IndeterminatePressed` | nothing | `^:indeterminate:pressed` | No style |
| [x] | `CommonStates` | `IndeterminateDisabled` | `EnabledBackground.Visibility = Collapsed`<br>`DisabledBackground.Visibility = Visible` | `^:indeterminate:disabled` | `^:disabled:not(:checked)`, as `Disabled` (Behaviour 10) |

### Transitions (0)

_none_

### Resources (15)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `PhoneForegroundBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | `BorderBrush`, `Foreground` |
| [x] | `PhoneBorderThickness` | `2.5` | same | yes | `BorderThickness` |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | `FontFamily` |
| [x] | `PhoneButtonFontWeight` | `Semibold` | same | yes | `FontWeight` |
| [x] | `TextStyleLargeFontSize` | `18.14` | same | yes | `FontSize` |
| [x] | `PhoneButtonContentPadding` | `9.5,0,9.5,3.5` | same | yes | `Padding` |
| [x] | `PhoneButtonMinHeight` | `57.5` | same | yes | `MinHeight` |
| [x] | `PhoneButtonMinWidth` | `109` | same | yes | `MinWidth` |
| [x] | `PhoneAccentBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `:checked` (disabled too), the Border's `Background`; follows an app's accent |
| [x] | `PhoneButtonBasePressedForegroundBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | `:checked` (disabled too), the presenter's `Foreground` |
| [-] | `PhoneDisabledBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | Not used: set by `Checked` on the collapsed `DisabledBackground`, never seen |
| [-] | `PhoneBackgroundBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | Not used: as `PhoneDisabledBrush` |
| [x] | `PhoneTouchTargetOverhang` | `0,9.5` | same | yes | The Border's `Margin` |
| [x] | `ButtonDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled:not(:checked)`, the Border's `BorderBrush` |
| [x] | `ButtonDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled:not(:checked)`, the presenter's `Foreground` |
