using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ProgressBar as the phone has it: the values its default style and template set (the 8.1 Update 1 runtime's
/// <c>progressbar.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), what the reference app
/// read and drew of the catalogue's two bars, <c>new ProgressBar { Value = 40 }</c> and
/// <c>new ProgressBar { IsIndeterminate = true }</c>, on the reference profile (runs <c>20261108-1443-dev11</c> and
/// <c>20261108-1732-dev11</c>, device 11, 1080 × 1920 at 2.4; PNGs in <c>ref/wp81/ProgressBar/</c>), and the
/// <c>TemplateSettings</c> it read through the API on all seven profiles (<c>-Native</c>, runs <c>20261111-1749-dev11</c>
/// to <c>-1842-dev13</c>; tools/refapp/README.md). Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ProgressBarReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 60 for the bar, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 60;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 34.5833, 412.0833, 3.3333], whole raw pixels at 2.4 (46, 83, 989, 8): the
    // bar stretched across its slot and centred down it, 3.5 high (MinHeight) rounded to the nearest raw pixel.
    public static readonly Rect InStage = new(46 / 2.4, 83 / 2.4, 989 / 2.4, 8 / 2.4);

    // The style's setters (progressbar.json), with themeresources.json's values, as the dump read them.
    public const double MinHeight = 3.5;                                         // ProgressBarThemeMinHeight
    public const double DotSize = 3.5;                                           // ProgressBarIndeterminateRectagleThemeSize
    public const double TrackOpacity = 0.16;                                     // ProgressBarBackgroundThemeBrush's Opacity; dump: #FF3E65FF@0.16
    public static readonly Color Accent = Color.Parse("#FF3E65FF");              // the foregrounds and the track's colour: the accent, Cobalt
    public static Color Page(bool dark) => dark ? Colors.Black : Colors.White;   // the stage's background, ApplicationPageBackgroundThemeBrush

    // The pictures' colours, at row 86: the indicator, the track (the accent at 0.16 over the page), the page.
    public static Color Track(bool dark) => dark ? Color.FromRgb(10, 16, 41) : Color.FromRgb(224, 230, 255);

    // The dump at rest (value-40, base): the indicator's Width 164.8333, 40% of the bar's 412.0833, its bounds 165
    // wide (396 raw pixels: 46 to 441 in the picture); the track to 1034.
    public const double Value = 40;
    public const double IndicatorWidth = 164.8333;

    // The dots at rest, in the bar (every dump with no storyboard running): E5, E4, E3, E2 and E1 left to right, 12.5
    // apart, as the phone rounds them (8 + 22 raw pixels): each 3.5 square, then the 9 of TemplateSettings.EllipseOffset.
    public static readonly double[] DotX = [50, 37.5, 25, 12.5, 0];              // E1 to E5
    public const double Gap = 9;                                                 // TemplateSettings.EllipseOffset; the gaps' Width, 9

    // The high-contrast bars (value-40, base): 0.8 thick at the bottom and the right, the phone's 2 raw pixels.
    public const double AccentBarThickness = 0.8;

    // TemplateSettings, read through the API (native.json), the same at rest and in every state: (width) → the
    // container's start and end, the well, the end. EllipseDiameter 6 and EllipseOffset 9 on every one.
    public static readonly (string Device, double Width, double Start, double End, double Well, double EllipseEnd)[] Settings =
    [
        ("7, 8", 361.6667, -63, 94.4054, 120.5556, 241.1111),
        ("9", 346, -63, 87.5869, 115.3333, 230.6667),
        ("10", 362.2222, -63, 94.6472, 120.7407, 241.4815),
        ("11", 412.0833, -63, 116.3478, 137.3611, 274.7222),
        ("12", 452.7273, -63, 134.037, 150.9091, 301.8182),
        ("13", 411.6667, -63, 116.1665, 137.2222, 274.4444),
    ];

    // The Indeterminate storyboard (progressbar.json, CommonStates, Indeterminate; RepeatBehavior Forever), as the plan
    // lists it. The grid from the start to the end over 3.917 s; each B: -40 at 0, 0, 0, 80 at its three times, in
    // straight lines; each E: 0, 0 at its begin, the well a second later and two seconds later, the end three seconds
    // later, into the well and the end through the KeySpline 0.4,0,0.6,1; each E's opacity 0 before its begin, 1 for 3 s.
    public const double ContainerSeconds = 3.917;
    public static readonly double[] DotBeginSeconds = [0, 0.167, 0.333, 0.5, 0.667];
    public static readonly double[][] SlideSeconds =
    [
        [0.5, 2, 3],
        [0.667, 2.167, 3.167],
        [0.833, 2.333, 3.333],
        [1, 2.5, 3.5],
        [1.167, 2.667, 3.667],
    ];
    public static readonly double[] Spline = [0.4, 0, 0.6, 1];

    // Moments of the running storyboard the dumps caught: the grid's X, and the left edge in the bar of each dot shown,
    // E1 first (its bounds: the grid, its column, its B and its E); the others at opacity 0. The catalogue's indeterminate
    // bar at rest (base), and the forced Indeterminate state of the 40% bar. Each is one storyboard time, on the
    // emulator's 64 Hz clock: 9/64, 10/64, 30/64, 32/64, 36/64 and 38/64 s.
    public static readonly (string Capture, double Grid, double[] Shown)[] Moments =
    [
        ("1443 dark indeterminate base", -55.8458, [-26.1336]),
        ("1443 dark value-40 Indeterminate", -41.5373, [67.5005, 11.0369, -28.6014]),
        ("1443 light indeterminate base", -56.5612, [-29.4829]),
        ("1443 light value-40 Indeterminate", -40.1065, [78.5731, 20.0537, -22.8468, -47.6065]),
        ("1732 dark value-40 Indeterminate", -35.8139, [103.9281, 49.3928, -2.1032, -36.9967]),
        ("1732 light value-40 Indeterminate", -37.2448, [95.6269, 39.2991, -9.5823, -41.1127]),
    ];

    // The 1732 pictures of the forced Indeterminate state (taken a frame before their dumps): two dots each, their left
    // edges in the bar from the accent pixels' coverage, 3.333 wide (8 raw pixels). The storyboard puts them there at
    // 31/64 s (dark) and 29/64 s (light), within 0.015.
    public static readonly (string Capture, double Seconds, double[] DotLeft)[] Pictures =
    [
        ("dark", 31 / 64.0, [15.47, 73.022]),
        ("light", 29 / 64.0, [6.718, 61.979]),
    ];

    /// <summary>The grid's X at a time in the storyboard, for a bar this wide, worked out here from the readings.</summary>
    public static double GridAt(double seconds, double width) => -63 + width * 0.435222 * Math.Min(seconds, ContainerSeconds) / ContainerSeconds;

    /// <summary>A B's X at a time in the storyboard, worked out here from the keys above.</summary>
    public static double SlideAt(int dot, double seconds) =>
        Straight([(0, -40), (SlideSeconds[dot][0], 0), (SlideSeconds[dot][1], 0), (SlideSeconds[dot][2], 80)], Round(seconds));

    /// <summary>An E's X at a time in the storyboard, for a bar this wide.</summary>
    public static double MoveAt(int dot, double seconds, double width)
    {
        var begin = DotBeginSeconds[dot];
        var t = Round(seconds);
        var well = width / 3;
        if (t <= begin)
        {
            return 0;
        }

        if (t <= begin + 1)
        {
            return well * Eased(t - begin);
        }

        if (t <= begin + 2)
        {
            return well;
        }

        if (t <= begin + 3)
        {
            return well + well * Eased(t - begin - 2);
        }

        return 2 * well;
    }

    /// <summary>An E's opacity at a time in the storyboard.</summary>
    public static double OpacityAt(int dot, double seconds)
    {
        var t = Round(seconds);
        return t >= DotBeginSeconds[dot] && t < DotBeginSeconds[dot] + 3 ? 1 : 0;
    }

    /// <summary>A dot's left edge in the bar: the grid, its column, its B and its E.</summary>
    public static double DotAt(int dot, double seconds, double width) => GridAt(seconds, width) + DotX[dot] + SlideAt(dot, seconds) + MoveAt(dot, seconds, width);

    private static double Eased(double progress) => KeySplineCurveTests.Bezier(Spline[0], Spline[1], Spline[2], Spline[3], progress);

    private static double Straight((double T, double V)[] keys, double t)
    {
        for (var i = 1; i < keys.Length; i++)
        {
            if (t <= keys[i].T)
            {
                return keys[i - 1].V + (keys[i].V - keys[i - 1].V) * (t - keys[i - 1].T) / (keys[i].T - keys[i - 1].T);
            }
        }

        return keys[^1].V;
    }

    /// <summary>To the microsecond, so a time made by adding up frames meets a key time exactly.</summary>
    private static double Round(double seconds) => Math.Round(seconds * 1e6) / 1e6;
}
