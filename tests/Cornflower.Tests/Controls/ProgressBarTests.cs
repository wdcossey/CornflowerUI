using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ProgressBarReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Avalonia's ProgressBar under the 8.1 theme (<c>Controls/ProgressBar.axaml</c>, <c>PhoneProgressBar</c>) against the
/// phone's style, the reference app's captures and dumps, the <c>TemplateSettings</c> it read, and the template's
/// Indeterminate storyboard (<see cref="ProgressBarReadings"/>). Time is a <see cref="TestClock"/>: a frame is 16 ms,
/// or the emulator's 15.625 ms where a test says. docs/controls/ProgressBar has the plan and the comparison.
/// </summary>
public class ProgressBarTests
{
    private const double FrameMs = 16;
    private const double PhoneFrameMs = 1000 / 64.0;
    private static readonly double Raw = 1 / Scene.ReferenceScaling;

    /// <summary>A bar on the reference app's stage: 450 × 60, the page's background, the bar in a slot 19 in and 12 down.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, ProgressBar? bar = null, IResourceDictionary? resources = null)
        {
            Bar = bar ?? new ProgressBar { Value = Value };
            var holder = new Grid { Margin = Slot, Children = { Bar } };
            var stage = new Grid { Children = { holder } };
            if (resources is not null)
            {
                stage.Resources = resources;
            }

            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
        }

        public ProgressBar Bar { get; }

        public Window Window => _scene.Window;

        public WriteableBitmap Capture() => _scene.Capture();

        /// <summary>The bar's top left in the window.</summary>
        public Point Origin => Bar.TranslatePoint(default, Window)!.Value;

        public void Layout() => Window.UpdateLayout();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(ProgressBar bar, string name) where T : Control =>
        bar.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Rectangle Dot(ProgressBar bar, int i) => Part<Rectangle>(bar, $"PART_E{i + 1}");

    private static TranslateTransform Move(ProgressBar bar, int i) => Assert.IsType<TranslateTransform>(Dot(bar, i).RenderTransform);

    private static TranslateTransform Slide(ProgressBar bar, int i) => Assert.IsType<TranslateTransform>(Part<Border>(bar, $"PART_B{i + 1}").RenderTransform);

    private static TranslateTransform GridMove(ProgressBar bar) => Assert.IsType<TranslateTransform>(Part<Grid>(bar, "PART_EllipseGrid").RenderTransform);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Color Pixel(WriteableBitmap frame, double x, double y) =>
        Frames.PixelAt(frame, (int)Math.Floor(x * Scene.ReferenceScaling), (int)Math.Floor(y * Scene.ReferenceScaling));

    // The style's setters (progressbar.json, themeresources.json's values), as the dump read them; and the clip the
    // phone's ProgressBar sets on itself (the dumps' clip, its bounds).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        var bar = stage.Bar;

        Assert.Equal(Accent, ColourOf(bar.Foreground));
        Assert.Equal(Accent, ColourOf(bar.Background));
        Assert.Equal(TrackOpacity, bar.Background!.Opacity, 1e-9);
        Assert.Equal(Colors.Transparent, ColourOf(bar.BorderBrush));
        Assert.Equal(default, bar.BorderThickness);
        Assert.Equal(MinHeight, bar.MinHeight);
        Assert.False(bar.IsTabStop);
        Assert.Equal(VerticalAlignment.Center, bar.VerticalAlignment);
        Assert.True(bar.ClipToBounds);
        // The phone's style sets Maximum to 100, which is Avalonia's default too, so the theme doesn't.
        Assert.Equal(100, bar.Maximum);
        Assert.Equal(0, bar.Minimum);
    }

    // The dump's bounds: across the slot and centred down it. The phone rounds 3.5 high to 8 raw pixels, Avalonia up to 9.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new Stage();
        var at = stage.Origin;

        Assert.Equal(InStage.X, at.X, 0.001);
        // The slot's right edge: the phone rounds the bar to 989 raw pixels wide, Avalonia's margin rounding to 988.
        Assert.InRange(stage.Bar.Bounds.Width, InStage.Width - Raw - 0.001, InStage.Width + 0.001);
        Assert.Equal(Math.Ceiling(MinHeight * Scene.ReferenceScaling) / Scene.ReferenceScaling, stage.Bar.Bounds.Height, 0.001);
        Assert.InRange(at.Y, InStage.Y - Raw - 0.001, InStage.Y + 0.001);
    }

    // At rest: the track across the bar, the indicator 40% of it from the left, in the phone's colours; the page beside.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Determinate_It_Draws_The_Track_And_The_Indicator(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var bar = stage.Bar;
        var indicator = Part<Border>(bar, "PART_Indicator");
        var track = Part<Border>(bar, "PART_DeterminateRoot");

        Assert.Equal(bar.Bounds.Width * Value / 100, indicator.Width, 1e-9);
        Assert.Equal(IndicatorWidth, InStage.Width * Value / 100, 0.001);
        Assert.Equal(0, indicator.TranslatePoint(default, bar)!.Value.X);
        Assert.Equal(bar.Bounds.Size, track.Bounds.Size);

        using var frame = stage.Capture();
        var o = stage.Origin;
        var y = o.Y + bar.Bounds.Height / 2;
        Assert.Equal(Accent, Pixel(frame, o.X + 1 * Raw, y));
        // The indicator's last raw pixel and the track's first: 396 raw pixels, as the phone's picture has it.
        Assert.Equal(Accent, Pixel(frame, o.X + 395.5 * Raw, y));
        // The track: the accent at 0.16 over the page, within one step of the phone's blend in each channel.
        AssertNear(Track(dark), Pixel(frame, o.X + 396.5 * Raw, y));
        AssertNear(Track(dark), Pixel(frame, o.X + bar.Bounds.Width - Raw / 2, y));
        Assert.Equal(Page(dark), Pixel(frame, o.X + bar.Bounds.Width + Raw / 2, y));
        Assert.Equal(Page(dark), Pixel(frame, o.X - Raw / 2, y));
    }

    // A new value moves the indicator at once, inside the bar's Padding, as the phone binds the indicator's Margin.
    [AvaloniaFact]
    public void The_Indicator_Follows_The_Value_Inside_The_Padding()
    {
        using var stage = new Stage();
        var bar = stage.Bar;
        var indicator = Part<Border>(bar, "PART_Indicator");

        bar.Value = 75;
        stage.Layout();
        Assert.Equal(bar.Bounds.Width * 0.75, indicator.Bounds.Width, 0.5);

        bar.Padding = new Thickness(10, 0, 20, 0);
        stage.Layout();
        Assert.Equal(new Thickness(10, 0, 20, 0), indicator.Margin);
        Assert.Equal(10, indicator.TranslatePoint(default, bar)!.Value.X, 0.5);
        Assert.Equal((bar.Bounds.Width - 30) * 0.75, indicator.Width, 1e-6);
    }

    // The phone's template has no Disabled state: a disabled bar is drawn as an enabled one.
    [AvaloniaFact]
    public void Disabled_It_Is_Drawn_The_Same()
    {
        using var enabled = new Stage();
        using var enabledFrame = enabled.Capture();
        var disabledBar = new ProgressBar { Value = Value, IsEnabled = false };
        using var disabled = new Stage(bar: disabledBar);
        using var disabledFrame = disabled.Capture();

        Assert.Equal(Bytes(enabledFrame), Bytes(disabledFrame));
    }

    // The dots at rest, where every dump with no storyboard running has them: E5 to E1 left to right, 12.5 apart,
    // 3.5 square, in the indeterminate brush, the gaps 9 wide; the grid hidden while determinate.
    [AvaloniaFact]
    public void Each_Dot_Rests_Where_The_Phones_Does()
    {
        using var stage = new Stage();
        var bar = stage.Bar;
        var grid = Part<Grid>(bar, "PART_EllipseGrid");

        Assert.Equal(0, grid.Opacity);
        Assert.Equal(bar.Bounds.Size, grid.Bounds.Size);
        for (var i = 0; i < 5; i++)
        {
            var dot = Dot(bar, i);
            Assert.Equal(DotX[i], dot.TranslatePoint(default, bar)!.Value.X, 0.001);
            Assert.Equal(DotSize, dot.Bounds.Width, 0.001);
            Assert.Equal(DotSize, dot.Bounds.Height, 0.001);
            Assert.Equal(Accent, ColourOf(dot.Fill));
            Assert.Equal(1, dot.Opacity);
        }

        var gaps = grid.Children.OfType<Rectangle>().Where(r => r.Name is null).ToList();
        Assert.Equal(4, gaps.Count);
        Assert.All(gaps, gap => Assert.Equal(Gap, gap.Bounds.Width, 0.001));
    }

    // The dots take ProgressBarIndeterminateForegroundThemeBrush from the theme, not the bar's Foreground, as the
    // phone's template does; the indicator takes the Foreground.
    [AvaloniaFact]
    public void The_Dots_Keep_Their_Brush_When_The_Foreground_Changes()
    {
        using var stage = new Stage();
        var bar = stage.Bar;
        var other = new SolidColorBrush(Colors.Transparent);
        bar.Foreground = other;

        Assert.Same(other, Part<Border>(bar, "PART_Indicator").Background);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(Accent, ColourOf(Dot(bar, i).Fill));
        }
    }

    // Indeterminate: the grid shows and the track goes, at once (the template's Duration="0" animations).
    [AvaloniaFact]
    public void Indeterminate_Shows_The_Dots_And_Hides_The_Track()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var bar = stage.Bar;
        var grid = Part<Grid>(bar, "PART_EllipseGrid");
        var track = Part<Border>(bar, "PART_DeterminateRoot");

        Assert.Equal(1, track.Opacity);
        bar.IsIndeterminate = true;
        Assert.Equal(1, grid.Opacity);
        Assert.Equal(0, track.Opacity);
        bar.IsIndeterminate = false;
        Assert.Equal(0, grid.Opacity);
        Assert.Equal(1, track.Opacity);
    }

    // The high-contrast bars: 0.8 at the bottom and the right, shown while determinate, in their own brush, which is
    // transparent in the dark and light themes. With the brush an app's colour, they draw while determinate only.
    [AvaloniaFact]
    public void The_High_Contrast_Bars_Show_While_Determinate()
    {
        var resources = new ResourceDictionary { ["ProgressBarHighContrastAccentBarThemeBrush"] = new SolidColorBrush(Colors.Transparent) };
        using var stage = new Stage(resources: resources);
        var bar = stage.Bar;
        var bottom = Part<Rectangle>(bar, "PART_HighContrastBtmAccentBar");
        var right = Part<Rectangle>(bar, "PART_HighContrastRitAccentBar");

        Assert.Equal(AccentBarThickness, bottom.Height);
        Assert.Equal(bar.Bounds.Height - bottom.Bounds.Height, bottom.Bounds.Y, 0.001);
        Assert.Equal(bar.Bounds.Width, bottom.Bounds.Width, 0.001);
        Assert.Equal(AccentBarThickness, right.Width);
        Assert.Equal(bar.Bounds.Width - right.Bounds.Width, right.Bounds.X, 0.001);
        Assert.Equal(bar.Bounds.Height, right.Bounds.Height, 0.001);
        Assert.Equal(1, bottom.Opacity);
        Assert.Equal(1, right.Opacity);
        Assert.Same(resources["ProgressBarHighContrastAccentBarThemeBrush"], bottom.Fill);
        Assert.Same(resources["ProgressBarHighContrastAccentBarThemeBrush"], right.Fill);

        using var clock = new TestClock();
        bar.IsIndeterminate = true;
        Assert.Equal(0, bottom.Opacity);
        Assert.Equal(0, right.Opacity);
    }

    // In the dark and light themes the brush is transparent: the bars draw nothing.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_High_Contrast_Bars_Are_Transparent(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        Assert.Equal(Colors.Transparent, ColourOf(Part<Rectangle>(stage.Bar, "PART_HighContrastBtmAccentBar").Fill));
        Assert.Equal(Colors.Transparent, ColourOf(Part<Rectangle>(stage.Bar, "PART_HighContrastRitAccentBar").Fill));
    }

    // TemplateSettings as the API read them on all seven profiles (native.json): the start -63, the end the width
    // over 9 for each of the 3.917 s on from it, the well a third of the width and the end two thirds.
    [Fact]
    public void TemplateSettings_Are_The_Phones()
    {
        foreach (var (device, width, start, end, well, ellipseEnd) in Settings)
        {
            var settings = PhoneProgressBar.TemplateSettings(width);
            Assert.True(Math.Abs(start - settings.ContainerStart) < 0.0001, $"device {device}: start {settings.ContainerStart}");
            Assert.True(Math.Abs(end - settings.ContainerEnd) < 0.0005, $"device {device}: end {settings.ContainerEnd} against {end}");
            Assert.True(Math.Abs(well - settings.EllipseWell) < 0.0001, $"device {device}: well {settings.EllipseWell}");
            Assert.True(Math.Abs(ellipseEnd - settings.EllipseEnd) < 0.0001, $"device {device}: end {settings.EllipseEnd}");
        }
    }

    // The storyboard's tracks, at and between their keys, against the template's keys worked out here.
    [Fact]
    public void The_Tracks_Give_The_Templates_Values()
    {
        const double width = 412.0833;
        for (var ms = 0; ms < 3917; ms++)
        {
            var time = TimeSpan.FromMilliseconds(ms);
            var s = ms / 1000.0;
            Assert.True(Math.Abs(GridAt(s, width) - PhoneProgressBar.ContainerAt(time, width)) < 0.0002, $"the grid at {ms} ms");
            for (var dot = 0; dot < 5; dot++)
            {
                var why = $"dot {dot + 1} at {ms} ms";
                Assert.True(Math.Abs(SlideAt(dot, s) - PhoneProgressBar.Slides[dot].ValueAt(time)!.Value) < 1e-9, $"{why}: B");
                Assert.True(Math.Abs(MoveAt(dot, s, width) - PhoneProgressBar.MoveAt(dot, time, width)) < 1e-6, $"{why}: E");
                Assert.Equal(OpacityAt(dot, s), PhoneProgressBar.Opacities[dot].ValueAt(time));
            }
        }

        for (var dot = 0; dot < 5; dot++)
        {
            var begin = DotBeginSeconds[dot];
            Assert.Equal(width / 3, PhoneProgressBar.MoveAt(dot, PhoneKeyframeTrack.Seconds(begin + 1), width), 1e-9);
            Assert.Equal(2 * width / 3, PhoneProgressBar.MoveAt(dot, PhoneKeyframeTrack.Seconds(begin + 3), width), 1e-9);
            Assert.Equal(0, PhoneProgressBar.Slides[dot].ValueAt(PhoneKeyframeTrack.Seconds(SlideSeconds[dot][0]))!.Value, 1e-9);
            Assert.Equal(80, PhoneProgressBar.Slides[dot].ValueAt(PhoneKeyframeTrack.Seconds(SlideSeconds[dot][2]))!.Value, 1e-9);
        }

        Assert.Equal(PhoneKeyframeTrack.Seconds(ContainerSeconds), PhoneProgressBar.Duration);
    }

    // A dot shows from its begin time until 3 s after, exactly at the key times.
    [Fact]
    public void A_Dot_Shows_From_Its_Begin_Until_3_Seconds_After()
    {
        for (var dot = 0; dot < 5; dot++)
        {
            var track = PhoneProgressBar.Opacities[dot];
            var begin = PhoneKeyframeTrack.Seconds(DotBeginSeconds[dot]);
            var hide = begin + PhoneKeyframeTrack.Seconds(3);
            if (begin > TimeSpan.Zero)
            {
                Assert.Equal(0, track.ValueAt(begin - TimeSpan.FromTicks(1)));
            }

            Assert.Equal(1, track.ValueAt(begin));
            Assert.Equal(1, track.ValueAt(hide - TimeSpan.FromTicks(1)));
            Assert.Equal(0, track.ValueAt(hide));
            Assert.Equal(0, track.ValueAt(PhoneProgressBar.Duration - TimeSpan.FromTicks(1)));
        }
    }

    // The phone's own runtime, caught mid-storyboard by the dumps: for each, one storyboard time at which the theme's
    // tracks give the grid's X within 0.001, every shown dot's left edge within 0.02, and show exactly those dots. Up
    // to four dots at once, so the begin times, the slides and the spline are the phone's, not only the template's
    // text; and each time is a frame of the emulator's 64 Hz clock.
    [Fact]
    public void Each_Captured_Moment_Is_One_Time_In_The_Storyboard()
    {
        var width = InStage.Width;
        foreach (var (capture, gridX, shown) in Moments)
        {
            var best = (Ticks: -1L, Off: double.MaxValue);
            for (var ticks = 0L; ticks < PhoneProgressBar.Duration.Ticks; ticks += TimeSpan.TicksPerMillisecond / 40)
            {
                var time = TimeSpan.FromTicks(ticks);
                // The grid within 0.001, weighed as a dot within 0.02.
                var off = Math.Abs(PhoneProgressBar.ContainerAt(time, width) - gridX) * 20;
                for (var dot = 0; dot < 5 && off < best.Off; dot++)
                {
                    var visible = PhoneProgressBar.Opacities[dot].ValueAt(time) == 1;
                    if (visible != dot < shown.Length)
                    {
                        off = double.MaxValue;
                    }
                    else if (visible)
                    {
                        var x = PhoneProgressBar.ContainerAt(time, width) + DotX[dot] + PhoneProgressBar.Slides[dot].ValueAt(time)!.Value
                                + PhoneProgressBar.MoveAt(dot, time, width);
                        off = Math.Max(off, Math.Abs(x - shown[dot]));
                    }
                }

                if (off < best.Off)
                {
                    best = (ticks, off);
                }
            }

            var seconds = best.Ticks / (double)TimeSpan.TicksPerSecond;
            Assert.True(best.Off < 0.02, $"{capture}: no storyboard time gives its dots; the nearest is {best.Off:0.####} off at {seconds * 1000} ms");
            Assert.True(Math.Abs(seconds * 64 - Math.Round(seconds * 64)) < 0.01, $"{capture}: {seconds * 1000} ms isn't a 64 Hz frame");
        }
    }

    // The 1732 pictures of the forced Indeterminate state: the theme's frame at their moment draws the two dots where
    // the phone's picture has them, in the accent, with the page between them and no track.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Pictures_Dots_Are_Drawn_Where_The_Phone_Drew_Them(string variant)
    {
        var dark = variant == "Dark";
        var (_, seconds, lefts) = Pictures.Single(p => p.Capture == variant.ToLowerInvariant());
        using var clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(PhoneFrameMs) };
        using var stage = new Stage(VariantOf(variant), new ProgressBar { Value = Value, IsIndeterminate = true });
        clock.At(seconds * 1000);
        using var frame = stage.Capture();
        var o = stage.Origin;
        var y = o.Y + stage.Bar.Bounds.Height / 2;

        foreach (var left in lefts)
        {
            Assert.Equal(Accent, Pixel(frame, o.X + left + DotSize / 2, y));
            Assert.Equal(Page(dark), Pixel(frame, o.X + left - 2 * Raw, y));
            Assert.Equal(Page(dark), Pixel(frame, o.X + left + DotSize + 2 * Raw, y));
        }

        Assert.Equal(Page(dark), Pixel(frame, o.X + (lefts[0] + lefts[1]) / 2, y));
        Assert.Equal(Page(dark), Pixel(frame, o.X + 300, y));
    }

    // The bar clips itself, as the phone's sets its Clip to its bounds: at 9/64 s the first dot is left of the bar
    // (29.5 left on the catalogue's bar: the light base dump), and nothing is drawn there. A 200-wide bar at the
    // right of the stage, so the dot's place is on the page.
    [AvaloniaFact]
    public void The_Bar_Clips_The_Dots_To_Its_Bounds()
    {
        using var clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(PhoneFrameMs) };
        using var stage = new Stage(bar: new ProgressBar { IsIndeterminate = true, Width = 200, HorizontalAlignment = HorizontalAlignment.Right });
        clock.At(9 * PhoneFrameMs);
        var dot = Dot(stage.Bar, 0);
        var left = dot.TranslatePoint(default, stage.Bar)!.Value.X;
        Assert.Equal(DotAt(0, 9 / 64.0, 200), left, 0.001);
        Assert.InRange(left, -40, -DotSize - 1);
        Assert.Equal(1, dot.Opacity);

        using var frame = stage.Capture();
        var o = stage.Origin;
        Assert.Equal(Page(true), Pixel(frame, o.X + left + DotSize / 2, o.Y + stage.Bar.Bounds.Height / 2));
    }

    // Running: every frame, each part at the storyboard's time, from 0 when the bar goes indeterminate, repeating
    // every 3.917 s.
    [AvaloniaFact]
    public void While_Indeterminate_Each_Frame_Shows_The_Storyboard_At_That_Time()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var bar = stage.Bar;
        var width = bar.Bounds.Width;
        const double start = 1000;
        clock.At(start);
        bar.IsIndeterminate = true;

        for (var ms = 0.0; ms <= 2 * ContainerSeconds * 1000 + 200; ms += FrameMs)
        {
            clock.At(start + ms);
            var storyboard = (ms % (ContainerSeconds * 1000)) / 1000;
            var why = $"{ms} ms after it went indeterminate";
            Assert.True(Math.Abs(GridAt(storyboard, width) - GridMove(bar).X) < 0.0002, $"{why}: the grid at {GridMove(bar).X}");
            for (var dot = 0; dot < 5; dot++)
            {
                Assert.True(Math.Abs(SlideAt(dot, storyboard) - Slide(bar, dot).X) < 1e-6, $"dot {dot + 1}, {why}: B at {Slide(bar, dot).X}");
                Assert.True(Math.Abs(MoveAt(dot, storyboard, width) - Move(bar, dot).X) < 1e-6, $"dot {dot + 1}, {why}: E at {Move(bar, dot).X}");
                Assert.True(OpacityAt(dot, storyboard) == Dot(bar, dot).Opacity, $"dot {dot + 1}, {why}: opacity {Dot(bar, dot).Opacity}");
            }
        }
    }

    // The positions follow the bar's width as it changes, as the phone's TemplateSettings do.
    [AvaloniaFact]
    public void A_Narrower_Bar_Moves_Its_Dots_By_Its_Own_Width()
    {
        using var clock = new TestClock();
        using var stage = new Stage(bar: new ProgressBar { IsIndeterminate = true, Width = 300, HorizontalAlignment = HorizontalAlignment.Left });
        var bar = stage.Bar;
        clock.At(1200);

        Assert.Equal(300, bar.Bounds.Width);
        Assert.Equal(GridAt(1.2, 300), GridMove(bar).X, 0.0002);
        Assert.Equal(MoveAt(0, 1.2, 300), Move(bar, 0).X, 1e-6);

        bar.Width = 200;
        stage.Layout();
        clock.At(1200 + FrameMs);
        Assert.Equal(GridAt(1.216, 200), GridMove(bar).X, 0.0002);
        Assert.Equal(MoveAt(0, 1.216, 200), Move(bar, 0).X, 1e-6);
    }

    // Determinate again, the parts get their own values back and no frame is pending; indeterminate again starts at 0.
    [AvaloniaFact]
    public void Going_Determinate_Stops_It_And_Indeterminate_Starts_It_Again()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var bar = stage.Bar;
        bar.IsIndeterminate = true;
        clock.At(40 * FrameMs);
        Assert.Equal(1, Dot(bar, 1).Opacity);
        Assert.NotEqual(0, Move(bar, 0).X);

        bar.IsIndeterminate = false;
        Assert.Equal(0, clock.Pending);
        Dispatcher.UIThread.RunJobs();                          // the opacities go back after the dispatcher's pass
        Assert.Equal(0, GridMove(bar).X);
        for (var dot = 0; dot < 5; dot++)
        {
            Assert.Equal(1, Dot(bar, dot).Opacity);
            Assert.NotEqual(BindingPriority.Animation, Dot(bar, dot).GetDiagnostic(Visual.OpacityProperty).Priority);
            Assert.Equal(0, Slide(bar, dot).X);
            Assert.Equal(0, Move(bar, dot).X);
        }

        clock.At(100 * FrameMs);
        bar.IsIndeterminate = true;
        Assert.Equal(-63, GridMove(bar).X, 1e-9);
        Assert.Equal(-40, Slide(bar, 0).X, 1e-9);
        Assert.Equal(1, Dot(bar, 0).Opacity);
        Assert.Equal(0, Dot(bar, 1).Opacity);
    }

    // Out of the window, the storyboard stops; back in, it starts again from 0, as the phone's state does on load.
    [AvaloniaFact]
    public void Leaving_The_Window_Stops_It()
    {
        using var clock = new TestClock();
        var bar = new ProgressBar { IsIndeterminate = true };
        var holder = new Grid { Children = { bar } };
        using var scene = new Scene(holder);
        clock.At(30 * FrameMs);
        Assert.NotEqual(-63, GridMove(bar).X);

        holder.Children.Remove(bar);
        Assert.Equal(0, clock.Pending);
        holder.Children.Add(bar);
        Assert.Equal(-63, GridMove(bar).X, 1e-9);
    }

    // Hidden, it asks for no frame and sets nothing; the storyboard's time runs on, and shown again the parts are where
    // that time puts them within a check.
    [AvaloniaFact]
    public void Hidden_It_Asks_For_No_Frames()
    {
        using var clock = new TestClock();
        var bar = new ProgressBar { IsIndeterminate = true };
        var holder = new Grid { Children = { bar } };
        using var scene = new Scene(holder);
        var width = bar.Bounds.Width;
        clock.At(10 * FrameMs);
        var x = GridMove(bar).X;

        holder.IsVisible = false;
        clock.At(10 * FrameMs + 1000);
        Assert.Equal(x, GridMove(bar).X);
        Assert.Equal(1, clock.Pending);                          // the check, not a frame

        holder.IsVisible = true;
        var shown = 10 * FrameMs + 1000 + PhoneProgressBar.HiddenCheck.TotalMilliseconds + FrameMs;
        clock.At(shown);
        var nearest = Enumerable.Range(0, (int)FrameMs + 1)
            .Select(back => Math.Abs(GridAt(PhoneProgressBar.StoryboardTime(TimeSpan.FromMilliseconds(shown - back)).TotalSeconds, width) - GridMove(bar).X))
            .Min();
        Assert.True(nearest < 0.001, $"the grid is {nearest} from any time in the last frame");
    }

    // The storyboard turned on after the template is applied (a theme replacing another) still finds the parts.
    [AvaloniaFact]
    public void Turned_On_After_The_Template_It_Still_Runs()
    {
        using var clock = new TestClock();
        var bar = new ProgressBar { IsIndeterminate = true };
        PhoneProgressBar.SetIsAnimated(bar, false);
        using var stage = new Stage(bar: bar);
        Assert.Equal(0, GridMove(bar).X);

        bar.ClearValue(PhoneProgressBar.IsAnimatedProperty);
        Assert.Equal(-63, GridMove(bar).X, 1e-9);
        Assert.Equal(0, Dot(bar, 1).Opacity);
    }

    // Each part's own X, read when the parts are found, is what it gets back, not 0.
    [AvaloniaFact]
    public void A_Parts_Own_Value_Is_Given_Back()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var bar = stage.Bar;
        PhoneProgressBar.SetIsAnimated(bar, false);
        Dispatcher.UIThread.RunJobs();
        GridMove(bar).X = 5;
        Slide(bar, 1).X = 6;
        Move(bar, 2).X = 7;
        bar.ClearValue(PhoneProgressBar.IsAnimatedProperty);

        bar.IsIndeterminate = true;
        clock.At(12 * FrameMs);
        Assert.NotEqual(5, GridMove(bar).X);
        Assert.NotEqual(6, Slide(bar, 1).X);
        bar.IsIndeterminate = false;
        Assert.Equal(5, GridMove(bar).X);
        Assert.Equal(6, Slide(bar, 1).X);
        Assert.Equal(7, Move(bar, 2).X);
    }

    // Turned off and on again in one pass (a theme swapped for one based on it): the new storyboard reads the parts'
    // own values, not the old one's mid-animation values.
    [AvaloniaFact]
    public void Turned_Off_And_On_In_One_Pass_It_Keeps_The_Parts_Own_Values()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var bar = stage.Bar;
        bar.IsIndeterminate = true;
        clock.At(20 * FrameMs);
        Assert.NotEqual(0, Move(bar, 0).X);

        PhoneProgressBar.SetIsAnimated(bar, false);
        bar.ClearValue(PhoneProgressBar.IsAnimatedProperty);
        bar.IsIndeterminate = false;
        Assert.Equal(0, GridMove(bar).X);
        Assert.Equal(0, Move(bar, 0).X);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, GridMove(bar).X);
        Assert.Equal(0, Slide(bar, 0).X);
        Assert.Equal(0, Move(bar, 0).X);
        Assert.Equal(1, Dot(bar, 1).Opacity);
    }

    // A theme without the storyboard taking over: the old one stops after the theme's pass, and asks for no frames.
    [AvaloniaFact]
    public void Another_Theme_Taking_Over_Stops_It()
    {
        using var clock = new TestClock();
        using var stage = new Stage(bar: new ProgressBar { IsIndeterminate = true });
        var bar = stage.Bar;
        clock.At(15 * FrameMs);
        var dot = Dot(bar, 1);
        Assert.Equal(1, dot.Opacity);

        bar.Theme = new ControlTheme(typeof(ProgressBar))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate((_, scope) =>
                    new Panel { Children = { new Border { Name = "PART_Indicator" }.RegisterInNameScope(scope) } })),
            },
        };
        Dispatcher.UIThread.RunJobs();

        Assert.False(PhoneProgressBar.GetIsAnimated(bar));
        Assert.Equal(0, clock.Pending);
        Assert.NotEqual(BindingPriority.Animation, dot.GetDiagnostic(Visual.OpacityProperty).Priority);
    }

    // Not the phone's: Avalonia's Orientation. Vertical, the indicator fills from the bottom; turned, it fills across
    // again (Avalonia clears the other side's size each time).
    [AvaloniaFact]
    public void Vertical_The_Indicator_Fills_From_The_Bottom()
    {
        var bar = new ProgressBar { Value = Value, Orientation = Orientation.Vertical, Width = 10, Height = 200 };
        using var stage = new Stage(bar: bar);
        var indicator = Part<Border>(bar, "PART_Indicator");

        Assert.Equal(10, indicator.Bounds.Width, 0.001);
        Assert.Equal(80, indicator.Bounds.Height, 0.5);
        Assert.Equal(200, indicator.Bounds.Bottom, 0.5);

        bar.Orientation = Orientation.Horizontal;
        bar.Width = 300;
        bar.Height = double.NaN;
        stage.Layout();
        Assert.Equal(120, indicator.Bounds.Width, 0.5);
        Assert.Equal(bar.Bounds.Height, indicator.Bounds.Height, 0.001);

        bar.Orientation = Orientation.Vertical;
        bar.Width = 10;
        bar.Height = 200;
        stage.Layout();
        Assert.Equal(10, indicator.Bounds.Width, 0.001);
        Assert.Equal(80, indicator.Bounds.Height, 0.5);
    }

    // The phone's dots run only across: a vertical indeterminate bar shows no dots and runs no storyboard; turned
    // across, it runs.
    [AvaloniaFact]
    public void Vertical_No_Dots_Run()
    {
        using var clock = new TestClock();
        var bar = new ProgressBar { IsIndeterminate = true, Orientation = Orientation.Vertical, Width = 10, Height = 200 };
        using var stage = new Stage(bar: bar);
        clock.At(10 * FrameMs);

        Assert.Equal(0, Part<Grid>(bar, "PART_EllipseGrid").Opacity);
        Assert.Equal(0, clock.Pending);
        Assert.Equal(0, GridMove(bar).X);

        bar.Orientation = Orientation.Horizontal;
        Assert.Equal(1, Part<Grid>(bar, "PART_EllipseGrid").Opacity);
        Assert.Equal(-63, GridMove(bar).X, 1e-9);
        Assert.Equal(1, clock.Pending);
    }

    // The indicator stays inside an app's border: at 100% it fills the border's inside, not the whole bar.
    [AvaloniaFact]
    public void The_Indicator_Stays_Inside_The_Border()
    {
        var bar = new ProgressBar { Value = 100, BorderThickness = new Thickness(2), Height = 20 };
        using var stage = new Stage(bar: bar);
        var indicator = Part<Border>(bar, "PART_Indicator");

        Assert.Equal(bar.Bounds.Width - 4, indicator.Bounds.Width, 0.5);
        Assert.Equal(2, indicator.TranslatePoint(default, bar)!.Value.X, 0.5);
    }

    // The phone's bar has no text: Avalonia's ShowProgressText draws nothing.
    [AvaloniaFact]
    public void It_Shows_No_Text()
    {
        using var stage = new Stage(bar: new ProgressBar { Value = Value, ShowProgressText = true });
        Assert.Empty(stage.Bar.GetVisualDescendants().OfType<TextBlock>());
    }

    private static void AssertNear(Color expected, Color actual)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1 && Math.Abs(expected.B - actual.B) <= 1
                    && expected.A == actual.A, $"{actual} against {expected}");
    }

    private static byte[] Bytes(WriteableBitmap frame)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        return bytes;
    }
}
