# ADR 0001 — What "faithful" means here

**Date:** 2026-11-08 · **Status:** Accepted 2026-11-08; item 1 amended 2026-11-08 and 2026-11-11

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
   | Profile | "Emulator 8.1 U1 1080P 5.5 inch", device index 11 in `AppDeployCmd /EnumerateDevices` |
   | Resolution | 1080×1920 |
   | Memory | 2048 MB |
   | Diagonal | 5.5" |
   | Scale | `RawPixelsPerViewPixel` **2.4**, a **450 × 800** view, read by the reference app on 2026-11-08 |

   **Amended 2026-11-08.** This item first named the WVGA profile (device 8), on the assumption
   that it ran at a scale of 1.0 and so would compare pixel for pixel with our frames. That was
   never read, and the maintainer chose 1080p instead. The reference app then read three profiles:

   | Device | Profile | `RawPixelsPerViewPixel` | View | `ResolutionScale` |
   |---|---|---|---|---|
   | 10 | U1 720P 4.7" | 1.8 | 400 × 711.11 | 180 |
   | **11** | **U1 1080P 5.5"** | **2.4** | **450 × 800** | 180 |
   | 12 | U1 1080P 6" | 2.2 | 490.91 × 872.73 | 180 |

   - **`ResolutionScale` reports 180 on every profile** (corrected 2026-11-11, below), so it isn't the scale. The scale is
     `RawPixelsPerViewPixel`, and the capture PNGs agree with it: a 100-pixel-tall stage is 180,
     240 and 220 pixels tall.
   - **Phones don't all lay out about 400 view pixels wide**, as was assumed: the 1080p profiles
     lay out 450 and 491 wide.
   - **The templates' values are the same in view pixels on all three.** On Button:
     `FontSize` 18.14, `BorderThickness` 2.5, `Padding` 9.5,0,9.5,3.5, `MinWidth` 109. What the
     profile changes is how layout rounds to the raw pixel grid (the same Button is 108.89, 109.17
     and 109.09 wide) and the view's width, which only a layout that stretches can see.

   Of the two 1080p profiles, device 11 is the reference: its view is whole numbers, so a test
   window can match it exactly, and it has the most raw pixels per view pixel. The 720p captures
   (device 10) and the device 12 captures are kept as second readings, not as the reference.

   **Amended 2026-11-11.** The reference app read the display through the API on all seven
   Update 1 profiles (`run.ps1 -Native`, PR #72; the readings and run folders are in
   [`tools/refapp/README.md`, Native readings](../../tools/refapp/README.md#native-readings-2026-11-11-devices-7-to-13)).
   The amendment above left WVGA's scale unread, and said `ResolutionScale` is 180 on every
   profile. Both are now read:

   | Device | Profile | `RawPixelsPerViewPixel` | View | `ResolutionScale` |
   |---|---|---|---|---|
   | 7, 8 | U1 WVGA 4" | 1.2 | 400 × 666.67 | 140 |
   | 9 | U1 WXGA 4.5" | 2 | 384 × 640 | 180 |
   | 10 | U1 720P 4.7" | 1.8 | 400 × 711.11 | 180 |
   | **11** | **U1 1080P 5.5"** | **2.4** | **450 × 800** | 180 |
   | 12 | U1 1080P 6" | 2.2 | 490.91 × 872.73 | 180 |
   | 13 | U1 qHD 5" | 1.2 | 450 × 800 | 140 |

   - **WVGA runs at 1.2, not 1.0**: its 480 × 800 pixels are a 400 × 666.67 view. So the first
     choice, WVGA, wouldn't have compared pixel for pixel with our frames either. qHD runs at 1.2
     too: its 540 × 960 pixels are the reference's 450 × 800 view.
   - **`ResolutionScale` is 140 on the three profiles at 1.2** (devices 7 and 8, WVGA, and 13,
     qHD), and 180 on the rest. It still isn't the scale; `RawPixelsPerViewPixel` is.

   The reference stays device 11.

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
