# Roadmap

Era 1 is Windows Phone 8.1. This file is the map; the GitHub issues are the work. `CLAUDE.md`
lists only what exists.

Process per issue: [`PROCESS.md`](PROCESS.md) §0. Start an issue only when its blockers are
merged. Decisions behind this plan: [ADR 0001](decisions/0001-what-faithful-means.md),
[ADR 0002](decisions/0002-decisions-of-2026-11-08.md),
[ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md),
[ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md) and
[ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md).

## Scope

| | |
|---|---|
| **In, first** | Windows Phone 8.1's XAML control set: a theme for Avalonia's controls plus the controls Avalonia lacks, dark and light, with the accent colour. Mobile and desktop are both priorities: the core is the phone, at any window size, and keeps the hooks a wider layout needs ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)) |
| **Later** | Windows Phone 8.0's Silverlight-only controls (Panorama, LongListSelector, HubTile, the toolkit's ListPicker), as an add-on to era 1. Layouts for wider windows, as a separate extension pack over the core theme, labelled as Cornflower's design, after Tier 2 ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)). Windows 10 Mobile as a second era. Windows 10 desktop as a third. High contrast |
| **Out** | MapControl, MediaElement, CaptureElement, WebView, ink, live tiles, the phone's system UI (Start screen, status bar, keyboard). Anything that needs a Microsoft font or a copied Microsoft template |

Not in scope at all: a port of UWP or WinRT XAML (Uno Platform does that), and Windows 11 Fluent
(Avalonia's own themes and FluentAvalonia cover it).

## The inventory

Read on 2026-11-07 from the phone's `Windows.winmd` and `generic.xaml`, taken from the Windows
Phone 8.1 SDK on the reference VM. The `.winmd` was read with [`tools/winrt-api`](../tools/winrt-api/Program.cs). The full type table is in
the reference folder, as `wp81-control-types.md`.

| | Count |
|---|---|
| Public types under `Windows.UI.Xaml.Controls` and `.Primitives` in the phone `Windows.winmd` | 115 |
| Of those, concrete types deriving from `Control` | 71 |
| With a default style in the phone `generic.xaml` | 56 |
| Without one | 15: AppBar, AppBarSeparator, SettingsFlyout, ToolTip, UserControl, and the bases ButtonBase, Control, ItemsControl, ListViewBase, ListViewBaseHeaderItem, MenuFlyoutItemBase, MenuFlyoutSeparator, RangeBase, Selector, SelectorItem |
| Default (unkeyed) styles in `generic.xaml` | 58, two of them for ListViewItem |
| Panels and other `FrameworkElement`s with no template | 36, among them Border, Canvas, Grid, StackPanel, Viewbox, Image, TextBlock, RichTextBlock, the icon elements, the virtualising panels, PivotPanel, PivotHeaderPanel, CarouselPanel, LoopingSelectorPanel, TickBar, Popup, WebView, MediaElement, CaptureElement |

AppBar, AppBarSeparator, SettingsFlyout and ToolTip are in the phone's metadata with no default
style: they are Windows 8.1 desktop controls carried in the API, and there is no phone look to
reproduce.

**Completeness.** Avalonia has templated controls the phone never had. The measure of era 1's
completeness is the count from the test that lists every templated Avalonia control without a
Wp81 theme ([#13](https://github.com/wdcossey/CornflowerUI/issues/13);
[ADR 0002](decisions/0002-decisions-of-2026-11-08.md#themes-and-scope)): 79 at Avalonia 12.1.4,
before any control (2026-11-09). Each control's PR deletes its own lines from
`tests/Cornflower.Tests/Completeness/Holes.txt`.

## Tiers

| Tier | Meaning |
|---|---|
| **0** | Tooling and acquisition. Nothing in Tier 1 starts until this is done, except the type ramp ([#26](https://github.com/wdcossey/CornflowerUI/issues/26)), by [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md) |
| **1** | Foundation controls, one PR each at full depth. Button first, then ProgressRing, then the rest |
| **2** | The shell: what makes a screen look like a phone app. Page, app bar, Pivot, Hub, list |
| **3** | Pickers and dialogs |
| **4** | The rest, and the plain themes for Avalonia controls the phone never had |

Button is the worked example every later themed control copies, as `ion-item` was for Avonic.
ProgressRing is the worked example for a control we own. Tier 2 starts only when every Tier 1
control is merged with its comparison.

## Controls

The Avalonia column was checked on 2026-11-06 against `Avalonia.Controls.dll` in the Avalonia
12.1.4 reference assemblies: each named type exists, or doesn't. Cells marked "to check" were
added on 2026-11-07 without that check. The rung is the planned step of
[the ladder](ARCHITECTURE.md#the-limit-of-a-theme); each is confirmed by its control's Compare
step, and a control that climbs a rung says so in its PR.

### Tier 1

| Issue | Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|---|
| [#27](https://github.com/wdcossey/CornflowerUI/issues/27) | Button | Button | Theme, tilt. The dry run of the whole loop | 1, 2 |
| [#28](https://github.com/wdcossey/CornflowerUI/issues/28) | ProgressRing | none | New control | 5 |
| [#26](https://github.com/wdcossey/CornflowerUI/issues/26) | TextBlock and the text styles | TextBlock | The type ramp, as style classes and resources | 1 |
| [#29](https://github.com/wdcossey/CornflowerUI/issues/29) | HyperlinkButton | HyperlinkButton | Theme, tilt | 1, 2 |
| [#30](https://github.com/wdcossey/CornflowerUI/issues/30) | RepeatButton, ToggleButton | the same | Theme, tilt | 1, 2 |
| [#31](https://github.com/wdcossey/CornflowerUI/issues/31) | CheckBox | CheckBox | Theme, glyph, tilt | 1, 2 |
| [#32](https://github.com/wdcossey/CornflowerUI/issues/32) | RadioButton | RadioButton | Theme, tilt | 1, 2 |
| [#33](https://github.com/wdcossey/CornflowerUI/issues/33) | ToggleSwitch | ToggleSwitch | Theme. Avalonia's has no `Header`: an attached property. A subclass only if the drag measurement fails | 1, 2 |
| [#34](https://github.com/wdcossey/CornflowerUI/issues/34) | Slider | Slider | Theme | 1 |
| [#35](https://github.com/wdcossey/CornflowerUI/issues/35) | ProgressBar | ProgressBar | Theme. The indeterminate dots are the hard part | 1 |
| [#36](https://github.com/wdcossey/CornflowerUI/issues/36) | TextBox | TextBox | Theme, clear button glyph, `Header` | 1, 2 |
| [#37](https://github.com/wdcossey/CornflowerUI/issues/37) | PasswordBox | TextBox with `PasswordChar` | Theme. No separate control in Avalonia | 1, 2 |

### Tier 2

| Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|
| ScrollViewer | ScrollViewer | Theme: the scroll indicators, set by a behaviour from the pointer type. Scroll physics measured first: a part class or a recorded deviation | 1, 2, 3 |
| ScrollBar, Thumb | both exist | Theme. ScrollBar is the phone's thin indicator | 1 |
| ContentControl, ContentPresenter | both exist | Theme | 1 |
| Page, Frame | NavigationPage, ContentPage (new in 12.0); Frame exists | Theme, plus our own page transitions. Frame is the navigation host | 1 |
| CommandBar, AppBarButton, AppBarToggleButton, AppBarSeparator | CommandBar, CommandBarButton and the rest (new in 12.0) | Theme. Measure what Avalonia's lacks: the minimised bar (`ClosedDisplayMode`, an attached property), the ellipsis menu. AppBarSeparator has no default style on the phone | 1, 2 |
| Pivot, PivotItem, PivotHeaderItem | none (`TabControl` and `Carousel` are the nearest) | New control. Its header strip scrolls with the content, which neither does | 5 |
| Hub, HubSection | none | New control | 5 |
| ListView, ListViewItem, ListViewHeaderItem, GroupItem | ListBox, ListBoxItem; the header items: to check | Theme. Selection modes and item press feedback to be measured; the multi-select check mode by a behaviour; reorder deferred. The header items are a grouped list's group headers | 1, 2 |

### Tier 3

| Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|
| ComboBox, ComboBoxItem | ComboBox, ComboBoxItem | Theme for the inline form. A subclass, `Cornflower.Controls.ComboBox`, opens `ListPickerFlyout` for the full-screen form a long list gets | 1, 4 |
| ListPickerFlyout, PickerFlyout, and their presenters | none | New controls. The full-screen presenters are where the phone look lives | 5 |
| DatePicker, TimePicker, DatePickerFlyoutPresenter, TimePickerFlyoutPresenter | DatePicker, TimePicker, DatePickerPresenter, TimePickerPresenter | Theme. Avalonia's already use looping selectors. Full-screen presentation to be measured; if Avalonia's presenter can't be placed full screen, the presenter becomes a part class | 1, 3 |
| LoopingSelector, LoopingSelectorItem (primitives) | DateTimePickerPanel is the nearest; to check | Part of the pickers. Decide with them whether Avalonia's panel can be themed to match or a new control is needed | 5 |
| Flyout, MenuFlyout, MenuFlyoutItem, ToggleMenuFlyoutItem, FlyoutPresenter, MenuFlyoutPresenter | Flyout, MenuFlyout, FlyoutPresenter, MenuFlyoutPresenter exist; ToggleMenuFlyoutItem: to check | Theme | 1 |
| ContentDialog | none | New control | 5 |
| AutoSuggestBox | AutoCompleteBox | Theme. A different API | 1 |

### Tier 4

| Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|
| GridView, GridViewItem, GridViewHeaderItem | ListBox with a wrapping panel; the header item: to check | Theme | 1, 2 |
| SemanticZoom | none | New control: the jump list | 5 |
| FlipView, FlipViewItem | Carousel | Theme. Measure the snap and swipe | 1 |
| ListBox, ListBoxItem | ListBox, ListBoxItem | Theme | 1 |
| RichEditBox, RichTextBlock | none in the free packages | Later, or out | — |
| Controls Avalonia has and the phone never had: Calendar, TreeView, DataGrid, SplitView, Menu, TabControl, Expander, NumericUpDown, ColorPicker and more | — | A plain theme in the era's style, labelled as ours and not a reproduction | 1 |

### Out of scope

| Windows Phone 8.1 | Why |
|---|---|
| MapControl, MediaElement, CaptureElement | Out of scope. MapControl has a default style in the phone file; it stays out |
| WebView | Not in `Avalonia.Controls` 12.1.4; a separate package if anywhere. Nothing to theme |
| AppBar, SettingsFlyout, ToolTip | No default style on the phone |

## Tier 0

Nothing in Tier 1 starts until this list is done, except the type ramp (#26), which may start once
#20, #21, #22 and #24 are open and green (`D-tier1-start`,
[ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md)). The desktop sample (#9) didn't hold up
Tier 1.

- [x] A reference VM with the Windows Phone 8.1 tools (2026-11-07). The RTM and Update 1
      emulators are installed. The Update 1 720p emulator was started over SSH and ran a test app
      on 2026-11-08.
- [x] The phone's `generic.xaml`, `themeresources.xaml` and `Windows.winmd` located and readable
      (2026-11-07). Copies are in the reference folder, outside the repo.
- [x] The repo, with the build gate, CI, the three projects and auto-discovery (2026-11-08,
      the bootstrap commit on `main`).
- [x] An issue per item with tier and kind labels; `main` protected, with `build-test` required
      (2026-11-08).
- [x] `CLAUDE.md`, stating only what exists (2026-11-08). Mapping rules move into it as controls
      prove them.
- [x] ADR 0001 accepted, naming the reference build, image, profile and resolution (2026-11-08).
- [x] `PROCESS.md`, `ROADMAP.md`, `ARCHITECTURE.md` and ADR 0002 in the repo (2026-11-08,
      [#4](https://github.com/wdcossey/CornflowerUI/issues/4)).
- [x] The reference profile's resolution scale read by the reference app's first run on it, and
      ADR 0001 confirmed or amended ([#3](https://github.com/wdcossey/CornflowerUI/issues/3)).
- [x] This inventory regenerated, with real counts (2026-11-07).
- [x] `tools/winrt-api` written and run on the 8.1 metadata (2026-11-07). It hasn't been run on
      the Windows 10 files.
- [x] The reference app and capture script (2026-11-08): every catalogued control at rest and in
      every template state, dark and light, a PNG and a template-tree JSON each, pulled back with
      one command. First run, on the 720p profile: 504 captures, 0 errors. In
      [`tools/refapp`](../tools/refapp/README.md).
- [x] The reference app, `tools/winrt-api` and the font measurement scripts moved into `tools/`
      ([#5](https://github.com/wdcossey/CornflowerUI/issues/5)).
- [ ] Captures committed under `ref/wp81/<control>/`, from the reference profile.
- [x] The reference app captures flyouts, dialogs and the app bar
      ([#23](https://github.com/wdcossey/CornflowerUI/issues/23)). The popups have tree dumps, and 1080 × 1920
      screenshots from the emulator's Screenshot tool (`run.ps1 -Popups only -Grab`). The page's
      app bar is drawn by the shell, so its screenshots are the reference (2026-11-09, #59).
- [x] Motion recordings: the tilt, page transitions, ToggleSwitch, the pickers
      ([#24](https://github.com/wdcossey/CornflowerUI/issues/24)), on 2026-11-10 (#66): template
      storyboards read every frame, theme transitions from video (`run.ps1 -Record`). The numbers
      are in [`tools/refapp`](../tools/refapp/README.md) "Motion" and
      [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-motion). Not yet: the pickers'
      and the full Flyout's per-row swivel curves, and the turnstile's forward start angle.
- [x] Read from the emulator: the item count at which ComboBox goes full screen, and the
      ToggleSwitch drag threshold ([#25](https://github.com/wdcossey/CornflowerUI/issues/25)), on 2026-11-09 (#58):
      5 items open inline and 6 full screen, by count, not by screen. A ToggleSwitch release is a tap
      if it moved under about 18 view px and was held under about 1,020 ms; otherwise the knob's
      halfway point decides. In [`tools/refapp`](../tools/refapp/README.md) "Probes".
- [x] The checklist generator: one control's spec from the `.winmd` and `generic.xaml`
      ([#7](https://github.com/wdcossey/CornflowerUI/issues/7)), as
      `tools/winrt-api checklist <Type>` (2026-11-09). Run on all 57 styled 8.1 controls.
- [x] ~~`tools/winjs-ref`~~ ([#8](https://github.com/wdcossey/CornflowerUI/issues/8)). Not needed: the shipped WinJS
      2.1 for Phone stylesheet was already pulled on 2026-11-07.
- [ ] A side-by-side tool: two frames in one image.
- [x] A CI check that no Microsoft font or SDK file is committed
      ([#6](https://github.com/wdcossey/CornflowerUI/issues/6)).
- [x] The CI actions moved off Node 20 ([#2](https://github.com/wdcossey/CornflowerUI/issues/2)), in the
      bootstrap.
- [x] The desktop sample: a window that opens phone-sized and can be resized, one auto-discovered
      page per control ([#9](https://github.com/wdcossey/CornflowerUI/issues/9)), on 2026-11-10 (#68):
      `samples/Cornflower.Sample.Desktop`, the `[SamplePage]` catalogue, and
      [`tools/real-window/drive.ps1`](../tools/real-window), from Avonic. Moved up so the extension
      pack has a window to be seen in ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)).
      The phone still opens `MainView` alone.
- [x] The Android sample and a first device run on the Pixel 8
      ([#10](https://github.com/wdcossey/CornflowerUI/issues/10)), with
      [`tools/android/run.ps1`](../tools/android/README.md) (2026-11-09). Start times are in its
      README and in [`docs/performance`](performance/README.md).
- [x] The test harness: `Scene`, `Frames`, the clock hook and `TestClock`
      ([#11](https://github.com/wdcossey/CornflowerUI/issues/11)).
- [x] Metrics from Avonic: the style budget, `StyleTrace`, the census, the benchmark, the Debug
      performance line, the breakage tool ([#12](https://github.com/wdcossey/CornflowerUI/issues/12)),
      in [`docs/performance`](performance/README.md) (2026-11-09). Avonic's real-window driver came
      with the desktop sample (#9, #68).
- [x] The test that lists every templated Avalonia control without a Wp81 theme
      ([#13](https://github.com/wdcossey/CornflowerUI/issues/13)), with the list in
      `tests/Cornflower.Tests/Completeness/Holes.txt` (2026-11-09): 79 holes.
- [x] Spike: does swapping the theme at run time re-template every control?
      ([#14](https://github.com/wdcossey/CornflowerUI/issues/14))
- [x] Spike: what does a control with no `ControlTheme` do?
      ([#15](https://github.com/wdcossey/CornflowerUI/issues/15))
- [x] Spike: a `ControlTheme` keyed to a subclass with `BasedOn` the base's gets the base's
      template and can add to it; a subclass whose `StyleKeyOverride` returns the base type gets
      the base's theme unchanged ([#16](https://github.com/wdcossey/CornflowerUI/issues/16)).
- [x] Spike: an attached tilt on a stock `Button` reads the press point through
      `handledEventsToo` ([#17](https://github.com/wdcossey/CornflowerUI/issues/17)).
- [x] Fonts: Selawik bundled; Semilight addressed by name and the line-height metric settled by
      test ([#18](https://github.com/wdcossey/CornflowerUI/issues/18)). Selawik was measured
      against Segoe UI on 2026-11-06 and against Segoe WP on 2026-11-07.
- [x] Tokens: theme colours dark and light, the accent and the 20 named accents, the type ramp,
      sizes ([#19](https://github.com/wdcossey/CornflowerUI/issues/19)).
- [x] The template glyph set, drawn as our own paths and tested
      ([#20](https://github.com/wdcossey/CornflowerUI/issues/20)), on 2026-11-10 (#63). 8.1's templates
      draw two: the tick (CheckBox and list items) and the selected item's earmark; the shell
      draws a third, the app bar's ellipsis. The rest are rectangles and ellipses.
- [x] The Lucide generator and icon control ([#21](https://github.com/wdcossey/CornflowerUI/issues/21)),
      on 2026-11-10 (#64): [`tools/lucide`](../tools/lucide), pinned to Lucide 1.71.0, and
      `LucideIcon`. 161 of WinRT's 193 `Symbol` values have a Lucide icon
      (`tools/lucide/symbols.tsv`).
- [x] The tilt behaviour, measured and tested ([#22](https://github.com/wdcossey/CornflowerUI/issues/22)),
      on 2026-11-10 (#65): `Tilt.IsEnabled`, fitted to #60's 100 grid presses within 0.019°, the
      screen-position bias included. Open: whether the eye distance scales with the screen, the
      fitted eye (1,055 view px) against PlaneProjection's measured camera (1000), and a press at
      the very edge, which clicks on the phone and must click here too
      ([ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-tilt)).

## What "done" means for Button

Button ([#27](https://github.com/wdcossey/CornflowerUI/issues/27)) is a dry run of the loop, to
prove the pipeline before anything is batched:

1. The checklist generated from the 8.1 files, every row ticked or explained.
2. Captures from the emulator: rest, pressed, disabled, dark and light, and a recording of the
   tilt.
3. Avalonia's Button measured against them.
4. The theme written; tests assert the measured sizes, colours and tilt timeline.
5. Our frame beside the emulator's, differences listed with measurements.
6. Two reviews, deliberate breakage, and the sample on the Android device.
7. Its README, with the `Status:` line.

If a step can't be done, the gap is in Tier 0 and is fixed there, not worked around in the
control.

## Order of eras

1. Era 1 to depth: the Tier 1 controls finished and compared, before anything else.
2. The rest of era 1.
3. Then a choice between Windows Phone 8.0's controls, as an add-on to era 1, and era 2, Windows
   10 Mobile. Era 2 reuses more.

The cost of a second era is not double. The controls package, the tools, the tests' structure
and the mapping rules carry over. The tokens, the templates and every comparison are new.

## Done

Rolled up at the end of each tier ([process](PROCESS.md#rolling-up-the-tables)). For the latest,
see the `Status:` line in each README under `docs/controls/`.

| Issue | Windows Phone 8.1 | Status |
|---|---|---|
| | None yet | |
