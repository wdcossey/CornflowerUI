using System.Text.RegularExpressions;

namespace Cornflower.Tests.Repository;

/// <summary>
/// No test calls <c>AvaloniaHeadlessPlatform.ForceRenderTimerTick</c>: under <see cref="ManualRenderTimer"/>
/// it ticks a timer that no longer drives the render loop, so it does nothing, and a test using it would
/// pass or fail for the wrong reason. Tests tick with <see cref="ManualRenderTimer.ForceTick"/>.
/// </summary>
internal static partial class RenderTickGuard
{
    [GeneratedRegex(@"\bForceRenderTimerTick\s*\(")]
    private static partial Regex PlatformTick();

    /// <summary>One message per call; empty when there are none.</summary>
    internal static IReadOnlyList<string> Check(IEnumerable<(string Path, Func<Stream> Open)> files)
    {
        var problems = new List<string>();
        foreach (var (path, open) in files)
        {
            if (!path.StartsWith("tests/", StringComparison.Ordinal) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            using var reader = new StreamReader(open());
            var number = 0;
            while (reader.ReadLine() is { } text)
            {
                number++;
                if (PlatformTick().IsMatch(text))
                    problems.Add($"{path}:{number}: ForceRenderTimerTick ticks nothing here; use ManualRenderTimer.ForceTick()");
            }
        }

        return problems;
    }
}
