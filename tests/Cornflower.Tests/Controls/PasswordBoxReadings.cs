using Avalonia;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 PasswordBox as the phone has it, where it differs from the TextBox (<see cref="TextBoxReadings"/>, whose
/// stage, sizes and colours it shares): the values its default style and template set (the 8.1 Update 1 runtime's
/// <c>passwordbox.json</c> and <c>themeresources.json</c>), and what the reference app read and drew on the reference
/// profile (device 11, 1080 × 1920 at 2.4): run <c>20261108-1443-dev11</c> (<c>password</c>:
/// <c>Header = "password box", Password = "secret", IsPasswordRevealButtonEnabled = true</c>) and run
/// <c>20261112-1429-dev11</c> (no header; empty with a placeholder; the reveal button on an empty box and with no
/// header), whose PNGs are in <c>ref/wp81/PasswordBox/</c>. Values only: the JSON stays in
/// <c>R:\cornflower-reference</c>.
/// </summary>
internal static class PasswordBoxReadings
{
    public const string Header = "password box";
    public const string Password = "secret";
    public const char PasswordChar = '●';                              // WinRT's default PasswordChar, U+25CF
    public const string RevealText = "Show Password";                  // the RevealButton's content, from WinRT's resources

    // With the RevealButton shown (IsPasswordRevealButtonEnabled, whatever the password: reveal-empty shows it too), on
    // the 140 stage, which leaves the box 109 (140, less the slot's 12 and the box's 9.5 twice): the dump's PasswordBox
    // 108.75 tall; BorderElement 26.6667 down and only 31.25 tall, the star row that is left beside the header and the
    // CheckBox's row; ContentElement still at 29.1667 and 34.1667 tall, past the border's bottom; the CheckBox
    // [0, 51.25, 167.9167, 57.5], PasswordBoxCheckBoxThemeMargin (0,-6.5,0,0) lifting it into the row above.
    public const double RevealHeight = 108.75;
    public const double RevealBorderHeight = 31.25;
    public const double ContentTop = 29.1667;
    public const double ContentHeight = 34.1667;
    public const double RevealTop = 51.25;
    public const double RevealButtonHeight = 57.5;
    public static readonly Thickness RevealMargin = new(0, -6.5, 0, 0);

    // With no header, on the 110 stage, which leaves 79 (reveal-no-header): the PasswordBox 78.75, BorderElement 27.9167,
    // the CheckBox 21.25 down.
    public const double RevealNoHeaderHeight = 78.75;
    public const double RevealNoHeaderBorderHeight = 27.9167;
    public const double RevealNoHeaderTop = 21.25;

    // The PNGs: BorderElement's rows 116 to 190 with the RevealButton (75 raw pixels).
    public const int RevealBorderBottomRow = 190;
}
