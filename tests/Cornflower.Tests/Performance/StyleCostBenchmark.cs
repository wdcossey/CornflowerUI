using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Sample.Diagnostics;
using Cornflower.Tests.Completeness;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Performance;

/// <summary>
/// What the theme costs in time, by hand, at each tier's roll-up: a CI machine's timings mean
/// nothing. It does nothing unless <c>CORNFLOWER_BENCHMARK</c> is the absolute path of a file, which
/// gets a row for the table in docs/performance/README.md. That also leaves Avalonia's diagnostics
/// off, as an app has them, so this has to run on its own. Alternates with and without the theme,
/// because warm-up flatters whichever comes second. Run it in Release.
/// </summary>
public class StyleCostBenchmark
{
    /// <summary>Names the file for the row.</summary>
    internal const string Variable = "CORNFLOWER_BENCHMARK";

    private const int Elements = 5000;
    private const int Depth = 20;
    private const int WarmUpRounds = 4;
    private const int Rounds = 25;

    private static double Attach(Func<Control> make, IStyle[] themes)
    {
        var panel = new StackPanel();
        for (var i = 0; i < Elements; i++)
        {
            panel.Children.Add(make());
        }

        Control root = panel;
        for (var i = 0; i < Depth; i++)
        {
            root = new Border { Child = root };
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        using var scene = new Scene(root, themes: themes);
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);

    [AvaloniaFact]
    public void Attach_Time()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { } file)
        {
            return;
        }

#if DEBUG
        Assert.Fail("Run this in Release: a Debug timing in the table would mean nothing.");
#endif
        var census = StyleCensus.Count(new Wp81Theme());
        var cells = new List<string>();
        foreach (var make in new Func<Control>[] { () => new TextBlock { Text = "x" }, () => new Border() })
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < WarmUpRounds + Rounds; round++)
            {
                var plain = Attach(make, []);
                var themed = Attach(make, [new Wp81Theme()]);
                if (round >= WarmUpRounds)
                {
                    without.Add(plain);
                    with.Add(themed);
                }
            }

            var before = Median(without) * 1000 / Elements;
            var after = Median(with) * 1000 / Elements;
            // The difference of the values as shown, so the three agree; the third section writes a difference that
            // rounds to zero as +0.0, where two sections would write -+0.0 for a small negative one.
            var delta = Math.Round(after, 1) - Math.Round(before, 1);
            cells.Add(string.Create(CultureInfo.InvariantCulture, $"{before:0.0} to {after:0.0} ({delta:+0.0;-0.0;+0.0})"));
        }

        // Themed: the templated controls that aren't holes.
        var templated = Holes.TemplatedControls(new SimpleTheme(), typeof(CornflowerClock).Assembly);
        var themedControls = templated.Count - Holes.In(new Wp81Theme(), templated).Count;
        var when = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        File.AppendAllText(file, $"| {when} | {themedControls} | {census.Rules} | {census.Alternatives} | {cells[0]} | {cells[1]} |{Environment.NewLine}");
    }
}
