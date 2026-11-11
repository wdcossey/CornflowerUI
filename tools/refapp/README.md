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
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Popups only -Grab               # the popups, with a screenshot of each, in about 4 minutes
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Demo                            # the demo page, left open in the emulator
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Probe tilt                        # one probe, with real touches (see Probes)
powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Probe pages -Record               # a probe, with a recording of the screen (see Motion)
```

Results go to `R:\cornflower-reference\refapp\<yyyyMMdd-HHmm>-dev<n>\`: one PNG and one JSON per
capture, `index.json` for the run, and `index.html`, which lays every state out dark beside light.
Device indexes are in `R:\cornflower-reference\wp81-emulator-profiles.md`.

`-Only <regex>` keeps the specimens and popups whose name matches. `-Popups also` adds the popups to a
run and `-Popups only` runs them alone; by default a run leaves them out, because with them inside
a full run the emulator connection broke at about 477 PNGs, twice. The app reads these from a
`run-options.txt` that `run.ps1` writes into the package for that run and then deletes.

`-Grab` holds each popup open for 5 s while `vm\grab-loop.ps1` saves a screenshot of the phone's
display, and `run.ps1` puts it beside the popup's dumps as `<id>__screen.png`: the popup's pixels,
at 1080 × 1920 on the reference profile (see [Popup pixels](#popup-pixels)). The loop's own shots
are kept in `frames\`, and each popup's hold, in UTC, in `held.txt`.

`-Demo` deploys the app with a page to use by hand instead of the suite: a button for each popup,
ContentDialog first, and dark and light. The app stays open in the emulator; nothing is captured.

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
  TimePickerFlyout, ComboBox open with every count of items from 3 to 10 (up to 5 open
  inline, 6 or more open the full-screen picker; below), and the page's app bar, compact and
  minimal, closed and open (below).
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
  the state: `useTransitions` is false. The motion is read by the probes, and recorded with
  `-Record` where the app can't read it ([Motion](#motion-2026-11-10-device-11-24)).
- A state is forced, not reached by input. Where `stateApplied` is false, the template has no
  state of that name and the PNG shows the rest state.
- **`RenderTargetBitmap` can't see popups on the phone,** so a popup's PNG comes from the screen (`-Grab`):
  rendering a popup's child throws `ArgumentException`, and rendering the whole window
  (`RenderAsync(null)`) leaves popups out. Moving a popup's child onto a stage does render, but
  not as the phone draws it: the content lays out again and loses its state (a list picker came
  out 1921 × 2066, a menu flyout with no items), so that was dropped.
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
| ListPickerFlyout, PickerFlyout, DatePickerFlyout, TimePickerFlyout, ComboBox with 6 to 10 items | the presenter, then a CommandBar | 0, 0, 450 × 800 | `#FF1F1F1F` / `#FFDDDDDD` |
| ComboBox with 3 to 5 items | none: it opens inline, in the control | | |

A ComboBox with 6 items or more opens a `ListPickerFlyoutPresenter`: the full-screen picker.

**Where ComboBox flips to full screen** (2026-11-09, #25): 5 items open inline and 6 open the
full-screen picker, in dark and light, on the reference profile (device 11, `20261109-2039-dev11`)
and on the 720p one (device 10, `20261109-2041-dev10`). The two profiles differ in view size
(450 × 800 and 400 × 711) and scale, so the count, not the room on screen, decides it. The
inline ones report "no popup open", which is expected.

## Probes

Behaviour that only real input shows. `run.ps1 -Probe <name>` deploys the app with
one probe from `app\Probe.cs` instead of the suite. The app lays a control out on a dark page,
writes the touches it wants as `gestures.txt` (in view pixels, window coordinates) and then
`ready.txt`. `vm\session.ps1` pulls them and `vm\touch.ps1` plays them on the emulator's display
with the mouse, which the emulator turns into touches. The app logs every pointer event, visual
state change and watched value to `probe.txt`, in milliseconds, and writes `done.txt` when the
last touch has settled. `touch.log` says where each step landed on the VM's screen.

- **Touches move the VM's real cursor** (`-TouchMode real`, the default), so the emulator has to
  be in front and the VM's mouse left alone during the run. `-TouchMode post` posts mouse
  messages to the display window instead; the emulator ignored them (2026-11-09).
- **One emulator at a time is safest.** `touch.ps1` picks the emulator by its XDE process's
  `/displayName`, because every emulator window is titled "XDE".
- **Resolution:** at the zoom that fits the VM's 1440-high screen, one screen pixel is 0.83 view
  pixels on the reference profile, so a probe can't place a touch finer than that. The app logs
  the positions it got.
- **Moves arrive in bursts about 100 ms apart.** The emulator delivers the mouse's moves to the
  phone in batches, so a probe reads a position, not a speed. Press and release times are kept.

Probes: `toggleswitch` (#25), `tilt` and `tiltgrid` (#22), and for #24 `motion`, `pages`,
`projections` and `ring` ([Motion](#motion-2026-11-10-device-11-24)). Each frame's value is read on the UI thread in
`CompositionTarget.Rendering`. That reads the animated values of the theme animations too: the
tilt's `PlaneProjection` moves every 15 ms in the log.

### ToggleSwitch: tap or drag (2026-11-09, device 11, #25)

Five runs, `20261109-2105-dev11`, `-2108`, `-2112`, `-2115` and `-2128`, 127 touches, each from
Off, pressing the knob and moving right. In the last, `touch.ps1` kept each step to its planned
time within a few milliseconds; before, its steps ran late, but the app logs the times it got,
and those are what is quoted. In view pixels on the reference profile:

- **Pressing goes straight to `Dragging`**: the Thumb's `DragStarted` fires on the press, and the
  knob's `KnobTranslateTransform.X` follows the finger one for one from the first move. There is
  no distance before the knob moves.
- **A release is a tap, and toggles, when the press moved less than about 18 view px and lasted
  less than about 1,020 ms.** Moved 17.47: tap; 18.33: not, in two runs. Lasted 993, 1,001
  and 1,022 ms: tap; 1,020, 1,021, 1,033 and 1,060: not. The two limits act on their own: a still press
  of 1,124 ms doesn't toggle, nor does a quick 20 px drag of 548 ms.
- **Otherwise the knob's position decides**: released at 28.32 it goes back Off, at 29.18 it goes
  On. The knob travels 56.5 (the template's `On` state), so the line is its halfway point, 28.25,
  as near as a 0.83 px step can tell.
- **After a tap**, `ToggleStates` goes `Dragging` > `Off` at once, and `Off` > `On` about 380 ms
  later. After a drag, `Dragging` > `On` or `Off` comes about 380 ms after the release. That is
  the knob's slide into place.
- **On the 720p profile** (device 10, 1.8 raw px per view px, `20261110-0733-dev10`, the same 32
  touches) the limits are the same in view pixels: moved 17.76 taps and 18.85 doesn't (17.47 and
  18.33 on the reference); held 984 ms taps and 1,018 doesn't; released at 27.19 goes back Off and
  at 28.34 goes On; the slide still ends about 380 ms after the release. So the 18 px limit isn't
  in raw pixels (32.0 to 33.9 raw here, 41.9 to 44.0 on the reference). In millimetres it is 2.60
  to 2.76 here (a view pixel is 0.147 mm at 312 dpi) and 2.66 to 2.79 on the reference (0.152 mm,
  about 400 dpi from its 5.5 inch diagonal): the two profiles' view pixels differ by 4%, which a
  0.83 px step can't separate, so view pixels and a physical distance both fit. View pixels are
  the simpler reading.

### Tilt: first readings (2026-11-09, device 11, for #22)

`20261109-2118-dev11`: a Button 300 × 120 view px, pressed and held for 600 ms at its centre, at
each corner and at the middle of each edge, 12 px in, then pressed at the centre and dragged 200
px down. The template root's `PlaneProjection`, read every frame:

| Pressed at (in the button) | `RotationX` | `RotationY` | `GlobalOffsetZ` |
|---|---|---|---|
| centre, 150, 60 | −0.38 | 0 | −24.45 |
| top middle, 150, 11.7 | −7.44 | 0 | −14.17 |
| bottom middle, 150, 108.3 | 6.79 | 0 | −15.12 |
| left middle, 11.7, 60 | −0.51 | 8.27 | −12.23 |
| right middle, 288.3, 60 | −0.51 | −8.27 | −12.23 |
| top left, 11.7, 11.7 | −7.65 | 8.30 | −1.80 |
| top right, 288.3, 11.7 | −7.65 | −8.30 | −1.80 |
| bottom left, 11.7, 108.3 | 6.71 | 8.28 | −3.19 |
| bottom right, 288.3, 108.3 | 6.71 | −8.28 | −3.19 |

`CenterOfRotation` is 0.5, 0.5, 0 throughout, and the root also gets a `CompositeTransform`
translating 112 down with a `GlobalOffsetY` of −112 against it. The press seemed to reach its
values in about 45 ms (three frames); the grid run below reads it closer, as a jump (see its
Timing). On release nothing moves for about 180 ms, then the tilt eases back to rest by about
345 ms. Dragged after the press, the tilt follows the finger. These are readings, not yet the
model: #22 fits them (next section).

### Tilt: the model (2026-11-09, device 11, for #22)

`20261109-2307-dev11` (`-Probe tiltgrid`): four Buttons on one page, A 120 × 60, B 300 × 120,
C 412 × 80 and D 200 × 200 view px, at 19 px in from the left and 120, 210, 360 and 470 px down.
Each was pressed and held still for 500 ms at a 5 × 5 grid of points, at 5%, 25%, 50%, 75% and
95% across and down: 100 presses. With `dx`, `dy` the press point's offset from the button's
centre as a fraction of its width and height (−0.5 to 0.5):

| Value | Rule | Fit |
|---|---|---|
| `RotationY` | −g(`dx`): a press left of centre tilts positive | over the four sizes, rms 0.08° once each button's offset (the bias below) is taken out; against the raw readings rms 0.38°, worst 1.26° (button A) |
| `RotationX` | g(`dy`): a press above centre tilts negative | |
| g(d) | 17.19° × d × (1 + 0.08 \|d\|); 17.19° is 0.3 rad | |
| `GlobalOffsetZ` | −25 × (1 − (\|`RotationX`\| + \|`RotationY`\|) / 17.19°), in view px | from the angles read: worst residual 0.002 px over 100 presses, with 17.19° as 0.3 rad exactly |
| `CenterOfRotation` | 0.5, 0.5, 0 | always |
| `RenderTransform` | a `CompositeTransform` translating by 0.8 × (the screen's centre − the button's centre), with `GlobalOffsetX` and `GlobalOffsetY` the opposite | exact for all four buttons and the first run's, and on the 720p profile (`20261110-0738-dev10`, `-Probe tilt`: translate −20, 76.44) |

- **Size doesn't matter.** The angles depend on the fractions `dx` and `dy`, not on pixels: a
  press 25% from the centre gives 4.33° to 4.45° on all four buttons. Each axis is independent:
  a corner press gives the same `RotationY` as a press at the middle of that side.
- **The depth is the angles' complement.** The constants are the Silverlight toolkit
  TiltEffect's, a 0.3 rad greatest angle and a 25 px greatest depth, and its rule: a centre
  press sinks fully and tilts nothing, and a corner press tilts fully and hardly sinks. The
  angles run a little above the toolkit's straight line, by the 0.08 term.
- **Where the button is on screen adds a little.** Measured by pressing points on opposite sides
  of the centre, the angles carry a bias that grows with the button's distance from the screen's
  centre, as if the press were a little further from it:

  | Button (centre, view px) | `RotationX` bias | `RotationY` bias |
  |---|---|---|
  | A (79, 150), far up and left | −0.83° | +0.25° |
  | B (169, 270) | −0.35° | +0.07° |
  | C (225, 400), at the screen's centre | 0.00° | 0.00° |
  | D (119, 570), below and left | +0.28° | +0.18° |

  It is under 1° everywhere measured, and goes with the perspective translation above. #65 models
  it (an eye 1,055 view px away, placed 0.8 of the way from the control's centre to the screen's);
  a test of the rule above alone needs a tolerance of about 1.3° away from the screen's centre, or
  the bias taken out.
- **Timing.** The press is a jump: the first frame after the press already shows 96% to 100% of
  the held values (99% on average, at 30 to 40 ms, the first frame the emulator draws). On release
  the values hold for about 180 ms, then ease back to rest over about 195 ms: a sine ease-out fits
  1,199 frames with an rms of 0.03 of the full value (quadratic 0.036, linear 0.037, cubic 0.059).
  Every press was within 1% of rest by 335 to 372 ms.
- **A tap at the very edge clicks** (`-Probe tiltedge`, `20261110-0822-dev11`): a Button 300 × 120,
  tapped for 150 ms at the middle of each edge, 0.84, 1.7 and 3.35 view px inside it (the nearest the
  cursor gets to 1, 2 and 3), three times each: all 36 taps clicked. The tilt sinks the edge away
  from the finger, but the phone still counts the press as on the button.
- **On the 720p profile** (`20261110-0851-dev10`, `-Probe tiltgrid`, C narrowed to 362 to fit the
  400 wide view; 100 presses, no errors): the same rule fits within 0.08° once each button's offset
  is taken out (0.33° raw, worst 1.04°), the depth within 0.002 px. The offsets are the bias again,
  for the buttons' places on a smaller screen (A −0.72° on `RotationX`, D +0.35°): #65 can check
  whether its eye distance scales with the screen.

## Motion (2026-11-10, device 11, #24)

How the theme moves: the ToggleSwitch's slide, the flyouts and pickers opening and closing, the
page transitions and the ProgressRing. Two ways of reading it:

- **In the app, every frame** (`-Probe motion`, `ring`): the probes watch a control's tree, and every
  open popup's, from `CompositionTarget.Rendering`, logging each element's opacity, render
  transform, projection and clip whenever they change. This reads a template's own storyboards (the
  MenuFlyout's, the ProgressRing's), as it read the tilt.
  Each popup is labelled `popup<n>` in the order it was first seen, so it keeps its label when
  another closes (each opening is a new popup, so the numbers climb through a run). A tree over 400
  elements is read up to the cut, and its readings past the cut carry over rather than read as rest;
  the log says so once for each tree.
- **On the screen** (`-Record`): the theme *transitions* (`PopupThemeTransition`,
  `NavigationThemeTransition`, the ToggleSwitch's `RepositionThemeAnimation`) run in the compositor
  and change nothing the app can read: their trees stay at rest every frame. `-Record` has
  `vm\session.ps1` record the emulator's display with `ffmpeg gdigrab` (lossless, each frame at the
  time it was grabbed), from before the probe's one tap, which starts its sequence, until it is done.
  It grabs about 40 frames a second (25 ms apart, ±10 ms), at 0.83 view px a pixel. The frames were
  measured with the scripts in [`tools/motion`](../motion/README.md), which says which numbers they reproduce.

**The emulator animates on a 64 Hz clock.** Every value moves in steps of 15.625 ms: the MenuFlyout's
`ScaleX` goes up by 0.041 a frame, 380 ms in 24.3 steps. An animation shows the same values in every
run, so the distinct values of many runs, in order, are its steps, one per 15.625 ms, at a finer
timing than the recording's.

**A PlaneProjection's camera** (`-Probe projections`, `20261110-0756-dev11`): a full-window panel given
known projections (`RotationY` ±10°, ±30°, ±60° about `CenterOfRotationX` 0, 0.5 and −0.25, and
`GlobalOffsetZ` ±100) fits a pinhole 999 view px in front of the screen, looking at the element's
centre (224, 400), within 0.53 px over the 13 states that stay on the screen. A positive `RotationY`
brings the right side nearer. The page transitions below are solved with it.

### ToggleSwitch: the knob's slide

The slide is the template's `RepositionThemeAnimation`: `KnobTranslateTransform.X` jumps to 56.5 at
once, and `ToggleStates` reaches `On` or `Off` about 380 ms later, when the slide is over (all of
#58's runs, and on the 720p profile). From the recording (`20261110-0819-dev11`, 20 slides, the
switch set from code): the knob travels 56.56 view px, the same way in both directions, through the
same positions every time. The 21 distinct positions, one per 15.625 ms step, fit the
`KeySpline` 0.1,0.9 0.2,1 over 370 ms, the first frame one step in, within 0.001 of the travel
(0.06 px); an exponential ease-out (k 6 to 8) fits ten times worse, the power ease-outs twenty.
370 ms and a step or two is the 380 ms to the state change.

### Flyouts, pickers and the dialog

`20261110-0819-dev11` (`-Probe motion -Record`): each opened from code four times, held 1.2 s, and
closed with `Hide()` (the ComboBoxes with `IsDropDownOpen`). The app's values show none of it, except
the MenuFlyout's template. Times are from the first frame that changed until the screen is 50%, 90%
and 99% of the way to its final look (mean absolute difference), the median of four runs, ±25 ms;
"top to bottom" gives when the top and the bottom 100 px band are half done.

| Popup | Opening | ms | Closing | ms |
|---|---|---|---|---|
| Flyout, top | swings down from its top edge (its bottom edge at 46, 55, 69, then 77 of 78 px) | 50 / 100 / 142 | gone in one frame, about 100 ms after `Hide()` | 0 |
| Flyout, full | sweeps in top to bottom (74 to 208) | 141 / 208 / 208 | sweeps out top to bottom (117 to 275) | 200 / 291 / 334 |
| MenuFlyout | the template's storyboard (below) | 483 / 516 / 516 | the menu goes in one frame; the full-window layer under it about 300 ms later | 0 / 0 / 317 |
| ListPickerFlyout | the header, then each item, swing in one after another, then the background fills downwards (75 to 367) | 209 / 325 / 367 | top to bottom (100 to 375) | 242 / 375 / 425 |
| DatePickerFlyout | the three columns and header (83 to 200); the last 1%, the app bar, by 400 | 117 / 200 / 400 | 117 to 325 | 150 / 242 / 358 |
| TimePickerFlyout | as the date picker (100 to 183; 408) | 125 / 183 / 408 | 125 to 333 | 167 / 234 / 358 |
| ComboBox, 6 items (the full-screen picker) | as the ListPickerFlyout (109 to 342) | 200 / 333 / 342 | top to bottom (116 to 408) | 291 / 408 / 458 |
| ComboBox, 5 items (inline) | opens in one frame, about 140 ms after the call; in the app, the items' `TranslateY` eases from −8.5 to 0 over 4 frames | 142 / 142 / 208 | closes in one frame | 0 |
| ContentDialog | about 200 ms | 34 / 192 / 209 | gone in one frame, about 125 ms after `Hide()` | 0 |

**MenuFlyout, from the app's values** (`20261110-0745-dev11` and `-0753`): `OuterBorder`'s `ScaleX` goes
0 to 1 linearly over 380 ms, origin (0.5, 0); then `InnerBorder`'s `ScaleY` 0 to 1 linearly over
120 ms. That is the template's `TopPortrait` storyboard, run as written. Its close has no storyboard.

### Page transitions

`20261110-0748-dev11` and `20261110-0810-dev11` (`-Probe pages -Record`): a Frame with a
`NavigationThemeTransition` (what the root frame has) navigates between two pages, a white and a blue
sheet inset 19, 60. The second run did each transition eight times. The sheet's outline in each frame
is solved for the page's rotation with the camera above.

- **The default transition is the turnstile**, and `CommonNavigationTransitionInfo` is the same. Only
  the page coming in moves: the page going out disappears at once. It turns about a vertical axis
  44 px left of the page's left edge and 99 px behind the screen (a PlaneProjection with
  `CenterOfRotationX` about −0.1 and `CenterOfRotationZ` −100): with that one axis, every frame of
  nine forward turns fits within 0.24 px. Forward it comes in from a negative `RotationY` (the right
  side away), back from a positive one (the right side nearer).
- **Its angle falls by a factor of 0.803 every 15.625 ms step** (a time constant of 71 ms), both
  ways: an exponential ease-out. Back, an `ExponentialEase` (exponent 6, ease-out) from +42.0° over
  430 ms, starting on the first frame shown, fits the 16 distinct angles within 0.07°. Forward, the
  first frame shown is at −67.0° (in 4 of 9 runs; else −53.8° or −43.1°), and an exponential
  ease-out from there ending 391 ms later (exponent 5.5) fits the 19 distinct angles within 0.05°;
  exponent 6 over 430 ms fits only with a start far off the screen. It is within 1° of rest after
  about 300 ms.
- **`SlideNavigationTransitionInfo`**: forward, the new page rises from below, first seen 200 px down;
  its offset falls by 0.765 a step (58 ms): an `ExponentialEase` (exponent 6, ease-out) from 200 px
  over 350 ms, starting on the first frame shown, fits the 16 distinct offsets within 0.05 px. Back, the page going out slides down, faster each step (×1.4 to 1.46, an ease-in)
  for about 220 ms, to about 140 to 200 px, and then the page under it shows at once.
- **`ContinuumNavigationTransitionInfo`**: forward, the old page fades out in two frames (about 60 ms),
  the screen stays black for 60 to 100 ms, then the new page grows about the screen's centre from a
  scale of 0.94 to 1 in about 280 ms. Back, the page going out slides down as in the slide's back,
  then the page under it fades in: what is left of its opacity falls by about 0.84 a step, over
  about 340 ms.

### ProgressRing: Active

The suite's forced `Active` state captures no dots: they show only while the storyboard runs. Read
every frame instead (`20261110-0829-dev11`, `-Probe ring`, 6 s, with a recording): a ring at its
default size, 57.5 view px, shows five dots, and one 120 px wide shows six. Each dot's `Canvas`
rotates and its `Ellipse` hides and shows as the template's storyboard says, the dots 150 to 167 ms
apart (the template's `BeginTime`s are 0.167 s apart). One cycle takes 4.13 s by the app's clock
(nine cycles), and a dot hides 3.10 s into it. The template's keyframes give 4.31 s (the last dot's
`BeginTime` and its 3.47 s) and 3.22 s: the same shape, 4% faster. Whether that is the emulator or
the storyboard's clock is open; #28 should compare its own timing with the template's keyframes and
with this log.

## The page's app bar (2026-11-09, device 11, #23)

`20261109-2124-dev11` and `20261109-2130-dev11` (`-Popups only -Only BottomAppBar -Grab`), the
same to the pixel row: a `CommandBar` set as the page's
`BottomAppBar`, with three primary commands (add; favorite, a checked toggle; delete, disabled)
and two secondary ones, in four variants, dark and light, each with a screenshot.

- **The shell draws it, not the page.** The `CommandBar` sits in a popup whose tree is dumped,
  but the dump is the same open or closed and doesn't match the pixels (its bar starts at 742.5,
  the drawn one at 737.08). Only the screenshots are the reference.
- **`IsOpen` set as the bar is attached is ignored.** The specimen opens it 400 ms after the
  bar's `Loaded`, records an error if `Opened` never fires, and on close detaches the bar only
  once it has `Closed`, since taking a popup away mid-animation has killed the app before.

Read from the screenshots, in view pixels (raw ÷ 2.4):

| Variant | Bar's top | Bar's height | Shows |
|---|---|---|---|
| Compact, closed | 737.08 | 62.92 | the three buttons' circles, no labels; the ellipsis at the top right |
| Minimal, closed | 773.75 | 26.25 | the ellipsis only |
| Compact or minimal, open | 514.58 | 285.42 | the circles with labels below, then the secondary commands as a list in large text |

The bar is `#FF1F1F1F` dark and `#FFDDDDDD` light, as the pickers' bars are. The ellipsis's dots
start 25 raw px (10.4 view px) below the bar's top in every variant. The checked toggle is a
filled circle; the disabled one is grey, its label too.

## Popup pixels

`RenderTargetBitmap` can't see popups, so `-Grab` takes the phone's display from the emulator.
What was tried on 2026-11-08, on device 11 (1080 × 1920) with the VM's screen at 2560 × 1440:

- **A grab of the emulator's window** (`OPWindowClass`, "Output Painter DX Child Window", with
  `PrintWindow`) is only as sharp as the emulator's zoom. At the zoom that fits the screen the
  window is 551 × 979, so the grab is scaled. At 100% it is 1080 × 1920, but the emulator doesn't
  draw the part of the window below the screen: every row past about 1406 came out blank, which
  is where the pickers' app bars are. The zoom also goes back to fitting when the emulator restarts.
  A hidden "Output Painter Child Window" is 1080 × 1920 too, but holds no frame of its own.
- **The emulator's own Screenshot tool** (Additional Tools > Screenshot > Capture, then Save...)
  reads the whole display at 1080 × 1920 whatever the zoom. `-Grab` uses it:
  `vm\grab-loop.ps1` watches the window, and each time the picture has changed and then held
  still for 1.2 s, presses Capture and Save... through UI Automation and fills the save dialog
  with Win32 messages (`vm\XdeWin32.cs`). Walking the modal file dialog through UI Automation
  was slow enough to hang. If Additional Tools is closed, as after a restart, it presses the
  toolbar's `>>` with posted mouse messages: the toolbar draws its own buttons, so UI Automation
  doesn't see them. `vm\xde-tree.ps1` lists the emulator's UI Automation tree, to find controls.

Two runs of `-Popups only -Grab`, the second from a cold emulator (`20261108-2248-dev11`,
`20261108-2254-dev11`), gave 24 shots each, all 1080 × 1920. Compared, three shots were identical
to the pixel and one differed in 7 pixels by 1 level, in the anti-aliasing of the app bar's icons.
The ComboBox with 3 items, which opens inline, gets a shot too: the hold doesn't depend on a popup.

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
