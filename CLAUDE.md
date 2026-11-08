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
| `Directory.Packages.props` | Every package version. Avalonia 12.1.4, xunit v3; Avalonia.Themes.Simple and Avalonia.Fonts.Inter for the tests only |
| `Directory.Build.targets` | Auto-discovery: a project that sets `CornflowerDiscoverXaml` gets an internal `XamlManifest.Paths` of its `CornflowerDiscoveredXaml` globs |
| `src/Cornflower.Controls/` | Cornflower's own controls and behaviours. Empty |
| `src/Cornflower.Themes.Wp81/` | The Windows Phone 8.1 theme. `Wp81Theme` merges every discovered file under `Tokens/` and `Controls/`, in ordinal order. No files yet |
| `src/Cornflower/` | Umbrella package referencing the two above. No code |
| `tests/Cornflower.Tests/` | Avalonia.Headless with Skia and xunit v3. Harness tests, discovery tests run on fixtures in `Discovery/Fixture/`, and the spikes in `Spikes/` (one collection, not run in parallel) |
| `tools/refapp/` | The Windows Phone 8.1 reference app and `run.ps1`, which builds it on the reference VM, runs it in the emulator and pulls the captures back. Not in the solution: it builds with MSBuild 14 on the VM |
| `tools/winrt-api/` | Lists the XAML control types in a `Windows.winmd`, and which have a default style in a `generic.xaml`. In the solution, so CI builds it |
| `tools/fonts/` | Node scripts that compare font metrics, weight by weight |
| `tools/collect-vm.ps1`, `tools/setres.ps1` | Run on the reference VM: inventory its SDK files; set its desktop resolution |
| `.github/workflows/ci.yml` | Restore, build and test in Release on every push to `main` and every PR |
| `docs/PROCESS.md` | The control loop: plan, build, compare, review, document |
| `docs/ROADMAP.md` | Scope, the control inventory by tier, the Tier 0 checklist |
| `docs/ARCHITECTURE.md` | Eras, layout, the ladder, theming, platform features, metrics and testing; the draft mapping rules |
| `docs/spikes/` | Questions about Avalonia 12.1.4 answered by tests in `tests/Cornflower.Tests/Spikes/` |
| `docs/decisions/` | ADR 0001, what "faithful" means; ADR 0002, the decisions of 2026-11-08 |

## Build

```sh
dotnet build Cornflower.sln
dotnet test  Cornflower.sln
```

## Controls

None yet.

## Avalonia facts the code relies on

Each is held by a spike test ([docs/spikes](docs/spikes/README.md)); an upgrade that changes one turns it red.

- **A control picks its `ControlTheme` once, when it joins a logical tree.** Install themes before setting content, and put `Wp81Theme` after `SimpleTheme`.
- **A missing `ControlTheme` fails silently**: a `ContentControl` gets a bare `ContentPresenter`, a `TextBox` gets nothing and measures 0×0. Every control and subclass has a test that it is templated by the theme.
- **A subclass gets no theme of its own.** It needs a `ControlTheme` with `BasedOn` the base's, or a `StyleKeyOverride` returning the base type.
- **`LetterSpacing` is in DIPs.** WinRT `CharacterSpacing` n is n × `FontSize` / 1000.

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
6. **No Microsoft fonts, templates or SDK files in the repo.** Every template is written new.
7. **Add files, don't register them.** A control's theme file is found by auto-discovery; a PR
   for one control touches no shared file.
8. **This file is ground truth.** Update it in the PR that makes a statement here true.

## Attribution & licence

Apache-2.0. Cornflower is an independent implementation for AvaloniaUI and is not affiliated with
or endorsed by Microsoft or the AvaloniaUI team.
