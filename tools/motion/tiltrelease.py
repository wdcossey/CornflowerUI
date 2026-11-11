"""The tilt's release in frames: the hold and the return, from `-Probe tilt`, `tiltedge` and `tiltgrid` logs (#60's
20261109-2307-dev11, and 20261109-2118-dev11, 20261110-0738-dev10, 20261110-0822-dev11, 20261110-0851-dev10).

Usage: python -I tiltrelease.py <run folder> [<run folder> ...]

For each press: the tilt held at the release (the last projection logged before it) and each projection logged after
it until rest, as a fraction of the held one (least squares over RotationX, RotationY and GlobalOffsetZ). The probe
logs a value only when it changes, so the hold's frames are not in the log; the return's are, one a frame. Each
return frame is numbered from the press's first changed frame by the stopwatch, at the runs' median frame spacing,
so a frame with no reading counts.

Prints:
- each run's presses and its frames' spacing by the stopwatch (the return's frames);
- the return's fractions frame by frame, the median of every press and their spread. They are the same in every
  press, whatever its frames' spacing: the return moves on animation time, 15.625 ms a frame;
- which frame after the release the first change is on: the stopwatch time from the release to it over the press's
  own frame spacing is spread over one frame, from n - 1 to n, for the n printed;
- the hold and the return in animation time, frame k after the release at (k - 1) x 15.625 ms: the first frame after
  a storyboard is begun from a visual state change shows its time 0, as the ProgressRing's `Active` storyboard did
  (20261111-2316-dev11). Fitted to the fractions: a sine ease-out, and a KeySpline. One frame open: were time 0
  the second frame after the release, each hold would be 15.625 ms shorter;
- for comparison, #60's fit: the sine against each frame's stopwatch time since the release.

Presses with nothing logged after the release (dragged off the button, which returned before it), or with no tilt
before it, are left out and counted. A press whose fractions are more than 0.002 off the median at any frame is
listed and left out of the medians.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
TICK = ml.TICK


def presses(run):
    """(release ms, held (rx, ry, gz), [(ms, (rx, ry, gz))] after it until rest) for each press with a tilt held and
    its return logged, and how many presses were left out."""
    out, left, cur = [], 0, None
    for t, what, det in ml.probe(run):
        p = det.split()
        if what == 'pressed':
            left += cur is not None
            label = p[0] if p and not p[0][:1].isdigit() and p[0][:1] != '-' else None
            cur = dict(label=label, held=None, released=None, ret=[])
        elif cur is None:
            continue
        elif what == 'released' and cur['released'] is None:
            cur['released'] = t
        elif what == 'value' and p[:1] == ['projection']:
            q = p[1:]
            if cur['label'] is not None:
                if q[:1] != [cur['label']]:
                    continue
                q = q[1:]
            if q[:1] != ['rot'] or len(q) < 16:
                continue
            v = (float(q[1]), float(q[2]), float(q[15]))
            if cur['released'] is None:
                cur['held'] = v
                continue
            if not (cur['held'] and any(cur['held'])):
                continue
            cur['ret'].append((t, v))
            if not any(v):
                out.append((cur['released'], cur['held'], cur['ret']))
                cur = None
    return out, left + (cur is not None)


def fraction(v, held):
    return sum(a * b for a, b in zip(v, held)) / sum(b * b for b in held)


def rms(v):
    return math.sqrt(sum(x * x for x in v) / len(v))


every = []  # (release ms, [(frame number from the first change, ms, fraction)]) for every press of every run
for run in runs:
    got, left = presses(run)
    gaps = [b[0] - a[0] for _, _, r in got for a, b in zip(r, r[1:])]
    if not gaps:
        raise SystemExit('%s: no press with a tilt held and its return logged' % run)
    spacing = ml.median(gaps)
    for released, held, ret in got:
        t0 = ret[0][0]
        every.append((released, [(1 + round((t - t0) / spacing), t, fraction(v, held)) for t, v in ret]))
    print('%s: %d presses%s; frames %.2f ms apart (median), %.3f (mean)' % (
        os.path.basename(os.path.normpath(run)), len(got), ', %d left out' % left if left else '', spacing,
        sum(gaps) / len(gaps)))

# The return, frame by frame.
length = max(f[-1][0] for _, f in every)
first = [ml.median([fr for _, f in every for j, _, fr in f if j == k] or [0.0]) for k in range(1, length + 1)]
odd = [i for i, (_, f) in enumerate(every) if any(abs(fr - first[j - 1]) > 0.002 for j, _, fr in f)]
kept = [p for i, p in enumerate(every) if i not in odd]
fracs, spread = [], []
for k in range(1, length + 1):
    col = [fr for _, f in kept for j, _, fr in f if j == k]
    if col:
        fracs.append(ml.median(col))
        spread.append(max(col) - min(col))
while fracs and fracs[-1] == 0 and len(fracs) > 1 and fracs[-2] == 0:
    fracs.pop()
    spread.pop()
print('\n%d presses; %d left out of the medians, off the median by more than 0.002 at a frame%s' % (
    len(every), len(odd), ''.join('\n  release at %.1f ms: frames %s' % (every[i][0], ' '.join(
        '%d:%.4f' % (j, fr) for j, _, fr in every[i][1])) for i in odd)))
print('The return, frame by frame from the first change (the median, and the spread over the presses):')
for k, (v, s) in enumerate(zip(fracs, spread), 1):
    print('  %2d  %.4f  %.4f' % (k, v, s))

# Which frame after the release the first change is on.
ns = []
for released, f in kept:
    j1, t1, _ = f[0]
    jn, tn, _ = f[-1]
    ns.append((t1 - released) / ((tn - t1) / (jn - j1)))
ns.sort()
m = round(ml.median(ns) + 0.5)
inside = sum(m - 1 < n <= m for n in ns)
print('\nRelease to the first change, over each press\'s own frame spacing: %.2f to %.2f frames (median %.2f; %d of %d '
      'within %d to %d, the 5%% to 95%% %.2f to %.2f). So the first change is frame %d after the release, and the last '
      'before rest frame %d' % (ns[0], ns[-1], ml.median(ns), inside, len(ns), m - 1, m, ns[len(ns) // 20],
                                  ns[-1 - len(ns) // 20], m, m + len(fracs) - 2))

# In animation time: frame k after the release at (k - 1) TICK.
taus = [(m - 1 + k) * TICK for k in range(len(fracs))]


def model(e, hold, ret, tau):
    u = (tau - hold) / ret
    return 1.0 if u <= 0 else 0.0 if u >= 1 else 1 - e(u)


def sine(u):
    return math.sin(u * math.pi / 2)


def residuals(e, hold, ret, ts=taus, fs=fracs):
    return [model(e, hold, ret, t) - f for t, f in zip(ts, fs)]


def fit_hold_return(e, ts, fs, hold0, ret0):
    """The hold and return minimising the rms, from a grid then Nelder-Mead."""
    best = min(((rms(residuals(e, h, r, ts, fs)), h, r) for h in range(int(hold0) - 40, int(hold0) + 41, 2)
                for r in range(int(ret0) - 60, int(ret0) + 61, 2)), key=lambda x: x[0])
    v, (h, r) = ml.minimize(lambda x: rms(residuals(e, x[0], x[1], ts, fs)), [best[1], best[2]], [1.0, 1.0], 400)
    return v, h, r


v, h, r = fit_hold_return(sine, taus, fracs, 180, 200)
worst = max(abs(x) for x in residuals(sine, h, r))
print('\nIn animation time, frame k after the release at (k - 1) x %.3f ms, so frame %d at %.3f ms. The hold\'s frames '
      'are not logged: they are counted above by the stopwatch, and this takes the first frame after the release as '
      'the release animation\'s time 0, as the first frame logged after the ProgressRing\'s Active storyboard began '
      'read storyboard time 0 (20261111-2316-dev11)' % (TICK, m, taus[0]))
print('  sine ease-out: hold %.1f ms, return %.1f ms; rms %.4f of the full value, worst %.4f' % (h, r, v, worst))


def spline_rms(x):
    x1, y1, x2, y2, hold, ret = x
    if not (all(0 <= c <= 1 for c in (x1, y1, x2, y2)) and ret > 10):  # a KeySpline's control points are in [0, 1]
        return 1e9
    return rms(residuals(ml.key_spline(x1, y1, x2, y2), hold, ret))


fits = []
for start in ([0.39, 0.575, 0.565, 1, h, r], [0.25, 0.25, 0.75, 0.75, h, r - 30], [0.1, 0.3, 0.8, 0.9, h + 5, r - 35]):
    _, x = ml.minimize(spline_rms, start, [0.1, 0.1, 0.1, 0.1, 5, 10], 3000)
    fits.append(ml.minimize(spline_rms, x, [0.02, 0.02, 0.02, 0.02, 1, 2], 3000))
v, (x1, y1, x2, y2, sh, sr) = min(fits, key=lambda f: f[0])
close = [f for f in fits if f[0] < 2 * v]
agree = max(abs(a - b) for f in close for a, b in zip(f[1][4:], (sh, sr)))
worst = max(abs(q) for q in residuals(ml.key_spline(x1, y1, x2, y2), sh, sr))
print('  KeySpline %.3f,%.3f %.3f,%.3f: hold %.1f ms, return %.1f ms; rms %.5f, worst %.5f (%d of %d starts within '
      'twice that rms, their holds and returns within %.1f ms)' % (x1, y1, x2, y2, sh, sr, v, worst, len(close),
                                                                  len(fits), agree))
ROUND = (0.1, 0.25, 0.75, 0.9)
e = ml.key_spline(*ROUND)
v, (rh, rr) = ml.minimize(lambda x: rms(residuals(e, x[0], x[1])), [sh, sr], [0.5, 0.5], 600)
worst = max(abs(q) for q in residuals(e, rh, rr))
print('  KeySpline %g,%g %g,%g, its control points fixed: hold %.1f ms, return %.1f ms; rms %.5f, worst %.5f' % (
    ROUND + (rh, rr, v, worst)))
print('  (the logged values are rounded to 0.001, about 0.0001 of a full tilt.) If time 0 were the second frame after '
      'the release, each hold would be %.3f ms shorter: %.1f ms for the KeySpline with its control points fixed' % (
          TICK, rh - TICK))

# #60's fit: the sine against the stopwatch, every frame of every kept press.
ts = [t - rel for rel, f in kept for _, t, _ in f]
fs = [fr for _, f in kept for _, _, fr in f]
v, h, r = fit_hold_return(sine, ts, fs, 180, 195)
print('\nBy the stopwatch, as #60 fitted it: sine ease-out, hold %.1f ms, return %.1f ms; rms %.4f over %d frames' % (
    h, r, v, len(ts)))
