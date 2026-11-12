using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/TextBox/COMPARISON.md: the reference app's TextBoxes headless on its stage (450 × 140 at
/// 2.4, so 1080 × 336 like the captures; 110 for no header, 220 wrapped), at rest, focused and disabled, dark and light,
/// named as the reference app names its captures. Focused is real focus, with the caret drawn transparent, as the
/// phone's forced state draws none. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~TextBoxFrames
/// </code>
/// </summary>
public class TextBoxFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        var specimens = new (string Name, Func<Avalonia.Controls.TextBox> Make, double Height)[]
        {
            ("empty", () => TextBoxTests.Specimen(placeholder: TextBoxReadings.Placeholder), 140),
            ("text", () => TextBoxTests.Specimen(text: TextBoxReadings.Text), 140),
            ("no-header", () => TextBoxTests.Specimen(text: TextBoxReadings.Text, header: false), 110),
            ("wrap", TextBoxTests.WrappedSpecimen, 220),
        };
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, make, height) in specimens)
        foreach (var state in new[] { "base", "CommonStates.Focused", "CommonStates.Disabled" })
        {
            var box = make();
            box.IsEnabled = state != "CommonStates.Disabled";
            box.CaretBrush = Avalonia.Media.Brushes.Transparent;
            using var stage = new TextBoxTests.Stage(variant, box, height);
            if (state == "CommonStates.Focused") box.Focus();
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__TextBox__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
