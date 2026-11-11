# Button

Status: done · [#27](https://github.com/wdcossey/CornflowerUI/issues/27) · device run passed 2026-11-11 (Pixel 8, Release, `run.ps1 -Page button`: cold 643 ms, the page opened in 243 ms, every state drawn) · real-window run passed 2026-11-11 (`tools/real-window/drive.ps1` on `main` at `bdd1c62`: 16 of 16, the button page opened in 17 ms with 88 elements)

The Windows Phone 8.1 Button: Avalonia's `Button`, themed by `Wp81Theme` with the phone's default style and
template, in `src/Cornflower.Themes.Wp81/Controls/Button.axaml`. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `Button` with no style is the phone's.

```xml
<!-- 8.1 -->
<Button Content="button" Click="OnClick" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<Button Content="button" Click="OnClick" />
```

It is at least 109 × 57.5, left-aligned and vertically centred in its slot, with its border drawn 9.5 inside its
top and bottom: the touch target is taller than what is drawn. To fill a row, set `HorizontalAlignment="Stretch"`,
as an 8.1 page does. A Button that must be smaller than the phone's minimum (chrome, an icon) sets `MinWidth` and
`MinHeight` itself.

## Behaviours

- **At rest**: a 2.5 border in the foreground, a transparent background, the text in `PhoneFontFamilyNormal`
  Semibold at 18.14, padded 9.5 at the sides and 3.5 below.
- **Pressed**: the background is the accent and the text `PhoneTextOverAccentColor`, white, in both themes; the
  border stays. The Button tilts towards the finger and sinks, follows it, and on release holds 180 ms and eases
  back over 195 ms: `Tilt`, turned on by the theme, drawn on the template's `Grid`, the whole Button, as the
  phone's `PointerDownThemeAnimation` is.
- **A press anywhere on it clicks**, the overhang above and below the border and the very edge of a tilted Button
  included, as on the phone: the template's root, untilted, takes the hits.
- **Disabled**: the border and the text in `PhoneBaseLowColor`, the background transparent, whatever the app set.
- **The pointer over it** changes nothing, as on the phone.
- **Dark and light**: every brush follows the theme variant; the pressed background follows the app's accent
  (`SystemColorControlAccentColor`).
- **Data's line** (a string, a number) is the phone's, 1.33 × `FontSize`, so the content is as tall as on the phone
  at any size. An app's `ContentTemplate` or `DataTemplate` keeps its own.
- **Keyboard focus** draws nothing, as the phone's template has no focus states.
- **Other controls' templates**: a plain Button in a template written for the theme under Wp81 (Simple's
  CalendarItem, CalendarDatePicker, CommandBar, NavigationPage, PipsPager, the file dialogs) keeps that theme's
  Button. The template decides, not the control's type: an app's own template, or a theme of the app's above Wp81,
  has the phone's Button.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`, so an app or an extension pack overrides them by
key ([ADR 0004](../../decisions/0004-larger-screens-as-an-extension-pack.md)): `PhoneForegroundBrush`,
`PhoneBorderThickness`, `PhoneFontFamilyNormal`, `PhoneButtonFontWeight`, `TextStyleLargeFontSize`,
`PhoneButtonContentPadding`, `PhoneButtonMinHeight`, `PhoneButtonMinWidth`, `PhoneTouchTargetOverhang`,
`ButtonPressedBackgroundThemeBrush`, `ButtonPressedForegroundThemeBrush`, `ButtonDisabledForegroundThemeBrush`,
`ButtonDisabledBorderThemeBrush`, `ButtonDisabledBackgroundThemeBrush`. No new key.

## The template's parts

`Root` (a transparent `Panel`, untilted, takes the hits) › `Grid` (a `Panel`, `Tilt.IsTarget`, the phone's `Grid`)
› `Border` (the phone's) › `PART_ContentPresenter` (the phone's `ContentPresenter`, under the name Avalonia's Button
looks for). A style that reaches into the template uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, the cleared focus adorner and the untilted root; rung 2 (attached
behaviour) for the tilt (`Tilt`, `Cornflower.Controls`), the phone's line (`PhoneContentLine` and `PhoneTextLine`,
internal to the theme) and the holes' Buttons (`HoleParts`, internal to the theme). No type is substituted.

## Extensions

None.

## Known gaps

- **The text is Selawik**, its baseline 0.44 DIP above the phone's at 18.14 (ADR 0003), and "button" 0.83 wider.
- **Only data gets the phone's line.** Content that is an element (an app's own `TextBlock`) keeps Selawik's 1.2 em unless
  it has a text style.
- **Sliding off while pressed**: Avalonia's Button stays `:pressed` until the finger lifts; whether the phone's
  leaves `Pressed` isn't measured yet. The tilt returns as the phone's does.
- **Access keys** are Avalonia's, where the phone had none: `_Open` draws `Open`, and Alt+O clicks it. A deliberate
  difference.
- **An app data template whose root is an `AccessText`** gets the phone's line, as the theme's own text does.
- **Not captured on the phone**: Pressed under a real touch (the capture is the state forced with no finger), and
  sliding off. The colours are the template's, so neither blocks the control.

## The checklist

In [PLAN.md](PLAN.md#checklist): 45 rows done or kept as Avalonia's, 6 left out with their reasons.
