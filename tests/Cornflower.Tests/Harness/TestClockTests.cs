using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Cornflower.Controls;

namespace Cornflower.Tests.Harness;

/// <summary>
/// <see cref="TestClock"/>, seen through <see cref="CornflowerClock"/>, as a behaviour sees it.
/// </summary>
public class TestClockTests
{
    [AvaloniaFact]
    public void Time_Starts_At_Zero_And_Moves_Only_With_At()
    {
        using var clock = new TestClock();

        Assert.Equal(TimeSpan.Zero, CornflowerClock.Now);
        Thread.Sleep(20);
        Assert.Equal(TimeSpan.Zero, CornflowerClock.Now);

        clock.At(250);

        Assert.Equal(TimeSpan.FromMilliseconds(250), CornflowerClock.Now);
    }

    [AvaloniaFact]
    public void At_Runs_Each_Due_Callback_In_Time_Order_At_Its_Own_Time()
    {
        using var clock = new TestClock();
        var ran = new List<string>();
        void Record(string name) => ran.Add($"{name}@{CornflowerClock.Now.TotalMilliseconds}");

        // Scheduled out of time order, so running them in the order they were scheduled shows.
        CornflowerClock.Schedule(() => Record("c"), TimeSpan.FromMilliseconds(30));
        CornflowerClock.Schedule(() => Record("b1"), TimeSpan.FromMilliseconds(20));
        CornflowerClock.Schedule(() => Record("a"), TimeSpan.FromMilliseconds(10));
        CornflowerClock.Schedule(() => Record("b2"), TimeSpan.FromMilliseconds(20));

        clock.At(25);

        Assert.Equal(["a@10", "b1@20", "b2@20"], ran);
        Assert.Equal(TimeSpan.FromMilliseconds(25), CornflowerClock.Now);

        clock.At(30);

        Assert.Equal(["a@10", "b1@20", "b2@20", "c@30"], ran);
    }

    // A timeline that reschedules itself, as a frame loop does, runs every step up to the target.
    [AvaloniaFact]
    public void A_Callback_Scheduled_On_The_Way_Runs_If_It_Falls_Due_By_The_Target()
    {
        using var clock = new TestClock();
        var ran = new List<double>();

        CornflowerClock.Schedule(() =>
        {
            ran.Add(CornflowerClock.Now.TotalMilliseconds);
            CornflowerClock.Schedule(() => ran.Add(CornflowerClock.Now.TotalMilliseconds), TimeSpan.FromMilliseconds(5));
            CornflowerClock.Schedule(() => ran.Add(CornflowerClock.Now.TotalMilliseconds), TimeSpan.FromMilliseconds(15));
        }, TimeSpan.FromMilliseconds(10));

        clock.At(20);

        Assert.Equal([10, 15], ran);
        Assert.Equal(1, clock.Pending);
    }

    [AvaloniaFact]
    public void A_Cancelled_Callback_Does_Not_Run()
    {
        using var clock = new TestClock();
        var ran = false;

        CornflowerClock.Schedule(() => ran = true, TimeSpan.FromMilliseconds(10)).Dispose();
        clock.At(100);

        Assert.False(ran);
        Assert.Equal(0, clock.Pending);
    }

    [AvaloniaFact]
    public void A_Frame_Is_A_Frame_Interval_After_It_Is_Requested_In_Or_Out_Of_A_Window()
    {
        using var clock = new TestClock();
        var inWindow = new Border();
        using var scene = new Scene(inWindow);
        var ran = new List<string>();

        clock.At(5);
        CornflowerClock.RequestFrame(inWindow, () => ran.Add($"window@{CornflowerClock.Now.TotalMilliseconds}"));
        CornflowerClock.RequestFrame(new Border(), () => ran.Add($"detached@{CornflowerClock.Now.TotalMilliseconds}"));

        clock.At(20);
        Assert.Empty(ran);

        clock.At(21);
        Assert.Equal(["window@21", "detached@21"], ran);
    }

    [AvaloniaFact]
    public void Disposing_Puts_The_Previous_Clock_Back()
    {
        var before = CornflowerClock.Source;

        new TestClock().Dispose();

        Assert.Same(before, CornflowerClock.Source);
    }

    // Two at once would leave the second's time installed when the first is disposed.
    [AvaloniaFact]
    public void A_Second_TestClock_Is_Refused()
    {
        using var clock = new TestClock();

        Assert.Throws<InvalidOperationException>(() => new TestClock());
        Assert.Same(clock, CornflowerClock.Source);
    }

    // DispatcherTimer refuses a negative delay too; accepted, it would run in the past.
    [AvaloniaFact]
    public void A_Negative_Delay_Is_Refused()
    {
        using var clock = new TestClock();
        clock.At(100);

        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerClock.Schedule(() => { }, TimeSpan.FromMilliseconds(-10)));
    }

    [AvaloniaFact]
    public void At_Is_Refused_From_A_Callback_It_Is_Running()
    {
        using var clock = new TestClock();
        Exception? refused = null;
        CornflowerClock.Schedule(() => refused = Record.Exception(() => clock.At(200)), TimeSpan.FromMilliseconds(10));

        clock.At(20);

        Assert.IsType<InvalidOperationException>(refused);
        Assert.Equal(TimeSpan.FromMilliseconds(20), CornflowerClock.Now);
    }

    // A real dispatcher would yield between runs; At would spin for ever and hang the session.
    [AvaloniaFact]
    public void A_Timeline_Rescheduling_Itself_Without_A_Delay_Fails_Instead_Of_Hanging()
    {
        using var clock = new TestClock();
        void Step() => CornflowerClock.Schedule(Step, TimeSpan.Zero);
        CornflowerClock.Schedule(Step, TimeSpan.Zero);

        Assert.Throws<InvalidOperationException>(() => clock.At(16));
    }

    [AvaloniaFact]
    public void Time_Does_Not_Go_Back()
    {
        using var clock = new TestClock();
        clock.At(10);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.At(5));
        Assert.Equal(TimeSpan.FromMilliseconds(10), CornflowerClock.Now);
    }
}
