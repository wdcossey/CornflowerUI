"""The ToggleSwitch knob's slide, from a `-Probe motion -Record` run (20261110-0819-dev11).

Usage: python -I knob.py <run folder> [--gray <file>]

The motion probe first sets a ToggleSwitch On and Off from code, ten times each. The knob's left edge is read in
every frame of the first 30 s (motionlib.knob_edges). Off and On at rest are the commonest positions on each side of
the middle; the travel is their difference. Each position the knob passes through going On is a distance short of
On; the distinct ones over 0.02 px, largest first, as fractions of the travel, are one per 15.625 ms tick. Fitted
to them: KeySplines and ease-outs over a duration, the first value a phase into it (motionlib.fit_fraction).
"""
import math
import os
import sys
from collections import Counter

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

edges = ml.knob_edges(ml.Recording(run, gray))
xs = [x for _, x in edges if x is not None]
lo, hi = min(xs), max(xs)
mid = (lo + hi) / 2
off = Counter(x for x in xs if x < mid).most_common(1)[0][0]
on = Counter(x for x in xs if x > mid).most_common(1)[0][0]
travel = round(on - off, 2)
going_on, going_off, prev = set(), set(), None
for _, x in edges:
    if x is None:
        continue
    if prev is not None and abs(x - prev) > 0.02:
        (going_on if x > prev else going_off).add(x)
    prev = x
short_on = sorted((round(on - x, 2) for x in going_on), reverse=True)
past_off = sorted((round(x - off, 2) for x in going_off), reverse=True)
print('knob: Off at %.2f, On at %.2f, travel %.2f view px' % (off, on, travel))
print('going On, short of On: %s' % ', '.join('%.2f' % v for v in short_on))
print('going Off, past Off:   %s' % ', '.join('%.2f' % v for v in past_off))
dist = [v for v in short_on if v > 0.02]
fracs = [round(v / travel, 5) for v in dist]
print('%d positions over 0.02 px, as fractions of the travel: %s' % (len(fracs), ','.join('%.5f' % v for v in fracs)))

curves = {
    'KeySpline 0.1,0.9 0.2,1': ml.key_spline(0.1, 0.9, 0.2, 1),
    'KeySpline 0,0 0.58,1 (ease-out)': ml.key_spline(0, 0, 0.58, 1),
    'KeySpline 0.25,0.1 0.25,1 (ease)': ml.key_spline(0.25, 0.1, 0.25, 1),
    'KeySpline 0,0 0,1': ml.key_spline(0, 0, 0, 1),
    'KeySpline 0.2,0.8 0.2,1': ml.key_spline(0.2, 0.8, 0.2, 1),
    'KeySpline 0.1,0.25 0.1,1': ml.key_spline(0.1, 0.25, 0.1, 1),
    'cubic ease-out': ml.power_out(3),
    'quartic ease-out': ml.power_out(4),
    'quintic ease-out': ml.power_out(5),
    'ExponentialEase 6': ml.expo_out(6),
    'ExponentialEase 8': ml.expo_out(8),
    'circle ease-out': ml.circle_out,
}
res = sorted((ml.fit_fraction(fracs, f) + (name,)) for name, f in curves.items())
for rms, d, ph, name in res:
    print('  %-34s over %3d ms, the first value %.1f ticks in: rms %.4f of the travel (%.2f px)' % (name, d, ph, rms, rms * travel))
