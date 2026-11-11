"""The slide and continuum page transitions, from a `-Probe pages -Record` run (20261110-0810-dev11).

Usage: python -I slide.py <run folder> [--gray <file>]

After its turnstile turns, the pages probe navigates forward and back with SlideNavigationTransitionInfo, then with
ContinuumNavigationTransitionInfo. Each frame's lit region is measured along its middle row and column (bbox, to
0.1 view px); the recording is put on the app's clock by the turnstile turns, which start a fixed lag after their
steps (a page shows that lag after its step, or up to 0.3 s sooner). Then, from 0.3 s before that to 0.9 s after:

- slide, forward: the new page's top (the blue page, gray level under 200) offset from rest, its distinct values
  (2 px or more, to 0.1, within 0.25 taken as one), the tick each is on, and fits: a pure exponential and an
  exponential ease-out ending at a tick, and an ExponentialEase (6) with a free start over a duration;
- slide, back: the page going out, its offset growing each tick, fitted the same way, read backwards;
- continuum, forward: the new page's scale (width / 412) from the first frame it shows until it is at rest;
- continuum, back: after the black frame, the page under it fading in: the distinct values of 255 - its gray level,
  3 or more (below that, the last levels are rounding), fitted as the slide.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

args = sys.argv[1:]
gray = None
if '--gray' in args:
    i = args.index('--gray')
    gray = args[i + 1]
    del args[i:i + 2]
if len(args) != 1:
    raise SystemExit(__doc__)
run = args[0]
rec = ml.Recording(run, gray)
steps = ml.steps(run)

# The lag from the app's clock to the recording's, from the turnstile turns (as turnstile.py finds them).
segs = ml.segments(ml.outline(rec))
turns = [t for t, d in steps if d.split()[1] in ('default', 'CommonNavigationTransitionInfo')]
lag = ml.median([s[0][1] - t / 1000 for s, t in zip(segs, turns)])
print('recording time = app time %+.3f s (median over %d turnstile turns)' % (lag, len(turns)))


def window(t_ms):
    t0 = t_ms / 1000 + lag
    return [(t, ml.bbox(fr, rec)) for i, t, fr in rec.frames(t0 - 0.3, t0 + 0.9)]


def fits(name, vals):
    r0, ticks = ml.tick_numbers(vals)
    print('  %d distinct values %s' % (len(vals), ','.join('%.1f' % v for v in vals)))
    print('  ticks %s (median ratio %.4f)' % (ticks, r0))
    for eased in (False, True):
        rms, r, N, A, tau, k = ml.fit_exponential(vals, ticks, eased)
        print('  %-6s from %8.3f, x%.4f a tick (time constant %.1f ms)%s, rms %.3f' % (
            'eased' if eased else 'pure', A, r, tau,
            '' if N is None else ', ending at tick %d (%.0f ms), exponent %.2f' % (N, N * ml.TICK, k), rms))
    if name:
        rms, d, ph, A = ml.fit_scaled(vals, ticks, ml.expo_out(6))
        print('  ExponentialEase 6: from %.2f over %d ms, the first value %.1f ticks in, rms %.3f' % (A, d, ph, rms))


def kind(d):
    return d.split()[0], d.split()[1]


rest_top = None
for which in ('forward', 'back'):
    offsets, lasts = [], []
    for t, d in steps:
        if kind(d) != (which, 'SlideNavigationTransitionInfo'):
            continue
        w = window(t)
        rest_top = w[0][1][1]
        run_off = [b[1] - rest_top for _, b in w if b is not None and b[1] is not None and b[4] < 200 and b[1] - rest_top > 0]
        offsets += run_off
        if run_off:
            lasts.append(max(run_off))
    vals = ml.distinct(offsets, decimals=1, tol=0.25, min_abs=2)
    print('slide %s: the page\'s top, %.1f at rest; %s' % (
        which, rest_top, 'first seen' if which == 'forward' else 'last seen'), ', '.join('%.1f' % v for v in lasts), 'px off it')
    fits(which == 'forward', vals)

deficits = []
for t, d in steps:
    which, info = kind(d)
    if info != 'ContinuumNavigationTransitionInfo':
        continue
    w = window(t)
    blank = next((k for k, (_, b) in enumerate(w) if b is None), None)
    if blank is None:
        print('continuum %s at %.3f s: no black frame' % (which, t / 1000 + lag))
        continue
    after = [(tt, b) for tt, b in w[blank:] if b is not None]
    if which == 'forward':
        shown = after[0][0]
        rest = next(tt for tt, b in after if b[0] is not None and b[2] is not None and abs((b[2] - b[0]) - 412) < 0.1)
        scale = (after[0][1][2] - after[0][1][0]) / 412
        fade = [tt for tt, b in w[:blank] if b[4] < 255]
        print('continuum forward: the old page fades for %d frames, black %.0f ms, then the new page grows from %.3f '
              'to 1 in %.0f ms' % (len(fade), (shown - w[blank][0]) * 1000, scale, (rest - shown) * 1000))
    else:
        top = after[-1][1][1]
        deficits += [255 - b[4] for _, b in after if b[1] is not None and abs(b[1] - top) < 0.5 and b[4] < 255]
vals = ml.distinct(deficits, decimals=0, tol=0, min_abs=3)
print('continuum back: the page under it fading in, 255 - its gray level (3 or more: the last two levels are rounding)')
fits(False, vals)
