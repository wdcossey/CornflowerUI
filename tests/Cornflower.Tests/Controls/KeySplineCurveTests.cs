using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// <see cref="KeySplineCurve"/>, the one KeySpline every Cornflower timeline eases through: the
/// ProgressRing's angle keys and the tilt's return, against the Bézier itself.
/// </summary>
public class KeySplineCurveTests
{
    /// <summary>A KeySpline's y at x: the Bézier with control points (x1, y1) and (x2, y2), solved for its parameter by bisection, the reference the tests hold the curve to.</summary>
    public static double Bezier(double x1, double y1, double x2, double y2, double x)
    {
        static double At(double a, double b, double t) => 3 * a * t * (1 - t) * (1 - t) + 3 * b * t * t * (1 - t) + t * t * t;
        double lo = 0, hi = 1;
        for (var i = 0; i < 60; i++)
        {
            var mid = (lo + hi) / 2;
            if (At(x1, x2, mid) < x)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return At(y1, y2, (lo + hi) / 2);
    }

    // The ring's angle keys and the tilt's return: the curve gives the y the Bézier does at every x, so a
    // spline key is reached as on the phone. (Avalonia's own KeySpline is up to 0.0006 off the ring's; the
    // themes don't use it.)
    [Fact]
    public void A_KeySpline_Eases_As_The_Templates_Bezier_Does()
    {
        var tilt = TiltModel.ReturnSpline;
        var curves = ProgressRingReadings.Splines.OfType<double[]>().Select(s => new KeySplineCurve(s[0], s[1], s[2], s[3]))
            .Append(tilt).ToList();
        Assert.True(curves.Count > 1);
        foreach (var curve in curves)
        {
            for (var x = 0.0; x <= 1.0; x += 0.01)
            {
                Assert.Equal(Bezier(curve.X1, curve.Y1, curve.X2, curve.Y2, x), curve.Ease(x), 1e-9);
            }
        }
    }

    // Solved, not approximated, near the corners too (x1 near 0, flat ends): at any parameter s, the curve's
    // value at the Bézier's x(s) is its y(s). The solver stops at an x within 1e-12, which on a steep curve
    // leaves about 1e-11 in y; 1e-9 holds that with room.
    [Theory]
    [InlineData(0.1, 0.25, 0.75, 0.9)]
    [InlineData(0.001, 0.27, 0.77, 0.91)]
    [InlineData(0.1, 0.9, 0.2, 1)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(0.5, 0, 0.5, 1)]
    [InlineData(0.42, 0, 0.58, 1)]
    public void The_Curve_Is_Exact(double x1, double y1, double x2, double y2)
    {
        var curve = new KeySplineCurve(x1, y1, x2, y2);
        for (var s = 0.0; s <= 1; s += 1.0 / 64)
        {
            double r = 1 - s;
            var x = 3 * x1 * s * r * r + 3 * x2 * s * s * r + s * s * s;
            var y = 3 * y1 * s * r * r + 3 * y2 * s * s * r + s * s * s;
            Assert.Equal(y, curve.Ease(x), 1e-9);
        }
    }
}
