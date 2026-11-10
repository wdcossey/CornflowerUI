using Avalonia.Headless.XUnit;

namespace Cornflower.Tests.Fonts;

/// <summary>
/// Which line does Avalonia give Selawik: its <c>hhea</c> line (1200 thousandths of an em) or its
/// <c>OS/2</c> win line (1330)? And how far is that from the phone?
/// </summary>
/// <remarks>
/// <para>What Avalonia 12.1.4 does. A line is the font's <c>hhea</c> ascender − descender + line gap,
/// unless <c>OS/2</c> sets <c>USE_TYPO_METRICS</c>, which no Selawik file does. So a line of Selawik is
/// 1.2002 em, with the baseline 0.9897 em down. A <c>LineHeight</c> beyond that is split evenly above
/// and below the text.</para>
/// <para>The phone. With no <c>LineHeight</c>, a Segoe WP <c>TextBlock</c> is 1.330 em tall at every
/// size and weight, read by the reference app on the reference profile (2026-11-08, captures in
/// <c>refapp/20261108-1443-dev11</c>): 24.1267 at 18.14 in <c>dark__Button__default__base.json</c>,
/// 21.28 at 16 (ComboBox header), 33.9167 at 25.5 (ToggleSwitch), 39.7167 at 29.86 (HyperlinkButton,
/// Segoe WP SemiLight) and 75.8133 at 57 (Pivot header). In the Button and CheckBox captures at 18.14,
/// the baseline is 1.0796 em below the top of the <c>TextBlock</c>, to within one of the capture's
/// pixels (0.023 em). That is Segoe WP's ascent, 2210 units.</para>
/// <para>So Selawik's line is 9.8% short. <c>LineHeight</c> = 1.33 × <c>FontSize</c> gives the phone's
/// line, with the baseline 0.025 em higher than the phone's: 0.45 DIP at 18.14.</para>
/// </remarks>
public class LineMetricTests
{
    private const double PhoneLine = 1.330;                 // em, measured; see the remarks
    private const double PhoneBaseline = 1.0796;            // em below the line's top, measured

    [AvaloniaTheory]
    [InlineData("PhoneFontFamilyNormal", 18.14, "Selawik/selawk.ttf")]           // TextStyleLargeFontSize
    [InlineData("PhoneFontFamilyNormal", 12, "Selawik/selawk.ttf")]
    [InlineData("PhoneFontFamilySemiLight", 57, "SelawikSemilight/selawksl.ttf")] // PivotHeaderItemFontSize
    [InlineData("PhoneFontFamilyLight", 25.5, "Selawik/selawkl.ttf")]
    public void A_Line_Of_Selawik_Is_Its_Hhea_Line_Not_Its_Win_Line(string key, double fontSize, string path)
    {
        var file = OpenTypeFile.Bundled(path);
        using var text = new ThemeText(key, "Agjy 0123", fontSize);

        Assert.Equal(0, file.FsSelection & 0x80);                                  // no USE_TYPO_METRICS
        Assert.Equal(file.HheaLine * fontSize, text.Line.Height, precision: 6);
        Assert.Equal(file.HheaAscender / (double)file.UnitsPerEm * fontSize, text.Line.Baseline, precision: 6);
        Assert.NotEqual(file.WinLine * fontSize, text.Line.Height, precision: 1);
    }

    [AvaloniaFact]
    public void Selawiks_Line_Is_A_Tenth_Short_Of_The_Phones()
    {
        using var text = new ThemeText("PhoneFontFamilyNormal", "button", 18.14);

        Assert.Equal(0.902, text.Line.Height / (PhoneLine * 18.14), precision: 3);
    }

    [AvaloniaTheory]
    [InlineData(18.14)]
    [InlineData(57)]
    public void LineHeight_Of_1_33_Em_Gives_The_Phones_Line_And_Splits_The_Extra_Evenly(double fontSize)
    {
        var file = OpenTypeFile.Bundled("Selawik/selawk.ttf");
        var natural = file.HheaLine * fontSize;
        var ascent = file.HheaAscender / (double)file.UnitsPerEm * fontSize;

        using var text = new ThemeText("PhoneFontFamilyNormal", "button", fontSize, lineHeight: PhoneLine * fontSize);

        Assert.Equal(PhoneLine * fontSize, text.Line.Height, precision: 6);
        Assert.Equal(ascent + (PhoneLine * fontSize - natural) / 2, text.Line.Baseline, precision: 6);
        Assert.Equal(0.025, PhoneBaseline - text.Line.Baseline / fontSize, precision: 3);
    }

    // The text styles' LineHeights are often shorter than Selawik's line (21.5 at 18.14, 57.5 at 57.5), and
    // PhoneTextLine relies on what Avalonia does then: the baseline stays at the font's ascent, so the line is
    // cut from below, and the descenders draw past it.
    [AvaloniaTheory]
    [InlineData(18.14, 21.5)]
    [InlineData(57.5, 57.5)]
    public void A_Shorter_LineHeight_Keeps_The_Baseline_At_The_Ascent(double fontSize, double lineHeight)
    {
        var file = OpenTypeFile.Bundled("Selawik/selawk.ttf");
        var ascent = file.HheaAscender / (double)file.UnitsPerEm * fontSize;
        Assert.True(lineHeight < file.HheaLine * fontSize);

        using var text = new ThemeText("PhoneFontFamilyNormal", "button", fontSize, lineHeight: lineHeight);

        Assert.Equal(lineHeight, text.Line.Height, precision: 6);
        Assert.Equal(ascent, text.Line.Baseline, precision: 6);
    }
}
