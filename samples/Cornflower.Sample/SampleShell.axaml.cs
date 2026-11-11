using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Reactive;
using Cornflower.Sample.Diagnostics;

namespace Cornflower.Sample;

/// <summary>
/// The sample's catalogue: the list of <see cref="SamplePageAttribute"/> pages, a page, and Back.
/// <see cref="MainWindow"/> holds one on desktop, and <see cref="App"/> shows one on a phone. Back is the Back button,
/// or Avalonia's back request (Alt+Left on desktop, the back button or gesture on a phone) once nothing else on its
/// route has taken it.
/// <para>
/// The shell times every page it opens, from navigation to the page's first drawn frame, and then writes the page's
/// <c>CORNFLOWER ready</c> line to standard output (logcat's <c>DOTNET</c> tag on Android), which
/// <c>tools/android/run.ps1</c> and <c>tools/real-window/drive.ps1</c> read. A page has no timing code of its own. In a
/// Debug build it also shows what the page cost to open, along the bottom (<see cref="PerformanceMonitor"/>).
/// </para>
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>What every ready line starts with.</summary>
    internal const string ReadyMarker = "CORNFLOWER ready";

    private static IReadOnlyList<SamplePageEntry>? _pages;

    /// <summary>
    /// Every page in the sample, found by its attribute. Found on first use rather than in a static
    /// initialiser, so that a page that can't be made fails with its own message, not a type initialiser's.
    /// </summary>
    internal static IReadOnlyList<SamplePageEntry> Pages => _pages ??= SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private TopLevel? _top;
    private IDisposable? _backRequests;

    /// <summary>The page shown, from its navigation until the shell leaves it; null on the list.</summary>
    private Opening? _opening;

#if DEBUG
    /// <summary>Avalonia's layout and render times, from each navigation; made when a page is first opened.</summary>
    private PerformanceMonitor? _performance;
#endif

    /// <summary>The sample's catalogue, <see cref="Pages"/>.</summary>
    public SampleShell()
        : this(Pages)
    {
    }

    /// <summary>A catalogue of <paramref name="catalogue"/>, for a test's own pages.</summary>
    internal SampleShell(IReadOnlyList<SamplePageEntry> catalogue)
    {
        Catalogue = catalogue;
        InitializeComponent();
        foreach (var page in catalogue)
        {
            var open = new Button { Content = page.Title, HorizontalAlignment = HorizontalAlignment.Stretch };
            open.Click += (_, _) => Open(page);
            PageList.Children.Add(open);
        }
    }

    /// <summary>The pages on the list.</summary>
    internal IReadOnlyList<SamplePageEntry> Catalogue { get; }

    /// <summary>Where each ready line goes. Standard output; a test replaces it.</summary>
    internal Action<string> Report { get; set; } = Console.WriteLine;

    /// <summary>
    /// Whether Back has a rail of its own beside the page, as on desktop; on by default. Off, as on a phone, the page
    /// has the shell's whole width, and the platform's back request is Back.
    /// </summary>
    public bool HasBackRail
    {
        get => Rail.IsVisible;
        set => Rail.IsVisible = value;
    }

    /// <summary>Whether a page is shown, rather than the list.</summary>
    public bool IsShowingPage => !ReferenceEquals(Host.Content, Index);

    /// <summary>The page shown, or the list.</summary>
    internal object? Shown => Host.Content;

    /// <summary>Shows the list.</summary>
    public void ShowIndex()
    {
        _opening = null;
        HidePerformance();
        BackButton.IsVisible = false;
        Host.Content = Index;
    }

    /// <summary>
    /// Makes the page called <paramref name="title"/> and shows it, as its button on the list does. Titles are matched
    /// without regard to case, as the catalogue already keeps apart titles that differ only in case.
    /// </summary>
    /// <exception cref="ArgumentException">No page has the title; the message lists those there are.</exception>
    public void Open(string title)
    {
        var page = Catalogue.FirstOrDefault(entry => string.Equals(entry.Title, title, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                $"The sample has no page titled '{title}'. Its pages are: {string.Join(", ", Catalogue.Select(entry => $"'{entry.Title}'"))}.",
                nameof(title));
        Open(page);
    }

    /// <summary>Makes <paramref name="page"/> and shows it, timed from before it's made.</summary>
    public void Open(SamplePageEntry page)
    {
        var started = Begin();
        Show(page.Create(), page.Title, started);
    }

    /// <summary>
    /// Shows <paramref name="page"/>, with Back to the list, timed from now under its <see cref="SamplePageAttribute"/>
    /// title, or its type's name if it has none.
    /// </summary>
    public void ShowPage(Control page)
    {
        var started = Begin();
        Show(page, page.GetType().GetCustomAttribute<SamplePageAttribute>()?.Title ?? page.GetType().Name, started);
    }

    /// <summary>The ready line: the time to open, what the device reports, and the page's title, last, as it can have spaces.</summary>
    internal static string ReadyLine(TimeSpan opened, string facts, string title) =>
        string.Create(CultureInfo.InvariantCulture, $"{ReadyMarker} open={opened.TotalMilliseconds:0}ms {facts} title={title}");

    /// <summary>Navigation: where a page's time starts, before it's made, because styles apply as soon as it's attached.</summary>
    private long Begin()
    {
#if DEBUG
        _performance ??= new PerformanceMonitor();
        _performance.Begin();
#endif
        return Stopwatch.GetTimestamp();
    }

    private void Show(Control page, string title, long started)
    {
        HidePerformance();
        BackButton.IsVisible = true;
        Host.Content = page;
        _opening = new Opening(page, title, started);
        AwaitFirstFrame();
    }

    /// <summary>
    /// Asks for the frame after the one that first draws the page shown, if the shell is in a window and the page
    /// hasn't been reported; a shell that isn't in one yet, as on a phone at start, asks once it is.
    /// </summary>
    private void AwaitFirstFrame()
    {
        if (_opening is not { Reported: false } opening || _top is not { } top)
            return;

        // The callback runs before the next frame is drawn; the one it asks for runs after it.
        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ => Drawn(opening, top)));
    }

    private void Drawn(Opening opening, TopLevel top)
    {
        // Frame requests can't be cancelled: a page that has gone, or a shell that has left its window, says nothing.
        if (!ReferenceEquals(opening, _opening) || opening.Reported || !ReferenceEquals(top, _top))
            return;

        opening.Reported = true;
        var elapsed = Stopwatch.GetElapsedTime(opening.Started);
        Report(ReadyLine(elapsed, DeviceFacts.Of(top, opening.Page), opening.Title));
#if DEBUG
        // None if the shell left its window and came back while the page was opening: its times went with it.
        if (_performance is not null)
        {
            PerformanceLine.Text = _performance.Read(opening.Title, opening.Page, elapsed);
            PerformanceLine.IsVisible = true;
        }
#endif
    }

    private void HidePerformance()
    {
        PerformanceLine.IsVisible = false;
        PerformanceLine.Text = null;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        // After every handler on the route, so that anything a page shows over itself goes back first.
        _backRequests = TopLevel.BackRequestedEvent.RouteFinished.Subscribe(new AnonymousObserver<RoutedEventArgs>(OnBackRequestFinished));
        AwaitFirstFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _backRequests?.Dispose();
        _backRequests = null;
        _top = null;
#if DEBUG
        // Its listener hears every meter in the process until it's disposed.
        _performance?.Dispose();
        _performance = null;
#endif
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// What's left of a back request once everything else has seen it: from a page, the list. On the
    /// list it's left alone, so that a phone leaves the app.
    /// </summary>
    private void OnBackRequestFinished(RoutedEventArgs e)
    {
        // RouteFinished is for every top level's request, so only this one's.
        if (e.Handled || !IsShowingPage || _top is null || e.Source is not Visual source || WindowOf(source) != _top)
            return;

        e.Handled = true;
        ShowIndex();
    }

    /// <summary>The window <paramref name="source"/> is in: for a popup, the window it was opened from.</summary>
    private static TopLevel? WindowOf(Visual source)
    {
        var top = TopLevel.GetTopLevel(source);
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent && parent != top)
            top = parent;
        return top;
    }

    /// <summary>A page shown: what it is, what it's called, when its navigation began, and whether its line is written.</summary>
    private sealed class Opening(Control page, string title, long started)
    {
        public Control Page { get; } = page;

        public string Title { get; } = title;

        public long Started { get; } = started;

        public bool Reported { get; set; }
    }
}
