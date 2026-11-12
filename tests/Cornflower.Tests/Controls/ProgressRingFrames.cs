using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.ProgressRingReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ProgressRing/COMPARISON.md: the catalogue's ring headless on the reference app's stage
/// (450 × 120 at 2.4, so 1080 × 288 like the capture), dark and light, named as the reference app names its captures.
/// Each is drawn at the storyboard time its capture shows (fitted from the capture's dots; the comparison says how),
/// to the nearest 16 ms frame; <c>ActiveStates.Inactive</c> inactive. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ProgressRingFrames
/// </code>
/// </summary>
public class ProgressRingFrames
{
    // The captures' moments, from their dots: one dot at 0.126 s; four at about 0.61 s. The forced Active capture
    // shows no dot (its storyboard hadn't drawn yet), so ours is drawn at the start, 0.
    private static readonly (string State, double Ms, bool Active)[] States =
    [
        ("base", 126, true),
        ("SizeStates.Large", 610, true),
        ("SizeStates.Small", 595, true),
        ("ActiveStates.Active", 0, true),
        ("ActiveStates.Inactive", 0, false),
    ];

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (state, ms, active) in States)
        {
            using var clock = new TestClock();
            var ring = new ProgressRing();
            var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { ring } } } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
            ring.IsActive = active;
            clock.At(Math.Round(ms / 16) * 16);
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ProgressRing__active__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
