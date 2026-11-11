using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// One 8.1 text style as the phone has it: what its <c>Style</c> sets (generic.xaml), and what the reference
/// app read and drew on the reference profile. Read from refapp run <c>20261108-1443-dev11</c> (device 11,
/// 450 × 800 at 2.4), from <c>dark__TextBlock__&lt;Key&gt;__base.json</c> and its PNG, and the light ones for the
/// colours; the text is "Agjy 0123 cornflower", 19 DIP from the stage's left and 12 from its top. The two
/// MessageDialog styles weren't captured: their values are generic.xaml's, and their heights the phone's
/// line worked out from it (<see cref="PhoneLine"/>).
/// </summary>
/// <param name="Key">The style's key, the same on both sides.</param>
/// <param name="FontSize">The dump's <c>fontSize</c>.</param>
/// <param name="FamilyKey">The font family key the style sets: the dump's <c>fontFamily</c> is "Segoe WP" for
/// <c>PhoneFontFamilyNormal</c> and "Segoe WP SemiLight" for <c>PhoneFontFamilySemiLight</c>.</param>
/// <param name="Weight">The dump's <c>fontWeight</c>.</param>
/// <param name="LineHeight">The dump's <c>lineHeight</c>, WinRT's.</param>
/// <param name="BaselineToBaseline">The dump's <c>lineStackingStrategy</c>; MaxHeight otherwise.</param>
/// <param name="Wrapping">The dump's <c>textWrapping</c>, mapped: WrapWholeWords is WrapWithOverflow.</param>
/// <param name="CharacterSpacing">The dump's <c>characterSpacing</c>, in thousandths of an em.</param>
/// <param name="Height">The dump's height of the <c>TextBlock</c>, in DIPs.</param>
/// <param name="ABottom">The bottom of the "A"'s ink below the <c>TextBlock</c>'s top, in DIPs, read from the
/// PNG to the pixel (0.417 DIP): its baseline. NaN where not captured.</param>
/// <param name="DarkInk">The ink pixel farthest from the black page in the dark capture: the text's colour,
/// where a stem covers a whole pixel. The dump's <c>foreground</c> is #FFFFFFFF where this is 255, and #99FFFFFF
/// (PhoneMidBrush) where it is 153; ComboBoxPlaceholder's is the page's, at its <c>Opacity</c>.</param>
/// <param name="LightInk">The same on the white page in the light capture: #FF000000 where 0, #73000000 where 140.</param>
/// <param name="Opacity">The style's <c>Opacity</c> in dark and light, where it sets one.</param>
internal sealed record TextStyleReading(
    string Key,
    double FontSize,
    string FamilyKey,
    FontWeight Weight,
    double LineHeight,
    bool BaselineToBaseline,
    TextWrapping Wrapping,
    int CharacterSpacing,
    double Height,
    double ABottom,
    byte DarkInk,
    byte LightInk,
    (double Dark, double Light)? Opacity = null)
{
    public bool Captured => !double.IsNaN(ABottom);

    /// <summary>The dump's <c>foreground</c>, in dark and light.</summary>
    public (uint Dark, uint Light) Foreground => Opacity is not null || DarkInk == 255 ? (0xFFFFFFFF, 0xFF000000) : (0x99FFFFFF, 0x73000000);

    public override string ToString() => Key;
}

/// <summary>The phone's line, read from the captures: see <c>PhoneTextLine</c> in the theme.</summary>
/// <remarks>
/// These repeat <c>PhoneTextLine</c>'s Segoe WP numbers (2210, 514, 2048) on purpose, written out here rather than
/// read from it: they are the tests' own oracle, so a wrong number in the theme fails against them rather than
/// passing by agreeing with itself.
/// </remarks>
internal static class PhoneLine
{
    /// <summary>The first baseline below the top, in ems: Segoe WP's ascent, 2210 of 2048 units.</summary>
    public const double FirstBaseline = 2210.0 / 2048;

    /// <summary>The share of <c>LineHeight</c> below the last baseline: Segoe WP's descent, 514 of 2724.</summary>
    public const double DescentShare = 514.0 / 2724;

    /// <summary>The phone's own line, 1.33 em (ADR 0003).</summary>
    public const double FontLine = 2724.0 / 2048;

    /// <summary>The height of <paramref name="lines"/> lines of BaselineToBaseline text.</summary>
    public static double Height(double fontSize, double lineHeight, int lines = 1) =>
        FirstBaseline * fontSize + (lines - 1) * lineHeight + DescentShare * lineHeight;
}

internal static class TextStyleReadings
{
    private const TextWrapping Words = TextWrapping.WrapWithOverflow;
    private const string Normal = "PhoneFontFamilyNormal";
    private const string SemiLight = "PhoneFontFamilySemiLight";

    public static readonly TextStyleReading[] All =
    [
        new("BaseTextBlockStyle", 18.14, Normal, FontWeight.Normal, 21.5, true, Words, 0, 23.631, 19.583, 255, 0),
        new("BodyTextBlockStyle", 18.14, Normal, FontWeight.Normal, 24, true, Words, 0, 24.103, 19.583, 255, 0),
        new("TitleTextBlockStyle", 18.14, Normal, FontWeight.SemiBold, 21.5, true, TextWrapping.NoWrap, 0, 23.631, 19.583, 255, 0),
        new("FlyoutPickerTitleTextBlockStyle", 18.14, Normal, FontWeight.SemiBold, 21.5, true, TextWrapping.NoWrap, 15, 23.631, 19.583, 255, 0),
        new("HeaderTextBlockStyle", 57.5, SemiLight, FontWeight.SemiLight, 57.5, true, TextWrapping.NoWrap, 0, 72.8986, 62.083, 255, 0),
        new("SubheaderTextBlockStyle", 30, SemiLight, FontWeight.SemiLight, 33, true, Words, 0, 38.601, 32.5, 255, 0),
        new("GroupHeaderTextBlockStyle", 23.73, SemiLight, FontWeight.Normal, 23, true, Words, -3, 29.948, 25.833, 153, 140),
        new("ControlHeaderTextBlockStyle", 16, Normal, FontWeight.Normal, 17.5, true, Words, 0, 20.567, 17.5, 153, 140),
        new("ControlContextualInfoTextBlockStyle", 16, Normal, FontWeight.Normal, 17.5, true, Words, 0, 20.567, 17.5, 153, 140),
        new("ListViewItemTextBlockStyle", 29.86, SemiLight, FontWeight.Normal, 33.5, true, TextWrapping.NoWrap, 0, 38.544, 32.5, 255, 0),
        new("ListViewEmptyStaticTextBlockStyle", 29.86, SemiLight, FontWeight.Normal, 33.5, true, Words, 0, 38.544, 32.5, 153, 140),
        new("ListViewItemSubheaderTextBlockStyle", 16, SemiLight, FontWeight.Normal, 20, false, TextWrapping.NoWrap, 0, 21.28, 17.083, 153, 140),
        new("ListViewItemContentTextBlockStyle", 16, Normal, FontWeight.Normal, 20, false, TextWrapping.NoWrap, 0, 21.28, 17.083, 255, 0),
        // The page's foreground at 0.6 and 0.45: the style sets no Foreground, and an Opacity per variant.
        new("ComboBoxPlaceholderTextBlockStyle", 20.26, Normal, FontWeight.Normal, 30, true, TextWrapping.NoWrap, 0, 27.523, 21.667, 153, 140, (0.6, 0.45)),
        // Not captured: generic.xaml's values, and the phone's line worked out from them.
        new("MessageDialogTitleStyle", 25.5, Normal, FontWeight.SemiBold, 31.5, true, Words, 3, PhoneLine.Height(25.5, 31.5), double.NaN, 255, 0),
        new("MessageDialogContentStyle", 18.14, Normal, FontWeight.Normal, 21.5, true, Words, 0, PhoneLine.Height(18.14, 21.5), double.NaN, 255, 0),
    ];

    public static TheoryData<string> Keys() => [.. All.Select(r => r.Key)];

    public static TheoryData<string> CapturedKeys() => [.. All.Where(r => r.Captured).Select(r => r.Key)];

    public static TextStyleReading Get(string key) => All.Single(r => r.Key == key);
}
