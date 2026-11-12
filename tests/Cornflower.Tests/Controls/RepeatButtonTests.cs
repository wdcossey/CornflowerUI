using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ToggleButtonReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 RepeatButton (<c>Controls/RepeatButton.axaml</c>) against the phone's style, the reference app's captures
/// (<see cref="ToggleButtonReadings"/>) and the probe of run <c>20261112-0755-dev11</c>, which read its defaults through
/// the API and logged every click under a real touch. docs/controls/RepeatButton has the plan and the comparison.
/// </summary>
/// <remarks>
/// The repeats run on Avalonia's own <c>DispatcherTimer</c>, inside <c>RepeatButton</c>, which runs on real time: no
/// <see cref="TestClock"/> reaches it. So the timing is held by the values the theme sets (<c>Delay</c> 500,
/// <c>Interval</c> 33, <c>ClickMode</c> Press, each the phone's reading) and by what real time can't make flaky: no
/// repeat before <c>Delay</c> has passed, none closer than <c>Interval</c>, and the repeats come. A slow machine makes
/// them late, never early.
/// </remarks>
public class RepeatButtonTests
{
    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static RepeatButton Repeat(bool enabled = true) => new() { Content = "repeat button", IsEnabled = enabled };

    private static Border BorderOf(RepeatButton button) => ButtonFamily.Part<Border>(button, "Border");

    private static Color Fill(RepeatButton button) => ButtonFamily.ColourOf(BorderOf(button).Background);

    /// <summary>
    /// Runs the dispatcher's own loop on real time until <paramref name="done"/> or <paramref name="limitMs"/> has
    /// passed. <c>RunJobs</c> runs only what is queued: a <c>DispatcherTimer</c> comes due only in the loop.
    /// </summary>
    private static void RunFor(double limitMs, Func<bool>? done = null)
    {
        var frame = new DispatcherFrame();
        var watch = Stopwatch.StartNew();
        var poll = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Background, (_, _) =>
        {
            if (watch.Elapsed.TotalMilliseconds >= limitMs || done?.Invoke() == true)
                frame.Continue = false;
        });
        poll.Start();
        Dispatcher.UIThread.PushFrame(frame);
        poll.Stop();
    }

    // repeatbutton.json's twelve setters, the content alignment WinRT has by default, the overhang and the padding; and
    // the probe's reading of what the style doesn't set: ClickMode Press, Delay 500, Interval 33.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat(), ButtonFamily.VariantOf(variant));
        var button = stage.Button;

        ButtonFamily.AssertButtonSetters(button, variant == "Dark", stage.Window);
        Assert.Equal(ButtonReadings.Overhang, BorderOf(button).Margin);
        Assert.Equal(ClickMode.Press, button.ClickMode);
        Assert.Equal(RepeatDelay, button.Delay);
        Assert.Equal(RepeatInterval, button.Interval);
    }

    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        ButtonFamily.AssertBounds(stage.Button, BorderOf(stage.Button), stage.Window);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Border_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ButtonStage<RepeatButton>(Repeat(), ButtonFamily.VariantOf(variant));
        using var frame = stage.Capture();

        ButtonFamily.AssertBorder(frame, ButtonReadings.Foreground(dark), ButtonReadings.Page(dark), ButtonReadings.Page(dark));
    }

    // CommonStates.Pressed: the accent behind the text over it, the border unchanged; back on release.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Pressed_Fills_With_The_Accent_And_Draws_The_Text_Over_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ButtonStage<RepeatButton>(Repeat(), ButtonFamily.VariantOf(variant));
        var button = stage.Button;

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Accent, Fill(button));
        Assert.Equal(ButtonReadings.OverAccent, ButtonFamily.ColourOf(ButtonFamily.PresenterOf(button).Foreground));
        using (var frame = stage.Capture())
            ButtonFamily.AssertBorder(frame, ButtonReadings.Foreground(dark), ButtonReadings.Page(dark), ButtonReadings.Accent);

        stage.Window.MouseUp(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Transparent, Fill(button));
        Assert.Equal(ButtonReadings.Foreground(dark), ButtonFamily.ColourOf(ButtonFamily.PresenterOf(button).Foreground));
    }

    // CommonStates.Disabled: the border and the text in PhoneBaseLowColor, and the app's background taken away.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does(string variant)
    {
        var dark = variant == "Dark";
        var button = Repeat(enabled: false);
        button.Background = Brushes.Red;
        using var stage = new ButtonStage<RepeatButton>(button, ButtonFamily.VariantOf(variant));

        Assert.Equal(ButtonReadings.Disabled(dark), ButtonFamily.ColourOf(BorderOf(button).BorderBrush));
        Assert.Equal(ButtonReadings.Transparent, Fill(button));
        Assert.Equal(ButtonReadings.Disabled(dark), ButtonFamily.ColourOf(ButtonFamily.PresenterOf(button).Foreground));
        using var frame = stage.Capture();
        ButtonFamily.AssertBorder(frame, ButtonReadings.DisabledInk(dark), ButtonReadings.Page(dark), ButtonReadings.Page(dark));
    }

    [AvaloniaFact]
    public void The_Pointer_Over_It_Changes_Nothing()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        stage.Window.MouseMove(stage.Centre);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsPointerOver);
        Assert.Equal(ButtonReadings.Transparent, Fill(stage.Button));
    }

    // The phone's RepeatButton runs no theme animation: its forced Pressed capture is flat, where Button's is tilted.
    [AvaloniaFact]
    public void A_Press_Doesnt_Tilt()
    {
        using var clock = new TestClock();
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(160);

        Assert.False(CfTilt.GetIsEnabled(stage.Button));
        Assert.All(stage.Button.GetVisualDescendants().OfType<Avalonia.Visual>(), v => Assert.Null(v.RenderTransform));
    }

    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsFocused, "Tab didn't reach the RepeatButton");
        var layer = AdornerLayer.GetAdornerLayer(stage.Button);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // ClickMode Press (run 0755: the click is logged before the press itself): a tap clicks once, on the press, and the
    // release doesn't click again. The overhang takes the press.
    [AvaloniaFact]
    public void A_Tap_Clicks_Once_On_The_Press_The_Overhang_Too()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var clicks = 0;
        stage.Button.Click += (_, _) => clicks++;
        var above = stage.At(54, 4);

        stage.Window.MouseDown(above, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        stage.Window.MouseUp(above, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    // Held: a click on the press, the first repeat no sooner than Delay (500) after it, and the next no closer than
    // Interval (33) apart (run 0755: 519 ms, then about every 60). Real time, so only the bounds a slow machine can't
    // break are asserted, and that the repeats come.
    [AvaloniaFact]
    public void A_Held_Press_Repeats_After_The_Delay_At_The_Interval()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var times = new List<double>();
        var watch = new Stopwatch();
        stage.Button.Click += (_, _) => times.Add(watch.Elapsed.TotalMilliseconds);

        watch.Start();
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        RunFor(10_000, () => times.Count >= 4);
        stage.Window.MouseUp(stage.Centre, MouseButton.Left);

        Assert.True(times.Count >= 4, $"{times.Count} clicks in 10 s");
        Assert.True(times[0] < RepeatDelay, $"the press clicked at {times[0]} ms");
        Assert.True(times[1] - times[0] >= RepeatDelay - 2, $"the first repeat came {times[1] - times[0]} ms after the press");
        for (var i = 2; i < times.Count; i++)
            Assert.True(times[i] - times[i - 1] >= RepeatInterval - 2, $"repeat {i} came {times[i] - times[i - 1]} ms after the last");
    }

    // Run 0755: held, slid off and held off for a second, then back. The phone clicked until the finger left, not once
    // while it was off, and again at the next tick once it was back: no second delay (here Delay is made long once the
    // repeats have begun, so a second delay would show). Neither a Click nor the Command while off.
    [AvaloniaFact]
    public void Off_The_Button_The_Repeats_Stop_And_Come_Back_With_The_Finger()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var button = stage.Button;
        var clicks = 0;
        var commands = 0;
        button.Click += (_, _) => clicks++;
        button.Command = new Counting(() => commands++);
        var off = stage.At(button.Bounds.Width / 2, button.Bounds.Height + 30);

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        RunFor(10_000, () => clicks >= 2);
        Assert.True(clicks >= 2, "no repeat");
        button.Delay = 60_000;

        stage.Window.MouseMove(off);
        Dispatcher.UIThread.RunJobs();
        var (atOff, commandsAtOff) = (clicks, commands);
        RunFor(10 * RepeatInterval + 100);
        Assert.Equal(atOff, clicks);
        Assert.Equal(commandsAtOff, commands);
        Assert.True(button.IsPressed);

        stage.Window.MouseMove(stage.Centre);
        RunFor(10_000, () => clicks > atOff);
        Assert.True(clicks > atOff, "no repeat after coming back");
        Assert.True(commands > commandsAtOff);
        stage.Window.MouseUp(stage.Centre, MouseButton.Left);
    }

    // A press slid off and lifted there leaves nothing behind: the next tap clicks at its press.
    [AvaloniaFact]
    public void After_A_Lift_Off_It_The_Next_Tap_Clicks()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var clicks = 0;
        stage.Button.Click += (_, _) => clicks++;
        var off = stage.At(stage.Button.Bounds.Width / 2, stage.Button.Bounds.Height + 30);
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        stage.Window.MouseMove(off);
        stage.Window.MouseUp(off, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        stage.Window.MouseMove(stage.Centre);
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, clicks);
        stage.Window.MouseUp(stage.Centre, MouseButton.Left);
    }

    // Only the RepeatButton's own Click is held back off it: a Button in its content clicking still reaches the window.
    [AvaloniaFact]
    public void A_Click_From_Its_Content_Isnt_Held_Back()
    {
        var inner = new Button { Content = "inner" };
        using var stage = new ButtonStage<RepeatButton>(new RepeatButton { Content = inner });
        var reached = new List<object?>();
        stage.Window.AddHandler(Button.ClickEvent, (_, e) => reached.Add(e.Source));
        stage.Window.MouseDown(stage.At(5, 4), MouseButton.Left);                       // the overhang: the RepeatButton's, not the inner one's
        stage.Window.MouseMove(stage.At(stage.Button.Bounds.Width / 2, stage.Button.Bounds.Height + 30));
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Button.IsPressed);

        inner.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Contains(inner, reached);
    }

    // The keyboard's clicks are Avalonia's, and nothing holds them back: Space clicks.
    [AvaloniaFact]
    public void Space_Clicks_Whatever_The_Pointer_Did_Before()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var clicks = 0;
        stage.Button.Click += (_, _) => clicks++;
        var off = stage.At(stage.Button.Bounds.Width / 2, stage.Button.Bounds.Height + 30);
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        stage.Window.MouseMove(off);
        stage.Window.MouseUp(off, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        var before = clicks;

        stage.Button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(clicks > before);
    }

    // PhoneRepeat is the theme's: a RepeatButton without it repeats wherever the pointer is, as Avalonia's does.
    [AvaloniaFact]
    public void Without_PhoneRepeat_Avalonias_Repeats_Go_On_Off_The_Button()
    {
        using var stage = new ButtonStage<RepeatButton>(Repeat());
        var button = stage.Button;
        PhoneRepeat.SetIsEnabled(button, false);
        button.Delay = 50;
        var clicks = 0;
        button.Click += (_, _) => clicks++;

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        stage.Window.MouseMove(stage.At(button.Bounds.Width / 2, button.Bounds.Height + 30));
        var atOff = clicks;
        RunFor(10_000, () => clicks > atOff);
        stage.Window.MouseUp(stage.Centre, MouseButton.Left);

        Assert.True(clicks > atOff);
    }

    private sealed class Counting(Action run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
