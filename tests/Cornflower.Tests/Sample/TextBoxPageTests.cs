using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's TextBox page: in the catalogue by #36, every TextBox on it the theme's, its header, and what is typed shown.</summary>
public class TextBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ProgressBar_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(TextBoxPage));
        Assert.Equal("textbox", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ProgressBarPage)));
    }

    [AvaloniaFact]
    public void Every_TextBox_On_The_Page_Has_The_Phones_Template()
    {
        var page = new TextBoxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<TextBox>().ToList();
        Assert.Equal(7, boxes.Count);
        Assert.All(boxes, box => Assert.Contains(box.GetVisualDescendants(), v => v is Border { Name: "BorderElement" }));
        Assert.Equal(3, boxes.Count(b => !b.IsEnabled));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new TextBoxPage();
        using var scene = new Scene(page);

        Assert.Equal("textbox", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_First_TextBoxs_Header_Is_Shown_And_What_Is_Typed_Follows_It()
    {
        var page = new TextBoxPage();
        using var scene = new Scene(page);
        var name = page.FindControl<TextBox>("YourName")!;
        var state = page.FindControl<TextBlock>("State")!;

        Assert.Contains(name.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "name");
        Assert.Equal("-", state.Text);

        Assert.True(name.Focus());
        scene.Window.KeyTextInput("Ada");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Ada", state.Text);       // read from the TextBox, not typed in

        name.Text = "";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("-", state.Text);
    }
}
