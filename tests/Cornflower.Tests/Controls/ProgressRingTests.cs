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
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ProgressRingReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Cornflower's ProgressRing and its 8.1 theme (<c>Controls/ProgressRing.axaml</c>, <c>PhoneProgressRing</c>) against
/// the phone's style, the reference app's captures and probes, and the template's Active storyboard
/// (<see cref="ProgressRingReadings"/>). Time is a <see cref="TestClock"/>: a frame is 16 ms.
/// docs/controls/ProgressRing has the plan and the comparison.
/// </summary>
public class ProgressRingTests
{
    private const double FrameMs = 16;

    /// <summary>A ring on the reference app's stage: 450 × 120, the page's background, the ring in a slot 19 in and 12 down.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, ProgressRing? ring = null, bool slot = true)
        {
            Ring = ring ?? new ProgressRing();
            var holder = new Grid { Margin = slot ? Slot : default, Children = { Ring } };
            var stage = new Grid { Children = { holder } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: slot ? StageHeight : 800);
        }

        public ProgressRing Ring { get; }

        public Window Window => _scene.Window;

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        /// <summary>Lays the window out again, after a size change.</summary>
        public void Layout() => Window.UpdateLayout();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(ProgressRing ring, string name) where T : Control =>
        ring.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Ellipse Dot(ProgressRing ring, int i) => Part<Ellipse>(ring, $"PART_E{i + 1}");

    private static Canvas DotCanvas(ProgressRing ring, int i) => Part<Canvas>(ring, i == 5 ? "PART_SixthCircle" : $"PART_E{i + 1}R");

    private static RotateTransform Rotation(ProgressRing ring, int i) => Assert.IsType<RotateTransform>(DotCanvas(ring, i).RenderTransform);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    // The style's setters (progressring.json, themeresources.json's values), as the dump read them.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        var ring = stage.Ring;

        Assert.Equal(Background(variant == "Dark"), ColourOf(ring.Background));
        Assert.Equal(Accent, ColourOf(ring.Foreground));
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), ring.FontFamily);
        Assert.Equal(FontSize, ring.FontSize);
        Assert.Equal(MinSide, ring.MinWidth);
        Assert.Equal(MinSide, ring.MinHeight);
        Assert.False(ring.IsHitTestVisible);
        Assert.False(ring.IsTabStop);
        Assert.Equal(HorizontalAlignment.Stretch, ring.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, ring.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, ring.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, ring.VerticalContentAlignment);
    }

    // WinRT's defaults: inactive. The states are pseudoclasses in pairs, one of each pair always set.
    [AvaloniaFact]
    public void It_Starts_Inactive_And_Raises_Each_State_As_A_Pseudoclass()
    {
        using var stage = new Stage();
        var ring = stage.Ring;

        Assert.False(ring.IsActive);
        Assert.Equal(new[] { ":inactive", ":large" }, ring.Classes.Where(c => c.StartsWith(':')).Order());

        ring.IsActive = true;
        Assert.Equal(new[] { ":active", ":large" }, ring.Classes.Where(c => c.StartsWith(':')).Order());

        ring.Width = ring.Height = 60;
        stage.Layout();
        Assert.Equal(new[] { ":active", ":small" }, ring.Classes.Where(c => c.StartsWith(':')).Order());
    }

    // The dump's bounds: the ring stretched to its slot, and Ring 57.5 square in its middle, to the raw pixel.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new Stage();
        var ring = stage.Ring;
        var inStage = ring.TranslatePoint(default, (Visual)stage.Window.Content!)!.Value;
        var part = Part<Grid>(ring, "PART_Ring");
        var raw = 1 / Scene.ReferenceScaling;

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        // The slot's right edge: the phone rounds it to 989 raw pixels wide, Avalonia's margin rounding to 988.
        Assert.InRange(ring.Bounds.Width, InStage.Width - raw - 0.001, InStage.Width + 0.001);
        Assert.Equal(InStage.Height, ring.Bounds.Height, 0.001);
        Assert.Equal(RingBounds.Width, part.Bounds.Width, 0.001);
        Assert.Equal(RingBounds.Height, part.Bounds.Height, 0.001);
        // Centred: the leftover is odd in raw pixels both ways, and the phone and Avalonia may round its half apart.
        Assert.InRange(part.Bounds.X, RingBounds.X - raw - 0.001, RingBounds.X + raw + 0.001);
        Assert.InRange(part.Bounds.Y, RingBounds.Y - raw - 0.001, RingBounds.Y + raw + 0.001);
    }

    // The dots, at rest, where the dump has them: 7.5 in from Ring's top left, 6.5 across, hidden.
    [AvaloniaFact]
    public void Each_Dot_Rests_Where_The_Phones_Does()
    {
        using var stage = new Stage();
        var ring = stage.Ring;
        var part = Part<Grid>(ring, "PART_Ring");

        for (var i = 0; i < 6; i++)
        {
            var dot = Dot(ring, i);
            var at = dot.TranslatePoint(default, part)!.Value;
            Assert.Equal(DotInRing.X, at.X, 0.001);
            Assert.Equal(DotInRing.Y, at.Y, 0.001);
            Assert.Equal(DotInRing.Width, dot.Bounds.Width, 0.001);
            Assert.Equal(DotInRing.Height, dot.Bounds.Height, 0.001);
            Assert.Equal(0, dot.Opacity);
            Assert.Equal(Accent, ColourOf(dot.Fill));
            Assert.Equal(new Size(RingBounds.Width, RingBounds.Height), DotCanvas(ring, i).Bounds.Size);
        }
    }

    // A dot's canvas turns about Ring's centre, clockwise for a positive angle: the dump's E1, turned -0.5973°,
    // has the box the turned square gives.
    [AvaloniaFact]
    public void A_Dot_Turns_About_The_Rings_Centre()
    {
        using var stage = new Stage();
        var ring = stage.Ring;
        var part = Part<Grid>(ring, "PART_Ring");
        Rotation(ring, 0).Angle = TurnedAngle;
        stage.Layout();

        var dot = Dot(ring, 0);
        var box = new Rect(dot.Bounds.Size).TransformToAABB(dot.TransformToVisual(part)!.Value);

        Assert.Equal(TurnedDotInRing.X, box.X, 0.01);
        Assert.Equal(TurnedDotInRing.Y, box.Y, 0.01);
        Assert.Equal(TurnedDotInRing.Width, box.Width, 0.01);
        Assert.Equal(TurnedDotInRing.Height, box.Height, 0.01);
    }

    // The overlay: the ring's background at 0.8 while active (ProgressRingActiveBackgroundOpacity), else hidden.
    [AvaloniaFact]
    public void The_Overlay_Shows_Only_While_Active()
    {
        using var stage = new Stage();
        var overlay = Part<Border>(stage.Ring, "PART_BackgroundOverlay");

        Assert.Equal(0, overlay.Opacity);
        Assert.Equal(stage.Ring.Bounds.Size, overlay.Bounds.Size);
        stage.Ring.IsActive = true;
        Assert.Equal(ActiveOverlayOpacity, overlay.Opacity);
        stage.Ring.IsActive = false;
        Assert.Equal(0, overlay.Opacity);
    }

    // TemplateSettings as the API read them on device 11 (#72): the smaller side, a tenth, four tenths at the top.
    [AvaloniaFact]
    public void TemplateSettings_Are_The_Phones()
    {
        foreach (var (width, height, side, diameter, offsetTop) in Settings)
        {
            var ring = new ProgressRing
            {
                Width = width, Height = height,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            };
            using var stage = new Stage(ring: ring, slot: false);
            var settings = ring.TemplateSettings;

            Assert.Equal(side, settings.MaxSideLength, 0.01);
            Assert.Equal(diameter, settings.EllipseDiameter, 0.01);
            Assert.Equal(new Thickness(0, offsetTop, 0, 0), new Thickness(0, Math.Round(settings.EllipseOffset.Top, 2), 0, 0));
            Assert.Equal(0, settings.EllipseOffset.Left);
            Assert.Equal(0, settings.EllipseOffset.Right);
            Assert.Equal(0, settings.EllipseOffset.Bottom);
        }
    }

    // A ring at its default size, alone, is 57.5 square (MinWidth, MinHeight): the ring probe's default ring.
    [AvaloniaFact]
    public void Alone_It_Is_The_Phones_Default_Size_With_Five_Dots()
    {
        var ring = new ProgressRing { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var stage = new Stage(ring: ring, slot: false);

        Assert.Equal(new Size(MinSide, MinSide), ring.Bounds.Size);
        Assert.Contains(":small", ring.Classes);
        Assert.False(DotCanvas(ring, 5).IsVisible);
    }

    // SizeStates (-Probe ringsize): Large, with the sixth dot, when the smaller laid-out side is over 60; no hysteresis.
    [AvaloniaFact]
    public void It_Is_Large_When_Its_Smaller_Side_Is_Over_60()
    {
        var ring = new ProgressRing { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var stage = new Stage(ring: ring, slot: false);

        foreach (var (width, height, large) in SizeStates.Concat(SizeStates.Reverse()))
        {
            ring.Width = width;
            ring.Height = height;
            stage.Layout();

            var why = $"{width} x {height}, laid out {ring.Bounds.Width} x {ring.Bounds.Height}";
            Assert.True(large == ring.Classes.Contains(":large"), why);
            Assert.True(large != ring.Classes.Contains(":small"), why);
            Assert.True(large == DotCanvas(ring, 5).IsVisible, why);
            Assert.Equal(Math.Min(ring.Bounds.Width, ring.Bounds.Height), ring.TemplateSettings.MaxSideLength);
        }
    }

    // The angle tracks' curves: the theme's KeySpline gives the y the template's Bézier does at every x, so a spline
    // key is reached as on the phone. (Avalonia's own KeySpline is up to 0.0006 off it; the theme doesn't use it.)
    [Fact]
    public void A_KeySpline_Eases_As_The_Templates_Bezier_Does()
    {
        foreach (var s in Splines.OfType<double[]>())
        {
            var spline = new KeySplineCurve(s[0], s[1], s[2], s[3]);
            for (var x = 0.0; x <= 1.0; x += 0.01)
            {
                Assert.Equal(Bezier(s[0], s[1], s[2], s[3], x), spline.Ease(x), 1e-9);
            }
        }
    }

    // The storyboard's tracks, at and between their keys, against the template's keys worked out here.
    [Fact]
    public void The_Tracks_Give_The_Templates_Values()
    {
        for (var dot = 0; dot < 6; dot++)
        {
            for (var ms = 0; ms <= 4400; ms += 1)
            {
                var time = TimeSpan.FromMilliseconds(ms);
                var expectedAngle = AngleAt(dot, ms / 1000.0);
                var angle = PhoneProgressRing.Angles[dot].ValueAt(time);
                Assert.True(expectedAngle.HasValue == angle.HasValue, $"dot {dot + 1} at {ms} ms");
                if (expectedAngle is { } e)
                {
                    Assert.True(Math.Abs(e - angle!.Value) < 0.05, $"dot {dot + 1} at {ms} ms: {angle} against {e}");
                }

                Assert.Equal(OpacityAt(dot, ms / 1000.0), PhoneProgressRing.Opacities[dot].ValueAt(time));
            }

            for (var k = 0; k < AngleSeconds.Length; k++)
            {
                var at = PhoneKeyframeTrack.Seconds(BeginSeconds[dot] + AngleSeconds[k]);
                Assert.Equal(Angles[dot][k], PhoneProgressRing.Angles[dot].ValueAt(at)!.Value, 1e-9);
            }
        }

        Assert.Equal(PhoneKeyframeTrack.Seconds(CycleSeconds), PhoneProgressRing.Duration);
    }

    // The phone's own runtime, caught mid-storyboard by the captures: for each, one storyboard time at which the
    // theme's tracks give every shown dot's angle within 0.06° and show exactly those dots. Up to five dots at
    // once, so the begin times and the splines are the phone's, not only the template's text.
    [Fact]
    public void Each_Captured_Moment_Is_One_Time_In_The_Storyboard()
    {
        foreach (var (capture, shown) in Moments)
        {
            var best = (Ticks: -1L, Off: double.MaxValue);
            for (var ticks = 0L; ticks < PhoneProgressRing.Duration.Ticks; ticks += TimeSpan.TicksPerMillisecond / 20)
            {
                var time = TimeSpan.FromTicks(ticks);
                var off = 0.0;
                for (var dot = 0; dot < 6 && off < best.Off; dot++)
                {
                    var visible = PhoneProgressRing.Opacities[dot].ValueAt(time) == 1;
                    if (visible != dot < shown.Length)
                    {
                        off = double.MaxValue;
                    }
                    else if (visible)
                    {
                        var angle = PhoneProgressRing.Angles[dot].ValueAt(time)!.Value;
                        off = Math.Max(off, Math.Abs(((angle - shown[dot]) % 360 + 540) % 360 - 180));
                    }
                }

                if (off < best.Off)
                {
                    best = (ticks, off);
                }
            }

            Assert.True(best.Off < 0.06, $"{capture}: no storyboard time gives its angles; the nearest is {best.Off:0.###}° off at {best.Ticks / 1e4} ms");
        }
    }

    // A dot shows from its begin time to 3.22 s after, then hides, exactly at the key times.
    [Fact]
    public void A_Dot_Shows_From_Its_Begin_Until_3_22_Seconds_After()
    {
        for (var dot = 0; dot < 6; dot++)
        {
            var track = PhoneProgressRing.Opacities[dot];
            var begin = PhoneKeyframeTrack.Seconds(BeginSeconds[dot]);
            var hide = begin + PhoneKeyframeTrack.Seconds(ShowsUntilSeconds);
            Assert.Null(track.ValueAt(begin - TimeSpan.FromTicks(1)));
            Assert.Equal(1, track.ValueAt(begin));
            Assert.Equal(1, track.ValueAt(hide - TimeSpan.FromTicks(1)));
            Assert.Equal(0, track.ValueAt(hide));
            Assert.Equal(0, track.ValueAt(PhoneKeyframeTrack.Seconds(CycleSeconds) - TimeSpan.FromTicks(1)));
        }
    }

    // Running: every frame, each dot at the storyboard's time, from 0 when the ring goes active, repeating every
    // 4.305 s. The catalogue's ring is large, so all six dots draw.
    [AvaloniaFact]
    public void While_Active_Each_Frame_Shows_The_Storyboard_At_That_Time()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var ring = stage.Ring;
        const double start = 1000;
        clock.At(start);
        ring.IsActive = true;

        for (var ms = 0.0; ms <= 2 * CycleSeconds * 1000 + 200; ms += FrameMs)
        {
            clock.At(start + ms);
            var storyboard = (ms % (CycleSeconds * 1000)) / 1000;
            for (var dot = 0; dot < 6; dot++)
            {
                var why = $"dot {dot + 1}, {ms} ms after it went active";
                var angle = AngleAt(dot, storyboard);
                Assert.True(Math.Abs((angle ?? 0) - Rotation(ring, dot).Angle) < 0.05, $"{why}: {Rotation(ring, dot).Angle} against {angle}");
                Assert.True((OpacityAt(dot, storyboard) ?? 0) == Dot(ring, dot).Opacity, $"{why}: opacity {Dot(ring, dot).Opacity}");
            }
        }
    }

    // Inactive again, the dots get their own values back and no frame is pending; active again starts at 0.
    [AvaloniaFact]
    public void Going_Inactive_Stops_It_And_Going_Active_Starts_It_Again()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var ring = stage.Ring;
        ring.IsActive = true;
        clock.At(40 * FrameMs);
        Assert.Equal(1, Dot(ring, 1).Opacity);

        ring.IsActive = false;
        Assert.Equal(0, clock.Pending);
        Dispatcher.UIThread.RunJobs();                          // the opacities go back after the dispatcher's pass
        for (var dot = 0; dot < 6; dot++)
        {
            Assert.Equal(0, Dot(ring, dot).Opacity);
            Assert.Equal(0, Rotation(ring, dot).Angle);
        }

        clock.At(100 * FrameMs);
        ring.IsActive = true;
        Assert.Equal(1, Dot(ring, 0).Opacity);
        Assert.Equal(Angles[0][0], Rotation(ring, 0).Angle, 1e-9);
        Assert.Equal(0, Dot(ring, 1).Opacity);
    }

    // Out of the window, the storyboard stops; back in, it starts again from 0, as the phone's state does on load.
    [AvaloniaFact]
    public void Leaving_The_Window_Stops_It()
    {
        using var clock = new TestClock();
        var ring = new ProgressRing { IsActive = true };
        var holder = new Grid { Children = { ring } };
        using var scene = new Scene(holder);
        clock.At(30 * FrameMs);
        Assert.NotEqual(Angles[0][0], Rotation(ring, 0).Angle);

        holder.Children.Remove(ring);
        Assert.Equal(0, clock.Pending);
        holder.Children.Add(ring);
        Assert.Equal(Angles[0][0], Rotation(ring, 0).Angle, 1e-9);
    }

    // Drawn: at the storyboard's start, the first dot alone, in the accent, at -110°; the overlay over the page.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void At_The_Start_The_First_Dot_Alone_Is_Drawn(string variant)
    {
        using var clock = new TestClock();
        using var stage = new Stage(VariantOf(variant));
        var ring = stage.Ring;
        ring.IsActive = true;
        var part = Part<Grid>(ring, "PART_Ring");

        using var frame = stage.Capture();
        var centre = part.TranslatePoint(new Point(part.Bounds.Width / 2, part.Bounds.Height / 2), stage.Window)!.Value;
        // The dot's centre: (-18, -18) from Ring's centre (7.5 + 3.25 - 28.75), turned -110° clockwise.
        var angle = Angles[0][0] * Math.PI / 180;
        var dx = -18 * Math.Cos(angle) + 18 * Math.Sin(angle);
        var dy = -18 * Math.Sin(angle) - 18 * Math.Cos(angle);
        Assert.Equal(Accent, Pixel(frame, centre.X + dx, centre.Y + dy));
        // Where the fourth dot will be, -128° (18° on, 8 px away: the dots are 6.5 across), nothing yet: the page
        // under the overlay.
        var other = Angles[3][0] * Math.PI / 180;
        var page = Pixel(frame, centre.X - 18 * Math.Cos(other) + 18 * Math.Sin(other), centre.Y - 18 * Math.Sin(other) - 18 * Math.Cos(other));
        Assert.Equal(Background(variant == "Dark"), page);
    }

    // The content alignment places Ring as the phone's template binds it. Across, Left and Right put it at the
    // edges. Down, the template puts it in an Auto row between two star rows, so the row is Ring's height and centred
    // whatever the alignment, on the phone as here.
    [AvaloniaFact]
    public void The_Content_Alignment_Places_The_Ring_Across()
    {
        var ring = new ProgressRing { HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top };
        using var stage = new Stage(ring: ring);
        var part = Part<Grid>(ring, "PART_Ring");
        var down = (ring.Bounds.Height - RingBounds.Height) / 2;
        var raw = 1 / Scene.ReferenceScaling;

        var at = part.TranslatePoint(default, ring)!.Value;
        Assert.Equal(0, at.X, 0.001);
        Assert.InRange(at.Y, down - raw, down + raw);

        ring.HorizontalContentAlignment = HorizontalAlignment.Right;
        ring.VerticalContentAlignment = VerticalAlignment.Bottom;
        stage.Layout();
        at = part.TranslatePoint(default, ring)!.Value;
        Assert.Equal(ring.Bounds.Width - RingBounds.Width, at.X, 0.001);
        Assert.InRange(at.Y, down - raw, down + raw);
    }

    // Turned off and on again in one pass (a theme swapped for one based on it): the new storyboard reads the dots' own
    // angles, not the old one's mid-animation angles, and gives those back when it stops.
    [AvaloniaFact]
    public void Turned_Off_And_On_In_One_Pass_It_Keeps_The_Dots_Own_Angles()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var ring = stage.Ring;
        ring.IsActive = true;
        clock.At(20 * FrameMs);
        Assert.NotEqual(0, Rotation(ring, 1).Angle);

        PhoneProgressRing.SetIsAnimated(ring, false);
        ring.ClearValue(PhoneProgressRing.IsAnimatedProperty);
        ring.IsActive = false;
        Dispatcher.UIThread.RunJobs();

        for (var dot = 0; dot < 6; dot++)
        {
            Assert.Equal(0, Rotation(ring, dot).Angle);
            Assert.Equal(0, Dot(ring, dot).Opacity);
        }
    }

    // The vertical alignment is bound as the phone binds it, though the template's Auto row leaves Ring where it is.
    [AvaloniaFact]
    public void The_Vertical_Content_Alignment_Reaches_Ring()
    {
        using var stage = new Stage();
        var part = Part<Grid>(stage.Ring, "PART_Ring");

        Assert.Equal(VerticalAlignment.Center, part.VerticalAlignment);
        stage.Ring.VerticalContentAlignment = VerticalAlignment.Bottom;
        Assert.Equal(VerticalAlignment.Bottom, part.VerticalAlignment);
    }

    // The storyboard turned on after the template is applied (a theme replacing another) still finds the dots.
    [AvaloniaFact]
    public void Turned_On_After_The_Template_It_Still_Runs()
    {
        using var clock = new TestClock();
        var ring = new ProgressRing { IsActive = true };
        PhoneProgressRing.SetIsAnimated(ring, false);
        using var stage = new Stage(ring: ring);
        Assert.Equal(0, Dot(ring, 0).Opacity);

        ring.ClearValue(PhoneProgressRing.IsAnimatedProperty);
        Assert.Equal(1, Dot(ring, 0).Opacity);
        Assert.Equal(Angles[0][0], Rotation(ring, 0).Angle, 1e-9);
    }

    // A dot not begun yet, and every dot once stopped, show the template's own angle, not 0.
    [AvaloniaFact]
    public void A_Dots_Own_Angle_Is_Given_Back()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var ring = stage.Ring;
        ring.IsActive = false;
        ring.ApplyTemplate();
        // As a template would set it: the runner reads each transform's own angle when it finds the parts.
        PhoneProgressRing.SetIsAnimated(ring, false);
        Dispatcher.UIThread.RunJobs();
        Rotation(ring, 1).Angle = 30;
        ring.ClearValue(PhoneProgressRing.IsAnimatedProperty);

        ring.IsActive = true;
        Assert.Equal(30, Rotation(ring, 1).Angle);              // dot 2 begins at 0.167 s
        clock.At(12 * FrameMs);
        Assert.NotEqual(30, Rotation(ring, 1).Angle);
        ring.IsActive = false;
        Assert.Equal(30, Rotation(ring, 1).Angle);
    }

    // Hidden, it asks for no frame and sets nothing; the storyboard's time runs on, and shown again the dots are where
    // that time puts them within a check.
    [AvaloniaFact]
    public void Hidden_It_Asks_For_No_Frames()
    {
        using var clock = new TestClock();
        var ring = new ProgressRing { IsActive = true };
        var holder = new Grid { Children = { ring } };
        using var scene = new Scene(holder);
        clock.At(10 * FrameMs);
        var angle = Rotation(ring, 0).Angle;

        holder.IsVisible = false;
        clock.At(10 * FrameMs + 1000);
        Assert.Equal(angle, Rotation(ring, 0).Angle);
        Assert.Equal(1, clock.Pending);                          // the check, not a frame

        // Shown, the next check finds it and frames start again: by a check and a frame later, the dot is where
        // a time in the last frame puts it.
        holder.IsVisible = true;
        var shown = 10 * FrameMs + 1000 + PhoneProgressRing.HiddenCheck.TotalMilliseconds + FrameMs;
        clock.At(shown);
        var nearest = Enumerable.Range(0, (int)FrameMs + 1)
            .Select(back => Math.Abs(AngleAt(0, PhoneProgressRing.StoryboardTime(TimeSpan.FromMilliseconds(shown - back)).TotalSeconds)!.Value - Rotation(ring, 0).Angle))
            .Min();
        Assert.True(nearest < 0.05, $"the dot is {nearest}\u00b0 from any time in the last frame");
    }

    // A theme without the storyboard taking over: the old one stops after the theme's pass, and asks for no frames.
    [AvaloniaFact]
    public void Another_Theme_Taking_Over_Stops_It()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var ring = stage.Ring;
        ring.IsActive = true;
        clock.At(5 * FrameMs);
        var dot = Dot(ring, 0);
        Assert.Equal(1, dot.Opacity);

        ring.Theme = new ControlTheme(typeof(ProgressRing))
        {
            Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate((_, _) => new Panel())) },
        };
        Dispatcher.UIThread.RunJobs();

        Assert.False(PhoneProgressRing.GetIsAnimated(ring));
        Assert.Equal(0, clock.Pending);
        // The storyboard's value is given back: what's left is the old dot's own, not one set at animation priority.
        Assert.NotEqual(BindingPriority.Animation, dot.GetDiagnostic(Visual.OpacityProperty).Priority);
    }

    private static Color Pixel(Avalonia.Media.Imaging.WriteableBitmap frame, double x, double y) =>
        Frames.PixelAt(frame, (int)Math.Round(x * Scene.ReferenceScaling), (int)Math.Round(y * Scene.ReferenceScaling));
}
