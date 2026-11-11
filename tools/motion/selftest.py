"""Checks motionlib's measurements and fits on synthetic data with known answers.

Usage: python -I selftest.py

Each check builds data from a known curve, camera or outline, runs the code the scripts use, and asserts it gets the
answer back. Prints one line a check and exits non-zero on the first failure. Takes about half a minute.
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
    # On the Bezier itself: the point at parameter 0.5 is (0.3625, 0.8 - 0.0125).
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


for fn in checks:
    fn()
    print('ok  %s' % fn.__name__)
print('%d checks passed' % len(checks))
