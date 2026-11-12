using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 HyperlinkButton as the phone has it: the values its default style and template set (the 8.1 Update 1
/// runtime's <c>hyperlinkbutton.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and
/// what the reference app read and drew of a <c>HyperlinkButton { Content = "hyperlink button" }</c> on the reference
/// profile, run <c>20261108-1443-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs are in
/// <c>ref/wp81/HyperlinkButton/</c>. Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class HyperlinkButtonReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 100, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 100;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 32.5, 211.25, 47.0833]. Its width is Segoe WP's "hyperlink button", so
    // only the left, the top and the height are compared.
    public static readonly Rect InStage = new(19.1667, 32.5, 211.25, 47.0833);

    // The dump's ContentPresenter, in the control: top 2.9167, 40 tall; its TextBlock 39.7167 tall (Segoe WP's line,
    // 1.33 em at 29.86). Grid and Border fill the control.
    public const double ContentTop = 2.9167;
    public const double ContentHeight = 40;

    // The style's setters (hyperlinkbutton.json), with the values themeresources.json gives their keys, as the dump
    // read them.
    public static readonly Thickness Padding = new(0, 3, 0, 4);                     // a literal in the style
    public static readonly Thickness BorderThickness = new(0);                      // HyperlinkButtonBorderThemeThickness
    public const double FontSize = 29.86;                                           // TextStyleExtraLargePlusFontSize
    public static readonly FontWeight Weight = FontWeight.SemiLight;                // a literal, 350

    // The brushes, per variant, from the dumps (base, CommonStates.Pressed and CommonStates.Disabled).
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);        // HyperlinkButtonBackground- and BorderThemeBrush

    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);    // HyperlinkForegroundThemeBrush; also Pressed's

    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x66FFFFFFu : 0x66000000u);      // HyperlinkDisabledThemeBrush, PhoneDisabledColor

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);          // ApplicationPageBackgroundThemeBrush

    // The PNGs' ink: of the 17 colours in base and Disabled, the commonest after the page (4,727 pixels, the glyphs'
    // insides) is the text's colour composited on the page. Pressed's 256 colours are all greys: no accent.
    public static Color Ink(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    public static Color DisabledInk(bool dark) => Color.FromUInt32(dark ? 0xFF666666u : 0xFF999999u);

    // No underline: the longest run of ink on any row of base is 31 px (the t's bar), against the text's 497 px.
    public const int LongestInkRun = 31;
    public const int TextInkWidth = 497;
}
