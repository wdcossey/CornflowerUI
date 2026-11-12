using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ProgressBar page: in the catalogue by #35, every bar on it the theme's, and its two buttons.</summary>
public class ProgressBarPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_By_Its_Issue()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ProgressBarPage));
        Assert.Equal("progressbar", entry.Title);
        Assert.Equal(35, Assert.Single(typeof(ProgressBarPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);   // the issue
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ProgressRingPage)));
    }

    [AvaloniaFact]
    public void Every_Bar_On_The_Page_Has_The_Phones_Template()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);

        var bars = page.GetVisualDescendants().OfType<ProgressBar>().ToList();
        Assert.Equal(5, bars.Count);
        Assert.All(bars, bar => Assert.Contains(bar.GetVisualDescendants(), v => v is Grid { Name: "PART_EllipseGrid" }));
        Assert.All(bars, bar => Assert.Equal(3.75, bar.Bounds.Height, 0.001));
        Assert.Contains(":indeterminate", page.FindControl<ProgressBar>("Indeterminate")!.Classes);
        Assert.Contains(":indeterminate", page.FindControl<ProgressBar>("LightIndeterminate")!.Classes);
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);

        Assert.Equal("progressbar", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_Button_Steps_The_Value_By_10_And_Back_To_0()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var bar = page.FindControl<ProgressBar>("Determinate")!;
        var step = page.FindControl<Button>("Step")!;

        step.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(50, bar.Value);
        for (var i = 0; i < 5; i++)
        {
            step.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        Assert.Equal(100, bar.Value);
        step.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(0, bar.Value);
    }

    [AvaloniaFact]
    public void The_Other_Button_Turns_The_Indeterminate_Bar_Off_And_On()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var bar = page.FindControl<ProgressBar>("Indeterminate")!;
        var toggle = page.FindControl<Button>("Toggle")!;

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.False(bar.IsIndeterminate);
        Assert.Equal("start", toggle.Content);

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.IsIndeterminate);
        Assert.Equal("stop", toggle.Content);
    }
}
