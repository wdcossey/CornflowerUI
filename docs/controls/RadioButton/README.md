# RadioButton

Status: done · [#32](https://github.com/wdcossey/CornflowerUI/issues/32) · owed: the device run on the Pixel 8, and a probe of the tilt and its release

The Windows Phone 8.1 RadioButton: Avalonia's `RadioButton`, themed by `Wp81Theme` with the phone's default style
and template, in `src/Cornflower.Themes.Wp81/Controls/RadioButton.axaml`. The plan, with every value and its source,
is [PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `RadioButton` with no style is the phone's.

```xml
<!-- 8.1 -->
<StackPanel>
  <RadioButton Content="red" />
  <RadioButton Content="green" IsChecked="True" />
</StackPanel>
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<StackPanel>
  <RadioButton Content="red" />
  <RadioButton Content="green" IsChecked="True" />
</StackPanel>
```

It is at least 168 wide and 57.5 tall, top-left in its slot, with its circle and text drawn 16 inside its top and
bottom: the touch target is taller than what is drawn. The text is the RadioButton's own font, `PhoneFontFamilyNormal`
at `TextStyleLargeFontSize` unless the app sets another, as on the phone.

## Behaviours

- **At rest**: a 25.5 circle with a 2.5 ring in the foreground, its inside transparent, the text 8 to its right.
- **Checked**: a 12.5 dot in the foreground, in the circle's middle. Unchecked and indeterminate show none.
- **A tap checks it**, and a tap on a checked one leaves it checked.
- **One checked in a group**: checking one unchecks the others with the same `GroupName`, or, with none, the others
  in the same parent. This is Avalonia's `RadioButton`, as WinRT's; the theme keeps it.
- **Pressed**: the circle fills with the accent, the dot white over it; the ring and the text stay. The circle and the
  text tilt towards the finger and sink, and ease back on release: `Tilt`, turned on by the theme, drawn on the
  template's `Container`, as the phone's `PointerDownThemeAnimation` is.
- **A press anywhere on it checks it**, the overhang above and below and the text included: the template's root,
  untilted, takes the hits, as the phone's does.
- **Disabled**: the ring, the dot and the text in `PhoneBaseLowColor`; an app's background stays.
- **The pointer over it** changes nothing, as on the phone. **Keyboard focus** draws nothing.
- **Dark and light**: every brush follows the theme variant; the pressed circle follows the app's accent
  (`SystemColorControlAccentColor`).
- **A larger font**: the circle is centred on the text's first line, through the template's empty `TextBlock`, here
  `PhoneLineSpacer`, one of the phone's lines of the RadioButton's font tall. An app's `TextBlock` style doesn't reach
  it.
- **Data's line** (a string, a number) is the phone's, 1.33 × the font size. **Access keys** are Avalonia's:
  `_radio` draws `radio`, and Alt+R checks it.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `PhoneRadioCheckBoxBrush`,
`PhoneRadioCheckBoxBorderBrush`, `PhoneRadioCheckBoxCheckBrush`, `PhoneForegroundBrush`, `PhoneStrokeThickness`,
`TextStyleLargeFontSize`, `PhoneFontFamilyNormal`, `CheckBoxAndRadioButtonTextPaddingThickness`,
`CheckBoxAndRadioButtonMinWidthSize`, `PhoneTouchTargetLargeOverhang`, `ButtonPressedBackgroundThemeBrush`,
`ButtonPressedForegroundThemeBrush`, `ButtonDisabledBorderThemeBrush`, `ButtonDisabledForegroundThemeBrush`. The
states use the Button's keys, as the phone's template does, not the `RadioButton*ThemeBrush` keys. No new key.

## The template's parts

`Root` (a transparent `Panel`, untilted, takes the hits) › `Container` (`Tilt.IsTarget`, the phone's `Container`,
two columns, in the overhang) › a `Panel` holding `CheckBackground` (`Ellipse`), `CheckMark` (`Ellipse`) and a
`PhoneLineSpacer` (the phone's empty `TextBlock`), and `PART_ContentPresenter` (the phone's `ContentPresenter`, under the name Avalonia looks for).
A style that reaches into the template uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, the cleared focus adorner and the untilted root; rung 2 (attached
behaviour) for the tilt (`Tilt`) and the phone's line (`PhoneContentLine`, internal to the theme). `PhoneLineSpacer`,
internal to the theme, is the template's empty `TextBlock`. No type is substituted.

## Extensions

None.

## Known gaps

- **Rounding** (ADR 0003): the circle is one raw pixel wider and taller than the phone's, at its right and bottom.
  The dot is on the phone's pixels.
- **`Foreground` is the theme's**, `PhoneForegroundBrush`, where the phone's RadioButton inherits its page's: an
  app's ancestor `Foreground` doesn't reach it. Set it on the RadioButton.
- **The text is Selawik**, its baseline 0.025 em above the phone's (ADR 0003).
- **The tilt on a RadioButton isn't measured on the phone**, nor its release: the template has no transition, so the
  phone's may snap back where ours eases, as the Button's does.
- **Owed**: the device run on the Pixel 8.

## The checklist

In [PLAN.md](PLAN.md#checklist): 47 rows done or kept as Avalonia's, 9 left out with their reasons.
