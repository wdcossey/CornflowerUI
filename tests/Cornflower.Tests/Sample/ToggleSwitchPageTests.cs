using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ToggleSwitch page: in the catalogue by #33, every switch on it the theme's, its header, and its state shown.</summary>
public class ToggleSwitchPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_Between_The_RadioButton_And_Slider_Pages()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ToggleSwitchPage));
        Assert.Equal("toggleswitch", entry.Title);
        var at = pages.IndexOf(entry);
        Assert.True(at > pages.FindIndex(page => page.Type == typeof(RadioButtonPage)));
        Assert.True(at < pages.FindIndex(page => page.Type == typeof(SliderPage)));
    }

    [AvaloniaFact]
    public void Every_ToggleSwitch_On_The_Page_Has_The_Phones_Template()
    {
        var page = new ToggleSwitchPage();
        using var scene = new Scene(page);

        var switches = page.GetVisualDescendants().OfType<ToggleSwitch>().ToList();
        Assert.Equal(8, switches.Count);
        Assert.All(switches, toggle => Assert.Contains(toggle.GetVisualDescendants(), v => v is Rectangle { Name: "SwitchKnob" }));
        Assert.Equal(3, switches.Count(s => !s.IsEnabled));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ToggleSwitchPage();
        using var scene = new Scene(page);

        Assert.Equal("toggleswitch", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_First_Switchs_Header_Is_Shown_And_Its_State_Follows_It()
    {
        var page = new ToggleSwitchPage();
        using var scene = new Scene(page);
        var wifi = page.FindControl<ToggleSwitch>("WiFi")!;
        var state = page.FindControl<TextBlock>("State")!;

        Assert.Contains(wifi.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "wi-fi");
        Assert.Equal("wi-fi is off", state.Text);

        wifi.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("wi-fi is on", state.Text);
    }
}
