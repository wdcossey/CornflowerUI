using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ToggleButton and RepeatButton as the phone has them: the values their default styles and templates set (the
/// 8.1 Update 1 runtime's <c>togglebutton.json</c> and <c>repeatbutton.json</c>, with <c>themeresources.json</c>), what
/// the reference app read and drew of a <c>ToggleButton { Content = "toggle button" }</c>, unchecked and checked, and a
/// <c>RepeatButton { Content = "repeat button" }</c> on the reference profile, run <c>20261108-1443-dev11</c> (device
/// 11, 1080 × 1920 at 2.4), whose PNGs are in <c>ref/wp81/ToggleButton/</c> and <c>ref/wp81/RepeatButton/</c>, and
/// what the probe of run <c>20261112-0755-dev11</c> read through the API and logged under a real touch. Values only:
/// the JSON stays in <c>R:\cornflower-reference</c>. The stage, the sizes and the setters they share with Button are
/// <see cref="ButtonReadings"/>'s: the dumps read the same.
/// </summary>
internal static class ToggleButtonReadings
{
    // The dumps' elementInStage: 19.1667, 27.5, and 57.5 tall; the width is the text's ("toggle button" 138.75,
    // "repeat button" 137.9167 in Segoe WP Semibold), so it isn't compared.
    public static readonly Point InStage = new(19.1667, 27.5);

    // The probe's reading through the API (run 20261112-0755-dev11), the phone's defaults.
    public const int RepeatDelay = 500;                   // RepeatButton.Delay
    public const int RepeatInterval = 33;                 // RepeatButton.Interval

    // The checked fill: PhoneAccentBrush, the accent, Cobalt. The text over it: PhoneButtonBasePressedForegroundBrush.
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);

    // CheckedDisabled: EnabledBackground at 0.4, drawn on the page: the border and the fill.
    public const double CheckedDisabledOpacity = 0.4;

    public static Color CheckedDisabledBorderInk(bool dark) => Color.FromUInt32(dark ? 0xFF666666u : 0xFF999999u);

    public static Color CheckedDisabledFillInk(bool dark) => Color.FromUInt32(dark ? 0xFF192866u : 0xFFB2C1FFu);

    // The PNGs, in raw pixels: the left border's columns 46-51 on row 135, and the top and bottom border's rows 89-94
    // and 175-180 on column 60, clear of the text; the page just outside (column 45, rows 88 and 181) and the fill
    // just inside (column 52 and 56, rows 95 and 174). The right border moves with the text's width.
    public static readonly int[] LeftColumns = [46, 47, 48, 49, 50, 51];
    public static readonly int[] Rows = [89, 90, 91, 92, 93, 94, 175, 176, 177, 178, 179, 180];
    public static readonly int[] OutsideColumns = [45];
    public static readonly int[] OutsideRows = [88, 181];
    public static readonly int[] InsideColumns = [52, 56];
    public static readonly int[] InsideRows = [95, 174];
    public const int ReadRow = 135, ReadColumn = 60;
}
