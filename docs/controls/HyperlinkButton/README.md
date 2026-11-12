# HyperlinkButton

Status: done · [#29](https://github.com/wdcossey/CornflowerUI/issues/29) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page hyperlinkbutton` on `main` at `764c435`: cold 776 ms, the page opened in 372 ms, every specimen drawn) · real-window run passed 2026-11-12 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `49e2311`: 56 of 56, the hyperlinkbutton page opened in 12 ms with 53 elements) · owed: a probe of the release

The Windows Phone 8.1 HyperlinkButton: Avalonia's `HyperlinkButton`, themed by `Wp81Theme` with the phone's default
style and template, in `src/Cornflower.Themes.Wp81/Controls/HyperlinkButton.axaml`. The plan, with every value and
its source, is [PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `HyperlinkButton` with no style is the phone's.

```xml
<!-- 8.1 -->
<HyperlinkButton Content="hyperlink button" NavigateUri="https://example.com" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<HyperlinkButton Content="hyperlink button" NavigateUri="https://example.com" />
```

It is plain text, as big as the text and its padding (0 at the sides, 3 above, 4 below), left-aligned and vertically
centred in its slot. It has no minimum size, no border and no underline.

## Behaviours

- **At rest**: the text in the foreground, `PhoneFontFamilySemiLight` Semilight at 29.86, on a transparent
  background. No underline, no accent.
- **Pressed**: the text tilts towards the finger and sinks, follows it, and on release holds 182.6 ms and eases back
  over 167 ms: `Tilt`, turned on by the theme, drawn on the template's `PART_ContentPresenter` alone, as the phone's
  `PointerDownThemeAnimation` is. No colour changes.
- **A press anywhere on it clicks**, the padding and the very edge of the tilted text included: the `Grid` and
  `Border` around the presenter stay still, and `Grid`'s transparent background takes the hits, whatever the app's
  `Background`.
- **Disabled**: the text in `PhoneDisabledColor` (#66 of the foreground). An app's background stays.
- **The pointer over it** changes nothing, and nor does **visited** (`IsVisited`, `:visited`): the phone has no
  visited state.
- **Dark and light**: every brush follows the theme variant.
- **Data's line** (a string, a number) is the phone's, 1.33 × `FontSize`. An app's `ContentTemplate` or
  `DataTemplate` keeps its own.
- **Keyboard focus** draws nothing, as the phone's template has no focus states.
- **`NavigateUri`** is Avalonia's: a click opens it through the platform's launcher.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`, so an app or an extension pack overrides them by key
([ADR 0004](../../decisions/0004-larger-screens-as-an-extension-pack.md)): `HyperlinkForegroundThemeBrush`,
`HyperlinkButtonBackgroundThemeBrush`, `HyperlinkButtonBorderThemeBrush`, `HyperlinkButtonBorderThemeThickness`,
`PhoneFontFamilySemiLight`, `TextStyleExtraLargePlusFontSize`, `HyperlinkDisabledThemeBrush`. No new key. The
padding (`0,3,0,4`) and the weight (`SemiLight`) are literals in the phone's style, and here.
`HyperlinkPressedForegroundThemeBrush` and `HyperlinkPointerOverForegroundThemeBrush` are in `Tokens/` but unused,
as the phone's template doesn't use them either.

## The template's parts

`Grid` (a transparent `Panel`, the phone's unnamed `Grid`, which takes the hits) › `Border` (the phone's) › `PART_ContentPresenter` (the phone's
`ContentPresenter`, under the name Avalonia's Button looks for, and `Tilt.IsTarget`). A style that reaches into the
template uses these names.

## The ladder

Rung 1 (theme) for the setters, the disabled state and the cleared focus adorner; rung 2 (attached behaviour) for the
tilt (`Tilt`, `Cornflower.Controls`) and the phone's line (`PhoneContentLine` and `PhoneTextLine`, internal to the
theme). No type is substituted.

## Extensions

None.

## Known gaps

- **The release isn't measured.** The phone's template has no transitions, so its text likely snaps back on
  release; ours eases back as a Button does. Owed: a probe pressing a HyperlinkButton.
- **The tilt's rule is taken from the Button's probes**: the same `PointerDownThemeAnimation`, on the presenter.
- **The text is Selawik Semilight**, its baseline 0.83 DIP above the phone's at 29.86 (ADR 0003), and "hyperlink
  button" 0.42 wider.
- **Only data gets the phone's line**, as in the Button's theme.
- **Access keys** are Avalonia's, where the phone had none: a deliberate difference, as in the Button's theme.
- **No hand cursor**, where Simple's theme sets one: the phone has no cursor.
- **`Grid` has a transparent background**, where the phone's has none, so a control whose app sets `Background` to
  null still takes a press anywhere on it, not only on its text.

## The checklist

In [PLAN.md](PLAN.md#checklist): 32 rows done or kept as Avalonia's, 6 left out with their reasons.
