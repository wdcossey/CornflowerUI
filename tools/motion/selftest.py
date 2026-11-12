"""Checks motionlib's measurements and fits on synthetic data with known answers.

Usage: python -I selftest.py

Each check builds data from a known curve, camera or outline, runs the code the scripts use, and asserts it gets the
answer back; and that the scripts stop, rather than go on with mispaired or missing data, where they should.
Prints one line a check and exits non-zero on the first failure. Takes about half a minute.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

checks = []


def check(fn):
    checks.append(fn)
    return fn


def near(a, b, tol, what):
    if abs(a - b) > tol:
        raise AssertionError('%s: %r, expected %r within %r' % (what, a, b, tol))


@check
def crossing():
    vals = bytes([0] * 10 + [200] * 20 + [0] * 10)
    near(ml.cross(vals, 100), 9.5, 1e-9, 'rising edge')
    near(ml.cross(vals, 100, rev=True), 29.5, 1e-9, 'falling edge, from the end')
    near(ml.cross(bytes([0, 50, 150, 200]), 100), 1.5, 1e-9, 'interpolated')
    assert ml.cross(bytes([0] * 5), 100) is None


class FakeRecording:
    """Frames of a lit rectangle, columns a..b-1 and rows c..d-1 at level 200, on black."""

    def __init__(self, rects, w=540, h=960):
        self.w, self.h, self.s = w, h, ml.VIEW_W / w
        self.rects = rects

    def frames(self, t0=-1e9, t1=1e9):
        for i, (a, b, c, d) in enumerate(self.rects):
            row = bytes([0] * a + [200] * (b - a) + [0] * (self.w - b))
            blank = bytes(self.w)
            yield i, i * 0.025, b''.join(row if c <= y < d else blank for y in range(self.h))


@check
def sheet_outline():
    rec = FakeRecording([(23, 518, 72, 888), (0, 300, 72, 888)])
    rows = ml.outline(rec)
    s = rec.s
    i, t, m, level = rows[0]
    near(m[0], 22.5 * s, 0.001, 'XL')
    near(m[1], 517.5 * s, 0.001, 'XR')
    near(m[2], 816 * s, 0.001, 'HL')
    near(m[3], 816 * s, 0.001, 'HR')
    assert level == 200
    assert rows[1][2][2] is None, 'a sheet off the left edge is clipped'
    box = ml.bbox(next(rec.frames())[2], rec)
    near(box[0], round(22.5 * s, 1), 1e-9, 'bbox left')
    near(box[1], round(71.5 * s, 1), 1e-9, 'bbox top')


@check
def easings():
    near(ml.expo_out(6)(0.0), 0.0, 1e-12, 'expo at 0')
    near(ml.expo_out(6)(1.0), 1.0, 1e-12, 'expo at 1')
    near(ml.expo_out(6)(0.5), (1 - math.exp(-3)) / (1 - math.exp(-6)), 1e-12, 'expo at 0.5')
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    near(f(0.0), 0.0, 1e-9, 'spline at 0')
    near(f(1.0), 1.0, 1e-9, 'spline at 1')
    # On the Bezier itself: the point at parameter 0.5 is (0.2375, 0.8375).
    near(f(3 * 0.1 / 8 + 3 * 0.2 / 8 + 1 / 8), 3 * 0.9 / 8 + 3 / 8 + 1 / 8, 1e-6, 'spline at s = 0.5')
    near(ml.key_spline(0.25, 0.25, 0.75, 0.75)(0.3), 0.3, 1e-6, 'a straight spline')


@check
def distinct_and_ticks():
    assert ml.distinct([-67.01, -67.04, -53.8, -53.83, -0.5, -43.12], 1, 0.25, 0.9) == [-67.0, -53.8, -43.1]
    vals = [100 * 0.8 ** k for k in (0, 1, 2, 4, 5, 6, 7, 9)]
    r0, ticks = ml.tick_numbers(vals)
    assert ticks == [0, 1, 2, 4, 5, 6, 7, 9], ticks
    near(r0, 0.8, 1e-9, 'median ratio')


@check
def exponential_ease_out():
    # An exponential ease-out from -67 reaching 0 at tick 25, falling 0.8031 a tick; one tick never shown.
    r, N, A = 0.8031, 25, -67.0
    ticks = list(range(18)) + [19]
    vals = [A * (r ** k - r ** N) / (1 - r ** N) for k in ticks]
    rms, rr, NN, AA, tau, k = ml.fit_exponential(vals, ticks, eased=True)
    near(rr, r, 1e-4, 'ratio')
    assert NN == N, NN
    near(AA, A, 0.01, 'start')
    near(rms, 0, 1e-3, 'rms')
    near(k, N * ml.TICK / (-ml.TICK / math.log(r)), 1e-9, 'exponent')


@check
def scaled_expo_fit():
    # ExponentialEase 6 from 200 over 350 ms, first value on the start, tick 2 missing.
    f = ml.expo_out(6)
    ticks = [0, 1] + list(range(3, 17))
    vals = [200 * (1 - f(k * ml.TICK / 350)) for k in ticks]
    rms, d, ph, A = ml.fit_scaled(vals, ticks, f)
    assert (d, ph) == (350, 0), (d, ph)
    near(A, 200, 1e-6, 'start')
    near(rms, 0, 1e-9, 'rms')


@check
def key_spline_fit():
    # The knob: KeySpline 0.1,0.9 0.2,1 over 370 ms, the first value one tick in; a power ease-out fits worse.
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    fracs = [1 - f((1 + k) * ml.TICK / 370) for k in range(21)]
    rms, d, ph = ml.fit_fraction(fracs, f)
    assert (d, ph) == (370, 1.0), (d, ph)
    near(rms, 0, 1e-9, 'rms')
    assert ml.fit_fraction(fracs, ml.power_out(5))[0] > 0.005


@check
def camera_fit():
    # Held states seen through a camera 999 px away, vanishing point (224, 400).
    states = [(10, 0, 0), (-10, 0, 0), (-30, 0, 0), (-60, 0, 0), (10, 0.5, 0), (30, 0.5, 0), (-30, -0.25, 0),
              (0, 0, -100), (30, 0, -100)]
    held = [(st, ml.project_plane(st[0], st[1], st[2], 999, 224)) for st in states]
    e, d, cx, sgn = ml.fit_camera(held)
    assert (d, cx, sgn) == (999, 224, 1), (d, cx, sgn)
    near(e, 0, 1e-9, 'rms')


@check
def turnstile_axis():
    # Frames of a sheet turning about x -44, z -99; the angle of each and the axis come back.
    d, cx = 1000.0, 225.0
    for theta in (-67.0, -22.3, -3.6, 33.7, 5.9):
        m = ml.project_y(theta, -44, -99, d, cx)
        near(ml.solve_angle(m, -44, -99, d, cx)[0], theta, 1e-4, 'angle')
    segs = [[(0, 0.0, ml.project_y(th, -44, -99, d, cx), 255)] for th in (-53.8, -34.6, -17.8, -7.2)]
    e, xa, za = ml.fit_axis(segs, d, cx)
    near(xa, -44, 0.25, 'axis x')
    near(za, -99, 0.25, 'axis z')


def exits(fn, *args, **kw):
    """True if fn stops the script (SystemExit), as the scripts do on data they can't trust."""
    try:
        fn(*args, **kw)
    except SystemExit:
        return True
    return False


@check
def pairing_steps_to_transitions():
    # Steps 1.3 s apart; each transition starts 27.8 s earlier on the recording's clock, give or take 20 ms; two other
    # transitions in between. Each step gets its own, in order.
    times = [32.0 + 1.3 * k for k in range(18)]
    jitter = [0.02 * ((k * 7) % 5 - 2) / 2 for k in range(18)]
    starts = sorted([t - 27.8 + j for t, j in zip(times, jitter)] + [4.85, 30.0])
    lag, pairs, lags = ml.match_steps(starts, times)
    near(lag, -27.8, 0.02, 'lag')
    assert [starts[j] for j in pairs] == [t - 27.8 + j for t, j in zip(times, jitter)]
    # A transition missing: the scripts stop rather than pair the rest one off.
    assert exits(ml.match_steps, starts[:5] + starts[6:], times)
    # Steps out of step with the transitions altogether.
    assert exits(ml.match_steps, starts, [t + 0.4 * (k % 2) for k, t in enumerate(times)])


@check
def segments_need_rest_first():
    still = [19.0, 431.0, 680.0, 680.0]
    rows = [(0, 0.0, still, 255), (1, 0.1, [30.0, 420.0, 600.0, 650.0], 255), (2, 0.2, still, 255),
            (3, 0.3, [0.0, 300.0, None, None], 255), (4, 0.4, still, 255), (5, 0.5, [25.0, 425.0, 670.0, 690.0], 255)]
    assert [[r[0] for r in s] for s in ml.segments(rows)] == [[1], [3], [5]]
    assert [[r[0] for r in s] for s in ml.segments(iter(rows), limit=2)] == [[1], [3]]
    assert exits(ml.segments, rows[3:])  # clipped first: no rest to compare with
    assert exits(ml.segments, [])


@check
def median_ratio():
    # Ratios 0.5, 0.6, 0.7, 0.8: the median is 0.65, not the upper middle value.
    vals = [100, 50, 30, 21, 16.8]
    near(ml.tick_numbers(vals)[0], 0.65, 1e-9, 'median ratio')


@check
def camera_off_the_grid():
    # A camera between the grid's points: the refinement, around the grid's best, finds it.
    states = [(10, 0, 0), (-10, 0, 0), (-30, 0, 0), (-60, 0, 0), (10, 0.5, 0), (30, 0.5, 0), (0, 0, -100), (30, 0, -100)]
    held = [(st, ml.project_plane(st[0], st[1], st[2], 1003, 221.75)) for st in states]
    e, d, cx, sgn = ml.fit_camera(held)
    assert (d, cx) == (1003, 221.75), (d, cx)


@check
def axis_needs_frames():
    clipped = [[(0, 0.0, [0.0, 300.0, None, None], 255)]]
    assert exits(ml.axis_rms, clipped, -44, -99, 1000, 225)


@check
def missing_tick_on_a_curve():
    # The knob's check: positions a tick apart land about a tick apart on the curve; one tick left out, two.
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    ticks = [0, 1, 2, 3, 5, 6, 7]
    fracs = [1 - f((1 + k) * ml.TICK / 370) for k in ticks]
    at = ml.ticks_on_curve(fracs, f, 370, 1.0)
    gaps = [b - a for a, b in zip(at, at[1:])]
    for g, (a, b) in zip(gaps, zip(ticks, ticks[1:])):
        near(g, b - a, 1e-6, 'ticks apart')
    rms, d, ph = ml.fit_fraction(fracs, f, ticks=ticks)
    assert (d, ph) == (370, 1.0) and rms < 1e-9


@check
def arguments():
    saved = sys.argv
    try:
        sys.argv = ['x.py', 'run', '--lag', '29.15', '--no-search']
        assert ml.parse_args('doc', options=('--gray', '--lag'), flags=('--no-search', '--other')) == (
            ['run'], {'--gray': None, '--lag': '29.15', '--no-search': True, '--other': False})
        sys.argv = ['x.py', 'run', '--unknown', '1']
        assert exits(ml.parse_args, 'doc', options=('--gray',))
        sys.argv = ['x.py']
        assert exits(ml.parse_args, 'doc')
        sys.argv = ['x.py', 'run', '--gray']
        assert exits(ml.parse_args, 'doc', options=('--gray',))
    finally:
        sys.argv = saved


@check
def recording_files():
    # A run folder of three 10 x 4 frames: the size from rect.log, the frames found by their size, a row read
    # alone, and the file closed by `with`.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        os.mkdir(os.path.join(run, 'analysis'))
        with open(os.path.join(run, 'frames.csv'), 'w') as f:
            f.write('0.000000,\n0.025000\n0.050000\n')
        with open(os.path.join(run, 'rect.log'), 'w') as f:
            f.write('12:00:00.000 display window at 1,2, 10 x 4\n')
        with open(os.path.join(run, 'analysis', 'record.gray'), 'wb') as f:
            f.write(bytes(range(120)))
        with open(os.path.join(run, 'analysis', 'small.gray'), 'wb') as f:
            f.write(bytes(60))
        with ml.Recording(run) as rec:
            assert rec.gray.endswith('record.gray')
            assert (rec.w, rec.h) == (10, 4)
            assert rec.row(2, 1) == bytes(range(90, 100))
            assert rec.frame(1) == bytes(range(40, 80))
            assert rec.index_at(0.03) == 2
        assert rec._f.closed


def run_script(name, run):
    import subprocess
    out = subprocess.run([sys.executable, '-I', os.path.join(os.path.dirname(os.path.abspath(__file__)), name), run],
                         capture_output=True, text=True)
    return out.returncode, out.stdout + out.stderr


@check
def scripts_on_partial_logs():
    # A tilt press with no tilt logged is left out, not a crash; a MenuFlyout rise cut off by the end of the log
    # still counts.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n')
            f.write('1.0\tpressed\tA 0 0 0 60 30\n2.0\tvalue\tprojection A rot ' + ' '.join(['0'] * 14) + ' -25\n3.0\treleased\tA\n')
            f.write('4.0\tpressed\tA 0 0 0 6 30\n5.0\treleased\tA\n')
        code, out = run_script('tiltcurve.py', run)
        assert code == 0 and 'presses 1, and 1 left out' in out, out
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            for k in range(25):
                f.write('%.1f\ttree\tpopup1/0 Border#OuterBorder | transform scale %g 1 centre 0 0 origin 0.5 0\n'
                        % (100 + 15.625 * k, round(k * 0.0411, 3)))
        code, out = run_script('menuflyout.py', run)
        assert code == 0 and 'OuterBorder ScaleX: 1 rises' in out and '24.3 steps' in out, out


@check
def minimize_finds_a_minimum():
    # Rosenbrock's valley, from well off it; and a bowl, where it stops once converged, long before its iterations.
    v, x = ml.minimize(lambda p: (1 - p[0]) ** 2 + 100 * (p[1] - p[0] ** 2) ** 2, [-1.2, 1.0], [0.5, 0.5], 4000)
    near(x[0], 1, 1e-4, 'x')
    near(x[1], 1, 1e-4, 'y')
    calls = []

    def bowl(p):
        calls.append(p)
        return (p[0] - 3) ** 2 + (p[1] + 1) ** 2

    v, x = ml.minimize(bowl, [0.0, 0.0], [1.0, 1.0], 100000)
    near(x[0], 3, 1e-6, 'bowl x')
    near(x[1], -1, 1e-6, 'bowl y')
    assert len(calls) < 2000, len(calls)


@check
def key_spline_is_exact():
    # At any parameter s, the spline's value at the Bezier's x(s) is its y(s), near x1 = 0 too, where Newton's method
    # alone stalls.
    for c in ((0.1, 0.25, 0.75, 0.9), (0.0, 0.3, 0.8, 0.9), (0.001, 0.27, 0.77, 0.91), (0.1, 0.9, 0.2, 1), (0, 0, 1, 1)):
        f = ml.key_spline(*c)
        for i in range(65):
            s = i / 64
            r = 1 - s
            x = 3 * c[0] * s * r * r + 3 * c[2] * s * s * r + s ** 3
            y = 3 * c[1] * s * r * r + 3 * c[3] * s * s * r + s ** 3
            near(f(x), y, 1e-10, 'KeySpline %s at s %g' % (c, s))


@check
def tilt_release_stops_on_too_little():
    # A run whose returns log one frame each has no frame spacing of its own: the script leaves it out and says so,
    # then, with no run left, stops, not a crash.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n1000.0\tpressed\tA 0 0 in-control 5 3 Touch\n')
            f.write('1020.0\tvalue\tprojection A rot -8 8 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 -0.6\n')
            f.write('1500.0\treleased\tA 0 0 in-control 5 3 Touch\n')
            f.write('1690.0\tvalue\tprojection A rot 0 0 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 0\n')
        code, out = run_script('tiltrelease.py', run)
    assert code != 0 and 'left out, no press logged two or more frames' in out and 'no run with a press' in out, out
    assert 'Traceback' not in out, out


@check
def tilt_release_in_frames():
    # Releases logged as the probe logs them: frames 15.0 ms apart by the stopwatch, each moving animation time
    # 15.625 ms, the first frame after the release at time 0; the tilt held 182.6 ms, then back down KeySpline
    # 0.1,0.25 0.75,0.9 over 167 ms; a value logged to 0.001, only when it changes. The fits must give them back.
    import re
    import tempfile
    e = ml.key_spline(0.1, 0.25, 0.75, 0.9)
    held = (-8.5, 8.2, -0.6)
    line = '%.1f\tvalue\tprojection A rot %g %g 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 %g\n'
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n')
            for i in range(12):
                t = 1000.0 + 2000 * i
                f.write('%.1f\tpressed\tA 0 0 in-control 5 3 Touch handled\n' % t)
                f.write(line % ((t + 20,) + held))
                released = t + 500 + i * 1.25  # at twelve phases of a frame
                f.write('%.1f\treleased\tA 0 0 in-control 5 3 Touch handled\n' % released)
                frame = t + 507.0  # the first frame after the release
                while frame <= released:
                    frame += 15.0
                last = held
                for k in range(1, 40):
                    u = ((k - 1) * ml.TICK - 182.6) / 167
                    left = 1.0 if u <= 0 else 0.0 if u >= 1 else 1 - e(u)
                    v = tuple(round(c * left, 3) + 0.0 for c in held)
                    if v != last:
                        f.write(line % ((frame + (k - 1) * 15.0,) + v))
                    last = v
                    if not any(v):
                        break
            # And one dragged off the button (a move below it) and let go while its tilt was halfway back: left out.
            t = 1000.0 + 2000 * 12
            f.write('%.1f\tpressed\tA 0 0 in-control 5 3 Touch handled\n' % t)
            f.write(line % ((t + 20,) + held))
            f.write('%.1f\tmoved\tA 0 0 in-control 5 90 Touch\n' % (t + 300))
            f.write(line % ((t + 600,) + tuple(c / 2 for c in held)))
            f.write('%.1f\treleased\tA 0 0 in-control 5 90 Touch handled\n' % (t + 610))
            f.write(line % ((t + 625,) + tuple(c / 4 for c in held)))
            f.write(line % ((t + 640,) + (0, 0, 0)))
        code, out = run_script('tiltrelease.py', run)
    assert code == 0, out
    assert '12 presses, 1 left out' in out, out
    assert 'the first change is frame 13 after the release' in out, out
    m = re.search(r'control points fixed: hold ([\d.]+) ms, return ([\d.]+) ms; rms ([\d.]+)', out)
    assert m, out
    near(float(m.group(1)), 182.6, 0.3, 'hold')
    near(float(m.group(2)), 167.0, 0.5, 'return')
    assert float(m.group(3)) < 0.0002, out


for fn in checks:
    fn()
    print('ok  %s' % fn.__name__)
print('%d checks passed' % len(checks))
