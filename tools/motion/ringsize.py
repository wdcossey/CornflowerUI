"""The ProgressRing's size threshold and the emulator's animation clock, from a `-Probe ringsize` log
(20261111-2316-dev11).

Usage: python -I ringsize.py <run folder>

The ringsize probe steps one active ring's size and logs, after each step, its laid-out size, the SizeStates state
and SixthCircle's visibility (`size` lines). Then it logs the Active storyboard's own time (Storyboard.GetCurrentTime)
every frame for 10 s (`value active`), and times a 4 s storyboard three times (`value known`, and an `event` line
at each Completed), all by the app's stopwatch.

Prints: the largest smaller side that was Small and the smallest that was Large, and whether every step agrees with
that; then, for the clock, how far animation time moves a frame, how far apart the frames are on the stopwatch, and
the ratio of the two by three measures (the steps over the frames, a least-squares fit, and the 4 s storyboard's
Completed).
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

(run,), _ = ml.parse_args(__doc__)
rows = ml.probe(run)

# The threshold.
size = re.compile(r'^(\S+) x (\S+) actual (\S+) x (\S+) state (\w+) sixth (\w+) max (\S+)$')
steps = []
for t, what, d in rows:
    m = size.match(d) if what == 'size' else None
    if m:
        side = min(float(m.group(3)), float(m.group(4)))
        steps.append((side, m.group(5), m.group(6), float(m.group(7))))
small = [s for s, state, _, _ in steps if state == 'Small']
large = [s for s, state, _, _ in steps if state == 'Large']
if not small or not large:
    sys.exit('no change of SizeStates in the log')
below, above = max(small), min(large)
sixth = all((state == 'Large') == (vis == 'Visible') for _, state, vis, _ in steps)
maxside = all(abs(s - m) < 0.001 for s, _, _, m in steps)
print('SizeStates: Small up to a smaller side of %g, Large from %g; every one of %d steps %s; SixthCircle visible '
      'exactly when Large: %s; MaxSideLength the smaller side: %s'
      % (below, above, len(steps), 'agrees' if below < above else 'does NOT agree', sixth, maxside))


def series(name):
    out = []
    for t, what, d in rows:
        if what == 'value' and d.startswith(name + ' ms '):
            out.append((t, float(d.split()[2])))
    return out


def slope(pts):
    mx = sum(x for x, _ in pts) / len(pts)
    my = sum(y for _, y in pts) / len(pts)
    return sum((x - mx) * (y - my) for x, y in pts) / sum((x - mx) ** 2 for x, _ in pts)


def runs(pts):
    """Splits a series where its time goes back (a storyboard wrapping or starting again)."""
    out, cur = [], []
    for p in pts:
        if cur and p[1] < cur[-1][1]:
            out.append(cur)
            cur = []
        cur.append(p)
    return out + [cur]


# The clock.
active = series('active')
if len(active) < 10:
    sys.exit('no Active clock readings in the log')
moves = [b[1] - a[1] for a, b in zip(active, active[1:]) if b[1] > a[1]]
step = ml.median(moves)
# Each reading is a frame, and the clock's move says how many frames each gap held (a wrap, one).
ticks = [max(1, round((b[1] - a[1]) / step)) if b[1] > a[1] else 1 for a, b in zip(active, active[1:])]
mean, frame, _ = ml.frame_spacing([t for t, _ in active], ticks)
print('Active: animation time moves %.3f ms a frame (%d of %d moves within 0.002); frames %.3f ms apart on the '
      'stopwatch (median; mean %.3f)' % (step, sum(abs(m - step) < 0.002 for m in moves), len(moves), frame, mean))
cycles = runs(active)
print('  ratio, animation time over the stopwatch: %.4f (step / mean frame), %.4f (fit, first cycle); the cycle '
      'wraps after %.1f ms of its own time, %.1f ms on the stopwatch'
      % (step / mean, slope(cycles[0]), cycles[0][-1][1], cycles[0][-1][0] - cycles[0][0][0]))
known = [r for r in runs(series('known')) if len(r) > 10]
done = [float(m.group(1)) for t, what, d in rows if what == 'event'
        for m in [re.match(r'^known Completed, (\S+) ms', d)] if m]
print('4 s storyboard: Completed after %s ms on the stopwatch (ratio %s); fits %s'
      % (', '.join('%.1f' % c for c in done), ', '.join('%.4f' % (4000 / c) for c in done),
         ', '.join('%.4f' % slope(r) for r in known)))
