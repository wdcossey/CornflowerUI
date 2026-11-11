using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Cornflower.Sample;

/// <summary>
/// The sample app: <c>SimpleTheme</c> then <c>Wp81Theme</c>, in Dark. On desktop it shows <see cref="MainWindow"/>, the
/// catalogue beside its rail for Back; on a phone, the same catalogue without the rail, opened at <see cref="StartPage"/>.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The Android intent's string extra that names the start page: <c>tools/android/run.ps1 -Page &lt;title&gt;</c>
    /// starts the app with <c>am start --es page &lt;title&gt;</c>.
    /// </summary>
    public const string PageExtra = "page";

    /// <summary>
    /// The page a phone opens at start, by its catalogue title in any case, or null for the first page. A head sets it
    /// before Avalonia makes the view: the Android head, from its intent's <see cref="PageExtra"/>. A title the
    /// catalogue doesn't have fails the start, naming the titles it does.
    /// </summary>
    public static string? StartPage { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case IActivityApplicationLifetime activity:
                // Read when the activity is made, so after the activity has set StartPage.
                activity.MainViewFactory = PhoneView;
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = PhoneView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>The phone's view: the sample's catalogue, opened at <see cref="StartPage"/>.</summary>
    internal static SampleShell PhoneView() => PhoneView(SampleShell.Pages, StartPage);

    /// <summary>
    /// The phone's view: the catalogue of <paramref name="pages"/> with no rail, as the platform's back request is Back
    /// there, opened at <paramref name="startPage"/>, or the first page if it's null.
    /// </summary>
    internal static SampleShell PhoneView(IReadOnlyList<SamplePageEntry> pages, string? startPage)
    {
        var shell = new SampleShell(pages) { HasBackRail = false };
        if (startPage is null)
            shell.Open(pages[0]);
        else
            shell.Open(startPage);
        return shell;
    }
}
