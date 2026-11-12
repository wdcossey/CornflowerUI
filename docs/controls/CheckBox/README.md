# CheckBox

Status: done · [#31](https://github.com/wdcossey/CornflowerUI/issues/31) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page checkbox` on `main` at `764c435`: cold 786 ms, the page opened in 402 ms, every specimen drawn)

The Windows Phone 8.1 CheckBox: Avalonia's `CheckBox`, themed by `Wp81Theme` with the phone's default style and
template, in `src/Cornflower.Themes.Wp81/Controls/CheckBox.axaml`. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `CheckBox` with no style is the phone's.

```xml
<!-- 8.1 -->
<CheckBox Content="check box" IsChecked="True" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<CheckBox Content="check box" IsChecked="True" />
```

It is at least 168 wide and 57.5 tall, top-left in its slot, with its box and text drawn 16 inside its top and
bottom: the touch target is taller than what is drawn. The text is `PhoneFontFamilyNormal` at
`TextStyleLargeFontSize`, Normal, set by the template, as on the phone: the CheckBox's own `FontSize` doesn't change
it. For other text, give the CheckBox a `ContentTemplate` (a `TextBlock` with one of the type ramp's styles, say).

## Behaviours

- **At rest**: a 25.5 box with a 2.5 border in the foreground, its background transparent, the text 8 to its right.
- **Checked**: the tick, `Wp81.Glyph.CheckMark`, in the foreground, in the box's middle.
- **Indeterminate** (`IsThreeState`, `IsChecked` null): the template's 13-square rectangle, filled with
  `CheckBoxBackgroundThemeBrush`, which is transparent: an indeterminate CheckBox looks unchecked, as on the phone.
  An app that wants it seen sets that key.
- **A tap toggles**: unchecked, checked, and, three-state, indeterminate, then unchecked.
- **Pressed**: the box fills with the accent, the tick white over it; the border and the text stay. The box and the
  text tilt towards the finger and sink, and ease back on release: `Tilt`, turned on by the theme, drawn on the
  template's `Grid`, as the phone's `PointerDownThemeAnimation` is.
- **A press anywhere on it toggles**, the overhang above and below and the text included: the template's root,
  untilted, takes the hits, as the phone's does.
- **Disabled**: the border, the tick and the text in `PhoneBaseLowColor`; an app's background stays.
- **The pointer over it** changes nothing, as on the phone. **Keyboard focus** draws nothing.
- **Other controls' templates**: a plain CheckBox in a template written for the theme under Wp81 (Simple's file
  dialog's "show hidden files") keeps that theme's CheckBox (`HoleParts`). An app's own template has the phone's.
- **Dark and light**: every brush follows the theme variant; the pressed box follows the app's accent
  (`SystemColorControlAccentColor`).
- **Data's line** (a string, a number) is the phone's, 1.33 × the font size. **Access keys** are Avalonia's:
  `_check` draws `check`, and Alt+C toggles it.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `CheckBoxBackgroundThemeBrush`,
`CheckBoxBorderThemeBrush`, `CheckBoxForegroundThemeBrush`, `PhoneForegroundBrush`, `PhoneBorderThickness`,
`TextStyleLargeFontSize`, `PhoneFontFamilyNormal`, `CheckBoxAndRadioButtonTextPaddingThickness`,
`CheckBoxAndRadioButtonMinWidthSize`, `PhoneTouchTargetLargeOverhang`, `CheckBoxPressedBackgroundThemeBrush`,
`CheckBoxPressedForegroundThemeBrush`, `CheckBoxDisabledBorderThemeBrush`, `CheckBoxDisabledForegroundThemeBrush`,
`CheckBoxDisabledBackgroundThemeBrush`; and the glyph `Wp81.Glyph.CheckMark`. No new key.

## The template's parts

`Root` (a transparent `Panel`, untilted, takes the hits) › `Grid` (`Tilt.IsTarget`, the phone's `Grid`, two
columns, in the overhang) › a `Panel` holding `CheckBackground` (`Border`), `NormalRectangle` (`Rectangle`) and
`CheckGlyph` (`Path`), and `PART_ContentPresenter` (the phone's `ContentPresenter`, under the name Avalonia looks
for). A style that reaches into the template uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, the glyph, the cleared focus adorner and the untilted root; rung 2
(attached behaviour) for the tilt (`Tilt`), the phone's line (`PhoneContentLine`) and the holes' CheckBoxes
(`HoleParts`), the last two internal to the theme. No type is substituted.

## Extensions

None.

## Known gaps

- **Rounding** (ADR 0003): the box is one raw pixel wider and taller than the phone's, at its right and bottom, and
  the tick one raw pixel left of the phone's.
- **`Foreground` is the theme's**, `PhoneForegroundBrush`, where the phone's CheckBox inherits its page's: an app's
  ancestor `Foreground` doesn't reach it. Set it on the CheckBox.
- **The text is Selawik**, its baseline 0.025 em above the phone's (ADR 0003).
- **The tilt on a CheckBox isn't measured on the phone**: its target is flatter than any Button probed, and a press in
  the overhang, outside the target, takes the tilt of the nearest edge. Nor is sliding off while pressed.
- **Device run**: passed on the Pixel 8, 2026-11-12 (the status line).

## The checklist

In [PLAN.md](PLAN.md#checklist): 52 rows done or kept as Avalonia's, 9 left out with their reasons.
