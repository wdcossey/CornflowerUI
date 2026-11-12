using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Slider/COMPARISON.md: the reference app's Slider (<c>Header = "slider", Value = 30</c>)
/// headless on its stage (450 × 140 at 2.4, so 1080 × 336 like the capture), at rest and disabled, dark and light,
/// named as the reference app names its captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a
/// folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~SliderFrames
/// </code>
/// </summary>
public class SliderFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var state in new[] { "base", "CommonStates.Disabled" })
        {
            var slider = SliderTests.Specimen();
            slider.IsEnabled = state != "CommonStates.Disabled";
            using var stage = new SliderTests.Stage(variant, slider);
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__Slider__value-30__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
