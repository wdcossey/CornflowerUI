# Performance

What the theme costs an app, and what stops that creeping up unnoticed. Carried over from Avonic's
[`docs/infra/performance/README.md`](https://github.com/wdcossey/Avonic/blob/v2/docs/infra/performance/README.md)
([#12](https://github.com/wdcossey/CornflowerUI/issues/12)); what changed is at the end.

## What it costs

Avalonia checks **every global style rule against every element**, in any app, even one that uses
none of these controls. It keeps no cache by type, and it visits nested rules whether or not the
parent matched. Avonic measured about **30 ns for each alternative between commas**, and about
385 ns for a descendant selector on an element at a depth of 20. Those are Avonic's numbers, from
one desktop PC; they haven't been measured again here.

**Rules inside a `ControlTheme` aren't global**: they cost only the controls that use the theme.
Cornflower themes Avalonia's own controls, so almost all its styling lives in `ControlTheme`s, and
its global rules should be few. That is why the limits below are far lower than Avonic's.
`StyleBudgetTests.A_ControlThemes_Rules_Are_Not_Checked_Against_Other_Elements` holds it.

## The budget

`StyleBudgetTests` counts `Wp81Theme`'s global rules, through Avalonia's own trace, and fails CI when:

| Check | Now | Limit |
|---|---|---|
| Alternatives in all global rules | 0 | 10 |
| Alternatives getting past a `TextBlock`'s type | 0 | 5 |
| Alternatives getting past the type of any other common element, at most | 0 | 5 |
| Alternatives that look at every ancestor of a common element | 0 | 0 |

The common elements are `TextBlock`, `Border`, `Panel`, `StackPanel`, `Grid`, `DockPanel`, `Image`,
`Button`, `ScrollViewer` and `ContentPresenter`. Each is counted alone: the trace listens only while
the element joins the tree, so its template's parts, styled later in layout, aren't counted against
it, and it keeps only checks of the theme's global rules, as Avalonia reports a `ControlTheme`'s
own checks through the same activity. The trace refuses to run with Avalonia's diagnostics off,
as they are when `CORNFLOWER_BENCHMARK` is set, since it would see nothing and every budget would
pass. A descendant combinator stops being an ancestor
walk when one of Cornflower's own controls follows it (`Pivot PivotItem > TextBlock`), as those are
rare; Avalonia's types don't stop it, because Cornflower themes `Button` and `TextBlock` directly.

**When a control goes over,** the failure lists the largest rules. First see whether the rule can
move into the control's `ControlTheme`, or the commas can go. If not, raise the limit in
`StyleBudgetTests` in that PR, a control's included (`CLAUDE.md`, rules 7 and 8), and say so in
its description, with the new total. The table above catches up at the roll-up. The limits are
meant to be raised; the point is that it's a decision.

**100 alternatives is a checkpoint,** about 3 µs per element at Avonic's rate. A test stops the
limit going past it. When the theme gets there, stop and run the benchmark on the Pixel 8 before
going on, to decide on real numbers.

## The trend

Run by hand at each tier's roll-up, on the same machine, and add a row. Timings in CI would mean
nothing.

```
CORNFLOWER_BENCHMARK=/absolute/path/row.md dotnet test tests/Cornflower.Tests -c Release --filter FullyQualifiedName~StyleCostBenchmark
```

It alternates with and without `Wp81Theme`, 25 rounds after 4 to warm up, attaching 5,000
elements at a depth of 20, with Avalonia's diagnostics off, as an app has them. Times are µs per
element. *Themed* is the templated controls that aren't holes
([`Completeness/Holes.txt`](../../tests/Cornflower.Tests/Completeness/Holes.txt)). A row can go
straight in the table.

| When | Themed | Rules | Alternatives | `TextBlock` | `Border` |
|---|---|---|---|---|---|
| 2026-11-09 | 0 | 0 | 0 | 12.7 to 12.7 (+0.0) | 2.1 to 2.4 (+0.3) |

The first row is before any control: what the tokens and fonts cost, with no global rules.

## In a Debug build

The sample shows a line along the bottom of its page, once it has been laid out the first time:

```
theme: 475 ms, 91 elements, 1 rules (1), layout 1.5, render 18.9
```

That one is from a headless test, in Debug, on the desktop PC (`PerformanceMonitorTests`): the
page is the first a fresh app opens, so its time includes the cold start. On the Pixel 8, the same
page in a Debug build, started cold by `tools/android/run.ps1 -Configuration Debug` on 2026-11-09
(`am start -W` total 994 ms):

```
theme: 472 ms, 91 elements, 1 rules (1), layout 186.5, render 23.8
```

Layout is about 120 times the desktop's headless figure. That's a Debug build, JIT-compiled on
first use, so it measures first-run cost more than the theme; a Release reading needs a Release
build of the line, which the sample doesn't have. It hasn't been read in a real desktop window.

That's the time to open the page, from before it's made, the elements in it, the global rules it
is checked against and their alternatives (in the application's `Styles`, and the page's and each
logical ancestor's: the one here is the page's own `TextBlock.section`), and Avalonia's own layout
and render times over that time. Layout always comes before the line is read, even with a frame in
flight, as layout doesn't wait for a render tick (a test holds it). A Release build doesn't have it. **Nothing is added to the library,**
because Avalonia has this built in, behind a switch that's off by default:

- `AppContext.SetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", true)`, before Avalonia
  starts. Each head sets it, in Debug only: the Android head in its `Application`'s static
  constructor, the tests in `TestApp`. Without it the line says so instead of showing zeros.
- Meter `Avalonia.Diagnostic.Meter`: `avalonia.ui.measure.time`, `.arrange.time`, `.render.time`,
  and others.
- Activity source `Avalonia.Diagnostic.Source`: `Avalonia.AttachingStyle`, with the selector and
  its result, which `StyleTrace` reads.

Tracing activities costs far more than the checks being traced, so use its counts and not its
times. The sample's line doesn't trace.

## Breakage runs

`tools/breakage/run.js` breaks each behaviour on its own, in a fresh clone, and checks that a test
goes red:

```
git clone <this repo> <temp folder>
node tools/breakage/run.js <temp folder> <list.js> <results.txt> "<test filter>"
```

The list and the meaning of each result are in the script's header. It finds the clone's one
`*.Tests.csproj` under `tests/` unless one is named. A run that times out (10 minutes, or
`BREAKAGE_TIMEOUT_MS`) has whatever it left running from the clone killed, so a hung test host
can't lock the next build. It has been run on this work: 20 breaks of the census, the trace, the
budget, `Scene`'s themes and the monitor, all caught, and one hung test, timed out and cleaned up.

## Known limits

- **Only style checks are budgeted.** A slow control, layout or animation wouldn't show here. The
  sample's line is how to spot those.
- **`render` can read low,** if a page opens within a frame of the last one, because its own render
  pass hasn't happened yet.
- **One reading on a phone, in Debug.** The benchmark runs headless on a desktop; the line has
  been read once on the Pixel 8, in a Debug build (above), which says little about Release.
- **`ItemsControl`, virtualising and scrolling** haven't been measured. These are all times to
  attach.

## From Avonic

Copied from Avonic's `v2` branch. What changed:

- The limits: 10, 5, 5 and a checkpoint of 100, against Avonic's 200, 60, 25 and 600.
- `StyleTrace.ChecksFor` takes the `Styles` to count, and `Scene` takes its themes, so only
  `Wp81Theme`'s rules are counted, not Simple's underneath.
- The ancestor-walk check stops at Cornflower's own control types, by name, read from
  `Cornflower.Controls`, where Avonic stopped at its `Avonic` prefix.
- The trace counts only the element's own checks of the theme's global rules, and refuses to run
  with the diagnostics off; Avonic's counted every check while the element was added and laid out.
- The line counts the `Styles` of the page and its logical ancestors as well as the application's,
  and says when the diagnostics are off.
- The breakage tool kills what a timed-out run leaves behind.
- The benchmark measures `Wp81Theme` against no theme, and counts themed controls through the holes
  list.
- `tools/breakage/run.js` finds the test project itself, so another library can copy it unchanged.
- Not carried over yet: `tools/real-window/drive.ps1`, which drives the desktop sample's real
  window. It waits for the desktop sample
  ([#9](https://github.com/wdcossey/CornflowerUI/issues/9)).
