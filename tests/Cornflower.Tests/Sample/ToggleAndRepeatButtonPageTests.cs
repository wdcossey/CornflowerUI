using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's ToggleButton and RepeatButton pages: in the catalogue by #30, every one the page places the theme's, its
/// header from its title, and what it does on a click. The scroll viewer's RepeatButtons keep Simple's theme.
/// </summary>
public class ToggleAndRepeatButtonPageTests
{
    /// <summary>The controls of type <typeparamref name="T"/> exactly that the page places itself, not those in templates.</summary>
    private static List<T> Placed<T>(Control page) where T : Button =>
        page.GetVisualDescendants().OfType<T>().Where(b => b.GetType() == typeof(T) && b.TemplatedParent is null).ToList();

    [AvaloniaFact]
    public void Both_Pages_Are_In_The_Catalogue_After_The_Button_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var button = pages.FindIndex(p => p.Type == typeof(ButtonPage));
        var toggle = Assert.Single(pages, p => p.Type == typeof(ToggleButtonPage));
        var repeat = Assert.Single(pages, p => p.Type == typeof(RepeatButtonPage));

        Assert.Equal("togglebutton", toggle.Title);
        Assert.Equal("repeatbutton", repeat.Title);
        Assert.True(pages.IndexOf(toggle) > button && pages.IndexOf(repeat) > button);
    }

    [AvaloniaFact]
    public void Every_ToggleButton_On_Its_Page_Has_The_Phones_Template()
    {
        var page = new ToggleButtonPage();
        using var scene = new Scene(page);

        var toggles = Placed<ToggleButton>(page);
        Assert.Equal(10, toggles.Count);
        Assert.All(toggles, t => Assert.Contains(t.GetVisualDescendants(), v => v is Border { Name: "EnabledBackground" }));
        Assert.Equal("togglebutton", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void Every_RepeatButton_On_Its_Page_Has_The_Phones_Template_And_The_Scroll_Bars_Keep_Simples()
    {
        var page = new RepeatButtonPage();
        using var scene = new Scene(page);

        var repeats = Placed<RepeatButton>(page);
        Assert.Equal(6, repeats.Count);
        Assert.All(repeats, r =>
        {
            Assert.Contains(r.GetVisualDescendants(), v => v is Border { Name: "Border" });
            Assert.True(PhonePress.GetIsEnabled(r));
        });
        Assert.All(page.GetVisualDescendants().OfType<ScrollBar>().SelectMany(b => b.GetVisualDescendants().OfType<RepeatButton>()),
            r => Assert.False(PhonePress.GetIsEnabled(r)));
        Assert.Equal("repeatbutton", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void Checking_The_Default_Says_So()
    {
        var page = new ToggleButtonPage();
        using var scene = new Scene(page);
        var toggle = page.FindControl<ToggleButton>("Default")!;

        toggle.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("checked", page.FindControl<TextBlock>("State")!.Text);

        toggle.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("unchecked", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void More_And_Less_Count()
    {
        var page = new RepeatButtonPage();
        using var scene = new Scene(page);
        var more = page.FindControl<RepeatButton>("More")!;
        var less = page.FindControl<RepeatButton>("Less")!;

        more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        less.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("1", page.FindControl<TextBlock>("Count")!.Text);
    }
}
