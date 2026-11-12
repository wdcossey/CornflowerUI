using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 TextBox as the phone has it: the values its default style and template set (the 8.1 Update 1 runtime's
/// <c>textbox.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and what the reference app
/// read and drew of its TextBoxes on the reference profile (device 11, 1080 × 1920 at 2.4): runs
/// <c>20261108-1443-dev11</c> (<c>Header = "text box"</c>, with <c>PlaceholderText = "placeholder"</c> or
/// <c>Text = "Agjy 0123"</c>) and <c>20261112-1319-dev11</c> (no header; and a wrapped one), whose PNGs are in
/// <c>ref/wp81/TextBox/</c>. Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class TextBoxReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 140 (110 for no header, 220 wrapped), the page's background, a
    // slot 19 in and 12 down, 19 from the right; the TextBox stretches across it and sits at its top, 9.5 down.
    public const double StageWidth = 450;
    public const double StageHeight = 140;
    public static readonly Thickness Slot = new(19, 12, 19, 0);
    public const string Header = "text box";
    public const string Text = "Agjy 0123";
    public const string Placeholder = "placeholder";
    public const string Wrapped = "Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123";

    // The dump's elementInStage: [19.1667, 21.6667, 412.0833, 65.8333]. The slot's side margins are 45.6 raw pixels
    // each; the phone gives the TextBox 989 of the 1080, Avalonia rounds both margins to 46 and gives it 988
    // (ADR 0003), so ours is 411.6667 wide and its right edge one raw pixel short of the phone's.
    public static readonly Point InStage = new(19.1667, 21.6667);
    public const double PhoneWidth = 412.0833;
    public const double OurWidth = 411.6667;

    // The style's setters (textbox.json's keys, themeresources.json's values, as the dump read them).
    public const double MinWidth = 51;                              // TextControlThemeMinWidth
    public const double MinHeight = 34;                             // TextControlThemeMinHeight, the box's and ContentElement's
    public const double BorderThickness = 2.5;                      // TextControlBorderThemeThickness
    public const double FontSize = 20.26;                           // ContentControlFontSize
    public static readonly Thickness Padding = new(7, 0.8, 8, 0);   // TextControlThemePadding
    public static readonly Thickness Margin = new(0, 9.5, 0, 9.5);  // TextControlMarginThemeThickness
    public const double RestOpacity = 0.8;                          // TextControlBorderThemeOpacity, the Normal state's

    // The header: HeaderContentPresenterStyle (PhoneFontFamilyNormal, TextStyleMediumFontSize 16, Normal, PhoneMidBrush)
    // with TextControlHeaderMarginThemeThickness below it. The dump's HeaderContentPresenter: [0, 0, 412.0833, 21.6667],
    // its TextBlock 21.28 tall (1.33 em at 16, 51.07 raw pixels, both round to 52), 4.8 above BorderElement.
    public const double HeaderFontSize = 16;
    public const double HeaderHeight = 21.6667;
    public const double HeaderGap = 4.8;

    // The dump's BorderElement: [0, 26.6667, 412.0833, 39.1667] (64 and 94 raw pixels: the header and its 4.8, 26.4667,
    // and ContentElement's 34.1333, its MinHeight or the text's 33.3333 and the Padding's 0.8, each to the pixel grid,
    // and the border's 2.5 twice). The TextBox: 65.8333 tall; with no header, 39.1667, BorderElement at its top, as the
    // phone collapses the header (margin and all) when Header is null.
    public const double BorderTop = 26.6667;
    public const double BorderHeight = 39.1667;
    public const double Height = 65.8333;
    public const double NoHeaderHeight = 39.1667;

    // The dump's text view (ContentElement's ScrollContentPresenter): at [9.5833, 30], 33.3333 tall, the ScrollViewer's
    // 34.1667 less the Padding's top. The placeholder's TextBlock at the same place, 26.9467 tall: 1.33 em at 20.26.
    public static readonly Point TextAt = new(9.5833, 30);
    public const double TextHeight = 33.3333;
    public const double PlaceholderLine = 26.9467;

    // The wrapped TextBox (Text = Wrapped, TextWrapping = Wrap, AcceptsReturn = true; run 20261112-1319-dev11): two lines,
    // the text view 54.1667 tall (130 raw pixels, 1.33 em twice to the grid), BorderElement 60, the TextBox 86.6667.
    // Their baselines 65 raw pixels apart: 27.0833, 1.33 em (26.9458) on the pixel grid.
    public const double WrappedTextHeight = 54.1667;
    public const double WrappedHeight = 86.6667;

    // BorderElement's raw pixels in the PNGs: rows 116 to 209, columns 46 to 1034, the border 6 raw pixels wide.
    public const int BorderTopRow = 116;
    public const int BorderBottomRow = 209;
    public const int BorderLeftColumn = 46;
    public const int BorderWidthPixels = 6;

    // The brushes, per variant, from the dumps and the PNGs.
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);       // TextSelectionHighlightColorThemeBrush: the accent, Cobalt
    public static readonly Color Black = Color.FromUInt32(0xFF000000);        // TextBoxForegroundThemeBrush, both
    public static readonly Color White = Color.FromUInt32(0xFFFFFFFF);        // TextBoxFocusedBackgroundThemeBrush, both
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);  // TextBoxDisabledBackgroundThemeBrush

    /// <summary>TextBoxBackgroundThemeBrush and TextBoxBorderThemeBrush: the same.</summary>
    public static Color Box(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFFBFBFBFu);

    /// <summary>TextBoxPlaceholderTextThemeBrush.</summary>
    public static Color PlaceholderColour(bool dark) => Color.FromUInt32(dark ? 0xFF909090u : 0x40000000u);

    /// <summary>The header's PhoneMidBrush.</summary>
    public static Color HeaderColour(bool dark) => Color.FromUInt32(dark ? 0x99FFFFFFu : 0x73000000u);

    /// <summary>TextBoxDisabledBorderThemeBrush and TextBoxDisabledHeaderForegroundThemeBrush (PhoneBaseLowColor).</summary>
    public static Color DisabledBorder(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);

    /// <summary>TextBoxDisabledForegroundThemeBrush (PhoneBaseLowSolidColor), the text's and the placeholder's.</summary>
    public static Color DisabledText(bool dark) => Color.FromUInt32(dark ? 0xFF747474u : 0xFFBFBFBFu);

    /// <summary>The page's background, ApplicationPageBackgroundThemeBrush.</summary>
    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);

    // The PNGs' pixels: at rest the box and its border are one colour, 204 grey, in both variants (white at 0.8 over
    // black; #BFBFBF at 0.8 over white); focused, the accent round white; disabled, the border alone, #73FFFFFF over
    // black (115 grey) or #40000000 over white (191 grey).
    public static readonly Color RestPixel = Color.FromRgb(204, 204, 204);

    /// <summary>The disabled border's pixel over the page.</summary>
    public static Color DisabledBorderPixel(bool dark) => dark ? Color.FromRgb(115, 115, 115) : Color.FromRgb(191, 191, 191);
}
