# ADR 0004 — Larger screens as an extension pack

**Date:** 2026-11-10 · **Status:** Accepted

## Context

Windows Phone 8.1 had no layouts for wide screens: no breakpoints, no size classes, and nothing on the phone to
measure a desktop or tablet window against. Until now the roadmap said only that desktop "works but isn't the
priority", and it planned Windows 10 desktop as a third era.

On 2026-11-09 the maintainer said that desktop and mobile are both priorities, and asked whether an extension
pack over the core components would be better than building wide layouts into them. Breakpoints and layouts could
be borrowed from Windows 10 or from Avonic. The question was put on the working site as `D-desktop-extension`, and
the maintainer chose the pack the same evening. They also chose `D-tier1-start: type-ramp`: the type ramp
([#26](https://github.com/wdcossey/CornflowerUI/issues/26)) names its resources so that the pack can override them.

Whatever Cornflower does with a wide window is its own design, not a reproduction. [ADR 0001](0001-what-faithful-means.md)
keeps "faithful" for what the phone did, and [ADR 0002](0002-decisions-of-2026-11-08.md#themes-and-scope) already
keeps the controls the phone never had apart from it, as plain themes labelled as Cornflower's own. This ADR does
the same for screen sizes.

## Decision

| Question | Decision | Why |
|---|---|---|
| Where do wide layouts live? | **In a separate package over the core theme**, `Cornflower.Adaptive` as a working name, settled in its first PR. An app that wants it adds it after `Wp81Theme`. Its documentation and package description say that it is Cornflower's design, not the phone's | The core stays the phone, measured and tested against captures. The invented part sits apart from the measured one, where a reviewer can't mistake one for the other. An app that only targets phones pays nothing for it |
| Where do the breakpoints come from? | **Windows 10's size classes**, in effective pixels: small up to 640, medium from 641 to 1007, large from 1008. The pack's first PR checks these against Microsoft's current "Screen sizes and breakpoints" page and the 14393 emulators on the reference VM, and records the source | Windows 10 is the same design family and the nearest published set. Its effective pixel is close to Avalonia's DIP, so the widths carry over without conversion. Microsoft's guidance has changed over the years, so the numbers are checked before they are used, not copied from memory |
| How does a control learn its size class? | **Avonic's mechanism**, ported library-neutral: one viewport per `TopLevel`, read from its `ClientSize` width, with a popup using the `TopLevel` it belongs to, and raising a change only when a breakpoint is crossed. Classes are cumulative, from each breakpoint up (`:breakpoint-medium`, `:breakpoint-large`), so that a later rule wins, as a later `@media (min-width)` does in CSS. Ported from `src/Avonic.Components/Controls/Grid/GridViewport.cs` (Avonic `ff0bc4b`) | Avonic has measured and tested this on Avalonia. A resize that doesn't cross a breakpoint costs nothing. It reads the window, not the parent, so a desktop window and a phone screen behave the same way |
| What does the core promise? | **Only hooks.** (1) Sizes the pack may change, starting with the type ramp, are named resources and not literals in templates; #26 names them. (2) No phone width (450, 480, 1080 raw pixels) is written into a template or a style: a page, an app bar, a dialog or a flyout takes its width from the window it is in. (3) Each hook a control offers is named in its plan, and a test holds it. Anything more, breakpoints included, is the pack's | The pack can then override resources and add its own rules without forking a template. A promise the core doesn't make can't break the core's fidelity |
| How is a resource reached when the pack may change it at run time? | **Decided per hook, with a measurement.** A size that changes when the window crosses a breakpoint needs a `DynamicResource` or a pack rule on the control's theme. #26 measures what a `DynamicResource` costs against a `StaticResource` on the type ramp, and chooses | The style budget ([`docs/performance`](../performance/README.md)) counts global rules, and a resource lookup is not one, but neither is free. The type ramp is the first case and the cheapest to measure |
| What does the pack's styling cost the core? | **Nothing in the core's budget.** The pack has its own style budget, measured the same way. Its rules go in the `ControlTheme`s it overrides, not in global rules, wherever it can | An app that doesn't add the pack shouldn't pay for it, and an app that does should see what it costs |
| When does the work start? | **After Tier 2, the shell** (page, app bar, Pivot, Hub, dialogs, flyouts). Those are the controls where width matters most, and the pack has nothing to change before they exist. Before then, the only work is the core's hooks, starting with #26 | The pack overrides the shell, so the shell comes first. The hooks cost little if they are kept from the start and a lot if they are added later |
| The desktop sample ([#9](https://github.com/wdcossey/CornflowerUI/issues/9)) | **Moves up.** It stays optional in Tier 0, and comes straight after #26 rather than "later" | The pack needs a window to be seen in, and desktop is now a priority. It is still a thin head over `Cornflower.Sample`, phone-sized by default, and resizable |

## Consequences

- The roadmap's scope gains the pack as a labelled Cornflower extension, and its Tier 0 note on #9 changes from
  "a nice-to-have" to "next after #26". `docs/ARCHITECTURE.md` lists the pack in its layout as planned, and states
  the three hooks. The docs roll-up of 2026-11-10 makes these edits.
- A control's plan names the sizes a pack may override, and says how they are reached. Its comparison checks that
  no phone width is baked in: the control is also tested in a window wider than the reference profile, and must
  not overflow or leave a fixed-width gap.
- The phone remains the only reference for the core. A wide-window screenshot in a core PR is evidence that the
  core keeps its hooks, not a claim of fidelity.
- Era 3, Windows 10 desktop, is unchanged. If it comes, it is a faithful era of its own, measured against Windows 10,
  and the pack's breakpoints may then be checked against it.
