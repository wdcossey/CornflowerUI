using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace Cornflower.Tests.Glyphs;

/// <summary>
/// The glyphs in <c>src/Cornflower.Themes.Wp81/Tokens/Glyphs.axaml</c>, each found through the theme by its
/// key, drawn where the phone drew it in the capture it was measured from, at the reference profile, and read
/// back. At 2.4 a frame's pixels are the capture's, so each point is a raw pixel of that capture with the luma
/// the phone drew there: inside the shape, just outside it, and on its edge at its extent. The captures are
/// the refapp's, in <c>R:\cornflower-reference\refapp\</c>.
/// </summary>
public class GlyphTests
{
    /// <summary>
    /// Skia's anti-aliasing isn't the phone's: on these glyphs an edge pixel differs from the capture's by up
    /// to 28 luma levels. Half a raw pixel's error in an edge moves it by 53 levels or more, even on the
    /// accent's smaller range (0x00 to 0x6A).
    /// </summary>
    private const int EdgeTolerance = 32;

    /// <summary>A pixel wholly inside or outside the shape is the fill or the background.</summary>
    private const int SolidTolerance = 4;

    private static readonly Color Accent = Color.FromRgb(0x3E, 0x65, 0xFF); // luma 0x6A
    private static readonly Color AppBar = Color.FromRgb(0x1F, 0x1F, 0x1F);

    /// <summary>A raw pixel of the capture and the luma the phone drew there.</summary>
    private readonly record struct Point(int X, int Y, int Luma, bool Edge = false);

    // refapp 20261108-1443-dev11, dark__CheckBox__checked__CheckStates.Checked.png: the CheckBox's CheckGlyph,
    // white on black, its box at raw (55, 77), 44 × 41. At its own size, unstretched.
    [AvaloniaFact]
    public void The_CheckMark_Matches_The_Checked_CheckBox()
    {
        Point[] points =
        [
            new(84, 95, 0xFF),              // inside the long arm
            new(88, 90, 0xFF),              // inside the long arm, higher
            new(62, 97, 0xFF),              // inside the short arm
            new(73, 108, 0xFF),             // where the arms meet
            new(73, 100, 0x00),             // the notch between the arms
            new(70, 95, 0x00),              // between the short arm's end and the long arm
            new(82, 90, 0x00),              // left of the long arm
            new(83, 90, 0xB7, Edge: true),  // the long arm's left edge
            new(93, 90, 0x57, Edge: true),  // its right edge: the long arm is 10.1 raw across a row
            new(94, 90, 0x00),              // right of it
            new(93, 76, 0x00),              // above the top corner
            new(93, 77, 0xA7, Edge: true),  // the top corner
            new(54, 96, 0x00),              // left of the left corner
            new(55, 96, 0xCB, Edge: true),  // the left corner
            new(99, 82, 0x00),              // right of the right corner
            new(98, 82, 0xA3, Edge: true),  // the right corner
            new(73, 118, 0x00),             // below the bottom corner
            new(73, 117, 0xAF, Edge: true), // the bottom corner
        ];

        AssertGlyph("Wp81.Glyph.CheckMark", new Rect(55, 77, 44, 41), Stretch.None, Colors.White, Colors.Black, points);
    }

    // refapp 20261108-1443-dev11, dark__ListViewItem__selected__MultiSelectStates.GridMultiSelect.png: the
    // item's SelectedGlyph, the same tick stretched to the template's 17 × 14.5 (arranged 17.08 × 14.58),
    // white on the accent, its box at raw (986, 37), 41 × 35.
    [AvaloniaFact]
    public void The_CheckMark_Stretched_Matches_The_Selected_Grid_Item()
    {
        Point[] points =
        [
            new(1015, 50, 0xFF),              // inside the long arm
            new(992, 54, 0xFF),               // inside the short arm
            new(1003, 64, 0xFF),              // where the arms meet
            new(1003, 55, 0x6A),              // the notch between the arms
            new(1021, 36, 0x6A),              // above the top corner
            new(1021, 37, 0xD5, Edge: true),  // the top corner
            new(985, 53, 0x6A),               // left of the left corner
            new(986, 53, 0xDC, Edge: true),   // the left corner
            new(1027, 41, 0x6A),              // right of the right corner
            new(1026, 41, 0xBD, Edge: true),  // the right corner
            new(1003, 72, 0x6A),              // below the bottom corner
            new(1003, 71, 0xC0, Edge: true),  // the bottom corner
        ];

        AssertGlyph("Wp81.Glyph.CheckMark", new Rect(986, 37, 41, 35), Stretch.Fill, Colors.White, Accent, points);
    }

    // refapp 20261108-1443-dev11, dark__ListViewItem__selected__MultiSelectStates.GridMultiSelect.png: the
    // item's SelectedEarmark, the accent on black, raw (947, 35), 82 × 82. The capture's border runs round
    // its top and right, and its tick sits on it, so the points keep clear of both.
    [AvaloniaFact]
    public void The_SelectedEarmark_Matches_The_Selected_Grid_Item()
    {
        Point[] points =
        [
            new(1020, 35, 0x6A),              // the top row, inside
            new(1028, 80, 0x6A),              // the right column, inside
            new(990, 90, 0x00),               // below the cut
            new(1009, 98, 0x00),              // just below the cut
            new(1010, 98, 0x35, Edge: true),  // on the cut: half covered
            new(1011, 98, 0x6A),              // just above it
            new(946, 35, 0x00),               // left of the top left corner
            new(947, 35, 0x35, Edge: true),   // the top left corner, on the cut
            new(1028, 115, 0x6A),             // above the bottom right corner
            new(1028, 116, 0x35, Edge: true), // the bottom right corner, on the cut
            new(1028, 117, 0x00),             // below it
        ];

        AssertGlyph("Wp81.Glyph.SelectedEarmark", new Rect(947, 35, 82, 82), Stretch.None, Accent, Colors.Black, points);
    }

    // refapp 20261109-2130-dev11, dark__BottomAppBar__minimal__popup__screen.png: the app bar's ellipsis, white
    // on the bar's #FF1F1F1F, its first dot's left and top at raw (976.56, 1882.18): off the pixel grid.
    [AvaloniaFact]
    public void The_AppBarEllipsis_Matches_The_Minimal_App_Bar()
    {
        Point[] points =
        [
            new(982, 1887, 0xFF),              // the first dot's centre
            new(1003, 1887, 0xFF),             // the second's
            new(1025, 1887, 0xFF),             // the third's
            new(992, 1887, 0x1F),              // between the first and second
            new(1014, 1887, 0x1F),             // between the second and third
            new(975, 1887, 0x1F),              // left of the first
            new(976, 1887, 0x7E, Edge: true),  // its left edge
            new(987, 1887, 0xB7, Edge: true),  // its right edge
            new(988, 1887, 0x1F),              // right of it
            new(982, 1881, 0x1F),              // above it
            new(982, 1882, 0xD0, Edge: true),  // its top edge
            new(982, 1893, 0x65, Edge: true),  // its bottom edge
            new(982, 1894, 0x1F),              // below it
            new(1009, 1887, 0x3B, Edge: true), // the second's right edge
            new(1030, 1887, 0x9B, Edge: true), // the third's right edge
            new(1031, 1887, 0x1F),             // right of it
        ];

        AssertGlyph("Wp81.Glyph.AppBarEllipsis", new Rect(976.56, 1882.18, 54, 11.14), Stretch.None, Colors.White, AppBar, points);
    }

    /// <summary>
    /// Draws the glyph under <paramref name="key"/>, found through the theme as a template would find it, in a
    /// box at <paramref name="box"/> (raw pixels, the capture's), and checks each point's luma.
    /// </summary>
    private static void AssertGlyph(string key, Rect box, Stretch stretch, Color fill, Color background, Point[] points)
    {
        const double scale = Scene.ReferenceScaling;
        // Layout rounding would move a box that isn't on the pixel grid, as the ellipsis's isn't.
        var path = new Path
        {
            Fill = new SolidColorBrush(fill),
            Stretch = stretch,
            Width = box.Width / scale,
            Height = box.Height / scale,
            UseLayoutRounding = false,
        };
        path.Bind(Path.DataProperty, path.GetResourceObservable(key));
        Canvas.SetLeft(path, box.X / scale);
        Canvas.SetTop(path, box.Y / scale);
        var canvas = new Canvas { Background = new SolidColorBrush(background), Children = { path } };

        using var scene = new Scene(canvas);
        Assert.IsAssignableFrom<Geometry>(path.Data);
        using var frame = scene.Capture();

        var wrong = new List<string>();
        foreach (var p in points)
        {
            var luma = Luma(Frames.PixelAt(frame, p.X, p.Y));
            if (Math.Abs(luma - p.Luma) > (p.Edge ? EdgeTolerance : SolidTolerance))
                wrong.Add($"({p.X}, {p.Y}): luma 0x{luma:X2}, the capture's 0x{p.Luma:X2}");
        }

        Assert.True(wrong.Count == 0, $"{key}: " + string.Join("; ", wrong));
    }

    private static int Luma(Color c) => (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
}
