# Architecture

How Cornflower is built, and how it is meant to grow. Most of this is a plan. Each section says
what exists today; the rest is **proposed** and changes as controls prove or disprove it. What
exists is also in `CLAUDE.md`, which is the ground truth.

How facts are marked here:

- **Measured** or **read**: checked on the reference files or the emulator, with the date.
- **Reported**: found by research at a source, not checked by us.
- **Unconfirmed**: neither. Not a basis for work until it is confirmed.
- **Proposed**: the plan.

## Eras

Cornflower recreates more than one design language, one at a time. An era is one named release of
the platform, with its own reference build, templates and theme.

| | Era | Design language | Stack | Reference | Sources in hand |
|---|---|---|---|---|---|
| 1 | **Windows Phone 8.1** (2014) | Metro | WinRT XAML | 8.1 Update 1 emulator ([ADR 0001](decisions/0001-what-faithful-means.md)) | Templates, theme resources and API metadata from the phone SDK; the emulator on the reference VM |
| 1b | Windows Phone 8.0 (2012) | Metro | Silverlight | The 8.0 emulator | The 8.0 SDK's design files, and its emulator, are installed on the reference VM |
| 2 | Windows 10 Mobile (2015–17) | MDL2 | UWP XAML | Windows 10 Mobile emulator | The 14393 SDK's templates; the Mobile emulator 10.0.14393.0 is installed, not yet run |
| 3 | Windows 10 desktop, late (1809–2004) | MDL2 with reveal and acrylic | UWP XAML | A UWP app on a PC or a VM | Templates and API metadata from the Windows 10 SDK |

Windows Phone 8.0's controls are an add-on to era 1, not a separate era
([ADR 0002](decisions/0002-decisions-of-2026-11-08.md#themes-and-scope)). The order of eras is in
[the roadmap](ROADMAP.md#order-of-eras). Whether Windows 10 Mobile ever received reveal or acrylic
is unconfirmed; if it didn't, there is no "Fluent on the phone" era to recreate.

### Why 8.1 first, and not 8.0

Both are Metro and look much the same. 8.1 is the better start:

- It is WinRT XAML, the same family as Windows 10, so mapping rules written for it carry to eras
  2 and 3. Silverlight's templates and theme keys (`PhoneAccentBrush`, `PhoneFontSizeLarge`)
  don't.
- Its emulator runs on a Windows 10 host. The 8.0 SDK is stated for Windows 8 x64 only.
- WinJS 3.0 gives it an openly licensed second opinion, in HTML and CSS. 8.0 has none.

What 8.0 has that 8.1 replaced (reported, from Microsoft's archived documentation):

| 8.0 (Silverlight) | In 8.1 |
|---|---|
| Panorama | Hub |
| LongListSelector | SemanticZoom with ListView |
| ApplicationBar | CommandBar with AppBarButton |
| Toolkit ListPicker, DatePicker, TimePicker | ListPickerFlyout, DatePickerFlyout, TimePickerFlyout |
| Toolkit tilt effect | Built into the controls' pressed states |
| Toolkit page transitions (turnstile, slide, swivel) | Navigation theme transitions |
| HubTile | none |

### What differs between eras

| | Metro (8.x) | MDL2 (Windows 10) |
|---|---|---|
| Text font | Segoe WP | Segoe UI |
| Icon font | Segoe UI Symbol | Segoe MDL2 Assets |
| Typical border | 8.1 (WinRT): 2.5 (`PhoneBorderThickness` in `Windows Phone Kits\8.1\Include\abi\Xaml\Design\themeresources.xaml`, and on device 11, 2026-11-08). Silverlight 8.0, and 8.1's Silverlight design file: 3 (`PhoneBorderThickness` in `Microsoft SDKs\Windows Phone\v8.0\Design\ThemeResources.xaml` and `v8.1\Design\ThemeResources.xaml`) | 2 (`ButtonBorderThemeThickness`, read in 19041) |
| Spacing unit | Silverlight 8.0, and 8.1's Silverlight design file: 12 (`PhoneMargin` 12, `PhoneHorizontalMargin` 12,0, in the same two files). 8.1 (WinRT): no unit is defined; several resources use 9.5 (`PhoneTouchTargetOverhang` 0,9.5, `PhoneButtonContentPadding` 9.5,0,9.5,3.5, in the WinRT `themeresources.xaml` and the 8.1 Update 1 runtime) | 4, on a 44 then 40 px control grid (reported) |
| App bar | 8.1 (WinRT): the page's app bar is drawn by the shell, so screenshots are its reference (#59): compact about 62.9 high, measured from a 1080p screenshot ÷ 2.4 (an API read is gap register B2). `AppBarThemeMinHeight` 54.5 (the WinRT `themeresources.xaml` and the Update 1 runtime) applies only to the XAML `AppBar` control. 8.0: 72 high, lowercase menu (reported; no 8.0 design file sets it) | CommandBar, compact height 40 (`AppBarThemeCompactHeight`, read in 19041) |
| Navigation | Pivot, Hub, back button | Hamburger and SplitView, Pivot |
| Press feedback | Tilt | `PointerDownThemeAnimation` in 38 places in 19041; what it draws on each control is unconfirmed |
| Accent | 20 named colours, the same names in the 8.0 and 8.1 SDK design files; only Cobalt's value differs (read). The phone's registry has the same 20 but for Cobalt, `#FF3E65FF`, which is the default (read, 8.1 Update 1 image) | The system accent; palette size unconfirmed |
| Theme dictionaries | 8.1: `Default` (dark), `Light`, `HighContrastBlack`, `HighContrastWhite` (read) | `Default` (dark), `Light`, `HighContrast` (read in 19041) |

Every "reported" cell is measured from the era's own files before it goes in a plan.

### How switching would work

Proposed. The mechanism is unproven; a Tier 0 spike tests it
([#14](https://github.com/wdcossey/CornflowerUI/issues/14)).

- **One controls package, one theme package per era.** `Cornflower.Controls` holds the new
  controls with behaviour only. `Cornflower.Themes.Wp81` holds the era's tokens and
  `ControlTheme`s, for Avalonia's controls and for ours.
- **The era is the theme class the app installs**: `<Wp81Theme />` in `Application.Styles`,
  where `FluentTheme` would go. Classic.Avalonia works this way.
- **Dark, light and accent are not eras.** They use Avalonia's `ThemeVariant` and a resource.
  Using `ThemeVariant` for the era too would multiply every dictionary by the number of eras.
- **Resource keys keep each era's own WinRT names.** A key that exists in two eras has the same
  name in both, so an app's override survives a switch.
- **Switching at run time** means replacing the theme instance in `Application.Styles`. Whether
  every control re-templates cleanly when that happens is unconfirmed. If it doesn't, switching
  is a start-up choice.

A control that exists in one era and not another (Hub in 8.1, NavigationView in Windows 10)
lives in the controls package once, with a theme in each era that has it.

## Layout

What exists today, and what is planned:

```
src/Cornflower.Controls/            exists          new controls, the listed subclasses and part classes, the behaviours themes attach
  Controls/<Name>/                  planned         one folder per control: class, enums, a functional stub ControlTheme
  Parts/                            planned         part classes placed inside Avalonia's templates
  Tilt.cs, TiltModel.cs             exists          the tilt: an attached behaviour a theme turns on, and its model (#22)
  Behaviours/                       planned         theme transitions and anything else a template attaches; the tilt moves here when there is a second
  Icons/                            exists          LucideIcon and LucideIconKind; the icons, generated by tools/lucide, with Lucide's licence beside them (ISC, and MIT for the icons from Feather)
src/Cornflower.Themes.Wp81/         exists          era 1: tokens and ControlThemes
  Tokens/                           exists          colours, accent, type ramp, sizes, font families, under their WinRT keys; TextStyles.axaml, the text styles' own sizes, under Cornflower's keys (<Style>FontSize, LineHeight, CharacterSpacing). The only place for literal colours
  Glyphs/                           exists          the glyphs the 8.1 templates and shell draw, as our own geometry, keyed Wp81.Glyph.<what it is>
  Controls/<Name>.axaml             exists          one ControlTheme per control, for Avalonia's and for ours: TextBlock's (the 16 text styles) and LucideIcon's so far
  PhoneTextLine.cs                  exists          the 8.1 line rules for a TextBlock (BaselineToBaseline, CharacterSpacing), internal to the theme: a rung-2 behaviour holding an era's numbers
  Fonts/                            exists          Selawik 1.01: Selawik/ (Light, Regular, Semibold, Bold) and SelawikSemilight/
  Wp81Theme.cs                      exists          the class an app installs
src/Cornflower/                     exists, no code the umbrella package
src/Cornflower.Fonts.Noto/          planned         the optional fallback font package
tests/Cornflower.Tests/             exists          Avalonia.Headless with Skia and xunit v3: harness, discovery, spikes, fonts, tokens, glyphs, controls, behaviours, sample, performance, tools, completeness, repository guards
samples/Cornflower.Sample/          exists          the shared sample app: the [SamplePage] catalogue (SampleShell, MainWindow; on a phone too), which times each page and writes its ready line, one page so far (MainView), with the Debug performance line
samples/Cornflower.Sample.Android/  exists          the real-device target, run by tools/android
samples/Cornflower.Sample.Desktop/  exists          a thin desktop head over Cornflower.Sample: its page at 450 × 800 beside a 40-wide rail for Back, resizable, driven by tools/real-window
src/Cornflower.Adaptive/            planned         working name: the extension pack for wider windows, after Tier 2 (ADR 0004)
tools/                              exists          reference capture (refapp), spec extraction and the checklist (winrt-api), the device run (android), breakage, font metrics, token generation (tokens), the icon generator (lucide), the desktop window driver (real-window), the guard's hash list
docs/                               exists          process, roadmap, decisions, performance, this file; controls/<Name>/ for each control's PLAN, COMPARISON, README and frames (TextBlock/ first)
ref/wp81/                           exists          committed reference captures from the emulator, one folder per control (TextBlock/ first, #69)
```

A later era adds `src/Cornflower.Themes.W10M/` and `ref/w10m/`. It doesn't change the controls
package unless it adds a control.

### How this differs from Avonic

| | Avonic | Cornflower |
|---|---|---|
| Who owns the controls | Avonic: every control is its own class | Avalonia, for most. Ours where Avalonia has none, plus a short listed set of subclasses and part classes |
| Behaviour project | All behaviour, no visual opinion | Only the new controls, the listed subclasses and part classes, and the attached behaviours |
| Theme project | Styles Avonic's controls | Styles Avalonia's controls and ours |
| API | Ionic's, mapped mechanically | Avalonia's for built-ins and subclasses; WinRT's for new controls |
| Reference | Runs on demand in Chromium | Captured ahead of time from an emulator |
| Target | Phone viewport, desktop sample | Phone, with an Android sample from Tier 0 |

The rule "no visual opinion in the behaviour project, no literal colours outside `Tokens/`"
carries over unchanged.

## The limit of a theme

A `ControlTheme` decides what a control looks like and which parts it has. It doesn't decide what
the control does. Avalonia's `ToggleSwitch`, `Slider`, `ComboBox` and `DatePicker` have their own
behaviour, and it won't match the phone's in every detail.

So Cornflower is a hybrid, with a ladder (accepted 2026-11-07,
[ADR 0002](decisions/0002-decisions-of-2026-11-08.md#the-shape-of-a-control)). For each
difference found in a control's Compare step, take the first rung that reaches the measured
behaviour, and record the rung in the control's plan.

1. **Theme.** The class already raises the state; the template, a transition or a resource
   expresses it. Many differences are really template differences: a transition, a part's hit
   area.
2. **Attached behaviour** in `Cornflower.Controls`, set by the template, reading only public
   state and events. The tilt, scroll indicators, list check mode, `Header` and
   `ClosedDisplayMode`. One that holds an era's own numbers, and so is a visual opinion, lives in
   that era's theme instead: `PhoneTextLine`, the 8.1 line rules, is internal to `Cornflower.Themes.Wp81`
   (rule 5).
3. **Part class**: our own control placed inside Avalonia's template, where the behaviour lives in
   a part. A `ScrollContentPresenter` subclass with our gesture recogniser for scroll physics; a
   presenter for a full-screen picker. The app-facing type doesn't change. Avonic's
   `ButtonInner` is this.
4. **Subclass** in `Cornflower.Controls`, with the WinRT name, when the control's own input or
   selection path must change. `StyleKeyOverride` stays at its own type, and its theme is
   `BasedOn` the base's. Allowed only if:
   - Avalonia's virtual exists for it;
   - every base member keeps its meaning;
   - the base type stays themed, so an app that forgets gets the look without the extra
     behaviour, not a hole;
   - the control's README lists it as a type substitution.

   Expected: `ComboBox`, for its full-screen mode. Keep the set short and listed.
5. **New control** with the WinRT name, when Avalonia has no type of that shape. Expected: Pivot,
   Hub, ProgressRing, ContentDialog, the picker flyouts and their presenters, SemanticZoom,
   LoopingSelector, and later Windows Phone 8.0's Panorama, LongListSelector and HubTile.
6. **Recorded deviation**, with its measurement and reason.

Behaviour in a subclass or part class is set at style priority, so it survives a replaced theme.
Rung 4 costs an app a type change; rung 3 and below cost it nothing.

What Avalonia's control already does decides most of this. Read from Avalonia 12.1.4 by
reflection, for the controls era 1 themes most:

| Avalonia 12.1.4 | Parts (`!` = required) | Pseudoclasses it raises |
|---|---|---|
| `Button : ContentControl` | `PART_ContentPresenter` | `:pressed` (and `:flyout-open` through `Flyout`) |
| `ToggleSwitch : ToggleButton` | `PART_MovingKnobs!`, `PART_SwitchKnob`, `PART_On/OffContentPresenter` | `:checked`, `:unchecked`, `:dragging` |
| `ComboBox : SelectingItemsControl` | `PART_Popup!` (a `Popup`), `PART_EditableTextBox` | `:dropdownopen`, `:pressed` |
| `ListBox` / `ListBoxItem` | `PART_ScrollViewer` | item: `:pressed`, `:selected` |
| `TextBox` | `PART_TextPresenter!`, `PART_ScrollViewer` | `:empty`, `:touch-mode` |
| `Slider : RangeBase` | `PART_Track`, increase and decrease buttons | `:pressed`, `:horizontal`, `:vertical` |
| `ProgressBar` | `PART_Indicator!` | `:indeterminate` |
| `ScrollViewer` | two `ScrollBar` parts | none; physics are in `ScrollGestureRecognizer` |
| `CommandBar` (12.0) | `PART_OverflowButton`, `PART_OverflowPopup`, `PART_OverflowPresenter` | none |
| `DatePicker`, `TimePicker` | `PART_FlyoutButton`, `PART_Popup`, a presenter with looping `DateTimePickerPanel`s, `PART_AcceptButton!`, `PART_DismissButton` | `:hasnodate`, `:hasnotime` |
| `NavigationPage`, `ContentPage` | nav bar, back button, page presenters, top and bottom command bar presenters | `:nav-bar-inset`, `:nav-bar-compact` |

Every control of interest is unsealed and exposes `OnApplyTemplate`, `OnPropertyChanged` and its
input handlers as protected virtuals. Two consequences follow from Avalonia's docs and are tested
by a Tier 0 spike ([#16](https://github.com/wdcossey/CornflowerUI/issues/16)): a subclass gets no
theme unless it overrides `StyleKeyOverride` to its base or ships a theme keyed to itself; and
that theme may be `BasedOn` the base's.

The real class-level differences between the phone and Avalonia are few: ComboBox's full-screen
mode, list multi-select and reorder, scroll physics, and the new controls. The planned rung of
every control is in [the roadmap](ROADMAP.md#controls).

## Applying the theme

What exists: `Wp81Theme` is a `Styles` class, as `FluentTheme` is. It merges every file under
`Tokens/`, `Glyphs/` and `Controls/` that auto-discovery found at build time, in ordinal order.
`Tokens/` holds six files, loaded as `Accent`, `Colors`, `Fonts`, `Sizes`, `TextStyles`,
`Typography`; none uses a `StaticResource` into another. `Controls/` holds `LucideIcon`'s theme
and `TextBlock`'s 16 keyed text styles.

```xml
<Application.Styles>
    <wp81:Wp81Theme />
</Application.Styles>
```

The theme sets no `ThemeVariant`: it follows the app's. The samples request Dark, the phone's
default ([ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md#tokens-45)).

Still proposed: an `Accent` property on the theme, as a shortcut for the resource below.

- Each control's theme is a `ControlTheme` keyed by type. A `ControlTheme` replaces the previous
  one whole.
- **An unthemed control is a hole**: it gets no template at all, or Fluent's if that is also
  installed. During development, Avalonia's Simple theme sits underneath `Wp81Theme` in the
  sample and the tests, and a test lists every templated Avalonia control with no Wp81 theme
  ([#13](https://github.com/wdcossey/CornflowerUI/issues/13)). A templated control is a public,
  concrete `TemplatedControl` that `SimpleTheme` themes by its type, in Dark or Light, or one of
  Cornflower's own, by its style key. The list is `tests/Cornflower.Tests/Completeness/Holes.txt`,
  79 names at Avalonia 12.1.4, and their count is the measure of completeness. `Wp81Theme` itself includes no fallback; a shipped app chooses its own. What a
  control with no `ControlTheme` does is a Tier 0 spike
  ([#15](https://github.com/wdcossey/CornflowerUI/issues/15)).

## Dark, light and accent

What exists ([#19](https://github.com/wdcossey/CornflowerUI/issues/19)):

- Dark and light are Avalonia's `ThemeVariant`. `Tokens/Colors.axaml` holds the 8.1 SDK's
  `Default` dictionary as `Dark` and its `Light` dictionary as `Light`: 319 keys each. Each
  brush takes its colour from its own variant's dictionary through a `StaticResource`. A
  `ThemeVariantScope` gets its own variant's brushes.
- The theme follows the app's variant; it doesn't force Dark. The phone's default is dark, so the
  samples and `Scene` request it.
- The accent is the WinRT key `SystemColorControlAccentColor`, in `Tokens/Accent.axaml`,
  defaulting to `#FF3E65FF`, the phone's default accent, Cobalt: `DefaultAccent` 5 in the 8.1 Update 1
  image's registry, named by `UIXMobileRes.dll.mui` string 10022. The emulator's default
  `PhoneAccentBrush` reads the same (2026-11-08, in both themes, on three profiles). Every accent
  brush takes it through a `DynamicResource`, so an app that defines the key in
  `Application.Resources` moves them all while it runs.
- The 20 named accents are resources an app can pick from, keyed `CornflowerAccent<Name>Color`
  (WinRT has no keys for them). Their values are the phone's registry palette (`Control Panel\Theme`,
  `Accents\0` to `19`), which equals the 8.1 SDK design files' but for Cobalt: the phone's is `#FF3E65FF`,
  the 8.1 SDK file's `#FF0B6CF8` and the 8.0 one's `#FF0050EF` (ADR 0002, corrected 2026-11-11).

Proposed:

- An `Accent` property on the theme that sets `SystemColorControlAccentColor`.
- Following the device's accent through `PlatformSettings.GetColorValues().AccentColor1`.
- High contrast is later. Avalonia has no built-in high-contrast variant; a custom `ThemeVariant`
  is possible. The 8.1 theme resources have `HighContrastBlack` and `HighContrastWhite`.

## Fonts and icons

On the measurements in [ADR 0001](decisions/0001-what-faithful-means.md). Text
([#18](https://github.com/wdcossey/CornflowerUI/issues/18)), the template glyphs
([#20](https://github.com/wdcossey/CornflowerUI/issues/20)) and Lucide
([#21](https://github.com/wdcossey/CornflowerUI/issues/21)) exist; the Noto package is proposed.

- **Text** is Selawik 1.01 (OFL-1.1), bundled in the theme as `avares://` fonts, so the theme
  package's licence is `Apache-2.0 AND OFL-1.1`. `Tokens/Fonts.axaml` holds the six 8.1 font
  family keys (`PhoneFontFamilyNormal`, `PhoneFontFamilyLight`, `PhoneFontFamilySemiLight`,
  `PhoneFontFamilySemiBold`, `ContentControlThemeFontFamily`, `PivotHeaderItemFontFamily`). 8.1
  has no Bold key. On Windows an app may point a key at Segoe UI, which is installed there; the
  library never ships it.
- **Semilight** is addressed by its own family name, from a folder of its own,
  `Fonts/SelawikSemilight/`, unpatched. In one collection with Light, Avalonia's
  `Typeface.Normalize` sends "Selawik Semilight" to Light's 300 slot, since Semilight's file
  reports `usWeightClass` 300. So `PhoneFontFamilyNormal` with `FontWeight="SemiLight"` draws
  Light; no 8.1 template does that.
- **Line height**: Avalonia uses the `hhea` line unless `OS/2` sets `USE_TYPO_METRICS`, which no
  Selawik file does. Selawik's line is 1.200 em; the phone's Segoe WP line is 1.330 em, measured
  in five dumps. Each text style and template where WinRT sets no `LineHeight` sets
  `LineHeight` = 1.33 × `FontSize`, which gives the phone's line. The baseline then sits 0.025 em
  above the phone's, a recorded deviation
  ([ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md#fonts-46)). It is set
  per style, never as a global `TextBlock` rule.
- **Other scripts**: Noto Sans, in the optional `Cornflower.Fonts.Noto` package. Selawik covers
  348 code points: Latin-1 and Latin Extended-A.
- **Glyphs in templates** are our own paths, each a named geometry resource in the theme's
  `Glyphs/`, keyed `Wp81.Glyph.<what it is>` (WinRT has no keys for them: the data sits inline in
  its templates), and measured from captures by fitting each edge to the anti-aliased coverage. The
  8.1 templates use no symbol-font glyph inside a template, and draw only two glyphs:
  `CheckMark` (the CheckBox's tick, also the list items' ticks) and `SelectedEarmark` (a selected
  item's corner). A third, `AppBarEllipsis`, is drawn by the shell, on the page's and the pickers'
  bars.
  There is no TextBox clear button and no ComboBox or picker arrow on the phone. The CheckBox's
  indeterminate mark is a `Rectangle` whose fill is transparent, so at rest it doesn't show in
  either theme. Each glyph has a test against named capture pixels
  ([#20](https://github.com/wdcossey/CornflowerUI/issues/20)). Circles are drawn as arcs in a
  `StreamGeometry`: an `EllipseGeometry` in a `GeometryGroup` drew about 0.45 raw px off centre
  in headless Skia.
- **Icons an app uses** are Lucide (ISC, with MIT for the icons it took from Feather), generated
  by `tools/lucide` from a pinned release into `Cornflower.Controls/Icons/`: one stroked path per
  icon in a 24-unit box, embedded with Brotli, and `LucideIconKind` with values kept across
  bumps. `LucideIcon` draws one; its Wp81 theme is 20 × 20, in the inherited `Foreground`, with
  stroke width, cap and join as theme resources (`LucideIconStrokeThickness`,
  `LucideIconStrokeLineCap`, `LucideIconStrokeJoin`). There is no `SymbolIcon`:
  `tools/lucide/symbols.tsv` maps WinRT's `Symbol` names to Lucide names, 161 of 193. Lucide's
  shapes are its own; that is a recorded deviation
  ([#21](https://github.com/wdcossey/CornflowerUI/issues/21)).

## Larger screens

The core theme is the phone at any window size. Layouts for wider windows are Cornflower's own
design, in a separate extension pack over the core theme, planned after Tier 2
([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)). The core keeps two hooks for it:

1. What a pack may change, starting with the type ramp, is a named resource reached by
   `DynamicResource`, never a literal in a template. A `StaticResource` would be resolved from the
   theme's own tokens and never see the pack's.
2. No fixed phone width, in any unit (450 or 480 DIP, 1080 raw pixels), is written into a template
   or a style. A control's plan names the sizes a pack may override, and its tests include one in a
   window wider than the reference profile.

## Platform features the look depends on

Checked against the Avalonia 12.1.4 reference assemblies for whether each type exists. The
proposals are unproven.

| The phone has | Avalonia 12.1.4 has | Proposal |
|---|---|---|
| Tilt on press | A perspective `MatrixTransform` | `Tilt.IsEnabled`, an attached behaviour that tilts toward the touch point, frame by frame on `CornflowerClock`, at animation priority, on the template part marked `Tilt.IsTarget`, inside an untilted `Transparent` root so that a press at the very edge still hits the control. Not `Rotate3DTransform`: it has no depth offset and puts its eye over its origin. Angle, depth and timing measured from the emulator ([#22](https://github.com/wdcossey/CornflowerUI/issues/22), [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-tilt)) |
| Navigation transitions (turnstile, slide, continuum) | `IPageTransition`, `PageSlide`, `TransitioningContentControl`, `NavigationPage` | Our own `IPageTransition`s, one per transition, timed from recordings: the turnstile, the slide and the continuum in [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-motion) |
| Content and entrance transitions | `ImplicitAnimationCollection` (composition) | An infrastructure item before the controls that use them |
| Pivot | `TabControl`, `Carousel` | A new control. Its header strip scrolls with the content, which neither does |
| Hub | none | A new control |
| ContentDialog | none | A new control |
| ProgressRing | none | A new control. ProgressBar's indeterminate dots are a template |
| App bar | `CommandBar`, `CommandBarButton` (new in 12.0) | Theme Avalonia's, and measure what it lacks: the minimised state, the ellipsis menu |
| Picker flyouts with looping selectors | `DatePicker`, `TimePicker`, `DatePickerPresenter` | Theme Avalonia's, which already loop. Full-screen presentation to be measured; if Avalonia's presenter can't be placed full screen, the presenter becomes a part class, not the picker a subclass |
| ComboBox's full-screen list | `ComboBox` opens its `PART_Popup` from the class | Theme for the inline form; a `ComboBox` subclass (rung 4) opens `ListPickerFlyout` for the full form. The flip is class behaviour, read from the emulator: 5 items open inline and 6 full screen, by count, not by screen ([#25](https://github.com/wdcossey/CornflowerUI/issues/25)) |
| Hardware back button | `BackRequested` | The app's job; `NavigationPage` may already handle it. Documented |
| Status bar and safe areas | `InsetsManager`, `SafeAreaPadding`, `DisplayEdgeToEdge` | The app's job, with an example. Avonic reached the same decision. Avalonia 12's `TopLevel.AutoSafeAreaPadding` is on by default, and the Android sample relies on it |
| Orientation states | none | A pseudoclass the control sets from the top level's size |
| Optical margin alignment on large titles | none | Measure the offset and apply it as a margin. A likely recorded deviation |
| Character spacing | `LetterSpacing` | Convert: WinRT's unit is thousandths of an em, Avalonia's is DIPs, so `LetterSpacing` = n × `FontSize` / 1000 ([spike 5](spikes/README.md#5-the-unit-of-textblockletterspacing)) |
| Focus rectangles | `FocusAdorner` | Low priority on a touch phone. Set it deliberately, even if to nothing |
| `VisualStateManager` | none | Pseudoclasses and nested selectors. See [the mapping rules](#mapping-rules-draft) |

## Performance and metrics

Avonic measured that every global style rule is checked against every element, at about 30 ns per
alternative (Avonic's
[`docs/infra/performance/README.md`](https://github.com/wdcossey/Avonic/blob/v2/docs/infra/performance/README.md)).
Cornflower should need far fewer global rules, because a theme for a control's own template
belongs in its `ControlTheme`, which doesn't have that cost. The budget test carries over anyway,
set low from the start, and the measurement is repeated on the Android device.

Avonic's metrics collection carried over ([#12](https://github.com/wdcossey/CornflowerUI/issues/12)).
What exists, the limits, the measurements, and what changed in the copy ("From Avonic") are in
[`docs/performance`](performance/README.md). The table below is the plan it was copied by, kept
for the Avonic paths; the last of it, `drive.ps1`, came with the desktop sample
([#9](https://github.com/wdcossey/CornflowerUI/issues/9)):

| Avonic file | What it does | For Cornflower |
|---|---|---|
| `tests/Avonic.Components.Tests/Performance/StyleBudgetTests.cs` | The budget CI enforces: total alternatives in all global rules, alternatives reaching a `TextBlock`, alternatives reaching any other common element, and zero rules that walk a common element's ancestors. Limits are raised on purpose in a PR; a checkpoint forces a real-phone measurement before going past it | Copy, with limits far lower than Avonic's 200: a theme for a stock control lives in its `ControlTheme`, which isn't counted. The ancestor-walk check whitelists types with Avonic's prefix; use ours, and treat Avalonia's types as common, because we theme `Button` and `TextBlock` directly |
| `tests/Avonic.Components.Tests/Performance/StyleTrace.cs` | Which global rules one element is checked against, from Avalonia's activity source `Avalonia.AttachingStyle`. Counts are exact, so CI can rely on them | Copy, with the prefix change above |
| `samples/Avonic.Sample/Diagnostics/StyleCensus.cs` | Counts rules and comma-separated alternatives in the installed `Styles`, ignoring commas inside `:is()`, `:not()` and property values | Copy unchanged |
| `tests/Avonic.Components.Tests/Performance/StyleCostBenchmark.cs` | Attach time with and without the theme: 5,000 elements at depth 20, 25 rounds, median, Release only, gated on an environment variable so CI never times anything | Copy, renaming the variable and the theme class. Run at each tier's roll-up on the same machine, and once on the Android device |
| `samples/Avonic.Sample/Diagnostics/PerformanceMonitor.cs` | The line along the bottom of a Debug sample: open time, element count, census, and Avalonia's own measure, arrange and render times from `Avalonia.Diagnostic.Meter`. Nothing is added to the library | Copy unchanged |
| `tests/Avonic.Components.Tests/Performance/PerformanceMonitorTests.cs` | Proves the line's format, that it reports once a page is in, and that Avalonia's meter still has its three instruments | Copy unchanged |
| `tools/real-window/drive.ps1` | Drives the real sample window with real input and saves a frame of every page | Copied with the desktop sample (#68): every page opened with a real click, a frame saved, Back, the page's 450 × 800 from the ready line, Alt+Left and resizing. Avonic's per-control sections and touch come back with controls. The phone equivalent is `tools/android/run.ps1` |
| `tools/breakage/run.js` | Breaks each behaviour in a fresh clone and checks a test goes red | Copy unchanged |
| `tests/Avonic.Components.Tests/Scene.cs`, `TestClock.cs`, `Frames.cs` | A shown headless window closed when the test ends; a clock the test advances by hand; `PixelAt` on a captured frame | **Done** ([#11](https://github.com/wdcossey/CornflowerUI/issues/11)), with changes: `Scene` is the reference profile, 450 × 800 at 2.4, with the themes in the window's `Styles` and Dark by default; `TestClock` replaces one internal source behind `CornflowerClock`, refuses to nest or go back, and lands a frame 16 ms after it is asked for; `PixelAt` checks bounds and reads only RGBA and BGRA |

The rules behind them, from Avonic's measurements: budget alternatives, not rules or time,
because alternatives track cost and counts are exact; no metrics code in the library, because
Avalonia has it built in; the sample's line doesn't trace, because tracing costs more than the
checks it would measure. Nothing has been measured on a phone in either project, so Cornflower's
Android sample is the first chance to put real numbers against the checkpoint.

## Testing

What exists: `tests/Cornflower.Tests` runs Avalonia.Headless with Skia and xunit v3, on the
harness from [#11](https://github.com/wdcossey/CornflowerUI/issues/11):

- **`Scene`**: a shown headless window at the reference profile, a 450 × 800 view at render
  scaling 2.4, so a frame is 1080 × 1920 like the emulator's. `SimpleTheme` then `Wp81Theme` go
  in the window's `Styles` before the content is set. Dark unless the test asks for Light.
  Another profile's size and scaling can be given for a second reading.
- **`Frames.PixelAt`**: one pixel of a captured frame, in the frame's own pixels and byte order.
- **`TestClock`**: replaces `CornflowerClock` for the test's life. Time moves only with `At(ms)`,
  which runs each callback due on the way at its own time. A frame lands 16 ms after it is
  requested.

Also the discovery tests, the spikes, the font and token tests, the glyph tests (each drawn
over its capture), the controls' tests (the text styles, `LucideIcon`), the behaviours' tests (the
tilt), the sample's tests, the style budget and its trace, the generators' tests (the checklist,
Lucide), the holes test, and the repository guards.
The device run, once per batch, puts the sample on the Android phone over ADB and pulls frames
back: [`tools/android/run.ps1`](../tools/android/README.md). Avonic's real-window driver found a
bug 1,486 headless tests had missed.

Decided ([ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md#layout-and-time-in-tests-44)):

- **Transitions are tested at their end value.** Avalonia 12's animation clock is internal, so
  `Transitions` and `Animation` run on real time and `TestClock` can't drive them. A motion that
  must match the phone frame by frame, such as the tilt, is a frame loop on `CornflowerClock`,
  and its test reads it at any millisecond.
- **Layout rounding is a recorded deviation.** Avalonia rounds a measured size up to the raw pixel
  grid; the phone rounds to the nearest. On the reference profile, a size whose raw pixels have a
  fraction under a half comes out one raw pixel (0.42 view px) wider. A control's comparison marks
  each such size as a deliberate difference.

Proposed:

- Tests for Avalonia's own controls test the theme: that a state shows the measured colour and
  size, by pixel and by bounds. They don't re-test Avalonia's behaviour.
- Tests for our controls, subclasses and behaviours assert the phone's behaviour, from the
  measurement recorded in the plan.
- Every discovered resource is built with the theme installed, since a mistyped
  `{StaticResource}` fails only on first lookup.

## Mapping rules (draft)

How a WinRT XAML template and API become Avalonia. In Avonic the equivalent table is the core of
`CLAUDE.md`: written once, applied mechanically, never improvised.

**Draft and unproven.** The left column was first drawn from the Windows 10 19041 templates
(Button, CheckBox, ToggleSwitch, Pivot, PivotHeaderItem); counts for the 8.1 file are added where
they were taken (2026-11-07). The right column is a proposal, checked only for whether the
Avalonia 12.1.4 type or pseudoclass exists. No rule here has been proven on a real control, and
the 8.1 templates may use constructs these don't. Each rule is confirmed or replaced by the first
control that needs it, and **moves into `CLAUDE.md` only once proven**.

### Structure

| WinRT XAML | Avalonia | Note |
|---|---|---|
| `<Style TargetType="Button">` with a `Template` setter, unkeyed | `<ControlTheme x:Key="{x:Type Button}" TargetType="Button">` | 58 unkeyed styles in 8.1; 117 in 19041 |
| `<Style x:Key="…" TargetType="Button">` | A keyed `ControlTheme`, same key, used through `Theme="{StaticResource …}"` | An app's `Style="{StaticResource …}"` becomes `Theme=` |
| `BasedOn="{StaticResource …}"` | `BasedOn` on the `ControlTheme` | |
| A WinRT control Avalonia has as another type with a property set (PasswordBox: a `TextBox` with `PasswordChar`) | A `ControlTheme` for that type keyed by the WinRT name, used as `Theme="{DynamicResource PasswordBox}"` | As PasswordBox does ([#103](https://github.com/wdcossey/CornflowerUI/pull/103)). The checklist generator treats such a control as one Avalonia lacks; its plan maps it by hand |
| `{ThemeResource Key}` | `{DynamicResource Key}` | 1,657 uses in 8.1; 3,696 in 19041 |
| `{StaticResource Key}` | `{StaticResource Key}` in the same file; `{DynamicResource}` across files | Avalonia's docs say a `StaticResource` into another included file fails at load. Measured for a template part's `Theme` (`Wp81HeaderContentPresenterTheme`, in `Controls/HeaderContentPresenter.axaml`, from `TextBox.axaml`): it compiles without a warning and throws `InvalidCastException` when the template is built |
| `{TemplateBinding P}` | `{TemplateBinding P}` | 502 uses in 8.1; 1,455 in 19041 |
| Theme dictionaries `Default`, `Light`, `HighContrastBlack`, `HighContrastWhite` (8.1) | `ResourceDictionary.ThemeDictionaries` keyed `Dark`, `Light` | `Default` is the dark one. High contrast is later |
| A resource key | The same key name | Faithful, and anyone who knows the platform's keys can restyle |
| A literal size in a template (`MinWidth`, `Padding`) | The same literal in our theme | A theme may hold sizes. Only colours are confined to `Tokens/` |

### Parts

| WinRT XAML | Avalonia | Note |
|---|---|---|
| `x:Name="ContentPresenter"` in a built-in's template | The part name Avalonia's control looks for, such as `PART_ContentPresenter` | Avalonia's code decides the name. Check each control's `[TemplatePart]` attributes |
| `x:Name="Foo"` in one of our controls' templates | `PART_Foo` | As Avonic does |
| A template part WinRT has and Avalonia's control doesn't know | Present for looks, but the control won't drive it; or a part class of ours drives it (rung 3) | A source of behaviour differences. List each in the plan |
| Two parts that show one content, one per state (ToggleButton's `EnabledBackground` and `EnabledContent`, `DisabledBackground` and `DisabledContent`) | One part, restyled per state | Avalonia can't show one element in two presenters. Same pixels ([#93](https://github.com/wdcossey/CornflowerUI/pull/93)) |
| `x:DeferLoadStrategy="Lazy"`, `x:Load` | No equivalent. `IsVisible="False"`, or built in code | 29 uses in 19041; not counted in 8.1. Costs start-up time; measure it |

### Visual states

Avalonia has no `VisualStateManager`. A state is a pseudoclass, and what a state sets is a setter
in a nested style.

| WinRT XAML | Avalonia | Note |
|---|---|---|
| `VisualState x:Name="PointerOver"` in `CommonStates` | `<Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">` | Check whether the phone shows a hover state at all. Avonic found `:pointerover` also applies during a touch; in Avalonia 12.1.4 a touch doesn't set it ([#93](https://github.com/wdcossey/CornflowerUI/pull/93)) |
| `Pressed` | `^:pressed` | |
| `Disabled` | `^:disabled` | |
| `Normal` | The theme's plain setters | |
| `ObjectAnimationUsingKeyFrames` with one `DiscreteObjectKeyFrame KeyTime="0"` | A `Setter` | An instant assignment written as an animation. 174 storyboards in 8.1, 531 in 19041; most are these |
| `<VisualState.Setters>` | `Setter`s | None in 8.1, where setters didn't exist yet; 480 blocks in 19041 |
| Combined states (`UncheckedNormal`, `CheckedPointerOver`, `IndeterminatePressed`) | Combined pseudoclasses: `^:checked:pointerover` | Avalonia has `:checked`, `:unchecked`, `:indeterminate` |
| A pressed state of a checked control that sets nothing (ToggleButton's `CheckedPressed`) | Left out of the checked selector: `^:checked:not(:pressed), ^:checked[(wp81\|PhonePress.IsOff)=True]` | The checked look comes back when the finger slides off, as on the phone. `PhonePress`, internal to the theme, tells that, as `:pointerover` can't under a touch ([#93](https://github.com/wdcossey/CornflowerUI/pull/93)) |
| ToggleSwitch: `On`, `Off`, `Dragging` | `^:checked`, `^:unchecked`, `^:dragging` | |
| `SelectionStates`: `Selected`, `Unselected` | `^:selected` | |
| `FocusStates`: `Focused`, `PointerFocused` | `^:focus-visible`, `^:focus` | |
| `CommonStates` `Focused` on a text control (TextBox, PasswordBox, RichEditBox) | `^:focus` | Not `^:focus-visible`: a tap focuses a text box on the phone too. The rule above is for `FocusStates`; the checklist generator maps `Focused` by its group. As TextBox does ([#101](https://github.com/wdcossey/CornflowerUI/pull/101)) |
| A state Avalonia's control doesn't raise | For a built-in: an attached behaviour that sets a pseudoclass, a part class, a subclass under rung 4, or a recorded gap, in that order. For ours: a pseudoclass named after the state, lower case | |
| `Orientation`: `Portrait`, `Landscape` | A pseudoclass our control sets from the top level's size | Avalonia has no `:landscape` |
| `VisualTransition From="A" To="B" GeneratedDuration="…"` | `Transitions` on the part, with that duration | 50 in 8.1. A WinRT transition runs between two named states only; an Avalonia transition runs on any change of the property. Record where that differs. Tested at its end value |

**8.1 states no rule covers yet, on controls Avalonia has.** The checklist generator, run on all
57 styled 8.1 controls on 2026-11-09, shows these as "no rule". The first control that has one
decides it. The generator matches Avalonia's control by name, so a control Avalonia has under
another name (PasswordBox, ListViewItem) is treated as one Avalonia lacks, and its states get the
lower-case pseudoclass of the rule for ours above; its plan maps them by hand. `Portrait` and
`Landscape` have the orientation rule above, but it names no pseudoclass yet, so the generator
doesn't apply it:

| State | Controls |
|---|---|
| `MouseOver` | CheckBox, RadioButton |
| `TouchIndicator`, `NoIndicator`, `MouseIndicator` | ScrollBar, ScrollViewer |
| `OnContent`, `OffContent` | ToggleSwitch |
| `Full`, `InlineNormal`, `InlinePlaceholder`, `Highlighted`, `Opened`, `Closed` | ComboBox |
| `Determinate`, `Updating`, `Paused`, `Error` | ProgressBar |
| `Portrait`, `Landscape` | FlyoutPresenter |
| `TopPortrait`, `BottomPortrait`, `LeftLandscape`, `RightLandscape`, `None` | MenuFlyoutPresenter |

Precedence, as Avonic measured it: a rule with a pseudoclass or class beats one without, then
later beats earlier. A value set directly on an element in the template beats a plain style but
not a pseudoclass style. So a part's default goes in a style, not on the element, if a state has
to change it.

### Theme animations

The hard part. These elements name an animation and the platform supplies the motion.

| WinRT XAML | Uses in 8.1 | Uses in 19041 | Avalonia | Note |
|---|---|---|---|---|
| `PointerDownThemeAnimation`, `PointerUpThemeAnimation` | 18, 31 | 38, 78 | An attached behaviour, set by the template (`Tilt.IsEnabled`, #65), driving a perspective `MatrixTransform` | The tilt. Angle, depth and timing read every frame under real touches, 100 presses over four sizes ([#60](https://github.com/wdcossey/CornflowerUI/pull/60)), and modelled in [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-tilt) |
| `PointerDownThemeAnimation` on a part other than the template's root | | | `Tilt.IsTarget` on that part; the root around it stays untilted and takes the hits, with a transparent background | The phone's root is already untilted round that part, so no hit root is added, as Button's theme adds one round its tilted `Grid`, the phone's root. HyperlinkButton's `PART_ContentPresenter` ([#91](https://github.com/wdcossey/CornflowerUI/pull/91)), CheckBox's `Grid` ([#92](https://github.com/wdcossey/CornflowerUI/pull/92)), RadioButton's `Container` ([#94](https://github.com/wdcossey/CornflowerUI/pull/94)). A template that runs no theme animation gets no tilt: ToggleButton and RepeatButton ([#93](https://github.com/wdcossey/CornflowerUI/pull/93)), whose forced `Pressed` captures are flat. A template with a `PointerDownThemeAnimation` and no `PointerUpThemeAnimation` transition (HyperlinkButton, RadioButton) may snap back on release; `Tilt` eases back as on a Button until a probe measures one |
| `RepositionThemeAnimation` | 9 | 5 | A transition on the part's offset | The ToggleSwitch knob |
| `FadeInThemeAnimation`, `FadeOutThemeAnimation` | 4, 4 | 17, 14 | An opacity transition | Durations measured |
| `EntranceThemeTransition`, `ContentThemeTransition` | — | 5, 4 | Composition implicit animations, or an `IPageTransition` | An infrastructure item |
| `AddDeleteThemeTransition`, `ReorderThemeTransition` | 3, 3 | 3, 4 | Composition implicit animations on the items panel | With lists |
| `DragItemThemeAnimation`, `DragOverThemeAnimation`, `DropTargetItemThemeAnimation` | —, 8, 2 | 4, 8, 2 | Later, with list reordering | |
| `PopupThemeTransition` | 1 | — | To be decided with the first popup | |
| `SplitOpenThemeAnimation`, `SplitCloseThemeAnimation` | — | 1, 1 | With ComboBox | |
| `ContentTransitions="{TemplateBinding …}"` | | | No equivalent property | Record |

A dash means the animation isn't in that file's count. A motion we must match frame by frame
reads `CornflowerClock`, which `TestClock` replaces, so a test asserts its timeline to the
millisecond. A theme transition built on Avalonia's `Transitions` is tested at its end value.

### Text

| WinRT XAML | Avalonia | Note |
|---|---|---|
| `PhoneFontFamilyNormal` and the other font family keys | The font resource, holding the fallback chain | See [Fonts and icons](#fonts-and-icons) |
| `FontWeight="SemiLight"` | `PhoneFontFamilySemiLight`, the Semilight family by name | Selawik's Semilight file reports weight 300. On the Normal family, SemiLight draws Light |
| `CharacterSpacing="n"` | `LetterSpacing` = n × `FontSize` / 1000 | WinRT's unit is thousandths of an em; Avalonia's is DIPs ([spike 5](spikes/README.md#5-the-unit-of-textblockletterspacing)) |
| `OpticalMarginAlignment="TrimSideBearings"` | No equivalent | Matters for Metro's large titles, which line up on the letter's edge. Measure the offset; likely a recorded deviation |
| `TextLineBounds`, `LineStackingStrategy` | No equivalent | Where the style sets no `LineHeight`, ours sets 1.33 × `FontSize` (see [Fonts and icons](#fonts-and-icons)). Styles with their own `LineHeight` and `BaselineToBaseline`, measured with the type ramp (#69): on the phone the first baseline is Segoe WP's ascent below the top (1.0791 em) whatever `LineHeight` is, and the height adds Segoe WP's share of descent (514/2724) × `LineHeight`. Ours keeps WinRT's `LineHeight` (the distance between baselines) and a `Padding` takes the difference above the first line and below the last, worked out from each `TextBlock`'s own font by `PhoneTextLine`, internal to the theme. Heights within 0.006 DIP of every dump |
| A `Path` drawing a glyph in a template | A `PathIcon` with a named geometry resource of our own drawing | |
| `SymbolIcon Symbol="Setting"` | Our icon control with the mapped Lucide name | |

### Other properties

| WinRT XAML | Avalonia | Note |
|---|---|---|
| `BackgroundSizing="OuterBorderEdge"` | `BackgroundSizing="OuterBorderEdge"` | Exists in 12.1.4 |
| `UseSystemFocusVisuals`, `FocusVisualMargin` | `FocusAdorner`, with the margin in its template | Avonic found an unset `FocusAdorner` falls back to the base theme's, so set it deliberately |
| `AutomationProperties.AccessibilityView="Raw"` | The same attached property | |
| `IsTabStop="False"` | `IsTabStop="False"` | |
| A WinRT flag Avalonia expresses as a class (`IsPasswordRevealButtonEnabled`) | That class (`revealPasswordButton`) | An app sets `Classes="revealPasswordButton"`. As PasswordBox does ([#103](https://github.com/wdcossey/CornflowerUI/pull/103)) |
| `ItemsPanelTemplate` | `ItemsPanelTemplate` | |
| `CornerRadius="{TemplateBinding CornerRadius}"` | The same | Zero throughout these eras |

### API of new controls and subclasses

| WinRT | Ours | Example |
|---|---|---|
| Class `Pivot` | The same name, in our namespace | Unless Avalonia has a type of that name; then Avalonia's is themed. If its own input or selection path must change, ours is a **subclass** of Avalonia's with the same name, under rung 4, never an unrelated class of that name |
| Class `ComboBox`, whose full-screen mode Avalonia's class can't reach | `Cornflower.Controls.ComboBox : Avalonia.Controls.ComboBox`; `StyleKeyOverride` unchanged; its `ControlTheme` `BasedOn` the base's | The one expected subclass. Every base member keeps its meaning; its README lists it as a type substitution |
| Dependency property `Title` | `StyledProperty` `Title`, same name and type | |
| Event `SelectionChanged` | A routed event, same name | |
| Method | The same name; an async WinRT method that only awaits an animation stays async | `ContentDialog.ShowAsync` |
| An enum | The same name and members | `PivotSlideInAnimationGroup` |
| `[TemplatePart]`, `[TemplateVisualState]` | `[TemplatePart]`, `[PseudoClasses]` | |
| A member that depends on the Windows platform (`CoreDispatcher`, tiles, contracts) | Not declared, with the reason in the checklist | No dead props |
| Something we add | An extension, in its own doc section, never shadowing a WinRT name | |

### Avalonia facts Avonic already paid for

These apply unchanged. Sources are in Avonic's
[swarm brief](https://github.com/wdcossey/Avonic/blob/v2/docs/infra/swarm-brief.md) and
[performance notes](https://github.com/wdcossey/Avonic/blob/v2/docs/infra/performance/README.md).

**Styling cost**

- Every global style rule is checked against every element in the app, at about 30 ns per
  alternative. Prefer a `ControlTheme` for anything about a control's own template.
- Never write a descendant selector whose target is `TextBlock` or another common type.
- Avalonia's diagnostics are built in behind an `AppContext` switch. Use its counts, not its
  times.

**ControlTheme limits**

- `c|Host ^` inside a `ControlTheme` compiles, ignores the host, and applies everywhere.
- A `ControlTheme` can't reach a control's content children. Those rules need a `Styles` file.
- Global `Styles` beat a `ControlTheme`.

**XAML**

- An XML comment can't contain `--`.
- `x:Static` can't take `x:Key`. `TemplatedParent` isn't allowed under comma selectors.
- A dictionary entry is built on first lookup, so a mistyped `{StaticResource}` fails late.

**Layout and rendering**

- `ContentControl`, `TemplatedControl` and `TextBlock` clip by default. Layout tests don't show
  it; check by pixel or hit test.
- A `Border`'s `ClipToBounds` clips its own `BoxShadow`.
- A child measured at zero stays at zero. Test in a scroller, not only a fixed-size box.
- `TextWrapping`, `TextTrimming` and `LineHeight` inherit.

**Input**

- `Tapped` has a movement limit and isn't raised on a second quick press. A button-like control
  tracks press and release itself.
- `:pointerover` also applies during a touch, Avonic found. Not in Avalonia 12.1.4: a touch doesn't set it
  ([#93](https://github.com/wdcossey/CornflowerUI/pull/93)), so the ToggleButton theme tells a finger slid off with `PhonePress`.

**Headless tests**

- Transitions run on real time: Avalonia 12's animation clock is internal. Set `Transitions = []`
  to test an end value.
- Hit tests read the last rendered scene, so render first.
- A test app with no theme installed shows nothing that comes from one. Install the theme in the
  test, and remove it afterwards.
- Headless tests can't see everything. Run the real target once per batch.
