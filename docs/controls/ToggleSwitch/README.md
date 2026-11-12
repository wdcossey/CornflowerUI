# ToggleSwitch

Status: done · [#33](https://github.com/wdcossey/CornflowerUI/issues/33) · owed: the device run on the Pixel 8, and probes of a tap on the header and the words and of a drag from on

The Windows Phone 8.1 ToggleSwitch: Avalonia's `ToggleSwitch`, themed by `Wp81Theme` with the phone's default style
and template, in `src/Cornflower.Themes.Wp81/Controls/ToggleSwitch.axaml`, and the phone's tap and drag, as the
emulator measured them, by `PhoneToggleSwitch`, internal to the theme. WinRT's `Header` is `cf:ControlHeader.Header`,
attached. The plan, with every value and its source, is [PLAN.md](PLAN.md); the phone's captures beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `ToggleSwitch` with no style is the phone's. `IsOn` is Avalonia's `IsChecked`; `Toggled` is
`IsCheckedChanged`. The header is attached.

```xml
<!-- 8.1 -->
<ToggleSwitch Header="wi-fi" IsOn="True" Toggled="OnToggled" />
<!-- Avalonia, with Wp81Theme in Application.Styles; xmlns:cf="using:Cornflower.Controls" -->
<ToggleSwitch cf:ControlHeader.Header="wi-fi" IsChecked="True" IsCheckedChanged="OnToggled" />
```

It stretches across its slot: the header and the words on the left, the 77-wide switch on the right, 65.4167 tall with
a header. Avalonia's `Content`, which Avalonia's own themes show above the switch, is shown as the header when there
is no `cf:ControlHeader.Header`.

## Behaviours

- **At rest**: the words (`OffContent` or `OnContent`, "Off" and "On" by default) at 25.5 under the header at 16 in
  `ToggleSwitchHeaderForegroundThemeBrush`; the switch, a 2.5 outline in the foreground, the knob a 20.5-wide bar in
  the foreground with a stroke in the background's colour, left when off, right when on, the accent filling the track
  up to it.
- **A tap on the switch** toggles, and runs `Click` and `Command`. A tap is a press that moves less than 17.86 view px
  (2.7 mm on the reference phone) on the screen and is let go before 1,000 ms. A tap on the header or the words does
  nothing, as on the phone: only the switch, 13.5 either side of it, 15.5 above and 6.5 below, takes the finger.
- **A drag** moves the knob with the finger from the first move. Let go past half way it goes over, short of it back,
  and `IsChecked` changes without a `Click`. A tap that moved the knob puts it back first, then toggles.
- **The slide**: the knob and the accent slide to their rest over 370 ms on the phone's `KeySpline` 0.1,0.9 0.2,1,
  after a tap, a drag, or `IsChecked` set from code; a switch shown on is on at once.
- **In a scrolling page**, a drag on the switch never scrolls the page; a drag on the header or the words does.
- **Disabled**: the header, the words and the outline in `ToggleSwitchDisabledForegroundThemeBrush`, the knob and the
  accent in `#FF666666` (`#FFCCCCCC` light).
- **No pressed, pointer-over or focus look**, as on the phone.
- **Keys**: Avalonia's, Space and Enter toggle.
- **Dark and light**: every brush follows the theme variant; the accent follows the app's
  (`SystemColorControlAccentColor`).

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `ToggleSwitchForegroundThemeBrush`,
`ToggleSwitchHeaderForegroundThemeBrush`, `ToggleSwitchOuterBorderBorderThemeBrush`,
`ToggleSwitchTrackBorderThemeBrush`, `ToggleSwitchCurtainBackgroundThemeBrush`,
`ToggleSwitchThumbBackgroundThemeBrush`, `ToggleSwitchThumbBorderThemeBrush`, the `Disabled` keys
(`ToggleSwitchDisabledForegroundThemeBrush`, `ToggleSwitchOuterBorderDisabledBorderThemeBrush`,
`ToggleSwitchCurtainDisabledBackgroundThemeBrush`, `ToggleSwitchThumbDisabledBackgroundThemeBrush`,
`ToggleSwitchThumbDisabledBorderThemeBrush`), `ContentControlThemeFontFamily`, `ControlContentThemeFontSize`, and for
the header and the words `PhoneFontFamilyNormal`, `TextStyleMediumFontSize` and `TextStyleExtraLargeFontSize`. No new
key.

## The template's parts

A `Border` › a `Grid` › `HeaderContentPresenter` and `PART_ContentPresenter` (Avalonia's `Content`, in the same
place), a `Grid` of `PART_OffContentPresenter` and `PART_OnContentPresenter`, and `SwitchGrid` › `SwitchKnobBounds`
(`OuterBorder` › `InnerBorder` › `SwitchCurtainBounds` › `SwitchCurtain`; `PART_SwitchKnob`, a `Canvas`, ›
`PART_MovingKnobs` › `SwitchKnob`) and `SwitchThumb`, transparent, on top. A style that reaches into the template
uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, the template, the knob's travel, the curtain and the slide's curve; rung 2
for `Header` and `HeaderTemplate` (`ControlHeader`, attached), the line (`PhoneHeaderLine`), the phone's
`ManipulationMode` (`PhoneNoManipulation`) and the tap, the drag and where the finger is taken
(`PhoneToggleSwitch`, new, internal to the theme); rung 6 (recorded) for the gaps below. No type is substituted:
Avalonia's drag fails the phone's measurements, but an attached behaviour reaches every one of them through public
members, so no subclass is needed.

## Extensions

None. `Cornflower.Controls.ControlHeader` (#96) gives WinRT's `Header` and `HeaderTemplate`.

## Known gaps

- **Rounding** (ADR 0003): the switch is one raw pixel taller (87, not 86) and the knob one wider (50, not 49), so with
  no header the switch is 62.0833 tall, not 61.6667. On, the knob is at the travel, 136 raw px, on whole pixels; the
  phone's sits at 135.6.
- **Past the control's right edge**: the phone's `SwitchThumb` reaches 13.5 beyond it; Avalonia hit-tests nothing
  outside a control's bounds, so a tap there misses.
- **From on**, the drag's line is half the travel back; the phone's code puts it 0.17 nearer. Unmeasured.
- **`:dragging`** isn't raised: only Avalonia's own drag sets it.
- **The keyboard** is Avalonia's: Enter toggles too. Unmeasured on the phone.
- **The text is Selawik**, its baseline 0.025 em above the phone's (ADR 0003).
- **Owed**: a probe of a tap on the header and the words, and of a drag from on; the device run on the Pixel 8.

## The checklist

In [PLAN.md](PLAN.md#checklist): 58 rows done or kept as Avalonia's, 7 left out with their reasons.
