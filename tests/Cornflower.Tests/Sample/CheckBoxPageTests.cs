using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's CheckBox page: in the catalogue by #31, every CheckBox on it the theme's, and its state shown.</summary>
public class CheckBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ProgressRing_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(CheckBoxPage));
        Assert.Equal("checkbox", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ProgressRingPage)));
    }

    [AvaloniaFact]
    public void Every_CheckBox_On_The_Page_Has_The_Phones_Template()
    {
        var page = new CheckBoxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<CheckBox>().ToList();
        Assert.Equal(8, boxes.Count);
        Assert.All(boxes, box => Assert.Contains(box.GetVisualDescendants(), v => v is Border { Name: "CheckBackground" }));
        Assert.Null(page.FindControl<CheckBox>("ThreeState")!.IsChecked);
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new CheckBoxPage();
        using var scene = new Scene(page);

        Assert.Equal("checkbox", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_Default_CheckBoxs_State_Is_Shown()
    {
        var page = new CheckBoxPage();
        using var scene = new Scene(page);
        var state = page.FindControl<TextBlock>("State")!;

        page.FindControl<CheckBox>("Default")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("checked", state.Text);
    }
}
