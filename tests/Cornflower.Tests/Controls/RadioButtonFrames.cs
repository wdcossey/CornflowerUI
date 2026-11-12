using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/RadioButton/COMPARISON.md: the RadioButton headless on the reference app's stage (450 × 100
/// at 2.4, so 1080 × 240 like the capture), checked and unchecked, at rest, pressed on its circle and settled, and
/// disabled, dark and light, named as the reference app names its captures. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~RadioButtonFrames
/// </code>
/// </summary>
public class RadioButtonFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, isChecked) in new[] { ("checked", true), ("unchecked", false) })
        foreach (var state in new[] { "base", "CommonStates.Pressed", "CommonStates.Disabled" })
        {
            using var clock = new TestClock();
            using var stage = new RadioButtonTests.Stage(variant, new RadioButton
            {
                Content = "radio button", IsChecked = isChecked, IsEnabled = state != "CommonStates.Disabled",
            });
            if (state == "CommonStates.Pressed") stage.Window.MouseDown(stage.Circle, MouseButton.Left);
            clock.At(160);
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__RadioButton__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
