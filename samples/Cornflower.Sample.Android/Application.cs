using Android.Runtime;
using Avalonia.Android;

namespace Cornflower.Sample.Android;

// No font is added here: the page's text is Selawik, which Wp81Theme bundles.
[global::Android.App.Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}
