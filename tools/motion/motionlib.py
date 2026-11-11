"""Shared code for the motion scripts: a refapp run's recording and probe log, the frame measurements, the
PlaneProjection camera, and the easing fits.

A run folder is what `tools/refapp/run.ps1` pulls back into the reference folder, e.g.
`R:\\cornflower-reference\\refapp\\20261110-0810-dev11`: `probe.txt` (the app's log), and with `-Record`
`record.mkv`, `frames.csv` (each frame's time, in seconds from the start of the recording) and `rect.log` (the
emulator's display window, whose size is the recording's). The scripts read the recording as raw 8-bit gray frames,
decoded once by `decode.py` into `<run>/analysis/`.

Python 3 standard library only.
"""
import glob
import math
import os
import statistics

TICK = 15.625  # ms: the emulator animates on a 64 Hz clock
VIEW_W = 450.0  # the reference profile's view width; the recording is 540 px wide, so 0.83 view px a pixel


# ---------------------------------------------------------------------------------------------------------------
# The run


def probe(run):
    """The probe log as (t ms, what, detail) rows."""
    rows = []
    with open(os.path.join(run, 'probe.txt'), encoding='utf-8') as f:
        for line in f:
            p = line.rstrip('\n').split('\t')
            if len(p) == 3:
                rows.append((float(p[0]), p[1], p[2]))
    return rows


def steps(run):
    """The probe's sequence: (t ms, detail) for each `step` line."""
    return [(t, d) for t, w, d in probe(run) if w == 'step']


class Recording:
    """A run's recording, as raw 8-bit gray frames (w x h bytes each, in the order of frames.csv)."""

    def __init__(self, run, gray=None, view_w=VIEW_W):
        self.run = run
        with open(os.path.join(run, 'frames.csv')) as f:
            self.times = [float(l.strip().rstrip(',')) for l in f if l.strip()]
        self.w, self.h = self._size(run)
        self.s = view_w / self.w
        size = self.w * self.h * len(self.times)
        if gray is None:
            found = [p for p in sorted(glob.glob(os.path.join(run, 'analysis', '*.gray'))) if os.path.getsize(p) == size]
            if not found:
                raise SystemExit('%s: no decoded recording of %d x %d x %d frames in analysis\\; run decode.py %s first'
                                 % (run, self.w, self.h, len(self.times), run))
            gray = found[0]
        elif os.path.getsize(gray) != size:
            raise SystemExit('%s is %d bytes, not %d x %d x %d frames' % (gray, os.path.getsize(gray), self.w, self.h, len(self.times)))
        self.gray = gray
        self._f = open(gray, 'rb')

    @staticmethod
    def _size(run):
        # rect.log: "... display window at 994,186, 540 x 960"
        with open(os.path.join(run, 'rect.log')) as f:
            last = [l for l in f if ' x ' in l][-1]
        w, h = last.rsplit(',', 1)[1].split(' x ')
        return int(w), int(h)

    def frame(self, i):
        self._f.seek(i * self.w * self.h)
        return self._f.read(self.w * self.h)

    def frames(self, t0=-1e9, t1=1e9):
        """(index, time, frame) for every frame between t0 and t1 s."""
        for i, t in enumerate(self.times):
            if t0 <= t <= t1:
                yield i, t, self.frame(i)

    def index_at(self, t):
        """The first frame at or after t s."""
        return next((i for i, tt in enumerate(self.times) if tt >= t), len(self.times) - 1)


def cross(vals, thr, rev=False):
    """Fractional index where vals first rises to thr (from the start, or from the end)."""
    n = len(vals)
    if rev:
        for i in range(n - 1, 0, -1):
            q, p = vals[i - 1], vals[i]
            if p < thr <= q:
                return i - (thr - p) / (q - p)
    else:
        for i in range(1, n):
            p, q = vals[i - 1], vals[i]
            if p < thr <= q:
                return i - 1 + (thr - p) / (q - p)
    return None


# ---------------------------------------------------------------------------------------------------------------
# Frame measurements


def outline(rec):
    """The pages probe's sheet (412 x 680 view px, 19 in and 60 down) in every frame: [XL, XR, HL, HR] in view px,
    XL and XR its edges on the middle row, HL and HR each edge's height, from two columns just inside it (3 and 13
    view px) extrapolated to the edge, where the gray level crosses half the sheet's. HL and HR are None when an edge
    leaves the frame ("clipped"); frames with nothing lit (level < 60) are left out. Values rounded to 0.001 px, as
    written to measure.tsv on 2026-11-10. Returns (index, time, [XL, XR, HL, HR], level)."""
    w, h, s = rec.w, rec.h, rec.s
    rows = []
    for i, t, frame in rec.frames():
        mid = frame[(h // 2) * w:(h // 2 + 1) * w]
        level = max(mid)
        if level < 60:
            continue
        thr = level / 2.0
        xl, xr = cross(mid, thr), cross(mid, thr, rev=True)
        xl = 0.0 if xl is None else xl
        xr = float(w) if xr is None else xr

        def height(x):
            x = int(round(x))
            if x < 0 or x >= w:
                return None
            col = frame[x::w]
            top, bot = cross(col, thr), cross(col, thr, rev=True)
            return None if top is None or bot is None else bot - top

        o1, o2 = 3 / s, 13 / s
        l1, l2, r1, r2 = height(xl + o1), height(xl + o2), height(xr - o1), height(xr - o2)
        if None in (l1, l2, r1, r2) or xl <= 0.5 or xr >= w - 0.5:
            rows.append((i, round(t, 3), [round(xl * s, 3), round(xr * s, 3), None, None], level))
            continue
        hl = (l1 + (l1 - l2) * o1 / (o2 - o1)) * s
        hr = (r1 + (r1 - r2) * o1 / (o2 - o1)) * s
        rows.append((i, round(t, 3), [round(v, 3) for v in (xl * s, xr * s, hl, hr)], level))
    return rows


def bbox(frame, rec):
    """The lit region's bounds in one frame: (L, T, R, B, level) in view px to 0.1 px (None for an edge not found),
    along its middle row and column, where the gray level crosses half the region's; None if nothing is lit (> 40)."""
    w, h, s = rec.w, rec.h, rec.s
    lit = [y for y in range(0, h, 2) if max(frame[y * w:(y + 1) * w]) > 40]
    if not lit:
        return None
    ymid = (lit[0] + lit[-1]) // 2
    row = frame[ymid * w:(ymid + 1) * w]
    level = max(row)
    thr = level / 2
    xc = int((row.find(bytes([level])) + row.rfind(bytes([level]))) / 2)
    col = frame[xc::w]
    vals = (cross(row, thr), cross(col, thr), cross(row, thr, rev=True), cross(col, thr, rev=True))
    return tuple(None if v is None else round(v * s, 1) for v in vals) + (level,)


def knob_edges(rec, row=64.0, x0=330.0, x1=449.0, t1=30.0):
    """The motion probe's ToggleSwitch knob: in each frame up to t1 s, its left edge in view px to 0.01 (where the gray
    level first rises through 128 between x0 and x1, on the row through the knob above the track), or None.
    Returns (time, x)."""
    s, w = rec.s, rec.w
    y, a, b = int(round(row / s)), int(x0 / s), int(x1 / s)
    out = []
    for i, t in enumerate(rec.times):
        if t > t1:
            break
        rec._f.seek(i * w * rec.h + y * w)
        r = rec._f.read(w)
        x = None
        for k in range(a + 1, b):
            if r[k - 1] < 128 <= r[k]:
                x = round((k - 1 + (128 - r[k - 1]) / (r[k] - r[k - 1])) * s, 2)
                break
        out.append((t, x))
    return out


def knob_lag(edges, run):
    """Recording time - app time, from the knob: each `toggle` step of the probe against the first frame that the
    knob jumped (over 0.5 px, at least 0.6 s after the last jump). Returns (median, the lags)."""
    moves, prev = [], None
    for t, x in edges:
        if x is None:
            prev = None
            continue
        if prev is not None and abs(x - prev) > 0.5 and (not moves or t - moves[-1] > 0.6):
            moves.append(t)
        prev = x
    toggles = [t / 1000 for t, d in steps(run) if d.startswith('toggle')]
    lags = [m - t for t, m in zip(toggles, moves)]
    return median(lags), lags


def segments(rows, tol=0.4):
    """Runs of consecutive outline rows that differ from the first row (rest) by more than tol px, or are clipped."""
    rest = rows[0][2]
    segs, cur = [], []
    for r in rows:
        m = r[2]
        if m[2] is None or any(abs(a - b) > tol for a, b in zip(m, rest) if a is not None):
            cur.append(r)
        elif cur:
            segs.append(cur)
            cur = []
    if cur:
        segs.append(cur)
    return segs


# ---------------------------------------------------------------------------------------------------------------
# The PlaneProjection camera: a pinhole d view px in front of the screen, its vanishing point at (cx, 400)


def project_y(theta, xa, za, d, cx, sheet=(19.0, 431.0), height=680.0):
    """The sheet's left and right edges (X) and heights (H) on the screen, [XL, XR, HL, HR], when it is turned by
    theta degrees about a vertical axis at x = xa, z = za (view px, z toward the viewer; a positive angle brings the
    right side nearer)."""
    r = math.radians(theta)
    out = []
    for x in sheet:
        xp = xa + (x - xa) * math.cos(r) + za * math.sin(r)
        z = za + (x - xa) * math.sin(r) - za * math.cos(r)
        k = d / (d - z)
        out.append((cx + (xp - cx) * k, height * k))
    return [out[0][0], out[1][0], out[0][1], out[1][1]]


def project_plane(theta, c, gz, d, cx, sgn=1):
    """As project_y for a PlaneProjection: RotationY theta about CenterOfRotationX c (a fraction of the 450 wide
    element), then GlobalOffsetZ gz; sgn -1 turns the other way."""
    th = math.radians(theta)
    xa = 450 * c
    out = []
    for x in (19.0, 431.0):
        xp = xa + (x - xa) * math.cos(th)
        z = sgn * (x - xa) * math.sin(th) + gz
        k = d / (d - z)
        out.append((cx + (xp - cx) * k, 680 * k))
    return [out[0][0], out[1][0], out[0][1], out[1][1]]


def fit_camera(held, d_range=range(300, 3001, 10), cx_range=range(150, 301, 5)):
    """The camera distance d and vanishing point cx that fit the held states ((theta, c, gz), [XL, XR, HL, HR]) best:
    a grid, then 1 px and 0.25 px. Returns (rms px, d, cx, sign)."""
    def err(d, cx, sgn):
        se = 0.0
        for (theta, c, gz), m in held:
            se += sum((a - b) ** 2 for a, b in zip(project_plane(theta, c, gz, d, cx, sgn), m))
        return math.sqrt(se / (4 * len(held)))

    best = None
    for sgn in (1, -1):
        for d in d_range:
            for cx in cx_range:
                e = err(d, cx, sgn)
                if best is None or e < best[0]:
                    best = (e, d, cx, sgn)
    e, d, cx, sgn = best
    for dd in range(d - 10, d + 11):
        for cc in [cx + i * 0.25 for i in range(-20, 21)]:
            e2 = err(dd, cc, sgn)
            if e2 < e:
                e, d, cx = e2, dd, cc
    return e, d, cx, sgn


def solve_angle(m, xa, za, d, cx, sign=0):
    """The rotation (degrees) that best explains one frame's outline m (None entries ignored): the best whole degree
    (of the given sign, if any), then a ternary search within 1 degree. Returns (theta, squared error, values used)."""
    def cost(th):
        return sum((a - b) ** 2 for a, b in zip(project_y(th, xa, za, d, cx), m) if b is not None)
    rng = range(0, 90) if sign > 0 else range(-89, 1) if sign < 0 else range(-89, 90)
    best = min(rng, key=cost)
    lo, hi = best - 1.0, best + 1.0
    for _ in range(40):
        a, b = lo + (hi - lo) / 3, hi - (hi - lo) / 3
        if cost(a) < cost(b):
            hi = b
        else:
            lo = a
    th = (lo + hi) / 2
    return th, cost(th), sum(1 for b in m if b is not None)


def axis_rms(segs, xa, za, d, cx):
    """rms px over every unclipped frame of segs, each frame at its own best angle about the axis (xa, za)."""
    se, n = 0.0, 0
    for s in segs:
        for _, _, m, _ in s:
            if m[2] is None:
                continue
            _, e, k = solve_angle(m, xa, za, d, cx)
            se += e
            n += k
    return math.sqrt(se / n)


def fit_axis(segs, d, cx):
    """The vertical axis (x, z view px) that fits every unclipped frame of segs best: a 10 px grid, then a pattern
    search down to 0.2 px. Returns (rms px, x, z)."""
    best = None
    for xa in range(-120, 41, 10):
        for za in range(-200, 1, 10):
            e = axis_rms(segs, xa, za, d, cx)
            if best is None or e < best[0]:
                best = (e, xa, za)
    e, xa, za = best
    step = 5.0
    while step > 0.2:
        moved = False
        for dx, dz in ((step, 0), (-step, 0), (0, step), (0, -step)):
            e2 = axis_rms(segs, xa + dx, za + dz, d, cx)
            if e2 < e:
                e, xa, za, moved = e2, xa + dx, za + dz, True
        if not moved:
            step /= 2
    return e, xa, za


# ---------------------------------------------------------------------------------------------------------------
# Easing curves, f(u) from 0 to 1 over u in 0..1


def expo_out(k):
    return lambda u: (1 - math.exp(-k * u)) / (1 - math.exp(-k))


def key_spline(x1, y1, x2, y2):
    """A KeySpline: the cubic Bezier from (0, 0) to (1, 1) with control points (x1, y1), (x2, y2), as y(x)."""
    def f(x):
        sv = x
        for _ in range(10):
            xs = 3 * x1 * sv * (1 - sv) ** 2 + 3 * x2 * sv * sv * (1 - sv) + sv ** 3
            dx = 3 * x1 * (1 - sv) ** 2 + 6 * (x2 - x1) * sv * (1 - sv) + 3 * (1 - x2) * sv * sv
            if abs(dx) < 1e-9:
                break
            sv = min(1.0, max(0.0, sv - (xs - x) / dx))
        return 3 * y1 * sv * (1 - sv) ** 2 + 3 * y2 * sv * sv * (1 - sv) + sv ** 3
    return f


def power_out(p):
    return lambda u: 1 - (1 - u) ** p


def circle_out(u):
    return math.sqrt(1 - (1 - u) ** 2)


# ---------------------------------------------------------------------------------------------------------------
# Fits to an animation's distinct values, one per tick of the 64 Hz clock


def distinct(vals, decimals=1, tol=0.25, min_abs=0.0):
    """The distinct values of many runs of one animation, largest first: rounded, those under min_abs dropped, and a
    value within tol of the one before it taken as the same step."""
    vals = sorted(set(round(v, decimals) for v in vals if abs(v) >= min_abs), key=abs, reverse=True)
    out = []
    for v in vals:
        if out and abs(out[-1] - v) <= tol:
            continue
        out.append(v)
    return out


def tick_numbers(vals):
    """Each distinct value's tick: consecutive, unless the ratio to the one before is near a power of the median
    ratio, which means a step no run showed. Returns (median ratio, ticks)."""
    ratios = sorted(vals[i + 1] / vals[i] for i in range(len(vals) - 1))
    r0 = ratios[len(ratios) // 2]
    n = [0]
    for i in range(1, len(vals)):
        q = vals[i] / vals[i - 1]
        n.append(n[-1] + max(1, round(math.log(q) / math.log(r0))))
    return r0, n


def fit_exponential(vals, ticks, eased):
    """A value falling by r a tick: pure, v_n = A r^n; or eased, an exponential ease-out reaching 0 at tick N,
    v_n = A (r^n - r^N) / (1 - r^N). r on a 0.0001 grid in 0.7..0.9, N up to 59 ticks past the last value; A by least
    squares. Returns (rms, r, N or None, A, time constant ms, exponent k = N TICK / time constant or None)."""
    best = None
    for ri in range(7000, 9000):
        r = ri / 10000
        for N in (range(ticks[-1] + 1, ticks[-1] + 60) if eased else [None]):
            g = [r ** k if N is None else (r ** k - r ** N) / (1 - r ** N) for k in ticks]
            A = sum(a * b for a, b in zip(g, vals)) / sum(a * a for a in g)
            se = sum((A * a - b) ** 2 for a, b in zip(g, vals))
            if best is None or se < best[0]:
                best = (se, r, N, A)
    se, r, N, A = best
    tau = -TICK / math.log(r)
    return math.sqrt(se / len(vals)), r, N, A, tau, (None if N is None else N * TICK / tau)


def fit_scaled(vals, ticks, f, durations=range(200, 801, 5), phases=[i / 5 for i in range(50)]):
    """value = A (1 - f((phase + tick) TICK / duration)), A free (least squares), for the duration and phase (ticks
    from the start to the first value) on their grids. Returns (rms, duration ms, phase ticks, A)."""
    best = None
    for d in durations:
        for ph in phases:
            g = [(0.0 if (ph + k) * TICK / d >= 1 else 1 - f((ph + k) * TICK / d)) for k in ticks]
            gg = sum(x * x for x in g)
            if gg == 0:
                continue
            A = sum(a * b for a, b in zip(g, vals)) / gg
            se = sum((A * a - b) ** 2 for a, b in zip(g, vals))
            if best is None or se < best[0]:
                best = (se, d, ph, A)
    se, d, ph, A = best
    return math.sqrt(se / len(vals)), d, ph, A


def fit_fraction(fracs, f, durations=range(200, 701, 5), phases=[i / 5 for i in range(30)]):
    """The remaining fraction 1 - f((phase + k) TICK / duration) at consecutive ticks k, for the duration and phase on
    their grids. Returns (rms, duration ms, phase ticks)."""
    best = None
    for d in durations:
        for ph in phases:
            se = 0.0
            for k, v in enumerate(fracs):
                u = (ph + k) * TICK / d
                se += ((0.0 if u >= 1 else 1 - f(u)) - v) ** 2
            if best is None or se < best[0]:
                best = (se, d, ph)
    se, d, ph = best
    return math.sqrt(se / len(fracs)), d, ph


def median(vals):
    return statistics.median(vals)
