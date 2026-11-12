using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's Slider page: in the catalogue by #34, every Slider on it the theme's, its header, and its value shown.</summary>
public class SliderPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_RadioButton_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(SliderPage));
        Assert.Equal("slider", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(RadioButtonPage)));
    }

    [AvaloniaFact]
    public void Every_Slider_On_The_Page_Has_The_Phones_Template()
    {
        var page = new SliderPage();
        using var scene = new Scene(page);

        var sliders = page.GetVisualDescendants().OfType<Slider>().ToList();
        Assert.Equal(8, sliders.Count);
        Assert.All(sliders, slider => Assert.Contains(slider.GetVisualDescendants(), v => v is Rectangle
        {
            Name: "HorizontalTrackRect" or "VerticalTrackRect",
        }));
        Assert.Equal(2, sliders.Count(s => s.Orientation == Orientation.Vertical));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new SliderPage();
        using var scene = new Scene(page);

        Assert.Equal("slider", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_First_Sliders_Header_Is_Shown_And_Its_Value_Follows_It()
    {
        var page = new SliderPage();
        using var scene = new Scene(page);
        var volume = page.FindControl<Slider>("Volume")!;
        var state = page.FindControl<TextBlock>("State")!;

        Assert.Contains(volume.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "volume");
        Assert.Equal("30", state.Text);           // read from the Slider, not typed in

        volume.Value = 72.4;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("72", state.Text);

        volume.Value = 72.5;                       // a half up, not to the even number
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("73", state.Text);
    }
}
