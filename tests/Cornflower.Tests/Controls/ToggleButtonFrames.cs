using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using static Cornflower.Tests.Controls.ButtonReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ToggleButton/COMPARISON.md and docs/controls/RepeatButton/COMPARISON.md: each headless on
/// the reference app's stage (450 × 100 at 2.4, so 1080 × 240 like the capture), in each look the captures have, dark and
/// light, named as the reference app names its captures. A state is reached as an app reaches it: checked, disabled, or
/// pressed by a mouse at the centre. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ToggleButtonFrames
/// </code>
/// </summary>
public class ToggleButtonFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var name = variant.ToString().ToLowerInvariant();
            foreach (var (file, isChecked, enabled, press) in new[]
                     {
                         ("unchecked__base", false, true, false),
                         ("checked__base", true, true, false),
                         ("unchecked__CommonStates.Disabled", false, false, false),
                         ("checked__CommonStates.CheckedDisabled", true, false, false),
                         ("checked__CommonStates.CheckedPressed", true, true, true),
                     })
                SaveOne(new ToggleButton { Content = "toggle button", IsChecked = isChecked, IsEnabled = enabled }, press, variant,
                    Path.Combine(folder, $"{name}__ToggleButton__{file}.png"));

            foreach (var (file, enabled, press) in new[]
                     {
                         ("base", true, false),
                         ("CommonStates.Pressed", true, true),
                         ("CommonStates.Disabled", false, false),
                     })
                SaveOne(new RepeatButton { Content = "repeat button", IsEnabled = enabled }, press, variant,
                    Path.Combine(folder, $"{name}__RepeatButton__default__{file}.png"));
        }
    }

    private static void SaveOne(Button button, bool press, ThemeVariant variant, string path)
    {
        var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { button } } } };
        stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
        using var scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
        if (press)
        {
            var centre = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), scene.Window)!.Value;
            scene.Window.MouseDown(centre, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        using var frame = scene.Capture();
        frame.Save(path, PngBitmapEncoderOptions.Default);
        if (press && button is RepeatButton)
        {
            var centre = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), scene.Window)!.Value;
            scene.Window.MouseUp(centre, MouseButton.Left);
        }
    }
}
