using System.Text;

namespace Cornflower.Tests.Repository;

/// <summary>
/// No test ticks the platform's render timer, which no longer drives anything. The last test scans the real
/// repository; the others prove the check on files made in memory.
/// </summary>
public class RenderTickGuardTests
{
    // Built in pieces, so this file doesn't hold the call it looks for.
    private const string PlatformTick = "AvaloniaHeadlessPlatform.ForceRender" + "TimerTick();";

    private static IReadOnlyList<string> Check(string path, string content) =>
        RenderTickGuard.Check([(path, () => new MemoryStream(Encoding.UTF8.GetBytes(content)))]);

    [Fact]
    public void A_Platform_Tick_In_A_Test_Fails() =>
        Assert.Contains("ManualRenderTimer.ForceTick", Assert.Single(Check("tests/Cornflower.Tests/Harness/X.cs", "        " + PlatformTick)));

    [Fact]
    public void A_Manual_Tick_Or_A_Mention_Passes() =>
        Assert.Empty(Check("tests/Cornflower.Tests/Harness/X.cs", "ManualRenderTimer.ForceTick();\n/// <c>AvaloniaHeadlessPlatform.ForceRenderTimerTick</c> ticks nothing."));

    [Fact]
    public void The_Tests_Never_Tick_The_Platforms_Timer() =>
        Assert.Empty(RenderTickGuard.Check(MicrosoftFileGuard.RepositoryFiles()));
}
