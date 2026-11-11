"""The ProgressRing's Active animation, from a `-Probe ring` log (20261110-0829-dev11; 20261110-0846-dev10 on the
720p profile).

Usage: python -I ring.py <run folder>

The ring probe shows two active ProgressRings, at the default size and 120 view px, and logs every frame each dot's
Canvas rotation and Ellipse opacity, by the app's clock. For each ring: its size, how many dots move, the cycle (the
mean time between one dot's hidings, over every cycle logged), how long into a cycle a dot hides (from showing again
to hiding, the median), and how far apart the dots hide (over every cycle, and in the first).
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

if len(sys.argv) != 2:
    raise SystemExit(__doc__)
run = sys.argv[1]
rings, events = {}, {}
dot = re.compile(r'^(\w+)/0/1/(\d+)/0 Ellipse#\w+ \| (opacity 0|rest)$')
for t, what, d in ml.probe(run):
    if what != 'tree':
        continue
    m = re.match(r'^(\w+) ProgressRing#\w+ \| at \S+ \S+ (\S+) (\S+)', d)
    if m:
        rings[m.group(1)] = (float(m.group(2)), float(m.group(3)))
        continue
    m = dot.match(d)
    if m:
        events.setdefault(m.group(1), {}).setdefault(int(m.group(2)), []).append((t, m.group(3) == 'rest'))

for name, (w, h) in rings.items():
    dots = events.get(name, {})
    hides = {k: [t for t, shown in ev if not shown and t > 0] for k, ev in dots.items()}
    first = hides[0]
    cycle = (first[-1] - first[0]) / (len(first) - 1)
    # The dots one after another, within each cycle of the first dot.
    gaps, cycles = [], []
    for t0 in first:
        ts = [min((t for t in hides[k] if t >= t0 - 1), default=None) for k in sorted(hides)]
        if None not in ts and ts[-1] - t0 < 1500:
            cycles.append([b - a for a, b in zip(ts, ts[1:])])
            gaps += cycles[-1]
    into = []
    for k, ev in dots.items():
        shown = None
        for t, rest in ev:
            if rest and t > 0:
                shown = t
            elif not rest and shown is not None:
                into.append(t - shown)
                shown = None
    print('%s: %g x %g view px, %d dots; the cycle %.3f s (%d cycles); a dot hides %.3f s after it shows (median of %d)'
          % (name, w, h, len(dots), cycle / 1000, len(first) - 1, ml.median(into) / 1000, len(into)))
    print('  the dots hide %.0f to %.0f ms apart (median %.0f); in the first cycle %s ms' % (
        min(gaps), max(gaps), ml.median(gaps), ', '.join('%.0f' % g for g in cycles[0])))
