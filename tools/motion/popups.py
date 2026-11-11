"""The popups opening and closing, from a `-Probe motion -Record` run (20261110-0819-dev11).

Usage: python -I popups.py <run folder> [--gray <file>] [--lag <s>] [--settle <s>]

The published table: python -I popups.py R:/cornflower-reference/refapp/20261110-0819-dev11 --lag 29.15
(29.15 s was typed in by hand on 2026-11-10; the knob's median, 29.158 s, moves the ListPickerFlyout's close row.)

After the ToggleSwitch, the motion probe opens each popup from code four times, holds it 1.2 s and closes it. The
recording is put on the app's clock by the knob (motionlib.knob_lag), or by --lag: recording time = app time - lag,
in s. For each open and close:
before = the last frame more than 30 ms before the call, after = the first frame `settle` s after it (0.7);
p(t) = 1 - |frame - after| / |before - after|, the mean absolute difference over every 5th pixel. Prints, per popup,
the median over its runs of: the first frame that changed, from the call; and from that frame, the time until p
reaches 0.1, 0.5, 0.9 and 0.99, each run's 50/90/99, and when each 100 view px band, top to bottom, is half done.
Then the top Flyout's bottom edge, frame by frame, in its first opening (column x 300, gray level 15).
"""
import os
import statistics
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

(run,), opts = ml.parse_args(__doc__, options=('--gray', '--lag', '--settle'))
rec = ml.Recording(run, opts['--gray'])
settle = 0.7 if opts['--settle'] is None else float(opts['--settle'])
if opts['--lag'] is None:
    lag, lags = ml.knob_lag(ml.knob_edges(rec), run)
    print('recording time = app time - %.3f s (median of %d knob moves, %.3f to %.3f)' % (-lag, len(lags), -max(lags), -min(lags)))
    print('note: the published table (2026-11-10) used --lag 29.15, typed in by hand; with this lag the '
          'ListPickerFlyout close row differs (see README)')
else:
    lag = -float(opts['--lag'])
    print('recording time = app time - %.3f s (given)' % -lag)

w, h, s = rec.w, rec.h, rec.s
calls = []
for t, d in ml.steps(run):
    if d.startswith('toggle') or 'closed,' in d:
        continue
    name, kind = d.strip().rsplit(' ', 1)
    calls.append((round(t / 1000 + lag, 3), name, kind))


def diff(a, b, y0=0, y1=h):
    return sum(abs(x - y) for x, y in zip(a[y0 * w:y1 * w:5], b[y0 * w:y1 * w:5]))


bands = [(int(y / s), int((y + 100) / s)) for y in range(0, 800, 100)]
res = {}
for t, name, kind in calls:
    before_i = max(rec.index_at(t - 0.03) - 1, 0)  # the last frame more than 30 ms before the call
    after_i = rec.index_at(t + settle)
    before, after = rec.frame(before_i), rec.frame(after_i)
    d0 = diff(before, after)
    if d0 == 0:
        continue
    # Each frame's difference from `after`, whole and by band, once.
    seq = []
    for i in range(before_i, after_i + 1):
        fr = rec.frame(i)
        seq.append((rec.times[i], fr != before, diff(fr, after), [diff(fr, after, y0, y1) for y0, y1 in bands]))
    tc = next(tt for tt, changed, _, _ in seq if changed)
    row = {'first': (tc - t) * 1000}
    for q in (0.1, 0.5, 0.9, 0.99):
        tq = next(tt for tt, _, d, _ in seq if 1 - d / d0 >= q)
        row[q] = (tq - tc) * 1000
    bt = []
    for k, (y0, y1) in enumerate(bands):
        db = diff(before, after, y0, y1)
        if db < 0.05 * d0 / len(bands):
            bt.append(None)
            continue
        tq = next(tt for tt, _, _, ds in seq if 1 - ds[k] / db >= 0.5)
        bt.append((tq - tc) * 1000)
    row['bands'] = bt
    res.setdefault((name, kind), []).append(row)

for (name, kind), rows in res.items():
    m = lambda k: statistics.median(r[k] for r in rows)
    per = ' | '.join('%.0f/%.0f/%.0f' % (r[0.5], r[0.9], r[0.99]) for r in rows)
    print('%-18s %-5s %d runs  first frame %4.0f ms after the call;  p>=0.1 %4.0f  0.5 %4.0f  0.9 %4.0f  0.99 %4.0f ms  (runs 50/90/99: %s)' % (
        name, kind, len(rows), m('first'), m(0.1), m(0.5), m(0.9), m(0.99), per))
    bl = []
    for k in range(len(bands)):
        v = [r['bands'][k] for r in rows if r['bands'][k] is not None]
        bl.append('-' if not v else '%.0f' % statistics.median(v))
    print('%26s bands 0-800 by 100 px, 50%% at: %s ms' % ('', ' '.join(bl)))

# The top Flyout swings down from its top edge: its bottom edge, where the gray level falls through 15 on column 300.
t = next((t for t, name, kind in calls if (name, kind) == ('Flyout top', 'open')), None)
if t is None:
    rec.close()
    raise SystemExit('no Flyout top opening in the log')
xcol, thr, top = int(300 / s), 15, int((78.33 + 20) / s)
edges, last = [], "start"
for i, tt, fr in rec.frames(t - 0.04, t + 0.42):
    col = fr[xcol::w]
    yb = None
    for y in range(top, 0, -1):
        if col[y] >= thr > col[y + 1]:
            yb = y + (col[y] - thr) / (col[y] - col[y + 1])
            break
    v = None if yb is None else round(yb * s, 2)
    if v != last:
        edges.append('%.0f ms %s' % ((tt - t) * 1000, '-' if v is None else '%.2f' % v))
        last = v
print('Flyout top, first opening, its bottom edge (view px): %s' % '; '.join(edges))
rec.close()
