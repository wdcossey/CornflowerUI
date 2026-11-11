# Button: plan

[#27](https://github.com/wdcossey/CornflowerUI/issues/27). The 8.1 Button, Avalonia's `Button` themed as the
phone's: its default style and template as an Avalonia `ControlTheme`, the tilt on the part the phone tilts, and
the phone's colours in each state, dark and light. The first full control, and the dry run of the loop.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `Button`,
`R:\cornflower-reference\flash-update1\xbf-json\button.json`, and the values of its keys from
`themeresources.json` beside it. The runtime's templates and theme resources equal the SDK's design files
(`generic.xaml`, `themeresources.xaml`) exactly ([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the
checklist, generated from the SDK, names the same keys. Each value is cited by its source: `button.json` for a
setter, a part or a state, and the resource key for a value.

The captures **check** the theme; they don't supply values. They are the reference app's, run
`20261108-1443-dev11` on the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/Button/`](../../../ref/wp81/Button/README.md). The tilt's own readings, from the probes of #60, are
in `tests/Cornflower.Tests/Behaviours/TiltReadings.cs`.

## The phone's style

`button.json`: one unkeyed style for `Button`, twelve setters and a template.

| Setter | Value | Resource value (`themeresources.json`) | Dump, dark / light |
|---|---|---|---|
| `Background` | `Transparent` | | `#00FFFFFF` |
| `BorderBrush` | `{ThemeResource PhoneForegroundBrush}` | `PhoneForegroundColor`: `#FFFFFFFF` dark, `#FF000000` light | `#FFFFFFFF` / `#FF000000` |
| `Foreground` | `{ThemeResource PhoneForegroundBrush}` | the same | the same |
| `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `2.5` | `2.5` on each side |
| `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `Segoe WP` | `Segoe WP` |
| `FontWeight` | `{ThemeResource PhoneButtonFontWeight}` | `Semibold` | 600 |
| `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `18.14` | 18.14 |
| `Padding` | `{ThemeResource PhoneButtonContentPadding}` | `9.5,0,9.5,3.5` | the same |
| `MinHeight` | `{ThemeResource PhoneButtonMinHeight}` | `57.5` | 57.5 |
| `MinWidth` | `{ThemeResource PhoneButtonMinWidth}` | `109` | 109 |
| `HorizontalAlignment` | `Left` | | `Left` |
| `VerticalAlignment` | `Center` | | `Center` |

The style sets no content alignment. The dump reads `hContentAlign` and `vContentAlign` as `Center`: WinRT's own
defaults for a `Control`. Avalonia's are `Stretch` for both, so the theme sets `Center` for both (an API value
the dump read, not a pixel).

### The template

`button.json`, `template.tree`:

```
Grid "Grid"                  Background Transparent
  Border "Border"            BorderBrush, BorderThickness, Background: TemplateBinding; Margin {ThemeResource PhoneTouchTargetOverhang} (0,9.5)
    ContentPresenter "ContentPresenter"
                             Foreground, Content, ContentTemplate: TemplateBinding; Margin: TemplateBinding Padding;
                             HorizontalAlignment, VerticalAlignment: TemplateBinding H/VContentAlignment
```

And ours, in `src/Cornflower.Themes.Wp81/Controls/Button.axaml`, written new:

```
Panel "Root"                 Background Transparent; untilted, takes the hits
  Panel "Grid"               Tilt.IsTarget: the phone's Grid, the part the tilt draws on
    Border "Border"          as the phone's
      ContentPresenter "PART_ContentPresenter"
                             as the phone's; the TextBlock it makes of data gets the phone's line (below)
```

One element more than the phone's: the root. The phone's `PointerDownThemeAnimation` targets `Grid`, the whole
Button, and the phone still clicks a press 0.84 px inside a tilted edge (run `20261110-0822-dev11`). Avalonia
hit-tests through a `RenderTransform`, so with the tilt on the root, a press within about 3 px of an edge
misses. So `Root` takes the hits, untilted, and `Grid` inside it, the same size, is marked `Tilt.IsTarget`
([CLAUDE.md](../../../CLAUDE.md), `Tilt`). The tilt's geometry is the target's bounds, the whole Button, as on
the phone.

`Grid` and `Root` are `Panel`s: a `Grid` with no rows or columns lays out as a `Panel` does. The phone's
`Foreground` on the `ContentPresenter` is a `TemplateBinding`; ours inherits it from the Button, which comes to
the same value, and the states set it on the presenter as the phone's do.

### The visual states

`button.json`, `visualStateGroups`: one group, `CommonStates`, on `Grid`.

| State | What it sets (`button.json`) | Resource value | Avalonia |
|---|---|---|---|
| `Normal` | nothing | | the plain setters |
| `PointerOver` | nothing | | no style for `:pointerover` |
| `Pressed` | `PointerDownThemeAnimation` on `Grid`; `ContentPresenter.Foreground` = `ButtonPressedForegroundThemeBrush`; `Border.Background` = `ButtonPressedBackgroundThemeBrush` | `PhoneTextOverAccentColor`, `#FFFFFFFF` in both; `SystemColorControlAccentColor`, Cobalt `#FF3E65FF` by default | `^:pressed`, and `Tilt` |
| `Disabled` | `ContentPresenter.Foreground` = `ButtonDisabledForegroundThemeBrush`; `Border.BorderBrush` = `ButtonDisabledBorderThemeBrush`; `Border.Background` = `ButtonDisabledBackgroundThemeBrush` | `PhoneBaseLowColor`, `#73FFFFFF` dark, `#40000000` light; `#00FFFFFF` | `^:disabled` |

Every animation but the theme animation is an `ObjectAnimationUsingKeyFrames` with one `DiscreteObjectKeyFrame`
at `KeyTime` 0: a setter. The transitions, `Pressed` → `PointerOver`, `PointerOver` → `Normal` and `Pressed` →
`Normal`, each run `PointerUpThemeAnimation` on `Grid`: the tilt's return, which `Tilt` runs on release (hold
180 ms, ease back over 195 ms; ADR 0005).

### The text's line

The phone's `ContentPresenter` puts content that isn't an element (a string, a number) in a `TextBlock` with no
`LineHeight`, so its line is Segoe WP's,
1.33 em: the dump's `ContentPresenter` is 24.1667 tall at 18.14, at 15 from the Button's top. Selawik's line is
1.2 em (21.77), which would make ours 2.1 shorter and its text 0.6 higher. ADR 0003 gives every template whose
text sets no `LineHeight` the phone's line, 1.33 × `FontSize`. The presenter is marked
`PhoneContentLine.IsEnabled`, internal to the theme: when Avalonia's default template makes the presenter's
`TextBlock` (content that isn't an element, and no `ContentTemplate` or `DataTemplate` of the app's matches it),
`PhoneContentLine` sets `PhoneTextLine.MaxHeightLineHeight` to 0 on it, so its `LineHeight` is the larger of 0 and
1.33 em, kept there when the Button's `FontSize` changes. The theme adds no data template, so an app's are found as
without it. Content that is an element (an app's own `TextBlock`, say) keeps its own line, as in any other control:
a known gap.

## Avalonia's Button against the phone's

Avalonia 12.1.4's `Button` raises `:pressed` from a left press until its release or a lost capture, `:disabled`,
`:pointerover`, and `:flyout-open` while its `Flyout` is open, and looks for `PART_ContentPresenter`. Measured
with `SimpleTheme`'s template and against the captures:

| Difference | The phone | Avalonia's Button | Rung |
|---|---|---|---|
| The style | The setters above | Simple's: other sizes, padding and colours | 1, theme |
| The pressed and disabled looks | The states above | Simple's | 1, theme |
| The tilt | `PointerDownThemeAnimation` and `PointerUpThemeAnimation` on `Grid` | none | 2, the `Tilt` attached behaviour, set by the theme |
| A press at a tilted edge | clicks (run 0822) | misses, with the tilt on the root | 1, the theme's untilted root around the tilted `Grid` |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine` and `PhoneTextLine`, internal to the theme |
| Focus | no `FocusStates` group: nothing drawn | the theme under Wp81 draws a focus rectangle when `FocusAdorner` is left unset | 1, the theme clears `FocusAdorner` |
| Buttons in the templates of controls Wp81 doesn't theme yet | not applicable | Simple's templates for CalendarItem, CalendarDatePicker, CommandBar, NavigationPage, PipsPager and the file dialogs hold plain Buttons, which would take Wp81's theme by type | 2, `HoleParts`, internal to the theme: such a Button takes the Button theme under Wp81 |
| Content alignment | `Center`, `Center` (WinRT's defaults) | `Stretch`, `Stretch` | 1, theme setters |
| Access keys | none: `_` is shown as written | Simple's template recognises them | none needed: the theme's presenter doesn't (`RecognizesAccessKey` is off), as the phone's |
| The pointer over it | changes nothing | `:pointerover` | nothing styled |
| Pressed while the finger slides off | not captured: WinRT's `ButtonBase` is believed to leave `Pressed` when the pointer leaves | stays `:pressed` until release | a gap until measured: see Dependencies |
| `:flyout-open` | no such state | raised | nothing styled; the flyout itself is themed with Flyout |
| Layout rounding | the nearest raw pixel | up to the raw pixel | ADR 0003's recorded deviation. Here both give 109.1667 (262 px) for 109 |

No type is substituted. Rung 1 and 2 only.

## Extensions

None. The theme declares nothing; the tilt is `Tilt`'s, from `Cornflower.Controls`, already public.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ButtonTests.cs` unless named.

1. **The style's values**, dark and light: the twelve setters and the content alignment, the overhang and the
   padding on the parts (`button.json`, the keys' values, the dump). `The_Style_Sets_What_The_Phone_Read`,
   `The_Font_Is_The_Phones_Whatever_It_Sits_In`.
2. **The bounds**: the Button at 19.1667, 27.5 on the stage, 109.1667 × 57.5; the Border 9.5833 down,
   109.1667 × 38.3333; the content 15 down, 24.1667 tall (the dump). `Its_Bounds_Are_The_Phones`.
3. **The content is centred** in the padding. `The_Content_Is_Centred_In_The_Padding`.
4. **Data's line is the phone's**, 1.33 em, at any `FontSize`, a number's too; an app's `ContentTemplate`, an
   application's `DataTemplate` and a `TextBlock` in the content keep their own.
   `A_Strings_Line_Is_The_Phones_At_Any_Size`, `A_Numbers_Line_Is_The_Phones_Too`,
   `The_Apps_ContentTemplate_Wins_Over_The_Strings_Line`, `An_Applications_String_DataTemplate_Wins`,
   `A_TextBlock_In_The_Content_Keeps_Its_Own_Line`.
5. **The border is drawn where the phone draws it**: every border pixel on the read row and column, and the page
   just outside and inside, against the PNGs, dark and light. `The_Border_Is_Drawn_Where_The_Phone_Draws_It`.
6. **Disabled** dims the border and the text to `PhoneBaseLowColor` (#FF737373 and #FFBFBFBF drawn on the page),
   and takes away an app's background. `Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does`,
   `Disabled_Takes_The_Apps_Background_Away`.
7. **Pressed** fills with the accent, the text over it in `PhoneTextOverAccentColor`, the border unchanged, and
   back on release. `Pressed_Fills_With_The_Accent_And_Draws_The_Text_Over_It`.
8. **The pointer over it changes nothing**: the `PointerOver` PNGs equal `base`'s, byte for byte.
   `The_Pointer_Over_It_Changes_Nothing`.
9. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
10. **A press tilts `Grid`, the whole Button, inside an untilted root.** `A_Press_Tilts_The_Grid_Inside_An_Untilted_Root`.
11. **The tilt is the phone's**: button A of run `20261109-2307-dev11`, 120 × 60, pressed at its 25 grid points,
    settles within 0.04° and 0.085 px of each reading. `A_Held_Press_Settles_At_The_Phones_Tilt_For_Its_Point`.
    The tilt's timeline (the jump, the frames, the hold and the return) is `Tilt`'s, tested in
    `Behaviours/TiltTests.cs`.
12. **A tap just inside each edge clicks** (run `20261110-0822-dev11`, 12 taps).
    `A_Tap_Just_Inside_Each_Edge_Clicks_As_On_The_Phone`.
13. **The overhang takes taps**, as the phone's transparent `Grid` does. `A_Tap_On_The_Overhang_Clicks`.
14. **Holes**: `Button` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
15. **No global rule**: `StyleBudgetTests`, unchanged.
16. **The sample's page**: in the catalogue, every Button on it the theme's, its header from its title, a click
    counted; the shell's list the phone's Buttons, and Back on the rail not (`Sample/ButtonPageTests.cs`).
17. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
18. **A theme change mid-press puts the tilt at rest.** `A_New_Theme_Mid_Press_Puts_The_Tilt_At_Rest_And_The_Next_Press_Tilts`;
    the rest of the tilt's behaviour (capture loss, drag, the return) is tested on this theme's Button in
    `Behaviours/TiltTests.cs`, which runs on Simple then Wp81.
19. **Buttons in the holes' templates keep the theme under Wp81**: every hole shown with Simple then Wp81, and a
    Calendar's header and arrows as wide as with Simple alone; an app's own templated control keeps Wp81's Button
    (`Completeness/HolePartsTests.cs`).

## Dependencies and captures

- `Tilt` ([#22](https://github.com/wdcossey/CornflowerUI/issues/22), #65), tokens
  ([#19](https://github.com/wdcossey/CornflowerUI/issues/19)), `PhoneTextLine` (#26), the desktop sample (#9):
  merged. All 14 resources the style reads are in `Tokens/`.
- Captures used: `base`, `CommonStates.Normal`, `CommonStates.PointerOver`, `CommonStates.Pressed`,
  `CommonStates.Disabled`, dark and light, run `20261108-1443-dev11`; the tilt probes `20261109-2307-dev11`
  and `20261110-0822-dev11`.
- **The Pressed capture is a forced state with no finger**: its `PointerDownThemeAnimation` ran with no press
  point and left `Grid` at −17.19°, 17.19°. Its colours check the theme; its tilt doesn't.
- **Owed:** a probe of the pressed colours while a finger slides off the Button and back (does `Pressed`
  leave with the pointer, as `ButtonBase` is believed to do?), and a pressed capture under a real touch for the
  comparison's picture. Neither blocks this control: the colours are the template's.

## Checklist

Generated on 2026-11-11 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist Button <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.Button`; Avalonia `Avalonia.Controls.Button`.

### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Flyout` | `FlyoutBase` | `Button.Flyout` | Avalonia's stands: it opens the flyout on click. The flyout's own look comes with Flyout |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands; without `CanExecute` the Button is `:disabled` and drawn as Disabled |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's stands (`Release`, `Press`; WinRT's `Hover` has no Avalonia member) |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; nothing is drawn for it, as on the phone (Behaviour 8) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; drives `:pressed` (Behaviour 7). Sliding off: owed |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's stands; a tilted press still clicks (Behaviours 12, 13) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent (mapping rules, theme animations). The Button's template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` in `ContentTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's; an app's template keeps its own line (Behaviour 4) |
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
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Setter, `Transparent` (rule 5 allows it) |
| [x] | `BorderBrush` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource` |
| [x] | `Foreground` | `{ThemeResource PhoneForegroundBrush}` | `TemplatedControl.Foreground` | Setter, `DynamicResource` |
| [x] | `BorderThickness` | `{ThemeResource PhoneBorderThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource` |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`: Selawik for Segoe WP |
| [x] | `FontWeight` | `{ThemeResource PhoneButtonFontWeight}` | `TemplatedControl.FontWeight` | Setter, `DynamicResource` |
| [x] | `FontSize` | `{ThemeResource TextStyleLargeFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource` |
| [x] | `Padding` | `{ThemeResource PhoneButtonContentPadding}` | `TemplatedControl.Padding` | Setter, `DynamicResource`; the presenter's `Margin`, as the phone's |
| [x] | `MinHeight` | `{ThemeResource PhoneButtonMinHeight}` | `Layoutable.MinHeight` | Setter, `DynamicResource` |
| [x] | `MinWidth` | `{ThemeResource PhoneButtonMinWidth}` | `Layoutable.MinWidth` | Setter, `DynamicResource` |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | Setter |

### Template parts (3)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Grid` | `Grid` | none | A `Panel` named `Grid`, marked `Tilt.IsTarget`, inside the untilted `Root` that takes the hits |
| [x] | `Border` | `Border` | none | A `Border` named `Border`, inside the overhang; the states set its background and border |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, as Avalonia's Button looks for it |

### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style, as the phone changes nothing (Behaviour 8) |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on Grid`<br>`ContentPresenter.Foreground = {ThemeResource ButtonPressedForegroundThemeBrush}`<br>`Border.Background = {ThemeResource ButtonPressedBackgroundThemeBrush}` | `^:pressed` | `^:pressed` setters on `Border#Border` and `PART_ContentPresenter`; the tilt by `Tilt.IsEnabled` on `Grid` |
| [x] | `CommonStates` | `Disabled` | `ContentPresenter.Foreground = {ThemeResource ButtonDisabledForegroundThemeBrush}`<br>`Border.BorderBrush = {ThemeResource ButtonDisabledBorderThemeBrush}`<br>`Border.Background = {ThemeResource ButtonDisabledBackgroundThemeBrush}` | `^:disabled` | `^:disabled` setters on `Border#Border` and `PART_ContentPresenter` |

### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on Grid` | `Tilt`'s return on release: hold 180 ms, ease back over 195 ms |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on Grid` | The same; with nothing tilted it does nothing |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on Grid` | The same |

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
| [x] | `ButtonPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | `:pressed`, the Border's `Background`; follows an app's accent |
| [x] | `ButtonDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the presenter's `Foreground` |
| [x] | `ButtonDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `:disabled`, the Border's `BorderBrush` |
| [x] | `ButtonDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | `:disabled`, the Border's `Background` |
| [x] | `PhoneTouchTargetOverhang` | `0,9.5` | same | yes | The Border's `Margin` |
