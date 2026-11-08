using Avalonia;
using Cornflower.Controls;

namespace Cornflower.Tests;

/// <summary>
/// Replaces <see cref="CornflowerClock"/>'s time for the life of a test, so every Cornflower
/// timeline is asserted exactly. Time starts at zero and moves only when <see cref="At"/> moves it,
/// which runs each callback that falls due on the way, at its own due time, in order. A frame is
/// <see cref="CornflowerClock.FrameInterval"/> after it is requested, in or out of a window.
/// </summary>
/// <remarks>
/// Avalonia's <c>Transitions</c> and <c>Animation</c> don't read this clock and still run on real
/// time. Only one <see cref="TestClock"/> can be installed at a time.
/// </remarks>
internal sealed class TestClock : IClockSource, IDisposable
{
    private readonly IClockSource _previous;
    private readonly List<Scheduled> _scheduled = [];
    private bool _disposed;

    public TestClock()
    {
        if (CornflowerClock.Source is TestClock)
            throw new InvalidOperationException("A TestClock is already installed. Dispose it first.");

        _previous = CornflowerClock.Source;
        CornflowerClock.Source = this;
    }

    public TimeSpan Now { get; private set; }

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

        while (_scheduled.Where(s => !s.IsDisposed && s.Due <= target).OrderBy(s => s.Due).FirstOrDefault() is { } next)
        {
            _scheduled.Remove(next);
            Now = next.Due;
            next.Callback();
        }

        _scheduled.RemoveAll(s => s.IsDisposed);
        Now = target;
        return Now;
    }

    public IDisposable Schedule(Action callback, TimeSpan delay)
    {
        var item = new Scheduled(Now + delay, callback);
        _scheduled.Add(item);
        return item;
    }

    public IDisposable RequestFrame(Visual visual, Action callback) => Schedule(callback, CornflowerClock.FrameInterval);

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
