# ADR 0001 — What "faithful" means here

**Date:** 2026-11-08 · **Status:** Accepted 2026-11-08

## Context

Cornflower reproduces the Windows Phone 8.1 design language for AvaloniaUI. The reference is a
closed platform from 2014. Its templates and API metadata can be read. Its control source can't.
Its fonts and icons can't be shipped. It runs only in an emulator.

The first version of Avonic, Cornflower's sibling project, failed because its founding decisions
contradicted "faithful" and were never revisited
([`docs/LESSONS_LEARNED.md`](https://github.com/wdcossey/Avonic/blob/v2/docs/LESSONS_LEARNED.md)
§1.4 in Avonic). So this is settled in writing before any code.

## Decision

1. **The reference for an era is one named build, in one named emulator profile, at one
   resolution and scale.** For the first era:

   | | |
   |---|---|
   | Build | Windows Phone 8.1 Update 1 (emulator package 8.10.14141.167) |
   | Emulator image | `flash.update1.vhd`, dated 2014-07-15 |
   | Profile | "Emulator 8.1 U1 WVGA 4 inch", device index 8 in `AppDeployCmd /EnumerateDevices` |
   | Resolution | 480×800 |
   | Memory | 1024 MB |
   | Diagonal | 4" |
   | Resolution scale | **Not read yet.** It was taken to be 1.0 when the profile was chosen; that was never checked. Windows Phone 8.1 is thought to lay out every phone class about 400 view pixels wide, which would put this profile at 1.2 (unconfirmed). The reference app reads `DisplayInformation.ResolutionScale` and `RawPixelsPerViewPixel` on its first run on this profile |

   This profile was chosen over "Emulator 8.1 U1 720P 4.7 inch" (device index 10), which made
   the first 504 captures. That profile reads `ResolutionScale` 180: 1.8 raw pixels per view
   pixel, a 400 × 711.11 view. The reason was that at a scale of 1.0 a headless frame from our tests
   and a capture from the emulator would compare pixel for pixel, with no resampling. That reason
   holds only if the scale is 1.0. **This item is provisional:** if the first run on this profile reads
   any other scale, the choice of profile is reopened and this item amended.

2. **Geometry, colour, motion and behaviour are reproduced and measured.** Every number in a
   control's plan comes from the templates or from a capture, with its source beside it.

3. **Fonts and icons are substituted, and that is a recorded deviation, not a gap.** Text is
   Selawik. Measured against Segoe WP, the phone's own font, it is exact at Light, within 0.3% on
   letters at Semilight, Regular and Bold, and 1.5 to 1.8% wide at Semibold; digits are 1.5 to
   4.1% wide at Semilight and above; its `hhea` line is 9.8% short. Template glyphs are our own
   drawings, matched to captures. On Windows, where Segoe is installed, the font resource may
   point at it. No Microsoft font ships.

4. **Behaviour comes from measurement first.** The phone's control source was never published.
   WinUI 3's source and Uno Platform's ports are hints about how a control is built, not evidence
   of what the phone did.

5. **A difference Avalonia forces may stand** if it's recorded with its measurement and reason,
   as in Avonic's
   [ADR 0002](https://github.com/wdcossey/Avonic/blob/v2/docs/decisions/0002-decisions-from-the-first-swarm.md).
   It isn't a licence to skip measuring.

6. **Names follow WinRT** for new controls and resource keys. For Avalonia's own controls, the
   API is Avalonia's, and a WinRT member Avalonia lacks is either added as a labelled extension
   or recorded as missing. Where a control must be a subclass of Avalonia's, it keeps the WinRT
   name ([ADR 0002](0002-decisions-of-2026-11-08.md#the-shape-of-a-control);
   [architecture](../ARCHITECTURE.md#the-limit-of-a-theme)).

## Consequences

- Nothing is "done" without a capture from the emulator beside it.
- A pixel comparison will never be exact where text is involved. Comparisons state the font used
  on each side.
- Captures that are compared against our frames come from the profile in item 1. A capture from
  another profile says which one it is.
- The second era can't start until the first has controls at depth. Avonic's lesson: 27
  controls at 25 to 65% were worth less than 5 at 95%.
