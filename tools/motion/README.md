# motion: the scripts behind the measured motion

Python scripts that turn the reference app's motion runs (recordings of the emulator's screen, and the probes'
per-frame logs) into the numbers in [`tools/refapp/README.md`](../refapp/README.md), "Motion" and the tilt sections,
and in [ADR 0005](../../docs/decisions/0005-decisions-of-2026-11-09-and-10.md). They were written for #66 and kept
outside the repo then; this folder is them tidied, each taking a run folder, so every fit can be run again.

Python 3 standard library only: nothing to install. `decode.py` also needs `ffmpeg`.

## Running them

A run folder is what `tools/refapp/run.ps1` pulls back into the reference folder, outside this repo, e.g.
`R:\cornflower-reference\refapp\20261110-0810-dev11`. The scripts only read it. A recorded run (`-Record`) is read
as raw gray frames: decode it once, which writes `<run>\analysis\record.gray` (2.5 GB for a two-minute run):

```sh
python -I tools/motion/decode.py <run>
python -I tools/motion/turnstile.py <run>
```

To reproduce the published popup table, give `popups.py` the lag the 2026-11-10 table used:

```sh
python -I tools/motion/popups.py R:/cornflower-reference/refapp/20261110-0819-dev11 --lag 29.15
```

That 29.15 s (recording time = app time − 29.15 s) was typed in by hand on 2026-11-10. Without `--lag`, the script
uses its own lag, the median over the knob's 20 moves, 29.158 s, and says so; that 8 ms moves the ListPickerFlyout's
close row (see the table below).

The other scripts find the decoded frames in `<run>\analysis\` by their size (any `*.gray` there of the recording's
width × height × frames), or take `--gray <file>`. The 2026-11-10 runs were decoded on the reference VM with the
command `decode.py` runs (`ffmpeg -i record.mkv -vf format=gray -fps_mode passthrough -f rawvideo`), and their
frames are already in their `analysis\` folders; `decode.py` itself has not been run here, as this PC has no ffmpeg.

| Script | Reads | Prints |
|---|---|---|
| `decode.py` | a recorded run | writes `analysis\record.gray` |
| `camera.py` | a `-Probe projections -Record` run | each held projection's outline, and the pinhole camera that fits them |
| `turnstile.py` | a `-Probe pages -Record` run | the turnstile's axis, each forward turn's first angle, the distinct angles of the forward and back turns and the curves fitted to them. The axis search takes a few minutes; `--no-search` skips it, `--axis <x>,<z>` sets the axis the angles are solved about (−44,−99) |
| `slide.py` | a `-Probe pages -Record` run | the slide's offsets in and out and their fits; the continuum's scale in and fade back. `--lag <s>` gives the lag (27.815 for `20261110-0810-dev11`) instead of finding it from the turnstile turns |
| `knob.py` | a `-Probe motion -Record` run | the ToggleSwitch knob's positions through its slide, and KeySplines and ease-outs fitted to them |
| `popups.py` | a `-Probe motion -Record` run | each popup's opening and closing times (50%, 90%, 99%, and by band), and the top Flyout's bottom edge as it opens |
| `menuflyout.py` | `-Probe motion` runs | the MenuFlyout's storyboard steps, from the app's log |
| `ring.py` | a `-Probe ring` run | the ProgressRing's dots and cycle, from the app's log |
| `ringsize.py` | a `-Probe ringsize` run | the ProgressRing's `SizeStates` threshold, and the animation clock against the app's stopwatch, from the app's log |
| `toggle.py` | `-Probe toggleswitch` runs | each press, and the tap limits |
| `tiltcurve.py` | a `-Probe tiltgrid` run | #60's tilt curve against each press |
| `tiltrelease.py` | `-Probe tilt`, `tiltedge` and `tiltgrid` runs | the tilt's release frame by frame, which frame after the release it starts on, and its hold and return in animation time: a sine and a `KeySpline` fitted; #60's stopwatch fit beside them. A few seconds a run |
| `motionlib.py` | | what they share: the run, the frame measurements, the projection, the easing curves and the fits |
| `selftest.py` | nothing | checks the measurements and fits on synthetic data with known answers |

Each script's docstring says what it measures and how. Three things they share:

- **Pairing the recording with the log.** A transition in the recording is paired with the probe step that started
  it by time: each starts a fixed lag after its step (`motionlib.match_steps`). A step with no transition near that
  lag, or two steps on one transition, stops the script with the steps named, rather than pair the rest one off.
  `camera.py`, `turnstile.py`, `slide.py` and `popups.py` (through the knob) pair this way.

- **The 64 Hz clock.** An animation's values move one step per 15.625 ms, the same steps every run, so the values
  of many runs, each counted once, are its steps in order. The fits work on those *distinct values*: a value a step,
  with a step skipped where the ratio to the one before says no run showed it (`motionlib.tick_numbers`). That
  ratio test assumes an exponential. The knob's KeySpline shrinks faster at its end, where the test would see gaps
  that aren't there, so `knob.py` takes its positions as consecutive and then checks that on the fitted curve: each
  position over 1% of the travel lands 0.53 to 1.32 ticks after the one before, where a missing tick would land two.
- **The camera.** `camera.py` finds PlaneProjection's eye 999 view px from the screen, looking at (224, 400). The
  turnstile was solved, on 2026-11-10, with 1000 and (225, 400); `turnstile.py` keeps those, so its numbers are the
  published ones. The difference moves the angles by less than the fit's residual.

## Checking them

```sh
python -I tools/motion/selftest.py
```

It builds data from known curves, a known camera and a known outline, runs the code the scripts use on it, and
asserts the answer comes back: the edge crossings, the sheet's outline, the easings, the distinct values and their
ticks (and a true median ratio), the exponential, ExponentialEase and KeySpline fits, the camera fit (on and off its
grid) and the turnstile's angle and axis, the knob's missing-tick check, the arguments, and reading a run folder.
It also checks that the scripts stop where they should: a transition missing or out of step with the log, a first
frame that isn't at rest, an axis with no frame to fit; and that they carry on past a tilt press with nothing
logged and a MenuFlyout rise cut off by the end of the log. It is not run by CI, which builds and tests the .NET
solution only.

## What they reproduce

Each published number, run again on its original run on 2026-11-11. The runs are in
`R:\cornflower-reference\refapp\`. "README" is `tools/refapp/README.md`; "ADR" is ADR 0005.

| Number | Cited in | Script | Run | Reproduced |
|---|---|---|---|---|
| PlaneProjection's camera: 999 view px, looking at (224, 400), within 0.53 px over 13 states | README "Motion", ADR | `camera.py` | `20261110-0756-dev11` | Yes: 999, 224.00, rms 0.526 px over 13 states |
| The 64 Hz clock: the MenuFlyout's `ScaleX` up 0.041 a step, 380 ms in 24.3 steps | README "Motion", ADR | `menuflyout.py` | `20261110-0745-dev11`, `-0753-dev11` | Yes: +0.0411 a step, 24.3 steps, 380 ms at 15.625 ms a step. The 15.625 ms is the clock's, not measured here: the app's own frames over the rise are 15.4 ms apart on average |
| MenuFlyout: `ScaleX` 0 to 1 linearly over 380 ms, origin (0.5, 0); then `ScaleY` linearly over 120 ms | README "Motion", ADR | `menuflyout.py` | `20261110-0745-dev11`, `-0753-dev11` | Yes: both within 0.0005 of a straight line; 380 ms and 120 ms (7.7 steps); origin (0.5, 0) |
| Turnstile: an axis 44 px left of the page and 99 px behind the screen; nine forward turns within 0.24 px | README, ADR | `turnstile.py` | `20261110-0810-dev11` | Yes: fitted x −43.4, z −98.8 (rms 0.236 px); at −44, −99, rms 0.241 px |
| Turnstile: the angle falls ×0.803 a step, a time constant of 71 ms | README | `turnstile.py` | `20261110-0810-dev11` | Yes: 0.8031, 71.3 ms, forward and back |
| Turnstile back: `ExponentialEase` (6) from +42.0° over 430 ms, from the first frame shown, the 16 distinct angles within 0.07° | README, ADR | `turnstile.py` | `20261110-0810-dev11` | Yes: from 41.97 over 430 ms, 0.0 steps in, rms 0.072° over 16 |
| Turnstile forward: the first frame at −67.0° in 4 of 9 runs, else −53.8° or −43.1° | README, ADR, gap register B10 | `turnstile.py` | `20261110-0810-dev11` | Yes: −67.0 in 4, −43.1 in 3, −53.8 in 2 |
| Turnstile forward: an exponential ease-out (5.5) from −67.0° ending 391 ms later, the 19 distinct angles within 0.05° | README, ADR | `turnstile.py` | `20261110-0810-dev11` | Yes: from −67.02, ending at tick 25 (391 ms), exponent 5.48, rms 0.050° over 19 |
| Turnstile forward: exponent 6 over 430 ms fits only with a start far off the screen | README, ADR | `turnstile.py` | `20261110-0810-dev11` | Yes: from −147.2°, 3.6 steps before the first frame, rms 0.063° |
| Slide in: first seen 200 px down; ×0.765 a step (58 ms); `ExponentialEase` (6) from 200 px over 350 ms, the 16 distinct offsets within 0.05 px | README, ADR | `slide.py` | `20261110-0810-dev11` | Yes: 200.0 in 3 of 4 runs (152.8 in one); ×0.7648 (58.3 ms); from 199.95 over 350 ms, rms 0.047 px over 16. The 2026-11-10 list was picked by hand; the script's cutoff, 2 px, gives the same 16 |
| Slide out: faster each step, ×1.4 to 1.46, for about 220 ms, to about 140 to 200 px | README | `slide.py` | `20261110-0810-dev11` | **In part.** ×1.43 a step (0.700): yes. About 220 ms: 188 ms here (the hand-picked list of 2026-11-10 had 1.0 px and not 4.2, and ended at 219 ms). 140 to 200 px: the last offsets seen are 64.6 to 137.3 px |
| Continuum forward: the old page fades in two frames (about 60 ms), black for 60 to 100 ms, then the new page grows from a scale of 0.94 to 1 in about 280 ms | README, ADR | `slide.py` | `20261110-0810-dev11` | **In part.** Scale 0.941 to 0.963 at the first frame, 281 to 312 ms to rest: yes. The fade: 1 or 2 frames. Black: 94 to 125 ms from the first black frame to the first of the new page, an upper bound at about 31 ms a frame |
| Continuum back: what is left of the fade falls by about 0.84 a step, over about 340 ms | README | `slide.py` | `20261110-0810-dev11` | Yes: ×0.8395, ending at 344 ms |
| ToggleSwitch knob: travels 56.56 view px, the same way both directions; 21 distinct positions fit `KeySpline` 0.1,0.9 0.2,1 over 370 ms, the first frame one step in, within 0.001 of the travel (0.06 px); exponential ease-outs (6 to 8) ten times worse, power ease-outs twenty | README, ADR | `knob.py` | `20261110-0819-dev11` | Yes: 56.56; going On and Off through the same positions, within 0.05 px down to the last 0.6 px; 21 positions, the same as 2026-11-10's; 370 ms, 1.0 step in, rms 0.0010 (0.06 px); `ExponentialEase` 6 and 8 0.0109 and 0.0102; power ease-outs 0.0195 to 0.0253 |
| Popups: each opening's and closing's 50 / 90 / 99% times, and the 100 px bands' ("74 to 208" and the like) | README "Flyouts, pickers and the dialog" | `popups.py` | `20261110-0819-dev11` | **With `--lag 29.15`, yes**: all 18 rows equal 2026-11-10's. That lag, recording time = app time − 29.15 s, was typed in then; the script's own, the median over the knob's 20 moves, is 29.158 s, and with it every row is the same but the ListPickerFlyout's close: 284 / 392 / 434 ms, not 242 / 375 / 425. That row moves with an 8 ms shift of its window, so it is less certain than ±25 ms |
| The top Flyout's bottom edge as it opens: 46, 55, 69, then 77 of 78 px | README | `popups.py` | `20261110-0819-dev11` | Yes: 46.26, 54.60, 68.60, 76.57, then 77.93 |
| ProgressRing `Active`: five dots at the default size (57.5), six at 120 px; 150 to 167 ms apart; a 4.13 s cycle (nine cycles); a dot hides 3.10 s into it | README, ADR | `ring.py` | `20261110-0829-dev11` | **In part.** Five and six dots, 4.132 s over nine cycles: yes (and 4.132 s on `20261110-0846-dev10`, the 720p re-run). Apart: 150 to 165 ms in the first cycle, but 129 to 167 ms over all nine. Hides: 3.090 s after the dot shows again (the median of 47), not 3.10 |
| ProgressRing `SizeStates`: `Large` when the smaller laid-out side is over 60 view px (60 `Small`, 60.417 `Large`), no hysteresis, the smaller side when not square; `SixthCircle` shown exactly when `Large` | README "ProgressRing: the size threshold and the clock", ADR | `ringsize.py` | `20261111-2316-dev11` | Yes: `Small` up to 60, `Large` from 60.417, all 208 steps agree |
| The clock: animation time 15.625 ms a frame; frames 15.0 ms apart (mean 15.017); ratio 1.0405 (step over mean frame), 1.0416 (fit); the cycle wraps at 4,296.9 ms of its own time; a 4 s storyboard's `Completed` at 3,856.5, 3,862.6 and 3,861.1 ms | README "Motion" and "ProgressRing: the size threshold and the clock", ADR | `ringsize.py` | `20261111-2316-dev11` | Yes: all 663 moves 15.625 ms; the rest as published |
| ToggleSwitch on the 720p profile: moved 17.76 taps, 18.85 doesn't (17.47 and 18.33 on the reference); held 984 ms taps, 1,018 doesn't; `ToggleStates` changes about 380 ms after the release | README, ADR | `toggle.py` | `20261110-0733-dev10`, `20261109-2128-dev11` | Yes: 17.76 / 18.85 and 984 / 1018 ms; 17.47 / 18.33 and 993 / 1019 ms on the reference; the state changes 374 to 391 ms after the release |
| #60's tilt curve on the reference profile: rms 0.08° once each button's offset is out, 0.38° raw, worst 1.26°; the depth within 0.002 px | README "Tilt: the model", ADR | `tiltcurve.py` | `20261109-2307-dev11` | Yes: 0.083°, 0.379°, 1.256°; depth worst 0.0015 px |
| #60's tilt curve on the 720p profile: within 0.08° once each button's offset is out, 0.33° raw, worst 1.04°; the depth within 0.002 px | README | `tiltcurve.py` | `20261110-0851-dev10` | Yes: 0.076°, 0.326°, 1.042°; depth worst 0.0017 px |
| #60's release by the stopwatch: held 180 ms (fit 178), then a sine over 195 ms (fit 196), rms 0.03 over 1,199 frames | README "Tilt: the model", ADR (until 2026-11-11) | `tiltrelease.py` | `20261109-2307-dev11` | Yes: 177.9 and 196.1 ms, rms 0.032 over 1,188 frames (one press of 11 frames left out, its first frame unread) |
| The release in frames: the same fractions every release; the first change on the 13th frame after the release; a hold of 182.6 ms, then `KeySpline` 0.1,0.25 0.75,0.9 over 167.0 ms, within 0.00075; a sine out by 0.027 | README "Tilt: the model", ADR, `TiltModel` | `tiltrelease.py` | the five tilt runs: `20261109-2307-dev11`, `20261109-2118-dev11`, `20261110-0738-dev10`, `20261110-0822-dev11`, `20261110-0851-dev10` | Yes: 252 presses (2 left out of the medians), spread 0.0002 at most; 12.40 frames' spacing to the first change (median); 182.6 and 167.0 ms, rms 0.00024, worst 0.00075; the sine 179.2 and 202.6 ms, worst 0.0270 |

### Published and reproduced, where they differ

Not corrected here; the docs are corrected separately.

| Number | Published | Reproduced | Why they differ |
|---|---|---|---|
| Slide out, how long it accelerates | about 220 ms | 188 ms (the eased fit ends at tick 12) | 2026-11-10's offsets were picked by hand, with 1.0 px and without 4.2; the script takes every offset of 2 px or more |
| Slide out, how far the page goes before the one under it shows | about 140 to 200 px | 64.6 to 137.3 px, the last offset seen in each run | |
| Continuum forward, the black | 60 to 100 ms | 94 to 125 ms | measured from the first black frame to the first of the new page, so an upper bound, at about 31 ms a frame |
| ProgressRing, the dots apart | 150 to 167 ms | 150 to 165 ms in the first cycle; 129 to 167 ms over all nine | the published range is the first cycle's |
| ProgressRing, when a dot hides | 3.10 s into the cycle | 3.090 s after the dot shows again (median of 47) | |
| Popups, ListPickerFlyout close | 242 / 375 / 425 ms | 284 / 392 / 434 ms without `--lag`; the published values with `--lag 29.15` | the lag (29.158 s from the knob, 29.15 s typed in on 2026-11-10) moves this row's window by 8 ms |

Not covered, with no script here:

- **The tilt's bias table** (−0.83° and so on, from presses on opposite sides of the centre): measured for #60, not by these
  scripts. `tiltcurve.py`'s per-button mean residuals are the same bias by another method (−0.88° for A).
- **The inline ComboBox's items** (`TranslateY` from −8.5 to 0 over 4 frames) and the recorder's rate (about 40
  frames a second): read from the log and `frames.csv` by eye.

## What was dropped

The 2026-11-10 scratch folder also had scripts whose numbers weren't published, or that the ones here replace:
`frames.py` and `sheet.py` (a per-frame summary and contact sheets, for looking), `quad.py` and `quad2.py` (the first
outline and rotation solves, replaced by `motionlib.outline` and the turnstile's axis), `transfit.py` (a free centre
and depth per frame, which led to the fixed axis), `fit.py`, `angfit.py` and `ticks.py` (whole-run curve fits and
the clock alignment, before the distinct-value fits), `bands.py` and `batch.py` (earlier versions of the popup
timing), `bezfit.py` (a free KeySpline fit, stopped before it finished) and the swivel angle in `swivel.py` (only
the edge was published; the per-row swivel curves are gap register B11).
