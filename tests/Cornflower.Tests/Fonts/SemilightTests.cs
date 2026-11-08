using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Cornflower.Tests.Fonts;

/// <summary>
/// Does each of the theme's font family keys draw the Selawik file it names, and above all does
/// <c>PhoneFontFamilySemiLight</c> draw Selawik Semilight? Its file says <c>usWeightClass</c> 300, the
/// same as Light (ADR 0002). The proof is what is drawn, not the name Avalonia reports: the width of
/// ten zeros is ten advances of the face that was shaped, and each of the five files has its own
/// advance for "0", read here from the file. The pixel test checks the frame as well.
/// </summary>
/// <remarks>
/// <para>What Avalonia 12.1.4 does. <c>Typeface.Normalize</c> takes any word in a family name that
/// parses as a weight, style or stretch and makes it the request: "Selawik Semilight" becomes family
/// "Selawik" at weight 350, and "Selawik Light" asked for in Bold is still Light. A font collection
/// (one per <c>avares://</c> folder) registers each face under its typographic family "Selawik" by
/// weight, and the first face loaded at a weight keeps it. With Light and Semilight in one folder,
/// "Selawik Semilight" falls back from 350 to the 300 slot, which Light holds, and draws Light.</para>
/// <para>So the theme bundles Semilight in a folder of its own, where it holds the 300 slot alone. The
/// font isn't patched. The cost is that <c>PhoneFontFamilyNormal</c> with <c>FontWeight.SemiLight</c>
/// draws Light, where the phone would draw Segoe WP Semilight. No 8.1 template asks for that: the
/// templates set <c>SemiLight</c> only with <c>PhoneFontFamilySemiLight</c>.</para>
/// </remarks>
public class SemilightTests
{
    private const string Zeros = "0000000000";
    private const double FontSize = 20.48;                  // 2048 units per em, so a unit is 0.01 DIP

    [AvaloniaTheory]
    [InlineData("Selawik/selawk.ttf", "Selawik", 400)]
    [InlineData("Selawik/selawkb.ttf", "Selawik", 700)]
    [InlineData("Selawik/selawkl.ttf", "Selawik Light", 300)]
    [InlineData("Selawik/selawksb.ttf", "Selawik Semibold", 600)]
    [InlineData("SelawikSemilight/selawksl.ttf", "Selawik Semilight", 300)]
    public void The_Bundled_Files_Are_Selawik_1_01(string path, string family, int weight)
    {
        var file = OpenTypeFile.Bundled(path);

        Assert.Equal(family, file.FamilyName);
        Assert.Equal(weight, file.WeightClass);
        Assert.Equal("Version 1.01", file.Version);
        Assert.Equal(2048, file.UnitsPerEm);
    }

    [AvaloniaFact]
    public void Each_File_Has_Its_Own_Advance_For_Zero()
    {
        string[] files = ["Selawik/selawkl.ttf", "SelawikSemilight/selawksl.ttf", "Selawik/selawk.ttf", "Selawik/selawksb.ttf", "Selawik/selawkb.ttf"];

        var advances = files.Select(f => OpenTypeFile.Bundled(f).Advance('0')).ToArray();

        Assert.Equal([1055, 1080, 1104, 1153, 1178], advances);
    }

    // The weight each row sets is one an 8.1 template sets with that key, or none (Normal).
    [AvaloniaTheory]
    [InlineData("PhoneFontFamilyNormal", "Normal", "Selawik/selawk.ttf")]
    [InlineData("PhoneFontFamilyNormal", "Light", "Selawik/selawkl.ttf")]              // the Hub header
    [InlineData("PhoneFontFamilyNormal", "SemiBold", "Selawik/selawksb.ttf")]          // PhoneButtonFontWeight
    [InlineData("PhoneFontFamilyNormal", "Bold", "Selawik/selawkb.ttf")]
    [InlineData("PhoneFontFamilyLight", "Normal", "Selawik/selawkl.ttf")]
    [InlineData("PhoneFontFamilyLight", "Bold", "Selawik/selawkl.ttf")]                // the name's weight wins
    [InlineData("PhoneFontFamilySemiLight", "SemiLight", "SelawikSemilight/selawksl.ttf")]  // HyperlinkButton, HeaderTextBlockStyle
    [InlineData("PhoneFontFamilySemiLight", "Normal", "SelawikSemilight/selawksl.ttf")]     // the Pivot header
    [InlineData("PhoneFontFamilySemiBold", "Normal", "Selawik/selawksb.ttf")]
    [InlineData("ContentControlThemeFontFamily", "Normal", "Selawik/selawk.ttf")]
    [InlineData("PivotHeaderItemFontFamily", "Normal", "SelawikSemilight/selawksl.ttf")]
    public void Each_Key_Draws_Its_File(string key, string weight, string expected)
    {
        using var text = new ThemeText(key, Zeros, FontSize, Enum.Parse<FontWeight>(weight));
        var file = OpenTypeFile.Bundled(expected);

        Assert.Equal(Zeros.Length * file.Advance('0') * FontSize / file.UnitsPerEm, text.Line.WidthIncludingTrailingWhitespace, precision: 6);
        Assert.Equal(file.FamilyName, text.Face.FamilyName);
    }

    [AvaloniaFact]
    public void SemiLight_Draws_Other_Pixels_Than_Light_With_More_Ink_And_Less_Than_Regular()
    {
        const string sample = "settings email+accounts 12:45";

        byte[] Frame(string key)
        {
            using var text = new ThemeText(key, sample, 40);
            return text.Pixels();
        }

        var light = Frame("PhoneFontFamilyLight");
        var semilight = Frame("PhoneFontFamilySemiLight");
        var regular = Frame("PhoneFontFamilyNormal");

        Assert.False(light.AsSpan().SequenceEqual(semilight));
        Assert.InRange(Ink(semilight), Ink(light) + 1, Ink(regular) - 1);
    }

    [AvaloniaFact]
    public void Normal_At_SemiLight_Weight_Draws_Light_A_Recorded_Gap()
    {
        using var text = new ThemeText("PhoneFontFamilyNormal", Zeros, FontSize, FontWeight.SemiLight);

        Assert.Equal(Zeros.Length * OpenTypeFile.Bundled("Selawik/selawkl.ttf").Advance('0') / 100.0, text.Line.WidthIncludingTrailingWhitespace, precision: 6);
    }

    // Why Semilight has a folder of its own: in one collection with Light, both names draw one file.
    [AvaloniaFact]
    public void In_One_Folder_With_Light_Selawik_Semilight_Draws_The_Same_File_As_Light()
    {
        double Width(string family)
        {
            // The parent folder: Avalonia reads font folders recursively, so this collection has all five.
            var block = new TextBlock { Text = Zeros, FontSize = FontSize, FontFamily = new FontFamily("avares://Cornflower.Themes.Wp81/Fonts#" + family) };
            block.Measure(Size.Infinity);
            return block.TextLayout.TextLines[0].WidthIncludingTrailingWhitespace;
        }

        Assert.Equal(Width("Selawik Light"), Width("Selawik Semilight"));
        Assert.Equal(Zeros.Length * 1104 / 100.0, Width("Selawik"), precision: 6);    // the collection itself works
    }

    private static long Ink(byte[] pixels)
    {
        long sum = 0;
        foreach (var b in pixels) sum += b;
        return sum;
    }
}
