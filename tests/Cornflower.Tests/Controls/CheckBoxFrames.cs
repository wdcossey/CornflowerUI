using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using static Cornflower.Tests.Controls.CheckBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/CheckBox/COMPARISON.md: the CheckBox headless on the reference app's stage (450 × 100 at
/// 2.4, so 1080 × 240 like the capture), checked, unchecked and indeterminate, at rest, pressed on its box and
/// settled, and disabled, dark and light, named as the reference app names its captures. By hand: it does nothing
/// unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~CheckBoxFrames
/// </code>
/// </summary>
public class CheckBoxFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, isChecked) in new (string, bool?)[] { ("checked", true), ("unchecked", false), ("indeterminate", null) })
        foreach (var state in new[] { "base", "CommonStates.Pressed", "CommonStates.Disabled" })
        {
            using var clock = new TestClock();
            var checkBox = new CheckBox
            {
                Content = "check box", IsThreeState = isChecked is null, IsChecked = isChecked, IsEnabled = state != "CommonStates.Disabled",
            };
            var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { checkBox } } } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
            var box = checkBox.TranslatePoint(new Point(Column / 2, GridTop + Column / 2), scene.Window)!.Value;
            if (state == "CommonStates.Pressed") scene.Window.MouseDown(box, MouseButton.Left);
            clock.At(160);
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__CheckBox__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
