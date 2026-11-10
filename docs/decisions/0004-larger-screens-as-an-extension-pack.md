# ADR 0004 — Larger screens as an extension pack

**Date:** 2026-11-10 · **Status:** Accepted: the first table is the maintainer's decision of 2026-11-09; the
second table was proposed with it and is accepted when the maintainer merges the PR that adds this ADR
([#61](https://github.com/wdcossey/CornflowerUI/pull/61)).

## Context

Windows Phone 8.1 had no layouts for wide screens: no breakpoints, no size classes, and nothing on the phone to
measure a desktop or tablet window against. Until now the roadmap said only that desktop "works but isn't the
priority", and it planned Windows 10 desktop as a third era.

On 2026-11-09 the maintainer said that desktop and mobile are both priorities, and asked whether an extension
pack over the core components would be better than building wide layouts into them. Breakpoints and layouts could
be borrowed from Windows 10 or from Avonic. The question was put on the working site as `D-desktop-extension`, and
the maintainer chose the pack the same evening. That option read: a separate package over the core theme,
breakpoints from Windows 10, the mechanism from Avonic's `GridViewport`, and a core that promises only hooks
(overridable resources, no phone widths baked into templates), with the desktop sample moved up. They also chose
`D-tier1-start: type-ramp`: the type ramp ([#26](https://github.com/wdcossey/CornflowerUI/issues/26)) names its
resources so that the pack can override them.

Whatever Cornflower does with a wide window is its own design, not a reproduction. [ADR 0001](0001-what-faithful-means.md)
keeps "faithful" for what the phone did, and [ADR 0002](0002-decisions-of-2026-11-08.md#themes-and-scope) already
keeps the controls the phone never had apart from it, as plain themes labelled as Cornflower's own. This ADR does
the same for screen sizes.

## Decision

### Decided by the maintainer (`D-desktop-extension: pack`, 2026-11-09)

| Question | Decision | Why |
|---|---|---|
| Where do wide layouts live? | **In a separate package over the core theme**, `Cornflower.Adaptive` as a working name, settled in its first PR. An app that wants it adds it after `Wp81Theme`. Its documentation and package description say that it is Cornflower's design, not the phone's | The core stays the phone, measured and tested against captures. The invented part sits apart from the measured one, where a reviewer can't mistake one for the other |
| Where do the breakpoints come from? | **Windows 10's size classes**, as minimum widths in DIPs: small from 0, medium from 641, large from 1008. A width belongs to the largest class whose minimum it reaches, so 640.38 is small | Windows 10 is the same design family and the nearest published set. Its effective pixel and Avalonia's DIP are both 1/96 inch at 100%, so the widths carry over unconverted. Minimums, not ranges, leave no fractional width unassigned |
| How does a window learn its size class? | **Avonic's mechanism**, ported library-neutral: one viewport per `TopLevel`, read from its `ClientSize` width, with a popup using the `TopLevel` it belongs to, and a change raised only when a breakpoint is crossed. Ported from `src/Avonic.Components/Controls/Grid/GridViewport.cs` (Avonic `ff0bc4b`) | Avonic has tested this on Avalonia. A resize that doesn't cross a breakpoint costs nothing. It reads the window, not the parent, so a desktop window and a phone screen behave the same way, and two windows of one app can be in different classes |
| What does the core promise? | **Only hooks:** (1) what a pack may change, starting with the type ramp, is a named resource, reached by `DynamicResource`, never a literal in a template; #26 names the type ramp's. (2) No fixed phone width, in any unit (450 or 480 DIP, 1080 raw pixels), is written into a template or a style: a page, an app bar, a dialog or a flyout takes its width from the window it is in. Anything more, breakpoints included, is the pack's | A `StaticResource` is resolved when `Wp81Theme`'s dictionary loads, from its own tokens, so a pack's key would never be seen: only a `DynamicResource` is a hook. A promise the core doesn't make can't break the core's fidelity |
| The desktop sample ([#9](https://github.com/wdcossey/CornflowerUI/issues/9)) | **Moves up** | The pack needs a window to be seen in, and desktop is now a priority |

### Proposed with this ADR (accepted on merge)

| Question | Proposal | Why |
|---|---|---|
| How does a size class reach a control the pack doesn't own? | **Resources first:** on a change, the pack puts the class's values into that `TopLevel`'s own `Resources`, never `Application.Resources`, so each window gets its own. **Pseudo-classes only where a value isn't enough** (a different layout, not a different size): the pack sets `:breakpoint-medium` and `:breakpoint-large`, cumulative as in Avonic, on the controls it styles, through `IPseudoClasses` (which `Classes` implements publicly), from an attached behaviour in its own `ControlTheme`s. The pack's first PR proves both with a spike, on a stock `TextBlock` and a `FlyoutPresenter` in two windows of different widths | Avonic's controls set their pseudo-classes on themselves, but most of what Cornflower themes is Avalonia's own controls, which it doesn't subclass. A resource needs no rule at all; a pseudo-class set from the pack's own theme adds no global rule to the core |
| What do the hooks cost the core? | **Only the `DynamicResource` lookups on hooked values.** #26 measures them on the type ramp against `StaticResource` (with the benchmark in [`docs/performance`](../performance/README.md)) and reports the cost in its PR. If the cost is more than the budget allows, it goes to the maintainer as a question, rather than quietly turning the hooks into `StaticResource` | A phone-only app pays for the hooks even without the pack: that cost is the price of the maintainer's choice, so it is measured and shown, not assumed to be zero |
| What does the pack's own styling cost? | **Nothing in the core's style budget.** The pack has a budget of its own, measured the same way, and its rules go in the `ControlTheme`s it overrides, not in global rules, wherever it can | An app that adds the pack should see what the pack costs, apart from the core |
| Checking the breakpoints | The pack's first PR checks 641 and 1008 against Microsoft's current "Screen sizes and breakpoints" page and against the defaults of a Windows 10 control with built-in thresholds (`NavigationView`'s `CompactModeThresholdWidth` and `ExpandedModeThresholdWidth`), and records both sources | Microsoft's guidance has changed over the years. The 14393 Windows 10 Mobile emulators on the reference VM are phones, 320 to 720 wide, so they can't show a medium or large class |
| When does the pack's work start? | **After Tier 2, the shell** (page, app bar, Pivot, Hub, dialogs, flyouts). Before then, the only work is the core's hooks, starting with #26 | The shell is where width matters most, and the pack has nothing to override before it exists. The hooks cost little if they are kept from the start and a lot if they are added later |
| Where #9 goes | **#9 stays an optional Tier 0 item that doesn't hold up Tier 1**, as the roadmap already says. Moving up means it is the next piece of work after #26, not a gate. It is still a thin head over `Cornflower.Sample`, phone-sized when it opens and resizable, so a wide window can be seen | `D-tier1-start` lets Tier 1 begin with #26 once the other Tier 0 items are open and green; making #9 a gate would hold that up |
| How a control keeps the hooks | A control's plan names the sizes a pack may override and their resource keys. Its tests include one in a window wider than the reference profile, where the control must not overflow or leave a fixed-width gap. That screenshot is evidence that the hooks are kept, not a claim of fidelity | Hook (2) is easy to break without noticing at the reference size |

## Consequences

- The roadmap's scope gains the pack as a labelled Cornflower extension, and #9's note changes from "a
  nice-to-have" to "next after #26, not a gate". `docs/ARCHITECTURE.md` lists the pack in its layout as planned,
  states the two hooks, and describes #9 as resizable. The next docs roll-up makes these edits. Until it merges,
  this ADR is the newer statement where they disagree.
- The phone remains the only reference for the core.
- Era 3, Windows 10 desktop, is unchanged. If it comes, it is a faithful era of its own, measured against Windows 10,
  and the pack's breakpoints may then be checked against it.
