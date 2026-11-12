using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using static Cornflower.Tests.Controls.ProgressBarReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ProgressBar/COMPARISON.md: the catalogue's two bars headless on the reference app's stage
/// (450 × 60 at 2.4, so 1080 × 144 like the capture), dark and light, named as the reference app names its captures.
/// Avalonia's ProgressBar has only determinate and indeterminate, so each of the phone's forced states is drawn as the
/// nearest of the two (the comparison says which); an indeterminate one at the storyboard time its capture shows, on
/// the emulator's 64 Hz frames. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ProgressBarFrames
/// </code>
/// </summary>
public class ProgressBarFrames
{
    // (variant, state, value, indeterminate, the storyboard's frame at 64 Hz, dark and light)
    private static readonly (string Variant, string State, double Value, bool Indeterminate, int DarkFrame, int LightFrame)[] States =
    [
        ("value-40", "base", Value, false, 0, 0),
        ("value-40", "CommonStates.Determinate", Value, false, 0, 0),
        ("value-40", "CommonStates.Updating", Value, false, 0, 0),
        ("value-40", "CommonStates.Paused", Value, false, 0, 0),
        ("value-40", "CommonStates.Error", Value, false, 0, 0),
        ("value-40", "CommonStates.Indeterminate", Value, true, 31, 29),
        ("indeterminate", "base", 0, true, 9, 9),
        ("indeterminate", "CommonStates.Determinate", 0, false, 0, 0),
        ("indeterminate", "CommonStates.Updating", 0, false, 0, 0),
        ("indeterminate", "CommonStates.Paused", 0, false, 0, 0),
        ("indeterminate", "CommonStates.Error", 0, false, 0, 0),
        ("indeterminate", "CommonStates.Indeterminate", 0, false, 0, 0),
    ];

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, state, value, indeterminate, darkFrame, lightFrame) in States)
        {
            using var clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(1000 / 64.0) };
            var bar = new ProgressBar { Value = value, IsIndeterminate = indeterminate };
            var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { bar } } } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
            clock.At((variant == ThemeVariant.Dark ? darkFrame : lightFrame) * 1000 / 64.0);
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ProgressBar__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
