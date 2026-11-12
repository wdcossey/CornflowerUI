namespace Cornflower.Controls;

/// <summary>
/// A <c>KeySpline</c>, as WinRT's keyframes and theme animations use it: the cubic Bézier from (0, 0) to (1, 1) with
/// control points (<paramref name="X1"/>, <paramref name="Y1"/>) and (<paramref name="X2"/>, <paramref name="Y2"/>),
/// mapping a time's progress (x) to a value's (y). Every Cornflower timeline that follows one eases through this.
/// </summary>
/// <remarks>
/// Solved to 1e-12, by Newton's method with bisection to fall back on. Not Avalonia's
/// <c>KeySpline.GetSplineProgress</c>, which approximates: on the 8.1 ProgressRing's keys it is up to 0.0006 off the
/// curve, 0.09° of a dot's angle (<c>ProgressRingTests</c>).
/// </remarks>
internal sealed record KeySplineCurve(double X1, double Y1, double X2, double Y2)
{
    /// <summary>The curve's y at <paramref name="x"/>, from 0 to 1; 0 and 1 outside.</summary>
    public double Ease(double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;

        // Newton's method on x(t) = x from t = x, then bisection if it strays or stalls.
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = At(X1, X2, t) - x;
            if (Math.Abs(error) < 1e-12) return At(Y1, Y2, t);
            var slope = Slope(X1, X2, t);
            if (Math.Abs(slope) < 1e-9) break;
            t -= error / slope;
            if (t is < 0 or > 1) break;
        }

        double lo = 0, hi = 1;
        for (var i = 0; i < 64; i++)
        {
            t = (lo + hi) / 2;
            var error = At(X1, X2, t) - x;
            if (Math.Abs(error) < 1e-12) break;
            if (error < 0) lo = t;
            else hi = t;
        }

        return At(Y1, Y2, t);
    }

    private static double At(double a, double b, double t) => ((1 - 3 * b + 3 * a) * t + (3 * b - 6 * a)) * t * t + 3 * a * t;

    private static double Slope(double a, double b, double t) => 3 * (1 - 3 * b + 3 * a) * t * t + 2 * (3 * b - 6 * a) * t + 3 * a;
}
