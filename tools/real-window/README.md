# tools/real-window

The desktop sample in a real window, with real input. Headless tests can't see everything: in
Avonic, the first run of its version of this found a control bug that 1,486 tests had missed.

**It takes over the cursor and focus for a few seconds a page, so ask first.** It puts the cursor
back and closes only the window it opened.

```powershell
dotnet build samples/Cornflower.Sample.Desktop
powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
```

A Debug build also checks the performance line. For a Release build, pass `-Exe`. Frames go to a
new folder in the temp folder for each run, which it prints, unless you pass `-Out`; the sample's
standard output goes there too, as `stdout.txt`. It exits 1 if a check fails.

**If something else keeps the focus, it stops sending input** rather than type into another app,
and fails those checks. It stops at once if the window doesn't fit on the screen.

## What it does

- **Every page on the list,** with nothing to add for a new control: opens it with a real click,
  saves a frame, fails on a button with no size and, in a Debug build, on a missing performance
  line, then clicks Back and checks the list is back.
- **The window's size:** the theme page's `CORNFLOWER ready` line has to report a 450 × 800 view,
  to the pixel. Windows gives a window whole pixels, so at 125 % it is 562 × 1000, a 449.6 view.
- **Alt+Left,** Avalonia's back on desktop, from a page to the list.
- **Resizing:** the window has to say it can be resized, and the page reports a wider view once
  it's made wider.

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
