using Android.Runtime;
using Avalonia.Android;

namespace Cornflower.Sample.Android;

// No font is added here: the page's text is Selawik, which Wp81Theme bundles.
[global::Android.App.Application]
public class Application : AvaloniaAndroidApplication<App>
{
#if DEBUG
    // Avalonia reads the switch once, so it's set before Avalonia starts: the sample's Debug line
    // reads Avalonia's own meter (docs/performance/README.md).
    static Application() => Cornflower.Sample.Diagnostics.PerformanceMonitor.EnableDiagnostics();
#endif

    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}
