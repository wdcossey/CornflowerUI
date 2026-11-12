using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Slider as the phone has it: the values its default style and template set (the 8.1 Update 1 runtime's
/// <c>slider.json</c> and <c>themeresources.json</c>, which equal the SDK's design files), and what the reference app
/// read and drew of a <c>Slider { Header = "slider", Value = 30 }</c> on the reference profile, run
/// <c>20261108-1443-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs are in <c>ref/wp81/Slider/</c>. Values
/// only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class SliderReadings
{
    // The stage (Suite.Place in tools/refapp): 450 × 140, the page's background, a slot 19 in and 12 down, 19 from the
    // right; the Slider stretches across it and down to its bottom.
    public const double StageWidth = 450;
    public const double StageHeight = 140;
    public static readonly Thickness Slot = new(19, 12, 19, 0);
    public const double Value = 30;
    public const string Header = "slider";

    // The dump's elementInStage: [19.1667, 12.0833, 412.0833, 127.9167]. The slot's margins are 45.6 raw pixels each;
    // the phone gives the Slider 989 of the 1080, Avalonia rounds both margins to 46 and gives it 988 (ADR 0003): ours
    // is 411.6667 wide, so the track ends one raw pixel short of the phone's.
    public static readonly Point InStage = new(19.1667, 12.0833);
    public const double PhoneWidth = 412.0833;
    public const double OurWidth = 411.6667;
    public const double Height = 127.9167;

    // The dump's HeaderContentPresenter: [0, 0, 412.0833, 21.6667], its TextBlock 21.28 tall: Segoe WP's line, 1.33 em
    // at 16 (TextStyleMediumFontSize), 51.07 raw pixels, which both round to 52.
    public const double HeaderHeight = 21.6667;
    public const double HeaderFontSize = 16;

    // The dump's SliderContainer and HorizontalTemplate: [0, 21.6667, 412.0833, 106.25], the rest of the Slider.
    public const double ContainerTop = 21.6667;
    public const double ContainerHeight = 106.25;

    // The template's rows: 17.5, Auto (the track's 9, SliderTrackThemeHeight) and 30.5. The dump's HorizontalTrackRect:
    // [0, 39.1667, 412.0833, 9.1667], 22 raw pixels; HorizontalBorder: [0, 21.6667, 412.0833, 57.0833], the three rows.
    public const double TrackTop = 39.1667;
    public const double TrackHeight = 9.1667;
    public const double BorderHeight = 57.0833;
    public const double OurUnstretchedHeight = 57.5;

    // The dump's HorizontalDecreaseRect: [0, 39.1667, 120.8333, 9.1667], its Width set by WinRT's code to 120.75, three
    // tenths of the track less the thumb; HorizontalThumb: [120.8333, 34.5833, 9.5833, 19.1667], 9.5 × 19 in the
    // template, 13 down from the container (12.9167, 31 raw pixels).
    public const double DecreaseWidth = 120.8333;
    public static readonly Point ThumbAt = new(120.8333, 34.5833);
    public const double ThumbWidth = 9.5833;
    public const double ThumbHeight = 19.1667;
    public const double ThumbBorderThickness = 0.8;

    // The template's vertical parts: columns 13.5, Auto (the track's 9) and 13.5; the thumb 9 × 9
    // (SliderTrackThemeHeight both ways). Not captured: the reference app has no vertical Slider.
    public const double VerticalSide = 13.5;
    public const double VerticalTrack = 9;

    // The outside tick bars: SliderOutsideTickBarThemeHeight, 5; the horizontal ones 4.5 past each end of the track,
    // the vertical ones 1.5 from it. Not captured: the reference app's Slider has no ticks.
    public const double TickBarHeight = 5;

    // The brushes, per variant, from the dumps (base, CommonStates.Disabled) and the PNGs.
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);       // SliderBorderThemeBrush, SliderDisabledBorderThemeBrush
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);            // SliderTrackDecreaseBackgroundThemeBrush: the accent, Cobalt

    /// <summary>SliderTrackBackgroundThemeBrush and SliderTrackBorderThemeBrush, and their Disabled keys: the same.</summary>
    public static Color Track(bool dark) => Color.FromUInt32(dark ? 0xFF1F1F1Fu : 0xFFDEDEDEu);

    /// <summary>SliderThumbBackgroundThemeBrush, and SliderThumbBorderThemeBrush (PhoneForegroundColor): the same.</summary>
    public static Color Thumb(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    /// <summary>SliderThumbDisabledBackgroundThemeBrush and SliderThumbDisabledBorderThemeBrush.</summary>
    public static Color DisabledThumb(bool dark) => Color.FromUInt32(dark ? 0xFF999999u : 0xFF808080u);

    /// <summary>SliderTrackDecreaseDisabledBackgroundThemeBrush.</summary>
    public static Color DisabledDecrease(bool dark) => Color.FromUInt32(dark ? 0xFF666666u : 0xFFBDBDBDu);

    /// <summary>PhoneMidBrush, the header's foreground (HeaderContentPresenterStyle): #99FFFFFF dark, #73000000 light.</summary>
    public static Color HeaderInk(bool dark) => Color.FromUInt32(dark ? 0x99FFFFFFu : 0x73000000u);

    /// <summary>SliderTickmarkOutsideBackgroundThemeBrush (PhoneBaseMediumMidColor), and its Disabled key: the same.</summary>
    public static Color Tick(bool dark) => Color.FromUInt32(dark ? 0x80FFFFFFu : 0x80000000u);

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);            // ApplicationPageBackgroundThemeBrush

    // The PNGs (1080 × 336), in raw pixels. On row 134, through the track: the page to 45, the decrease rect 46-335,
    // the thumb 336-358, the track 359-1034 (ours to 1033), the page from 1035. On column 200 the track is rows 123-144;
    // on column 347, the thumb is rows 112-157. Every pixel whole: no edge is shared.
    public const int TrackRow = 134;
    public const int DecreaseLeft = 46, DecreaseRight = 335;
    public const int ThumbLeft = 336, ThumbRight = 358;
    public const int PhoneTrackRight = 1034, OurTrackRight = 1033;
    public const int TrackColumn = 200, ThumbColumn = 347;
    public const int TrackTopRow = 123, TrackBottomRow = 144;
    public const int ThumbTopRow = 112, ThumbBottomRow = 157;
}
