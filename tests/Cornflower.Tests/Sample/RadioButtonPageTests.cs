using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's RadioButton page: in the catalogue by #32, every RadioButton on it the theme's, its groups, and its choice shown.</summary>
public class RadioButtonPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_HyperlinkButton_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(RadioButtonPage));
        Assert.Equal("radiobutton", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(HyperlinkButtonPage)));
    }

    [AvaloniaFact]
    public void Every_RadioButton_On_The_Page_Has_The_Phones_Template()
    {
        var page = new RadioButtonPage();
        using var scene = new Scene(page);

        var radios = page.GetVisualDescendants().OfType<RadioButton>().ToList();
        Assert.Equal(12, radios.Count);
        Assert.All(radios, radio => Assert.Contains(radio.GetVisualDescendants(), v => v is Ellipse { Name: "CheckBackground" }));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new RadioButtonPage();
        using var scene = new Scene(page);

        Assert.Equal("radiobutton", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_First_Groups_Choice_Is_Shown_And_Moves()
    {
        var page = new RadioButtonPage();
        using var scene = new Scene(page);
        var state = page.FindControl<TextBlock>("State")!;
        Assert.Equal("green", state.Text);           // read from the RadioButtons, not typed in

        page.FindControl<RadioButton>("Blue")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("blue", state.Text);
        Assert.False(page.FindControl<RadioButton>("Green")!.IsChecked);
    }

    [AvaloniaFact]
    public void The_Named_Group_Spans_Its_Two_Panels()
    {
        var page = new RadioButtonPage();
        using var scene = new Scene(page);

        page.FindControl<RadioButton>("Large")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.False(page.FindControl<RadioButton>("Small")!.IsChecked);
        Assert.Equal("green", page.FindControl<TextBlock>("State")!.Text);       // another group, untouched
    }
}
