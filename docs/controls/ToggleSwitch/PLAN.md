# ToggleSwitch: plan

[#33](https://github.com/wdcossey/CornflowerUI/issues/33). The 8.1 ToggleSwitch, Avalonia's `ToggleSwitch` themed as
the phone's: its default style and template as an Avalonia `ControlTheme`, the phone's colours at rest and disabled,
dark and light, the header by `ControlHeader` (#96), and the phone's tap and drag as the emulator measured them, by an
attached behaviour internal to the theme (rung 2).

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `ToggleSwitch`,
`R:\cornflower-reference\flash-update1\xbf-json\toggleswitch.json`, and the values of its keys from
`themeresources.json` beside it. The runtime's templates and theme resources equal the SDK's design files
([CLAUDE.md](../../../CLAUDE.md), "Reference material"), so the checklist, generated from the SDK, names the same keys.

The captures **check** the theme; they don't supply values. They are the reference app's, run `20261108-1443-dev11` on
the reference profile (device 11, 1080 × 1920 at 2.4), committed in
[`ref/wp81/ToggleSwitch/`](../../../ref/wp81/ToggleSwitch/README.md): `off` and `on`
(`new ToggleSwitch { Header = "toggle switch", IsOn = … }`) and `no-header` (`new ToggleSwitch()`), at rest and
disabled, dark and light. Run `20261108-1732-dev11`'s are byte for byte the same.

The motion and the input are the touch probes' and the recordings' (tools/refapp/README.md, "ToggleSwitch: tap or
drag", "ToggleSwitch: the hold limit, the distance and the halfway line" and "ToggleSwitch: the knob's slide"; ADR
0005; `tools/motion/toggle.py` and `knob.py`), and the knob's offsets `-Native`'s (`TemplateSettings`, runs
`20261111-1759-dev11` and `20261111-1839-dev11`). Where WinUI's source (`dxaml/xcp/dxaml/lib/ToggleSwitch_Partial.cpp`,
microsoft-ui-xaml) is used as a guide, it is said.

## The phone's style

`toggleswitch.json`: one unkeyed style for `ToggleSwitch`, nine setters and a template.

| Setter | Value | Resource value | Dump, dark / light |
|---|---|---|---|
| `Foreground` | `{ThemeResource ToggleSwitchForegroundThemeBrush}` | `PhoneForegroundColor` | `#FFFFFFFF` / `#FF000000` |
| `HorizontalAlignment` | `Stretch` | | |
| `VerticalAlignment` | `Center` | | |
| `HorizontalContentAlignment` | `Left` | | `Left` (and `vContentAlign` `Center`, WinRT's default) |
| `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `Segoe WP` | |
| `FontWeight` | `SemiBold` | | 600 |
| `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | 12 | 12 |
| `Margin` | `0` | | |
| `Padding` | `0,0,25.5,0` | | |

### The template

```
Border                                TemplateBinding Background, BorderBrush, BorderThickness
  Grid                                Background Transparent; rows Auto, Auto, 9.5; columns *, 13.5, 77
    ContentPresenter "HeaderContentPresenter"
                                      Margin: Padding; HeaderContentPresenterStyle; Foreground
                                      ToggleSwitchHeaderForegroundThemeBrush; Header, HeaderTemplate
    Grid                              Row 1; Margin: Padding
      ContentPresenter "OffContentPresenter"   Opacity 0, not hit; PhoneFontFamilyNormal, TextStyleExtraLargeFontSize
                                      (25.5), Normal; OffContent, OffContentTemplate; aligned by the content alignments
      ContentPresenter "OnContentPresenter"    the same, OnContent
    Grid                              Rows 0-1, column 2; ManipulationMode None; VerticalAlignment Bottom
      Grid "SwitchKnobBounds"         Height 36
        Border "OuterBorder"          ToggleSwitchOuterBorderBorderThemeBrush, 2.5; Margin 3,4.5
          Border "InnerBorder"        ToggleSwitchTrackBorderThemeBrush (transparent), 2.5
            ContentPresenter "SwitchCurtainBounds" > ContentPresenter "SwitchCurtainClip" (clipped in code)
              Rectangle "SwitchCurtain"   Width 64; ToggleSwitchCurtainBackgroundThemeBrush (the accent);
                                      TranslateTransform "CurtainTranslateTransform" X -64
        Rectangle "SwitchKnob"        Width 20.5, Left; ToggleSwitchThumbBackgroundThemeBrush, stroke
                                      ToggleSwitchThumbBorderThemeBrush 2.5; TranslateTransform "KnobTranslateTransform"
      Thumb "SwitchThumb"             Margin -13.5,-15.5,-13.5,-6.5; its template a transparent Rectangle
```

`HeaderContentPresenterStyle`: `PhoneFontFamilyNormal`, `TextStyleMediumFontSize` (16), `Normal`, `PhoneMidBrush`
(the template's own `Foreground`, `ToggleSwitchHeaderForegroundThemeBrush`, `PhoneBaseMidColor`, wins: the same value).

The dumps (`dark__ToggleSwitch__on__base.json`): the switch at 19.1667, 43.3333 in the stage, 412.0833 × 65.4167; the
header 296.25 × 21.6667 (its text 21.28, 1.33 em of 16); the content 34.1667 (its text 33.9167, 1.33 em of 25.5); the
switch's grid at 335, 20, 77.0833 × 35.8333; `OuterBorder` 71.25 × 26.6667 at 2.9167, 4.5833 in it;
`SwitchCurtainBounds` 61.25 × 16.6667; `SwitchKnob` 20.4167 × 35.8333, its `X` 56.5 on; `SwitchThumb` 104.1667 ×
57.9167 at 321.6667, 4.5833. With no header (`no-header`), the switch is 61.6667 tall: the header row is still
17.9167, half the switch's 35.8333, as the switch spans both rows and the grid shares its height between them.

`-Native` (`TemplateSettings`): `KnobOnToOffOffset` 56.67 (136 raw px: the 185 of `SwitchKnobBounds` less the knob's
49) and `CurtainOnToOffOffset` 61.25 on device 11, the `Current` offsets 0 at rest. WinUI's source computes them the
same way in `SizeChangedHandler`.

### The visual states

| Group | State | Sets | Ours |
|---|---|---|---|
| `CommonStates` | `Normal`, `PointerOver`, `Pressed` | nothing | nothing; `FocusAdorner` cleared |
| `CommonStates` | `Disabled` | the header, both contents `ToggleSwitchDisabledForegroundThemeBrush`; the knob `ToggleSwitchThumbDisabledBackgroundThemeBrush` and `…DisabledBorderThemeBrush`; `OuterBorder` `ToggleSwitchOuterBorderDisabledBorderThemeBrush`; the curtain `ToggleSwitchCurtainDisabledBackgroundThemeBrush` | `^:disabled`, the same keys (and Avalonia's `Content`, shown as the header) |
| `ToggleStates` | `Off` | the curtain's `X` −64 | `^:unchecked`: the knob at 0, the curtain 0 wide |
| `ToggleStates` | `On` | the curtain's `X` 0, the knob's 56.5 | `^:checked`: the knob at `KnobOnToOffOffset`, the curtain to it |
| `ToggleStates` | `Dragging` | nothing | nothing: the knob follows the finger (`PhoneToggleSwitch`) |
| `ContentStates` | `OffContent`, `OnContent` | that presenter's `Opacity` 1 and `IsHitTestVisible` | `^:unchecked` and `^:checked` on `PART_OffContentPresenter` and `PART_OnContentPresenter` |

The four `ToggleStates` transitions run a `RepositionThemeAnimation` on the knob and the curtain, from the
`TemplateSettings` offset to the state's place: the knob's slide (below).

## Avalonia's ToggleSwitch against the phone's

Avalonia 12.1.4's `ToggleSwitch` is a `ToggleButton` (read at the `12.1.4` tag: `ToggleSwitch.cs`, `Button.cs`,
`ToggleButton.cs`, and Simple's `ToggleSwitch.xaml`). It moves its knob by `Canvas.Left` on `PART_MovingKnobs`, 0 off
and `PART_SwitchKnob`'s width on, with `KnobTransitions` on that panel; it drags only on `PART_MovingKnobs`. Measured
headless under Simple's theme (`PART_SwitchKnob` 20 wide):

- **A press that moves 3 px or less is a click**, and toggles on release, wherever on the control. The knob doesn't
  move for those 3 px.
- **Past 3 px it drags**: the knob follows, and the release decides at half `PART_SwitchKnob`'s width. A move of 3.5,
  5 or 9.9 went back off; 10.1 and 15 went on. So a 5 px press, a tap on the phone, doesn't toggle.
- **After a drag the knob jumps** to its rest: Avalonia clears `Canvas.Left` and sets it with the transitions off (the
  change log reads `NaN`, then 0 or 20 at local priority, with no animated value). The phone slides.
- **No hold limit**: a still press of 1.2 s toggled.
- `:dragging` while it drags; Enter and Space toggle (Button's).

| Difference | The phone | Avalonia's ToggleSwitch | Rung |
|---|---|---|---|
| The style and template | Above | Simple's: a 40 × 20 rounded switch below its content | 1, theme |
| `Header`, `HeaderTemplate` | on the switch | none; Avalonia's themes show `Content` above the switch | 2, `cf:ControlHeader.Header` and `HeaderTemplate` (#96); `Content` shown in their place when there is no `Header` (decided below) |
| The header's and contents' line | Segoe WP's 1.33 em; no access keys | Selawik's 1.2 em | 2, `PhoneHeaderLine` on all four presenters |
| The knob's travel | `KnobOnToOffOffset`, the bounds less the knob: 136 raw px | `PART_SwitchKnob`'s width | 1, a `Canvas` the knob's 20.5 short of the 77: 136 raw px too |
| The curtain | moves with the knob, clipped | none | 1, as wide as `Canvas.Left`, so its edge stays under the knob |
| Where the finger is taken | the `SwitchThumb` only: the switch, 13.5 either side, 15.5 above, 6.5 below | a click anywhere on the control; a drag from `PART_MovingKnobs` | 2, `PhoneToggleSwitch` (new, internal): a transparent `SwitchThumb` on top; a press elsewhere is held back from the Button |
| The knob under the finger | follows one for one from the first move | after 3 px | 2, `PhoneToggleSwitch` |
| A tap | the `Tapped` gesture: moved under 2.7 mm, no pointer event 1,000 ms or more after the press | moved 3 px or less, any length | 2, `PhoneToggleSwitch`: 17.86 view px, 1,000 ms on `CornflowerClock` |
| A drag's release | over if past half `KnobOnToOffOffset` (28.33) | over if past half `PART_SwitchKnob` | 1 and 2: the same width, decided by `PhoneToggleSwitch` |
| The slide | `RepositionThemeAnimation`: `KeySpline` 0.1,0.9 0.2,1 over 370 ms (#66), after a tap, a drag or a change from code | `KnobTransitions`, but none after a drag | 1, `KnobTransitions` the measured curve; 2, `PhoneToggleSwitch` keeps them on after a drag |
| A tap that moved the knob | back to rest at once (`Dragging` → `Off`), then the slide (`Off` → `On`) | | 2, `PhoneToggleSwitch` |
| A drag on the switch in a scrolling page | never pans (`ManipulationMode` None) | the `ScrollViewer` takes it | 2, `PhoneNoManipulation` (#96) on the switch's grid |
| Focus | no `FocusStates` | a focus adorner | 1, `FocusAdorner` cleared |
| Rounding | the nearest raw pixel | up | ADR 0003; below |

Each is reached by the theme or an attached behaviour, so **no subclass** (rung 4): `PhoneToggleSwitch` reads the
template's parts by name and sets only `Canvas.Left`, the moving panel's `Transitions` and `IsChecked`, and toggles a
tap through the switch's own toggle pattern (`ToggleButtonAutomationPeer`'s `IToggleProvider`, which runs Avalonia's
click), all public. No type is substituted. Rungs 1, 2 and 6.

### Where rounding up differs from the phone's

At 2.4:

| Size | Template | Raw pixels | The phone | Ours | What shows |
|---|---|---|---|---|---|
| The stage's slot | 450 less 19 each side | 1080 − 45.6 − 45.6 | 989, 412.0833 | 988, 411.6667 | The switch is one raw pixel further left |
| `SwitchKnobBounds` | 36 | 86.4 | 86, 35.8333 | 87, 36.25 | The switch one raw pixel taller, at the top: 19.5833 down, not 20 |
| The empty header row | half the switch | | 43, 17.9167 | 44, 18.3333 | A switch with no header is 62.0833 tall, not 61.6667 |
| `SwitchKnob` | 20.5 | 49.2 | 49, 20.4167 | 50, 20.8333 | The knob one raw pixel wider; on, it reaches one past the switch's right edge |
| The travel | 77 less 20.5 | 184.8 − 49.2 | 136, 56.6667 | 136, 56.6667 (a margin rounds to the nearest) | Nothing |
| The header, the content | 21.28, 33.915 | 51.07, 81.4 | 52, 82 | 52, 82 | Nothing |

On, the phone draws the knob at the template's 56.5 (135.6 raw px, by a render transform), so its edges are
part-covered pixels; ours is at 136, on whole pixels.

## Other controls' templates

No template of Simple 12.1.4 holds a plain `ToggleSwitch`: of its 79 control files only `ToggleSwitch.xaml` names one
(read at the `12.1.4` tag), so no fallback is needed (`HoleParts` isn't set). Wp81's `ToggleButton` theme (#93) is
keyed by `ToggleButton` and doesn't reach a `ToggleSwitch`, whose style key is its own; `PhonePress` is off for it.

## Extensions

None new. `Cornflower.Controls.ControlHeader` (#96) gives WinRT's `Header` and `HeaderTemplate`.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ToggleSwitchTests.cs` that fails without it.

1. **The style's values**, dark and light: `toggleswitch.json`; the dumps. `The_Style_Sets_What_The_Phone_Read`.
2. **The bounds** of the switch, its rows, the switch's column, the borders, the curtain's clip and the knob, with
   ADR 0003's rounding: the dumps. `The_Parts_Are_Where_The_Phone_Has_Them`.
3. **No header, half the switch's height above the content**: the `no-header` dump.
   `Without_A_Header_The_First_Row_Is_Half_The_Switch`.
4. **The knob's travel is `KnobOnToOffOffset`**, 136 raw px, and a switch shown on is on at once: `-Native`.
   `The_Knob_Travels_The_Phones_Offset`.
5. **The pixels** of row 180 through the switch, off and on, dark and light: the PNGs.
   `The_Switch_Is_Drawn_As_The_Phone_Draws_It`.
6. **Disabled** takes the phone's keys: the template; the `Disabled` dumps and PNGs. `Disabled_Takes_The_Phones_Keys`,
   `Disabled_Sets_The_Knobs_Border_By_Its_Own_Key`.
7. **`ContentStates`**: the template. `The_Content_Shown_Is_The_States`.
8. **`OnContent` and `OffContent`** with their templates, aligned, in the phone's font and line: the template; the
   dumps' 33.9167. `The_Content_Is_The_Switchs_In_The_Phones_Line`.
9. **The header** from `ControlHeader`, with its template, in the phone's line, without access keys: the template;
   the dump's 21.28. `The_Header_Is_ControlHeaders`.
10. **Avalonia's `Content` in the header's place** when there is no header (decided below).
    `Avalonias_Content_Stands_In_For_A_Missing_Header`.
11. **The slide's curve**: `KnobTransitions`, `Canvas.Left`, 370 ms, `KeySpline` 0.1,0.9 0.2,1 (#66).
    `The_Knob_Slides_On_The_Phones_Curve`.
12. **Set from code, it slides** to its rest, the curtain with it: the recording (#66).
    `Set_From_Code_It_Slides_To_The_Other_Rest`.
13. **A tap toggles**, mouse and touch, running `Click` and `Command`: the probes (`Tapped` on every toggling press).
    `A_Tap_Toggles`.
14. **The knob follows the finger one for one from the first move**, between its rests: #25's probes.
    `The_Knob_Follows_The_Finger_From_The_First_Move`; past the ends, from where the finger has it (WinUI's
    `MoveDelta` adds up the moves): `Past_The_End_The_Finger_Must_Come_Back_Before_The_Knob_Does`.
15. **A tap can move under 2.7 mm**: 17.47 view px tapped, 18.33 didn't, either way on the screen.
    `A_Tap_Can_Move_Under_Three_Millimetres`.
16. **A tap that moved the knob** puts it back at once, then slides: the probes' `Dragging` > `Off` at once, `Off` >
    `On` 380 ms later. `A_Tap_That_Moved_The_Knob_Slides_From_Its_Rest`.
17. **A drag goes over past half way**, and slides from where it is: 28.32 back off, 28.35 on (device 11); from on,
    the same distance back; no `Click`. `A_Drag_Goes_Over_Past_Half_Way`.
18. **A press held 1,000 ms isn't a tap**: an event at 984 ms tapped, one at 1,000 didn't; the release counts (decided
    below). `A_Press_Held_A_Second_Is_Not_A_Tap`; a held drag still goes by the knob: `A_Held_Drag_Still_Goes_By_The_Knob`.
19. **Only the switch takes the finger**, and only a left press (a touch, a pen, the mouse's left button), as the
    phone's `Tapped` and Avalonia's click: the template's `SwitchThumb`; WinUI's source has `Tapped` and the drag on
    it. The rest of the template is transparent, as the phone's grid, so it is still the switch's.
    `Only_The_Switch_Takes_The_Finger`; a control in the content keeps its press:
    `A_Control_In_The_Content_Takes_Its_Own_Press`; a second finger changes nothing (chosen: WinRT's `Thumb` follows
    one pointer): `A_Second_Finger_Changes_Nothing`; turned off and on again, as a theme swap does, it lets go and
    takes hold: `Turned_Off_It_Lets_Go_And_Turned_On_It_Takes_Hold`.
20. **A lost capture puts the knob back** (chosen: WinRT's `DragCompleted` with `Canceled`).
    `A_Lost_Capture_Puts_The_Knob_Back`.
21. **A drag on the switch never scrolls its page**: `ManipulationMode` None. `A_Drag_On_The_Switch_Never_Scrolls_Its_Page`.
22. **No pointer-over, pressed or focus look**: the template. `Pointer_Over_Press_And_Focus_Change_No_Pixel`.
23. **The colours follow the variant**. `The_Colours_Follow_A_Change_Of_Variant`.

## Decided on the user's behalf

- **The tap, the drag and the hit area are an attached behaviour, `PhoneToggleSwitch`, internal to the theme**
  (rung 2), not a `ToggleSwitch` subclass (rung 4). ROADMAP.md allows a subclass "only if the drag measurement fails";
  Avalonia's drag does fail it (above), but everything it misses is reached from outside through public members, so the
  app keeps Avalonia's type. To reverse: a subclass overriding the pointer handlers, its README listing the
  substitution.
- **A tap toggles through Avalonia's click** (the switch's `IToggleProvider`, from its automation peer), so `Click`
  and `Command` run as Avalonia's tap runs them; **a drag sets `IsChecked`** without a `Click`, as Avalonia's own drag
  does. On the phone both raise `Toggled`, Avalonia's `IsCheckedChanged`, which runs either way. To reverse: set
  `IsChecked` for a tap too.
- **The hold is timed on `CornflowerClock`, at each pointer event, the release included.** The phone times it by the
  pointers' timestamps and leaves the release out, but its digitiser reports a held finger all along (the emulator
  every 100 ms), so a press held past 1,000 ms always sees an event there. Avalonia's mouse, and some touch screens,
  report nothing while the pointer is still, so the release counts too, as the phone's digitiser would have it. The
  clock is the one every Cornflower timeline reads, so the tests hold the limit to the millisecond; an event is handled
  within a frame of its timestamp. To reverse: judge only `PointerMoved`, and read `e.Timestamp`.
- **The tap's distance is 17.86 view px**: 2.7 mm at the reference profile's 167.9 view px an inch, between the
  emulator's farthest tap and nearest miss (2.64 and 2.75 mm). Avalonia knows no screen's physical size, so on other
  screens it is 17.86 view px, about 2.8 mm on Android's 160 to the inch. It is measured on the screen, both ways, from
  where the press began: the probes moved only across. To reverse: the constant in `PhoneToggleSwitch`.
- **Only the switch's `SwitchThumb` takes the finger**: a tap on the header or the words does nothing. No probe has
  pressed anywhere but the knob. The template puts its only `Thumb` over the switch, and WinUI's source handles
  `Tapped` and the drag on that `Thumb` alone. Avalonia's toggles on a click anywhere. To reverse: let a press outside
  the thumb through to the Button (`PhoneToggleSwitch.OnPressed`).
- **From on, the drag's line is half the travel back**, 28.33 from the right. WinRT's code measures from the template's
  56.5, so its line from on is 0.17 nearer. No probe dragged from on. To reverse: start an on drag at 56.5.
- **A press mid-slide takes the knob from where it is going**, as on the phone, whose slide runs in the compositor and
  leaves the knob's translation at its end.
- **Avalonia's `Content` is shown in the header's place, in the header's style, when `ControlHeader.Header` isn't
  set.** WinRT's ToggleSwitch has no content; Avalonia's themes show it above the switch, so an Avalonia app's
  `<ToggleSwitch Content="wi-fi" />` keeps its label rather than losing it. With a `Header`, `Content` isn't shown.
  To reverse: drop `PART_ContentPresenter`.
- **No `:dragging`**: Avalonia's own drag sets it, and it never runs (a protected member, which a behaviour can't
  set). The phone's `Dragging` state sets nothing. To reverse: a subclass.
- **The keyboard is Avalonia's**: Space and Enter toggle (Button's). Unmeasured; WinUI's source toggles on Space only.

## Dependencies and captures

- Tokens (#19), `PhoneTextLine` (#26), the sample (#9), `ControlHeader`, `PhoneHeaderLine` and `PhoneNoManipulation`
  (#34, PR #96), the probes of #25 and #33 (PRs #58, #90) and the slide's recording (#66): merged. All 17 resources
  the style reads are in `Tokens/`, but `HeaderContentPresenterStyle`, a `Style` whose four values the theme sets on
  the presenter.
- Captures used: `off`, `on` and `no-header`, `base` and `CommonStates.Disabled`, dark and light, run
  `20261108-1443-dev11`. `CommonStates.Normal` is `base`, byte for byte. The forced `ToggleStates` and `ContentStates`
  captures aren't the control's own states (forcing `Dragging` drops `On`'s storyboard, so an on switch draws off), so
  they aren't used.
- **Owed:** a probe of a tap on the header and on the words, and of a drag from on; the device run on the Pixel 8.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist ToggleSwitch <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ToggleSwitch`; Avalonia `Avalonia.Controls.ToggleSwitch`.

### Properties (8)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `OnContentTemplate` | `DataTemplate` | `ToggleSwitch.OnContentTemplate` | Avalonia's; the template shows it |
| [x] | `OnContent` | `Object` | `ToggleSwitch.OnContent` | Avalonia's (default "On", as the phone's); the template shows it |
| [x] | `OffContentTemplate` | `DataTemplate` | `ToggleSwitch.OffContentTemplate` | Avalonia's; the template shows it |
| [x] | `OffContent` | `Object` | `ToggleSwitch.OffContent` | Avalonia's (default "Off"); the template shows it |
| [x] | `IsOn` | `Boolean` | `ToggleButton.IsChecked` | Avalonia's; `null` is neither state's content |
| [x] | `HeaderTemplate` | `DataTemplate` | none | `cf:ControlHeader.HeaderTemplate`, attached (#96) |
| [x] | `Header` | `Object` | none | `cf:ControlHeader.Header`, attached (#96); Avalonia's `Content` in its place without one |
| [-] | `TemplateSettings` (read-only) (not a dependency property) | `ToggleSwitchTemplateSettings` | none | not applicable: the template's transitions read it; the knob's travel is the `PART_SwitchKnob` Canvas's width, the same 136 raw px |

### Events (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Toggled` | `RoutedEventHandler` | `ToggleButton.IsCheckedChanged` | Avalonia's; raised by a tap, a drag and code |

### Methods (4)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnToggled()` (protected, overridable) | `ToggleButton.OnIsCheckedChanged` | Avalonia's |
| [-] | `void OnOnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: a subclass's hook; Avalonia's `OnPropertyChanged` serves |
| [-] | `void OnOffContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: as above |
| [-] | `void OnHeaderChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: the header is attached |

### Default style: setters (9)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource ToggleSwitchForegroundThemeBrush}` | `TemplatedControl.Foreground` | Setter, by key |
| [x] | `HorizontalAlignment` | `Stretch` | `Layoutable.HorizontalAlignment` | Setter |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | Setter |
| [x] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Setter; and `VerticalContentAlignment` `Center`, WinRT's default |
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `TemplatedControl.FontFamily` | Setter, by key |
| [x] | `FontWeight` | `SemiBold` | `TemplatedControl.FontWeight` | Setter |
| [x] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `TemplatedControl.FontSize` | Setter, by key |
| [x] | `Margin` | `0` | `Layoutable.Margin` | Setter |
| [x] | `Padding` | `0,0,25.5,0` | `TemplatedControl.Padding` | Setter; the header's and the contents' margin |

### Template parts (13)

Avalonia's control looks for: `PART_MovingKnobs`, `PART_OffContentPresenter`, `PART_OnContentPresenter`, `PART_SwitchKnob`, `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HeaderContentPresenter` | `ContentPresenter` | none | The same name; `ControlHeader`'s; `PART_ContentPresenter` beside it for Avalonia's `Content` |
| [x] | `OffContentPresenter` | `ContentPresenter` | `PART_OffContentPresenter` | Avalonia's name |
| [x] | `OnContentPresenter` | `ContentPresenter` | `PART_OnContentPresenter` | Avalonia's name |
| [x] | `SwitchKnobBounds` | `Grid` | none | The same |
| [x] | `OuterBorder` | `Border` | none | The same |
| [x] | `InnerBorder` | `Border` | none | The same |
| [x] | `SwitchCurtainBounds` | `ContentPresenter` | none | A `Panel` that clips, for WinRT's code-set clip |
| [-] | `SwitchCurtainClip` | `ContentPresenter` | none | not applicable: `SwitchCurtainBounds` clips |
| [x] | `SwitchCurtain` | `Rectangle` | none | The same, as wide as the knob's `Canvas.Left` |
| [-] | `CurtainTranslateTransform` | `TranslateTransform` | none | not applicable: the curtain's width stands in for its offset |
| [x] | `SwitchKnob` | `Rectangle` | `PART_SwitchKnob` | The same name, in `PART_MovingKnobs`, in the `PART_SwitchKnob` Canvas |
| [-] | `KnobTranslateTransform` | `TranslateTransform` | none | not applicable: Avalonia moves the knob by `Canvas.Left` |
| [x] | `SwitchThumb` | `Thumb` | none | A transparent `Panel`, the same margins; `PhoneToggleSwitch` takes its presses |

### Visual states (9)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | Nothing |
| [x] | `CommonStates` | `Pressed` | nothing | `^:pressed` | Nothing |
| [x] | `CommonStates` | `Disabled` | `HeaderContentPresenter.Foreground = {ThemeResource ToggleSwitchDisabledForegroundThemeBrush}`<br>`OffContentPresenter.Foreground = {ThemeResource ToggleSwitchDisabledForegroundThemeBrush}`<br>`OnContentPresenter.Foreground = {ThemeResource ToggleSwitchDisabledForegroundThemeBrush}`<br>`SwitchKnob.Fill = {ThemeResource ToggleSwitchThumbDisabledBackgroundThemeBrush}`<br>`SwitchKnob.Stroke = {ThemeResource ToggleSwitchThumbDisabledBorderThemeBrush}`<br>`OuterBorder.BorderBrush = {ThemeResource ToggleSwitchOuterBorderDisabledBorderThemeBrush}`<br>`SwitchCurtain.Fill = {ThemeResource ToggleSwitchCurtainDisabledBackgroundThemeBrush}` | `^:disabled` | Themed, every key |
| [x] | `ToggleStates` | `Dragging` | nothing | `^:dragging` | Nothing; Avalonia's `:dragging` isn't raised (decided) |
| [x] | `ToggleStates` | `Off` | `CurtainTranslateTransform.X: DoubleAnimation To=-64 Duration=0` | `^:unchecked` | The knob at 0 (Avalonia's), the curtain 0 wide |
| [x] | `ToggleStates` | `On` | `CurtainTranslateTransform.X: DoubleAnimation To=0 Duration=0`<br>`KnobTranslateTransform.X: DoubleAnimation To=56.5 Duration=0` | `^:checked` | The knob at the travel, 56.6667 (Avalonia's), the curtain to it |
| [x] | `ContentStates` | `OffContent` | `OffContentPresenter.Opacity: DoubleAnimation To=1 Duration=0`<br>`OffContentPresenter.IsHitTestVisible = True` | no rule | `^:unchecked` on `PART_OffContentPresenter` (the rule this control decides) |
| [x] | `ContentStates` | `OnContent` | `OnContentPresenter.Opacity: DoubleAnimation To=1 Duration=0`<br>`OnContentPresenter.IsHitTestVisible = True` | no rule | `^:checked` on `PART_OnContentPresenter` |

### Transitions (4)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `ToggleStates` | `Dragging` → `On` | `0` | `RepositionThemeAnimation on SwitchKnob (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.KnobCurrentToOnOffset})`<br>`RepositionThemeAnimation on SwitchCurtain (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.CurtainCurrentToOnOffset})` | `KnobTransitions` from where the drag left the knob (`PhoneToggleSwitch` keeps them on) |
| [x] | `ToggleStates` | `Dragging` → `Off` | `0` | `RepositionThemeAnimation on SwitchKnob (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.KnobCurrentToOffOffset})`<br>`RepositionThemeAnimation on SwitchCurtain (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.CurtainCurrentToOffOffset})` | The same |
| [x] | `ToggleStates` | `On` → `Off` | `0` | `RepositionThemeAnimation on SwitchKnob (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.KnobOnToOffOffset})`<br>`RepositionThemeAnimation on SwitchCurtain (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.CurtainOnToOffOffset})` | `KnobTransitions`: `Canvas.Left`, 370 ms, `KeySpline` 0.1,0.9 0.2,1 (#66) |
| [x] | `ToggleStates` | `Off` → `On` | `0` | `RepositionThemeAnimation on SwitchKnob (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.KnobOffToOnOffset})`<br>`RepositionThemeAnimation on SwitchCurtain (FromHorizontalOffset={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.CurtainOffToOnOffset})` | The same |

### Resources (17)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ToggleSwitchForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By key |
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | By key |
| [x] | `ControlContentThemeFontSize` | `12` | same | yes | By key |
| [x] | `ToggleSwitchDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `ToggleSwitchThumbDisabledBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidSolidColor} → #FF666666` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidSolidColor} → #FFCCCCCC` | yes | By key |
| [x] | `ToggleSwitchThumbDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | By key |
| [x] | `ToggleSwitchOuterBorderDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `ToggleSwitchCurtainDisabledBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidSolidColor} → #FF666666` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidSolidColor} → #FFCCCCCC` | yes | By key |
| [x] | `HeaderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | Its four values set on the header's presenter, as Slider's |
| [x] | `ToggleSwitchHeaderForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMidColor} → #99FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMidColor} → #73000000` | yes | By key |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | By key |
| [x] | `TextStyleExtraLargeFontSize` | `25.5` | same | yes | By key |
| [x] | `ToggleSwitchOuterBorderBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By key |
| [x] | `ToggleSwitchTrackBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | By key |
| [x] | `ToggleSwitchCurtainBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | By key |
| [x] | `ToggleSwitchThumbBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By key |
| [x] | `ToggleSwitchThumbBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | By key |
