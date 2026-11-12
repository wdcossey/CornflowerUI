# Slider

Status: done · [#34](https://github.com/wdcossey/CornflowerUI/issues/34) · device run passed 2026-11-12 (Pixel 8, Release, over wireless adb, `run.ps1 -Page slider` on `main` at `764c435`: cold 830 ms, the page opened in 397 ms, every specimen drawn, scrolled into view by hand) · real-window run passed 2026-11-12 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `49e2311`: 56 of 56, the slider page opened in 23 ms with 188 elements) · owed: captures of a vertical Slider and of ticks, and a probe of a tap and a drag

The Windows Phone 8.1 Slider: Avalonia's `Slider`, themed by `Wp81Theme` with the phone's default style and its
horizontal and vertical templates, in `src/Cornflower.Themes.Wp81/Controls/Slider.axaml`. WinRT's `Header` is
`cf:ControlHeader.Header`, attached. The plan, with every value and its source, is [PLAN.md](PLAN.md); the phone's
captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `Slider` with no style is the phone's. The header is attached.

```xml
<!-- 8.1 -->
<Slider Header="volume" Value="30" />
<!-- Avalonia, with Wp81Theme in Application.Styles; xmlns:cf="using:Cornflower.Controls" -->
<Slider cf:ControlHeader.Header="volume" Value="30" />
```

It stretches across its slot. Below its header it is 57.5 tall (the phone's 57.0833, ADR 0003), a 9-tall track 17.5
down, and all of it takes the finger. Stretched taller, as in the reference app, the rest takes the finger too.

## Behaviours

- **At rest**: a 9-tall track in `SliderTrackBackgroundThemeBrush`, the value's part from the left in the accent (the
  Slider's `Foreground`), and a 9.5 × 19 thumb in the foreground at the value, 13 below the header.
- **The header**: `cf:ControlHeader.Header`, shown above in `PhoneMidBrush` at 16, in the phone's 1.33 em line;
  `cf:ControlHeader.HeaderTemplate` shows it with a template. With none, nothing above the track.
- **A tap anywhere below the header** moves the thumb's middle to the finger; **a drag** follows the finger, off the
  Slider and past the ends, until it lifts. From the thumb too. This is Avalonia's `Slider`, as WinRT's, by WinUI's
  source.
- **Disabled**: the value's part `#FF666666` and the thumb `#FF999999` (dark), `#FFBDBDBD` and `#FF808080` (light);
  the track and the header stay.
- **In a scrolling page**, a drag on the Slider moves the thumb and never scrolls the page, as the phone's
  `ManipulationMode="None"` makes it (`PhoneNoManipulation`, internal to the theme).
- **No pressed, pointer-over or focus look**, as on the phone.
- **Vertical** (`Orientation="Vertical"`): the phone's vertical template, 36 wide, the track 13.5 in, the value's part
  from the bottom, a 9 × 9 thumb.
- **Reversed** (`IsDirectionReversed`): the value's part from the other end.
- **Ticks** (`TickPlacement` `TopLeft`, `BottomRight` or `Outside`, with `TickFrequency` or `Ticks`): the phone's
  5-tall bars, above and below (left and right), in `SliderTickmarkOutsideBackgroundThemeBrush`, each tick where the
  thumb's middle stops for its value.
- **Keys**: Avalonia's: the arrows by `SmallChange`, Page Up and Down by `LargeChange`, Home and End.
- **Dark and light**: every brush follows the theme variant; the value's part follows the app's accent
  (`SystemColorControlAccentColor`), or the Slider's own `Foreground`; the track its `Background`.

## Resource keys

The phone's own, all in `Tokens/`, each by `DynamicResource`: `SliderTrackBackgroundThemeBrush`,
`SliderTrackBorderThemeBrush`, `SliderTrackDecreaseBackgroundThemeBrush`, `SliderBorderThemeBrush`,
`SliderBorderThemeThickness`, `SliderThumbBackgroundThemeBrush`, `SliderThumbBorderThemeBrush`,
`SliderTrackThemeHeight`, `SliderOutsideTickBarThemeHeight`, `SliderTickmarkOutsideBackgroundThemeBrush`, the
`Disabled` keys (`SliderDisabledBorderThemeBrush`, `SliderTrackDecreaseDisabledBackgroundThemeBrush`,
`SliderTrackDisabledBackgroundThemeBrush`, `SliderTrackDisabledBorderThemeBrush`,
`SliderThumbDisabledBackgroundThemeBrush`, `SliderThumbDisabledBorderThemeBrush`,
`SliderTickMarkOutsideDisabledForegroundThemeBrush`), `ContentControlThemeFontFamily`, `ControlContentThemeFontSize`,
and for the header `PhoneFontFamilyNormal`, `TextStyleMediumFontSize` and `PhoneMidBrush`. No new key. The parts'
themes are keyed `Wp81SliderHorizontalDecreaseTheme`, `Wp81SliderVerticalDecreaseTheme`, `Wp81SliderIncreaseTheme`,
`Wp81SliderHorizontalThumbTheme` and `Wp81SliderVerticalThumbTheme`.

## The template's parts

`Root` › `HeaderContentPresenter` and `SliderContainer` › `HorizontalTemplate` (or `VerticalTemplate`) ›
`HorizontalTrackRect`, `TopTickBar`, `BottomTickBar`, `PART_Track` (`PART_DecreaseButton` drawing
`HorizontalDecreaseRect`, `PART_IncreaseButton`, `HorizontalThumb` drawing `ThumbBorder`) and `HorizontalBorder`; the
vertical names likewise. A style that reaches into the template uses these names.

## The ladder

Rung 1 (theme) for the setters, the states, both templates, the hit area across the container and the cleared focus
adorner; rung 2 for `Header` and `HeaderTemplate` (`ControlHeader`, attached) and the header's line
(`PhoneHeaderLine`, internal to the theme) and the phone's `ManipulationMode` (`PhoneNoManipulation`, internal to
the theme); rung 6 (recorded) for a drag from the thumb, `StepFrequency` and the
ticks' look. No type is substituted.

## Extensions

`Cornflower.Controls.ControlHeader`: `Header` and `HeaderTemplate`, attached, WinRT's two properties. The Slider's
theme shows them.

## Known gaps

- **Rounding** (ADR 0003): stretched in the reference app's slot, the track ends one raw pixel short of the phone's;
  unstretched, the Slider is 57.5 below its header, not 57.0833. Nothing else drawn moves.
- **A drag from the thumb** puts the thumb's middle under the finger on the first move, up to 4.79 (4.5 vertical)
  from where the phone's would be; then it follows the same.
- **`StepFrequency`**, `SnapsTo` and `IntermediateValue` are missing: a tap gives any value, the phone's the nearest
  whole number. `IsSnapToTickEnabled="True" TickFrequency="1"` gives the phone's steps.
- **`Inline` ticks** are missing (no Avalonia member); with no `TickFrequency` the phone draws none, so the default
  looks the same. **The ticks** are Avalonia's `TickBar`'s, each where the thumb stops,
  without the template's −4.5 margins: unmeasured.
- **The text is Selawik**, its baseline 0.025 em above the phone's (ADR 0003).
- **Unmeasured**: the vertical Slider, ticks, no header, and all input are from the template and WinUI's source.
- **Owed**: those captures, and a probe of a tap and a drag.

## The checklist

In [PLAN.md](PLAN.md#checklist): 58 rows done or kept as Avalonia's, 13 left out with their reasons.
