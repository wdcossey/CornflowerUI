# The control loop

Cornflower is built **one control or family per PR**. The loop below is the only way a control
enters the codebase. No step is skipped, and nothing is done until the last step is signed off.
The human reviews once, at the PR.

```
 ┌────────┐   ┌───────┐   ┌─────────┐   ┌────────┐   ┌──────────┐
 │  PLAN  │ → │ BUILD │ → │ COMPARE │ → │ REVIEW │ → │ DOCUMENT │ → next control
 └────────┘   └───────┘   └─────────┘   └────────┘   └──────────┘
```

The loop is Avonic's
([`docs/PROCESS.md`](https://github.com/wdcossey/Avonic/blob/v2/docs/PROCESS.md) in Avonic), with
the reference swapped: Avonic's runs in Chromium on demand; ours is a Windows Phone 8.1 emulator,
captured ahead of time. What that changes is in [The reference](#the-reference).

Several steps below rely on tools that don't exist yet. Each is marked with the Tier 0 item that
builds it. Until it lands, the step is done by hand and the PR says how.

## 0. Working from a GitHub issue

Every control and piece of infrastructure is a GitHub issue with a tier label (`tier 0` to
`tier 4`), a kind label (`control`, `infra`, `spike`, `reference`, `tooling`) and the issues that
block it ([ADR 0002](decisions/0002-decisions-of-2026-11-08.md#how-the-work-runs)). Start one
only when its blockers are closed.

1. Branch from `main`, named for the kind and the item, such as `infra/ci-actions`.
2. Run the loop. The plan isn't gated on a human mid-flight: write `PLAN.md`, then continue
   through Build, Compare, Review and Document.
3. Open a PR into `main` with the PR template, ending `Closes #N`. `main` is protected: the
   `build-test` check must pass. The human reads the plan, the comparison and the diff at the PR,
   and merges or asks for changes. Only the merge marks the control done.

A control's PR adds its own files and touches no shared file, except the holes list: it deletes
its own lines from `tests/Cornflower.Tests/Completeness/Holes.txt`, which `HolesTests` requires
([#13](https://github.com/wdcossey/CornflowerUI/issues/13)). If it adds global style rules past
the budget, it also raises a limit in `StyleBudgetTests`, on purpose and said in its description
([docs/performance](performance/README.md)). A theme file is found by
auto-discovery (`Directory.Build.targets`), so parallel PRs merge in any order; two that delete
neighbouring lines of the holes list need a rebase. A control's PR
doesn't edit the tables in `CLAUDE.md` or [`ROADMAP.md`](ROADMAP.md); see
[Rolling up the tables](#rolling-up-the-tables).

## 1. Plan

`docs/controls/<Name>/PLAN.md`, before any code. It is read at the PR beside the comparison, so
decisions and omissions are written out.

1. **The checklist.** Paste the control's generated checklist. Every row is ticked, or has a
   one-line reason it is left out (`not applicable: Windows tiles`), and its Action filled in. An
   unexplained gap is a planning failure. It's generated from the two files below:

   ```
   dotnet run --project tools/winrt-api -- checklist Button <Windows.winmd> <design folder>
   ```

   where the design folder holds `generic.xaml` and `themeresources.xaml`. A control outside
   `Windows.UI.Xaml.Controls` and `.Primitives` is named in full. Its Avalonia column is
   mechanical (the mapping rules are a draft), so each row is checked, not trusted.
2. **The behaviours**, each with its evidence: a capture, a template line, or a measurement. One
   test each.
3. **The visual states** and the resource keys that drive each one, from the template.
4. **For one of Avalonia's controls:** Avalonia's control measured against the phone's, and for
   each difference, the rung of [the ladder](ARCHITECTURE.md#the-limit-of-a-theme) chosen.
5. **Extensions**, in their own section, never shadowing a WinRT or Avalonia name.
6. **Dependencies** on other controls and whether they exist, and the captures still needed.

### The spec is in two files

Avonic's spec is one file. Here it is two, both read from the Windows Phone 8.1 SDK, which stays
outside the repo ([ADR 0002](decisions/0002-decisions-of-2026-11-08.md#reference-material)):

| File | Gives |
|---|---|
| `Windows.winmd` | The API: properties (and which are dependency properties), events and methods. Not the template parts or visual states: WinRT metadata doesn't carry them |
| `generic.xaml` (with `themeresources.xaml`) | The template: setters, parts, visual state groups and states, the resources each state uses, the theme animations |

The checklist is generated from both.

### Built-in controls have a third column

For a control Avalonia already has, each row is the WinRT member, Avalonia's equivalent, and what
we do:

| WinRT | Avalonia | Action |
|---|---|---|
| `ToggleSwitch.IsOn` | `ToggleSwitch.IsChecked` | Nothing: Avalonia's API stands |
| `ToggleSwitch.Header` | none | Extension, or recorded as missing |
| Visual state `Dragging` | `:dragging` | Themed |

For a new control, the checklist is WinRT's API, mapped mechanically.

### Behaviour has no source to read

The phone's control source was never published ([ADR 0001](decisions/0001-what-faithful-means.md)).
So the behaviours come from:

1. The emulator: exercise the control and record what it does, what moves, when, by how much,
   and what cancels it. A template storyboard can be read in the app every frame (a probe, as
   `run.ps1 -Probe` runs them); a theme transition runs in the compositor and leaves nothing to
   read, so it is timed from a screen recording stepped by frame (`run.ps1 -Record`). The
   emulator animates on a 64 Hz clock.
2. The template: what it declares, its states, transitions and durations.
3. WinUI 3's source or Uno Platform's port of the same control, as a guide to what to look for.
   A behaviour found only there, and not seen in the emulator, isn't listed.
4. Avalonia's control, measured the same way, if there is one. The differences are the work.

## 2. Build

One PR, one control or family:

| Deliverable | Where |
|---|---|
| For a new control: the class, with behaviour, properties, events and pseudoclasses, and no visual opinion | `src/Cornflower.Controls/Controls/<Name>/` |
| For a new control: a functional stub template, structure only | the same folder |
| The era's `ControlTheme`, using resource keys | `src/Cornflower.Themes.Wp81/Controls/<Name>.axaml` |
| Resource keys, named as the era names them; the only place for a literal colour | `src/Cornflower.Themes.Wp81/Tokens/<Name>.axaml` |
| Tests, one per planned behaviour or measured state; each fails when its behaviour is removed | `tests/Cornflower.Tests/Controls/<Name>Tests.cs` |
| A sample page | `samples/Cornflower.Sample/Pages/<Name>Page.axaml`. *The sample is a Tier 0 item ([#9](https://github.com/wdcossey/CornflowerUI/issues/9))* |

Not negotiable:

- `dotnet build` with 0 warnings and `dotnet test` green **before every commit**.
- A declared property works and has a test that fails without it. If it can't work yet, it isn't
  declared.
- Tests assert the reference's behaviour, from a capture or measurement named in the plan, not
  the implementation's own state.
- No literal colour in a theme outside `Tokens/`. No visual opinion in `Cornflower.Controls`.
- No Microsoft font, template or SDK file. Every template is written new.

A control's tests build on the harness from
[#11](https://github.com/wdcossey/CornflowerUI/issues/11): `Scene`, a shown headless window at
the reference profile, closed when the test ends; `TestClock`, a clock the test advances by hand;
and `Frames.PixelAt`, pixels from a captured frame. A theme transition is tested at its end
value; only a motion that must match frame by frame is a timeline on `CornflowerClock`
([ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md)).

## 3. Compare

`docs/controls/<Name>/COMPARISON.md`: the emulator's capture beside ours, and every planned
behaviour marked **match**, **deliberate difference** or **gap**.

- Captures come from `ref/wp81/<control>/`, made on the reference profile
  ([ADR 0001](decisions/0001-what-faithful-means.md)). See [The reference](#the-reference).
- Our side is a headless frame from the tests or the sample, and says which.
- **Text isn't compared by pixel.** The fonts differ by design. A comparison states the font on
  each side, compares geometry (bounds, borders, spacing, colours) by measurement, and compares
  text by position and size.
- A gap goes back to Build. If it can't be closed, because Avalonia forces it or it is out of
  reach, it stands, recorded with its measurement and reason in `COMPARISON.md` and the README's
  known gaps. A gap an app would notice gets an issue.

*A side-by-side tool, two frames in one image, is a Tier 0 tool with no issue yet. Until it
exists, the two images sit next to each other in the comparison.*

## 4. Review

- `/code-review` at high effort, **twice** for a new control or a behaviour change, the second
  pass on the fixed diff. Once for docs and samples. Findings are fixed or waived, with reasons,
  in the PR.
- **Prove the tests can fail** by breaking each behaviour in a clean clone and checking a test
  goes red, with `tools/breakage/run.js` ([docs/performance](performance/README.md) has how).
- **A screenshot** in the PR description of anything visual: the emulator's capture beside ours,
  through URLs pinned to the commit, naming the emulator profile and saying if ours is headless.
- **The device run**, once per batch, before it is called ready to merge: the sample on the
  Android device over ADB, with frames pulled back, by
  [`tools/android/run.ps1`](../tools/android/README.md). Ask first.
- **The style budget** in CI. Over the limit, see whether the alternatives can go, or raise the
  limit on purpose and say so in the PR (`StyleBudgetTests`;
  [docs/performance](performance/README.md)).
- A review finding can be wrong. Check it against the reference.

## 5. Document

`docs/controls/<Name>/README.md`: usage, the final ticked checklist, the behaviours, extensions,
resource keys, the rung of the ladder and any type substitution, and known gaps. Directly under
its heading goes the status line, written only now:

```markdown
Status: done · [#N](https://github.com/wdcossey/CornflowerUI/issues/N)
```

**Then** the next control starts. Not before.

## The reference

The reference can't run in CI. It is the Windows Phone 8.1 emulator on a reference VM, reached
over SSH.

- **Captures are made ahead of time** on the VM by the reference app, which renders each control
  at rest and in every visual state its template declares, dark and light, and pulls the results
  back. The PNGs are committed under `ref/wp81/<control>/`, with the emulator profile,
  resolution, scale and date beside them. The JSON template-tree dumps stay outside the repo; a
  plan quotes the numbers it uses and names the dump
  ([ADR 0002](decisions/0002-decisions-of-2026-11-08.md#reference-material)).
  The reference app is [`tools/refapp`](../tools/refapp/README.md). The first committed
  captures are `ref/wp81/TextBlock/` (#69).
- **A capture is a forced state on a fresh instance, without the motion into it.** Motion (the
  tilt, page transitions, the ToggleSwitch drag) comes from per-frame probes and screen
  recordings of the emulator ([#24](https://github.com/wdcossey/CornflowerUI/issues/24)), in
  `tools/refapp/README.md` "Motion". Flyouts and dialogs live in popups outside the captured
  stage, and the page's app bar is drawn by the shell, so all three are captured with the
  emulator's Screenshot tool, and for the app bar the screenshots are the only reference
  ([#23](https://github.com/wdcossey/CornflowerUI/issues/23)).
- **One emulator, one driver.** An agent in a worktree doesn't get the VM: two agents driving one
  emulator would collide. So each control's capture list is written at planning time and captured
  before its batch starts. A plan that needs a state not yet captured says so under "For you to
  decide". Whoever runs the batch can capture a new state on demand if the VM is up.
- Microsoft's SDK files, the dumps and anything else read from the VM live outside the repo, in
  the reference folder named in `CLAUDE.md`.

## Batches

After Button and ProgressRing ([ADR 0002](decisions/0002-decisions-of-2026-11-08.md#how-the-work-runs)),
controls run two or three at a time, as Avonic's do. Each agent works in its own worktree, with:

- a list of shared files it doesn't touch;
- a "Proposed for shared files" section in its PR, for changes it needed and didn't make;
- a "For you to decide" section, for questions it couldn't settle.

## Rolling up the tables

The controls table in `CLAUDE.md` and the tables in [`ROADMAP.md`](ROADMAP.md) are shared, so a
control's PR leaves them alone. When every issue in a tier is closed, or sooner if the maintainer
asks, one PR adds the finished rows to both. Only one roll-up PR is open at a time.

The tables lag between roll-ups, so a missing row doesn't mean "not built". The current record is
the status lines: `git grep -l "^Status: done" docs/controls`.

`CLAUDE.md` states only what exists. A plan, a proposal or an intention goes in
[`ROADMAP.md`](ROADMAP.md) or [`ARCHITECTURE.md`](ARCHITECTURE.md), and moves to `CLAUDE.md` in
the PR that makes it true.

## Lessons from Avonic that apply directly

From Avonic's
[`docs/LESSONS_LEARNED.md`](https://github.com/wdcossey/Avonic/blob/v2/docs/LESSONS_LEARNED.md):

- **§1.1 Check the reference pipeline exists.** Avonic's first version was told to read a path
  that wasn't there, and nobody noticed for 27 controls.
- **§1.4 Settle the founding decision in writing.** Here, [ADR 0001](decisions/0001-what-faithful-means.md).
- **§1.5 Build before anything.** The gate and CI came before the first control.
- **§1.6 Docs state only what exists.**
- **§1.7 Depth over breadth.** Five real controls before a sixth. One era before a second.
- **§3.8 Icons are infrastructure.** The template glyph set and the Lucide generator are Tier 0,
  not something each control improvises.
- **§3.11 Scope is explicit.** In, later and out, counted against the inventory in
  [`ROADMAP.md`](ROADMAP.md).

And from Avonic's swarm brief: measure the reference before writing behaviour; look at the two
images side by side every time; `--no-build` runs stale binaries; test in the layouts an app
really uses, not only a fixed-size box.
