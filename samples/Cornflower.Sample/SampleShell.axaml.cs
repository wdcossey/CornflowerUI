using System.Diagnostics;
using System.Globalization;
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
/// Debug build it also writes what the page cost to open, a <c>CORNFLOWER cost</c> line, and shows it up the rail
/// (<see cref="PerformanceMonitor"/>).
/// </para>
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>What every ready line starts with.</summary>
    internal const string ReadyMarker = "CORNFLOWER ready";

    /// <summary>What the Debug line starts with, written after the page's ready line.</summary>
    internal const string CostMarker = "CORNFLOWER cost";

    private static IReadOnlyList<SamplePageEntry>? _pages;

    /// <summary>
    /// Every page in the sample, found by its attribute. Found on first use rather than in a static
    /// initialiser, so that a page that can't be made fails with its own message, not a type initialiser's.
    /// </summary>
    internal static IReadOnlyList<SamplePageEntry> Pages => _pages ??= SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private TopLevel? _top;
    private IDisposable? _backRequests;

    /// <summary>
    /// The page being opened, from its navigation until its ready line is written, or until the shell leaves it or its
    /// window; then null.
    /// </summary>
    private Opening? _opening;

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

    /// <summary>Where each ready line, and in Debug each cost line, goes. Standard output; a test replaces it.</summary>
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
        Abandon();
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

    /// <summary>Makes <paramref name="page"/> and shows it, timed from before it's made, because styles apply as soon as it's attached.</summary>
    public void Open(SamplePageEntry page)
    {
        var started = Stopwatch.GetTimestamp();
        Show(page.Create(), page.Title, started);
    }

    /// <summary>Shows <paramref name="page"/>, made by a test, under <paramref name="title"/>, timed from now.</summary>
    internal void ShowPage(Control page, string title) => Show(page, title, Stopwatch.GetTimestamp());

    /// <summary>The ready line: the time to open, what the device reports, and the page's title, last, as it can have spaces.</summary>
    internal static string ReadyLine(TimeSpan opened, string facts, string title) =>
        string.Create(CultureInfo.InvariantCulture, $"{ReadyMarker} open={opened.TotalMilliseconds:0}ms {facts} title={title}");

    private void Show(Control page, string title, long started)
    {
        Abandon();
        HidePerformance();
        BackButton.IsVisible = true;
        Host.Content = page;
        _opening = new Opening(page, title, started);
        AwaitFirstFrame();
    }

    /// <summary>Leaves the page being opened unreported, and stops measuring it.</summary>
    private void Abandon()
    {
        _opening?.Dispose();
        _opening = null;
    }

    /// <summary>
    /// Asks for the frame after the one that first draws the page being opened, if the shell is in a window; a shell
    /// that isn't in one yet, as on a phone at start, asks once it is.
    /// </summary>
    private void AwaitFirstFrame()
    {
        if (_opening is not { } opening || _top is not { } top)
            return;

#if DEBUG
        // Only now, in a window, so that a shell never shown has no listener; layout and render can't come before.
        opening.Measure();
#endif
        // The callback runs before the next frame is drawn; the one it asks for runs after it.
        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ => Drawn(opening, top)));
    }

    private void Drawn(Opening opening, TopLevel top)
    {
        // Frame requests can't be cancelled: a page left, or a shell that has left its window since, says nothing.
        if (!ReferenceEquals(opening, _opening) || !ReferenceEquals(top, _top))
            return;

        _opening = null;
        var elapsed = Stopwatch.GetElapsedTime(opening.Started);
        Report(ReadyLine(elapsed, DeviceFacts.Of(top, opening.Page), opening.Title));
#if DEBUG
        var cost = opening.Read(elapsed);
        Report($"{CostMarker} {cost}");
        PerformanceLine.Text = cost;
        PerformanceLine.IsVisible = true;
#endif
        opening.Dispose();
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
        // A page still opening when its shell leaves the window is never reported: its time would include the gap.
        Abandon();
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

    /// <summary>A page being opened: what it is, what it's called, when its navigation began, and in Debug its cost.</summary>
    private sealed class Opening(Control page, string title, long started) : IDisposable
    {
        public Control Page { get; } = page;

        public string Title { get; } = title;

        public long Started { get; } = started;

#if DEBUG
        /// <summary>Avalonia's layout and render times for this page; a process-wide listener, so disposed with it.</summary>
        private PerformanceMonitor? _performance;

        public void Measure() => _performance ??= new PerformanceMonitor();

        public string Read(TimeSpan opened) => _performance!.Read(Title, Page, opened);

        public void Dispose()
        {
            _performance?.Dispose();
            _performance = null;
        }
#else
        public void Dispose()
        {
        }
#endif
    }
}
