using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's Button page: in the catalogue by #27, every Button on it the theme's, and Click counted. And the
/// shell with Button themed: its list is the phone's Buttons, and Back on the rail isn't.
/// </summary>
public class ButtonPageTests
{
    // Back is the sample's chrome: 40 × 40 on the rail, with no phone minimum, overhang or tilt.
    [AvaloniaFact]
    public void Back_On_The_Rail_Is_Chrome_Not_The_Phones_Button()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + 40);
        shell.ShowPage(new Border(), "a page");
        Dispatcher.UIThread.RunJobs();
        var back = shell.FindControl<Button>("BackButton")!;

        Assert.Equal(new Avalonia.Size(40, 40), back.Bounds.Size);
        Assert.False(Cornflower.Controls.Tilt.GetIsEnabled(back));
        Assert.DoesNotContain(back.GetVisualDescendants(), v => v is Panel { Name: "Grid" });
    }

    // Back's chrome: the pointer over it shows PhoneLowBrush behind it, and keyboard focus draws no rectangle.
    [AvaloniaFact]
    public void Back_Shows_The_Pointer_Over_It_And_No_Focus_Rectangle()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + 40);
        shell.ShowPage(new Border(), "a page");
        Dispatcher.UIThread.RunJobs();
        var back = shell.FindControl<Button>("BackButton")!;
        var presenter = back.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First();
        var low = (Avalonia.Media.ISolidColorBrush)scene.Window.FindResource(scene.Window.ActualThemeVariant, "PhoneLowBrush")!;

        scene.Window.MouseMove(back.TranslatePoint(new Avalonia.Point(20, 20), scene.Window)!.Value);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(low.Color, Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(presenter.Background).Color);

        back.Focus(Avalonia.Input.NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(back.IsFocused);
        var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(back);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    [AvaloniaFact]
    public void The_Lists_Buttons_Are_The_Phones()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + 40);

        var list = shell.FindControl<StackPanel>("PageList")!.Children.OfType<Button>().ToList();
        Assert.NotEmpty(list);
        Assert.All(list, button => Assert.True(Cornflower.Controls.Tilt.GetIsEnabled(button)));
    }

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

    // The header reads the page's own [SamplePage] title.
    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);

        Assert.Equal("button", page.FindControl<SamplePageHeader>("Header")!.Title);
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
