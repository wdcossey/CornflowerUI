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
/// within 0.019° (rms 0.006°), and the depths within 0.04.
/// </para>
/// <para>
/// <b>The projection.</b> The phone puts a <c>PlaneProjection</c> on the template root, about its
/// centre, and translates the root by <see cref="EyeShift"/> × (the screen's centre − the root's
/// centre) with a <c>RenderTransform</c>, cancelled by the projection's <c>GlobalOffsetX/Y</c>.
/// The projection's camera is on its own element's centre; the translation moves the element and
/// leaves the camera, so the eye ends up over the point that far towards the screen's centre.
/// <see cref="EyeDistance"/> is that camera's distance, fitted to the grid. Avalonia has no
/// projection, so <see cref="ToMatrix"/> builds the same mapping as one perspective matrix, with
/// the eye placed directly.
/// </para>
/// </remarks>
internal static class TiltModel
{
    /// <summary>The greatest angle, 0.3 rad (17.19°), in degrees.</summary>
    public const double MaxAngle = 0.3 * 180 / Math.PI;

    /// <summary>The greatest depth, in view pixels: a press at the centre sinks this far.</summary>
    public const double MaxDepth = 25;

    /// <summary>How far the eye sits towards the screen's centre from the element's: the 0.8 of the phone's translation.</summary>
    public const double EyeShift = 0.8;

    /// <summary>
    /// The eye's distance from the screen, in view pixels. Fitted to the grid's 200 angles on the
    /// reference profile (rms least at 1,050 to 1,060); whether it scales with the screen is open.
    /// </summary>
    public const double EyeDistance = 1055;

    /// <summary>How long the tilt holds after the finger lets go: 180 ms (fit to 1,199 frames: 178).</summary>
    public static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(180);

    /// <summary>How long it then takes to ease back, on a sine ease-out: 195 ms (fit: 196).</summary>
    public static readonly TimeSpan Return = TimeSpan.FromMilliseconds(195);

    /// <summary>Below this change in a frame, in degrees and view pixels, a press has settled.</summary>
    public const double Settled = 1e-4;

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
    public static TiltState Step(TiltState current, Size size, Point press, Point eye)
    {
        var local = Unproject(current, size, press, eye);
        return ForOffset(local.X / size.Width - 0.5, local.Y / size.Height - 0.5);
    }

    /// <summary>Where the eye sits, in the element's space, for an element whose centre is <paramref name="toScreenCentre"/> from the screen's.</summary>
    public static Point Eye(Size size, Vector toScreenCentre) =>
        new Point(size.Width / 2, size.Height / 2) + toScreenCentre * EyeShift;

    /// <summary>How much of the held tilt is left <paramref name="sinceRelease"/> after the finger let go: 1, then a sine ease-out to 0.</summary>
    public static double ReturnFactor(TimeSpan sinceRelease)
    {
        var progress = (sinceRelease - Hold) / Return;
        if (progress <= 0) return 1;
        if (progress >= 1) return 0;
        return 1 - Math.Sin(progress * Math.PI / 2);
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
    public static Point Unproject(TiltState tilt, Size size, Point press, Point eye)
    {
        var r = Rotation(tilt);
        double cx = size.Width / 2, cy = size.Height / 2, d = EyeDistance;

        // The plane's normal, R (0, 0, 1), and a point on it, its centre (cx, cy, z).
        double nx = r.M02, ny = r.M12, nz = r.M22;
        // The line from the eye (eye, d) through the press (press, 0).
        double lx = press.X - eye.X, ly = press.Y - eye.Y, lz = -d;
        var t = ((cx - eye.X) * nx + (cy - eye.Y) * ny + (tilt.OffsetZ - d) * nz) / (lx * nx + ly * ny + lz * nz);
        double hx = eye.X + t * lx - cx, hy = eye.Y + t * ly - cy, hz = d + t * lz - tilt.OffsetZ;

        // Back into the plane's own axes: Rᵀ.
        return new Point(
            cx + r.M00 * hx + r.M10 * hy + r.M20 * hz,
            cy + r.M01 * hx + r.M11 * hy + r.M21 * hz);
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
