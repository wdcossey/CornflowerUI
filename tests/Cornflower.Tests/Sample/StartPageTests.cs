using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornflower.Sample;
using Xunit;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The phone shows the catalogue (Q-phone-catalogue), without the rail, opened at the page the Android intent's
/// <c>page</c> extra names (<c>tools/android/run.ps1 -Page</c>), or the first page.
/// </summary>
public class StartPageTests
{
    [SamplePage("first", Order = 1)]
    private sealed class First : Border;

    [SamplePage("second page", Order = 2)]
    private sealed class Second : Border;

    private static IReadOnlyList<SamplePageEntry> Catalogue => SamplePageCatalog.Discover([typeof(First), typeof(Second)]);

    [AvaloniaTheory]
    [InlineData("second page")]
    [InlineData("Second Page")]
    public void The_Phone_Opens_The_Page_Its_Start_Page_Names(string title)
    {
        var shell = App.PhoneView(Catalogue, title);
        using var scene = new Scene(shell);

        Assert.IsType<Second>(shell.Shown);
    }

    [AvaloniaFact]
    public void With_No_Start_Page_The_Phone_Opens_The_First()
    {
        var shell = App.PhoneView(Catalogue, null);
        using var scene = new Scene(shell);

        Assert.IsType<First>(shell.Shown);
    }

    [AvaloniaFact]
    public void An_Unknown_Start_Page_Fails_Naming_The_Pages_There_Are()
    {
        var error = Assert.Throws<ArgumentException>(() => App.PhoneView(Catalogue, "third"));

        Assert.Equal("The sample has no page titled 'third'. Its pages are: 'first', 'second page'. (Parameter 'title')", error.Message);
    }

    // The device run waits for this line, and checks it names the page it asked for.
    [AvaloniaFact]
    public void The_Start_Pages_Ready_Line_Names_It_And_The_Page_Has_The_Whole_View()
    {
        var lines = new List<string>();
        var shell = App.PhoneView(Catalogue, "second page");
        shell.Report = lines.Add;
        using var scene = new Scene(shell);
        for (var frame = 0; frame < 4; frame++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Assert.False(shell.HasBackRail);
        Assert.Matches("^CORNFLOWER ready open=[0-9]+ms scaling=2.4 view=450x800 raw=1080x1920 safe=0,0,0,0 page=450x800 title=second page$", Assert.Single(lines));
    }

    // With no rail, the platform's back request is Back: to the list, then out of the app.
    [AvaloniaFact]
    public void On_The_Phone_Back_Goes_To_The_List_And_Then_Leaves()
    {
        var shell = App.PhoneView(Catalogue, null);
        using var scene = new Scene(shell);

        Assert.True(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
        Assert.False(Back(scene.Window));
    }

    // The view the app's phone lifetimes are given (Avalonia's lifetimes can't be made outside it), on the sample's
    // catalogue, from the start page the Android head sets from its intent.
    [AvaloniaFact]
    public void The_Apps_Phone_View_Opens_The_Start_Page_The_Head_Set()
    {
        var previous = App.StartPage;
        try
        {
            App.StartPage = "THEME";
            var shell = App.PhoneView();
            using var scene = new Scene(shell);
            Assert.IsType<MainView>(shell.Shown);
            Assert.False(shell.HasBackRail);

            App.StartPage = "no such page";
            var error = Assert.Throws<ArgumentException>(App.PhoneView);
            Assert.Contains("'no such page'", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            App.StartPage = previous;
        }
    }

    /// <summary>What Avalonia does for the platform's back: raises the request, and reads <c>Handled</c>.</summary>
    private static bool Back(Interactive from)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        from.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e.Handled;
    }
}
