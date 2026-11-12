using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ProgressRing as the phone has it: the values its default style and template set (the 8.1 Update 1
/// runtime's <c>progressring.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), what the
/// reference app read and drew of an active <c>ProgressRing</c> on the reference profile (run
/// <c>20261108-1732-dev11</c>, device 11, 1080 × 1920 at 2.4; PNGs in <c>ref/wp81/ProgressRing/</c>), and what its
/// probes read (<c>tools/refapp/README.md</c>). Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ProgressRingReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 120 for the ring, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 120;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 12.0833, 412.0833, 107.9167], whole raw pixels at 2.4 (46, 29, 989,
    // 259): the ring stretches to its slot.
    public static readonly Rect InStage = new(46 / 2.4, 29 / 2.4, 989 / 2.4, 259 / 2.4);

    // The dump's Ring, in the ProgressRing: [177.0833, 25.4167, 57.5, 57.5], centred to the raw pixel.
    public static readonly Rect RingBounds = new(425 / 2.4, 61 / 2.4, 57.5, 57.5);

    // The dump's E1 at rest (ActiveStates.Inactive): [184.5833, 32.9167, 6.5, 6.5] in the ProgressRing, so 7.5 in
    // from Ring's top left (ProgressRingElipseThemeMargin), 6.5 across (ProgressRingElipseThemeSize).
    public static readonly Rect DotInRing = new(7.5, 7.5, 6.5, 6.5);

    // The dump of SizeStates.Small, a moment of the running storyboard: E1's canvas turned -0.5973°, and E1's
    // bounds [184.363, 33.0716, 6.5674, 6.5674] in the ProgressRing, the turned square's box. In Ring:
    public const double TurnedAngle = -0.5973;
    public static readonly Rect TurnedDotInRing = new(184.363 - 425 / 2.4, 33.0716 - 61 / 2.4, 6.5674, 6.5674);

    // The style's setters (progressring.json), with themeresources.json's values, as the dump read them.
    public const double MinSide = 57.5;                                          // ProgressRingThemeSize
    public const double FontSize = 18.14;                                        // TextStyleLargeFontSize
    public const double ActiveOverlayOpacity = 0.8;                              // ProgressRingActiveBackgroundOpacity; dump: 0.8 active, 0 inactive
    public static readonly Color Accent = Color.Parse("#FF3E65FF");              // ProgressBarIndeterminateForegroundThemeBrush: the accent, Cobalt
    public static Color Background(bool dark) => dark ? Colors.Black : Colors.White; // PhoneBackgroundBrush; dump: #FF000000 / #FFFFFFFF

    // TemplateSettings, read through the API on device 11 (#72, tools/refapp/README.md "ProgressRing"):
    // (width, height) → (MaxSideLength, EllipseDiameter, EllipseOffset's top).
    public static readonly (double Width, double Height, double Side, double Diameter, double OffsetTop)[] Settings =
    [
        (989 / 2.4, 259 / 2.4, 107.92, 10.79, 43.17),                            // the catalogue's ring, 989 x 259 raw
        (57.5, 57.5, 57.5, 5.75, 23),                                            // at its default size
        (120, 120, 120, 12, 48),                                                 // 120 square
    ];

    // SizeStates (-Probe ringsize, 20261111-2316-dev11): Large when the smaller laid-out side is over 60 view px.
    // Each (width, height, large) as the probe set them; the laid-out sizes snap to raw pixels (0.417 at 2.4).
    public static readonly (double Width, double Height, bool Large)[] SizeStates =
    [
        (59, 59, false),
        (60, 60, false),
        (60.25, 60.25, true),                                                    // laid out 60.417
        (61, 61, true),
        (130, 130, true),
        (200, 60, false),
        (60, 200, false),
        (200, 61, true),
        (61, 200, true),
    ];

    // The Active storyboard (progressring.json, ActiveStates, Active; RepeatBehavior Forever), as the plan lists it.
    // Each dot's begin time, its angle keys (SplineDoubleKeyFrame; the KeySplines, null for none) and its opacity
    // keys (DiscreteDoubleKeyFrame).
    public static readonly double[] BeginSeconds = [0, 0.167, 0.334, 0.501, 0.668, 0.835];
    public static readonly double[] AngleSeconds = [0, 0.433, 1.2, 1.617, 2.017, 2.783, 3.217];
    public static readonly double[][] Angles =
    [
        [-110, 10, 93, 205, 357, 439, 585],                                      // E1R
        [-116, 4, 87, 199, 351, 433, 579],                                       // E2R
        [-122, -2, 81, 193, 345, 427, 573],                                      // E3R
        [-128, -8, 75, 187, 339, 421, 567],                                      // E4R
        [-134, -14, 69, 181, 331, 415, 561],                                     // E5R
        [-140, -20, 63, 175, 325, 409, 555],                                     // E6R
    ];
    public static readonly double[]?[] Splines =
    [
        [0.13, 0.21, 0.1, 0.7],
        [0.02, 0.33, 0.38, 0.77],
        null,
        [0.57, 0.17, 0.95, 0.75],
        [0, 0.19, 0.07, 0.72],
        null,
        [0, 0, 0.95, 0.37],
    ];
    // Moments of the running storyboard the catalogue's captures caught (run 20261108-1732-dev11; the dumps'
    // canvases' angles, in degrees, of the dots shown; the others at opacity 0). Each is one storyboard time.
    public static readonly (string Capture, double[] Shown)[] Moments =
    [
        ("dark base", [-0.5973, -36.3981, -106.8601]),
        ("dark SizeStates.Small", [49.3787, 25.3259, 1.2437, -25.617, -66.6872]),
        ("dark SizeStates.Large", [51.07, 27.01, 2.93, -23.21, -62.33]),
        ("light base", [-5.0625, -44.0645]),
        ("light SizeStates.Small", [40.93, 16.9, -7.35, -39.55, -93.92]),
        ("light SizeStates.Large", [44.31, 20.26, -3.78, -33.56, -81.46]),
    ];

    public const double ShowsUntilSeconds = 3.22;                                 // opacity 1 from 0, 0 from 3.22 (3.21 → 1, 3.47 → 0)
    public const double CycleSeconds = 4.305;                                    // the last dot's 0.835 and its 3.47

    /// <summary>
    /// The template's angle for a dot at a time in the storyboard, worked out here from the keys above, apart from
    /// the theme's code: each key reached from the one before through its KeySpline, a cubic Bézier from (0, 0) to
    /// (1, 1), solved for its x by bisection. Null before the dot's begin time.
    /// </summary>
    public static double? AngleAt(int dot, double storyboardSeconds)
    {
        var local = Local(dot, storyboardSeconds);
        if (local < 0)
        {
            return null;
        }

        for (var i = 1; i < AngleSeconds.Length; i++)
        {
            if (local > AngleSeconds[i])
            {
                continue;
            }

            var progress = (local - AngleSeconds[i - 1]) / (AngleSeconds[i] - AngleSeconds[i - 1]);
            var eased = Splines[i] is { } s ? Bezier(s[0], s[1], s[2], s[3], progress) : progress;
            return Angles[dot][i - 1] + (Angles[dot][i] - Angles[dot][i - 1]) * eased;
        }

        return Angles[dot][^1];
    }

    /// <summary>The template's opacity for a dot at a time in the storyboard; null before the dot's begin time.</summary>
    public static double? OpacityAt(int dot, double storyboardSeconds)
    {
        var local = Local(dot, storyboardSeconds);
        return local < 0 ? null : local < ShowsUntilSeconds ? 1 : 0;
    }

    /// <summary>The time into a dot's track, to the microsecond, so a time made by adding up frames meets a key time exactly.</summary>
    private static double Local(int dot, double storyboardSeconds) => Math.Round((storyboardSeconds - BeginSeconds[dot]) * 1e6) / 1e6;

    /// <summary>A KeySpline's y at x: the Bézier with control points (x1, y1) and (x2, y2), solved for its parameter.</summary>
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
}
