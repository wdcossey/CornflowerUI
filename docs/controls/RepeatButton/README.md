# RepeatButton

Status: done · [#30](https://github.com/wdcossey/CornflowerUI/issues/30) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page repeatbutton` on `main` at `764c435`: cold 817 ms, the page opened in 398 ms, every specimen drawn)

The Windows Phone 8.1 RepeatButton: Avalonia's `RepeatButton`, themed by `Wp81Theme` with the phone's default style,
template and timing, in `src/Cornflower.Themes.Wp81/Controls/RepeatButton.axaml`. The plan, with every value and its
source, is [PLAN.md](PLAN.md); the phone's captures and timing beside ours are in [COMPARISON.md](COMPARISON.md). Its
family's other member is [ToggleButton](../ToggleButton/README.md).

## Usage

As on the phone: a `RepeatButton` with no style is the phone's.

```xml
<!-- 8.1 -->
<RepeatButton Content="more" Click="OnMore" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<RepeatButton Content="more" Click="OnMore" />
```

It is Button's size: at least 109 × 57.5, left-aligned, its border 9.5 inside its top and bottom.

## Behaviours

- **At rest**: Button's look. **Pressed**: the accent inside, the text white over it. **Disabled**: the border and the
  text in `PhoneBaseLowColor`. The pointer over it changes nothing; keyboard focus draws nothing.
- **No tilt**: the phone's RepeatButton runs no theme animation.
- **The timing is the phone's**, read in the emulator: `ClickMode` `Press`, `Delay` 500, `Interval` 33. A press clicks
  at once; held, it repeats after half a second, then every 33 ms (the theme's setters; an app sets its own as it
  would on the phone).
- **Off the button the repeats stop**, while it stays pressed, and the next tick after the finger comes back clicks, as
  on the phone: `PhonePress`, internal to the theme. The keyboard's clicks are Avalonia's.
- **Dark and light**, and **the app's accent**.
- **Data's line** is the phone's 1.33 em, as Button's.
- **Other controls' templates**: a plain RepeatButton in a template written for the theme under Wp81 (Simple's
  ScrollBar, the menu ScrollViewer) keeps that theme's RepeatButton, timing and all, so scroll bars keep Simple's arrows.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `PhoneForegroundBrush`, `PhoneBorderThickness`,
`PhoneFontFamilyNormal`, `PhoneButtonFontWeight`, `TextStyleLargeFontSize`, `PhoneButtonContentPadding`,
`PhoneButtonMinHeight`, `PhoneButtonMinWidth`, `PhoneTouchTargetOverhang`, `ButtonPressedBackgroundThemeBrush`,
`ButtonPressedForegroundThemeBrush`, `ButtonDisabledForegroundThemeBrush`, `ButtonDisabledBorderThemeBrush`,
`ButtonDisabledBackgroundThemeBrush`. No new key. `ClickMode`, `Delay` and `Interval` are the control's own defaults on
the phone, not keys, so the theme sets them as values.

## The template's parts

A transparent `Panel` (takes the hits on the overhang) › `Border` (the phone's) › `PART_ContentPresenter` (the phone's
`ContentPresenter`).

## The ladder

Rung 1 (theme) for the setters, the states, the timing's values and the cleared focus adorner; rung 2 (attached
behaviour) for the repeats off the button (`PhonePress`), the phone's line (`PhoneContentLine`) and the holes'
RepeatButtons (`HoleParts`), all internal to the theme. No type is substituted.

## Extensions

None.

## Known gaps

- **The cadence**: the emulator repeated every 60 ms though its `Interval` reads 33; ours repeats every 33. A
  deliberate choice of the API's value, in the PR's "Decided on the user's behalf"; one setter changes it.
- **The text is Selawik**: "repeat button" 2.08 wider.
- **Access keys** are Avalonia's, as Button's.
- **A repeat's `Click` is held back by marking it handled**: a handler an app adds with `handledEventsToo` still sees
  it while the finger is off, and a `Flyout` on the button still opens and closes with each held-back repeat
  (Avalonia's Button does that before it raises Click).

## The checklist

In [PLAN.md](PLAN.md#checklist): 42 rows done or kept as Avalonia's, 6 left out with their reasons.
