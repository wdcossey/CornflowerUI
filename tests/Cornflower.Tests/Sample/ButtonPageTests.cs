using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's Button page: in the catalogue by #27, every Button on it the theme's, and Click counted.</summary>
public class ButtonPageTests
{
    [AvaloniaFact]
    public void The_Button_Page_Is_In_The_Catalogue_After_The_Theme_Page()
    {
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(ButtonPage));
        Assert.Equal("button", entry.Title);
        Assert.True(SampleShell.Pages.ToList().IndexOf(entry) > 0);
    }

    [AvaloniaFact]
    public void Every_Button_On_The_Page_Has_The_Phones_Template()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);

        var buttons = page.GetVisualDescendants().OfType<Button>().Where(b => b.GetType() == typeof(Button)).ToList();   // not the scroll bar's RepeatButtons
        Assert.Equal(8, buttons.Count);
        Assert.All(buttons, button => Assert.Contains(button.GetVisualDescendants(), v => v is Panel { Name: "Grid" }));
    }

    [AvaloniaFact]
    public void A_Click_Is_Counted()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);
        var clicks = page.FindControl<TextBlock>("Clicks")!;

        page.FindControl<Button>("Default")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        page.FindControl<Button>("Default")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("clicked 2 times", clicks.Text);
    }
}
