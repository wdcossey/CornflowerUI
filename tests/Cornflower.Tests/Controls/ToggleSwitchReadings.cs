using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ToggleSwitch as the phone has it: the values its default style and template set (the 8.1 Update 1 runtime's
/// <c>toggleswitch.json</c> and <c>themeresources.json</c>, which equal the SDK's design files); what the reference app
/// read and drew of its three specimens on the reference profile, run <c>20261108-1443-dev11</c> (device 11, 1080 ×
/// 1920 at 2.4), whose PNGs are in <c>ref/wp81/ToggleSwitch/</c>; what <c>-Native</c> read of its
/// <c>TemplateSettings</c>; and what the touch probes measured (tools/refapp/README.md, "ToggleSwitch: tap or drag"
/// and "the hold limit, the distance and the halfway line"; tools/motion/toggle.py and knob.py). Values only: the JSON
/// stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ToggleSwitchReadings
{
    // The stage (Suite.Place in tools/refapp): 450 wide, 140 tall (110 for the one without a header), the page's
    // background, a slot 19 in and 12 down, 19 from the right; the style centres the switch in it.
    public const double StageWidth = 450;
    public const double StageHeight = 140;
    public const double NoHeaderStageHeight = 110;
    public static readonly Thickness Slot = new(19, 12, 19, 0);
    public const string Header = "toggle switch";

    // The dump's elementInStage: [19.1667, 43.3333, 412.0833, 65.4167], and [19.1667, 30.4167, 412.0833, 61.6667]
    // without a header. Avalonia rounds the slot's 45.6 raw px margins to 46 (ADR 0003), so ours is 411.6667 wide.
    public static readonly Point InStage = new(19.1667, 43.3333);
    public const double PhoneWidth = 412.0833;
    public const double OurWidth = 411.6667;
    public const double Height = 65.4167;
    public const double NoHeaderPhoneHeight = 61.6667;

    // The rows: the header (21.6667: 1.33 em of 16, 21.28, rounded up), the content (34.1667: 1.33 em of 25.5,
    // 33.915) and the template's 9.5 (9.5833). Without a header the first row is still 17.9167 (43 raw px), half the
    // switch's 35.8333: the switch spans both rows. Ours is half our 36.25, 18.3333.
    public const double HeaderHeight = 21.6667;
    public const double ContentHeight = 34.1667;
    public const double BottomRow = 9.5833;
    public const double PhoneEmptyHeaderRow = 17.9167;
    public const double OurEmptyHeaderRow = 18.3333;
    public const double HeaderFontSize = 16;
    public const double ContentFontSize = 25.5;

    // The template's columns: *, 13.5 and the switch's 77 (77.0833, 185 raw px), right of the header's and content's
    // Padding 0,0,25.5,0 (the dump's presenters 296.25 wide, ours 295.8333).
    public const double SwitchWidth = 77.0833;
    public const double PresenterWidth = 296.25;
    public static readonly Thickness Padding = new(0, 0, 25.5, 0);

    // SwitchKnobBounds: Height 36, 35.8333 (86 raw px) on the phone, bottom-aligned over the first two rows, so 20 down
    // with a header; Avalonia rounds 86.4 up to 87, 36.25, 19.5833 down (ADR 0003).
    public const double PhoneSwitchHeight = 35.8333;
    public const double OurSwitchHeight = 36.25;
    public const double PhoneSwitchTop = 20;
    public const double OurSwitchTop = 19.5833;

    // OuterBorder: Margin 3,4.5, BorderThickness 2.5, [2.9167, 4.5833, 71.25, 26.6667] in the bounds; InnerBorder 2.5
    // inside it; SwitchCurtainBounds [7.9167, 9.5833, 61.25, 16.6667], the curtain's clip, which -Native's
    // CurtainOnToOffOffset reads (61.25). Ours are the same across, and 0.4167 taller.
    public static readonly Point OuterBorderAt = new(2.9167, 4.5833);
    public const double OuterBorderWidth = 71.25;
    public const double BorderThickness = 2.5;
    public const double CurtainBoundsWidth = 61.25;

    // SwitchKnob: Width 20.5, 20.4167 (49 raw px) on the phone; Avalonia rounds 49.2 up to 50, 20.8333 (ADR 0003).
    public const double PhoneKnobWidth = 20.4167;
    public const double OurKnobWidth = 20.8333;
    public const double KnobStrokeThickness = 2.5;

    // -Native, run 20261111-1759-dev11 (and 20261111-1839-dev11) (tools/refapp/README.md, "TemplateSettings"): KnobOnToOffOffset 56.67, 136 raw
    // px, the switch's 185 less the knob's 49. The knob's travel, and the drag's halfway line is half of it.
    public const double KnobOnToOffOffset = 136 / 2.4;

    // SwitchThumb: the switch's grid with Margin -13.5,-15.5,-13.5,-6.5, [321.6667, 4.5833, 104.1667, 57.9167].
    public static readonly Thickness ThumbOverhang = new(13.5, 15.5, 13.5, 6.5);

    // The touch probes, on the reference profile (device 11): a release is a tap when the press moved less than 2.7 mm
    // (17.47 view px tapped, 18.33 didn't) and saw no pointer event 1,000 ms or more after it (984 ms tapped, 1,000
    // didn't, by the pointer's timestamps); a drag released past half KnobOnToOffOffset goes over (28.35 went On; 28.06
    // and 28.32 went back Off). The knob follows the finger one for one from the first move.
    public const double TappedMove = 17.47;
    public const double NotTappedMove = 18.33;
    public const double TappedEventMs = 984;
    public const double HeldEventMs = 1000;
    public const double BackOff = 28.32;
    public const double GoesOn = 28.35;

    // The slide (knob.py, run 20261110-0819-dev11): KeySpline 0.1,0.9 0.2,1 over 370 ms of animation time.
    public static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(370);
    public static readonly (double X1, double Y1, double X2, double Y2) SlideSpline = (0.1, 0.9, 0.2, 1);

    // The brushes, per variant, from themeresources.json and the dumps (base, CommonStates.Disabled).
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);      // ToggleSwitchCurtainBackgroundThemeBrush
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF); // ToggleSwitchTrackBorderThemeBrush

    /// <summary>ToggleSwitchForegroundThemeBrush, ToggleSwitchOuterBorderBorderThemeBrush and ToggleSwitchThumbBackgroundThemeBrush: PhoneForegroundColor.</summary>
    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    /// <summary>ToggleSwitchThumbBorderThemeBrush and ToggleSwitchThumbDisabledBorderThemeBrush: PhoneBackgroundColor.</summary>
    public static Color Background(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);

    /// <summary>ToggleSwitchHeaderForegroundThemeBrush: PhoneBaseMidColor.</summary>
    public static Color HeaderInk(bool dark) => Color.FromUInt32(dark ? 0x99FFFFFFu : 0x73000000u);

    /// <summary>ToggleSwitchDisabledForegroundThemeBrush and ToggleSwitchOuterBorderDisabledBorderThemeBrush: PhoneBaseLowColor.</summary>
    public static Color DisabledInk(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);

    /// <summary>ToggleSwitchThumbDisabledBackgroundThemeBrush and ToggleSwitchCurtainDisabledBackgroundThemeBrush: PhoneBaseMediumMidSolidColor.</summary>
    public static Color DisabledFill(bool dark) => Color.FromUInt32(dark ? 0xFF666666u : 0xFFCCCCCCu);

    // The PNGs (1080 × 336), row 180, through the switch, in raw px. Off: the knob's fill 856-892 (its stroke, the
    // page's colour, either side), the outer border's right side 1022-1027. On: the outer border's left side 857-862,
    // the curtain 869-984, the knob's stroke from 985.6, its fill 992-1027 (991 and 1028 part-covered: the knob is at
    // the template's 56.5, 135.6 raw px). Ours is one raw px further left (the 988-wide slot), its knob whole pixels.
    public const int Row = 180;
    public const int PhoneOffKnobLeft = 856, PhoneOffKnobRight = 892;
    public const int PhoneBorderRightLeft = 1022, PhoneBorderRightRight = 1027;
    public const int PhoneBorderLeftLeft = 857, PhoneBorderLeftRight = 862;
    public const int PhoneCurtainLeft = 869, PhoneCurtainRight = 984;
    public const int PhoneOnKnobLeft = 992, PhoneOnKnobRight = 1027;

    // Ours, on the same row: the slot one raw px narrower, the knob 50 raw px.
    public const int OurOffKnobLeft = 855, OurOffKnobRight = 892;
    public const int OurBorderRightLeft = 1021, OurBorderRightRight = 1026;
    public const int OurBorderLeftLeft = 856, OurBorderLeftRight = 861;
    public const int OurCurtainLeft = 868, OurCurtainRight = 984;
    public const int OurOnKnobLeft = 991, OurOnKnobRight = 1028;

    public static Color Page(bool dark) => Background(dark);                // ApplicationPageBackgroundThemeBrush
}
