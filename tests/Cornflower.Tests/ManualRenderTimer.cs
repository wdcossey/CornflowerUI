using System.Reflection;
using Avalonia;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Cornflower.Tests;

/// <summary>
/// The headless render loop's timer, ticked only by
/// <c>AvaloniaHeadlessPlatform.ForceRenderTimerTick()</c>, so a frame is composed only when a test,
/// or one of Avalonia's headless helpers (input, <c>CaptureRenderedFrame</c>), asks for one.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia.Headless drives its render loop from <c>HeadlessRenderTimer</c>, a real
/// <c>DispatcherTimer</c> at the platform's frame rate, so any <c>RunJobs</c> could run a tick that
/// happened to be due and compose a frame no test asked for, more often on a slower machine.
/// <see cref="TestApp"/> installs this timer after each test's platform setup (tests are isolated
/// per test, each with its own render loop).
/// </para>
/// <para>
/// Avalonia's MediaContext has a real timer of its own, <c>_animationsTimer</c>, 16 ms: with
/// nothing in flight, it runs the next render pass for frame callbacks still waiting (one asked
/// for from inside another, say) and for <c>Transitions</c> and <c>Animation</c>. It's made a
/// forced tick's too, so those also move only on a tick.
/// </para>
/// <para>
/// <see cref="IRenderTimer"/> is <c>[PrivateApi]</c>: Avalonia's reference assemblies make its
/// members internal, so C# can't implement it. At run time it's an ordinary public interface
/// (<c>Tick</c>, <c>RunsInBackground</c>), which <see cref="DispatchProxy"/> implements here.
/// </para>
/// </remarks>
public class ManualRenderTimer : DispatchProxy
{
    /// <summary>Each tick is one frame of a 60 Hz display, whatever the wall clock says.</summary>
    private static readonly TimeSpan FrameInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);

    private TimeSpan _now;

    /// <summary>The loop's tick while it is awake; null while it sleeps.</summary>
    private Action<TimeSpan>? _tick;

    /// <summary>MediaContext's animations timer, which never fires on its own here.</summary>
    private DispatcherTimer? _animationsTimer;

    /// <summary>What the animations timer does when it fires: MediaContext.ScheduleRender(false).</summary>
    private Action? _scheduleRender;

    /// <summary>
    /// Puts a manual timer under the current platform's render loop, in place of the platform's
    /// own, which is stopped. The platform's forced tick is pointed at the manual timer, so
    /// <c>ForceRenderTimerTick</c> and the headless helpers that call it still draw.
    /// </summary>
    internal static void Install()
    {
        // AvaloniaLocator.Current is hidden from the reference assemblies too.
        var locator = typeof(AvaloniaLocator).GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null)
            ?? throw Changed("AvaloniaLocator.Current");
        var getService = locator.GetType().GetMethod("GetService", [typeof(Type)])
            ?? throw Changed("AvaloniaLocator.GetService(Type)");
        var loop = getService.Invoke(locator, [typeof(IRenderLoop)])
            ?? throw new InvalidOperationException("The platform has no render loop.");

        // Avalonia 12.1.4's DefaultRenderLoop keeps its timer in _timer, and the compositor holds
        // the loop it was given at setup, so the timer is swapped inside the loop.
        var timerField = loop.GetType().GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw Changed($"{loop.GetType()}._timer");
        var tickProperty = typeof(IRenderTimer).GetProperty("Tick")
            ?? throw Changed("IRenderTimer.Tick");
        var platformTimer = timerField.GetValue(loop)!;
        // HeadlessRenderTimer.ForceTick, which ForceRenderTimerTick calls, invokes _forceTick.
        var forceTickField = platformTimer.GetType().GetField("_forceTick", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw Changed($"{platformTimer.GetType()}._forceTick");

        var proxy = DispatchProxy.Create<IRenderTimer, ManualRenderTimer>();
        var manual = (ManualRenderTimer)(object)proxy;

        var tick = (Action<TimeSpan>?)tickProperty.GetValue(platformTimer);
        tickProperty.SetValue(platformTimer, null); // Stops its DispatcherTimer.
        timerField.SetValue(loop, proxy);
        manual._tick = tick;
        // Set after the stop above, which clears it; the loop no longer sets the platform timer's
        // Tick, so nothing restarts it or resets this.
        forceTickField.SetValue(platformTimer, (Action)manual.ForceTick);

        // MediaContext is internal; each test's locator holds its own.
        var mediaContextType = typeof(AvaloniaObject).Assembly.GetType("Avalonia.Media.MediaContext")
            ?? throw Changed("Avalonia.Media.MediaContext");
        var mediaContext = mediaContextType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
            ?? throw Changed("MediaContext.Instance");
        var animationsTimer = mediaContextType.GetField("_animationsTimer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(mediaContext) as DispatcherTimer
            ?? throw Changed("MediaContext._animationsTimer");
        var scheduleRender = mediaContextType.GetMethod("ScheduleRender", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)])
            ?? throw Changed("MediaContext.ScheduleRender(bool)");
        animationsTimer.Stop();
        animationsTimer.Interval = TimeSpan.FromDays(1);
        manual._animationsTimer = animationsTimer;
        manual._scheduleRender = () => scheduleRender.Invoke(mediaContext, [false]);
    }

    /// <summary>
    /// One render tick, a frame interval after the last: the loop's, if it's awake, and the
    /// animations timer's, if MediaContext started it.
    /// </summary>
    private void ForceTick()
    {
        _now += FrameInterval;
        _tick?.Invoke(_now);
        if (_animationsTimer is { IsEnabled: true })
        {
            _animationsTimer.Stop();
            _scheduleRender!();
        }
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
    {
        "get_Tick" => _tick,
        "set_Tick" => _tick = (Action<TimeSpan>?)args![0],
        "get_RunsInBackground" => false,
        _ => throw Changed($"IRenderTimer.{targetMethod?.Name}"),
    };

    private static InvalidOperationException Changed(string what) =>
        new($"{what} is not as Avalonia 12.1.4 had it: the headless render timer needs another look.");
}
