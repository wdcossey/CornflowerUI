using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Performance;

/// <summary>
/// What the type ramp's hooks cost (ADR 0004): a <c>TextBlock</c> with a text style, its values reached by
/// <c>DynamicResource</c> as the theme has them, against the same values fixed, as <c>StaticResource</c>
/// would fix them when the theme loads. By hand, like <see cref="StyleCostBenchmark"/>, and only when
/// <c>CORNFLOWER_BENCHMARK</c> names a file, which gets a table. Run it in Release, on its own:
/// <code>
/// CORNFLOWER_BENCHMARK=/absolute/path/ramp.md dotnet test tests/Cornflower.Tests -c Release --filter FullyQualifiedName~TypeRampCostBenchmark
/// </code>
/// Five ways, in turn each round, 25 rounds after 4 to warm up, 5,000 elements at a depth of 20, times in µs
/// per element (see <see cref="Ways"/>).
/// </summary>
public class TypeRampCostBenchmark
{
    private const int Elements = 5000;
    private const int Depth = 20;
    private const int WarmUpRounds = 4;
    private const int Rounds = 25;

    /// <summary>The ways a <c>TextBlock</c> is styled, in the table's order.</summary>
    internal static readonly string[] Ways =
    [
        "No style",
        // Every value fixed, PhoneTextLine's worked out once: what StaticResource would give.
        "`StaticResource`",
        // Only what must follow the variant or an app's font: the brushes and font families.
        "Brushes and fonts dynamic",
        // Every resource dynamic, PhoneTextLine's values fixed.
        "`DynamicResource`",
        // As shipped: every resource dynamic, and PhoneTextLine works its values out for each TextBlock.
        "The theme",
    ];

    private static double Attach(ControlTheme? theme)
    {
        var panel = new StackPanel();
        for (var i = 0; i < Elements; i++)
        {
            panel.Children.Add(new TextBlock { Text = "x", Theme = theme });
        }

        Control root = panel;
        for (var i = 0; i < Depth; i++)
        {
            root = new Border { Child = root };
        }

        // Loaded before the clock starts, as StyleCostBenchmark's are.
        var wp81 = new Wp81Theme();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        using var scene = new Scene(root, themes: [wp81]);
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>The style's setters, base first, each property's last.</summary>
    private static Dictionary<AvaloniaProperty, object?> Setters(ControlTheme theme)
    {
        var chain = new List<ControlTheme>();
        for (var t = theme; t is not null; t = t.BasedOn)
            chain.Insert(0, t);

        var setters = new Dictionary<AvaloniaProperty, object?>();
        foreach (var setter in chain.SelectMany(t => t.Setters).OfType<Setter>())
            setters[setter.Property!] = setter.Value;
        return setters;
    }

    /// <summary>The style, flattened, keeping as dynamic only the resources <paramref name="dynamic"/> picks.</summary>
    private static ControlTheme Flattened(ControlTheme theme, TextBlock styled, Func<AvaloniaProperty, bool> dynamic)
    {
        var flat = new ControlTheme(typeof(TextBlock));
        var setters = Setters(theme);
        foreach (var (property, value) in setters)
        {
            // PhoneTextLine's own properties become the values they set, below.
            if (property.OwnerType == typeof(PhoneTextLine))
                continue;

            flat.Setters.Add(new Setter(property, value is DynamicResourceExtension && dynamic(property) ? value : styled.GetValue(property)));
        }

        foreach (var property in new AvaloniaProperty[] { TextBlock.PaddingProperty, TextBlock.LetterSpacingProperty, TextBlock.LineHeightProperty })
        {
            if (!setters.ContainsKey(property))
                flat.Setters.Add(new Setter(property, styled.GetValue(property)));
        }

        return flat;
    }

    /// <summary>The five ways of <see cref="Ways"/>, for one text style.</summary>
    internal static ControlTheme?[] Variants(ControlTheme theme)
    {
        var styled = new TextBlock { Text = "x", Theme = theme };
        using var scene = new Scene(styled, themes: [new Wp81Theme()]);

        return
        [
            null,
            Flattened(theme, styled, _ => false),
            Flattened(theme, styled, p => p == TextBlock.ForegroundProperty || p == TextBlock.FontFamilyProperty),
            Flattened(theme, styled, _ => true),
            theme,
        ];
    }

    private static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);

    [AvaloniaFact]
    public void Attach_Time()
    {
        if (Environment.GetEnvironmentVariable(StyleCostBenchmark.Variable) is not { } file)
        {
            return;
        }

#if DEBUG
        Assert.Fail("Run this in Release: a Debug timing would mean nothing.");
#endif
        var rows = new List<string>
        {
            $"| Style | {string.Join(" | ", Ways)} |",
            $"|---|{string.Concat(Ways.Select(_ => "---|"))}",
        };
        foreach (var key in new[] { "BodyTextBlockStyle", "GroupHeaderTextBlockStyle" })
        {
            new Wp81Theme().TryGetResource(key, null, out var found);
            var ways = Variants((ControlTheme)found!);

            var times = ways.Select(_ => new List<double>()).ToArray();
            for (var round = 0; round < WarmUpRounds + Rounds; round++)
            {
                for (var i = 0; i < ways.Length; i++)
                {
                    // Rotate the order, as warm-up flatters whichever comes later.
                    var way = (i + round) % ways.Length;
                    var time = Attach(ways[way]);
                    if (round >= WarmUpRounds)
                        times[way].Add(time);
                }
            }

            var us = times.Select(t => Median(t) * 1000 / Elements).ToArray();
            rows.Add(string.Create(CultureInfo.InvariantCulture, $"| `{key}` | {string.Join(" | ", us.Select(u => u.ToString("0.0", CultureInfo.InvariantCulture)))} |"));
        }

        var when = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        File.AppendAllText(file, $"{Environment.NewLine}{when}, µs per element{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, rows)}{Environment.NewLine}");
    }
}
