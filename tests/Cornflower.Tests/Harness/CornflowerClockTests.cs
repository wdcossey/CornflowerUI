using System.Diagnostics;
using System.Reflection;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.Media;
using Cornflower.Controls;

namespace Cornflower.Tests.Harness;

/// <summary>
/// <see cref="CornflowerClock"/> with no test clock installed: real time. The real source is
/// tested directly, so a test clock elsewhere can't change what these see.
/// </summary>
public class CornflowerClockTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    // Why the hook exists: Avalonia 12's own animation clock can't be replaced from outside, so
    // Transitions and Animation run on real time. An Avalonia upgrade that opens it turns this red.
    [Fact]
    public void Avalonias_Animation_Clock_Cannot_Be_Replaced()
    {
        var avalonia = typeof(Animatable).Assembly;

        Assert.False(avalonia.GetType("Avalonia.Animation.IClock", throwOnError: true)!.IsPublic);
        Assert.False(avalonia.GetType("Avalonia.Animation.Clock", throwOnError: true)!.IsPublic);
        Assert.Null(typeof(Animatable).GetProperty("Clock", BindingFlags.Public | BindingFlags.Instance));
    }

    [AvaloniaFact]
    public void By_Default_The_Clock_Is_Real_Time()
    {
        Assert.Same(RealClock.Instance, CornflowerClock.Source);
    }

    [AvaloniaFact]
    public void Real_Time_Moves_On_Its_Own()
    {
        var start = RealClock.Instance.Now;
        Thread.Sleep(30);

        Assert.True(RealClock.Instance.Now - start >= TimeSpan.FromMilliseconds(25));
    }

    [AvaloniaFact]
    public async Task Real_Schedule_Runs_Once_On_The_UI_Thread_After_The_Delay()
    {
        var stopwatch = Stopwatch.StartNew();
        var runs = new List<(TimeSpan At, bool OnUIThread)>();

        RealClock.Instance.Schedule(() => runs.Add((stopwatch.Elapsed, Dispatcher.UIThread.CheckAccess())), TimeSpan.FromMilliseconds(50));

        await Until(() => runs.Count > 0);
        await Task.Delay(100);

        var run = Assert.Single(runs);
        Assert.True(run.At >= TimeSpan.FromMilliseconds(45), $"ran after {run.At.TotalMilliseconds} ms");
        Assert.True(run.OnUIThread);
    }

    [AvaloniaFact]
    public async Task Real_Schedule_Cancelled_Does_Not_Run()
    {
        var ran = false;

        RealClock.Instance.Schedule(() => ran = true, TimeSpan.FromMilliseconds(20)).Dispose();
        await Task.Delay(100);
        Dispatcher.UIThread.RunJobs();

        Assert.False(ran);
    }

    [AvaloniaFact]
    public void Real_RequestFrame_Runs_On_The_Windows_Next_Frame()
    {
        var content = new Border { Background = Brushes.Red };
        using var scene = new Scene(content);
        // A frame in flight: the change is committed, and composed only on the next render tick.
        // Until then the window's next frame waits, so a callback merely posted to the dispatcher
        // would run in the RunJobs below.
        content.Background = Brushes.Blue;
        Dispatcher.UIThread.RunJobs();
        var ran = false;

        RealClock.Instance.RequestFrame(content, () => ran = true);
        Dispatcher.UIThread.RunJobs();
        Assert.False(ran);

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.True(ran);
    }

    [AvaloniaFact]
    public void Real_RequestFrame_Cancelled_Does_Not_Run()
    {
        var content = new Border();
        using var scene = new Scene(content);
        var ran = false;

        RealClock.Instance.RequestFrame(content, () => ran = true).Dispose();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.False(ran);
    }

    [AvaloniaFact]
    public async Task Real_RequestFrame_Outside_A_Window_Runs_After_A_Frame_Interval()
    {
        var stopwatch = Stopwatch.StartNew();
        TimeSpan? ranAt = null;

        RealClock.Instance.RequestFrame(new Border(), () => ranAt = stopwatch.Elapsed);

        await Until(() => ranAt is not null);

        Assert.True(ranAt >= CornflowerClock.FrameInterval - TimeSpan.FromMilliseconds(2), $"ran after {ranAt?.TotalMilliseconds} ms");
    }

    private static async Task Until(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > Timeout) throw new TimeoutException("The callback never ran.");
            await Task.Delay(5);
        }
    }
}
