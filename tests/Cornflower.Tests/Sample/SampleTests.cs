using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Themes.Wp81;
using Xunit;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample app every head runs (<c>samples/Cornflower.Sample</c>): its theme set-up, and its one
/// page drawn by the theme's keys. The device run (<c>tools/android/run.ps1</c>) reads the page's
/// ready line, so its format is held here too.
/// </summary>
public class SampleTests
{
    [AvaloniaFact]
    public void The_Sample_Requests_Dark_With_Wp81Theme_Over_SimpleTheme()
    {
        var app = new App();
        app.Initialize();

        Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
        Assert.Collection(app.Styles,
            first => Assert.IsType<SimpleTheme>(first),
            second => Assert.IsType<Wp81Theme>(second));
    }

    [AvaloniaTheory]
    [InlineData("Dark", 0xFF000000u)]
    [InlineData("Light", 0xFFFFFFFFu)]
    public void The_Page_Is_The_Variants_Page_Background(string variant, uint expected)
    {
        using var scene = new Scene(new MainView(), variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
        using var frame = scene.Capture();

        // The left-hand margin, beside the content: nothing but the page. (The right-hand one has
        // SimpleTheme's scroll bar until ScrollViewer is themed.)
        Assert.Equal(Color.FromUInt32(expected), Frames.PixelAt(frame, 10, 30));
        Assert.Equal(Color.FromUInt32(expected), Frames.PixelAt(frame, 10, 1900));
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_Accent_Swatch_Is_The_Default_Accent_In_Both_Variants(string variant)
    {
        var view = new MainView();
        using var scene = new Scene(view, variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
        using var frame = scene.Capture();

        Assert.Equal(Color.FromUInt32(0xFF3E65FF), CentreOf(frame, Named<Border>(view, "AccentSwatch")));
    }

    [AvaloniaTheory]
    [InlineData("Lime", 0xFFA4C400u)]
    [InlineData("Cobalt", 0xFF0B6CF8u)] // 8.1's Cobalt; 8.0's was #FF0050EF (ADR 0002)
    [InlineData("Taupe", 0xFF87794Eu)]
    public void A_Named_Accent_Swatch_Draws_Its_Token(string name, uint expected)
    {
        var view = new MainView();
        using var scene = new Scene(view);
        var swatch = Named<Border>(view, $"Accent{name}");
        swatch.BringIntoView(); // the last rows are below the fold at 450 x 800
        using var frame = scene.Capture();

        Assert.Equal(Color.FromUInt32(expected), CentreOf(frame, swatch));
    }

    [AvaloniaFact]
    public void Every_Named_Accent_Has_A_Swatch_And_A_Token()
    {
        var view = new MainView();
        using var scene = new Scene(view);

        Assert.Equal(20, MainView.NamedAccentNames.Distinct().Count());
        foreach (var name in MainView.NamedAccentNames)
        {
            Assert.True(scene.Window.TryFindResource($"CornflowerAccent{name}Color", scene.Window.ActualThemeVariant, out var colour),
                $"No token for {name}.");
            var swatch = (SolidColorBrush)Named<Border>(view, $"Accent{name}").Background!;
            Assert.Equal(colour, swatch.Color);
        }
    }

    [AvaloniaFact]
    public void The_Type_Ramp_And_Families_Are_Read_Through_Their_Keys()
    {
        var view = new MainView();
        using var scene = new Scene(view);

        Assert.Equal(18.14, Named<TextBlock>(view, "TextStyleLargeFontSize").FontSize);
        Assert.Equal(34.14, Named<TextBlock>(view, "TextStyleExtraExtraLargeFontSize").FontSize);
        Assert.Equal(18.14, view.FontSize);
        Assert.Contains("Selawik", view.FontFamily.ToString());
        Assert.Contains("SelawikSemilight", Named<TextBlock>(view, "PhoneFontFamilySemiLight").FontFamily.ToString());
        Assert.Contains("SelawikSemilight", Named<TextBlock>(view, "PageTitle").FontFamily.ToString());
    }

    [AvaloniaFact]
    public void The_Text_Styles_Are_The_Themes_By_Their_Keys()
    {
        var view = new MainView();
        using var scene = new Scene(view);
        var blocks = Named<StackPanel>(view, "TextStyles").Children.Cast<TextBlock>().ToList();

        Assert.Equal(16, blocks.Count);
        Assert.All(blocks, block => Assert.Same(scene.Window.FindResource(block.Name!), block.Theme));
    }

    [AvaloniaFact]
    public void The_Device_Facts_Are_The_Views_Scaling_Size_And_Safe_Area()
    {
        var view = new MainView();
        using var scene = new Scene(view);

        Assert.Equal("scaling=2.4 view=450x800 raw=1080x1920 safe=0,0,0,0 page=450x800", view.DeviceFacts);
    }

    [AvaloniaFact]
    public void Once_The_Page_Has_Drawn_It_Reports_Ready()
    {
        var lines = new List<string>();
        var previous = MainView.Report;
        MainView.Report = lines.Add;
        try
        {
            var view = new MainView();
            using var scene = new Scene(view);
            for (var frame = 0; frame < 4; frame++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal([$"{MainView.ReadyMarker} {view.DeviceFacts}"], lines);
        }
        finally
        {
            MainView.Report = previous;
        }
    }

    [AvaloniaFact]
    public void A_Page_That_Leaves_Before_Its_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var previous = MainView.Report;
        MainView.Report = lines.Add;
        try
        {
            // In and out with no dispatcher work between, so no render pass (Scene's own RunJobs
            // runs one) can run the page's frame requests before it leaves.
            using var scene = new Scene();
            var view = new MainView();
            scene.Window.Content = view;
            Assert.True(view.IsAttachedToVisualTree());
            scene.Window.Content = null;
            Assert.False(view.IsAttachedToVisualTree());
            for (var frame = 0; frame < 4; frame++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Empty(lines);
        }
        finally
        {
            MainView.Report = previous;
        }
    }

    // The swatches and the ramp's lines are made in code, outside the XAML name scope.
    private static T Named<T>(MainView view, string name) where T : Control =>
        view.GetLogicalDescendants().OfType<T>().Single(c => c.Name == name);

    private static Color CentreOf(Avalonia.Media.Imaging.WriteableBitmap frame, Control control)
    {
        var top = TopLevel.GetTopLevel(control)!;
        var centre = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), top)!.Value;
        var scaling = top.RenderScaling;
        return Frames.PixelAt(frame, (int)(centre.X * scaling), (int)(centre.Y * scaling));
    }
}
