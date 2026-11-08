using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 4: can an attached behaviour on a stock <c>Button</c> see where it was pressed, and when
/// to let go? <c>Button</c> marks the press handled. Driven with Avalonia.Headless mouse input, and
/// once with its headless touch input. <c>SimpleTheme</c> gives the button a template to hit.
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: the press reaches a handler on the button on the tunnel route
/// unhandled, then on the bubble route handled, which only <c>handledEventsToo</c> sees.
/// <c>GetPosition(button)</c> is the press point in the button's space. The pointer is captured by
/// the template part that was hit, so a release outside the button still arrives, and
/// <c>PointerCaptureLost</c> is raised directly on the captured element and on each of its
/// ancestors, the button included, after a release or when capture is taken away. Leaving the
/// button raises <c>PointerExited</c> but keeps capture.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class TiltPressPointTests
{
    // The button sits at (50, 30) in the window, 200 × 100.
    private static readonly Point Origin = new(50, 30);

    /// <summary>The behaviour under test: what the tilt would subscribe to, recording what it sees.</summary>
    private sealed class TiltProbe
    {
        public List<string> Seen { get; } = [];

        public TiltProbe(Button button, RoutingStrategies routes, bool handledEventsToo)
        {
            button.AddHandler(InputElement.PointerPressedEvent,
                (_, e) => Seen.Add($"pressed {e.Route} handled={e.Handled} at {e.GetPosition(button)}"),
                routes, handledEventsToo);
            button.AddHandler(InputElement.PointerReleasedEvent,
                (_, e) => Seen.Add($"released {e.Route} at {e.GetPosition(button)}"),
                routes, handledEventsToo);
            button.AddHandler(InputElement.PointerCaptureLostEvent,
                (_, e) => Seen.Add($"capture lost {e.Route}"),
                RoutingStrategies.Direct, handledEventsToo);
            button.AddHandler(InputElement.PointerExitedEvent,
                (_, e) => Seen.Add($"exited {e.Route}"),
                RoutingStrategies.Direct, handledEventsToo);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly IDisposable _theme = Spike.InApplication(new SimpleTheme());
        private readonly SpikeWindow _scene;

        public Button Button { get; } = new()
        {
            Content = "press",
            Width = 200,
            Height = 100,
            Margin = new Thickness(Origin.X, Origin.Y, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        public Window Window => _scene.Window;

        public Fixture() => _scene = new SpikeWindow(Button);

        public void Dispose()
        {
            _scene.Dispose();
            _theme.Dispose();
        }
    }

    private const RoutingStrategies Both = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;

    [AvaloniaFact]
    public void The_Press_Reaches_Both_Routes_With_HandledEventsToo()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, Both, handledEventsToo: true);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);

        Assert.Equal(["pressed Tunnel handled=False at 30, 20", "pressed Bubble handled=True at 30, 20"], probe.Seen);
        Assert.True(f.Button.IsPressed);
    }

    // Doesn't work: Button handles the press, so without handledEventsToo the bubble never arrives.
    [AvaloniaFact]
    public void Without_HandledEventsToo_The_Bubble_Misses_The_Press()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, Both, handledEventsToo: false);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);

        Assert.Equal(["pressed Tunnel handled=False at 30, 20"], probe.Seen);
    }

    [AvaloniaFact]
    public void GetPosition_Gives_The_Press_Point_In_The_Buttons_Space()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, RoutingStrategies.Tunnel, handledEventsToo: true);

        f.Window.MouseDown(Origin + new Vector(170, 90), MouseButton.Left);
        f.Window.MouseUp(Origin + new Vector(170, 90), MouseButton.Left);
        f.Window.MouseDown(Origin + new Vector(1, 2), MouseButton.Left);

        Assert.Contains("pressed Tunnel handled=False at 170, 90", probe.Seen);
        Assert.Contains("pressed Tunnel handled=False at 1, 2", probe.Seen);
    }

    [AvaloniaFact]
    public void The_Pointer_Is_Captured_By_The_Template_Part_That_Was_Hit_Not_The_Button()
    {
        using var f = new Fixture();
        IInputElement? captured = null;
        f.Button.AddHandler(InputElement.PointerPressedEvent, (_, e) => captured = e.Pointer.Captured,
            RoutingStrategies.Bubble, handledEventsToo: true);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);

        Assert.NotNull(captured);
        Assert.NotSame(f.Button, captured);
        Assert.Same(f.Button, ((Visual)captured).GetSelfAndVisualAncestors().OfType<Button>().First());
    }

    [AvaloniaFact]
    public void Leaving_And_Releasing_Outside_Still_Releases_Then_Loses_Capture()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, RoutingStrategies.Tunnel, handledEventsToo: true);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);
        f.Window.MouseMove(new Point(350, 350));
        f.Window.MouseUp(new Point(350, 350), MouseButton.Left);

        Assert.Equal(
            ["pressed Tunnel handled=False at 30, 20", "exited Direct", "released Tunnel at 300, 320", "capture lost Direct"],
            probe.Seen);
        Assert.False(f.Button.IsPressed);
    }

    [AvaloniaFact]
    public void Capture_Taken_Mid_Press_Raises_CaptureLost_On_The_Button()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, RoutingStrategies.Tunnel, handledEventsToo: true);
        IPointer? pointer = null;
        f.Button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);
        pointer!.Capture(null);                                    // as a gesture recogniser taking the pointer would

        Assert.Equal(["pressed Tunnel handled=False at 30, 20", "capture lost Direct"], probe.Seen);
        Assert.False(f.Button.IsPressed);
    }

    // Tunnel alone is enough for press and release on a stock Button: neither is handled on the way down.
    [AvaloniaFact]
    public void Tunnel_Alone_Without_HandledEventsToo_Sees_Press_Release_And_Capture_Lost()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, RoutingStrategies.Tunnel, handledEventsToo: false);

        f.Window.MouseDown(Origin + new Vector(30, 20), MouseButton.Left);
        f.Window.MouseUp(Origin + new Vector(30, 20), MouseButton.Left);

        Assert.Equal(["pressed Tunnel handled=False at 30, 20", "released Tunnel at 30, 20", "capture lost Direct"], probe.Seen);
    }

    [AvaloniaFact]
    public void Headless_Touch_Behaves_Like_The_Mouse()
    {
        using var f = new Fixture();
        var probe = new TiltProbe(f.Button, Both, handledEventsToo: true);

        var touch = f.Window.TouchBegin(Origin + new Vector(30, 20));
        f.Window.TouchEnd(touch, Origin + new Vector(30, 20));

        Assert.Equal(
            ["pressed Tunnel handled=False at 30, 20", "pressed Bubble handled=True at 30, 20",
             "released Tunnel at 30, 20", "released Bubble at 30, 20", "capture lost Direct"],
            probe.Seen.Where(s => !s.StartsWith("exited")));
    }
}
