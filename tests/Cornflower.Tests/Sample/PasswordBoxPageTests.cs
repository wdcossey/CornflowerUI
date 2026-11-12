using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's PasswordBox page: in the catalogue by #37, every box on it the phone's PasswordBox, its header, and the length typed shown.</summary>
public class PasswordBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_TextBox_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(PasswordBoxPage));
        Assert.Equal("passwordbox", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(TextBoxPage)));
    }

    [AvaloniaFact]
    public void Every_Box_On_The_Page_Is_The_Phones_PasswordBox()
    {
        var page = new PasswordBoxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<TextBox>().ToList();
        Assert.Equal(4, boxes.Count);
        Assert.All(boxes, box => Assert.Same(scene.Window.FindResource("PasswordBox"), box.Theme));
        Assert.Equal(3, boxes.Count(b => b.GetVisualDescendants().OfType<CheckBox>().Single().IsVisible));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new PasswordBoxPage();
        using var scene = new Scene(page);

        Assert.Equal("passwordbox", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_First_Boxs_Length_Follows_What_Is_Typed()
    {
        var page = new PasswordBoxPage();
        using var scene = new Scene(page);
        var secret = page.FindControl<TextBox>("Secret")!;
        var state = page.FindControl<TextBlock>("State")!;

        Assert.Contains(secret.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "password");
        Assert.Equal("0 characters", state.Text);

        Assert.True(secret.Focus());
        scene.Window.KeyTextInput("h");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("1 character", state.Text);

        scene.Window.KeyTextInput("unter2");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7 characters", state.Text);      // read from the box, never the password
    }
}
