# tools/android

The device run: the sample on a real phone, over ADB (ADR 0002, "Platforms";
[`PROCESS.md`](../../docs/PROCESS.md#4-review), "The device run").

```sh
pwsh -NoProfile -File tools/android/run.ps1             # build, install, start, capture
pwsh -NoProfile -File tools/android/run.ps1 -NoBuild    # reuse the last build
pwsh -NoProfile -File tools/android/run.ps1 -Page theme # open a page of the catalogue
```

It runs on Linux, macOS and Windows, under PowerShell 7.4 or later (`pwsh`; see
[`tools/README.md`](../README.md#powershell) to install it), with the .NET SDK and its `android`
workload for the build.

The phone shows the sample's catalogue, as the desktop does, with no rail for Back: the phone's
back button or gesture is Back, to the list, and from the list out of the app. `-Page <title>`
opens a page of the catalogue at start, by its title in any case (quote a title with a space); it is
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

   `open` is the shell's time from navigation, before the page is made, to the frame after the
   page's first, once that frame's layout is done; on a cold start it includes making the app's
   view. `view` is the window's size in view pixels and `raw` in raw pixels, `safe` the safe area,
   `page` the page's own size, laid out in the same frame as `open`, and the title is last, as it
   can have spaces. On a phone, Avalonia gives the safe area to the shell, the view's content, as
   its padding, so `page` is the view less the safe area, each side rounded to the raw pixel grid:
   on the Pixel 8, 840 tall (914.29 less 50.29 and 24), as every device run of 2026-11-12 read (`page=411.43x840`).
   Before the shell, the page was the view's content and padded itself, so it read 915. The first
   run after the shell read `page=412x720`: the line was read before the layout of a safe area or
   view that had changed since the page's first frame.

   The script checks that the title is the page `-Page` asked for, in any case. A Debug build also
   writes `CORNFLOWER cost <line>` after the ready line, what the page cost to open
   ([`docs/performance`](../../docs/performance/README.md#in-a-debug-build)).
6. Saves the screen with `adb exec-out screencap -p`, its PNG redirected to `frame.png` byte for
   byte (PowerShell 7.4 keeps a native command's bytes when it redirects them to a file), and
   checks the file starts with PNG's signature: `exec-out` passes on neither screencap's exit code
   nor its errors apart from its output, so a failed screencap shows as text in the file, which the
   script quotes.

It writes one folder per run, `artifacts/device/<yyyyMMdd-HHmmss>-<model>/` unless `-Out` says
otherwise. `artifacts/` is ignored by git.

| File | What |
|---|---|
| `frame.png` | The phone's screen at its own resolution, status bar and all |
| `run.json` | The device, the APK, `launchState` and `totalTimeMs` from `am start -W`, the page opened (`page`) and the shell's time to open it (`openMs`), the ready line's facts (`ready`), and in a Debug build the cost line (`cost`) |
| `logcat.txt` | The run's whole log, also written when the ready line never comes |

`adb` is taken from `$ANDROID_HOME/platform-tools`, then `$ANDROID_SDK_ROOT/platform-tools`, then
the SDK's default folder, where Android Studio puts it (`%LOCALAPPDATA%\Android\Sdk` on Windows,
`~/Library/Android/sdk` on macOS, `~/Android/Sdk` on Linux), then `PATH`. Keep to one adb: a
different one restarts the adb server, which can drop the phone's wireless pairing. The build is
given the SDK that adb is in (`-p:AndroidSdkDirectory`, as CI gives it `$ANDROID_HOME`), so that
one SDK is used for both; an adb that isn't in an SDK's `platform-tools`, such as a packaged one
on `PATH`, leaves the build to find its own.

Run it with `-NoProfile`, so that nothing in a PowerShell profile changes the run. The script also
pins the two settings it depends on, `$PSNativeCommandArgumentPassing` (`Standard`, so a title's
quotes reach adb intact) and `$PSNativeCommandUseErrorActionPreference` (off, as each call's exit
code is checked).

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
