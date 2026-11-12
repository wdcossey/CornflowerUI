# RepeatButton: plan

[#30](https://github.com/wdcossey/CornflowerUI/issues/30), with [ToggleButton](../ToggleButton/PLAN.md). The 8.1
RepeatButton, Avalonia's `RepeatButton` themed as the phone's: its default style and template as an Avalonia
`ControlTheme`, its look in each state, dark and light, and its timing: when it clicks, how soon it repeats, and that it
stops while the finger is off it.

## Where the values come from

The style is read from the 8.1 Update 1 runtime's default style for `RepeatButton`,
`R:\cornflower-reference\flash-update1\xbf-json\repeatbutton.json`, and the values of its keys from
`themeresources.json` beside it, as for [Button](../Button/PLAN.md#where-the-values-come-from). The captures, run
`20261108-1443-dev11` on the reference profile, are in [`ref/wp81/RepeatButton/`](../../../ref/wp81/RepeatButton/README.md).

The timing isn't in the style: `Delay`, `Interval` and `ClickMode` are the control's own defaults, in native code.
They were read in the emulator, through the API and under a real touch, by a probe of this PR's: run
`20261112-0755-dev11`, device 11, `-Probe repeat` (`Probe.Repeat.cs`, kept beside the run outside the repo; "Proposed
for shared files" in the PR). Microsoft's documentation says 250 and 250 for both; WinUI 3's source says 500 and 33. The
phone says 500 and 33.

## The phone's style

`repeatbutton.json`: one unkeyed style for `RepeatButton`. Its twelve setters are Button's, value for value (the
[ToggleButton plan](../ToggleButton/PLAN.md#the-phones-style) has them with the dump's readings, which are the same
here), and its template is Button's without the tilt:

```
Grid                          Background Transparent (unnamed: Button's is named Grid, for its theme animations)
  Border "Border"             BorderBrush, BorderThickness, Background: TemplateBinding; Margin {ThemeResource PhoneTouchTargetOverhang}
    ContentPresenter "ContentPresenter"
                              Foreground, Content, ContentTemplate: TemplateBinding; Margin: TemplateBinding Padding; H/V alignment: the content alignment
```

And ours, in `src/Cornflower.Themes.Wp81/Controls/RepeatButton.axaml`, written new:

```
Panel                         Background Transparent: takes the hits on the overhang
  Border "Border"             as the phone's
    ContentPresenter "PART_ContentPresenter"
                              the phone's ContentPresenter, under the name Avalonia's ContentControl looks for; data's line is the phone's (PhoneContentLine)
```

No untilted root around a tilted part, as Button's has: nothing tilts.

### The visual states

`repeatbutton.json`, `visualStateGroups`: one group, `CommonStates`. Every animation is a discrete keyframe at 0: a
setter. **No theme animation and no transitions**, so no tilt: the forced `Pressed` capture is flat, where Button's is
tilted by the same forcing.

| State | What it sets | Resource value | Ours |
|---|---|---|---|
| `Normal`, `PointerOver` | nothing | | the plain setters; no `:pointerover` style |
| `Pressed` | `ContentPresenter.Foreground` = `ButtonPressedForegroundThemeBrush`; `Border.Background` = `ButtonPressedBackgroundThemeBrush` | `#FFFFFFFF` in both; the accent | `^:pressed` |
| `Disabled` | `ContentPresenter.Foreground` = `ButtonDisabledForegroundThemeBrush`; `Border.BorderBrush` = `ButtonDisabledBorderThemeBrush`; `Border.Background` = `ButtonDisabledBackgroundThemeBrush` | `PhoneBaseLowColor`; transparent | `^:disabled` |

## The timing, from the probe

A RepeatButton (`Content = "repeat"`, 109.1667 × 57.5) on a dark page, its states, pointer events and every `Click`
logged with the app's stopwatch, touched for real:

| | The phone |
|---|---|
| `Delay`, `Interval`, `ClickMode`, read through the API | **500**, **33**, **`Press`** (Avalonia's: 300, 100, `Release`) |
| A tap (150 ms), and holds of 300 ms | one click, on the press (logged before the press event itself), none on the lift |
| Held 3 s | a click on the press; the first repeat **519 ms** after it (517 and 516.7 in the other holds); then 40 repeats **59.95 ms** apart on average, 57.9 to 61.2 |
| Held 600 ms | the press, then repeats at 517 and 576.5 ms |
| Held, slid 90 down (off it) after a second, held there a second, slid back, held | repeats every 60 ms until the finger crossed the bottom edge; **none while it was off**; the first again 10 ms after it came back, then every 60 ms: no second delay |
| The state while slid off | stayed `Pressed`: no state change was logged as it left (the ToggleButton's left `CheckedPressed` at the edge) |

So the phone clicks at the press, waits `Delay`, and repeats while the finger is on it. **The repeats came every 60 ms,
not every 33**: `Interval` reads 33, and the first repeat came 17 to 19 ms after `Delay` too. Both are the emulator's
timer; which of the two to reproduce is a decision (below).

## Avalonia's RepeatButton against the phone's

Avalonia 12.1.4's `RepeatButton` is a `Button` that starts a `DispatcherTimer` on a left press (or Space), at `Delay`,
then `Interval`, raising `Click` (and the `Command`) on each tick, and stops it when `IsPressed` goes false.

| Difference | The phone | Avalonia's RepeatButton | Rung |
|---|---|---|---|
| The style and states | as above | Simple's | 1, theme |
| The tilt | none | none | nothing |
| `ClickMode` | `Press`: a click at the press, none at the lift | `Release`: the timer starts at the press, a click at the lift | 1, theme setter `Press` |
| `Delay` | 500 | 300 | 1, theme setter 500 |
| `Interval` | 33 (read); the emulator repeated every 60 | 100 | 1, theme setter 33: the API's value. Ours repeats every 33.0 (measured headless, 28 gaps, 32.9 to 33.0), the first after 502 |
| Off the button while held | no repeats; the next tick after it comes back clicks | repeats wherever the pointer is | 2, `PhoneRepeat`, internal to the theme: a class handler marks a repeat's `Click` handled while the pressing pointer is off the button, so neither the app's handlers nor the `Command` run; the timer keeps running, as the phone's does |
| The look while slid off | stays `Pressed` | stays `:pressed` | nothing: the same |
| Focus | no `FocusStates` | a focus rectangle when `FocusAdorner` is unset | 1, the theme clears `FocusAdorner` |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine`, as Button's |
| Content alignment | `Center`, `Center` | `Stretch`, `Stretch` | 1, theme setters |
| Access keys | none | recognised | 1, kept, as Button's |
| RepeatButtons in templates written for the theme under Wp81 | not applicable | Simple's ScrollBar (eight, both orientations) and its menu ScrollViewer (two) hold plain RepeatButtons, which would take Wp81's theme by type: every scroll bar's arrows 109 × 57.5 | 2, `HoleParts` by style key ([ToggleButton's plan](../ToggleButton/PLAN.md#holeparts-by-style-key)) |

No type is substituted. Rung 1 and 2 only.

### Testing the timing

The repeats run on Avalonia's `DispatcherTimer`, private to `RepeatButton`, on real time: `CornflowerClock` and the
tests' `TestClock` don't reach it, and `RunJobs` doesn't run it (a timer comes due only in the dispatcher's loop, so the
tests run the loop with a `DispatcherFrame`). Reproducing the timer on `CornflowerClock` would mean a part class or a
subclass to replace Avalonia's timing, for a timing the theme reaches with three setters. So the tests hold:

- the values, exactly: `ClickMode` `Press`, `Delay` 500, `Interval` 33;
- on real time, only what a slow machine can't break: the press clicks at once, no repeat sooner than `Delay` after it,
  none closer than `Interval` (each less 2 ms), and that they come;
- off the button: nothing while off for ten intervals and more, with `Delay` raised to a minute once the repeats have
  begun, so that a repeat after coming back proves there was no second delay.

## Extensions

None.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/RepeatButtonTests.cs` unless named.

1. **The style's values**, dark and light, and the timing's: `ClickMode` Press, `Delay` 500, `Interval` 33.
   `The_Style_Sets_What_The_Phone_Read`.
2. **The bounds**, as the dumps'. `Its_Bounds_Are_The_Phones`.
3. **The border's pixels**, dark and light. `The_Border_Is_Drawn_Where_The_Phone_Draws_It`.
4. **Pressed** fills with the accent and draws the text over it, the PNGs' pixels; back on release.
   `Pressed_Fills_With_The_Accent_And_Draws_The_Text_Over_It`.
5. **Disabled** dims the border and the text and takes the app's background away. `Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does`.
6. **The pointer over it changes nothing.** `The_Pointer_Over_It_Changes_Nothing`.
7. **No tilt.** `A_Press_Doesnt_Tilt`.
8. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
9. **A tap clicks once, at the press**, on the overhang too. `A_Tap_Clicks_Once_On_The_Press_The_Overhang_Too`.
10. **A held press repeats after `Delay`, at `Interval`.** `A_Held_Press_Repeats_After_The_Delay_At_The_Interval`.
11. **Off the button the repeats stop, and come back with the finger**, without a second delay; the `Command` too.
    `Off_The_Button_The_Repeats_Stop_And_Come_Back_With_The_Finger`; `PhoneRepeat` is what does it:
    `Without_PhoneRepeat_Avalonias_Repeats_Go_On_Off_The_Button`.
12. **The keyboard's clicks aren't held back.** `Space_Clicks_Whatever_The_Pointer_Did_Before`.
13. **RepeatButtons in templates written for the theme under Wp81 keep that theme's**, and without `PhoneRepeat`: a
    ScrollBar's, every hole's (`Completeness/HoleTogglePartsTests.cs`).
14. **Holes**: `RepeatButton` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
15. **No global rule**: `StyleBudgetTests`, unchanged.
16. **The sample's page**: in the catalogue, every RepeatButton it places the theme's, the scroll bars' not, the count
    (`Sample/ToggleAndRepeatButtonPageTests.cs`).

## Dependencies and captures

- Button (#27) for `PhoneContentLine` and `HoleParts`; tokens: merged. All 14 resources are in `Tokens/`.
- Captures used: `base`, `PointerOver`, `Pressed`, `Disabled`, dark and light, run `20261108-1443-dev11`; the probe,
  run `20261112-0755-dev11`.
- **Not measured**: the cadence on a real phone (is 60 ms the emulator's, or the platform's?), and a RepeatButton with
  `Interval` set by the app (does the emulator's cadence follow it, or round it?). Either would settle the decision below.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist RepeatButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.Primitives.RepeatButton`; Avalonia `Avalonia.Controls.RepeatButton`.

### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Interval` | `Int32` | `RepeatButton.Interval` | Avalonia's; the theme sets the phone's 33 (Behaviours 1, 10) |
| [x] | `Delay` | `Int32` | `RepeatButton.Delay` | Avalonia's; the theme sets the phone's 500 (Behaviours 1, 10) |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands; not run for a repeat while the finger is off (Behaviour 11) |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's; the theme sets the phone's `Press` (Behaviour 9) |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; draws nothing (Behaviour 6) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; drives `:pressed` (Behaviour 4), stays true off the button, as the phone's `Pressed` does |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's; at the press, then each repeat, none while off the button (Behaviours 9 to 11) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent; the template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's |
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

### Default style: setters (12)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Setter, `Transparent` |
| [x] | `BorderBrush` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource` |
| [x] | `Foreground` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.Foreground` | Setter, `DynamicResource` |
| [x] | `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource` |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource` |
| [x] | `FontWeight` | `{ThemeResource PhoneButtonFontWeight}` | `TemplatedControl.FontWeight` | Setter, `DynamicResource` |
| [x] | `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource` |
| [x] | `Padding` | `{ThemeResource PhoneButtonContentPadding}` | `TemplatedControl.Padding` | Setter, `DynamicResource`; the presenter's `Margin` |
| [x] | `MinHeight` | `{ThemeResource PhoneButtonMinHeight}` | `Layoutable.MinHeight` | Setter, `DynamicResource` |
| [x] | `MinWidth` | `{ThemeResource PhoneButtonMinWidth}` | `Layoutable.MinWidth` | Setter, `DynamicResource` |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | Setter |

### Template parts (2)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Border` | `Border` | none | A `Border` named `Border`, inside the overhang; the states set it |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter` |

### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style (Behaviour 6) |
| [x] | `CommonStates` | `Pressed` | `ContentPresenter.Foreground = {ThemeResource ButtonPressedForegroundThemeBrush}`<br>`Border.Background = {ThemeResource ButtonPressedBackgroundThemeBrush}` | `^:pressed` | `^:pressed` setters; no tilt (Behaviours 4, 7) |
| [x] | `CommonStates` | `Disabled` | `ContentPresenter.Foreground = {ThemeResource ButtonDisabledForegroundThemeBrush}`<br>`Border.BorderBrush = {ThemeResource ButtonDisabledBorderThemeBrush}`<br>`Border.Background = {ThemeResource ButtonDisabledBackgroundThemeBrush}` | `^:disabled` | `^:disabled` setters (Behaviour 5) |

### Transitions (0)

_none_

### Resources (14)

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
| [x] | `ButtonPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | `:pressed`, the presenter's `Foreground` |
| [x] | `ButtonPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `:pressed`, the Border's `Background` |
| [x] | `ButtonDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the presenter's `Foreground` |
| [x] | `ButtonDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the Border's `BorderBrush` |
| [x] | `ButtonDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | `:disabled`, the Border's `Background` |
| [x] | `PhoneTouchTargetOverhang` | `0,9.5` | same | yes | The Border's `Margin` |
