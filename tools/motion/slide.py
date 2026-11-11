"""The slide and continuum page transitions, from a `-Probe pages -Record` run (20261110-0810-dev11).

Usage: python -I slide.py <run folder> [--gray <file>] [--lag <s>]

After its turnstile turns, the pages probe navigates forward and back with SlideNavigationTransitionInfo, then with
ContinuumNavigationTransitionInfo. Each frame's lit region is measured along its middle row and column (bbox, to 0.1
view px); the recording is put on the app's clock by the turnstile turns, which start a fixed lag after their steps
(motionlib.match_steps; the recording is read only until they are over), or by --lag: recording time = app time -
lag, in s. A page shows that lag after its step, or up to 0.3 s sooner. Rest is the first frame's page. Then, from
0.3 s before that to 0.9 s after (a warning if that window doesn't start at rest):

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

(run,), opts = ml.parse_args(__doc__, options=('--gray', '--lag'))
steps = ml.steps(run)


def kind(d):
    return d.split()[0], d.split()[1]


def fits(vals, curves=()):
    r0, ticks = ml.tick_numbers(vals)
    print('  %d distinct values %s' % (len(vals), ','.join('%.1f' % v for v in vals)))
    print('  ticks %s (median ratio %.4f)' % (ticks, r0))
    ml.print_fits(vals, ticks, curves)


with ml.Recording(run, opts['--gray']) as rec:
    if opts['--lag'] is None:
        # The lag from the app's clock to the recording's, from the turnstile turns (as turnstile.py pairs them),
        # reading the recording only until they are over.
        turns = [t / 1000 for t, d in steps if kind(d)[1] in ('default', 'CommonNavigationTransitionInfo')]
        segs = ml.segments(ml.outline_rows(rec), limit=len(turns) + 2)
        lag, pairs, lags = ml.match_steps([s[0][1] for s in segs], turns, what='turnstile turns')
        print('recording time = app time - %.3f s (median over %d turnstile turns, %.3f to %.3f)'
              % (-lag, len(turns), -max(lags), -min(lags)))
    else:
        lag = -float(opts['--lag'])
        print('recording time = app time - %.3f s (given)' % -lag)

    rest = ml.bbox(rec.frame(0), rec)
    if rest is None or rest[1] is None:
        raise SystemExit('the first frame shows no page, so there is no rest to measure from')
    rest_top = rest[1]

    def window(t_ms):
        t0 = t_ms / 1000 + lag
        w = [(t, ml.bbox(fr, rec)) for i, t, fr in rec.frames(t0 - 0.3, t0 + 0.9)]
        if not w or w[0][1] is None or w[0][1][1] is None or abs(w[0][1][1] - rest_top) > 0.5:
            print('  warning: the window from %.3f s does not start at rest' % (t0 - 0.3))
        return w

    for which in ('forward', 'back'):
        offsets, lasts = [], []
        for t, d in steps:
            if kind(d) != (which, 'SlideNavigationTransitionInfo'):
                continue
            run_off = [b[1] - rest_top for _, b in window(t)
                       if b is not None and b[1] is not None and b[4] < 200 and b[1] - rest_top > 0]
            offsets += run_off
            if run_off:
                lasts.append(max(run_off))
        vals = ml.distinct(offsets, decimals=1, tol=0.25, min_abs=2)
        print("slide %s: the page's top, %.1f at rest; %s" % (
            which, rest_top, 'first seen' if which == 'forward' else 'last seen'), ', '.join('%.1f' % v for v in lasts), 'px off it')
        fits(vals, (('ExponentialEase 6', ml.expo_out(6)),) if which == 'forward' else ())

    deficits = []
    for t, d in steps:
        which, info = kind(d)
        if info != 'ContinuumNavigationTransitionInfo':
            continue
        w = window(t)
        blank = next((k for k, (_, b) in enumerate(w) if b is None), None)
        after = [] if blank is None else [(tt, b) for tt, b in w[blank:] if b is not None and None not in b[:3]]
        if not after:
            print('continuum %s at %.3f s: no black frame, or no page after it' % (which, t / 1000 + lag))
            continue
        if which == 'forward':
            shown = after[0][0]
            at_rest = next((tt for tt, b in after if abs((b[2] - b[0]) - 412) < 0.1), None)
            scale = (after[0][1][2] - after[0][1][0]) / 412
            fade = [tt for tt, b in w[:blank] if b[4] < 255]
            print('continuum forward: the old page fades for %d frames, black %.0f ms, then the new page grows from %.3f '
                  'to 1 in %s ms' % (len(fade), (shown - w[blank][0]) * 1000, scale,
                                    '(not at rest in the window)' if at_rest is None else '%.0f' % ((at_rest - shown) * 1000)))
        else:
            top = after[-1][1][1]
            deficits += [255 - b[4] for _, b in after if abs(b[1] - top) < 0.5 and b[4] < 255]
vals = ml.distinct(deficits, decimals=0, tol=0, min_abs=3)
print('continuum back: the page under it fading in, 255 - its gray level (3 or more: the last two levels are rounding)')
fits(vals)
