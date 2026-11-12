# ProgressBar

Status: done · [#35](https://github.com/wdcossey/CornflowerUI/issues/35) · owed: the device run on the Pixel 8

The Windows Phone 8.1 ProgressBar: Avalonia's `ProgressBar`, themed by `Wp81Theme` with the phone's default style and
template, in `src/Cornflower.Themes.Wp81/Controls/ProgressBar.axaml`, and its indeterminate dots run frame by frame by
`PhoneProgressBar`. The plan, with every value and its source, is [PLAN.md](PLAN.md); the phone's captures beside ours
are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `ProgressBar` with no style is the phone's.

```xml
<!-- 8.1 -->
<ProgressBar Value="{Binding Percent}" />
<ProgressBar IsIndeterminate="True" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<ProgressBar Value="{Binding Percent}" />
<ProgressBar IsIndeterminate="True" />
```

It stretches across its slot, 3.5 high (`MinHeight`), centred down it. It takes no input and no focus.

## Behaviours

- **Determinate**: the track across the bar, the accent at 0.16 (`Background`), and the indicator from the left, the
  value's share of the bar inside its `Padding`, in the accent (`Foreground`). A new value moves it at once.
- **Indeterminate**: the track goes, and five small squares, 3.5 across and 12.5 apart, slide in from the left in turn,
  bunch up a third of the way across, and slide on and out to the right, while their row drifts right: the phone's
  storyboard, repeating every 3.917 s, from its start whenever the bar goes indeterminate or comes back into a window.
  Their travel scales with the bar's width, as the phone's does. They are drawn only on the bar.
- **The squares keep the accent** (`ProgressBarIndeterminateForegroundThemeBrush`) whatever the bar's `Foreground`, as
  on the phone.
- **Disabled** looks the same: the phone's template has no disabled state.
- **High contrast**: two 0.8 bars, at the bottom and the right, show while determinate, in
  `ProgressBarHighContrastAccentBarThemeBrush`. It is transparent in the dark and light themes.
- **Vertical** (`Orientation`, Avalonia's, not the phone's): the phone's bar turned, at least 3.5 wide, centred across
  its slot and stretched down it, the indicator filling from the bottom. Indeterminate, a vertical bar shows its
  track alone: the phone's dots run only across.

The storyboard runs in the theme, frame by frame on `CornflowerClock`, so its tests are exact to the millisecond. It
works out the phone's `TemplateSettings` from the bar's width, as the phone does: Avalonia's own `TemplateSettings`
are left as Avalonia sets them.

## The checklist

[PLAN.md](PLAN.md#the-checklist) has the table, with what each row became. In short: `IsIndeterminate`, `Value`,
`Minimum`, `Maximum` and the rest of `RangeBase`, Avalonia's; `TemplateSettings`, the phone's worked out in the theme;
the 8 setters, all but `Maximum`, which is already 100 in Avalonia; all 15 parts, as `PART_` names, the indicator as Avalonia's
`PART_Indicator`; `Determinate` and `Indeterminate`, through `:indeterminate`; all 8 resource keys, already in `Tokens/`,
and the template's `ProgressBarRectangleStyle`, as the dots' style. Left out: `ShowPaused` and `ShowError`, with the
`Paused` and `Error` states, and the three transitions.

## Resource keys

`ProgressBarForegroundThemeBrush`, `ProgressBarBackgroundThemeBrush`, `ProgressBarBorderThemeBrush`,
`ProgressBarBorderThemeThickness`, `ProgressBarThemeMinHeight`, `ProgressBarIndeterminateRectagleThemeSize` (the phone's
spelling), `ProgressBarIndeterminateForegroundThemeBrush` and `ProgressBarHighContrastAccentBarThemeBrush`, by
`DynamicResource`.

## The ladder

Rung 1, a theme, with the storyboard as the theme's timeline. No type substitution: the indicator is Avalonia's
`PART_Indicator`, a `Border`, where the phone's is a `Rectangle`.

## Known gaps

- **`Paused` and `Error` can't be shown**: Avalonia's `ProgressBar` has no `ShowPaused` or `ShowError`.
- **No motion on a value change or on leaving indeterminate.** The phone slides the indicator to its new length and
  fades the dots' row; neither is recorded, and ours changes at once.
- **One raw pixel taller, and a stretched bar one narrower**, from Avalonia's layout rounding (ADR 0003). Each square is
  0.4 raw pixels wider than the phone's, which rounds them to 8 (ours are 8.4, so they sit 12.5 apart as the phone's do).
- **The phone's `TemplateSettings` are read between 346 and 453 wide.** A narrower bar takes the same formulas; whether
  the phone changes them for a narrow bar isn't read.
- **`ShowProgressText` shows nothing**: the phone's bar has no text.
