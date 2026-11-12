using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 CheckBox as the phone has it: the values its default style and template set (the 8.1 Update 1
/// runtime's <c>checkbox.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and what
/// the reference app read and drew of a <c>CheckBox { Content = "check box" }</c> (checked, unchecked, and
/// three-state indeterminate) on the reference profile, run <c>20261108-1443-dev11</c> (device 11, 1080 × 1920
/// at 2.4), whose PNGs are in <c>ref/wp81/CheckBox/</c>. Values only: the JSON stays in
/// <c>R:\cornflower-reference</c>.
/// </summary>
internal static class CheckBoxReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 100, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 100;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 12.0833, 167.9167, 57.5]. MinWidth 168 is 403.2 raw pixels, which the
    // phone rounds to 403 and Avalonia up to 404 (ADR 0003): ours is 168.3333.
    public static readonly Point InStage = new(19.1667, 12.0833);
    public const double PhoneWidth = 167.9167;
    public const double OurWidth = 168.3333;
    public const double Height = 57.5;

    // The dump's Grid, in the CheckBox: [0, 15.8333, 167.9167, 25.4167]; PhoneTouchTargetLargeOverhang 0,16.
    public const double GridTop = 15.8333;

    // The dump's CheckBackground: [0, 15.8333, 25.4167, 25.4167], 25.5 in the template: 61.2 raw pixels, the
    // phone's 61, Avalonia's 62 (ADR 0003). The column it sits in, 25.5 too, is 25.4167 in both.
    public const double PhoneBox = 25.4167;
    public const double OurBox = 25.8333;
    public const double Column = 25.4167;

    // The dump's CheckGlyph: [3.75, 20, 18.3333, 17.0833], so 3.75, 4.1667 in the box: 44 × 41 raw pixels, 9 and
    // 10 in. Centred in the 61-raw column, it is 8.5 raw pixels in, which the phone rounds to 9 and Avalonia to 8.
    public static readonly Size Glyph = new(18.3333, 17.0833);
    public const double PhoneGlyphLeft = 3.75;
    public const double OurGlyphLeft = 3.3333;
    public const double GlyphTop = 4.1667;

    // The dump's NormalRectangle when indeterminate: [6.25, 22.0833, 12.9167, 12.9167]; 13 in the template, 31.2 raw:
    // the phone's 31, Avalonia's 32.
    public const double PhoneRectangle = 12.9167;
    public const double OurRectangle = 13.3333;

    // The dump's ContentPresenter: [33.3333, 15.8333, 80, 24.1667], the TextBlock in it 24.1267 tall: Segoe WP's line,
    // 1.33 em at 18.14. Its width is the font's, so it isn't compared.
    public static readonly Point ContentAt = new(33.3333, 15.8333);
    public const double ContentHeight = 24.1667;

    // The style's setters (checkbox.json), with the values themeresources.json gives their keys, as the dump read them.
    public static readonly Thickness Padding = new(8, 0, 0, 0);                  // CheckBoxAndRadioButtonTextPaddingThickness
    public static readonly Thickness BorderThickness = new(2.5);                 // PhoneBorderThickness
    public const double MinWidth = 168;                                          // CheckBoxAndRadioButtonMinWidthSize
    public const double FontSize = 18.14;                                        // TextStyleLargeFontSize
    public static readonly Thickness Overhang = new(0, 16);                      // PhoneTouchTargetLargeOverhang

    // The brushes, per variant, from the dumps (base, CommonStates.Pressed and CommonStates.Disabled).
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);     // CheckBoxBackgroundThemeBrush, CheckBoxDisabledBackgroundThemeBrush
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);          // CheckBoxPressedBackgroundThemeBrush: the accent, Cobalt
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);      // CheckBoxPressedForegroundThemeBrush, both variants

    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);   // PhoneForegroundColor

    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);     // PhoneBaseLowColor

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);         // ApplicationPageBackgroundThemeBrush

    /// <summary>The disabled colour as the PNGs draw it on the page: #FF737373 dark, #FFBFBFBF light.</summary>
    public static Color DisabledInk(bool dark) => Color.FromUInt32(dark ? 0xFF737373u : 0xFFBFBFBFu);

    // The PNGs, in raw pixels: the box's border is 6 pixels wide (2.5 at 2.4), columns 46-51 and 101-106, rows
    // 67-72 and 122-127, every pixel of it the border's colour on the page. Just outside and inside, the page.
    // Ours is one raw pixel wider and taller (62, not 61), so its right and bottom sides are one further out.
    public static readonly int[] LeftColumns = [46, 47, 48, 49, 50, 51];
    public static readonly int[] TopRows = [67, 68, 69, 70, 71, 72];
    public static readonly int[] PhoneRightColumns = [101, 102, 103, 104, 105, 106];
    public static readonly int[] PhoneBottomRows = [122, 123, 124, 125, 126, 127];

    /// <summary>The row the columns are read on, and the column the rows are read on: inside the box, clear of the tick.</summary>
    public const int ReadRow = 74, ReadColumn = 53;

    // The tick in dark__CheckBox__checked__base.png: wholly inked at (86, 90) on its long arm and (70, 108) on its
    // short one, ours too, one raw pixel to the left; the page at (60, 80), above the short arm, in both.
    public static readonly PixelPoint[] TickInk = [new(86, 90), new(70, 108)];
    public static readonly PixelPoint ClearOfTheTick = new(60, 80);
}
