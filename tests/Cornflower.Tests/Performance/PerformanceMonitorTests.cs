using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Sample;
using Cornflower.Sample.Diagnostics;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Performance;

/// <summary>The line along the bottom of the sample in a Debug build.</summary>
public class PerformanceMonitorTests
{
    [Fact]
    public void The_Line_Has_Each_Number()
    {
        var line = PerformanceMonitor.Format("theme", 41.4, 214, 98, 174, 2.14, 3.44);

        Assert.Equal("theme: 41 ms, 214 elements, 98 rules (174), layout 2.1, render 3.4", line);
    }

    [AvaloniaFact]
    public void It_Reports_A_Page_Once_It_Is_In()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel { Children = { new TextBlock(), new TextBlock() } };
        string? line = null;

        monitor.Begin();
        host.Content = page;
        monitor.Report("a page", page, text => line = text);
        Assert.Null(line);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(line);
        Assert.StartsWith("a page: ", line, StringComparison.Ordinal);
        Assert.Contains("3 elements", line, StringComparison.Ordinal);
    }

    // Styles apply as the page is attached, which is before it can be reported.
    [AvaloniaFact]
    public void The_Time_Includes_Making_The_Page()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string? line = null;

        monitor.Begin();
        Thread.Sleep(60);
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        var opened = int.Parse(Regex.Match(line!, "a page: ([0-9]+) ms").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.True(opened >= 50, line);
    }

    // Against a rename in Avalonia: the line would read zero for ever otherwise.
    [AvaloniaTheory]
    [InlineData(PerformanceMonitor.LayoutMeasure)]
    [InlineData(PerformanceMonitor.LayoutArrange)]
    [InlineData(PerformanceMonitor.Render)]
    public void Avalonias_Meter_Still_Has(string instrument)
    {
        using var scene = new Scene(new Panel());
        var published = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (found, _) =>
        {
            if (found.Meter.Name == "Avalonia.Diagnostic.Meter")
            {
                published.Add(found.Name);
            }
        };
        listener.Start();

        Assert.Contains(instrument, published);
    }

    // Not render: its pass can come after the line is read. See the known limits in docs/performance/README.md.
    [AvaloniaFact]
    public void Layout_Comes_From_Avalonias_Own_Meter()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel();
        for (var i = 0; i < 200; i++)
        {
            page.Children.Add(new TextBlock { Text = "x" });
        }

        string? line = null;
        monitor.Begin();
        host.Content = page;
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("layout 0.0", line!, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_That_Has_Gone_Is_Not_Reported()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new Panel();
        host.Content = page;
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        host.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Null(line);
    }

    // Through the sample's own page, so that this covers the wiring: a Debug build shows it, and a Release build doesn't.
    [AvaloniaFact]
    public void The_Sample_Shows_The_Line_Only_In_A_Debug_Build()
    {
        var view = new MainView();
        using var scene = new Scene(view);
        Dispatcher.UIThread.RunJobs();
        var line = view.GetControl<TextBlock>("PerformanceLine");

#if DEBUG
        Assert.True(line.IsVisible);
        Assert.StartsWith("theme: ", line.Text, StringComparison.Ordinal);
#else
        Assert.False(line.IsVisible);
#endif
    }

    // Large enough that what Avalonia itself reports meanwhile can't change the leading digits.
    [AvaloniaFact]
    public void Measure_And_Arrange_Are_Layout_And_Render_Is_Separate()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        monitor.Record(PerformanceMonitor.LayoutMeasure, 10000);
        monitor.Record(PerformanceMonitor.LayoutArrange, 20000);
        monitor.Record(PerformanceMonitor.Render, 50000);
        Dispatcher.UIThread.RunJobs();

        Assert.Matches("layout 300[0-9][0-9][.][0-9], render 500[0-9][0-9][.][0-9]$", line!);
    }

    [AvaloniaFact]
    public void Times_Start_Again_With_Each_Page()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        monitor.Record(PerformanceMonitor.LayoutMeasure, 90000);
        monitor.Record(PerformanceMonitor.Render, 90000);
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("9000", line!, StringComparison.Ordinal);
    }

    // The window's styles, as in a Scene, and the application's, as in the sample.
    [AvaloniaFact]
    public void It_Counts_The_Theme_Rules_Of_The_Window_And_The_Application()
    {
        var windowRule = new Style(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>()));
        windowRule.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var appRule = new Style(x => x.OfType<Border>());
        appRule.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page, themes: [new Wp81Theme(), new Styles { windowRule }]);
        Application.Current!.Styles.Add(appRule);
        string? line = null;
        try
        {
            monitor.Begin();
            monitor.Report("a page", page, text => line = text);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            Application.Current.Styles.Remove(appRule);
        }

        Assert.Contains("2 rules (3)", line!, StringComparison.Ordinal);
    }
}
