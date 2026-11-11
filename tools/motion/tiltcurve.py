"""#60's tilt curve against a `-Probe tiltgrid` log (20261109-2307-dev11 on the reference profile,
20261110-0851-dev10 on the 720p profile).

Usage: python -I tiltcurve.py <run folder>

The tilt grid presses four Buttons at a 5 x 5 grid of points each and logs every frame the PlaneProjection the tilt
gives the button. For each press, the last projection before the release against #60's curve: RotationX g(dy) and
RotationY -g(dx), with dx, dy the press point's offset from the button's centre as a fraction of its size and
g(d) = 17.19 deg x d x (1 + 0.08 |d|); and GlobalOffsetZ against -25 (1 - (|RotationX| + |RotationY|) / 0.3 rad).
Prints the angles' rms and worst residual, each button's mean residual (its offset) and rms after it, the rms with
each button's offset taken out, and the depth's worst residual and rms. (#65's model, through the drawn tilt and
PlaneProjection's camera, is tested in tests/Cornflower.Tests/Behaviours/, not here.)
"""
import collections
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

if len(sys.argv) != 2:
    raise SystemExit(__doc__)
sizes, presses, cur, last = {}, [], None, {}
for t, what, det in ml.probe(sys.argv[1]):
    p = det.split()
    if what == 'layout' and p[0] == 'button':  # button A x y w h ...
        sizes[p[1]] = (float(p[4]), float(p[5]))
    elif what == 'pressed':  # A ... ix iy: the press point in the button
        cur = (p[0], float(p[4]), float(p[5]))
        last = {}
    elif what == 'value' and p[0] == 'projection' and cur and p[1] == cur[0] and p[2] == 'rot':
        last = dict(rx=float(p[3]), ry=float(p[4]), gz=float(p[17]))
    elif what == 'released' and cur:
        presses.append((cur[0], cur[1], cur[2], last['rx'], last['ry'], last['gz']))
        cur = None

G = 17.19
RAD = math.degrees(0.3)


def g(d):
    return G * d * (1 + 0.08 * abs(d))


def rms(v):
    return math.sqrt(sum(x * x for x in v) / len(v))


res, allr, zres = collections.defaultdict(list), [], []
for b, ix, iy, rx, ry, gz in presses:
    w, h = sizes[b]
    dx, dy = ix / w - 0.5, iy / h - 0.5
    ex, ey = g(dy), -g(dx)
    res[b].append((rx - ex, ry - ey))
    allr += [rx - ex, ry - ey]
    zres.append(gz + 25 * (1 - (abs(rx) + abs(ry)) / RAD))
print('presses %d' % len(presses))
print('angles: rms %.3f deg, worst %.3f' % (rms(allr), max(abs(x) for x in allr)))
cent = []
for b in sorted(res):
    xs, ys = [a for a, _ in res[b]], [c for _, c in res[b]]
    mx, my = sum(xs) / len(xs), sum(ys) / len(ys)
    cent += [x - mx for x in xs] + [y - my for y in ys]
    print('  %s: mean residual RotationX %.3f RotationY %.3f; rms after it %.3f %.3f' % (
        b, mx, my, rms([x - mx for x in xs]), rms([y - my for y in ys])))
print('angles with each button\'s mean residual taken out: rms %.3f deg' % rms(cent))
print('depth: worst residual %.4f px, rms %.4f px' % (max(abs(z) for z in zres), rms(zres)))
