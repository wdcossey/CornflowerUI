# Cornflower — CLAUDE.md

**Cornflower** is a faithful reproduction of the Windows Phone design languages for AvaloniaUI,
mobile first. It themes Avalonia's own controls and adds the controls Avalonia lacks. The first
era is Windows Phone 8.1.

This file states **only what exists**. A plan, a proposal or an intention does not go here.
Plans live in `docs/ROADMAP.md`; process in `docs/PROCESS.md`; decisions in `docs/decisions/`; design in `docs/ARCHITECTURE.md`.

## Persona

Principal engineer on an AvaloniaUI theme and control library. You care about fidelity to the
reference, correctness on touch devices, and never declaring something that doesn't work. You
measure the reference before writing a line, and you build and test before every commit.

## Repository

| Path | What it is |
|---|---|
| `Cornflower.sln` | The solution. Libraries target `net8.0`, tests `net10.0`; `global.json` pins SDK 10 |
| `Directory.Build.props` | Shared settings: nullable, warnings as errors, central package versions, NuGet metadata |
| `Directory.Packages.props` | Every package version. Avalonia 12.1.4, xunit v3; Avalonia.Themes.Simple for the tests and the sample, Avalonia.Fonts.Inter for the tests, Avalonia.Android for the Android sample |
| `Directory.Build.targets` | Auto-discovery: a project that sets `CornflowerDiscoverXaml` gets an internal `XamlManifest.Paths` of its `CornflowerDiscoveredXaml` globs |
| `src/Cornflower.Controls/` | Cornflower's own controls and behaviours. No controls yet. `CornflowerClock`, internal: the time every Cornflower timeline reads (now, a one-shot delay, the next frame), so a test can replace it |
| `src/Cornflower.Themes.Wp81/` | The Windows Phone 8.1 theme. `Wp81Theme` merges every discovered file under `Tokens/` and `Controls/`, in ordinal order. `Tokens/` holds the 8.1 `themeresources.xaml` values under their WinRT keys: `Colors.axaml` (dark and light as `ThemeVariant` dictionaries), `Accent.axaml` (`SystemColorControlAccentColor`, default `#FF3E65FF`, and the 20 named accents), `Typography.axaml` (font sizes, weights, character spacing) and `Sizes.axaml`. `Tokens/Fonts.axaml` holds the six WinRT font family keys; `Fonts/` bundles Selawik 1.01 (OFL-1.1), with Semilight in a folder of its own (`Fonts/README.md`), so the package's licence is `Apache-2.0 AND OFL-1.1`, set in its csproj. No `Controls/` yet |
| `src/Cornflower/` | Umbrella package referencing the two above. No code |
| `samples/Cornflower.Sample/` | The sample app every head runs: `App` (`SimpleTheme` then `Wp81Theme`, Dark) and one page, `MainView`, showing the accent, the 20 named accents, the type ramp and the font families through their keys, and what the device reports. Once drawn, the page writes a `CORNFLOWER ready` line to standard output |
| `samples/Cornflower.Sample.Android/` | The Android head (`net10.0-android`, `io.github.wdcossey.cornflower.sample`). Not in the solution: it needs the `android` workload, and CI builds it in its own job |
| `tests/Cornflower.Tests/` | Avalonia.Headless with Skia and xunit v3. The harness: `Scene`, a shown window at the reference profile (a 450 × 800 view at render scaling 2.4, so a frame is 1080 × 1920 like the emulator's; `SimpleTheme` then `Wp81Theme` in its `Styles` before the content; dark unless light is asked for), closed and emptied on dispose; `Frames.PixelAt`; `TestClock`, which replaces `CornflowerClock` and moves only when the test moves it. Their tests in `Harness/`, discovery tests run on fixtures in `Discovery/Fixture/`, the spikes in `Spikes/` (one collection, not run in parallel), the font tests in `Fonts/`, the token tests in `Tokens/` (expected values written out, with their sources), the sample's tests in `Sample/`, and the repository guards in `Repository/`: no Microsoft files, no literal colour under `src/` outside a `Tokens/` folder |
| `tools/refapp/` | The Windows Phone 8.1 reference app and `run.ps1`, which builds it on the reference VM, runs it in the emulator and pulls the captures back. Not in the solution: it builds with MSBuild 14 on the VM |
| `tools/winrt-api/` | Lists the XAML control types in a `Windows.winmd`, and which have a default style in a `generic.xaml`. In the solution, so CI builds it |
| `tools/fonts/` | Node scripts that compare font metrics, weight by weight |
| `tools/tokens/` | Python scripts: `crosscheck.py` compares the SDK's theme resource values with a refapp run's captures; `gen.py` writes the four generated files in `Tokens/` and `TokenExpectations.cs` from the SDK files and that comparison. They read Microsoft's files at run time from outside the repo |
| `tools/android/` | `run.ps1`, the device run: builds and installs the Android sample over ADB, starts it cold, waits for its ready line, and pulls back a frame, `run.json` and logcat into `artifacts/device/` |
| `tools/collect-vm.ps1`, `tools/setres.ps1` | Run on the reference VM: inventory its SDK files; set its desktop resolution |
| `tools/guard/` | `hash-reference.js` writes the SHA-256 list of the pulled Microsoft files that the repository guard test checks against |
| `.github/workflows/ci.yml` | On every push to `main` and every PR: `build-test` restores, builds and tests the solution in Release; `android` installs the workload and builds the Android sample |
| `docs/PROCESS.md` | The control loop: plan, build, compare, review, document |
| `docs/ROADMAP.md` | Scope, the control inventory by tier, the Tier 0 checklist |
| `docs/ARCHITECTURE.md` | Eras, layout, the ladder, theming, platform features, metrics and testing; the draft mapping rules |
| `docs/spikes/` | Questions about Avalonia 12.1.4 answered by tests in `tests/Cornflower.Tests/Spikes/` |
| `docs/decisions/` | ADR 0001, what "faithful" means; ADR 0002, the decisions of 2026-11-08; ADR 0003, the decisions on the Tier 0 infrastructure (rounding, transitions, line height, variant, licence) |

## Build

```sh
dotnet build Cornflower.sln
dotnet test  Cornflower.sln
```

## Controls

None yet.

## Avalonia facts the code relies on

Each is held by a test, most of them spikes ([docs/spikes](docs/spikes/README.md)); an upgrade that changes one turns it red.

- **A control picks its `ControlTheme` once, when it joins a logical tree.** Install themes before setting content, and put `Wp81Theme` after `SimpleTheme`.
- **A missing `ControlTheme` fails silently**: a `ContentControl` gets a bare `ContentPresenter`, a `TextBox` gets nothing and measures 0×0. Every control and subclass has a test that it is templated by the theme.
- **A subclass gets no theme of its own.** It needs a `ControlTheme` with `BasedOn` the base's, or a `StyleKeyOverride` returning the base type.
- **`LetterSpacing` is in DIPs.** WinRT `CharacterSpacing` n is n × `FontSize` / 1000.
- **A weight word in a family name overrides `FontWeight`, and one font collection has one face per weight.** "Selawik Semilight" is asked for as "Selawik" at 350, and the first face loaded at 300 wins that slot. Selawik Semilight's file says 300, so it is bundled alone in `Fonts/SelawikSemilight/`. Held by `tests/Cornflower.Tests/Fonts/SemilightTests.cs`.
- **A line is the font's `hhea` line**, unless `OS/2` sets `USE_TYPO_METRICS`; a larger `LineHeight` is split evenly above and below. Selawik's line is 1.200 em; the phone's Segoe WP line is 1.330 em. Held by `tests/Cornflower.Tests/Fonts/LineMetricTests.cs`.
- **Avalonia's animation clock is internal.** `Transitions` and `Animation` run on real time, so a test checks a transition at its end value; a timeline a test must assert by the millisecond is a frame loop on `CornflowerClock`. Held by `tests/Cornflower.Tests/Harness/CornflowerClockTests.cs`.
- **Layout rounds a size up to the raw pixel grid**, where the phone rounds to the nearest: at 1.8 a 109-wide Button is 109.44, the phone's 108.89. A recorded deviation (ADR 0003). Held by `tests/Cornflower.Tests/Harness/SceneTests.cs`.
- **A `DynamicResource` on a brush's `Color`, inside a theme's dictionary, follows a key redefined in `Application.Resources`.** An app sets the accent this way, by defining `SystemColorControlAccentColor`. Held by `tests/Cornflower.Tests/Tokens/TokenTests.cs`.

## Reference material

Microsoft's Windows Phone 8.1 SDK files (default templates, theme resources, API metadata, design
files, fonts) are the reference. They are read as a specification and **never enter this
repository**. They live outside it, in `R:\cornflower-reference\` on the maintainer's machine.

## Rules

1. **Build gate.** `dotnet build` with 0 warnings and `dotnet test` green before every commit.
2. **One control or family per PR.**
3. **No dead props.** Declared ⇒ works ⇒ has a test that fails without it.
4. **Tests assert the reference's behaviour**, from a capture or measurement named in the PR.
5. **No literal colours outside a theme's `Tokens/`.** No visual opinion in `Cornflower.Controls`.
   `LiteralColourGuardTests` fails on a literal colour in any file under `src/` outside a `Tokens/`
   folder: in XAML, a value Avalonia parses as a colour, outside comments; in C#, a hex string,
   `Color.From*`, `Color.Parse`, `Colors.*` or `Brushes.*`. `Transparent` is allowed.
6. **No Microsoft fonts, templates or SDK files in the repo.** Every template is written new.
   `MicrosoftFileGuardTests` fails on any file git would commit that is a font not on its
   allowed list, is named `generic.xaml`, `themeresources.xaml` or `*.winmd`, or has the bytes of a
   file pulled from the reference VM. The hash list is regenerated with
   `node tools/guard/hash-reference.js <reference folder>` whenever more files are pulled.
7. **Add files, don't register them.** A control's theme file is found by auto-discovery; a PR
   for one control touches no shared file.
8. **This file is ground truth.** Update it in the PR that makes a statement here true.

## Attribution & licence

Apache-2.0. `Cornflower.Themes.Wp81` also carries Selawik under the SIL Open Font License 1.1, so
its package licence is `Apache-2.0 AND OFL-1.1`. Cornflower is an independent implementation for
AvaloniaUI and is not affiliated with or endorsed by Microsoft or the AvaloniaUI team.
