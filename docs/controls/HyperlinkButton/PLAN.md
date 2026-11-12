# HyperlinkButton: plan

[#29](https://github.com/wdcossey/CornflowerUI/issues/29). The 8.1 HyperlinkButton, Avalonia's `HyperlinkButton`
themed as the phone's: its default style and template as an Avalonia `ControlTheme`, and the tilt on the part the
phone tilts. It looks like a Button's sibling but isn't drawn like one: it is plain text, it tilts only its content,
and pressing it changes no colour.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `HyperlinkButton`,
`R:\cornflower-reference\flash-update1\xbf-json\hyperlinkbutton.json`, and the values of its keys from
`themeresources.json` beside it. The runtime's templates and theme resources equal the SDK's design files
([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the checklist, generated from the SDK, names the same
keys.

The captures **check** the theme; they don't supply values. They are the reference app's, run
`20261108-1443-dev11` on the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/HyperlinkButton/`](../../../ref/wp81/HyperlinkButton/README.md). Run `20261108-1732-dev11` has the same
PNGs but for Pressed, and the same dumps. The tilt's readings, from the Button probes of #60, are in
`tests/Cornflower.Tests/Behaviours/TiltReadings.cs`.

## The phone's style

`hyperlinkbutton.json`: one unkeyed style for `HyperlinkButton`, ten setters and a template.

| Setter | Value | Resource value (`themeresources.json`) | Dump, dark / light |
|---|---|---|---|
| `Foreground` | `{ThemeResource HyperlinkForegroundThemeBrush}` | `PhoneForegroundColor`: `#FFFFFFFF` dark, `#FF000000` light | `#FFFFFFFF` / `#FF000000` |
| `Background` | `{ThemeResource HyperlinkButtonBackgroundThemeBrush}` | `#00FFFFFF` | `#00FFFFFF` |
| `BorderBrush` | `{ThemeResource HyperlinkButtonBorderThemeBrush}` | `#00FFFFFF` | `#00FFFFFF` |
| `BorderThickness` | `{ThemeResource HyperlinkButtonBorderThemeThickness}` | `0` | |
| `Padding` | `0,3,0,4` (a literal) | | `0,3,0,4` |
| `HorizontalAlignment` | `Left` | | `Left` |
| `VerticalAlignment` | `Center` | | `Center` |
| `FontFamily` | `{ThemeResource PhoneFontFamilySemiLight}` | `Segoe WP SemiLight` | the same |
| `FontWeight` | `SemiLight` (a literal) | | 350 |
| `FontSize` | `{ThemeResource TextStyleExtraLargePlusFontSize}` | `29.86` | 29.86 |

No `MinWidth` or `MinHeight`: unlike a Button, it is as big as its text. The style sets no content alignment; the
dump reads `Center`, `Center`, WinRT's defaults, so the theme sets both (Avalonia's are `Stretch`), as the Button's
does.

### The template

`hyperlinkbutton.json`, `template.tree`:

```
Grid                         (no name, no background)
  Border "Border"            Background, BorderBrush, BorderThickness: TemplateBinding
    ContentPresenter "ContentPresenter"
                             Content, ContentTemplate, ContentTransitions: TemplateBinding; Margin: TemplateBinding Padding;
                             HorizontalAlignment, VerticalAlignment: TemplateBinding H/VContentAlignment;
                             AutomationProperties.AccessibilityView Raw
```

And ours, in `src/Cornflower.Themes.Wp81/Controls/HyperlinkButton.axaml`, written new:

```
Panel "Grid"                 the phone's Grid; untilted
  Border "Border"            as the phone's; its transparent background takes the hits across the control
    ContentPresenter "PART_ContentPresenter"
                             as the phone's; Tilt.IsTarget, the part the tilt draws on; the text it makes of data
                             gets the phone's line
```

The same three elements as the phone's. The Button's template needs a fourth, an untilted `Root` around the tilted
`Grid`, because the phone tilts a Button's whole `Grid`. Here the phone tilts only the `ContentPresenter`, so the
`Grid` and `Border` already stay still around it and take the hits: a press at a tilted edge clicks without an
extra element. `Grid` is a `Panel`: a `Grid` with no rows or columns lays out as a `Panel` does.

### The visual states

`hyperlinkbutton.json`, `visualStateGroups`: one group, `CommonStates`, on `Grid`. **No transitions.**

| State | What it sets (`hyperlinkbutton.json`) | Resource value | Avalonia |
|---|---|---|---|
| `Normal` | nothing | | the plain setters |
| `PointerOver` | nothing | | no style for `:pointerover` |
| `Pressed` | `PointerDownThemeAnimation` on `ContentPresenter`; no colour | | `Tilt`, on `PART_ContentPresenter`; no style for `:pressed` |
| `Disabled` | `ContentPresenter.Foreground` = `HyperlinkDisabledThemeBrush` | `PhoneDisabledColor`, `#66FFFFFF` dark, `#66000000` light | `^:disabled` |

Four differences from the Button's, each checked in the dumps and PNGs:

- **The tilt is on the `ContentPresenter`**, not on `Grid`: the Pressed dump's `renderTransform` and `projection`
  are on the presenter (−17.19°, 17.19°, the forced state's), and the `Grid` and `Border` keep their bounds.
- **Pressed changes no colour.** The Pressed dump's foregrounds are the rest's, and its PNG's 256 colours are all
  greys, dark and light. `HyperlinkPressedForegroundThemeBrush` (the accent) and
  `HyperlinkPointerOverForegroundThemeBrush` are in `themeresources.json`, but the template uses neither.
- **Disabled is `PhoneDisabledColor` (#66)**, not the Button's `PhoneBaseLowColor` (#73), and only the text: the
  background is left as it is. The PNG's glyph insides are `#FF666666` dark and `#FF999999` light.
- **No transitions.** The Button's template runs `PointerUpThemeAnimation` on leaving `Pressed`; this one declares
  none. See "The release", below.

### The release

`Tilt` has one release, the Button's: it holds 182.6 ms, then eases back over 167 ms (ADR 0005), the measured
`PointerUpThemeAnimation` that the Button's transitions run. The HyperlinkButton's template has no transitions. In
WinRT, leaving a state with no transition stops its storyboard, which would put the presenter back at once, with
no hold and no ease. That is a reading of the template; **no probe has pressed a HyperlinkButton**, and the probes
are Button's only (`tools/refapp/app/Probe.cs`). So the theme keeps `Tilt`'s release, and the PR records it under
"Decided on the user's behalf", with a probe owed. If a probe shows it snaps back, `Tilt` needs a way to turn the
return off (a shared file), proposed in the PR.

### The text's line

The dump's `TextBlock` is 39.7167 tall at 29.86: Segoe WP's 1.33 em line. ADR 0003 gives a template's text the
phone's line, and `PhoneContentLine` does it, as in the Button's theme. The presenter is 40 tall at 2.9167 from the
top on both sides.

### Underline, visited, cursor

The PNGs draw no underline: the longest ink on any row of `base` is 31 px (the t's bar), of the text's 497. Simple's
theme underlines its HyperlinkButton, draws it in its accent (`#CC119EDA`), turns it purple (`#FFC58AF9`) when
`:visited`, and sets a hand cursor. The phone has none of these: WinRT's HyperlinkButton has no visited state. So
the theme sets no `TextDecorations`, no `:visited` style and no `Cursor` (see "Decided on the user's behalf" in the
PR for the cursor).

## Avalonia's HyperlinkButton against the phone's

Avalonia 12.1.4's `HyperlinkButton` is a `Button` with `NavigateUri` (opened through the platform's launcher on click)
and `IsVisited` (`:visited`). It has its own style key, so Button's `ControlTheme` doesn't reach it; Simple themes it
on its own. Measured with `SimpleTheme`'s template (a lone `PART_ContentPresenter`, padding 4, font size 12) and
against the captures:

| Difference | The phone | Avalonia's HyperlinkButton (Simple) | Rung |
|---|---|---|---|
| The style | The setters above | Accent text, underlined, padding 4, the window's font | 1, theme |
| The disabled look | the text at `PhoneDisabledColor` | Simple's | 1, theme |
| The pressed look | the text tilts; no colour | Simple's | 1, theme, and 2 below |
| The tilt | `PointerDownThemeAnimation` on `ContentPresenter` | none | 2, the `Tilt` attached behaviour, set by the theme, on `PART_ContentPresenter` (`Tilt.IsTarget`) |
| The release | no transition: believed to snap back (not measured) | none | 2, `Tilt`'s eased release, kept until measured |
| The text's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneContentLine` and `PhoneTextLine`, internal to the theme |
| Visited | no such state | `:visited`, drawn purple by Simple | 1, nothing styled: `IsVisited` works, and draws nothing |
| The cursor | not applicable | a hand | 1, not set (decided) |
| Focus | no `FocusStates` group: nothing drawn | a focus rectangle | 1, the theme clears `FocusAdorner` |
| Content alignment | `Center`, `Center` | `Stretch`, `Stretch` | 1, theme setters |
| Access keys | none: `_` is shown as written | Simple's presenter recognises them | 1, kept as Avalonia's, as in the Button's theme |
| `AccessibilityView` `Raw` on the presenter | the text isn't a node of its own | Avalonia's peer already gives the text no control or content node (`NoneAutomationPeer`) | not set: it would change nothing |
| Layout rounding | the nearest raw pixel | up to the raw pixel | ADR 0003's recorded deviation. Here both give 47.0833 tall |

No type is substituted. Rung 1 and 2 only.

## Extensions

None. The theme declares nothing; the tilt is `Tilt`'s, from `Cornflower.Controls`, already public.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/HyperlinkButtonTests.cs` unless named.

1. **The style's values**, dark and light: the ten setters, the content alignment, no minimum size, the padding on
   the presenter. `The_Style_Sets_What_The_Phone_Read`, `The_Font_Is_The_Phones_Whatever_It_Sits_In`.
2. **The bounds**: at 19.1667, 32.5 on the stage, 47.0833 tall; `Grid` and `Border` fill it; the presenter 2.9167
   down, 40 tall, as wide as the control. `Its_Bounds_Are_The_Phones`.
3. **The content is centred** in a wider control. `The_Content_Is_Centred_In_A_Wider_Control`.
4. **Data's line is the phone's**, 1.33 em. `A_Strings_Line_Is_The_Phones`.
5. **No underline**, in the frame and on the text. `There_Is_No_Underline`.
6. **The text in the foreground**: the glyphs' insides `#FFFFFFFF` dark, `#FF000000` light.
   `The_Text_Is_Drawn_In_The_Foreground`.
7. **Disabled** dims the text to `PhoneDisabledColor`, drawn `#FF666666` and `#FF999999`, and keeps an app's
   background. `Disabled_Dims_The_Text_As_The_Phone_Does`, `Disabled_Keeps_The_Apps_Background`.
8. **Pressed changes no colour**: the text keeps its colour, and the frame has no colour but greys.
   `Pressed_Changes_No_Colour`.
9. **The pointer over it changes nothing**, and **visited changes nothing**: the frame is the rest's, pixel for
   pixel. `The_Pointer_Over_It_Changes_Nothing`, `Visited_Changes_Nothing`.
10. **Keyboard focus draws nothing.** `Keyboard_Focus_Draws_Nothing`.
11. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
12. **Access keys**: `_Open` draws `Open` and Alt+O clicks. `An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Clicks_It`.
13. **A press tilts the presenter alone**, not `Grid` or `Border`. `A_Press_Tilts_The_ContentPresenter_Alone`.
14. **The tilt is the phone's**: a presenter with button A's bounds (run `20261109-2307-dev11`, 120 × 60), pressed at
    its 25 grid points, settles within 0.04° and 0.085 px of each reading. The rule is the element's, whatever the
    element. `A_Held_Press_Settles_At_The_Phones_Tilt_For_Its_Point`.
15. **A tap just inside each edge clicks** (run `20261110-0822-dev11`'s 12 taps, on a presenter of the first
    Button's bounds). `A_Tap_Just_Inside_Each_Edge_Clicks`.
16. **The padding takes taps.** `A_Tap_In_The_Padding_Clicks`.
17. **Holes**: `HyperlinkButton` is themed (`Completeness/HolesTests`, its line deleted from `Holes.txt`).
18. **No global rule**: `StyleBudgetTests`, unchanged.
19. **The sample's page**: in the catalogue after the Button's, every HyperlinkButton on it the theme's, its header,
    a click counted (`Sample/HyperlinkButtonPageTests.cs`).

## Dependencies and captures

- `Tilt` (#22, #65), tokens (#19), `PhoneTextLine` and `PhoneContentLine` (#26, #27), the sample (#9): merged. All 7
  resources the style reads are in `Tokens/`.
- Captures used: `base`, `CommonStates.Normal`, `CommonStates.PointerOver`, `CommonStates.Pressed`,
  `CommonStates.Disabled`, dark and light, run `20261108-1443-dev11`; the tilt probes `20261109-2307-dev11` and
  `20261110-0822-dev11` (Buttons).
- **The Pressed capture is a forced state with no finger**: it left the presenter at −17.19°, 17.19°. Its colours
  check the theme; its tilt doesn't.
- **Owed:** a probe of a HyperlinkButton's press and release (does the release snap back with no transition, or ease
  as a Button's?), and a pressed capture under a real touch for the picture. Both need `tools/refapp` to press a
  HyperlinkButton, which it can't yet.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist HyperlinkButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.HyperlinkButton`; Avalonia `Avalonia.Controls.HyperlinkButton`.

### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `NavigateUri` | `Uri` | `HyperlinkButton.NavigateUri` | Avalonia's stands: a click opens it through the platform's launcher. The theme doesn't touch it |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | `Button.CommandParameter` | Avalonia's stands |
| [x] | `Command` | `ICommand` | `Button.Command` | Avalonia's stands; without `CanExecute` it is `:disabled` and drawn as Disabled |
| [x] | `ClickMode` | `ClickMode` | `Button.ClickMode` | Avalonia's stands (`Release`, `Press`; WinRT's `Hover` has no Avalonia member) |
| [x] | `IsPointerOver` (read-only) | `Boolean` | `InputElement.IsPointerOver` | Avalonia's stands; nothing is drawn for it, as on the phone (Behaviour 9) |
| [x] | `IsPressed` (read-only) | `Boolean` | `Button.IsPressed` | Avalonia's stands; the tilt runs from the press itself (Behaviours 8, 13) |

### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | `Button.Click` | Avalonia's stands; a tilted press still clicks (Behaviours 15, 16) |

### Methods, from `ButtonBase` (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent (mapping rules, theme animations). The template binds it, but sets none |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an Avalonia `IDataTemplate` in `ContentTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's |
| [x] | `Content` | `Object` | `ContentControl.Content` | Template-bound to the presenter, as the phone's; data gets the phone's line (Behaviour 4) |
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
| [x] | `Foreground` | `{ThemeResource HyperlinkForegroundThemeBrush}` | `TemplatedControl.Foreground` | Setter, `DynamicResource` |
| [x] | `Background` | `{ThemeResource HyperlinkButtonBackgroundThemeBrush}` | `TemplatedControl.Background` | Setter, `DynamicResource`; the Border's, which takes the hits |
| [x] | `BorderBrush` | `{ThemeResource HyperlinkButtonBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource` |
| [x] | `BorderThickness` | `{ThemeResource HyperlinkButtonBorderThemeThickness}` | `TemplatedControl.BorderThickness` | Setter, `DynamicResource` |
| [x] | `Padding` | `0,3,0,4` | `TemplatedControl.Padding` | Setter, the literal; the presenter's `Margin`, as the phone's |
| [x] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | Setter |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilySemiLight}` | `TemplatedControl.FontFamily` | Setter, `DynamicResource`: Selawik Semilight for Segoe WP Semilight |
| [x] | `FontWeight` | `SemiLight` | `TemplatedControl.FontWeight` | Setter, the literal (350) |
| [x] | `FontSize` | `{ThemeResource TextStyleExtraLargePlusFontSize}` | `TemplatedControl.FontSize` | Setter, `DynamicResource` |

### Template parts (2)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Border` | `Border` | none | A `Border` named `Border`, inside a `Panel` named `Grid` (the phone's unnamed `Grid`) |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, as Avalonia's Button looks for it; `Tilt.IsTarget`. Its `AccessibilityView` `Raw` isn't set: Avalonia's peer gives the text no node of its own already |

### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style, as the phone changes nothing (Behaviour 9) |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on ContentPresenter` | `^:pressed` | No style: the tilt by `Tilt.IsEnabled`, on `PART_ContentPresenter` (Behaviours 8, 13) |
| [x] | `CommonStates` | `Disabled` | `ContentPresenter.Foreground = {ThemeResource HyperlinkDisabledThemeBrush}` | `^:disabled` | `^:disabled` setter on `PART_ContentPresenter` (Behaviour 7) |

### Transitions (0)

_none_. `Tilt` still eases the presenter back on release, as a Button's transitions do: see "The release".

### Resources (7)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `HyperlinkForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | `Foreground` |
| [x] | `HyperlinkButtonBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | `Background` |
| [x] | `HyperlinkButtonBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | `BorderBrush` |
| [x] | `HyperlinkButtonBorderThemeThickness` | `0` | same | yes | `BorderThickness` |
| [x] | `PhoneFontFamilySemiLight` | `Segoe WP SemiLight` | same | yes | `FontFamily` |
| [x] | `TextStyleExtraLargePlusFontSize` | `29.86` | same | yes | `FontSize` |
| [x] | `HyperlinkDisabledThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | `:disabled`, the presenter's `Foreground` |
