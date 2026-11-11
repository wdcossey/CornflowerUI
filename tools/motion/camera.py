"""PlaneProjection's camera, from a `-Probe projections -Record` run (20261110-0756-dev11).

Usage: python -I camera.py <run folder> [--gray <file>]

The probe gives a full-window panel known projections, one after another, each held still: RotationY +-10, +-30 and
+-60 degrees about CenterOfRotationX 0, 0.5 and -0.25, and GlobalOffsetZ +-100. The sheet's outline is measured in
every frame; each held state is the median of the middle half of its frames, and a state with an edge off the screen
is left out. A pinhole camera d view px in front of the screen, its vanishing point at (cx, 400), is fitted to them.
Prints each state, the best d, cx and rms, and each state's residuals.
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

rows = ml.outline(ml.Recording(run, gray))
known = []
for t, d in ml.steps(run):
    if d != 'rest':
        q = d.split()  # rotationY 10 centreX 0 globalZ 0
        known.append((float(q[1]), float(q[3]), float(q[5])))
segs = [s for s in ml.segments(rows) if len(s) >= 5]
print('held states %d, steps %d' % (len(segs), len(known)))
held = []
for s, st in zip(segs, known):
    mid = s[len(s) // 4: 3 * len(s) // 4]
    if any(r[2][2] is None for r in mid):
        print('  rotY %6.1f c %5.2f gz %6.1f  ->  an edge off the screen, left out' % st)
        continue
    med = [sorted(r[2][k] for r in mid)[len(mid) // 2] for k in range(4)]
    held.append((st, med))
    print('  rotY %6.1f c %5.2f gz %6.1f  ->  XL %7.2f XR %7.2f HL %7.2f HR %7.2f  (%d frames)' % (st + tuple(med) + (len(s),)))

e, d, cx, sgn = ml.fit_camera(held)
print('camera: %d view px from the screen, vanishing point (%.2f, 400), positive RotationY brings the %s side nearer; '
      'rms %.3f px over %d states' % (d, cx, 'right' if sgn > 0 else 'left', e, len(held)))
for (theta, c, gz), m in held:
    p = ml.project_plane(theta, c, gz, d, cx, sgn)
    print('  rotY %6.1f c %5.2f gz %6.1f  residual XL XR HL HR %s' % (theta, c, gz, ' '.join('%6.2f' % (a - b) for a, b in zip(m, p))))
