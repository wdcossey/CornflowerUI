# tools/android

The device run: the sample on a real phone, over ADB (ADR 0002, "Platforms";
[`PROCESS.md`](../../docs/PROCESS.md#4-review), "The device run").

```powershell
powershell -ExecutionPolicy Bypass -File tools\android\run.ps1             # build, install, start, capture
powershell -ExecutionPolicy Bypass -File tools\android\run.ps1 -NoBuild    # reuse the last build
```

`run.ps1`:

1. Checks that `adb devices` lists exactly one phone, and reads its model, Android version, screen
   size and density.
2. Builds `samples/Cornflower.Sample.Android` (Release by default), and installs the signed APK.
3. Wakes the screen. A phone with a secure lock has to be unlocked by hand: the script says so and
   waits (`-UnlockTimeoutSeconds`, 30 by default), then waits for the screen to be fully awake.
4. Goes to the home screen, stops the app, and starts it cold with `am start -W -S`, which reports
   the time Android measured from the intent to the activity's first frame (`TotalTime`). Going
   home first matters: a screen waking from doze can bring the app's last task back by itself,
   and then `am start` only delivers to it and times nothing. If Android doesn't report a `COLD`
   start, the script warns and records no time.
5. Waits for the page's `CORNFLOWER ready` line in logcat (the `DOTNET` tag; `-TimeoutSeconds`, 60
   by default). The page writes it
   after its first drawn frame, with the render scaling, the view's size in view pixels and raw
   pixels, and the safe area.
6. Saves the screen with `screencap` on the phone and pulls it, so the PNG's bytes aren't
   re-encoded by PowerShell 5.1.

It writes one folder per run, `artifacts/device/<yyyyMMdd-HHmmss>-<model>/` unless `-Out` says
otherwise. `artifacts/` is ignored by git.

| File | What |
|---|---|
| `frame.png` | The phone's screen at its own resolution, status bar and all |
| `run.json` | The device, the APK, `launchState` and `totalTimeMs` from `am start -W`, and the ready line's facts |
| `logcat.txt` | The run's whole log, also written when the ready line never comes |

The ADB path is taken from `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `%LOCALAPPDATA%\Android\Sdk`, then
`PATH`.

## The phone

A Pixel 8 (`shiba`) on the maintainer's desktop. It sits on a USB-C switch shared with a laptop,
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
