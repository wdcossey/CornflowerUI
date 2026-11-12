using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/PasswordBox/COMPARISON.md: the reference app's PasswordBoxes headless on its stage (450 ×
/// 140 at 2.4, 110 with no header), named as the reference app names its captures: <c>password</c> (the reveal button
/// on) at rest, under a pointer, focused, disabled and with the button off; <c>empty</c> at rest, focused and disabled;
/// <c>no-header</c>, <c>reveal-no-header</c> and <c>reveal-empty</c> at rest. Focused is real focus, the caret drawn
/// transparent, as the phone's forced state draws none; disabled is really disabled, so the CheckBox is too, where the
/// phone's forced state leaves it. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PasswordBoxFrames
/// </code>
/// </summary>
public class PasswordBoxFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        var shots = new (string Name, string State, Func<Avalonia.Controls.TextBox> Make, double Height)[]
        {
            ("password", "base", () => PasswordBoxTests.Specimen(reveal: true), 140),
            ("password", "CommonStates.PointerOver", () => PasswordBoxTests.Specimen(reveal: true), 140),
            ("password", "CommonStates.Focused", () => PasswordBoxTests.Specimen(reveal: true), 140),
            ("password", "CommonStates.Disabled", () => PasswordBoxTests.Specimen(reveal: true), 140),
            ("password", "ButtonStates.ButtonCollapsed", () => PasswordBoxTests.Specimen(), 140),
            ("empty", "base", () => PasswordBoxTests.Specimen(password: null, placeholder: TextBoxReadings.Placeholder), 140),
            ("empty", "CommonStates.Focused", () => PasswordBoxTests.Specimen(password: null, placeholder: TextBoxReadings.Placeholder), 140),
            ("empty", "CommonStates.Disabled", () => PasswordBoxTests.Specimen(password: null, placeholder: TextBoxReadings.Placeholder), 140),
            ("no-header", "base", () => PasswordBoxTests.Specimen(header: false), 110),
            ("reveal-no-header", "base", () => PasswordBoxTests.Specimen(header: false, reveal: true), 110),
            ("reveal-empty", "base", () => PasswordBoxTests.Specimen(password: null, reveal: true), 140),
        };
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, state, make, height) in shots)
        {
            var box = make();
            box.IsEnabled = state != "CommonStates.Disabled";
            box.CaretBrush = Avalonia.Media.Brushes.Transparent;
            using var stage = new TextBoxTests.Stage(variant, box, height);
            if (state == "CommonStates.Focused") box.Focus();
            if (state == "CommonStates.PointerOver") stage.Window.MouseMove(stage.At(300, TextBoxReadings.BorderTop + 20));
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__PasswordBox__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
