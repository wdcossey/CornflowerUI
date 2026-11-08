using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cornflower.Controls;

/// <summary>
/// The time every Cornflower timeline reads: the tilt, our transitions, and any defer. A behaviour
/// asks it for the time, for a one-shot callback after a delay, and for a callback before the next
/// frame, and never asks a <see cref="Stopwatch"/>, a <see cref="DispatcherTimer"/> or the
/// <see cref="TopLevel"/> itself.
/// </summary>
/// <remarks>
/// <para>
/// It exists so that a test can replace time. The tests' <c>TestClock</c> sets <see cref="Source"/>
/// and advances it by hand, so a timeline is asserted to the millisecond instead of roughly.
/// Avalonia's own animation clock can't be replaced: in Avalonia 12 <c>IClock</c>, <c>Clock</c>
/// and <c>Animatable.Clock</c> are internal, so <c>Transitions</c> and <c>Animation</c> run on
/// real time. A Cornflower timeline that has to be tested by the millisecond is a frame loop on
/// this clock instead.
/// </para>
/// <para>
/// It has no visual opinion and no public surface. It is process-wide and not thread-safe: it is
/// read and set on the UI thread.
/// </para>
/// </remarks>
internal static class CornflowerClock
{
    /// <summary>One frame, for what has no frame of its own: a visual outside a window, and a test's clock.</summary>
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);

    /// <summary>Where the time comes from. Real time unless a test has replaced it.</summary>
    internal static IClockSource Source { get; set; } = RealClock.Instance;

    /// <summary>Monotonic time since an arbitrary start. Only differences mean anything.</summary>
    public static TimeSpan Now => Source.Now;

    /// <summary>Runs <paramref name="callback"/> once on the UI thread after <paramref name="delay"/>. Dispose to cancel.</summary>
    public static IDisposable Schedule(Action callback, TimeSpan delay) => Source.Schedule(callback, delay);

    /// <summary>
    /// Runs <paramref name="callback"/> once, before the next frame of <paramref name="visual"/>'s window.
    /// Outside a window there is no frame, so it runs after <see cref="FrameInterval"/>. Dispose to cancel.
    /// </summary>
    public static IDisposable RequestFrame(Visual visual, Action callback) => Source.RequestFrame(visual, callback);
}

/// <summary>A source of time for <see cref="CornflowerClock"/>.</summary>
internal interface IClockSource
{
    /// <inheritdoc cref="CornflowerClock.Now"/>
    TimeSpan Now { get; }

    /// <inheritdoc cref="CornflowerClock.Schedule"/>
    IDisposable Schedule(Action callback, TimeSpan delay);

    /// <inheritdoc cref="CornflowerClock.RequestFrame"/>
    IDisposable RequestFrame(Visual visual, Action callback);
}

/// <summary>Real time: a stopwatch, the dispatcher's timers and the window's frames.</summary>
internal sealed class RealClock : IClockSource
{
    public static readonly RealClock Instance = new();

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private RealClock()
    {
    }

    public TimeSpan Now => _stopwatch.Elapsed;

    public IDisposable Schedule(Action callback, TimeSpan delay) => DispatcherTimer.RunOnce(callback, delay);

    public IDisposable RequestFrame(Visual visual, Action callback)
    {
        if (TopLevel.GetTopLevel(visual) is not { } top)
        {
            return Schedule(callback, CornflowerClock.FrameInterval);
        }

        var frame = new CancellableFrame();
        top.RequestAnimationFrame(_ =>
        {
            if (!frame.IsCancelled)
            {
                callback();
            }
        });
        return frame;
    }

    private sealed class CancellableFrame : IDisposable
    {
        public bool IsCancelled { get; private set; }

        public void Dispose() => IsCancelled = true;
    }
}
