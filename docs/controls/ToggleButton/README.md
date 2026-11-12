# ToggleButton

Status: done · [#30](https://github.com/wdcossey/CornflowerUI/issues/30) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page togglebutton` on `main` at `764c435`: cold 781 ms, the page opened in 377 ms, every specimen drawn)

The Windows Phone 8.1 ToggleButton: Avalonia's `ToggleButton`, themed by `Wp81Theme` with the phone's default style
and template, in `src/Cornflower.Themes.Wp81/Controls/ToggleButton.axaml`. The plan, with every value and its source,
is [PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md). Its family's other
member is [RepeatButton](../RepeatButton/README.md).

## Usage

As on the phone: a `ToggleButton` with no style is the phone's.

```xml
<!-- 8.1 -->
<ToggleButton Content="toggle button" IsChecked="True" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<ToggleButton Content="toggle button" IsChecked="True" />
```

It is Button's size: at least 109 × 57.5, left-aligned, its border 9.5 inside its top and bottom. Listen to
`IsCheckedChanged` where the phone had `Checked`, `Unchecked` and `Indeterminate`.

This theme is the plain ToggleButton's. CheckBox, RadioButton, ToggleSwitch and Avalonia's other subclasses each have
their own style key, so none of them takes it.

## Behaviours

- **Unchecked**: Button's look: a 2.5 border in the foreground, transparent inside, the text Semibold at 18.14.
- **Checked**: the accent inside the border, the text in `PhoneButtonBasePressedForegroundBrush`, white.
- **Pressed** changes nothing drawn, and **a checked one pressed shows the unchecked look**: the phone's
  `CheckedPressed` sets nothing. The release toggles. If the finger slides off a checked one while pressed, it shows
  checked again, and lifting there doesn't toggle it, as on the phone (`PhonePress`, internal to the theme).
- **Disabled**: the border and the text in `PhoneBaseLowColor`, transparent inside, whatever the app set.
  **Checked and disabled**: the checked look at 0.4, the border, accent and text together, as one layer.
- **Indeterminate** (`IsThreeState`, `IsChecked` null) looks unchecked; disabled, it looks disabled.
- **No tilt**: the phone's ToggleButton runs no theme animation.
- **The pointer over it** changes nothing; **keyboard focus** draws nothing.
- **Dark and light**, and **the app's accent**: every brush follows the variant and `SystemColorControlAccentColor`.
- **Data's line** is the phone's 1.33 em, as Button's.
- **Other controls' templates**: a plain ToggleButton in a template written for the theme under Wp81 (Simple's
  ComboBox, DrawerPage) keeps that theme's ToggleButton, not its Button.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `PhoneForegroundBrush`, `PhoneBorderThickness`,
`PhoneFontFamilyNormal`, `PhoneButtonFontWeight`, `TextStyleLargeFontSize`, `PhoneButtonContentPadding`,
`PhoneButtonMinHeight`, `PhoneButtonMinWidth`, `PhoneTouchTargetOverhang`, `PhoneAccentBrush`,
`PhoneButtonBasePressedForegroundBrush`, `ButtonDisabledBorderThemeBrush`, `ButtonDisabledForegroundThemeBrush`. No
new key. (`PhoneDisabledBrush` and `PhoneBackgroundBrush`, which the phone's `Checked` sets on parts it has collapsed,
are never drawn.)

## The template's parts

A transparent `Panel` (takes the hits on the overhang) › `EnabledBackground` (a `Border`, the phone's) ›
`PART_ContentPresenter` (the phone's `EnabledContent`). The phone's `DisabledBackground` and `DisabledContent` aren't
made: their look is drawn by the same Border and presenter when disabled.

## The ladder

Rung 1 (theme) for the setters and the states, the one-layer opacity and the cleared focus adorner; rung 2 (attached
behaviour, internal to the theme) for the slide off (`PhonePress`), the phone's line (`PhoneContentLine`) and the
holes' ToggleButtons (`HoleParts`, now by style key). No
type is substituted.

## Extensions

None.

## Known gaps

- **The text is Selawik**: "toggle button" 2.08 wider, the baseline 0.44 high (ADR 0003).
- **One Border and one presenter**, not the phone's two of each: an app's style that names `DisabledBackground` or
  `DisabledContent` finds nothing.
- **Access keys** are Avalonia's, as Button's: a deliberate difference.
- **Not captured on the phone**: a finger sliding back onto a pressed ToggleButton.
- **A finger held still while the ToggleButton moves away** keeps the pressed look until the finger moves.

## The checklist

In [PLAN.md](PLAN.md#checklist): 50 rows done or kept as Avalonia's, 13 left out with their reasons.
