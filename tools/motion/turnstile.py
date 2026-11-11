"""The page turnstile, from a `-Probe pages -Record` run (20261110-0810-dev11; 20261110-0748-dev11 has fewer turns).

Usage: python -I turnstile.py <run folder> [--gray <file>] [--axis <x> <z>] [--no-search]

The pages probe navigates a Frame between two pages, a white and a blue sheet inset 19, 60, forward and back with
the default transition and CommonNavigationTransitionInfo (the turnstile), then the slide and continuum. The sheet's
outline is measured in every frame; each run of frames off rest is one transition, the first 2n of them the n
forward and n back turns. Each frame is solved for a rotation about one vertical axis, with the camera of the
projections probe (1000 view px, vanishing point at the element's centre, 225, 400, as the 2026-11-10 fits used it).

Prints:
- the axis that fits every unclipped frame of the forward turns best (skipped with --no-search; it takes minutes),
  and the rms at the published axis, x -44 z -99 (or --axis);
- each forward turn's first angle;
- the distinct angles of the forward and back turns (|angle| >= 0.9, to 0.1, within 0.25 taken as one), the tick of
  the 64 Hz clock each is on, and fits to them: a pure exponential and an exponential ease-out ending at a tick
  (the ratio a tick, the time constant, the end and the exponent), and ExponentialEase curves with a free start
  value over a duration.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

D, CX = 1000.0, 225.0
args = sys.argv[1:]
gray, axis, search = None, (-44.0, -99.0), True
if '--gray' in args:
    i = args.index('--gray')
    gray = args[i + 1]
    del args[i:i + 2]
if '--axis' in args:
    i = args.index('--axis')
    axis = (float(args[i + 1]), float(args[i + 2]))
    del args[i:i + 3]
if '--no-search' in args:
    args.remove('--no-search')
    search = False
if len(args) != 1:
    raise SystemExit(__doc__)
run = args[0]

rows = []
for i, t, m, level in ml.outline(ml.Recording(run, gray)):
    if m[2] is None:  # clipped: keep an edge only if it is on the screen
        m = [m[0] if m[0] > 0.5 else None, m[1] if m[1] < 449.5 else None, None, None]
    rows.append((i, t, m, level))
segs = ml.segments(rows)
turns = [(t, d) for t, d in ml.steps(run) if d.split()[1] in ('default', 'CommonNavigationTransitionInfo')]
forward = [s for k, s in enumerate(segs[:len(turns)]) if k % 2 == 0]
back = [s for k, s in enumerate(segs[:len(turns)]) if k % 2 == 1]
lags = [s[0][1] - t / 1000 for s, (t, d) in zip(segs, turns)]
print('%d turns in the log, %d forward and %d back in the recording; each starts %.3f to %.3f s after its step '
      '(video time - app time)' % (len(turns), len(forward), len(back), min(lags), max(lags)))

if search:
    e, xa, za = ml.fit_axis(forward, D, CX)
    print('axis fitted to the forward turns: x %.1f z %.1f (CenterOfRotationX %.3f of 450), rms %.3f px' % (xa, za, xa / 450, e))
print('axis x %.1f z %.1f: rms %.3f px over every unclipped frame of the %d forward turns'
      % (axis[0], axis[1], ml.axis_rms(forward, axis[0], axis[1], D, CX), len(forward)))


def angles(seg):
    full = [ml.solve_angle(m, axis[0], axis[1], D, CX)[0] for _, _, m, _ in seg if m[2] is not None]
    sign = 1 if sum(full) > 0 else -1
    return [((t - seg[0][1]) * 1000, round(ml.solve_angle(m, axis[0], axis[1], D, CX, sign)[0], 4)) for _, t, m, _ in seg]


fwd = [angles(s) for s in forward]
bk = [angles(s) for s in back]
firsts = {}
for a in fwd:
    v = round(a[0][1], 1)
    firsts[v] = firsts.get(v, 0) + 1
print('forward, the first angle shown: %s' % ', '.join('%.1f in %d' % (v, n) for v, n in sorted(firsts.items(), key=lambda x: -x[1])))

k6 = ml.expo_out(6)
for name, runs in (('forward', fwd), ('back', bk)):
    vals = ml.distinct([v for a in runs for _, v in a], decimals=1, tol=0.25, min_abs=0.9)
    r0, ticks = ml.tick_numbers(vals)
    print('%s: %d distinct angles %s' % (name, len(vals), ','.join('%.1f' % v for v in vals)))
    print('  ticks %s (median ratio %.4f)' % (ticks, r0))
    for eased in (False, True):
        rms, r, N, A, tau, k = ml.fit_exponential(vals, ticks, eased)
        print('  %-6s from %8.3f, x%.4f a tick (time constant %.1f ms)%s, rms %.3f deg' % (
            'eased' if eased else 'pure', A, r, tau,
            '' if N is None else ', ending at tick %d (%.0f ms), exponent %.2f' % (N, N * ml.TICK, k), rms))
    for kname, f in (('ExponentialEase 6', k6), ('ExponentialEase 5', ml.expo_out(5))):
        rms, d, ph, A = ml.fit_scaled(vals, ticks, f)
        print('  %s: from %.2f over %d ms, the first value %.1f ticks in, rms %.3f deg' % (kname, A, d, ph, rms))
