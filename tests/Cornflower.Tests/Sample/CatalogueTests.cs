using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornflower.Sample;
using Xunit;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The desktop sample (#9): its window, which opens at the reference profile's view (ADR 0001) and
/// can be resized, and the catalogue it shows, one page per <see cref="SamplePageAttribute"/>
/// found with no registration, with Back to the list.
/// </summary>
public class CatalogueTests
{
    [SamplePage("second b", Order = 2)]
    private sealed class SecondB : Border;

    [SamplePage("second a", Order = 2)]
    private sealed class SecondA : Border;

    [SamplePage("first", Order = 1)]
    private sealed class First : Border;

    [SamplePage("no order")]
    private sealed class NoOrder : Border;

    private sealed class NotAPage : Border;

    [SamplePage("not a control")]
    private sealed class NotAControl;

    [SamplePage("needs an argument")]
    private sealed class NeedsAnArgument(int value) : Border
    {
        public int Value { get; } = value;
    }

    [SamplePage("generic")]
    private sealed class Generic<T> : Border;

    [SamplePage("first")]
    private sealed class AlsoFirst : Border;

    [SamplePage("First")]
    private sealed class FirstInCapitals : Border;

    [SamplePage("a window")]
    private sealed class AWindow : Window;

    [AvaloniaFact]
    public void Pages_Are_Found_By_Their_Attribute_By_Order_Then_Title()
    {
        var pages = SamplePageCatalog.Discover([typeof(NoOrder), typeof(SecondB), typeof(NotAPage), typeof(First), typeof(SecondA)]);

        Assert.Equal(["first", "second a", "second b", "no order"], pages.Select(page => page.Title));
        Assert.IsType<First>(pages[0].Create());
        Assert.IsType<NoOrder>(pages[3].Create());
        Assert.NotSame(pages[0].Create(), pages[0].Create());
    }

    [AvaloniaTheory]
    [InlineData(typeof(NotAControl))]
    [InlineData(typeof(NeedsAnArgument))]
    [InlineData(typeof(Generic<>))]
    [InlineData(typeof(AWindow))]
    public void A_Page_That_Cant_Be_Made_Fails_When_The_Catalogue_Is_Built(Type type)
    {
        var error = Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([type]));
        Assert.Contains(type.FullName!, error.Message);
    }

    // A page is opened by its title, on the list and by tools/real-window/drive.ps1.
    [AvaloniaFact]
    public void Two_Pages_With_One_Title_Fail_When_The_Catalogue_Is_Built()
    {
        var error = Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([typeof(First), typeof(AlsoFirst)]));
        Assert.Contains("'first'", error.Message);
    }

    // drive.ps1 finds a page by its name through UI Automation; two names that differ only in case are too easily one.
    [AvaloniaFact]
    public void Two_Titles_That_Differ_Only_In_Case_Fail_When_The_Catalogue_Is_Built()
    {
        Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([typeof(First), typeof(FirstInCapitals)]));
    }

    [AvaloniaFact]
    public void A_Page_Reads_Its_Title_From_Its_Attribute()
    {
        Assert.Equal("first", SamplePageCatalog.TitleOf(typeof(First)));
        Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.TitleOf(typeof(NotAPage)));
    }

    [AvaloniaFact]
    public void The_Samples_First_Page_Is_The_Theme_Page()
    {
        var first = SampleShell.Pages[0];

        Assert.Equal("theme", first.Title);
        Assert.Equal(typeof(MainView), first.Type);
        Assert.Equal(first.Title, SamplePageCatalog.TitleOf(typeof(MainView)));
    }

    [AvaloniaFact]
    public void The_Window_Opens_At_The_Reference_View_And_Can_Be_Resized()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new Size(Scene.ReferenceWidth + RailWidth, Scene.ReferenceHeight), window.ClientSize);
            Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
            Assert.True(window.CanResize);
            var shell = Assert.IsType<SampleShell>(window.Content);
            Assert.False(shell.IsShowingPage);

            window.Width = 1024;
            window.Height = 768;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Size(1024, 768), shell.Bounds.Size);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void The_List_Has_A_Button_Per_Page_In_The_Catalogues_Order()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);

        Assert.Equal(SampleShell.Pages.Select(page => page.Title), ListButtons(shell).Select(button => button.Content));
    }

    [AvaloniaFact]
    public void A_Button_On_The_List_Opens_Its_Page_And_Back_Shows_The_List()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        var index = shell.Shown;
        var back = shell.FindControl<Button>("BackButton")!;
        Assert.False(back.IsVisible);

        ListButtons(shell).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(shell.IsShowingPage);
        Assert.IsType<MainView>(shell.Shown);
        Assert.True(back.IsVisible);

        back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.False(shell.IsShowingPage);
        Assert.Same(index, shell.Shown);
    }

    // Back is in a rail beside the page, so the page is laid out at the reference view itself, with nothing over it.
    [AvaloniaFact]
    public void A_Page_Has_The_Whole_View_And_Back_Is_Beside_It()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        var page = new Border();
        shell.ShowPage(page);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Rect(0, 0, Scene.ReferenceWidth, Scene.ReferenceHeight), page.Bounds);
        var back = shell.FindControl<Button>("BackButton")!;
        Assert.True(back.Bounds.Height > 0);
        var backInShell = back.TranslatePoint(default, shell)!.Value;
        Assert.True(backInShell.X >= Scene.ReferenceWidth, $"Back starts at {backInShell.X}, over the page");
    }

    // Whether a page is shown is the shell's own state, not how Back looks.
    [AvaloniaFact]
    public void A_Page_Is_Shown_Whatever_Backs_Visibility()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        shell.ShowPage(new Border());
        shell.FindControl<Button>("BackButton")!.IsVisible = false;

        Assert.True(shell.IsShowingPage);
        Assert.True(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // A flyout or drop-down is a popup with a top level of its own; its back request is still this window's.
    [AvaloniaFact]
    public void A_Back_Request_From_A_Popup_On_The_Page_Shows_The_List()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        var inPopup = new Border { Width = 50, Height = 50 };
        var page = new Panel();
        var popup = new Popup { Child = inPopup, PlacementTarget = page };
        page.Children.Add(popup);
        shell.ShowPage(page);
        Dispatcher.UIThread.RunJobs();
        popup.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Back(inPopup));
        Assert.False(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Back_Request_On_A_Page_Shows_The_List_And_Is_Handled()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        shell.ShowPage(new Border());

        Assert.True(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // Unhandled, a phone goes back from the app, and leaves it.
    [AvaloniaFact]
    public void A_Back_Request_On_The_List_Is_Left_Unhandled()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);

        Assert.False(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // Alt+Left is Avalonia's back on desktop, through the same request as a phone's back button.
    [AvaloniaFact]
    public void Alt_Left_Goes_Back()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        shell.ShowPage(new Border());

        scene.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        scene.Window.KeyReleaseQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();

        Assert.False(shell.IsShowingPage);
    }

    // What a page shows over itself goes back first: the shell takes only what's left.
    [AvaloniaFact]
    public void A_Back_Request_A_Page_Took_Keeps_The_Page()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        var page = new Border();
        shell.ShowPage(page);
        scene.Window.AddHandler(TopLevel.BackRequestedEvent, (_, e) => e.Handled = true);

        Assert.True(Back(page));
        Assert.True(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Back_Request_In_Another_Window_Is_Left_Alone()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        shell.ShowPage(new Border());
        using var other = new Scene(new Border());

        Assert.False(Back(other.Window));
        Assert.True(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Shell_That_Has_Left_Its_Window_Stops_Listening()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);
        shell.ShowPage(new Border());
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.False(Back(scene.Window));
        Assert.True(shell.IsShowingPage);
    }

    /// <summary>The shell's rail for Back, beside the page; MainWindow is this much wider than the reference view.</summary>
    private const double RailWidth = 40;

    private static IEnumerable<Button> ListButtons(SampleShell shell) =>
        shell.FindControl<StackPanel>("PageList")!.Children.OfType<Button>();

    /// <summary>What Avalonia does for the platform's back: raises the request, and reads <c>Handled</c>.</summary>
    private static bool Back(Interactive from)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        from.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e.Handled;
    }
}
