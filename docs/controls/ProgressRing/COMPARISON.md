# ProgressRing: comparison

**The phone:** the reference app's captures, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4,
2026-11-08, in [`ref/wp81/ProgressRing/`](../../../ref/wp81/ProgressRing/README.md): an active ring stretched to its slot.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/ProgressRingFrames.cs`, Avalonia 12.1.4 with Skia, on
the same stage: 450 × 120 at 2.4, the ring in a slot 19 in and 12 down, in [`frames/`](frames). No text on either.

**Each capture is a moment of the running storyboard**, so ours is drawn at the same moment. The moment is fitted from
the capture's dots: the storyboard time at which the template's keyframes put the visible dots where the picture
has them (the dots found as accent pixels, each dot's angle about `Ring`'s centre). Ours is drawn at the nearest 16 ms
frame:

| Capture | Dots | The phone's moment | Ours drawn at |
|---|---|---|---|
| `base` | 1 | 0.1262 s (dark), 0.1267 s (light); the angle within 0.01° | 128 ms |
| `SizeStates.Large` | 4 | 0.6099 s (dark), 0.6102 s (light); within 0.40° | 608 ms |
| `SizeStates.Small` | 4 | 0.5945 s (dark), 0.6102 s (light); within 0.39° | 592 ms |
| `ActiveStates.Active` | 0: the forced state's storyboard hadn't drawn | | 0 ms: the first dot, at −110° |
| `ActiveStates.Inactive` | 0 | | inactive |

The phone's ring is `Large` in every picture (its smaller side is 107.92). Its forced `Small` state only collapses the
sixth dot, which hasn't begun by 0.61 s, so the two `SizeStates` pictures differ only in their moment. Ours is drawn as
the ring is, `Large`.

## Dark

| State | Phone | Ours |
|---|---|---|
| `base` | ![](../../../ref/wp81/ProgressRing/dark__ProgressRing__active__base.png) | ![](frames/dark__ProgressRing__active__base.png) |
| `SizeStates.Large` | ![](../../../ref/wp81/ProgressRing/dark__ProgressRing__active__SizeStates.Large.png) | ![](frames/dark__ProgressRing__active__SizeStates.Large.png) |
| `SizeStates.Small` | ![](../../../ref/wp81/ProgressRing/dark__ProgressRing__active__SizeStates.Small.png) | ![](frames/dark__ProgressRing__active__SizeStates.Small.png) |
| `ActiveStates.Active` | ![](../../../ref/wp81/ProgressRing/dark__ProgressRing__active__ActiveStates.Active.png) | ![](frames/dark__ProgressRing__active__ActiveStates.Active.png) |
| `ActiveStates.Inactive` | ![](../../../ref/wp81/ProgressRing/dark__ProgressRing__active__ActiveStates.Inactive.png) | ![](frames/dark__ProgressRing__active__ActiveStates.Inactive.png) |

## Light

| State | Phone | Ours |
|---|---|---|
| `base` | ![](../../../ref/wp81/ProgressRing/light__ProgressRing__active__base.png) | ![](frames/light__ProgressRing__active__base.png) |
| `SizeStates.Large` | ![](../../../ref/wp81/ProgressRing/light__ProgressRing__active__SizeStates.Large.png) | ![](frames/light__ProgressRing__active__SizeStates.Large.png) |
| `SizeStates.Small` | ![](../../../ref/wp81/ProgressRing/light__ProgressRing__active__SizeStates.Small.png) | ![](frames/light__ProgressRing__active__SizeStates.Small.png) |
| `ActiveStates.Active` | ![](../../../ref/wp81/ProgressRing/light__ProgressRing__active__ActiveStates.Active.png) | ![](frames/light__ProgressRing__active__ActiveStates.Active.png) |
| `ActiveStates.Inactive` | ![](../../../ref/wp81/ProgressRing/light__ProgressRing__active__ActiveStates.Inactive.png) | ![](frames/light__ProgressRing__active__ActiveStates.Inactive.png) |

## Measurements

In view pixels (DIP) unless raw pixels (px) are said. The phone's numbers are the dumps' and the pictures'; ours are
`Bounds` and our frames, measured the same way.

| | Phone | Ours | |
|---|---|---|---|
| The ring on the stage | 46, 29 px; 989 × 259 px | 46, 29 px; 988 × 259 px | **gap**: 1 px narrower. Avalonia rounds the slot's two margins, 45.6 px each, to 46; the phone rounds the ring's edges, to 46 and 1035 |
| `Ring` in the ring | 425, 61 px; 57.5 square | 425, 60 px; 57.5 square | **gap**: the 60.5 px left over down the ring rounds to 61 on the phone and 60 in Avalonia, 0.42 DIP |
| A dot at rest | 7.5, 7.5 in `Ring`; 6.5 across; opacity 0 | the same | **match**, with layout rounding off on the dots (Avalonia would make them 16 px, 6.67) |
| A dot turned −0.5973° | box 7.28, 7.65 in `Ring`, 6.567 across | within 0.01 | **match** |
| The dots' distance from the centre, in the pictures | 61.6 to 61.7 px | 60.0 to 62.1 px, about a centre half a pixel to the left and one up (the two gaps above) | **match** within the gaps |
| Each picture's moment, refitted from our own frame | | 133, 611, 596, 0.3 ms, against 128, 608, 592, 0 drawn | **match**: the fit's own error, 1 to 5 ms |
| The overlay | the background at 0.8 active, 0 inactive | the same | **match** |
| The dots' colour | `#FF3E65FF`, the accent | the same | **match** |
| The background | `#FF000000` / `#FFFFFFFF` | the same | **match** |
| `TemplateSettings` | #72's three sizes | the same, to 0.01 | **match** |
| The size state | `Large` over 60 on the smaller side | the same at the 208 probe steps | **match** |
| The storyboard | six moments the dumps caught, up to five dots at once | each is one time in ours, within 0.053° | **match** |
| The cycle | the keyframes' 4.305 s (the emulator shows 4.13 s on its fast clock, B6) | 4.305 s | **match**, against the keyframes |

## Not compared

- **The motion against a recording.** The `ring` probe's recording (`20261110-0829-dev11`) is at the emulator's
  window's zoom, 0.83 DIP a pixel, and on the emulator's fast clock. The captures' moments and the dumps' angles check
  the same storyboard more finely.
- **Another profile's threshold.** The 60 is the reference profile's, at scale 2.4.
