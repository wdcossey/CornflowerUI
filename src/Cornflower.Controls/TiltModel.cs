using Avalonia;

namespace Cornflower.Controls;

/// <summary>
/// A tilt at one moment, in the phone's <c>PlaneProjection</c> terms: <see cref="RotationX"/> and
/// <see cref="RotationY"/> in degrees, <see cref="OffsetZ"/> in view pixels, negative away from the
/// viewer (its <c>GlobalOffsetZ</c>).
/// </summary>
internal readonly record struct TiltState(double RotationX, double RotationY, double OffsetZ)
{
    public static readonly TiltState Rest = default;

    public TiltState Scale(double factor) => new(RotationX * factor, RotationY * factor, OffsetZ * factor);
}

/// <summary>
/// The 8.1 tilt's geometry and timing, measured in the emulator (#60, <c>tools/refapp/README.md</c>,
/// "Tilt: the model"). Pure functions; <see cref="Tilt"/> drives them frame by frame.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule.</b> With <c>dx</c>, <c>dy</c> the press point's offset from the element's centre as
/// a fraction of its width and height, clamped to ±0.5, <c>RotationY</c> = −<see cref="MaxAngle"/>
/// × <c>dx</c>, <c>RotationX</c> = <see cref="MaxAngle"/> × <c>dy</c>, and <c>OffsetZ</c> =
/// −<see cref="MaxDepth"/> × (1 − (|<c>RotationX</c>| + |<c>RotationY</c>|) / <see cref="MaxAngle"/>):
/// the Silverlight toolkit TiltEffect's rule and constants.
/// </para>
/// <para>
/// <b>The press point is hit-tested through the tilt.</b> Each frame the phone takes the finger's
/// point on the element as the tilted, projected element shows it, and applies the rule to that.
/// So a press settles over a few frames at a fixed point, a little further out than the finger
/// is, and further for an element far from the screen's centre. That is the whole of #60's
/// (1 + 0.08 |d|) term and of its bias of up to 1° by screen position: with the linear rule and
/// this step, the settled angles match all 100 grid presses of run <c>20261109-2307-dev11</c>
/// within 0.026° (rms 0.008°), and the depths within 0.06; on the 720p profile (run
/// <c>20261110-0851-dev10</c>), within 0.021° and 0.045, with nothing changed.
/// </para>
/// <para>
/// <b>The projection.</b> The phone puts a <c>PlaneProjection</c> on the template root, about its
/// centre, and translates the root by 0.8 × (the screen's centre − the root's centre) with a
/// <c>RenderTransform</c>, cancelled by the projection's <c>GlobalOffsetX/Y</c>. That moves the
/// element and not the projection's camera, so the eye ends up over a point towards the screen's
/// centre. <see cref="EyeDistance"/> is the camera's distance as measured (#66), and
/// <see cref="EyeShift"/> is fitted to the grid with it. The grid fixes their ratio, not each:
/// 0.8 at 1,055 fits as well. Avalonia has no projection, so <see cref="ToMatrix"/> builds the
/// same mapping as one perspective matrix, with the eye placed directly.
/// </para>
/// </remarks>
internal static class TiltModel
{
    /// <summary>The greatest angle, 0.3 rad (17.19°), in degrees.</summary>
    public const double MaxAngle = 0.3 * 180 / Math.PI;

    /// <summary>The greatest depth, in view pixels: a press at the centre sinks this far.</summary>
    public const double MaxDepth = 25;

    /// <summary>
    /// How far the eye sits towards the screen's centre from the element's, as a fraction. Fitted
    /// to the grid's 200 angles with <see cref="EyeDistance"/> fixed (least rms at 0.755 to 0.76).
    /// The phone translates the element by 0.8, so the eye is not quite over its translated centre.
    /// </summary>
    public const double EyeShift = 0.76;

    /// <summary>
    /// The eye's distance from the screen, in view pixels: <c>PlaneProjection</c>'s camera, as
    /// stream A's <c>projections</c> probe measured it (#66, <c>tools/refapp/README.md</c>, "Motion").
    /// The same in view pixels on the 720p profile, so it doesn't scale with the screen.
    /// </summary>
    public const double EyeDistance = 999;

    /// <summary>
    /// How long the tilt holds after the finger lets go: 182.6 ms of animation time. The emulator's
    /// release moves 15.625 ms of animation time a frame, however far apart its frames come (#84);
    /// re-read so from the 252 releases of the five tilt runs (<c>tools/motion/tiltrelease.py</c>):
    /// the first change is the 13th frame after the release, counted by the stopwatch (12.4 frames'
    /// spacing on average), with the first frame after the release taken as time 0. Were time 0 the
    /// second frame, the hold would be 167.0 ms. (#60 read 178 against the stopwatch.)
    /// </summary>
    public static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(182.6);

    /// <summary>
    /// How long it then takes to ease back, on <see cref="ReturnSpline"/>: 167.0 ms of animation
    /// time, fitted with <see cref="Hold"/> to the release's eleven frames (#60 fitted a sine over
    /// 196 ms against the stopwatch; in frames the sine is out by up to 0.027 of the tilt).
    /// </summary>
    public static readonly TimeSpan Return = TimeSpan.FromMilliseconds(167.0);

    /// <summary>
    /// The return's easing, a <c>KeySpline</c> 0.1,0.25 0.75,0.9: the fraction of the tilt gone
    /// at a fraction of <see cref="Return"/>. Every release reads the same fraction at each frame,
    /// within 0.0002; this fits them within 0.00075 (rms 0.00024), and within 0.0010 with the hold and
    /// return rounded to 182.6 and 167.0 ms, as here.
    /// </summary>
    public static readonly (double X1, double Y1, double X2, double Y2) ReturnSpline = (0.1, 0.25, 0.75, 0.9);

    /// <summary>Below this change in a frame, in degrees and view pixels, a press has settled.</summary>
    public const double Settled = 1e-4;

    /// <summary>
    /// The most frames a press steps for at one point of the finger. Every press the readings
    /// cover settles in four or five. An element tens of thousands of pixels across, pressed far
    /// from the eye, can swing between tilts for ever; after this many, it takes the rule for the
    /// point as it is, untilted (a step from rest), and stops.
    /// </summary>
    public const int MaxSteps = 30;

    /// <summary>The rule itself, for a press at fractions <paramref name="dx"/>, <paramref name="dy"/> from the centre.</summary>
    public static TiltState ForOffset(double dx, double dy)
    {
        dx = Math.Clamp(dx, -0.5, 0.5);
        dy = Math.Clamp(dy, -0.5, 0.5);
        var rotationX = MaxAngle * dy;
        var rotationY = -MaxAngle * dx;
        var offsetZ = -MaxDepth * (1 - (Math.Abs(rotationX) + Math.Abs(rotationY)) / MaxAngle);
        return new TiltState(rotationX, rotationY, offsetZ);
    }

    /// <summary>
    /// One frame's step: the tilt for <paramref name="press"/> as an element of <paramref name="size"/>
    /// tilted by <paramref name="current"/> shows it. All points are in the element's own untilted
    /// space; <paramref name="eye"/> is the point under the eye.
    /// </summary>
    public static TiltState Step(TiltState current, Size size, Point press, Point eye) =>
        Step(current, size, press, eye, Matrix.Identity);

    /// <summary>
    /// <see cref="Step(TiltState, Size, Point, Point)"/> for an element with a transform of its own,
    /// <paramref name="inner"/>, on its space and drawn before the tilt, as the phone draws a
    /// <c>RenderTransform</c> before a <c>PlaneProjection</c>: the point under the tilt is taken back
    /// through it too, to the point of the element. One that can't be undone is left out.
    /// </summary>
    public static TiltState Step(TiltState current, Size size, Point press, Point eye, Matrix inner)
    {
        var local = Unproject(current, size, press, eye);
        if (!inner.IsIdentity && inner.TryInvert(out var back) && back.Transform(local) is var undone &&
            double.IsFinite(undone.X) && double.IsFinite(undone.Y))
            local = undone;
        return ForOffset(local.X / size.Width - 0.5, local.Y / size.Height - 0.5);
    }

    /// <summary>Where the eye sits, in the element's space, for an element whose centre is <paramref name="toScreenCentre"/> from the screen's.</summary>
    public static Point Eye(Size size, Vector toScreenCentre) =>
        new Point(size.Width / 2, size.Height / 2) + toScreenCentre * EyeShift;

    /// <summary>How much of the held tilt is left <paramref name="sinceRelease"/> after the finger let go: 1, then down <see cref="ReturnSpline"/> to 0.</summary>
    public static double ReturnFactor(TimeSpan sinceRelease)
    {
        var progress = (sinceRelease - Hold) / Return;
        if (progress <= 0) return 1;
        if (progress >= 1) return 0;
        var (x1, y1, x2, y2) = ReturnSpline;
        return 1 - Spline(progress, x1, y1, x2, y2);
    }

    /// <summary>
    /// A <c>KeySpline</c>'s value at <paramref name="x"/>: the cubic Bézier from (0, 0) to (1, 1)
    /// with control points (<paramref name="x1"/>, <paramref name="y1"/>) and (<paramref name="x2"/>,
    /// <paramref name="y2"/>), its parameter solved from x by Newton's method kept inside a bisection
    /// bracket, to 1e-12. The control points' x are in [0, 1], so x rises with the parameter.
    /// </summary>
    internal static double Spline(double x, double x1, double y1, double x2, double y2)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        double lo = 0, hi = 1, s = x;
        for (var i = 0; i < 200; i++)
        {
            var error = Bezier(s, x1, x2) - x;
            if (Math.Abs(error) <= 1e-12) break;
            if (error > 0) hi = s;
            else lo = s;
            var slope = BezierSlope(s, x1, x2);
            var next = slope > 0 ? s - error / slope : double.NaN;
            s = next > lo && next < hi ? next : (lo + hi) / 2;
        }

        return Bezier(s, y1, y2);
    }

    private static double Bezier(double s, double a, double b)
    {
        var r = 1 - s;
        return 3 * a * s * r * r + 3 * b * s * s * r + s * s * s;
    }

    private static double BezierSlope(double s, double a, double b)
    {
        var r = 1 - s;
        return 3 * a * r * r + 6 * (b - a) * s * r + 3 * (1 - b) * s * s;
    }

    /// <summary>
    /// The tilt as a 2D perspective matrix on the element's own space (origin top left): a point of
    /// the element goes to where the tilted, sunk element is drawn, seen from the eye. Avalonia's
    /// row-vector convention: (x, y, 1) × matrix = (X, Y, W), drawn at (X / W, Y / W).
    /// </summary>
    public static Matrix ToMatrix(TiltState tilt, Size size, Point eye)
    {
        var r = Rotation(tilt);
        double cx = size.Width / 2, cy = size.Height / 2, d = EyeDistance;

        // A point (x, y) is u = x − cx, v = y − cy from the centre. In 3D it goes to
        // P = R (u, v, 0) + (cx, cy, z), and is drawn at eye + (P − eye) × d / (d − P.z).
        // With W = (d − P.z) / d, the drawn point times W is eye × W + P.xy − eye: linear in x, y.
        var wx = -r.M20 / d;
        var wy = -r.M21 / d;
        var w0 = 1 - tilt.OffsetZ / d - wx * cx - wy * cy;

        return new Matrix(
            eye.X * wx + r.M00, eye.Y * wx + r.M10, wx,
            eye.X * wy + r.M01, eye.Y * wy + r.M11, wy,
            eye.X * w0 - r.M00 * cx - r.M01 * cy + cx - eye.X,
            eye.Y * w0 - r.M10 * cx - r.M11 * cy + cy - eye.Y,
            w0);
    }

    /// <summary>
    /// The point of the element under <paramref name="press"/>, as the tilted element is drawn: where
    /// the line from the eye through the press meets the tilted plane, in the element's own space.
    /// </summary>
    /// <remarks>
    /// For an element thousands of pixels across, pressed far enough from the eye, the line meets
    /// the plane's extension behind the eye or not at all: no point of the element is drawn there.
    /// Then the press is taken as it is, untilted, so a step stays finite and settles; without that,
    /// the steps swing between corners every frame and never settle.
    /// </remarks>
    public static Point Unproject(TiltState tilt, Size size, Point press, Point eye)
    {
        var r = Rotation(tilt);
        double cx = size.Width / 2, cy = size.Height / 2, d = EyeDistance;

        // The plane's normal, R (0, 0, 1), and a point on it, its centre (cx, cy, z).
        double nx = r.M02, ny = r.M12, nz = r.M22;
        // The line from the eye (eye, d) through the press (press, 0): t = 0 at the eye, 1 at the screen.
        double lx = press.X - eye.X, ly = press.Y - eye.Y, lz = -d;
        var t = ((cx - eye.X) * nx + (cy - eye.Y) * ny + (tilt.OffsetZ - d) * nz) / (lx * nx + ly * ny + lz * nz);
        if (!(t > 0) || !double.IsFinite(t))
            return press;                                           // parallel, or met behind the eye
        double hx = eye.X + t * lx - cx, hy = eye.Y + t * ly - cy, hz = d + t * lz - tilt.OffsetZ;

        // Back into the plane's own axes: Rᵀ.
        var local = new Point(
            cx + r.M00 * hx + r.M10 * hy + r.M20 * hz,
            cy + r.M01 * hx + r.M11 * hy + r.M21 * hz);
        return double.IsFinite(local.X) && double.IsFinite(local.Y) ? local : press;
    }

    /// <summary>
    /// The rotation, as a column-vector matrix on (x, y, z) with y down and z towards the viewer:
    /// <c>RotationX</c> first, then <c>RotationY</c>, which fits the readings (the other order is
    /// 20 times worse). A negative <c>RotationX</c> takes the top away; a positive <c>RotationY</c>
    /// takes the left away. So the side pressed sinks.
    /// </summary>
    private static Rotation3 Rotation(TiltState tilt)
    {
        var a = tilt.RotationX * Math.PI / 180;
        var b = tilt.RotationY * Math.PI / 180;
        double ca = Math.Cos(a), sa = Math.Sin(a), cb = Math.Cos(b), sb = Math.Sin(b);

        // RY(b) × RX(a), with RX(a) = [1 0 0; 0 ca sa; 0 −sa ca] and RY(b) = [cb 0 −sb; 0 1 0; sb 0 cb].
        return new Rotation3(
            cb, sb * sa, -sb * ca,
            0, ca, sa,
            sb, -cb * sa, cb * ca);
    }

    /// <summary>A 3 × 3 rotation, row by row: Mij is row i, column j.</summary>
    private readonly record struct Rotation3(
        double M00, double M01, double M02,
        double M10, double M11, double M12,
        double M20, double M21, double M22);
}
