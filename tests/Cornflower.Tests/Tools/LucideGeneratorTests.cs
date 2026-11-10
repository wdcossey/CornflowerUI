using System.IO.Compression;
using System.Text;
using IconGenerator;

namespace Cornflower.Tests.Tools;

/// <summary>
/// The Lucide generator (<c>tools/lucide</c>) on icons of our own drawing in Lucide's convention, so
/// no network and no release is needed. The committed output is checked in <c>Controls/LucideIconTests</c>.
/// </summary>
public class LucideGeneratorTests
{
    private const string Root =
        """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">""";

    private static readonly Output Out = new("unused", "Fixture.Icons", "FixtureKind", "fixture.br");

    // Relative steps become absolute, and H and V become L.
    [Fact]
    public void Relative_Commands_Become_Absolute() =>
        Assert.Equal("M2 3 L5 7 L9 7 L9 1 L10 2", PathData.Normalize("m2 3l3 4h4v-6l1 1"));

    // Further pairs after a moveto are linetos, relative after m, and any command repeats.
    [Fact]
    public void Implicit_Commands_Are_Written_Out() =>
        Assert.Equal("M1 1 L2 2 L4 4 M10 10 L11 11 L12 12", PathData.Normalize("m1 1 1 1 2 2M10 10 11 11 12 12"));

    // A smooth curve after a command that isn't a curve starts at the current point. Avalonia reflects
    // the stale control point instead (Avonic's SvgPath.cs), so it's written as C with that point.
    [Fact]
    public void A_Smooth_Curve_After_A_Line_Starts_At_The_Current_Point() =>
        Assert.Equal("M0 0 C1 1 2 0 3 0 L5 0 C5 0 6 1 7 0", PathData.Normalize("M0 0C1 1 2 0 3 0L5 0S6 1 7 0"));

    [Fact]
    public void A_Smooth_Curve_After_A_Curve_Reflects_Its_Control_Point() =>
        Assert.Equal("M0 0 C1 1 2 1 3 0 C4 -1 5 -1 6 0", PathData.Normalize("M0 0c1 1 2 1 3 0s2-1 3 0"));

    [Fact]
    public void A_Smooth_Quadratic_Reflects_The_Last_Quadratic() =>
        Assert.Equal("M0 0 Q1 2 2 0 Q3 -2 4 0 L5 0 Q5 0 6 0", PathData.Normalize("M0 0q1 2 2 0t2 0l1 0t1 0"));

    // Lucide 1.71.0 writes arc flags without separators in 78 places: "a2 2 0 012 2".
    [Fact]
    public void Compact_Arc_Flags_Are_Spaced() =>
        Assert.Equal("M4 4 A2 2 0 0 1 6 6 A1 1 0 1 0 8 6", PathData.Normalize("M4 4a2 2 0 012 2a1 1 0 102 0"));

    [Fact]
    public void After_Closepath_The_Current_Point_Is_The_Subpath_Start() =>
        Assert.Equal("M2 2 L4 2 L4 4 Z M3 3 L4 4", PathData.Normalize("M2 2h2v2zm1 1l1 1"));

    [Fact]
    public void Signs_Exponents_And_Run_Together_Numbers_Are_Read() =>
        Assert.Equal("M1 -0.5 L0.5 0.25 L20 1", PathData.Normalize("M+1-.5L.5.25 2e1 1E0"));

    // Sums of relative steps carry floating-point noise; five decimals is the most Lucide writes.
    [Fact]
    public void Numbers_Are_Rounded_To_Five_Decimals_Without_Negative_Zero()
    {
        Assert.Equal("M0.1 0 L0.3 0 M0.1 0", PathData.Normalize("m.1 0 .2 0m-.2 0"));
        Assert.Equal("0.79796", PathData.Format(0.79796));
        Assert.Equal("0", PathData.Format(-0.000001));
    }

    [Theory]
    [InlineData("L1 1")]           // must start with a moveto
    [InlineData("M1 1Z 2 2")]      // a number after closepath: Avalonia would loop forever
    [InlineData("M1 1L2")]         // a pair cut short
    [InlineData("M1 1A1 1 0 2 1 3 3")] // a flag that isn't 0 or 1
    [InlineData("M1 1B2 2")]       // not a command
    [InlineData("")]
    public void Malformed_Data_Is_Refused(string data) =>
        Assert.Throws<FormatException>(() => PathData.Normalize(data));

    [Fact]
    public void Each_Shape_Becomes_Path_Data_As_Svg_Defines_It() =>
        Assert.Equal(
            "M2 12 A10 10 0 1 1 22 12 A10 10 0 1 1 2 12 Z"                 // circle
            + " M8 6 A4 2 0 1 1 16 6 A4 2 0 1 1 8 6 Z"                     // ellipse
            + " M3 3 L21 3 L21 21 L3 21 Z"                                 // rect
            + " M6 4 L18 4 A2 2 0 0 1 20 6 L20 18 A2 2 0 0 1 18 20 L6 20 A2 2 0 0 1 4 18 L4 6 A2 2 0 0 1 6 4 Z" // rect, rounded
            + " M1 2 L3 4"                                                 // line
            + " M1 1 L2 3 L4 1"                                            // polyline
            + " M5 5 L7 9 L9 5 Z",                                         // polygon
            SvgIcon.ToPath("shapes", Svg(
                """<circle cx="12" cy="12" r="10" />""",
                """<ellipse cx="12" cy="6" rx="4" ry="2" />""",
                """<rect x="3" y="3" width="18" height="18" />""",
                """<rect x="4" y="4" width="16" height="16" rx="2" />""",
                """<line x1="1" y1="2" x2="3" y2="4" />""",
                """<polyline points="1 1 2,3 4 1" />""",
                """<polygon points="5 5 7 9 9 5" />""")));

    // A filled circle no wider than the stroke is covered by it, so it's kept as an outline.
    [Fact]
    public void A_Fill_The_Stroke_Covers_Is_Accepted() =>
        Assert.Equal("M7 7.5 A0.5 0.5 0 1 1 8 7.5 A0.5 0.5 0 1 1 7 7.5 Z",
            SvgIcon.ToPath("dot", Svg("""<circle cx="7.5" cy="7.5" r=".5" fill="currentColor" />""")));

    [Theory]
    [InlineData("""<circle cx="12" cy="12" r="3" fill="currentColor" />""")] // a fill the stroke doesn't cover
    [InlineData("""<path d="M1 1L2 2" stroke-width="1" />""")]              // a pen of its own
    [InlineData("""<path d="M1 1L2 2" transform="scale(2)" />""")]
    [InlineData("""<g><path d="M1 1L2 2" /></g>""")]
    [InlineData("""<text>A</text>""")]
    [InlineData("""<rect x="1" y="1" width="0" height="4" />""")]
    [InlineData("")]
    public void A_Shape_Outside_The_Convention_Is_Refused(string shape) =>
        Assert.Throws<FormatException>(() => SvgIcon.ToPath("bad", Svg(shape)));

    [Theory]
    [InlineData("stroke-width=\"2\"", "stroke-width=\"1.5\"")]
    [InlineData("viewBox=\"0 0 24 24\"", "viewBox=\"0 0 32 32\"")]
    [InlineData("fill=\"none\"", "fill=\"currentColor\"")]
    [InlineData("stroke-linecap=\"round\" ", "")]
    [InlineData("<svg ", "<svg class=\"x\" ")]
    public void A_Root_Outside_The_Convention_Is_Refused(string from, string to)
    {
        var svg = Svg("""<path d="M1 1L2 2" />""").Replace(from, to, StringComparison.Ordinal);

        var error = Assert.Throws<FormatException>(() => SvgIcon.ToPath("bad", svg));
        Assert.StartsWith("bad:", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("x", "X")]
    [InlineData("arrow-down-0-1", "ArrowDown01")]
    [InlineData("a-large-small", "ALargeSmall")]
    public void Members_Are_Lucides_PascalCase_Names(string name, string member) =>
        Assert.Equal(member, Generator.Member(name));

    [Theory]
    [InlineData("Arrow")]
    [InlineData("1-arrow")]
    [InlineData("arrow_up")]
    public void A_Name_That_Makes_No_Member_Is_Refused(string name) =>
        Assert.Throws<FormatException>(() => Generator.Member(name));

    // The enum's value is the data's line: both in ordinal order of name, whatever order the zip holds.
    [Fact]
    public void The_Enum_And_The_Data_Follow_The_Names_Ordinal_Order()
    {
        var files = Generator.Generate(Release(("minus", "M5 12h14"), ("arrow-up", "M12 19V5"), ("a-b", "M1 1h1")), Out);

        var source = Encoding.UTF8.GetString(files["FixtureKind.g.cs"]);
        Assert.Contains("namespace Fixture.Icons;", source, StringComparison.Ordinal);
        Assert.Contains("public enum FixtureKind\n{\n    AB,\n    ArrowUp,\n    Minus,\n}", source, StringComparison.Ordinal);
        Assert.Contains("Lucide 9.9.9", source, StringComparison.Ordinal);
        Assert.Equal("a-b\tM1 1 L2 1\narrow-up\tM12 19 L12 5\nminus\tM5 12 L19 12\n", Decompress(files["fixture.br"]));
    }

    [Fact]
    public void The_Licence_Is_Copied_Verbatim()
    {
        var files = Generator.Generate(Release(("minus", "M5 12h14")), Out);

        Assert.Equal("ISC License\r\n\r\nCopyright (c) Fixture", Encoding.UTF8.GetString(files["LICENSE"]));
    }

    [Fact]
    public void The_Same_Release_Gives_The_Same_Bytes()
    {
        var first = Generator.Generate(Release(("minus", "M5 12h14"), ("plus", "M5 12h14M12 5v14")), Out);
        var second = Generator.Generate(Release(("plus", "M5 12h14M12 5v14"), ("minus", "M5 12h14")), Out);

        Assert.Equal(first.Keys.Order(), second.Keys.Order());
        foreach (var (name, bytes) in first)
        {
            Assert.Equal(bytes, second[name]);
        }
    }

    [Fact]
    public void Two_Names_With_One_Member_Are_Refused() =>
        Assert.Throws<FormatException>(() => Generator.Generate(Release(("arrow-1-0", "M1 1h1"), ("arrow-10", "M1 1h1")), Out));

    [Fact]
    public void Read_Takes_The_Svgs_Out_Of_A_Release_Zip()
    {
        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "icons/plus.svg", Svg("""<path d="M5 12h14" />"""));
            Add(archive, "icons/plus.json", "{}");
            Add(archive, "icons/minus.svg", Svg("""<path d="M5 12h14" />"""));
        }

        zip.Position = 0;
        var release = Generator.Read("1.0.0", zip, "licence");

        Assert.Equal(["minus", "plus"], release.Icons.Select(icon => icon.Name));
    }

    [Fact]
    public void A_Download_With_Another_Hash_Is_Refused()
    {
        var bytes = Encoding.UTF8.GetBytes("lucide");
        var pinned = new PinnedFile("https://example.invalid/icons.zip", Pin.Sha256(bytes));

        Pin.Verify(pinned, bytes);
        Assert.Throws<InvalidDataException>(() => Pin.Verify(pinned, Encoding.UTF8.GetBytes("lucidE")));
    }

    private static string Svg(params string[] shapes) => Root + "\n  " + string.Join("\n  ", shapes) + "\n</svg>\n";

    private static Release Release(params (string Name, string Data)[] icons) =>
        new("9.9.9", icons.Select(icon => (icon.Name, Svg($"""<path d="{icon.Data}" />"""))).ToList(), "ISC License\r\n\r\nCopyright (c) Fixture");

    private static string Decompress(byte[] data)
    {
        using var brotli = new BrotliStream(new MemoryStream(data), CompressionMode.Decompress);
        using var reader = new StreamReader(brotli, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void Add(ZipArchive archive, string name, string text)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
        writer.Write(text);
    }
}
