using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using static Cornflower.Tests.Controls.ButtonReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Button/COMPARISON.md: the Button headless on the reference app's stage (450 × 100 at
/// 2.4, so 1080 × 240 like the capture), at rest, with the pointer over it, pressed at its centre and settled, and
/// disabled, dark and light, named as the reference app names its captures. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ButtonFrames
/// </code>
/// </summary>
public class ButtonFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var state in new[] { "base", "CommonStates.PointerOver", "CommonStates.Pressed", "CommonStates.Disabled" })
        {
            using var clock = new TestClock();
            var button = new Button { Content = "button", IsEnabled = state != "CommonStates.Disabled" };
            var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { button } } } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
            var centre = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), scene.Window)!.Value;
            if (state == "CommonStates.PointerOver") scene.Window.MouseMove(centre);
            if (state == "CommonStates.Pressed") scene.Window.MouseDown(centre, MouseButton.Left);
            clock.At(160);
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__Button__default__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
