# TextBox

Status: done · [#36](https://github.com/wdcossey/CornflowerUI/issues/36) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page textbox` on `main` at `7dddcde`: cold 789 ms, the page opened in 401 ms, the first screen's specimens drawn, a tap focuses a box with the accent round it, and typing works) · real-window run passed 2026-11-12 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `3bcf02e`: 71 of 71, the textbox page opened in 62 ms with 123 elements) · owed: captures of a caret, a selection and the keyboard

The Windows Phone 8.1 TextBox: Avalonia's `TextBox`, themed by `Wp81Theme` with the phone's default style and
template, in `src/Cornflower.Themes.Wp81/Controls/TextBox.axaml`. WinRT's `Header` is `cf:ControlHeader.Header`,
attached. The plan, with every value and its source, is [PLAN.md](PLAN.md); the phone's captures beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `TextBox` with no style is the phone's. The header is attached; `PlaceholderText` is WinRT's name
and Avalonia's.

```xml
<!-- 8.1 -->
<TextBox Header="name" PlaceholderText="type your name" />
<!-- Avalonia, with Wp81Theme in Application.Styles; xmlns:cf="using:Cornflower.Controls" -->
<TextBox cf:ControlHeader.Header="name" PlaceholderText="type your name" />
```

It stretches across its slot, 9.5 above and below. With no header it is 39.1667 tall, 2.5 of border round at least 34;
a header adds its line and 4.8.

## Behaviours

- **At rest**: a white box (`#FFBFBFBF` light) with a 2.5 border of the same, at 0.8 over the page, so it reads 204
  grey in both themes; black text at 20.26 in the phone's 1.33 em line, 7 in and 0.8 down.
- **The header**: `cf:ControlHeader.Header`, shown above in `PhoneMidBrush` at 16, in the phone's line, 4.8 above the
  box; `cf:ControlHeader.HeaderTemplate` shows it with a template. With none, no room is taken.
- **The placeholder**: `PlaceholderText`, in `TextBoxPlaceholderTextThemeBrush` (or `PlaceholderForeground`) at
  20.26 whatever the box's size, where the text starts, while the box is empty and not focused.
- **Focused** (a tap or the keyboard): white, with the accent round it, at full opacity.
- **Disabled**: no background, the border `TextBoxDisabledBorderThemeBrush`, the text and the placeholder
  `#FF747474` (`#FFBFBFBF` light), the header dimmed.
- **More lines** (`TextWrapping="Wrap"`, `AcceptsReturn="True"`): each 1.33 em below the last, the box growing a line
  at a time. `MinLines` and `MaxLines` count the phone's lines. An app's `LineHeight` wins.
- **Scrolling**: long text scrolls sideways inside the padding, and a touch drag pans it. No scroll bars.
- **No pointer-over look, no focus rectangle, no context menu, no clear button**, as on the phone.
- **Selection** in the accent. The caret, the selected text's colour and the keyboard are Avalonia's and the
  platform's (`TextInputOptions` for the keyboard's kind, WinRT's `InputScope`).
- **Masked** (any `PasswordChar`), the box tells the keyboard it holds a password: `TextInputOptions.ContentType`
  `Password`, `IsSensitive`, no suggestions (`PhonePasswordKeyboard`, internal; an app's own values, its style's or a
  derived theme's win; a box with no `PasswordChar` is left as Avalonia leaves it). On the Pixel 8 Gboard had suggested
  words from a typed password (#104), and with this shows none.
- **In Simple's templates** (AutoCompleteBox, CalendarDatePicker, ComboBox's editable box, the file dialog,
  NumericUpDown), a TextBox keeps Simple's theme (`HoleParts`), as their Buttons do.
- **Dark and light**: every brush follows the theme variant; the focus border and the selection follow the app's
  accent (`SystemColorControlAccentColor`).

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `TextControlThemeMinWidth`, `TextControlThemeMinHeight`,
`TextBoxForegroundThemeBrush`, `TextSelectionHighlightColorThemeBrush`, `TextBoxBackgroundThemeBrush`,
`TextBoxBorderThemeBrush`, `TextControlBorderThemeThickness`, `PhoneFontFamilyNormal`, `ContentControlFontSize`,
`TextControlThemePadding`, `TextControlMarginThemeThickness`, `TextControlBorderThemeOpacity`,
`TextBoxFocusedBackgroundThemeBrush`, `TextBoxPlaceholderTextThemeBrush`, `TextControlHeaderMarginThemeThickness`, the
`Disabled` keys (`TextBoxDisabledBackgroundThemeBrush`, `TextBoxDisabledBorderThemeBrush`,
`TextBoxDisabledForegroundThemeBrush`, `TextBoxDisabledHeaderForegroundThemeBrush`), and for the header
`TextStyleMediumFontSize` and `PhoneMidBrush`. No new key. The part's `ScrollViewer` theme is keyed
`Wp81TextBoxScrollViewerTheme`.

## The template's parts

A `Grid` › `BorderElement`, `HeaderContentPresenter`, and `ContentRow` › `InnerLeftContentPresenter`,
`PART_ScrollViewer` (the phone's `ContentElement`) › `PART_TextPresenter`, `PlaceholderTextContentPresenter` and
`InnerRightContentPresenter`. A style that reaches into the template uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, the template, the placeholder, the part's `ScrollViewer` theme and the
cleared focus adorner; rung 2 for `Header` and `HeaderTemplate` (`ControlHeader`, attached), the header's and the
placeholder's line (`PhoneHeaderLine`) and the text's (`PhoneTextBoxLine`, a converter the style binds the box's
`LineHeight` through), all but `ControlHeader` internal to the theme; rung 6 (recorded) for the baseline and the
rounding. No type is substituted.

## Extensions

None new. `Cornflower.Controls.ControlHeader` (#96) gives WinRT's `Header` and `HeaderTemplate`.

## Known gaps

- **Rounding** (ADR 0003): stretched in the reference app's slot, the box ends one raw pixel short of the phone's.
- **The text's baseline** is 0.95 DIP above the phone's (ADR 0003's 0.025 em and 0.44 more); the header's 0.1.
- **At rest in light** the box is 203 grey, the phone's 204: Skia's blend.
- **`MaxLines`** leaves 0.56 of the last line's bottom below the view (Avalonia's measure); the phone has none.
- **No `SelectionChanged` event, `GetRectFromCharacterIndex`, `IsSpellCheckEnabled`, `IsColorFontEnabled` or
  `PreventKeyboardDisplayOnProgrammaticFocus`**: Avalonia has none.
- **Data validation shows nothing**, as the phone has no such display; Simple's `DataValidationErrors` isn't in the
  template.
- **Unmeasured**: the caret, the selection's text colour and handles, the keyboard, and scrolling.
- **The text is Selawik** (ADR 0001).

## The checklist

In [PLAN.md](PLAN.md#checklist): 63 rows done or kept as Avalonia's, 7 left out with their reasons.
