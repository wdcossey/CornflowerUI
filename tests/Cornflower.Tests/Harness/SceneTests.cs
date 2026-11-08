using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Harness;

/// <summary>
/// <see cref="Scene"/> is the reference profile of ADR 0001: device 11, "Emulator 8.1 U1 1080P 5.5
/// inch", a 450 × 800 view at <c>RawPixelsPerViewPixel</c> 2.4, 1080 × 1920 raw pixels.
/// </summary>
public class SceneTests
{
    [AvaloniaFact]
    public void A_Scene_Is_A_Shown_450_By_800_View_At_A_Scaling_Of_2_4()
    {
        using var scene = new Scene(new Border());

        Assert.True(scene.Window.IsVisible);
        Assert.Equal(new Size(450, 800), scene.Window.Bounds.Size);
        Assert.Equal(2.4, scene.Window.RenderScaling);
    }

    // The emulator's capture of the whole screen is 1080 × 1920 (ADR 0001, item 1).
    [AvaloniaFact]
    public void A_Frame_Is_1080_By_1920_Like_The_Emulators()
    {
        using var scene = new Scene(new Border());
        using var frame = scene.Capture();

        Assert.Equal(new PixelSize(1080, 1920), frame.PixelSize);
    }

    // Proves the frame is drawn at 2.4, not drawn at 1 and scaled up: a line one raw pixel wide,
    // 1/2.4 of a view pixel, is one solid pixel, at 2.4 × its view position.
    [AvaloniaFact]
    public void A_Line_One_Raw_Pixel_Wide_Is_One_Solid_Pixel()
    {
        var line = new Border
        {
            Width = 1 / Scene.ReferenceScaling,
            Height = 10,
            Margin = new Thickness(10, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Red,
        };
        using var scene = new Scene(new Panel { Background = Brushes.Black, Children = { line } });
        using var frame = scene.Capture();

        Assert.Equal(Colors.Black, Frames.PixelAt(frame, 23, 30));
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, 24, 30));
        Assert.Equal(Colors.Black, Frames.PixelAt(frame, 25, 30));
    }

    // ADR 0001, item 1: the phone's Button, MinWidth 109, is 109.17 view pixels wide on the
    // reference profile (device 11) and 109.09 on device 12, because layout rounds to each
    // profile's raw pixel grid: 261.6 raw pixels become 262, and 239.8 become 240.
    [AvaloniaTheory]
    [InlineData(2.4, 450, 800, 109.17)]
    [InlineData(2.2, 490.91, 872.73, 109.09)]
    public void Layout_Rounds_To_The_Raw_Pixel_Grid_As_The_Phone_Does(double scaling, double width, double height, double phone)
    {
        var box = new Border { MinWidth = 109, Height = 40, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = new Scene(new Panel { Children = { box } }, width: width, height: height, scaling: scaling);

        Assert.Equal(phone, box.Bounds.Width, 2);
    }

    // Not the same rounding, though. On device 10, at 1.8, the phone's Button is 108.89 wide: 196.2
    // raw pixels become 196, the nearest. Avalonia rounds a measured size up, to 197, so 109.44.
    // They agree above only because 261.6 and 239.8 round up anyway. On the reference profile a
    // size whose raw pixels have a fraction under a half is one raw pixel wider than the phone's.
    [AvaloniaFact]
    public void Layout_Rounds_A_Size_Up_Where_The_Phone_Rounds_To_The_Nearest()
    {
        var box = new Border { MinWidth = 109, Height = 40, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = new Scene(new Panel { Children = { box } }, width: 400, height: 711.11, scaling: 1.8);

        Assert.Equal(197 / 1.8, box.Bounds.Width, 6);
        Assert.NotEqual(108.89, box.Bounds.Width, 2);
    }

    // Without a theme a TextBox has no template and measures 0 × 0, and themes added after the
    // content don't theme it (docs/spikes, 1 and 2).
    [AvaloniaFact]
    public void The_Themes_Are_In_Before_The_Content_So_It_Is_Templated()
    {
        var textBox = new TextBox();
        using var scene = new Scene(new StackPanel { Children = { textBox } });

        Assert.NotEmpty(textBox.GetVisualChildren());
        Assert.True(textBox.Bounds.Height > 0);
    }

    // The later Styles wins (docs/spikes, 2), so Wp81 goes after Simple.
    [AvaloniaFact]
    public void Wp81Theme_Is_After_SimpleTheme()
    {
        using var scene = new Scene();
        var styles = scene.Window.Styles.ToList();

        var simple = styles.FindIndex(s => s is SimpleTheme);
        var wp81 = styles.FindIndex(s => s is Wp81Theme);

        Assert.True(simple >= 0, "SimpleTheme isn't installed");
        Assert.True(wp81 > simple, "Wp81Theme isn't after SimpleTheme");
    }

    [AvaloniaFact]
    public void A_Scene_Is_Dark_Unless_Light_Is_Asked_For()
    {
        using var dark = new Scene();
        using var light = new Scene(variant: ThemeVariant.Light);
        using var darkFrame = dark.Capture();
        using var lightFrame = light.Capture();

        Assert.Equal(ThemeVariant.Dark, dark.Window.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Light, light.Window.ActualThemeVariant);
        Assert.True(Luminance(Frames.PixelAt(darkFrame, 540, 960)) < Luminance(Frames.PixelAt(lightFrame, 540, 960)));
    }

    [AvaloniaFact]
    public void Disposing_Closes_The_Window_And_Detaches_The_Content()
    {
        var content = new Border();
        var scene = new Scene(content);

        scene.Dispose();

        Assert.False(scene.Window.IsVisible);
        Assert.False(content.IsAttachedToVisualTree());
    }

    [AvaloniaFact]
    public void Content_Can_Be_Shown_Again_In_Another_Scene()
    {
        var content = new Border();
        new Scene(content).Dispose();

        using var second = new Scene(content);

        Assert.Same(second.Window, TopLevel.GetTopLevel(content));
    }

    private static double Luminance(Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
}
