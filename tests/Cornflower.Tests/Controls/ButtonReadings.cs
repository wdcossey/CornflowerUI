using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Button as the phone has it: the values its default style and template set (the 8.1 Update 1
/// runtime's <c>button.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and what
/// the reference app read and drew of a <c>Button { Content = "button" }</c> on the reference profile, run
/// <c>20261108-1443-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs are in <c>ref/wp81/Button/</c>.
/// Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ButtonReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 100, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 100;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 27.5, 109.1667, 57.5]. MinWidth 109, rounded to 262 raw pixels.
    public static readonly Rect InStage = new(19.1667, 27.5, 109.1667, 57.5);

    // The dump's Border, in the Button: [0, 9.5833, 109.1667, 38.3333]; PhoneTouchTargetOverhang 0,9.5.
    public static readonly Rect BorderBounds = new(0, 9.5833, 109.1667, 38.3333);

    // The dump's ContentPresenter, in the Button: top 15, 24.1667 tall (Segoe WP's line, 1.33 em at 18.14,
    // rounded). Its left and width are the font's, so they aren't compared.
    public const double ContentTop = 15;
    public const double ContentHeight = 24.1667;

    // The style's setters (button.json), with the values themeresources.json gives their keys, as the dump read them.
    public static readonly Thickness Padding = new(9.5, 0, 9.5, 3.5);          // PhoneButtonContentPadding
    public static readonly Thickness BorderThickness = new(2.5);                 // PhoneBorderThickness
    public const double MinWidth = 109;                                          // PhoneButtonMinWidth
    public const double MinHeight = 57.5;                                        // PhoneButtonMinHeight
    public const double FontSize = 18.14;                                        // TextStyleLargeFontSize
    public static readonly FontWeight Weight = FontWeight.SemiBold;                // PhoneButtonFontWeight, Semibold, 600
    public static readonly Thickness Overhang = new(0, 9.5);                     // PhoneTouchTargetOverhang

    // The brushes, per variant, from the dumps (base, CommonStates.Pressed and CommonStates.Disabled).
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);     // Background; ButtonDisabledBackgroundThemeBrush
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);          // ButtonPressedBackgroundThemeBrush: the accent, Cobalt
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);      // ButtonPressedForegroundThemeBrush, both variants

    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);   // PhoneForegroundBrush

    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);     // PhoneBaseLowColor

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);         // ApplicationPageBackgroundThemeBrush

    // The PNGs, in raw pixels: the border is 6 pixels wide (2.5 at 2.4), columns 46-51 and 302-307, rows
    // 89-94 and 175-180, every pixel of it the border's colour composited on the page: #FFFFFFFF dark,
    // #FF000000 light; disabled #FF737373 dark, #FFBFBFBF light. Everything just outside and inside is the page.
    public static readonly int[] BorderColumns = [46, 47, 48, 49, 50, 51, 302, 303, 304, 305, 306, 307];
    public static readonly int[] BorderRows = [89, 90, 91, 92, 93, 94, 175, 176, 177, 178, 179, 180];
    public static readonly int[] PageColumns = [45, 52, 301, 308];
    public static readonly int[] PageRows = [88, 95, 174, 181];

    /// <summary>The row the columns are read on, and the column the rows are read on: clear of the text.</summary>
    public const int ReadRow = 135, ReadColumn = 150;

    public static Color DisabledInk(bool dark) => Color.FromUInt32(dark ? 0xFF737373u : 0xFFBFBFBFu);
}
