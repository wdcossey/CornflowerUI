# ToggleSwitch, from the emulator

The reference app's captures of its three ToggleSwitches ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)):
`off`, `new ToggleSwitch { Header = "toggle switch" }`; `on`, the same with `IsOn = true`; and `no-header`,
`new ToggleSwitch()`. Dark and light, at rest (`base`) and in `CommonStates` `Disabled`, each forced with
`VisualStateManager.GoToState` on a fresh instance. Copied unchanged from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 140 view pixels (1080 × 336 raw) for `off` and `on`, 450 × 110 (1080 × 264) for `no-header`, the page's background; the switch in a slot 19 from the left and right and 12 from the top, centred in it by its style, so at 19.1667, 43.3333, 412.0833 × 65.4167 (19.1667, 30.4167, 412.0833 × 61.6667 without a header) once laid out |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__ToggleSwitch__<variant>__<state>.png`. The template-tree dumps beside
them stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers
read from them are quoted in [`docs/controls/ToggleSwitch/PLAN.md`](../../../docs/controls/ToggleSwitch/PLAN.md) and
written out in `tests/Cornflower.Tests/Controls/ToggleSwitchReadings.cs`.

The run's `CommonStates.Normal` capture is `base`, byte for byte, for each switch in each theme, and isn't copied, nor
are `PointerOver` and `Pressed`, which the template leaves empty. The forced `ToggleStates` and `ContentStates`
captures aren't the control's own states (forcing `Dragging` on an on switch drops `On`'s storyboard, so it draws off
with its on words), so they aren't copied either. Run `20261108-1732-dev11`'s captures of these twelve are byte for
byte these.

The motion (the tap, the drag and the knob's slide) has no capture: it is the touch probes' and the recordings', in
`tools/refapp/README.md`, "Motion".
