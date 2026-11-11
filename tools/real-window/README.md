# tools/real-window

The desktop sample in a real window, with real input. Headless tests can't see everything: in
Avonic, the first run of its version of this found a control bug that 1,486 tests had missed.

**It takes over the cursor and focus for a few seconds a page, so ask first.** It puts the cursor
back and closes only the window it opened.

```powershell
powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
```

It builds the sample first, from the repo so that `global.json` picks the SDK, as
`tools/android/run.ps1` does: a stale build runs the old code, and fails checks the new code passes.
`-Configuration Release` builds and runs Release instead of Debug; only a Debug build has the
performance line to check. `-NoBuild` runs the build as it is. **Close the sample first** if it's
open: it locks its files, so the script stops rather than build. Frames go to a
new folder in the temp folder for each run, which it prints, unless you pass `-Out`; the sample's
standard output goes there too, as `stdout.txt`. It exits 1 if a check fails.

**If something else keeps the focus, it stops sending input** rather than type into another app,
and fails those checks. It stops at once if the window doesn't fit on the screen.

## What it does

- **Every page on the list,** with nothing to add for a new control: opens it with a real click,
  checks that the shell writes the page's ready line under its title, saves a frame, fails on a
  button with no size and, in a Debug build, on a missing performance line, then clicks Back (in the
  rail beside the page) and checks the list is back. A page below the fold of a long list is
  opened through UI Automation instead: a click never goes outside the window. The ready line and the
  performance line (which the shell shows up the rail, beside the page) are looked for under the page's
  catalogue title, from its `[SamplePage]`. Titles are matched exactly, case and all.
- **The page's size:** the theme page's ready line,
  `CORNFLOWER ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>`,
  has to name the theme page and report a 450 × 800 page (`page=`, the page's own size, so chrome
  taking space from it shows; `view=` is the window's client area, 40 wider for Back's rail), to the
  pixel. Windows gives a window whole pixels, so at 125 % a 450 page can come out at 449.6. Only
  `page=`, `scaling=` and `title=` are read.
- **Alt+Left,** Avalonia's back on desktop, from a page to the list.
- **Resizing:** the window has to say it can be resized, and the page is laid out wider once the
  window is.

A new interactive control should get a section of its own, as Avonic's controls have in its copy.

**Look at the frames as well.** Most of what's wrong with a page isn't something it checks.

## What it can't tell you

- **Scale** is whatever this PC's display is set to.
- **Windows only.** CI doesn't run it, as CI has no display.
- **Not touch.** Avonic's copy injects touch through Windows; it comes back here with the first
  control that takes a tap. The phone is the real touch test (`tools/android`).

The frames come from the screen, not `PrintWindow`, which offsets a window by its invisible
border, and that looks like a bug in the app. They are the window's client area: UI Automation
reports that as the window's bounds.

Ported from Avonic's `tools/real-window/drive.ps1` (`wdcossey/Avonic`, `v2` at `edf87b8`): the
window, the input, the frames and the every-page loop. New here: the ready line's size, Alt+Left,
resizing, and asking UI Automation for the focus when Windows won't give it to the script.
