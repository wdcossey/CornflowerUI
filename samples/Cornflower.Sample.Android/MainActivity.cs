using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace Cornflower.Sample.Android;

// A fixed Java name, so tools/android/run.ps1 can start it with
// `am start -n io.github.wdcossey.cornflower.sample/cornflower.sample.MainActivity`.
[Activity(
    Name = "cornflower.sample.MainActivity",
    Label = "Cornflower",
    Theme = "@style/Cornflower.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Before Avalonia makes the view, which opens this page: `run.ps1 -Page <title>` starts the activity with
        // `--es page <title>`; with no extra, as from the launcher, the first page.
        App.StartPage = Intent?.GetStringExtra(App.PageExtra);
        base.OnCreate(savedInstanceState);
    }
}
