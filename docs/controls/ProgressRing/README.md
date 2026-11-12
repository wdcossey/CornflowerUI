# ProgressRing

Status: done · [#28](https://github.com/wdcossey/CornflowerUI/issues/28) · owed: the device run on the Pixel 8

The Windows Phone 8.1 ProgressRing: Cornflower's own control, `Cornflower.Controls.ProgressRing`, as Avalonia has none,
and the phone's default style and template as `Wp81Theme`'s `ControlTheme`, in
`src/Cornflower.Themes.Wp81/Controls/ProgressRing.axaml`. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone:

```xml
<!-- 8.1 -->
<ProgressRing IsActive="{Binding IsBusy}" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<cf:ProgressRing IsActive="{Binding IsBusy}" />   <!-- xmlns:cf="using:Cornflower.Controls" -->
```

It stretches to its slot and draws its ring, 57.5 across, in the middle. It is at least 57.5 square. Over 60 on its
smaller side it shows six dots, else five; the ring itself stays 57.5 across, as the phone's does. It takes no input
and no focus.

## Behaviours

- **Active**: the ring's `Background` (the page's colour) covers it at 0.8, and the dots run: each appears 0.167 s
  after the one before, turns about the ring's centre through the template's seven keys and curves over 3.217 s, and
  hides 3.22 s after it appeared. The storyboard repeats every 4.305 s, from its start whenever the ring goes active or
  comes back into a window.
- **Inactive**: nothing is drawn.
- **Large** (the smaller laid-out side over 60) or **small**: the sixth dot shows only when large.
- **`TemplateSettings`**: `MaxSideLength` the smaller side, `EllipseDiameter` a tenth of it, `EllipseOffset` four
  tenths of it at the top, as the phone works them out. The 8.1 template binds only `MaxSideLength`.

The storyboard runs in the theme, frame by frame on `CornflowerClock`, so its tests are exact to the millisecond. The
control has no animation of its own: another theme can draw it any way.

## The checklist

Every row ticked; [PLAN.md](PLAN.md#the-checklist) has the table, with what each became. In short: `IsActive` and
`TemplateSettings`, the API; all 12 setters; all 15 parts, as `PART_` names (the phone's named transforms are the
canvases they turn, as Avalonia's transforms have no names); all 4 states, as `:active`, `:inactive`, `:large` and
`:small`; all 7 resource keys, already in `Tokens/`, and `ProgressRingEllipseStyle`, the template's own, as the dots'
style. Added from WinRT's `Control`: `HorizontalContentAlignment` and `VerticalContentAlignment`.

## Resource keys

`PhoneBackgroundBrush`, `ProgressBarIndeterminateForegroundThemeBrush`, `PhoneFontFamilyNormal`, `TextStyleLargeFontSize`,
`ProgressRingThemeSize`, `ProgressRingElipseThemeSize`, `ProgressRingElipseThemeMargin` (the phone's spelling) and
`ProgressRingActiveBackgroundOpacity`, by `DynamicResource`.

## The ladder

Rung 5, a new control: Avalonia has no ProgressRing. The control is behaviour only; the theme is rung 1.

## Known gaps

- **A stretched ring can be one raw pixel narrower** than the phone's, and its `Ring` one raw pixel higher (0.42 DIP),
  from Avalonia's layout rounding ([COMPARISON.md](COMPARISON.md#measurements)).
- **The size threshold is the reference profile's**, 60 view pixels at scale 2.4; another scale isn't read yet.
- **The dots run on our clock, not the emulator's.** The emulator's runs about 4% fast (B6), so a recording of it
  shows a 4.13 s cycle; ours is the keyframes' 4.305 s.
