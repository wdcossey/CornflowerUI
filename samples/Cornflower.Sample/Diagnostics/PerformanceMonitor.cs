using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cornflower.Sample.Diagnostics;

/// <summary>
/// The line along the bottom of the sample in a Debug build: what a page cost to open, from Avalonia's own meter. It adds
/// nothing to the library, and it doesn't trace, because tracing would cost more than what it measures. See
/// docs/performance/README.md.
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private const string Meter = "Avalonia.Diagnostic.Meter";
    public const string LayoutMeasure = "avalonia.ui.measure.time";
    public const string LayoutArrange = "avalonia.ui.arrange.time";
    public const string Render = "avalonia.ui.render.time";

    private readonly MeterListener _listener = new();
    private readonly object _gate = new();
    private double _layout;
    private double _render;
    private long _started;

    /// <summary>Avalonia reads the switch once, so call this before it starts.</summary>
    public static void EnableDiagnostics() => AppContext.SetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", true);

    public PerformanceMonitor()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == Meter && instrument.Name is LayoutMeasure or LayoutArrange or Render)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => Record(instrument.Name, value));
        _listener.Start();
    }

    /// <summary>A time from Avalonia's meter, in milliseconds.</summary>
    public void Record(string instrument, double milliseconds)
    {
        lock (_gate)
        {
            if (instrument == Render)
            {
                _render += milliseconds;
            }
            else
            {
                _layout += milliseconds;
            }
        }
    }

    /// <summary>Call this before making a page, because styles apply as soon as it's attached.</summary>
    public void Begin()
    {
        lock (_gate)
        {
            _layout = 0;
            _render = 0;
        }

        _started = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Call this once the page is in. <paramref name="report"/> gets the line after it's been laid out, unless it's gone by
    /// then.
    /// </summary>
    public void Report(string title, Control page, Action<string> report)
    {
        var started = _started;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (TopLevel.GetTopLevel(page) is not { } top)
                {
                    return;
                }

                var elapsed = Stopwatch.GetElapsedTime(started);
                var (rules, alternatives) = Census(top);
                double layout;
                double render;
                lock (_gate)
                {
                    layout = _layout;
                    render = _render;
                }

                report(Format(title, elapsed.TotalMilliseconds, page.GetVisualDescendants().Count() + 1, rules, alternatives, layout, render));
            },
            DispatcherPriority.Background);
    }

    /// <summary>
    /// The global rules a page is checked against from its themes: the application's and its top
    /// level's. The sample puts its themes in the application; the tests' <c>Scene</c>, in its window.
    /// </summary>
    public static (int Rules, int Alternatives) Census(TopLevel top)
    {
        var (appRules, appAlternatives) = Application.Current is { } current ? StyleCensus.Count(current.Styles) : (0, 0);
        var (ownRules, ownAlternatives) = StyleCensus.Count(top.Styles);
        return (appRules + ownRules, appAlternatives + ownAlternatives);
    }

    /// <summary>The line.</summary>
    public static string Format(string title, double openMs, int elements, int rules, int alternatives, double layoutMs, double renderMs) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{title}: {openMs:0} ms, {elements} elements, {rules} rules ({alternatives}), layout {layoutMs:0.0}, render {renderMs:0.0}");

    public void Dispose() => _listener.Dispose();
}
