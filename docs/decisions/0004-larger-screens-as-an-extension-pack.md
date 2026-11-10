# ADR 0004 — Larger screens as an extension pack

**Date:** 2026-11-10 · **Status:** Proposed in [#61](https://github.com/wdcossey/CornflowerUI/pull/61). The first
table is the maintainer's decision of 2026-11-09, in the words of the option they chose. The second table is what
this ADR proposes on top of it, and is accepted when the maintainer merges #61.

## Context

Windows Phone 8.1 had no layouts for wide screens: no breakpoints, no size classes, and nothing on the phone to
measure a desktop or tablet window against. Until now the roadmap said only that desktop "works but isn't the
priority", and it planned Windows 10 desktop as a third era.

On 2026-11-09 the maintainer said that desktop and mobile are both priorities, and asked whether an extension
pack over the core components would be better than building wide layouts into them. Breakpoints and layouts could
be borrowed from Windows 10 or from Avonic. The question was put on the working site as `D-desktop-extension`, and
the maintainer chose the pack the same evening. That option read: a separate package over the core theme, with
breakpoints from Windows 10's three size classes (the exact widths to be checked), the mechanism from Avonic
(cumulative `:breakpoint-*` pseudo-classes set from the window's width, by `GridViewport`), the pack overriding the
type ramp and the shell controls at the wider classes, and a core that promises only hooks (overridable resources,
no phone widths baked into templates), with the desktop sample moved up. They also chose `D-tier1-start:
type-ramp`: the type ramp ([#26](https://github.com/wdcossey/CornflowerUI/issues/26)) names its resources so that
the pack can override them.

Whatever Cornflower does with a wide window is its own design, not a reproduction. [ADR 0001](0001-what-faithful-means.md)
keeps "faithful" for what the phone did, and [ADR 0002](0002-decisions-of-2026-11-08.md#themes-and-scope) already
keeps the controls the phone never had apart from it, as plain themes labelled as Cornflower's own. This ADR does
the same for screen sizes.

## Decision

### Decided by the maintainer (`D-desktop-extension: pack`, 2026-11-09)

| Question | Decision |
|---|---|
| Where do wide layouts live? | **In a separate package over the core theme**, `Cornflower.Adaptive` as a working name. The core stays the phone, faithful and measured; the pack is labelled as Cornflower's own |
| Where do the breakpoints come from? | **Windows 10's three size classes**, the exact widths to be checked |
| How does a window learn its size class? | **Avonic's mechanism:** `GridViewport`, with cumulative `:breakpoint-*` pseudo-classes set from the window's width |
| What does the pack override? | The type ramp and the shell controls (page, app bar, Pivot, Hub, dialogs, flyouts) at the wider classes |
| What does the core promise? | **Only hooks:** overridable resources, and no phone widths baked into templates |
| The desktop sample ([#9](https://github.com/wdcossey/CornflowerUI/issues/9)) | **Moves up**, so the pack has a window to be seen in |

### Proposed with this ADR (accepted on merge)

| Question | Proposal | Why |
|---|---|---|
| The widths | **Minimum widths in DIPs:** small from 0, medium from 641, large from 1008. A width belongs to the largest class whose minimum it reaches, so 640.38 is small | Windows 10's effective pixel and Avalonia's DIP are both 1/96 inch at 100%, so the widths carry over unconverted. Minimums, not ranges, leave no fractional width unassigned |
| A phone in landscape | **A phone stays in the small class whatever its orientation.** The reference phone is 800 DIP wide in landscape and a Pixel 8 about 914, so by width alone both would be medium, and the pack would replace the phone's own landscape layout (the core's orientation states, `FlyoutPresenter`'s, `ContentDialogContentLandscapeWidth`). How the pack tells a phone from a narrow desktop window (the window's shorter side, or the platform) is settled in its first PR | The core is the phone, landscape included; the pack is for screens the phone never had |
| What counts as a hook | **(1)** A value a pack may change is a named resource reached by `DynamicResource`. A `StaticResource` is resolved when `Wp81Theme`'s dictionary loads, from its own tokens, so a pack's key would never be seen. **(2)** No phone width, in any unit (450 or 480 DIP, 1080 raw pixels), is written into a template or a style as a literal. The phone's own measured widths stay: they are tokens (`ContentDialogContentLandscapeWidth`, 400), reached by `DynamicResource`, so each is itself a hook a pack may change | This keeps the phone's measured sizes faithful and still lets a pack change them |
| How the window's class is followed | **Ported from Avonic's `GridViewport`** (`src/Avonic.Components/Controls/Grid/GridViewport.cs`, Avonic `ff0bc4b`), library-neutral: one viewport per `TopLevel`, read from its `ClientSize`, with a popup using the `TopLevel` it belongs to. **The class is applied when the pack attaches to a window, as well as when a breakpoint is crossed.** `GridViewport` sets its first class silently in its constructor and raises `Changed` only on a crossing, so a window that opens at 1200 would otherwise keep the phone's sizes until it was resized | Avonic has tested this on Avalonia. A resize that doesn't cross a breakpoint costs nothing. It reads the window, not the parent, so two windows of one app can be in different classes |
| One size watcher per window | The core already plans an orientation pseudo-class set from the `TopLevel`'s size (ARCHITECTURE, "Orientation states"). The pack's first PR decides whether one viewport, in `Cornflower.Controls`, reports both orientation and size class, so that a window has one watcher and the two can't disagree | Two watchers on the same `ClientSize` are two places to get a resize wrong |
| How a size class reaches a control the pack doesn't own | **A change from the chosen option: resources first, pseudo-classes second.** The option named pseudo-classes as the mechanism. This proposes that the pack mostly changes resources, and sets pseudo-classes only where a value isn't enough (a different layout, not a different size). Resources: the pack keeps one dictionary of its own, merged into each `TopLevel`'s `Resources` and never into `Application.Resources`, so each window gets its own. Pseudo-classes: `:breakpoint-medium` and `:breakpoint-large`, cumulative as in Avonic, on the controls the pack styles, from an attached behaviour in its own `ControlTheme`s, through `IPseudoClasses`. `Classes` implements that interface explicitly, so it takes a cast, as in Avonic (`((IPseudoClasses)control.Classes)`); `Classes.Add` refuses a pseudo-class. The pack's first PR proves both with a spike, on a stock `TextBlock` and a `FlyoutPresenter` in two windows of different widths | Avonic's controls set their pseudo-classes on themselves, but most of what Cornflower themes is Avalonia's own controls, which it doesn't subclass, so a pseudo-class needs a behaviour on each one. A resource needs no rule at all. If the maintainer wants pseudo-classes as the main channel after all, this row changes before merge |
| An app's own overrides | A `DynamicResource` finds a key in the window's `Resources` before `Application.Resources`, so the pack's values would hide an app's override there, the way an app sets `SystemColorControlAccentColor`. **The pack doesn't put a key into a window that the app has set in `Application.Resources` or in that window's own `Resources`.** An app that wants a value per size class sets it through the pack, by an API its first PR designs | An app's override has to win, at every size |
| What do the hooks cost the core? | **Only the `DynamicResource` lookups on hooked values.** #26 adds a benchmark that compares the ways a value is reached (`StaticResource`, `DynamicResource`), since the existing `StyleCostBenchmark` in [`docs/performance`](../performance/README.md) compares a theme against none. #26 reports the cost in its PR. If the cost is more than the budget allows, it goes to the maintainer as a question, rather than quietly turning the hooks into `StaticResource` | A phone-only app pays for the hooks even without the pack: that cost is the price of the maintainer's choice, so it is measured and shown, not assumed to be zero |
| What does the pack's own styling cost? | **Nothing in the core's style budget.** The pack has a budget of its own, measured the same way, and its rules go in the `ControlTheme`s it overrides, not in global rules, wherever it can | An app that adds the pack should see what the pack costs, apart from the core |
| Checking the breakpoints | The pack's first PR checks 641 and 1008 against Microsoft's current "Screen sizes and breakpoints" page, and against `NavigationView`'s `CompactModeThresholdWidth` and `ExpandedModeThresholdWidth` defaults, read from Microsoft's documentation: `NavigationView` came with Windows 10 1709 (16299), after the SDKs on the reference VM (10240, 10586, 14393). It records both sources. The 14393 Windows 10 Mobile emulators haven't been run yet. In landscape a 1080p profile may reach 641, so the PR runs one to see whether a Windows 10 phone reaches the medium class there, which bears on the landscape row above | Microsoft's guidance has changed over the years, and the VM can only check what its SDKs have |
| When does the pack's work start? | **After Tier 2, the shell** (page, app bar, Pivot, Hub, dialogs, flyouts). Before then, the only work is the core's hooks, starting with #26 | The shell is where width matters most, and the pack has nothing to override before it exists. The hooks cost little if they are kept from the start and a lot if they are added later |
| Where #9 goes | **#9 stays an optional Tier 0 item that doesn't hold up Tier 1**, as the roadmap already says. Moving up means it is the next piece of work after #26, not a gate. It is still a thin head over `Cornflower.Sample`, phone-sized when it opens and resizable, so a wide window can be seen | `D-tier1-start` lets Tier 1 begin with #26 once the other Tier 0 items are open and green; making #9 a gate would hold that up |
| How a control keeps the hooks | A control's plan names the sizes a pack may override and their resource keys. Its tests include one in a window wider than the reference profile, where the control must not overflow or leave a fixed-width gap. That screenshot is evidence that the hooks are kept, not a claim of fidelity | Hook (2) is easy to break without noticing at the reference size |

## Consequences

- The roadmap's scope gains the pack as a labelled Cornflower extension, and its "In, first" row stops saying that
  desktop "isn't the priority": mobile and desktop are both priorities. #9's note changes from "a nice-to-have" to
  "next after #26, not a gate". `docs/ARCHITECTURE.md` lists the pack in its layout as planned, states the two
  hooks, and describes #9 as resizable. The next docs roll-up makes these edits. Until it merges, this ADR is the
  newer statement where they disagree.
- The phone remains the only reference for the core, in portrait and in landscape.
- Era 3, Windows 10 desktop, is unchanged. If it comes, it is a faithful era of its own, measured against Windows 10,
  and the pack's breakpoints may then be checked against it.
