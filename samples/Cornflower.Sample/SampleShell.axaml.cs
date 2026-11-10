using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Reactive;

namespace Cornflower.Sample;

/// <summary>
/// The sample's catalogue: the list of <see cref="SamplePageAttribute"/> pages, a page, and Back.
/// <see cref="MainWindow"/> holds one on desktop. Back is the Back button, or Avalonia's back
/// request (Alt+Left on desktop) once nothing else on its route has taken it.
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>Every page in the sample, found by its attribute.</summary>
    internal static readonly IReadOnlyList<SamplePageEntry> Pages =
        SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private TopLevel? _top;
    private IDisposable? _backRequests;

    public SampleShell()
    {
        InitializeComponent();
        foreach (var page in Pages)
        {
            var open = new Button { Content = page.Title, HorizontalAlignment = HorizontalAlignment.Stretch };
            open.Click += (_, _) => ShowPage(page.Create());
            PageList.Children.Add(open);
        }
    }

    /// <summary>Whether a page is shown, rather than the list.</summary>
    public bool IsShowingPage => BackButton.IsVisible;

    /// <summary>The page shown, or the list.</summary>
    internal object? Shown => Host.Content;

    /// <summary>Shows the list.</summary>
    public void ShowIndex()
    {
        BackButton.IsVisible = false;
        Host.Content = Index;
    }

    /// <summary>Shows <paramref name="page"/>, with Back to the list.</summary>
    public void ShowPage(Control page)
    {
        BackButton.IsVisible = true;
        Host.Content = page;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        // After every handler on the route, so that anything a page shows over itself goes back first.
        _backRequests = TopLevel.BackRequestedEvent.RouteFinished.Subscribe(new AnonymousObserver<RoutedEventArgs>(OnBackRequestFinished));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _backRequests?.Dispose();
        _backRequests = null;
        _top = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// What's left of a back request once everything else has seen it: from a page, the list. On the
    /// list it's left alone, so that a phone leaves the app.
    /// </summary>
    private void OnBackRequestFinished(RoutedEventArgs e)
    {
        // RouteFinished is for every top level's request, so only this one's.
        if (e.Handled || !IsShowingPage || _top is null || e.Source is not Visual source || TopLevel.GetTopLevel(source) != _top)
            return;

        e.Handled = true;
        ShowIndex();
    }
}
