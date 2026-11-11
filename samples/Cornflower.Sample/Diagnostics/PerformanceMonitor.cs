using System.Diagnostics.Metrics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Cornflower.Sample.Diagnostics;

/// <summary>
/// The line along the bottom of the sample in a Debug build: what a page cost to open, from <see cref="SampleShell"/>'s
/// time and Avalonia's own meter. It adds nothing to the library, and it doesn't trace, because tracing would cost more
/// than what it measures. See docs/performance/README.md.
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

    /// <summary>The switch that turns on Avalonia's diagnostics, off by default.</summary>
    public const string DiagnosticsSwitch = "Avalonia.Diagnostics.Diagnostic.IsEnabled";

    /// <summary>
    /// Avalonia reads the switch once, so call this before it starts: each head of the sample does, in
    /// Debug. Without it the meter is silent, and the line says so rather than show zeros.
    /// </summary>
    public static void EnableDiagnostics() => AppContext.SetSwitch(DiagnosticsSwitch, true);

    /// <summary>Whether the switch is on.</summary>
    public static bool DiagnosticsEnabled => AppContext.TryGetSwitch(DiagnosticsSwitch, out var enabled) && enabled;

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

    /// <summary>Call this before making a page, because styles apply as soon as it's attached: Avalonia's times count from here.</summary>
    public void Begin()
    {
        lock (_gate)
        {
            _layout = 0;
            _render = 0;
        }
    }

    /// <summary>
    /// The line for <paramref name="page"/>, which took <paramref name="opened"/> to open: <see cref="SampleShell"/> calls
    /// this once the page's first frame has drawn, with its own time from navigation, so that the line and the page's ready
    /// line agree. Layout has always run by then.
    /// </summary>
    public string Read(string title, Control page, TimeSpan opened)
    {
        if (!DiagnosticsEnabled)
        {
            return $"{title}: Avalonia's diagnostics are off. Call PerformanceMonitor.EnableDiagnostics() before Avalonia starts.";
        }

        var (rules, alternatives) = Census(page);
        double layout;
        double render;
        lock (_gate)
        {
            layout = _layout;
            render = _render;
        }

        return Format(title, opened.TotalMilliseconds, page.GetVisualDescendants().Count() + 1, rules, alternatives, layout, render);
    }

    /// <summary>
    /// The global rules <paramref name="page"/> is checked against: the application's, and those in
    /// the <c>Styles</c> of the page and each of its logical ancestors, its top level among them.
    /// Rules lower down cost only their own part of the page, and aren't counted.
    /// </summary>
    public static (int Rules, int Alternatives) Census(StyledElement page)
    {
        var (rules, alternatives) = Application.Current is { } app ? StyleCensus.Count(app.Styles) : (0, 0);
        for (var element = page; element is not null; element = element.Parent)
        {
            var (own, ownAlternatives) = StyleCensus.Count(element.Styles);
            rules += own;
            alternatives += ownAlternatives;
        }

        return (rules, alternatives);
    }

    /// <summary>The line.</summary>
    public static string Format(string title, double openMs, int elements, int rules, int alternatives, double layoutMs, double renderMs) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{title}: {openMs:0} ms, {elements} elements, {rules} rules ({alternatives}), layout {layoutMs:0.0}, render {renderMs:0.0}");

    public void Dispose() => _listener.Dispose();
}
