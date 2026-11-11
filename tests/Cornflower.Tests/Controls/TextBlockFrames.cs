using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/TextBlock/COMPARISON.md: each text style headless, on the reference app's stage
/// (450 × 120 at 2.4, so 1080 × 288 like the capture), dark and light, named as the reference app names its
/// captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~TextBlockFrames
/// </code>
/// </summary>
public class TextBlockFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var reading in TextStyleReadings.All)
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var block = new TextBlock { Text = "Agjy 0123 cornflower", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var stage = new Border { Child = new StackPanel { Margin = new Thickness(19, 12, 19, 0), Children = { block } } };
            stage.Bind(Border.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            stage.Bind(TextElement.ForegroundProperty, stage.GetResourceObservable("PhoneForegroundBrush"));
            using var scene = new Scene(stage, variant, width: 450, height: 120);
            block.Theme = (ControlTheme)scene.Window.FindResource(reading.Key)!;
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__TextBlock__{reading.Key}__base.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
