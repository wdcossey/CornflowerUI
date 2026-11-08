using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 5: what unit is <c>TextBlock.LetterSpacing</c> in? WinRT's <c>CharacterSpacing</c> is in
/// thousandths of an em. Measured with Inter from <c>Avalonia.Fonts.Inter</c>, so it's the same
/// font on every OS.
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: <c>LetterSpacing</c> is in DIPs, whatever the font size, and is added
/// after every character, the last one included. So WinRT's <c>CharacterSpacing="n"</c> is
/// <c>LetterSpacing = n * FontSize / 1000</c>.
/// </remarks>
public class LetterSpacingUnitTests
{
    private static readonly FontFamily Inter = new("avares://Avalonia.Fonts.Inter/Assets#Inter");
    private const string Text = "HHHHHHHHHH";                       // ten characters

    private static double Width(double fontSize, double letterSpacing)
    {
        var block = new TextBlock { Text = Text, FontFamily = Inter, FontSize = fontSize, LetterSpacing = letterSpacing };
        block.Measure(Size.Infinity);
        Assert.Equal("Inter", block.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.GlyphTypeface.FamilyName);
        return block.DesiredSize.Width;
    }

    [AvaloniaTheory]
    [InlineData(20, 10)]
    [InlineData(40, 10)]
    [InlineData(20, 2.5)]
    public void LetterSpacing_Adds_That_Many_DIPs_After_Every_Character(double fontSize, double letterSpacing)
    {
        var added = Width(fontSize, letterSpacing) - Width(fontSize, 0);

        Assert.Equal(Text.Length * letterSpacing, added, precision: 3);
    }
}
