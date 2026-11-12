using Avalonia;
using Cornflower.Controls;

namespace Cornflower.Tests;

/// <summary>
/// Replaces <see cref="CornflowerClock"/>'s time for the life of a test, so every Cornflower
/// timeline is asserted exactly. Time starts at zero and moves only when <see cref="At"/> moves it,
/// which runs each callback that falls due on the way, at its own due time, in order. A frame is
/// <see cref="FrameInterval"/> after it is requested, in or out of a window, or with
/// <see cref="FramesOnGrid"/> at the next multiple of it.
/// </summary>
/// <remarks>
/// Avalonia's <c>Transitions</c> and <c>Animation</c> don't read this clock and still run on real
/// time. The clock is process-wide, so only one <see cref="TestClock"/> can be installed at a time,
/// and a test doesn't hold one across an <c>await</c>: other tests share the UI thread and could
/// run in the gap.
/// </remarks>
internal sealed class TestClock : IClockSource, IDisposable
{
    private readonly IClockSource _previous;
    /// <summary>More callbacks than this in one <see cref="At"/> is a timeline rescheduling itself without moving on.</summary>
    private const int MaxCallbacksPerAt = 100_000;

    private readonly List<Scheduled> _scheduled = [];
    private bool _advancing;
    private bool _disposed;

    public TestClock()
    {
        if (CornflowerClock.Source is TestClock)
            throw new InvalidOperationException("A TestClock is already installed. Dispose it first.");

        _previous = CornflowerClock.Source;
        CornflowerClock.Source = this;
    }

    public TimeSpan Now { get; private set; }

    /// <summary>
    /// How long after it is requested a frame runs: <see cref="CornflowerClock.FrameInterval"/>, 16 ms,
    /// unless a test sets another, as one checking against the emulator's 15.625 ms frames does.
    /// </summary>
    public TimeSpan FrameInterval { get; set; } = CornflowerClock.FrameInterval;

    /// <summary>
    /// Frames at the next multiple of <see cref="FrameInterval"/> after they are requested, as a
    /// display's frames come on its own clock, rather than a whole interval after. Off by default:
    /// the harness's own tests hold the interval-after rule.
    /// </summary>
    public bool FramesOnGrid { get; set; }

    /// <summary>Callbacks scheduled and not yet run or cancelled.</summary>
    public int Pending => _scheduled.Count(s => !s.IsDisposed);

    /// <summary>
    /// Moves to <paramref name="milliseconds"/> after the start, running every callback due on the
    /// way. A callback sees <see cref="Now"/> as the time it was due for, and one it schedules runs
    /// too if it falls due by the target. Callbacks due at the same time run in the order they were
    /// scheduled. Time doesn't go back.
    /// </summary>
    public TimeSpan At(double milliseconds)
    {
        var target = TimeSpan.FromMilliseconds(milliseconds);
        if (target < Now)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), $"The clock is at {Now.TotalMilliseconds} ms and doesn't go back.");
        if (_advancing)
            throw new InvalidOperationException("At was called from a callback it was running.");

        _advancing = true;
        try
        {
            var ran = 0;
            while (_scheduled.Where(s => !s.IsDisposed && s.Due <= target).OrderBy(s => s.Due).FirstOrDefault() is { } next)
            {
                if (++ran > MaxCallbacksPerAt)
                    throw new InvalidOperationException($"More than {MaxCallbacksPerAt} callbacks by {target.TotalMilliseconds} ms: a timeline is rescheduling itself without a delay.");

                _scheduled.Remove(next);
                Now = next.Due;
                next.Callback();
            }
        }
        finally
        {
            _advancing = false;
        }

        _scheduled.RemoveAll(s => s.IsDisposed);
        Now = target;
        return Now;
    }

    /// <summary>As <see cref="Avalonia.Threading.DispatcherTimer"/>, a negative delay is refused.</summary>
    public IDisposable Schedule(Action callback, TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        var item = new Scheduled(Now + delay, callback);
        _scheduled.Add(item);
        return item;
    }

    public IDisposable RequestFrame(Visual visual, Action callback)
    {
        if (!FramesOnGrid) return Schedule(callback, FrameInterval);
        var next = (Now.Ticks / FrameInterval.Ticks + 1) * FrameInterval.Ticks;
        return Schedule(callback, TimeSpan.FromTicks(next) - Now);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CornflowerClock.Source = _previous;
    }

    private sealed class Scheduled(TimeSpan due, Action callback) : IDisposable
    {
        public TimeSpan Due { get; } = due;

        public Action Callback { get; } = callback;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
