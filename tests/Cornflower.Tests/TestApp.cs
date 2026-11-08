using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Cornflower.Tests.TestApp))]

namespace Cornflower.Tests;

/// <summary>
/// Headless application used by every <c>[AvaloniaFact]</c> test. Renders with Skia so tests
/// can capture real frames to set beside the emulator's. Installs no theme: a test that needs
/// one adds it.
/// </summary>
public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
