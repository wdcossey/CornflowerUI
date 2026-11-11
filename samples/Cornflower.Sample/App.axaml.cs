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

    private static string? _startPage;
    private static bool _startPageSet;

    /// <summary>
    /// The page a phone opens at start, by its catalogue title in any case, or null for the first page. A head sets it
    /// before Avalonia makes the view: the Android head, from its intent's <see cref="PageExtra"/>, in its activity's
    /// <c>OnCreate</c>. A title the catalogue doesn't have fails the start, naming the titles it does.
    /// </summary>
    /// <remarks>
    /// Avalonia's activity lifetime takes a view factory with no arguments, and makes the view when the activity is
    /// created; a static set by the activity is how the intent reaches it. <see cref="ActivityView"/> fails if the view
    /// is made before the activity has set this, rather than ignore the page asked for.
    /// </remarks>
    public static string? StartPage
    {
        get => _startPage;
        set
        {
            _startPage = value;
            _startPageSet = true;
        }
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case IActivityApplicationLifetime activity:
                // Made when the activity is created, after its OnCreate has set StartPage.
                activity.MainViewFactory = ActivityView;
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = PhoneView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The Android head's view, <see cref="PhoneView()"/>, once its activity has set <see cref="StartPage"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The activity hasn't set it: Avalonia made the view first.</exception>
    internal static SampleShell ActivityView() =>
        _startPageSet
            ? PhoneView()
            : throw new InvalidOperationException(
                "Avalonia made the phone's view before MainActivity set App.StartPage from its intent's 'page' extra, " +
                "so the page asked for would be ignored. Set it earlier, before base.OnCreate.");

    /// <summary>Forgets <see cref="StartPage"/>, as at a process's start; for a test.</summary>
    internal static void ForgetStartPage()
    {
        _startPage = null;
        _startPageSet = false;
    }

    /// <summary>The phone's view: the sample's catalogue, opened at <see cref="StartPage"/>.</summary>
    internal static SampleShell PhoneView() => PhoneView(SampleShell.Pages, StartPage);

    /// <summary>
    /// The phone's view: the catalogue of <paramref name="pages"/> with no rail, as the platform's back request is Back
    /// there, opened at <paramref name="startPage"/>, or the first page if it's null.
    /// </summary>
    /// <exception cref="ArgumentException">No page has the title <paramref name="startPage"/>.</exception>
    /// <exception cref="InvalidOperationException">There are no pages.</exception>
    internal static SampleShell PhoneView(IReadOnlyList<SamplePageEntry> pages, string? startPage)
    {
        if (pages.Count == 0)
            throw new InvalidOperationException("The sample has no pages to open: a page is a control marked [SamplePage].");

        var shell = new SampleShell(pages) { HasBackRail = false };
        if (startPage is null)
            shell.Open(pages[0]);
        else
            shell.Open(startPage);
        return shell;
    }
}
