using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Cornflower.Tests;

/// <summary>
/// Proves the headless test harness itself works. Every control's tests build on this; if this
/// fails, nothing else is meaningful.
/// </summary>
public class HarnessTests
{
    [AvaloniaFact]
    public void Headless_Window_Opens_And_Lays_Out()
    {
        var window = new Window { Width = 390, Height = 844, Content = new Border() };
        window.Show();

        Assert.True(window.IsVisible);
        Assert.Equal(390, window.Bounds.Width);
    }

    // The compare step sets our frames beside the emulator's, so a frame must be real pixels.
    [AvaloniaFact]
    public void Skia_Renders_A_Real_Frame()
    {
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Background = Brushes.Black,
            Content = new Border { Background = new SolidColorBrush(Color.FromRgb(0x1B, 0xA1, 0xE2)) },
        };
        window.Show();

        using var frame = window.CaptureRenderedFrame();

        Assert.NotNull(frame);
        Assert.Equal(100, frame.PixelSize.Width);
    }
}
