using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 RadioButton as the phone has it: the values its default style and template set (the 8.1 Update 1
/// runtime's <c>radiobutton.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and what
/// the reference app read and drew of a <c>RadioButton { Content = "radio button" }</c> (checked and unchecked) on the
/// reference profile, run <c>20261108-1443-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs are in
/// <c>ref/wp81/RadioButton/</c>. Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class RadioButtonReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 100, the page's background, a slot 19 in and 12 down.
    public const double StageWidth = 450;
    public const double StageHeight = 100;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dump's elementInStage: [19.1667, 12.0833, 167.9167, 57.5]. MinWidth 168 is 403.2 raw pixels, which the
    // phone rounds to 403 and Avalonia up to 404 (ADR 0003): ours is 168.3333.
    public static readonly Point InStage = new(19.1667, 12.0833);
    public const double OurWidth = 168.3333;
    public const double Height = 57.5;

    // The dump's Container, in the RadioButton: [0, 15.8333, 167.9167, 25.4167]; PhoneTouchTargetLargeOverhang 0,16.
    public const double ContainerTop = 15.8333;

    // The dump's CheckBackground: [0, 15.8333, 25.4167, 25.4167], 25.5 in the template: 61.2 raw pixels, the phone's
    // 61, Avalonia's 62 (ADR 0003). The column it sits in, 25.5 too, is 25.4167 in both: Avalonia's Grid rounds a
    // column to the nearest.
    public const double OurCircle = 25.8333;
    public const double Column = 25.4167;

    // The dump's CheckMark: [6.6667, 22.5, 12.5, 12.5], so 6.6667, 6.6667 in the circle: 30 raw pixels, 16 in.
    public const double Dot = 12.5;
    public const double DotIn = 6.6667;

    // The dump's empty TextBlock in the circle's cell: [0, 15.8333, 0, 24.1276], Segoe WP's line, 1.33 em at 18.14
    // (PhoneLineSpacer here).
    public const double EmptyLine = 24.1276;

    // The dump's ContentPresenter: [33.3333, 15.8333, 100.4167, 24.1667], the TextBlock in it 24.1267 tall: Segoe WP's
    // line, 1.33 em at 18.14. Its width is the font's, so it isn't compared.
    public static readonly Point ContentAt = new(33.3333, 15.8333);
    public const double ContentHeight = 24.1667;

    // The style's setters (radiobutton.json), with the values themeresources.json gives their keys, as the dump read them.
    public static readonly Thickness Padding = new(8, 0, 0, 0);                  // CheckBoxAndRadioButtonTextPaddingThickness
    public const double StrokeThickness = 2.5;                                   // PhoneStrokeThickness
    public const double MinWidth = 168;                                          // CheckBoxAndRadioButtonMinWidthSize
    public const double FontSize = 18.14;                                        // TextStyleLargeFontSize
    public static readonly Thickness Overhang = new(0, 16);                      // PhoneTouchTargetLargeOverhang

    // The brushes, per variant, from the dumps (base, CommonStates.Pressed and CommonStates.Disabled) and the PNGs.
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);     // PhoneRadioCheckBoxBrush
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);          // ButtonPressedBackgroundThemeBrush: the accent, Cobalt
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);      // ButtonPressedForegroundThemeBrush, both variants

    /// <summary>PhoneForegroundColor: PhoneRadioCheckBoxBorderBrush, PhoneRadioCheckBoxCheckBrush, and the text.</summary>
    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    /// <summary>PhoneBaseLowColor: ButtonDisabledBorderThemeBrush and ButtonDisabledForegroundThemeBrush.</summary>
    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);         // ApplicationPageBackgroundThemeBrush

    /// <summary>The disabled colour as the PNGs draw it on the page: #FF737373 dark, #FFBFBFBF light.</summary>
    public static Color DisabledInk(bool dark) => Color.FromUInt32(dark ? 0xFF737373u : 0xFFBFBFBFu);

    // The PNGs, in raw pixels, on the circle's middle row (97) and column (76): the ring is 6 pixels wide (2.5 at 2.4),
    // columns 46-51 and 101-106 (100 a trace, 106 nearly whole), rows 67-72 and 122-127, every pixel of it wholly the
    // ring's colour but those two edges. Just outside and inside, the page. Ours is one raw pixel wider and taller (62,
    // not 61), so its right and bottom sides are one further out, and wholly inked: 102-107 and 123-128.
    public const int MiddleRow = 97, MiddleColumn = 76;
    public static readonly int[] LeftColumns = [46, 47, 48, 49, 50, 51];
    public static readonly int[] TopRows = [67, 68, 69, 70, 71, 72];
    public static readonly int[] OurRightColumns = [102, 103, 104, 105, 106, 107];
    public static readonly int[] OurBottomRows = [123, 124, 125, 126, 127, 128];

    // The dot: columns 62-91 on the middle row (62 and 91 a little short of whole) and rows 83-112 on the middle column,
    // 30 pixels across; the page between it and the ring, 52-61 and 92-99, 73-82 and 113-121. Ours is on the same pixels.
    public const int DotLeft = 62, DotRight = 91, DotTop = 83, DotBottom = 112;
}
