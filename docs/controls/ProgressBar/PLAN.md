# ProgressBar: plan

[#35](https://github.com/wdcossey/CornflowerUI/issues/35). The 8.1 ProgressBar: Avalonia's own `ProgressBar`, themed (rung
1), with the phone's default style and template as the Wp81 theme's `ControlTheme`, and its `Indeterminate` storyboard
run frame by frame, as ProgressRing's `Active` is.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `ProgressBar`,
`R:\cornflower-reference\flash-update1\xbf-json\progressbar.json`, and the values of its keys from `themeresources.json`
beside it, which equal the SDK's `generic.xaml` and `themeresources.xaml` ([CLAUDE.md](../../../CLAUDE.md), "Reference
material"). What the phone's control works out in its own code is read from the reference app on the reference profile:

- the captures, runs `20261108-1443-dev11` and `20261108-1732-dev11`, the second committed in
  [`ref/wp81/ProgressBar/`](../../../ref/wp81/ProgressBar/README.md), with their tree dumps;
- `TemplateSettings` read through the API on all seven profiles (`-Native`, runs `20261111-1749-dev11` to
  `20261111-1842-dev13`, #72; tools/refapp/README.md, "`TemplateSettings`").

The numbers the tests use are written out, with their sources, in `tests/Cornflower.Tests/Controls/ProgressBarReadings.cs`.

## The phone's template

`progressbar.json`, `template.tree`:

```
Grid                             (Grid.Resources: ProgressBarRectangleStyle, a Rectangle's Width and Height,
                                  {ThemeResource ProgressBarIndeterminateRectagleThemeSize}, 3.5)
  Grid "EllipseGrid"             Opacity 0; RenderTransform TranslateTransform; nine Auto columns
    Border "B1"  column 8        RenderTransformOrigin .5,.5; RenderTransform TranslateTransform
      Rectangle "E1"             Fill {ThemeResource ProgressBarIndeterminateForegroundThemeBrush};
                                 Style ProgressBarRectangleStyle; RenderTransform TranslateTransform
    Rectangle    column 7        Width {Binding TemplateSettings.EllipseOffset}
    Border "B2"  column 6 … B5 column 0, the same, with a Rectangle between each
  Rectangle "HighContrastBtmAccentBar"   VerticalAlignment Bottom; Height 0.8; Opacity 0;
                                         Fill {ThemeResource ProgressBarHighContrastAccentBarThemeBrush}
  Rectangle "HighContrastRitAccentBar"   HorizontalAlignment Right; Width 0.8; the same
  Border "DeterminateRoot"       Background, BorderBrush, BorderThickness, MinHeight: TemplateBinding
    Rectangle "ProgressBarIndicator"     Margin {TemplateBinding Padding}; Fill {TemplateBinding Foreground};
                                         HorizontalAlignment Left
```

The dumps, at rest (`value-40`, `base`): the bar 412.0833 × 3.3333 (3.5 rounded to 8 raw pixels); the dots, E5 to E1,
at 0, 12.5, 25, 37.5 and 50, each 3.3333 square drawn (3.5 rounded), the gaps 9 (9.1667 drawn); the indicator's `Width`
164.8333, 40% of the bar, 165 drawn (396 raw pixels); the high-contrast bars 0.8333 at the bottom and the right. And
**the ProgressBar's own `Clip`, its bounds**: the phone's control clips itself, so the dots are drawn only on the bar.

**The dots don't take the bar's `Foreground`**: the template fills them with the theme resource directly.

## The Indeterminate storyboard

`progressbar.json`, `CommonStates`, `Indeterminate`: `RepeatBehavior="Forever"`, no `Duration` of its own, so as long as
its longest child. Every track starts at 0 (no begin times); each targets a `TranslateTransform.X` or an `Opacity`:

- **The grid** (`EllipseGrid`): a `DoubleAnimation` from `TemplateSettings.ContainerAnimationStartPosition` to
  `ContainerAnimationEndPosition` over **3.917 s**, a straight line. The storyboard's length.
- **Each B** (`B1` to `B5`), `EasingDoubleKeyFrame`s with no easing, so straight lines: −40 at 0, 0, 0, 80 at

  | B | 0 | leaves 0 | 80 |
  |---|---|---|---|
  | B1 | 0.5 | 2 | 3 |
  | B2 | 0.667 | 2.167 | 3.167 |
  | B3 | 0.833 | 2.333 | 3.333 |
  | B4 | 1 | 2.5 | 3.5 |
  | B5 | 1.167 | 2.667 | 3.667 |

- **Each E** (`E1` to `E5`), each 0.167 s after the one before (begins 0, 0.167, 0.333, 0.5, 0.667): 0 at 0 and at its
  begin; `TemplateSettings.EllipseAnimationWellPosition` a second after its begin and two seconds after; and
  `EllipseAnimationEndPosition` three seconds after. The keys into the well and the end are `SplineDoubleKeyFrame`s with
  the `KeySpline` **0.4,0,0.6,1**. The others are `EasingDoubleKeyFrame`s with an `ExponentialEase` (exponent 16, out
  into the begin, in into the second well), but each joins two equal values (0 to 0, the well to the well), so they move
  nothing.
- **Each E's opacity**: 0 until its begin, 1 from it, 0 from three seconds after. Each step is two keys at one time.
- **The grid's opacity to 1, and `DeterminateRoot`'s to 0**, both `Duration="0"`: at once. A `FadeOutThemeAnimation` on
  `DeterminateRoot` sits beside the second; the dumps at 9/64 s already read 0, so the fade shows nothing.

## The phone's `TemplateSettings`

The template binds five of them. The reference app read them through the API on all seven profiles (native.json),
the same at rest and in every forced state:

| Device | Width | `ContainerAnimationStartPosition` | `ContainerAnimationEndPosition` | `EllipseAnimationWellPosition` | `EllipseAnimationEndPosition` | `EllipseOffset` |
|---|---|---|---|---|---|---|
| 7, 8 | 361.6667 | −63 | 94.4054 | 120.5556 | 241.1111 | 9 |
| 9 | 346 | −63 | 87.5869 | 115.3333 | 230.6667 | 9 |
| 10 | 362.2222 | −63 | 94.6472 | 120.7407 | 241.4815 | 9 |
| **11** | 412.0833 | −63 | 116.3478 | 137.3611 | 274.7222 | 9 |
| 12 | 452.7273 | −63 | 134.0370 | 150.9091 | 301.8182 | 9 |
| 13 | 411.6667 | −63 | 116.1665 | 137.2222 | 274.4444 | 9 |

So, for every width read: the start is −63; the end less the start is 0.43522 of the width, which is **the width over 9
for each of the grid's 3.917 s** (to the readings' four places on all seven); the well is a third of the width and the
end two thirds. `EllipseDiameter` is 6 and unused by the template. `IndicatorLengthDelta` is minus the indicator's
length (−164.8333 at 40%), and binds only the `Updating` → `Determinate` transition (below).

**Avalonia's `ProgressBar.TemplateSettings` can't carry them.** It has `ContainerAnimationStartPosition` and
`ContainerAnimationEndPosition`, but works them out its own way (ModernWPF's: −1.8 and 3 times 40% of the width), only
while indeterminate, and has no well, end or offset. A theme can't add properties to Avalonia's class, and a new class
in `Cornflower.Controls` for this would be a shared file. So `PhoneProgressBar` works the phone's values out from the
bar's laid-out width with the formulas above, every frame, and the gaps' width is the literal 9.

## The behaviours

| Behaviour | Evidence | Test (`ProgressBarTests`) |
|---|---|---|
| The style's setters, and the clip | `progressbar.json`; the dumps (`clip`) | `The_Style_Sets_What_The_Phone_Read` (dark, light) |
| Across its slot, centred down it, 3.5 high | the dump: 46, 83 raw; 989 × 8 | `Its_Bounds_Are_The_Phones` |
| Determinate: the indicator the value's share from the left, in the accent; the track, the accent at 0.16 | the pictures: 396 raw pixels, `#FF3E65FF`; the track `#0A1029` / `#E0E6FF` | `Determinate_It_Draws_The_Track_And_The_Indicator` (dark, light) |
| The indicator follows the value, inside the `Padding` | the template's `Margin` binding; Avalonia's sizing | `The_Indicator_Follows_The_Value_Inside_The_Padding` |
| Disabled draws the same | the template has no `Disabled` state | `Disabled_It_Is_Drawn_The_Same` |
| The dots at rest: 12.5 apart, 3.5 square, the gaps 9; the grid hidden | the dumps | `Each_Dot_Rests_Where_The_Phones_Does` |
| The dots keep the indeterminate brush whatever the `Foreground` | the template's `Fill` | `The_Dots_Keep_Their_Brush_When_The_Foreground_Changes` |
| Indeterminate shows the grid and hides the track, at once | the storyboard's `Duration="0"` animations; the dumps | `Indeterminate_Shows_The_Dots_And_Hides_The_Track` |
| The high-contrast bars, 0.8 at the bottom and the right, while determinate, in their own brush | the template; the dumps | `The_High_Contrast_Bars_Show_While_Determinate`, `The_High_Contrast_Bars_Are_Transparent` (dark, light) |
| `TemplateSettings` for every width read | native.json, seven profiles | `TemplateSettings_Are_The_Phones` |
| The tracks give the template's values, at every millisecond and exactly at the keys | the storyboard | `The_Tracks_Give_The_Templates_Values` |
| A dot shows from its begin until 3 s after, to the tick | the opacity keys | `A_Dot_Shows_From_Its_Begin_Until_3_Seconds_After` |
| **The dumps' moments are moments of this storyboard**: one time gives the grid and every shown dot, each time a 64 Hz frame | six moments, up to four dots at once, within 0.013 | `Each_Captured_Moment_Is_One_Time_In_The_Storyboard` |
| **The pictures' dots are drawn where the phone drew them** | the 1732 pictures, at 31/64 and 29/64 s | `The_Pictures_Dots_Are_Drawn_Where_The_Phone_Drew_Them` (dark, light) |
| The bar clips the dots | the dumps' `clip`; the indeterminate `base` picture, empty | `The_Bar_Clips_The_Dots_To_Its_Bounds` |
| While indeterminate, every frame shows the storyboard at that time, repeating every 3.917 s | the storyboard | `While_Indeterminate_Each_Frame_Shows_The_Storyboard_At_That_Time` |
| The positions follow the width as it changes | `TemplateSettings` are worked out from the width | `A_Narrower_Bar_Moves_Its_Dots_By_Its_Own_Width` |
| Determinate again, the parts get their own values back; indeterminate again starts at 0 | `GoToState` starts a state's storyboard from its start | `Going_Determinate_Stops_It_And_Indeterminate_Starts_It_Again` |
| Out of the window it stops; back in, it starts at 0 | as above, on load | `Leaving_The_Window_Stops_It` |
| Hidden, it asks for no frames | drawing nothing costs nothing | `Hidden_It_Asks_For_No_Frames` |
| Turned on after the template is applied, it still finds the parts | a theme replacing another | `Turned_On_After_The_Template_It_Still_Runs` |
| Each part's own `X` is given back, not 0 | WinRT: a stopped storyboard leaves the part its own value | `A_Parts_Own_Value_Is_Given_Back`, `Turned_Off_And_On_In_One_Pass_It_Keeps_The_Parts_Own_Values` |
| Another theme taking over stops it and gives the values back | the old theme's unset pass | `Another_Theme_Taking_Over_Stops_It` |
| Vertical (Avalonia's, not the phone's): the indicator from the bottom | Avalonia's `Orientation` | `Vertical_The_Indicator_Fills_From_The_Bottom` |
| No text, whatever `ShowProgressText` says | the phone's template has none | `It_Shows_No_Text` |

## How it runs

As ProgressRing's ([its plan](../ProgressRing/PLAN.md#how-it-runs)): the storyboard runs in the theme, frame by frame on
`CornflowerClock`, because Avalonia's `Animation` runs on real time and can't be tested to the millisecond (ADR 0003).

- `PhoneProgressBar` (internal to Wp81, its own file), turned on by the theme's setter, holds the storyboard as
  `PhoneKeyframeTrack`s: the grid's travel and the E slides in widths of the bar, the B slides in view pixels, the
  opacities as discrete keys. One runner per bar runs them while the bar is indeterminate, in a window and templated, from
  the storyboard's start. Each frame it reads the bar's width, sets each `TranslateTransform.X`, and sets each dot's
  opacity at animation priority when it changes. When it stops it gives everything back: each `X` to the transform's own,
  read when the parts were found, and each opacity's value removed after the dispatcher's pass (it can stop inside a
  theme's unset pass). Hidden, it asks for no frames and looks again every 250 ms.
- `PhoneKeyframeTrack` and `KeySplineCurve` are used as they are. The B tracks' keys with no easing are spline keys
  with no curve (a straight line); the two keys at one time of each opacity step are one discrete key.
- The `Duration="0"` animations (the grid shown, the track hidden) and the `Determinate` state's (the high-contrast bars
  shown) are style setters on `:indeterminate`.

## The visual states

`CommonStates`. Avalonia's `ProgressBar` raises `:indeterminate` (with `:horizontal` or `:vertical`); the generator's
rule maps `Indeterminate` to it, and the other four have no rule ([ARCHITECTURE](../../ARCHITECTURE.md#mapping-rules-draft)).

| State | What it changes | Avalonia | Action |
|---|---|---|---|
| `Determinate` | the high-contrast bars' opacity to 1 | not `:indeterminate` | the bars at 1 unless `:indeterminate` |
| `Updating` | nothing | none: Avalonia's bar has no such state | nothing to draw; its transition, below |
| `Indeterminate` | the storyboard above | `:indeterminate` | setters, and `PhoneProgressBar` |
| `Error` | the indicator's opacity 0 | none: no `ShowError` | not reachable (below) |
| `Paused` | the indicator's opacity to 0.5 over 0.25 s | none: no `ShowPaused` | not reachable (below) |

## Avalonia against the phone

| Difference | The phone | Ours | Rung |
|---|---|---|---|
| The control | WinRT's `ProgressBar : RangeBase` | Avalonia's `ProgressBar : RangeBase` | 1, a theme |
| The indicator | `ProgressBarIndicator`, a `Rectangle` the control sizes | Avalonia sizes `PART_Indicator`, which must be a `Border` | 1: `PART_Indicator`, a `Border` filled by its `Background` |
| `TemplateSettings` | the phone's seven, from the width | Avalonia's own, other values | 1: worked out in `PhoneProgressBar` from the width |
| The storyboard | the template's, on the compositor's clock | Avalonia's `Animation` runs on real time | 1, the theme's timeline on `CornflowerClock` |
| The clip | the control's `Clip`, its bounds | none by default | 1, `ClipToBounds` set by the style |
| The bar's height | 3.5, drawn 8 raw pixels (the phone rounds to the nearest) | 9 raw pixels, 3.75 (Avalonia rounds up) | none: Avalonia's layout rounding (gap, ADR 0003) |
| The dots' pitch | 12.5 (8 + 22 raw pixels) | rounded up, 9 + 22, 12.92 | 1: layout rounding off in the grid, so 12.5 exactly; each dot is then 3.5, 8.4 raw pixels (gap: 0.4 raw pixels wider) |
| A stretched bar's width | 989 raw pixels in the reference app's slot | 988: Avalonia rounds the slot's margins, the phone its edges | none (gap) |
| The track's colour | `#0A1029` dark, `#E0E6FF` light | `#091028`, `#E0E7FF` | none: one step of rounding in the blend (match within 1) |
| `ShowPaused`, `ShowError` and their states | WinRT's properties | none in Avalonia | not added (below) |
| `Orientation`, `ShowProgressText`, `ProgressTextFormat`, `Percentage` | none: the phone's bar is horizontal and has no text | Avalonia's | kept: vertical drawn from the bottom; no text |

## Extensions

None.

## Decided on the user's behalf

- **`ShowPaused` and `ShowError` aren't added**, so the `Paused` and `Error` states can't be reached. Avalonia's
  `ProgressBar` has no such properties; adding them means a subclass or attached properties in `Cornflower.Controls`, a
  shared file and a new public surface. To reverse: an attached `ShowPaused`/`ShowError` (or a subclass) raising
  `:paused`/`:error`, and two styles here: `:error` the indicator at 0, `:paused` at 0.5 with a 0.25 s transition.
- **The transitions aren't drawn.** `Updating` → `Determinate` runs a `RepositionThemeAnimation` that slides the
  indicator in from its old length (`IndicatorLengthDelta`); `Paused` → `Determinate` fades the indicator back over 0.25
  s; leaving `Indeterminate` runs a `FadeInThemeAnimation` on the grid. Avalonia's bar has no `Updating` or `Paused`
  state, and no recording times the reposition or the fade. So a new value moves the indicator at once, and leaving
  indeterminate shows the track at once. To reverse: a recording of a value change, then a `Transitions` entry on a
  `TranslateTransform` of the indicator.
- **The gaps between the dots are the literal 9**, the phone's `EllipseOffset` on all seven profiles, and the storyboard's
  values are the formulas above at any width. The phone might step them at a width narrower than any read (346); none is
  read. To reverse: a `-Native` run with narrow bars, then a width rule in `PhoneProgressBar`.
- **Vertical bars fill from the bottom**, as Simple's do. The phone's bar is only horizontal. A vertical indeterminate
  bar runs the same horizontal dots, clipped to its width. To reverse: drop the `:vertical` style.
- **No text.** Avalonia's `ShowProgressText` draws nothing under this theme, as the phone's bar has none. To reverse: a
  `TextBlock` in the template bound to `Percentage` and `ProgressTextFormat`.
- **`Maximum` isn't set by the theme.** The phone's style sets it to 100, which is already Avalonia's default; a setter
  would do nothing a test could see.

## Dependencies

None on other controls. The captures needed are in hand (`ref/wp81/ProgressBar/`). Owed: the device run on the Pixel,
and a recording of a value change if the reposition is wanted.

## The checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist ProgressBar <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ProgressBar`; Avalonia `Avalonia.Controls.ProgressBar`.

### Properties (4)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ShowPaused` | `Boolean` | none | Not added: no Avalonia property; see "Decided on the user's behalf" |
| [-] | `ShowError` | `Boolean` | none | Not added: as `ShowPaused` |
| [x] | `IsIndeterminate` | `Boolean` | `ProgressBar.IsIndeterminate` | Avalonia's stands; raises `:indeterminate`, which the theme styles and `PhoneProgressBar` runs on |
| [x] | `TemplateSettings` (read-only) (not a dependency property) | `ProgressBarTemplateSettings` | `ProgressBar.TemplateSettings` | Avalonia's has other values; the phone's are worked out in `PhoneProgressBar` from the width |

### Events (0), methods (0)

_none_

### Properties, from `RangeBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Value` | `Double` | `RangeBase.Value` | Avalonia's stands; sizes `PART_Indicator` |
| [x] | `SmallChange` | `Double` | `RangeBase.SmallChange` | Avalonia's stands; a bar takes no input |
| [x] | `Minimum` | `Double` | `RangeBase.Minimum` | Avalonia's stands |
| [x] | `Maximum` | `Double` | `RangeBase.Maximum` | Avalonia's stands; 100 by default, as the phone's style sets it |
| [x] | `LargeChange` | `Double` | `RangeBase.LargeChange` | Avalonia's stands |

### Events, from `RangeBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ValueChanged` | `RangeBaseValueChangedEventHandler` | `RangeBase.ValueChanged` | Avalonia's stands |

### Methods, from `RangeBase` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnMinimumChanged(Double oldMinimum, Double newMinimum)` (protected, overridable) | none | Not applicable: Avalonia's `OnPropertyChanged` |
| [-] | `void OnMaximumChanged(Double oldMaximum, Double newMaximum)` (protected, overridable) | none | Not applicable: as above |
| [-] | `void OnValueChanged(Double oldValue, Double newValue)` (protected, overridable) | none | Not applicable: as above |

### Default style: setters (8)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource ProgressBarForegroundThemeBrush}` | `TemplatedControl.Foreground` | `DynamicResource`; the accent, `#FF3E65FF` |
| [x] | `Background` | `{ThemeResource ProgressBarBackgroundThemeBrush}` | `TemplatedControl.Background` | `DynamicResource`; the accent at 0.16 |
| [x] | `BorderBrush` | `{ThemeResource ProgressBarBorderThemeBrush}` | `TemplatedControl.BorderBrush` | `DynamicResource`; transparent |
| [x] | `BorderThickness` | `{ThemeResource ProgressBarBorderThemeThickness}` | `TemplatedControl.BorderThickness` | `DynamicResource`; 0 |
| [x] | `Maximum` | `100` | `RangeBase.Maximum` | No setter: Avalonia's default is 100 already |
| [x] | `MinHeight` | `{ThemeResource ProgressBarThemeMinHeight}` | `Layoutable.MinHeight` | `DynamicResource`; 3.5 |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | literal |
| [x] | `VerticalAlignment` | `Center` | `Layoutable.VerticalAlignment` | literal |

One more, not a setter: `ClipToBounds` true, as the phone's control sets its own `Clip`.

### Template parts (15)

Avalonia's control looks for: `PART_Indicator`. The rule for parts Avalonia doesn't name: `Foo` is `PART_Foo`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `EllipseGrid` | `Grid` | none | `PART_EllipseGrid`, with its `TranslateTransform`; layout rounding off |
| [x] | `B1` | `Border` | none | `PART_B1`, with its `TranslateTransform` |
| [x] | `E1` | `Rectangle` | none | `PART_E1`, with its `TranslateTransform`; the template's rectangle style as the theme's `Rectangle.dot` style |
| [x] | `B2` | `Border` | none | `PART_B2`, as `B1` |
| [x] | `E2` | `Rectangle` | none | `PART_E2`, as `E1` |
| [x] | `B3` | `Border` | none | `PART_B3`, as `B1` |
| [x] | `E3` | `Rectangle` | none | `PART_E3`, as `E1` |
| [x] | `B4` | `Border` | none | `PART_B4`, as `B1` |
| [x] | `E4` | `Rectangle` | none | `PART_E4`, as `E1` |
| [x] | `B5` | `Border` | none | `PART_B5`, as `B1` |
| [x] | `E5` | `Rectangle` | none | `PART_E5`, as `E1` |
| [x] | `HighContrastBtmAccentBar` | `Rectangle` | none | `PART_HighContrastBtmAccentBar` |
| [x] | `HighContrastRitAccentBar` | `Rectangle` | none | `PART_HighContrastRitAccentBar` |
| [x] | `DeterminateRoot` | `Border` | none | `PART_DeterminateRoot` |
| [x] | `ProgressBarIndicator` | `Rectangle` | `PART_Indicator` | `PART_Indicator`, a `Border` (Avalonia's required part and type), its `Background` the `Foreground` |

The four unnamed gap `Rectangle`s are the theme's `Rectangle.gap` style, 9 wide.

### Visual states (5)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Determinate` | `HighContrastBtmAccentBar.Opacity: DoubleAnimation To=1 Duration=0`<br>`HighContrastRitAccentBar.Opacity: DoubleAnimation To=1 Duration=0` | not `:indeterminate` | the bars at 1; `:indeterminate` sets 0 |
| [x] | `CommonStates` | `Updating` | nothing | no rule | nothing to draw |
| [x] | `CommonStates` | `Indeterminate` | the grid, the B and E slides, the E opacities (listed above); `EllipseGrid.Opacity` to 1, `DeterminateRoot.Opacity` to 0, a `FadeOutThemeAnimation` on `DeterminateRoot` | `^:indeterminate` | the opacities as setters; the storyboard in `PhoneProgressBar`; the fade shows nothing (the dumps read 0 at once) |
| [-] | `CommonStates` | `Error` | `ProgressBarIndicator.Opacity = 0` | no rule | Not reachable: no `ShowError` |
| [-] | `CommonStates` | `Paused` | `ProgressBarIndicator.Opacity: DoubleAnimation To=0.5 Duration=0:0:0.25` | no rule | Not reachable: no `ShowPaused` |

### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [-] | `CommonStates` | `Updating` → `Determinate` |  | `RepositionThemeAnimation on ProgressBarIndicator (FromHorizontalOffset=TemplateSettings.IndicatorLengthDelta)` | Not drawn: no `Updating` state, and the motion isn't recorded; a new value moves the indicator at once |
| [-] | `CommonStates` | `Paused` → `Determinate` |  | `ProgressBarIndicator.Opacity: DoubleAnimation To=1 Duration=0:0:0.25` | Not reachable: no `Paused` |
| [-] | `CommonStates` | `Indeterminate` → `*` |  | `FadeInThemeAnimation on EllipseGrid` | Not drawn: not recorded; the grid hides at once |

### Resources (9)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ProgressBarForegroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | used as is |
| [x] | `ProgressBarBackgroundThemeBrush` | `SolidColorBrush Color={ThemeResource SystemColorControlAccentColor} Opacity=0.16` | same | yes | used as is |
| [x] | `ProgressBarBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | used as is |
| [x] | `ProgressBarBorderThemeThickness` | `0` | same | yes | used as is |
| [x] | `ProgressBarThemeMinHeight` | `3.5` | same | yes | `MinHeight` |
| [x] | `ProgressBarIndeterminateRectagleThemeSize` | `3.5` | same | yes | each dot's size (the phone's spelling) |
| [x] | `ProgressBarIndeterminateForegroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | each dot's fill |
| [x] | `ProgressBarRectangleStyle` | not in themeresources.xaml |  | no | Not applicable: a style in the template's own resources, re-expressed as the theme's `Rectangle.dot` style |
| [x] | `ProgressBarHighContrastAccentBarThemeBrush` | `SolidColorBrush Transparent` | same | yes | the high-contrast bars' fill |
