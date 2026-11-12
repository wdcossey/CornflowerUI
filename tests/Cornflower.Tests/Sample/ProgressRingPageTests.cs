using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ProgressRing page: in the catalogue by #28, every ring on it the theme's, and the button turning one off and on.</summary>
public class ProgressRingPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Button_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ProgressRingPage));
        Assert.Equal("progressring", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ButtonPage)));
    }

    [AvaloniaFact]
    public void Every_Ring_On_The_Page_Has_The_Phones_Template_And_Its_Size_State()
    {
        var page = new ProgressRingPage();
        using var scene = new Scene(page);

        var rings = page.GetVisualDescendants().OfType<ProgressRing>().ToList();
        Assert.Equal(4, rings.Count);
        Assert.All(rings, ring => Assert.Contains(ring.GetVisualDescendants(), v => v is Grid { Name: "PART_Ring" }));
        Assert.Contains(":small", page.FindControl<ProgressRing>("Default")!.Classes);
        Assert.Contains(":large", page.FindControl<ProgressRing>("Large")!.Classes);
        Assert.Contains(":large", page.FindControl<ProgressRing>("Stretched")!.Classes);
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ProgressRingPage();
        using var scene = new Scene(page);

        Assert.Equal("progressring", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_Button_Turns_The_First_Ring_Off_And_On()
    {
        var page = new ProgressRingPage();
        using var scene = new Scene(page);
        var ring = page.FindControl<ProgressRing>("Default")!;
        var toggle = page.FindControl<Button>("Toggle")!;

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.False(ring.IsActive);
        Assert.Equal("start", toggle.Content);

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(ring.IsActive);
        Assert.Equal("stop", toggle.Content);
    }
}
