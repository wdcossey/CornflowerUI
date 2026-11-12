using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ToggleSwitch/COMPARISON.md: the reference app's three ToggleSwitches (<c>off</c> and
/// <c>on</c> with the header "toggle switch", and <c>no-header</c>) headless on their stage (450 × 140, or 110, at 2.4,
/// so 1080 × 336 or 264 like the captures), at rest and disabled, dark and light, named as the reference app names its
/// captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ToggleSwitchFrames
/// </code>
/// </summary>
public class ToggleSwitchFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var specimen in new[] { "off", "on", "no-header" })
        foreach (var state in new[] { "base", "CommonStates.Disabled" })
        {
            var toggle = ToggleSwitchTests.Specimen(on: specimen == "on", header: specimen != "no-header");
            toggle.IsEnabled = state != "CommonStates.Disabled";
            using var stage = new ToggleSwitchTests.Stage(variant, toggle);
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ToggleSwitch__{specimen}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
