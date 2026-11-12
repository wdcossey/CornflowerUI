using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's HyperlinkButton page: in the catalogue by #29, every HyperlinkButton on it the theme's, and Click counted.</summary>
public class HyperlinkButtonPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Button_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(HyperlinkButtonPage));
        Assert.Equal("hyperlinkbutton", entry.Title);
        Assert.Equal(29, Assert.Single(typeof(HyperlinkButtonPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);   // the issue
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ButtonPage)));
    }

    // The theme's template: a Grid around the Border, and the tilt on the presenter (Simple's has neither).
    [AvaloniaFact]
    public void Every_HyperlinkButton_On_The_Page_Has_The_Phones_Template()
    {
        var page = new HyperlinkButtonPage();
        using var scene = new Scene(page);

        var links = page.GetVisualDescendants().OfType<HyperlinkButton>().ToList();
        Assert.Equal(6, links.Count);
        Assert.All(links, link =>
        {
            Assert.Contains(link.GetVisualDescendants(), v => v is Panel { Name: "Grid" });
            Assert.True(Cornflower.Controls.Tilt.GetIsEnabled(link));
        });
        Assert.NotNull(page.FindControl<HyperlinkButton>("Navigate")!.NavigateUri);
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new HyperlinkButtonPage();
        using var scene = new Scene(page);

        Assert.Equal("hyperlinkbutton", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void A_Click_Is_Counted()
    {
        var page = new HyperlinkButtonPage();
        using var scene = new Scene(page);
        var clicks = page.FindControl<TextBlock>("Clicks")!;

        page.FindControl<HyperlinkButton>("Default")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        page.FindControl<HyperlinkButton>("Default")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("clicked 2 times", clicks.Text);
    }
}
