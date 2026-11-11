# tools/android

The device run: the sample on a real phone, over ADB (ADR 0002, "Platforms";
[`PROCESS.md`](../../docs/PROCESS.md#4-review), "The device run").

```powershell
powershell -ExecutionPolicy Bypass -File tools\android\run.ps1             # build, install, start, capture
powershell -ExecutionPolicy Bypass -File tools\android\run.ps1 -NoBuild    # reuse the last build
powershell -ExecutionPolicy Bypass -File tools\android\run.ps1 -Page theme # open a page of the catalogue
```

The phone shows the sample's catalogue, as the desktop does, with no rail for Back: the phone's
back button or gesture is Back, to the list, and from the list out of the app. `-Page <title>`
opens a page of the catalogue at start, by its title in any case (quote a title with a space; a
title with a double quote is refused, as PowerShell 5.1 can't pass one to adb intact); it is
passed to the app as the intent's `page` extra (`am start --es page <title>`), which
`MainActivity` gives `App.StartPage` before Avalonia makes the view (the app fails if Avalonia
ever made it first, rather than ignore the page). Without it, the app opens the first page,
`theme`, as a start from the launcher does. A title the catalogue doesn't have stops the app at
start, naming the titles it has, and the script stops with the app's crash report.

`run.ps1`:

1. Checks that `adb devices` lists exactly one phone, and reads its model, Android version, screen
   size and density.
2. Builds `samples/Cornflower.Sample.Android` (Release by default), and installs the signed APK.
3. Wakes the screen. A phone with a secure lock has to be unlocked by hand: the script says so and
   waits (`-UnlockTimeoutSeconds`, 60 by default), then waits for the screen to be fully awake.
4. Goes to the home screen, stops the app, and starts it cold with `am start -W -S` (and the `page`
   extra, given `-Page`), which reports
   the time Android measured from the intent to the activity's first frame (`TotalTime`). Going
   home first matters: a screen waking from doze can bring the app's last task back by itself,
   and then `am start` only delivers to it and times nothing. If Android doesn't report a `COLD`
   start, the script warns and records no time. If `am start` fails, the script first looks for
   the app's crash report, so that a crash at start is reported with the app's own message.
5. Waits for the page's ready line in logcat (`-TimeoutSeconds`, 60 by default), or stops at once
   on a crash report for the app: Java's (`FATAL EXCEPTION` with `Process: <package>`) or .NET's
   (`UNHANDLED EXCEPTION`), read from the errors in the main and crash buffers. The sample's shell
   writes the line, under logcat's `DOTNET` tag, for the page it opened, after the page's first
   drawn frame:

   ```
   CORNFLOWER ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>
   ```

   `open` is the shell's time from navigation, before the page is made, to that frame; on a cold
   start it includes making the app's view. `view` is the window's size in view pixels and `raw` in
   raw pixels, `safe` the safe area, `page` the page's own size, and the title is last, as it can
   have spaces. On a phone, Avalonia gives the safe area to the shell, the view's content, as its
   padding, so `page` is the view less the safe area: on the Pixel 8, 840 tall (914.29 − 50.29 − 24).
   Before the shell, the page was the view's content and padded itself, so it read 915. The shell
   lays the page out before it reads its size, so a safe area or view that changed after the
   page's first frame is in it (the first run after the shell read `page=412x720`). The script checks that the title is the page `-Page` asked for, in any case. A
   Debug build also writes `CORNFLOWER cost <line>` after it, what the page cost to open
   ([`docs/performance`](../../docs/performance/README.md#in-a-debug-build)).
6. Saves the screen with `screencap` on the phone and pulls it, so the PNG's bytes aren't
   re-encoded by PowerShell 5.1.

It writes one folder per run, `artifacts/device/<yyyyMMdd-HHmmss>-<model>/` unless `-Out` says
otherwise. `artifacts/` is ignored by git.

| File | What |
|---|---|
| `frame.png` | The phone's screen at its own resolution, status bar and all |
| `run.json` | The device, the APK, `launchState` and `totalTimeMs` from `am start -W`, the page opened (`page`) and the shell's time to open it (`openMs`), the ready line's facts (`ready`), and in a Debug build the cost line (`cost`) |
| `logcat.txt` | The run's whole log, also written when the ready line never comes |

The ADB path is taken from `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `%LOCALAPPDATA%\Android\Sdk`, then
`PATH`.

## The phone

A Pixel 8 (`shiba`) on the maintainer's desktop. It keeps its secure lock, because it's signed in to
the maintainer's Google account, so a run on a locked phone waits for a hand. It locks 30 s after
the screen goes off, so runs close together don't each need an unlock. It sits on a USB-C switch shared with a laptop,
so it may need switching over first; wireless debugging (Settings → Developer options → Wireless
debugging, then `adb pair` and `adb connect`) avoids the switch. Over wireless, `adb devices` can list
the phone twice (its mDNS name and its `ip:port`); the script wants one entry, so
`adb disconnect` one of them.

`-Configuration Debug` works too: the Android project embeds its assemblies in the APK in every
configuration, so an APK installed with `adb install` starts without Fast Deployment.

Read by the first runs, 2026-11-09, Android 17 (API 37):

| | |
|---|---|
| Screen | 1080 × 2400 at density 420 |
| Render scaling | **2.625**, so the view is **411.43 × 914.29** view pixels |
| Safe area | 50.29 at the top (the status bar); 24 at the bottom with the gesture bar showing, 0 on the lock screen |
| Cold start, `am start -W` `TotalTime` | 1171 ms on the first start after install, 610 ms on the next; 644 ms by `run.ps1` |
| APK, Release | 37,961,165 bytes |

The reference profile is 450 × 800 at 2.4 (ADR 0001), so the phone's view is 38.57 view pixels
narrower and 114.29 taller than the emulator's.
