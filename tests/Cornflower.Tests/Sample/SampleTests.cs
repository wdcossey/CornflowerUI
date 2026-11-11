using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Cornflower.Sample;
using Cornflower.Themes.Wp81;
using Xunit;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample app every head runs (<c>samples/Cornflower.Sample</c>): its theme set-up, and the theme
/// page drawn by the theme's keys. The ready line the shell writes for each page is held in
/// <see cref="PageTimingTests"/>; the phone's start page in <see cref="StartPageTests"/>.
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
    [InlineData("Cobalt", 0xFF3E65FFu)] // pins the phone's Cobalt (the 8.1 SDK file has #FF0B6CF8; ADR 0002). It is also the
    [InlineData("Cyan", 0xFF1BA1E2u)]   // default, so Lime, Cyan and Taupe carry the check that a swatch draws its own key
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
        Assert.Contains("SelawikSemilight", Named<TextBlock>(view, "PageTitle").FontFamily.ToString());   // the header's
        Assert.Equal("theme", Named<SamplePageHeader>(view, "Header").Title);
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
