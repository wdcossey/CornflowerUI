using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Rendering;

namespace Cornflower.Tests;

/// <summary>
/// The headless render loop's timer, ticked only by <see cref="ForceTick"/>, so a frame is drawn
/// only when a test asks for one.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia.Headless drives its render loop from <c>HeadlessRenderTimer</c>, a real
/// <c>DispatcherTimer</c> at the platform's frame rate, so any <c>RunJobs</c> (a test's own, or
/// <see cref="Scene"/>'s) could run a tick that happened to be due and draw frames no test asked
/// for, more often on a slower machine. <see cref="TestApp"/> puts this timer under each test's
/// render loop in place of that one. <c>AvaloniaHeadlessPlatform.ForceRenderTimerTick</c> still
/// ticks the platform's timer, which no longer drives anything: use <see cref="ForceTick"/>.
/// </para>
/// <para>
/// <see cref="IRenderTimer"/> is <c>[PrivateApi]</c>: Avalonia's reference assemblies make its
/// members internal, so C# can't implement it. At run time it's an ordinary public interface
/// (<c>Tick</c>, <c>RunsInBackground</c>), which <see cref="DispatchProxy"/> implements here.
/// </para>
/// </remarks>
public class ManualRenderTimer : DispatchProxy
{
    private static ManualRenderTimer? s_current;

    private readonly Stopwatch _clock = Stopwatch.StartNew();

    /// <summary>The loop's tick while it is awake; null while it sleeps.</summary>
    private Action<TimeSpan>? _tick;

    /// <summary>
    /// Runs <paramref name="count"/> render ticks, as the platform's timer would have: each runs the
    /// frame callbacks due and renders. A loop with nothing to render is asleep, and a tick does
    /// nothing, as it would on a device.
    /// </summary>
    public static void ForceTick(int count = 1)
    {
        var timer = s_current
            ?? throw new InvalidOperationException("No manual render timer: TestApp installs one after each test's setup.");
        for (var i = 0; i < count; i++)
        {
            timer._tick?.Invoke(timer._clock.Elapsed);
        }
    }

    /// <summary>
    /// Puts a new manual timer under the current platform's render loop, in place of its own, and
    /// stops that one. Runs after every test's platform setup: tests are isolated per test, each
    /// with its own render loop.
    /// </summary>
    internal static void Install()
    {
        // AvaloniaLocator.Current is hidden from the reference assemblies too.
        var locator = typeof(AvaloniaLocator).GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null)
            ?? throw new InvalidOperationException("No AvaloniaLocator.Current: Avalonia's locator has changed.");
        var getService = locator.GetType().GetMethod("GetService", [typeof(Type)])
            ?? throw new InvalidOperationException("No AvaloniaLocator.GetService(Type): Avalonia's locator has changed.");
        var loop = getService.Invoke(locator, [typeof(IRenderLoop)])
            ?? throw new InvalidOperationException("The platform has no render loop.");

        // Avalonia 12.1.4's DefaultRenderLoop keeps its timer in _timer, and the compositor holds
        // the loop it was given at setup, so the timer is swapped inside the loop.
        var field = loop.GetType().GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{loop.GetType()} has no _timer field: Avalonia's render loop has changed.");
        var tickProperty = typeof(IRenderTimer).GetProperty("Tick")
            ?? throw new InvalidOperationException("IRenderTimer has no Tick: Avalonia's render timer has changed.");

        var platformTimer = field.GetValue(loop)!;
        var proxy = DispatchProxy.Create<IRenderTimer, ManualRenderTimer>();
        var manual = (ManualRenderTimer)(object)proxy;

        var tick = (Action<TimeSpan>?)tickProperty.GetValue(platformTimer);
        tickProperty.SetValue(platformTimer, null); // Stops its DispatcherTimer.
        field.SetValue(loop, proxy);
        manual._tick = tick;
        s_current = manual;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
    {
        "get_Tick" => _tick,
        "set_Tick" => _tick = (Action<TimeSpan>?)args![0],
        "get_RunsInBackground" => false,
        _ => throw new NotSupportedException($"IRenderTimer.{targetMethod?.Name} is new: Avalonia's render timer has changed."),
    };
}
