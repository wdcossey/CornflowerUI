using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Cornflower.Controls;
using IconGenerator;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The Lucide icon control, its generated set, and its Wp81 theme.
/// </summary>
/// <remarks>
/// The reference is the 8.1 <c>SymbolIcon</c> in the AppBarButton capture of run 20261108-1732-dev11
/// (<c>dark__AppBarButton__default__base</c>): the Add glyph's ink is 44.25 px wide at 2.4, the
/// SymbolIcon's text follows its presenter's foreground (<c>#FFFFFFFF</c>), and its root is a Grid
/// with a transparent background.
/// </remarks>
public class LucideIconTests
{
    private const string SymbolTable = "Cornflower.Tests.Tools.symbols.tsv";

    // The view is 450 wide at 2.4, so a box centred in it starts at (450 - side) / 2 view pixels.
    private const double Scaling = Scene.ReferenceScaling;
    private const int CentreRow = 960;

    [AvaloniaFact]
    public void Every_Icon_Parses_Inside_Lucides_Box()
    {
        var problems = new List<string>();
        foreach (var kind in Enum.GetValues<LucideIconKind>())
        {
            var bounds = LucideIcons.Get(kind).Bounds;
            if (bounds.Width <= 0 && bounds.Height <= 0 || bounds.Left < -0.001 || bounds.Top < -0.001 || bounds.Right > 24.001 || bounds.Bottom > 24.001)
            {
                problems.Add($"{kind}: {bounds}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // Each data line carries its member's value, so the two must be generated together.
    [Fact]
    public void The_Data_Has_One_Line_Per_Member_Under_Its_Value()
    {
        var kinds = Enum.GetValues<LucideIconKind>();

        Assert.Equal(kinds.Length, LucideIcons.Count);
        Assert.Equal(2046, kinds.Length); // Lucide 1.71.0
        Assert.All(kinds, kind => Assert.Equal(kind.ToString(), Generator.Member(LucideIcons.NameOf(kind))));
    }

    // The values are explicit, and the generator keeps them across bumps: these two are the first
    // pin's, and a bump must not move them.
    [Fact]
    public void Values_Are_The_First_Pins()
    {
        Assert.Equal(0, (int)LucideIconKind.AArrowDown);
        Assert.Equal(2045, (int)LucideIconKind.ZoomOut);
    }

    [Fact]
    public void A_Kind_That_Is_No_Icon_Has_No_Geometry() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LucideIcons.NameOf((LucideIconKind)5000));

    [Fact]
    public void A_Known_Icon_Has_Lucides_Path()
    {
        Assert.Equal("settings", LucideIcons.NameOf(LucideIconKind.Settings));
        Assert.Equal("M5 12 L19 12 M12 5 L12 19", LucideIcons.PathOf(LucideIconKind.Plus));
        Assert.Equal("M2 12 A10 10 0 1 1 22 12 A10 10 0 1 1 2 12 Z", LucideIcons.PathOf(LucideIconKind.Circle));
    }

    // The table in tools/lucide/symbols.tsv: every 8.1 Symbol, and only icons this set has.
    [Fact]
    public void The_Symbol_Table_Lists_Every_Symbol_And_Names_Only_Icons_There_Are()
    {
        var names = Enum.GetValues<LucideIconKind>().Select(LucideIcons.NameOf).ToHashSet(StringComparer.Ordinal);
        var rows = SymbolRows();

        Assert.Equal(193, rows.Count); // the Symbol enum in the 8.1 Windows.winmd
        Assert.Equal(rows.Count, rows.Select(row => row.Symbol).Distinct().Count());
        Assert.All(rows, row => Assert.True(row.Lucide == "none" || names.Contains(row.Lucide), $"{row.Symbol}: {row.Lucide} isn't a Lucide icon."));
    }

    [AvaloniaFact]
    public void Kind_Gives_The_Icons_Geometry_And_Null_Gives_None()
    {
        var icon = new LucideIcon();
        Assert.Null(icon.Data);

        icon.Kind = LucideIconKind.Plus;
        Assert.Same(LucideIcons.Get(LucideIconKind.Plus), icon.Data);

        icon.Kind = LucideIconKind.Minus;
        Assert.Same(LucideIcons.Get(LucideIconKind.Minus), icon.Data);

        icon.Kind = null;
        Assert.Null(icon.Data);
    }

    // A value a later Lucide dropped, compiled into a library or stored, draws nothing rather than throwing.
    [AvaloniaTheory]
    [InlineData(-1)]
    [InlineData(5000)]
    public void A_Kind_That_Is_No_Icon_Draws_Nothing(int value)
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Plus };
        icon.Kind = (LucideIconKind)value;

        Assert.Null(icon.Data);
        Assert.False(LucideIcons.Has((LucideIconKind)value));
    }


    // 20 view px, so an icon reaching its box's edges inks 22 of 24 units: 18.33 view px, 44 px at 2.4,
    // where the 8.1 Add glyph inks 44.25 px.
    [AvaloniaFact]
    public void The_Theme_Draws_An_Icon_At_The_Phones_Glyph_Size()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Circle };
        using var scene = new Scene(Stage(icon));
        using var frame = scene.Capture();

        Assert.Equal(new Size(20, 20), icon.Bounds.Size);
        var (left, right) = Ink(frame, CentreRow);
        Assert.Equal(44, right - left + 1);
        Assert.Equal(Px(215 + 1 * 20 / 24.0), left); // Lucide's circle: r 10 at 12, so its ink starts at unit 1
    }

    [AvaloniaFact]
    public void The_Stroke_Follows_The_Foreground_It_Inherits()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Circle };
        var stage = Stage(icon);
        using var scene = new Scene(stage);

        // The circle's stroke runs from unit 1 to 3 on the centre row: unit 2 is inside it.
        var stroke = Px(215 + 2 * 20 / 24.0);
        using (var frame = scene.Capture())
        {
            Assert.Equal(Colors.White, Frames.PixelAt(frame, stroke, CentreRow));
        }

        stage.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x1B, 0xA1, 0xE2)));
        using (var frame = scene.Capture())
        {
            Assert.Equal(Color.FromRgb(0x1B, 0xA1, 0xE2), Frames.PixelAt(frame, stroke, CentreRow));
        }
    }

    // The pen is three resources. At 240 view px a unit is 10 view px, 24 px.
    [AvaloniaFact]
    public void The_Stroke_Width_Is_A_Resource_Of_Two_Units()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Circle, Width = 240, Height = 240 };
        var stage = Stage(icon);
        using var scene = new Scene(stage);

        using (var frame = scene.Capture())
        {
            Assert.Equal((Unit(1), Unit(23) - 1), Ink(frame, CentreRow));
        }

        stage.Resources["LucideIconStrokeThickness"] = 4.0;
        using (var frame = scene.Capture())
        {
            Assert.Equal((Unit(0), Unit(24) - 1), Ink(frame, CentreRow));
        }
    }

    // Plus's arm ends at unit 5; a round cap reaches to 4, a flat one stops at 5.
    [AvaloniaFact]
    public void Caps_Are_Round_From_A_Resource()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Plus, Width = 240, Height = 240 };
        var stage = Stage(icon);
        using var scene = new Scene(stage);

        using (var frame = scene.Capture())
        {
            Assert.Equal((Unit(4), Unit(20) - 1), Ink(frame, CentreRow));
        }

        stage.Resources["LucideIconStrokeLineCap"] = PenLineCap.Flat;
        using (var frame = scene.Capture())
        {
            Assert.Equal((Unit(5), Unit(19) - 1), Ink(frame, CentreRow));
        }
    }

    // Chevron-right's tip is a right angle at (15, 12): a round join ends at 16, a miter at 16.41.
    [AvaloniaFact]
    public void Joins_Are_Round_From_A_Resource()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.ChevronRight, Width = 240, Height = 240 };
        var stage = Stage(icon);
        using var scene = new Scene(stage);

        using (var frame = scene.Capture())
        {
            Assert.Equal(Unit(16) - 1, Ink(frame, CentreRow).Right);
        }

        stage.Resources["LucideIconStrokeJoin"] = PenLineJoin.Miter;
        using (var frame = scene.Capture())
        {
            Assert.InRange(Ink(frame, CentreRow).Right, Unit(16.41) - 2, Unit(16.41) + 1);
        }
    }

    [AvaloniaFact]
    public void The_Whole_Box_Takes_Input()
    {
        var icon = new LucideIcon { Kind = LucideIconKind.Plus };
        using var scene = new Scene(Stage(icon));
        using var frame = scene.Capture();

        // A corner of the box, where Plus draws nothing.
        var hit = scene.Window.InputHitTest(new Point(216, 391)) as Visual;

        Assert.Same(icon, hit?.FindAncestorOfType<LucideIcon>(includeSelf: true));
    }

    [AvaloniaFact]
    public void No_Kind_Draws_Nothing()
    {
        using var scene = new Scene(Stage(new LucideIcon()));
        using var frame = scene.Capture();

        Assert.Equal((-1, -1), Ink(frame, CentreRow));
    }

    /// <summary>White on black, so ink is anything bright.</summary>
    private static Panel Stage(LucideIcon icon)
    {
        var stage = new Panel { Background = Brushes.Black, Children = { icon } };
        stage.SetValue(TextElement.ForegroundProperty, Brushes.White);
        return stage;
    }

    /// <summary>The first and last pixels in the row more than half covered, or (-1, -1).</summary>
    private static (int Left, int Right) Ink(WriteableBitmap frame, int row)
    {
        int left = -1, right = -1;
        for (var x = 0; x < frame.PixelSize.Width; x++)
        {
            var pixel = Frames.PixelAt(frame, x, row);
            if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > 127)
            {
                left = left < 0 ? x : left;
                right = x;
            }
        }

        return (left, right);
    }

    private static int Px(double view) => (int)Math.Round(view * Scaling);

    /// <summary>Unit <paramref name="unit"/> of a 240-wide icon centred in the view, in pixels.</summary>
    private static int Unit(double unit) => (int)Math.Round((105 + unit * 10) * Scaling);

    private static List<(string Symbol, string Lucide)> SymbolRows()
    {
        using var stream = typeof(LucideIconTests).Assembly.GetManifestResourceStream(SymbolTable)
            ?? throw new InvalidOperationException($"{SymbolTable} isn't embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('\t') is [var symbol, var lucide] ? (symbol, lucide) : throw new FormatException($"Not a row: {line}"))
            .ToList();
    }
}
