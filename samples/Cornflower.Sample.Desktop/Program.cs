using Avalonia;

namespace Cornflower.Sample.Desktop;

// No font is added here: the pages' text is Selawik, which Wp81Theme bundles.
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Avalonia reads the switch once, so it's set before Avalonia starts: the sample's Debug line
        // reads Avalonia's own meter (docs/performance/README.md).
        Diagnostics.PerformanceMonitor.EnableDiagnostics();
#endif
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Public and named so, for Avalonia's designer and tools that look for it.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
