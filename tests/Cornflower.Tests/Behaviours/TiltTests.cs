using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Behaviours.TiltReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Behaviours;

/// <summary>
/// The tilt on stock Buttons laid out as the reference app's probes laid them out, on the reference
/// profile, pressed with headless touch and mouse input, against the emulator's readings
/// (<see cref="TiltReadings"/>). Time is a <see cref="TestClock"/>: a frame is 16 ms.
/// </summary>
public class TiltTests
{
    /// <summary>
    /// How near a settled angle has to be, in degrees: 1.5 times the model's worst over the 200
    /// grid presses of both profiles (0.026°). The readings are logged to 0.001°; without the bias
    /// (#60's curve, the bias taken out) the readings are out by up to 1.26°.
    /// </summary>
    private const double AngleTolerance = 0.04;

    /// <summary>How near a settled depth has to be, in view pixels: 1.5 times the model's worst over both grids (0.056).</summary>
    private const double DepthTolerance = 0.085;

    /// <summary>Long enough for any press to settle: it takes four or five frames.</summary>
    private const double SettleMs = 160;

    private sealed class Page : IDisposable
    {
        private readonly Scene _scene;

        public Page(params Layout[] layouts)
            : this(layouts, theme: null)
        {
        }

        /// <param name="layouts">The Buttons, as the probe laid them out.</param>
        /// <param name="theme">The Buttons' theme, if not the scene's.</param>
        /// <param name="width">The view: another profile's.</param>
        /// <param name="height">The view: another profile's.</param>
        /// <param name="scaling">Raw pixels per view pixel: another profile's.</param>
        public Page(Layout[] layouts, ControlTheme? theme, double width = Scene.ReferenceWidth,
            double height = Scene.ReferenceHeight, double scaling = Scene.ReferenceScaling)
        {
            var canvas = new Canvas();
            foreach (var layout in layouts)
            {
                var button = new Button { Content = layout.Name, Width = layout.Width, Height = layout.Height };
                if (theme is not null) button.Theme = theme;
                Canvas.SetLeft(button, layout.Left);
                Canvas.SetTop(button, layout.Top);
                CfTilt.SetIsEnabled(button, true);
                canvas.Children.Add(button);
                Buttons[layout.Name] = button;
                Layouts[layout.Name] = layout;
            }

            // Simple's Button, whose template root is what tilts: these tests are about the behaviour, not Wp81's Button.
            _scene = new Scene(canvas, width: width, height: height, scaling: scaling, themes: [new SimpleTheme()]);
        }

        public Dictionary<string, Button> Buttons { get; } = [];

        public Dictionary<string, Layout> Layouts { get; } = [];

        public Window Window => _scene.Window;

        public Point At(string button, Point inButton) => Layouts[button].Bounds.TopLeft + inButton;

        public void Dispose() => _scene.Dispose();
    }

    private static string Describe(TiltState s) =>
        string.Create(CultureInfo.InvariantCulture, $"({s.RotationX:F3}, {s.RotationY:F3}, {s.OffsetZ:F3})");

    private static void AssertNear(TiltState expected, TiltState actual, double angle = AngleTolerance, double depth = DepthTolerance)
    {
        Assert.True(
            Math.Abs(expected.RotationX - actual.RotationX) <= angle &&
            Math.Abs(expected.RotationY - actual.RotationY) <= angle &&
            Math.Abs(expected.OffsetZ - actual.OffsetZ) <= depth,
            $"expected {Describe(expected)}, got {Describe(actual)}");
    }

    /// <summary>Presses every grid point of one button with a touch, lets each settle, and compares it with the reading.</summary>
    [AvaloniaTheory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("D")]
    public void A_Held_Press_Settles_At_The_Reading_For_Its_Point(string name)
    {
        using var page = new Page(GridButtons);
        AssertGrid(page, name, GridPresses);
    }

    // The 720p profile: a smaller view at another scaling, and the same eye, in view pixels, fits
    // it as well as the reference profile (worst 0.021°, rms 0.0075°).
    [AvaloniaTheory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("D")]
    public void On_The_720p_Profile_A_Held_Press_Settles_At_The_Reading_For_Its_Point(string name)
    {
        using var page = new Page(GridButtons720, theme: null, width: 400, height: 711.111, scaling: 1.8);
        AssertGrid(page, name, GridPresses720);
    }

    private static void AssertGrid(Page page, string name, Press[] presses)
    {
        using var clock = new TestClock();
        var button = page.Buttons[name];
        var misses = new List<string>();
        var time = 0.0;

        foreach (var press in presses.Where(p => p.Button == name))
        {
            var at = page.At(name, press.Point);
            var touch = page.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            var got = CfTilt.Current(button);
            var expected = new TiltState(press.RotationX, press.RotationY, press.OffsetZ);
            if (Math.Abs(expected.RotationX - got.RotationX) > AngleTolerance ||
                Math.Abs(expected.RotationY - got.RotationY) > AngleTolerance ||
                Math.Abs(expected.OffsetZ - got.OffsetZ) > DepthTolerance)
                misses.Add($"at {press.X}, {press.Y}: read {Describe(expected)}, got {Describe(got)}");

            page.Window.TouchEnd(touch, at);
            clock.At(time += 1000);
            Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        }

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    // The first frame shows the toolkit's rule for the point as it is: run 2118's first frame, 31 ms
    // after the press, is 0.016°, -24.977. The next frames step through the tilt.
    [AvaloniaFact]
    public void A_Press_Jumps_Then_Settles_Frame_By_Frame_As_The_Phone_Does()
    {
        using var clock = new TestClock();
        using var page = new Page(FirstButton);
        var button = page.Buttons[FirstButton.Name];

        page.Window.TouchBegin(page.At(FirstButton.Name, new Point(150, 60)));

        for (var frame = 0; frame < CentrePressFrames.Length; frame++)
        {
            clock.At(frame * CornflowerClock.FrameInterval.TotalMilliseconds);
            var (rotationX, offsetZ) = CentrePressFrames[frame];
            AssertNear(new TiltState(rotationX, 0, offsetZ), CfTilt.Current(button), angle: 0.02, depth: 0.04);
        }
    }

    [AvaloniaFact]
    public void A_Release_Holds_For_180_ms_Then_Eases_Back_Over_195_ms()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["A"];
        var corner = page.At("A", new Point(5.848, 3.344));

        var touch = page.Window.TouchBegin(corner);
        clock.At(SettleMs);
        var held = CfTilt.Current(button);
        AssertNear(new TiltState(-8.527, 8.233, -0.623), held);

        page.Window.TouchEnd(touch, corner);
        var released = clock.Now.TotalMilliseconds;

        Assert.Equal(held, CfTilt.Current(button));
        clock.At(released + 176);                                   // eleven frames on: still held
        Assert.Equal(held, CfTilt.Current(button));

        // The return's frames are 16 ms apart from the end of the hold. Six frames in, it is 96 ms
        // of 195 along a sine ease-out: 1 - sin(96 / 195 × π / 2) of the held tilt is left.
        clock.At(released + 180 + 96);
        AssertNear(held.Scale(1 - Math.Sin(96.0 / 195 * Math.PI / 2)), CfTilt.Current(button), angle: 1e-9, depth: 1e-9);

        clock.At(released + 180 + 192);                             // 3 ms short of the end: all but gone
        Assert.InRange(Math.Abs(CfTilt.Current(button).RotationX), 1e-6, 0.01);

        clock.At(released + 180 + 208);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(CfTilt.TargetOf(button).RenderTransform);
        Assert.Equal(0, clock.Pending);
    }

    // Run 2118 dragged a press down from the centre: the tilt followed the finger. Its settled
    // values on the way, at 70.033, 89.967 and 114.164 down the button.
    [AvaloniaFact]
    public void A_Move_While_Pressed_Follows_The_Finger()
    {
        using var clock = new TestClock();
        using var page = new Page(FirstButton);
        var button = page.Buttons[FirstButton.Name];
        var name = FirstButton.Name;

        var touch = page.Window.TouchBegin(page.At(name, new Point(150, 60)));
        clock.At(SettleMs);
        AssertNear(new TiltState(-0.377, 0, -24.452), CfTilt.Current(button));

        var time = SettleMs;
        foreach (var (y, rotationX, offsetZ) in DragFrames)
        {
            page.Window.TouchMove(touch, page.At(name, new Point(150, y)));
            clock.At(time += SettleMs);
            AssertNear(new TiltState(rotationX, 0, offsetZ), CfTilt.Current(button));
        }
    }

    // Moves come faster than frames: one before the press has settled still decides where it settles.
    [AvaloniaFact]
    public void A_Move_Before_The_Press_Settles_Is_Followed()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];

        var touch = page.Window.TouchBegin(page.At("B", new Point(149.98, 60.033)));
        clock.At(8);                                                // half a frame: the next step is pending
        page.Window.TouchMove(touch, page.At("B", new Point(14.98, 5.803)));
        clock.At(SettleMs);

        AssertNear(new TiltState(-8.504, 8.186, -0.726), CfTilt.Current(button));
    }

    // Run 2118: dragged out of the button, the tilt stayed as it was at the edge and stopped
    // following, then went back 182 ms after the finger left, as from a release.
    [AvaloniaFact]
    public void Leaving_The_Control_Holds_Then_Returns_And_Stops_Following()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];

        var touch = page.Window.TouchBegin(page.At("B", new Point(150, 100)));
        clock.At(SettleMs);
        var held = CfTilt.Current(button);

        page.Window.TouchMove(touch, page.At("B", new Point(150, 120.852)));
        var left = clock.Now.TotalMilliseconds;
        clock.At(left + 100);
        page.Window.TouchMove(touch, page.At("B", new Point(150, 200)));    // further out: nothing follows
        clock.At(left + 176);
        Assert.Equal(held, CfTilt.Current(button));

        clock.At(left + 400);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(CfTilt.TargetOf(button).RenderTransform);
        page.Window.TouchEnd(touch, page.At("B", new Point(150, 200)));
        Assert.Equal(0, clock.Pending);
    }

    // Not measured on 8.1: the toolkit's TiltEffect, whose rule and constants 8.1's are, tilts again
    // when the finger comes back onto the element. So does this.
    [AvaloniaFact]
    public void Coming_Back_Onto_The_Control_Tilts_Again()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];

        var touch = page.Window.TouchBegin(page.At("B", new Point(150, 100)));
        clock.At(SettleMs);
        page.Window.TouchMove(touch, page.At("B", new Point(150, 200)));
        clock.At(SettleMs + 1000);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));

        page.Window.TouchMove(touch, page.At("B", new Point(14.98, 5.803)));
        clock.At(SettleMs + 1000 + SettleMs);
        AssertNear(new TiltState(-8.504, 8.186, -0.726), CfTilt.Current(button));
    }

    [AvaloniaFact]
    public void Losing_Capture_Mid_Press_Holds_Then_Returns()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["D"];
        IPointer? pointer = null;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);

        page.Window.MouseDown(page.At("D", new Point(10.017, 10)), MouseButton.Left);
        clock.At(SettleMs);
        var held = CfTilt.Current(button);
        AssertNear(new TiltState(-7.699, 8.375, -1.621), held);

        pointer!.Capture(null);                                     // as a gesture taking the pointer would
        var lost = clock.Now.TotalMilliseconds;
        clock.At(lost + 176);
        Assert.Equal(held, CfTilt.Current(button));
        clock.At(lost + 400);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
    }

    [AvaloniaFact]
    public void A_Press_During_The_Return_Tilts_Again_From_There()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["C"];
        var corner = page.At("C", new Point(20.804, 4.197));

        var touch = page.Window.TouchBegin(corner);
        clock.At(SettleMs);
        page.Window.TouchEnd(touch, corner);
        clock.At(SettleMs + 260);
        Assert.InRange(CfTilt.Current(button).RotationX, -7, -1);

        page.Window.TouchBegin(page.At("C", new Point(391.657, 75.803)));
        clock.At(SettleMs + 260 + SettleMs);
        AssertNear(new TiltState(8.032, -8.110, -1.522), CfTilt.Current(button));
        clock.At(SettleMs + 1000);
        AssertNear(new TiltState(8.032, -8.110, -1.522), CfTilt.Current(button));
    }

    // The pressed side goes away from the viewer, so it's drawn smaller. Pressed at its top left,
    // A's top edge is drawn shorter than its bottom edge, and its left edge than its right.
    [AvaloniaFact]
    public void The_Side_Pressed_Is_Drawn_Further_Away()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["A"];
        var root = CfTilt.TargetOf(button);
        var (w, h) = (root.Bounds.Width, root.Bounds.Height);

        page.Window.TouchBegin(page.At("A", new Point(5.848, 3.344)));
        clock.At(SettleMs);

        Assert.NotSame(button, root);                               // the template's root, as on the phone
        Point Drawn(double x, double y) => root.TranslatePoint(new Point(x, y), button)!.Value;
        double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        Point topLeft = Drawn(0, 0), topRight = Drawn(w, 0), bottomLeft = Drawn(0, h), bottomRight = Drawn(w, h);

        Assert.True(Distance(topLeft, topRight) < Distance(bottomLeft, bottomRight) - 0.5,
            $"top {Distance(topLeft, topRight)}, bottom {Distance(bottomLeft, bottomRight)}");
        Assert.True(Distance(topLeft, bottomLeft) < Distance(topRight, bottomRight) - 0.5,
            $"left {Distance(topLeft, bottomLeft)}, right {Distance(topRight, bottomRight)}");
    }

    // The step is what Avalonia's own hit testing does through the drawn transform: the point of the
    // root under the finger, by Avalonia's inverse of the matrix, is the point the model unprojects.
    [AvaloniaFact]
    public void Avalonias_Hit_Testing_Through_The_Tilt_Finds_The_Point_The_Model_Steps_From()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["A"];
        var inButton = new Point(30.002, 15.016);

        page.Window.TouchBegin(page.At("A", inButton));
        clock.At(SettleMs);

        var root = CfTilt.TargetOf(button);
        var seen = button.TranslatePoint(inButton, root)!.Value;
        var eye = TiltModel.Eye(root.Bounds.Size, new Point(225, 400) - (page.Layouts["A"].Bounds.TopLeft + root.Bounds.Center));
        var modelled = TiltModel.Unproject(CfTilt.Current(button), root.Bounds.Size, inButton - root.Bounds.Position, eye);

        // Avalonia transforms points in single precision.
        Assert.Equal(modelled.X, seen.X, 1e-4);
        Assert.Equal(modelled.Y, seen.Y, 1e-4);
    }

    // With the tilt on the template's root (Simple's Button), a tilted press off the edges clicks.
    // Within about 3 px of an edge it doesn't: see Run_0822_A_Tap_Just_Inside_Each_Edge_Clicks.
    [AvaloniaFact]
    public void A_Tilted_Press_Still_Clicks()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["A"];
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var corner = page.At("A", new Point(30.002, 15.016));

        var touch = page.Window.TouchBegin(corner);
        clock.At(SettleMs);
        page.Window.TouchEnd(touch, corner);

        Assert.Equal(1, clicks);
    }

    /// <summary>
    /// A Button template in the shape the tilt asks for: an untilted root that takes the hits,
    /// with the part that tilts inside it.
    /// </summary>
    private static ControlTheme HitLayerTheme() => new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((_, _) =>
            {
                var part = new Border { Name = "Tilted", Background = Brushes.Transparent };
                CfTilt.SetIsTarget(part, true);
                return new Grid { Background = Brushes.Transparent, Children = { part } };
            })),
        },
    };

    // Run 0822: on the phone, every tap at 0.84, 1.7 and 3.35 px inside an edge of a tilted Button
    // clicked. With the tilted part inside an untilted root, so does Avalonia's.
    [AvaloniaFact]
    public void Run_0822_A_Tap_Just_Inside_Each_Edge_Clicks()
    {
        using var clock = new TestClock();
        using var page = new Page([FirstButton], HitLayerTheme());
        var button = page.Buttons[FirstButton.Name];
        var clicked = new List<Point>();
        var time = 0.0;

        foreach (var tap in EdgeTaps)
        {
            var clicks = 0;
            void Count(object? sender, RoutedEventArgs e) => clicks++;
            button.Click += Count;
            var at = page.At(FirstButton.Name, tap);
            var touch = page.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
            page.Window.TouchEnd(touch, at);
            button.Click -= Count;
            if (clicks == 1) clicked.Add(tap);
            clock.At(time += 1000);
        }

        Assert.Equal(EdgeTaps, clicked);
    }

    // Why IsTarget exists: Avalonia hit-tests through the tilt, so with Simple's Button, whose
    // template root is what tilts, run 0822's taps 0.84 px inside the top, left and right edges
    // don't click. (The bottom one does: the eye is below this button, and the bottom edge, sunk,
    // is drawn towards it, under the finger.)
    [AvaloniaFact]
    public void With_The_Root_Tilted_Avalonia_Misses_A_Tap_Just_Inside_An_Edge()
    {
        using var clock = new TestClock();
        using var page = new Page(FirstButton);
        var button = page.Buttons[FirstButton.Name];
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var time = 0.0;

        foreach (var tap in new Point[] { new(150, 0.852), new(0.838, 60), new(299.162, 60) })
        {
            var at = page.At(FirstButton.Name, tap);
            var touch = page.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            page.Window.TouchEnd(touch, at);
            clock.At(time += 1000);
        }

        Assert.Equal(0, clicks);
    }

    // The part marked IsTarget tilts, and its root doesn't; without a mark, the root tilts.
    [AvaloniaFact]
    public void The_Part_Marked_IsTarget_Is_What_Tilts()
    {
        using var clock = new TestClock();
        using var page = new Page([FirstButton], HitLayerTheme());
        var button = page.Buttons[FirstButton.Name];
        var root = (Control)button.GetVisualChildren().Single();
        var part = (Control)root.GetVisualChildren().Single();

        page.Window.TouchBegin(page.At(FirstButton.Name, new Point(30, 20)));
        clock.At(SettleMs);

        Assert.Same(part, CfTilt.TargetOf(button));
        Assert.IsType<MatrixTransform>(part.RenderTransform);
        Assert.Null(root.RenderTransform);
    }

    // A mark in the content is the content's business, not the control's template.
    [AvaloniaFact]
    public void IsTarget_In_The_Content_Doesnt_Move_The_Tilt()
    {
        using var page = new Page(FirstButton);
        var button = page.Buttons[FirstButton.Name];
        var inner = new Border();
        CfTilt.SetIsTarget(inner, true);
        button.Content = inner;
        Dispatcher.UIThread.RunJobs();

        Assert.Same(button.GetVisualChildren().Single(), CfTilt.TargetOf(button));
    }

    // The toolkit tilts only the nearest tiltable element: a tilting Button inside a tilting item
    // tilts, and the item doesn't.
    [AvaloniaFact]
    public void Only_The_Nearest_Tilting_Control_Tilts()
    {
        using var clock = new TestClock();
        var button = new Button { Content = "inner", Width = 200, Height = 80 };
        CfTilt.SetIsEnabled(button, true);
        var item = new ContentControl { Content = button, Padding = new Thickness(20) };
        CfTilt.SetIsEnabled(item, true);
        var canvas = new Canvas { Children = { item } };
        Canvas.SetLeft(item, 50);
        Canvas.SetTop(item, 100);
        using var scene = new Scene(canvas);

        scene.Window.TouchBegin(new Point(50 + 20 + 30, 100 + 20 + 20));
        clock.At(SettleMs);

        Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // A new template mid-press (a theme swapped, say) takes the captured part away without a
    // release: the tilt goes to rest, and the next press tilts the new root.
    [AvaloniaFact]
    public void A_New_Template_Mid_Press_Puts_It_At_Rest_And_The_Next_Press_Tilts()
    {
        using var clock = new TestClock();
        using var page = new Page([FirstButton], HitLayerTheme());
        var button = page.Buttons[FirstButton.Name];
        var at = page.At(FirstButton.Name, new Point(30, 20));

        page.Window.TouchBegin(at);
        clock.At(8);                                                // a step pending
        var oldPart = CfTilt.TargetOf(button);
        button.Theme = HitLayerTheme();
        Dispatcher.UIThread.RunJobs();
        clock.At(SettleMs);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(oldPart.RenderTransform);

        page.Window.TouchBegin(at);
        clock.At(2 * SettleMs);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
        Assert.NotSame(oldPart, CfTilt.TargetOf(button));
        Assert.IsType<MatrixTransform>(CfTilt.TargetOf(button).RenderTransform);
    }

    // The pointer is captured by what was hit. If that leaves the tree mid-press (content swapped),
    // Avalonia 12.1.4 hands the capture to its nearest ancestor still in the tree, inside the
    // control, so the release still arrives and the tilt returns.
    [AvaloniaFact]
    public void When_The_Captured_Element_Goes_Mid_Press_The_Release_Still_Arrives()
    {
        using var clock = new TestClock();
        using var page = new Page(FirstButton);
        var button = page.Buttons[FirstButton.Name];
        var content = new Border { Width = 300, Height = 120, Background = Brushes.Transparent };
        button.Content = content;
        button.Padding = default;
        Dispatcher.UIThread.RunJobs();
        IPointer? pointer = null;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer ??= e.Pointer, RoutingStrategies.Tunnel);
        var at = page.At(FirstButton.Name, new Point(30, 20));

        var touch = page.Window.TouchBegin(at);
        clock.At(SettleMs);
        Assert.Same(content, pointer!.Captured);
        button.Content = null;                                      // the captured element goes
        Dispatcher.UIThread.RunJobs();
        Assert.Same(CfTilt.TargetOf(button), pointer.Captured);

        page.Window.TouchEnd(touch, at);
        clock.At(SettleMs + 1000);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
    }

    [AvaloniaFact]
    public void The_Controls_Own_Transform_Is_Left_Alone_And_The_Roots_Put_Back()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];
        var own = new ScaleTransform(1, 1);
        button.RenderTransform = own;
        var root = CfTilt.TargetOf(button);
        var rootOwn = new TranslateTransform(0, 0);
        root.RenderTransform = rootOwn;
        var at = page.At("B", new Point(75, 30));

        var touch = page.Window.TouchBegin(at);
        clock.At(SettleMs);
        Assert.IsType<MatrixTransform>(root.RenderTransform);
        Assert.Same(own, button.RenderTransform);

        page.Window.TouchEnd(touch, at);
        clock.At(SettleMs + 1000);
        Assert.Same(rootOwn, root.RenderTransform);
        Assert.Same(own, button.RenderTransform);
    }

    // The tilt's transform goes on above the root's own value, styled or local, and comes off
    // without leaving a local value behind: a style still sets the root's transform afterwards.
    [AvaloniaFact]
    public void A_Styled_Transform_On_The_Root_Still_Follows_Its_Style()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];
        var styled = new ScaleTransform(1, 1);
        var style = new Style(x => x.OfType<Button>().Template().OfType<Avalonia.Controls.Presenters.ContentPresenter>())
        {
            Setters = { new Setter(Visual.RenderTransformProperty, styled) },
        };
        page.Window.Styles.Add(style);
        Dispatcher.UIThread.RunJobs();
        var root = CfTilt.TargetOf(button);
        Assert.Same(styled, root.RenderTransform);
        var at = page.At("B", new Point(75, 30));

        var touch = page.Window.TouchBegin(at);
        clock.At(SettleMs);
        Assert.IsType<MatrixTransform>(root.RenderTransform);
        page.Window.TouchEnd(touch, at);
        clock.At(SettleMs + 1000);
        Assert.Same(styled, root.RenderTransform);
        Assert.Equal(BindingPriority.Style, root.GetDiagnostic(Visual.RenderTransformProperty).Priority);
    }

    [AvaloniaFact]
    public void Without_IsEnabled_Nothing_Tilts()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];
        CfTilt.SetIsEnabled(button, false);

        page.Window.TouchBegin(page.At("B", new Point(75, 30)));
        clock.At(SettleMs);

        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(CfTilt.TargetOf(button).RenderTransform);
    }

    [AvaloniaFact]
    public void Turning_It_Off_Mid_Press_Puts_The_Control_At_Rest()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];

        page.Window.TouchBegin(page.At("B", new Point(75, 30)));
        clock.At(SettleMs);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));

        CfTilt.SetIsEnabled(button, false);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(CfTilt.TargetOf(button).RenderTransform);
        Assert.Equal(0, clock.Pending);
    }

    [AvaloniaFact]
    public void Leaving_The_Tree_Mid_Press_Puts_The_Control_At_Rest()
    {
        using var clock = new TestClock();
        using var page = new Page(GridButtons);
        var button = page.Buttons["B"];

        page.Window.TouchBegin(page.At("B", new Point(75, 30)));
        clock.At(SettleMs);
        var root = CfTilt.TargetOf(button);
        ((Canvas)button.GetVisualParent()!).Children.Remove(button);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(root.RenderTransform);
        Assert.Equal(0, clock.Pending);
    }
}
