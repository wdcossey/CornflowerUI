# refapp: the Windows Phone 8.1 reference app

The reference app named in [PROCESS](../../docs/PROCESS.md), with its capture script. It runs in
the Windows Phone 8.1 emulator on the reference VM and photographs every control in the
catalogue. Each control is captured at rest and in every visual state its default template
declares, in dark and light. Beside each PNG it writes a JSON dump of the control's whole
template tree: bounds, brushes, thicknesses, fonts and transforms, as the phone resolved them.
That pair is what a Cornflower theme is measured against.

It grew out of a one-page probe on 2026-11-08, which proved the pipeline. The probe's first
numbers are kept below.

## Running it

From this PC, with the VM up and signed in:

```
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1             # device 11, the reference: 8.1 Update 1 1080p 5.5"
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Device 10  # 8.1 Update 1 720p 4.7"
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Popups only                     # the popups alone, in about 2 minutes
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Only 'CheckBox|Radio'             # just these specimens
```

Results go to `R:\cornflower-reference\refapp\<yyyyMMdd-HHmm>-dev<n>\`: one PNG and one JSON per
capture, `index.json` for the run, and `index.html`, which lays every state out dark beside light.
Device indexes are in `R:\cornflower-reference\wp81-emulator-profiles.md`.

`-Only <regex>` keeps the specimens and popups whose name matches. `-Popups also` adds the popups to a
run and `-Popups only` runs them alone; by default a run leaves them out, because with them inside
a full run the emulator connection broke at about 477 PNGs, twice. The app reads these from a
`run-options.txt` that `run.ps1` writes into the package for that run and then deletes.

A run that stops making PNGs for 3 minutes is given up, and whatever the app wrote is still
pulled back, to a folder ending `-partial`. Its `progress.txt` names the last item started.

## What it does

| Step | Where | How |
|---|---|---|
| Copy | `run.ps1` | `scp` of `app\` and `vm\` to `C:\cornflower\refapp\` on the VM |
| Build | `vm\build.ps1` over SSH | MSBuild 14, Debug, x86. About 10 s. No certificate needed |
| Deploy, run, pull | `vm\session.ps1`, as a scheduled task in the signed-in desktop | `AppDeployCmd /installlaunch` starts the emulator if needed; `ISETool dir` polls for `done.txt`; `ISETool ts` pulls the app's local folder. Progress goes to `status.txt` |
| Wait | `run.ps1` | Reads `status.txt` every 20 s |
| Collect | `run.ps1` | `scp` back, flattens, writes `index.html`, removes the task |

Each step is its own SSH command, because one long remote script that also polled a scheduled
task once hung the session. The emulator, `AppDeployCmd` and `ISETool` only see each other from
the signed-in desktop. Run from an SSH session, `ISETool` starts a hidden second emulator instead.

## The app

- **`Catalog.cs`** holds the specimens. Each is a control in one configuration, such as
  `CheckBox` `indeterminate`, with the height of its stage. Every TextBlock style key in the phone's
  `generic.xaml` and `themeresources.xaml` is a specimen too, loaded through `XamlReader`. Add a
  control by adding a line here.
- **`Suite.cs`** builds a fresh stage per capture. The stage is the window's width and the
  specimen's height, in the requested theme, over the page's own background. The specimen sits
  at the page margin of 19 logical pixels, or full width for Pivot and Hub. The suite forces each
  state with `VisualStateManager.GoToState`, waits 450 ms, renders the stage with
  `RenderTargetBitmap` and writes the PNG and JSON. It writes `index.json` and then `done.txt` last.
- **`Popups.cs`** holds the popups: Flyout (top and full), MenuFlyout, ContentDialog (default and
  full size), ListPickerFlyout (single and multiple), PickerFlyout, DatePickerFlyout,
  TimePickerFlyout, and ComboBox open with 3 items (inline) and 10 (the full-screen picker).
  **`Suite.Popups.cs`** opens each from a button or the control on a stage, gives each open popup
  the requested theme, renders the stage, and dumps every open popup's tree, in window
  coordinates, to `<theme>__<name>__<variant>__popup<n>.json`. It then closes the popup and waits
  up to 3 s for it to animate out.
- **`TreeDump.cs`** writes the visual tree. Bounds are relative to the specimen, and the stage
  offset is recorded, so a JSON rectangle maps to PNG pixels as
  `(elementInStage + bounds) × rawPixelsPerViewPixel`. Not `resolutionScale`: it reports 180 on
  every profile read so far, including the 1080p ones at 2.4 and 2.2.

What a capture is and isn't:

- It is what the template draws in that state, on a fresh instance. It is not the motion into
  the state: `useTransitions` is false. The tilt and other theme animations come from screen
  recordings with `ffmpeg`.
- A state is forced, not reached by input. Where `stateApplied` is false, the template has no
  state of that name and the PNG shows the rest state.
- **A popup has numbers but no pixels.** `RenderTargetBitmap` can't see popups on the phone:
  rendering a popup's child throws `ArgumentException`, and rendering the whole window
  (`RenderAsync(null)`) leaves popups out. Moving a popup's child onto a stage does render, but
  not as the phone draws it: the content lays out again and loses its state (a list picker came
  out 1921 × 2066, a menu flyout with no items), so that was dropped. Popup pixels need a capture
  of the emulator's screen; that is still to do, with the app bar.
- **Never close a popup by force.** Setting `IsOpen = false` on a full-screen picker or dialog
  while it animates out killed the app in all three runs that did it. Closing with `Hide()` and
  waiting for the popup to go, the next run finished.

## Facts the pipeline relies on

- `RenderTargetBitmap` sizes its bitmap to what is drawn, not to the layout slot. A stage with an
  explicit size and an opaque background gives full frames. On the probe, a 400 × 711 page with
  no explicit size gave a 652 × 262 bitmap.
- Theme resources resolve against the theme of the tree they are mounted in. That is why every
  stage carries its own `RequestedTheme`, and why the index's resource probe is mounted before it
  is read.
- First boot of the emulator takes about 2 min 20 s. A redeploy to a running emulator takes
  about 20 s.

## First run (2026-11-08, device 10, Update 1 720p 4.7")

`R:\cornflower-reference\refapp\20261108-0754-dev10\`: **504 captures, 0 errors**, 4 min 30 s
in the app and 5 min 21 s end to end on a warm VM with a cold emulator. Every forced state was
accepted.

| Read by the app | Value |
|---|---|
| Display | scale 180, window 400 × 711.11 logical px, raw DPI 309 × 312 |
| Page background | dark `#FF000000`, light `#FFFFFFFF` |
| `PhoneForegroundBrush` | dark `#FFFFFFFF`, light `#FF000000` |
| `PhoneAccentBrush` (the emulator's default accent) | `#FF3E65FF` in both themes |
| Device | Microsoft "Virtual", Windows Phone |
| Button, `Pressed` forced | Template root gets a `PlaneProjection` of −17.19° X and +17.19° Y (0.3 rad) with compensating offsets; the border fills with the accent. The visible border is 41.8 tall, inset 9.5 inside the 57.2 tall control |

Captures per control, both themes: TextBlock styles 28, Button 10, HyperlinkButton 10,
RepeatButton 10, ToggleButton 52, CheckBox 54, RadioButton 36, ToggleSwitch 60, Slider 6,
ProgressBar 24, ProgressRing 10, TextBox 16, PasswordBox 14, ComboBox 22, DatePicker 2,
TimePicker 2, AppBarButton 12, AppBarToggleButton 36, ListViewItem 84, ListView 2, Pivot 6, Hub 8.

**What it doesn't reach yet:**
- **States on inner parts.** DatePicker, TimePicker and ListView declare no states on their own
  template root. Their states are on inner parts, such as the date picker's flyout button and
  the list's items, and the suite doesn't walk those yet. The list items are covered by the
  separate ListViewItem specimen. The Slider's template really has only Normal and Disabled.
- **Popup pixels and the app bar.** Popups are dumped, not rendered (above). The page's app bar
  isn't captured at all yet, though the full-screen pickers and ContentDialog each bring a
  CommandBar in a popup of their own, and its tree is dumped.
- **Other profiles.** Devices 10, 11 and 12 have run (below). Run `-Device 13` for qHD 5", and so on.

## Runs by profile (2026-11-08)

All three: 504 captures, 0 errors, accent `#FF3E65FF`. The reference profile is device 11
([ADR 0001](../../docs/decisions/0001-what-faithful-means.md)).

| Device | Profile | Folder | `RawPixelsPerViewPixel` | View | `ResolutionScale` |
|---|---|---|---|---|---|
| 10 | U1 720P 4.7" | `20261108-0754-dev10` | 1.8 | 400 × 711.11 | 180 |
| 11 | U1 1080P 5.5" | `20261108-1443-dev11` | 2.4 | 450 × 800 | 180 |
| 12 | U1 1080P 6" | `20261108-1451-dev12` | 2.2 | 490.91 × 872.73 | 180 |

## Popups on the reference profile (2026-11-08, device 11)

`20261108-1705-dev11` and `20261108-1734-dev11` (`-Popups only`): 44 popup dumps each, both themes. The open
popups, in the order `GetOpenPopups` returns them, with their window bounds in logical pixels:

| Popup | Open popups | Bounds | Chrome, dark / light |
|---|---|---|---|
| Flyout, default (top) | FlyoutPresenter | 0, 0, 450 × 78.33 | `#FF1F1F1F` / `#FFDDDDDD` |
| Flyout, full | FlyoutPresenter | 0, 0, 450 × 800 | same |
| MenuFlyout | a full-window Rectangle, then MenuFlyoutPresenter | menu at 0, 85, 450 × 193.33 | `#FF000000` / `#FFFFFFFF` |
| ContentDialog, both | a CommandBar, the ContentDialog, a `#99000000` Rectangle | 0, 0, 450 × 800 | `#FF1F1F1F` / `#FFDDDDDD` |
| ListPickerFlyout, PickerFlyout, DatePickerFlyout, TimePickerFlyout, ComboBox with 10 items | the presenter, then a CommandBar | 0, 0, 450 × 800 | `#FF1F1F1F` / `#FFDDDDDD` |
| ComboBox with 3 items | none: it opens inline, in the control | | |

The ComboBox with 10 items opens a `ListPickerFlyoutPresenter`: the full-screen picker.

## The probe's first numbers (2026-11-08, Update 1 720p 4.7", dark)

| | Value |
|---|---|
| `DisplayInformation.ResolutionScale` | 180 (`RawPixelsPerViewPixel` 1.8, `LogicalDpi` 172.8, raw DPI 309 × 312) |
| `Window.Current.Bounds` | 400 × 711.11 logical px |
| Default `Button`, content "button" | 108.89 × 57.22, `MinWidth` 109, `MinHeight` 57.5, `Padding` 9.5,0,9.5,3.5, `BorderThickness` 2.5, `FontSize` 18.14 |
| `TitleTextBlockStyle` | 18.14 px, weight 600, height 23.63, Segoe WP |
| `BodyTextBlockStyle` | 18.14 px, Segoe WP |

Nothing in this folder is Microsoft's: the project, manifest and placeholder logos were written
here. The captures are ours, but they stay in `R:\cornflower-reference\` until the repo's
`ref/wp81/` exists.
