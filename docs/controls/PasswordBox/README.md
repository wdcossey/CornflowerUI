# PasswordBox

Status: done · [#37](https://github.com/wdcossey/CornflowerUI/issues/37) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page passwordbox` on `main` at `3bcf02e`: cold 804 ms, the page opened in 410 ms, the first screen's specimens drawn, typing shows circles and Show Password shows the text) · real-window run passed 2026-11-12 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `3bcf02e`: 71 of 71, the passwordbox page opened in 25 ms with 98 elements) · owed: a capture of a shown password

The Windows Phone 8.1 PasswordBox: Avalonia's `TextBox` with the `ControlTheme` keyed `PasswordBox`, WinRT's name, the
phone's default style and template, in `src/Cornflower.Themes.Wp81/Controls/PasswordBox.axaml`. Avalonia has no
PasswordBox type: its TextBox masks with `PasswordChar` and shows with `RevealPassword`. The plan, with every value,
its source and why it is a key, is [PLAN.md](PLAN.md); the phone's captures beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

```xml
<!-- 8.1 -->
<PasswordBox Header="password" IsPasswordRevealButtonEnabled="True" />
<!-- Avalonia, with Wp81Theme in Application.Styles; xmlns:cf="using:Cornflower.Controls" -->
<TextBox Theme="{DynamicResource PasswordBox}" Classes="revealPasswordButton" cf:ControlHeader.Header="password" />
```

WinRT's `Password` is `Text`, `PasswordChanged` is `TextChanged`, `IsPasswordRevealButtonEnabled` is the
`revealPasswordButton` class (Avalonia's own, as Simple and Fluent read it). A `TextBox` with a `PasswordChar` and no
key is already the phone's PasswordBox without the reveal button, as WinRT's is by default; the key gives the `●`, one
line, the reveal button and the phone's pointer-over look.

## Behaviours

- **As the TextBox** (#36): the box at 0.8 at rest, the accent round white focused, the border alone disabled; the
  header; the placeholder while empty, here at the box's own size; the text in the phone's line.
- **Masked** with `●`, WinRT's default `PasswordChar`, on one line with no return.
- **The keyboard is told it's a password** (`TextInputOptions`: `ContentType` `Password`, `IsSensitive`, no
  suggestions), masked or shown, as by every TextBox with a `PasswordChar` under Wp81 (`PhonePasswordKeyboard`, the
  TextBox's too): on the Pixel 8 Gboard had suggested words from a typed password (#104), and with this shows none,
  masked or shown. An app's own `TextInputOptions` on the box, its style's or a derived theme's win (a PIN box's `Pin`). Whether the phone's own keyboard suggests in a PasswordBox is unmeasured.
- **"Show Password"**: with the `revealPasswordButton` class, Wp81's CheckBox below the box, lifted 6.5 into the box's
  row, at the left; ticking it is `RevealPassword`, which shows the password, and setting `RevealPassword` ticks it. It
  shows whether or not there is a password, as on the phone, but not on a read-only or many-line box. A tap on it
  leaves a focused box focused (an unfocused one it focuses, where the phone's wouldn't). When it shows or hides it is unticked, as the phone's `ButtonStates` untick it, so a password is never left
  shown with nothing to hide it. Its text is `Wp81PasswordBoxRevealText`, "Show Password", which an app can define for
  its language.
- **The border with the reveal button** is what the box's height leaves, as on the phone: on the reference app's stage
  31.25 tall, the text overflowing it below; with room, the TextBox's 39.1667.
- **Under a pointer** (a mouse) the box is drawn whole, as the phone's empty `PointerOver` state ends the 0.8.
- **Disabled**, the CheckBox is disabled too.
- **Dark and light**: every brush follows the theme variant; the focus border and the selection follow the accent.

## Resource keys

The TextBox's (#36's README), and `RichEditBoxTextThemeMargin` (the content's inset), `TextBoxForegroundHeaderThemeBrush`
(the CheckBox's text) and `PasswordBoxCheckBoxThemeMargin` (its 6.5 lift), all in `Tokens/`. The theme's key is
`PasswordBox`; the CheckBox's text is `Wp81PasswordBoxRevealText`. It shares the TextBox's `Wp81TextBoxScrollViewerTheme`.

## The template's parts

A `Grid` › `BorderElement`, `HeaderContentPresenter`, `ContentRow` › `InnerLeftContentPresenter`, `PART_ScrollViewer`
(the phone's `ContentElement`) › `PART_TextPresenter`, `PlaceholderTextContentPresenter` and
`InnerRightContentPresenter`, and `RevealButton`, a `CheckBox`.

## The ladder

Rung 1 (theme): a keyed `ControlTheme` for Avalonia's `TextBox`, the reveal button by Avalonia's class, Wp81's CheckBox
for it; rung 2 for `Header` (`ControlHeader`) and the lines (`PhoneHeaderLine`, `PhoneTextBoxLine`, shared with the
TextBox); `PhoneRevealButton`, internal, for the `ButtonStates`' unticking; rung 6 (recorded) for the baseline and the rounding. No type is substituted: no PasswordBox
class.

## Extensions

None new.

## Known gaps

- **Rounding** (ADR 0003): the box ends one raw pixel short; the CheckBox's square is one raw pixel larger (#31's).
- **The text's baseline**, and so the circles, 0.95 DIP above the phone's (#36's).
- **Selawik has no `●`**: the circles come from the platform's fallback font.
- **A tap on "Show Password" focuses an unfocused box**; the phone's leaves focus where it was.
- **The keyboard's options inherit** (Avalonia's): a control an app puts in `InnerLeftContent`, `InnerRightContent` or
  the header gets a password keyboard too.
- **A `PasswordChar` set or cleared on a focused box** reaches the keyboard at the next focus: the keyboard reads the
  options when the box takes it.
- **Unmeasured on the phone**: a shown password, the caret, the selection and the keyboard.

## The checklist

In [PLAN.md](PLAN.md#checklist): 60 rows done or mapped to Avalonia's TextBox, 3 left out with their reasons.
