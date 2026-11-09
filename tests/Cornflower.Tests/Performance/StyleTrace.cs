using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Sample.Diagnostics;

namespace Cornflower.Tests.Performance;

/// <summary>
/// Which global style rules an element is checked against, from Avalonia's own diagnostics. See
/// docs/performance/README.md. Counts are exact, so CI can rely on them. The time of each check
/// isn't: tracing costs more than the check does. <see cref="StyleCostBenchmark"/> measures time,
/// with tracing off.
/// </summary>
internal static class StyleTrace
{
    private const string Source = "Avalonia.Diagnostic.Source";
    private const string AttachingStyle = "Avalonia.AttachingStyle";

    /// <summary>One rule checked against one element.</summary>
    internal readonly record struct Check(string Selector, string Result)
    {
        /// <summary>The check got past the element's type, which costs several times what a rejected type does.</summary>
        public bool ReachedElement => Result != "NeverThisType";

        /// <summary>A comma makes a rule cost as much as that many rules.</summary>
        public IReadOnlyList<string> Alternatives => StyleCensus.SplitAlternatives(Selector);
    }

    /// <summary>The names of Cornflower's own controls, which stop an ancestor walk (<see cref="WalksAncestors(string)"/>).</summary>
    internal static readonly IReadOnlySet<string> Ours = typeof(CornflowerClock).Assembly.GetExportedTypes()
        .Where(type => type.IsSubclassOf(typeof(StyledElement)))
        .Select(type => type.Name)
        .ToHashSet(StringComparer.Ordinal);

    // Avalonia reads the switch once, so it has to be set before Avalonia starts. The benchmark leaves it off, as an app has it.
    [ModuleInitializer]
    internal static void Enable()
    {
        if (Environment.GetEnvironmentVariable(StyleCostBenchmark.Variable) is null)
        {
            PerformanceMonitor.EnableDiagnostics();
        }
    }

    /// <summary>
    /// The checks for one element, attached to a window that's already open with
    /// <paramref name="themes"/> as its only <c>Styles</c>, so nothing else is counted.
    /// </summary>
    public static IReadOnlyList<Check> ChecksFor(Control element, params IStyle[] themes)
    {
        using var scene = new Scene(new Panel(), themes: themes);
        var host = (Panel)scene.Window.Content!;
        var checks = new List<Check>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == AttachingStyle)
                {
                    checks.Add(new Check(
                        activity.GetTagItem("Style")?.ToString() ?? string.Empty,
                        activity.GetTagItem("SelectorResult")?.ToString() ?? string.Empty));
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        host.Children.Add(element);
        Dispatcher.UIThread.RunJobs();
        return checks;
    }

    private static string WithoutSpacedCombinators(string alternative) =>
        alternative.Replace(" > ", ">", StringComparison.Ordinal).Replace(" /template/ ", ">", StringComparison.Ordinal);

    /// <summary>The index of the last of these outside brackets, or -1.</summary>
    private static int LastOutsideBrackets(string text, params char[] wanted)
    {
        var depth = 0;
        var inValue = false;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            switch (text[i])
            {
                case ']':
                    inValue = true;
                    break;
                case '[':
                    inValue = false;
                    break;
                case ')' when !inValue:
                    depth++;
                    break;
                case '(' when !inValue:
                    depth--;
                    break;
                default:
                    if (depth == 0 && !inValue && wanted.Contains(text[i]))
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <inheritdoc cref="WalksAncestors(string, IReadOnlySet{string})"/>
    public static bool WalksAncestors(string alternative) => WalksAncestors(alternative, Ours);

    /// <summary>
    /// Whether a common element can get as far as a descendant combinator, which then looks at
    /// every ancestor. One of <paramref name="ours"/> after it stops that, as in
    /// <c>Pivot PivotItem &gt; TextBlock</c>, where only a pivot item's child gets that far: our
    /// controls are rare. Avalonia's own types aren't, because Cornflower themes <c>Button</c> and
    /// <c>TextBlock</c> directly. So does a name on the element, because it can't change.
    /// </summary>
    public static bool WalksAncestors(string alternative, IReadOnlySet<string> ours)
    {
        var parts = WithoutSpacedCombinators(alternative);
        var space = LastOutsideBrackets(parts, ' ');
        if (space < 0)
        {
            return false;
        }

        var after = parts[(space + 1)..];
        var subject = after[(LastOutsideBrackets(after, '>') + 1)..];
        return !subject.Contains('#', StringComparison.Ordinal)
            && !after.Split('>').Any(part => ours.Contains(TypeName(part)));
    }

    /// <summary>The type a part of a selector names, without its namespace prefix, classes, pseudoclasses, name or properties.</summary>
    private static string TypeName(string part)
    {
        var end = part.IndexOfAny(['.', ':', '#', '[']);
        var type = end < 0 ? part : part[..end];
        return type[(type.IndexOf('|') + 1)..];
    }

    /// <summary>
    /// Whether an alternative gets past the type of an element, judged alone: a rule reports one result for all its commas.
    /// </summary>
    public static bool Reaches(string alternative, Type element)
    {
        var parts = WithoutSpacedCombinators(alternative);
        var subject = parts[(LastOutsideBrackets(parts, '>', ' ') + 1)..];
        const string Is = ":is(";
        if (subject.StartsWith(Is, StringComparison.Ordinal))
        {
            var name = subject[Is.Length..subject.IndexOf(')', StringComparison.Ordinal)];
            for (var type = element; type is not null; type = type.BaseType)
            {
                if (type.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        var target = TypeName(subject);
        return target.Length == 0 || target == element.Name;
    }
}
