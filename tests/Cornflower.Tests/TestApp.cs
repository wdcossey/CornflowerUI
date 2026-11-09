using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Cornflower.Sample.Diagnostics;
using Cornflower.Tests.Performance;

[assembly: AvaloniaTestApplication(typeof(Cornflower.Tests.TestApp))]

namespace Cornflower.Tests;

/// <summary>
/// Headless application used by every <c>[AvaloniaFact]</c> test. Renders with Skia so tests
/// can capture real frames to set beside the emulator's. Installs no theme: a test that needs
/// one adds it. Frames are composed only on a forced render tick (<see cref="ManualRenderTimer"/>).
/// </summary>
public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .AfterSetup(_ => ManualRenderTimer.Install());

    /// <summary>
    /// Turns on Avalonia's diagnostics, which <c>StyleTrace</c> and the Debug line's tests read. Avalonia
    /// reads the switch once, so it's set as the assembly loads, before Avalonia starts. The benchmark
    /// leaves it off, as an app has it, so <see cref="StyleTrace.ChecksFor"/> refuses to run then.
    /// </summary>
    [ModuleInitializer]
    internal static void EnableDiagnostics()
    {
        if (Environment.GetEnvironmentVariable(StyleCostBenchmark.Variable) is null)
        {
            PerformanceMonitor.EnableDiagnostics();
        }
    }
}
