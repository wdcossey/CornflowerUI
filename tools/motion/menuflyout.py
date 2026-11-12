"""The MenuFlyout's opening storyboard and the 64 Hz animation clock, from `-Probe motion` logs (20261110-0745-dev11,
20261110-0753-dev11; 20261110-0819-dev11 has four more openings).

Usage: python -I menuflyout.py <run folder> [<run folder> ...]

The probe logs every value a popup's template animates, every frame, by the app's clock. A MenuFlyout opening shows
`OuterBorder`'s ScaleX rising from 0, then `InnerBorder`'s ScaleY. For each, over every opening: the distinct
values, the step between them (the least-squares slope of the values above 0 against their order; the first may be
a part step), how many steps 0 to 1 takes and how long that is at 15.625 ms a step; the largest departure from that
line; and the mean time between the app's frames over the rise, by its own clock.
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
parts = {'OuterBorder': (re.compile(r'Border#OuterBorder \| transform scale (\S+) (\S+)(?: centre \S+ \S+)?(?: origin (\S+) (\S+))?'), 0),
         'InnerBorder': (re.compile(r'StackPanel#InnerBorder \| transform scale (\S+) (\S+)'), 1)}
for name, (pattern, axis) in parts.items():
    rises, origins = [], set()
    for run in runs:
        cur = None
        for t, what, d in ml.probe(run):
            if what != 'tree' or name not in d:
                continue
            m = pattern.search(d)
            if not m:  # "rest": the rise is over
                if cur:
                    rises.append(cur)
                cur = None
                continue
            v = float(m.group(1 + axis))
            if m.lastindex and m.lastindex >= 4 and m.group(3):
                origins.add((m.group(3), m.group(4)))
            if v == 0:
                if cur:
                    rises.append(cur)
                cur = []
            if cur is not None:
                cur.append((t, v))
        if cur:  # a rise still going when the log ends
            rises.append(cur)
    if not rises or len(set(v for r in rises for _, v in r)) < 3:
        print('%s: no rise in the logs' % name)
        continue
    # Every rise shows the same values, one a step: the distinct values above 0, in order, against a straight line.
    vals = sorted(set(v for r in rises for _, v in r))
    pts = list(enumerate(vals[1:]))
    n = len(pts)
    mk = sum(k for k, _ in pts) / n
    mv = sum(v for _, v in pts) / n
    step = sum((k - mk) * (v - mv) for k, v in pts) / sum((k - mk) ** 2 for k, _ in pts)
    worst = max(abs(v - (mv + step * (k - mk))) for k, v in pts)
    # Each reading on a rise is a frame, and its value's move says how many frames each gap held.
    gaps = [ml.frame_spacing([t for t, _ in r[1:]], [max(1, round((b - a) / step)) for (_, a), (_, b) in zip(r[1:], r[2:])])[0]
            for r in rises if len(r) > 2] or [float('nan')]
    print('%s Scale%s: %d rises, %d distinct values (%s ... %s)' % (
        name, 'XY'[axis], len(rises), len(vals), ', '.join('%g' % v for v in vals[:4]), ', '.join('%g' % v for v in vals[-2:])))
    print('  +%.4f a step, within %.4f of a straight line: 0 to 1 in %.1f steps, %.0f ms at %.3f ms a step%s' % (
        step, worst, 1 / step, ml.TICK / step, ml.TICK, '; origin %s' % ', '.join('(%s, %s)' % o for o in sorted(origins)) if origins else ''))
    print('  the app\'s frames over the rise: %.2f ms apart on average (%.2f to %.2f by rise)' % (
        sum(gaps) / len(gaps), min(gaps), max(gaps)))
