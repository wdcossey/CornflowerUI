using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's <c>HeaderContentPresenterStyle</c>, one keyed theme (<c>Wp81HeaderContentPresenterTheme</c>,
/// <c>Controls/HeaderContentPresenter.axaml</c>) that the Slider's, the TextBox's, the PasswordBox's and the
/// ToggleSwitch's templates set on their header presenters. Each control's own tests hold the values it shows; these
/// hold that the four share the one theme, and that a template's own values and states still win over it.
/// </summary>
public class HeaderContentPresenterTests
{
    private const string Key = "Wp81HeaderContentPresenterTheme";

    private static readonly Color Mid = Color.FromUInt32(0xFF0000FFu);
    private static readonly Color ToggleSwitchHeader = Color.FromUInt32(0xFFFF0000u);

    private static ContentPresenter Part(Visual of, string name) =>
        of.GetVisualDescendants().OfType<ContentPresenter>().First(c => c.Name == name);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static TextBox PasswordBox()
    {
        var box = new TextBox();
        box.Bind(StyledElement.ThemeProperty, box.GetResourceObservable("PasswordBox"));
        return box;
    }

    private static T WithHeader<T>(T control) where T : Control
    {
        ControlHeader.SetHeader(control, "header");
        return control;
    }

    /// <summary>Every header the theme draws, and the ToggleSwitch's Content in a header's place.</summary>
    private static (Control Control, string Part)[] Headers() =>
    [
        (WithHeader(new Slider()), "HeaderContentPresenter"),
        (WithHeader(new Slider { Orientation = Orientation.Vertical, Height = 100 }), "HeaderContentPresenter"),
        (WithHeader(new TextBox()), "HeaderContentPresenter"),
        (WithHeader(PasswordBox()), "HeaderContentPresenter"),
        (WithHeader(new ToggleSwitch()), "HeaderContentPresenter"),
        (new ToggleSwitch { Content = "content" }, "PART_ContentPresenter"),
    ];

    private static Scene SceneOf(IEnumerable<Control> controls)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(controls);
        return new Scene(panel);
    }

    // Each header presenter wears the one keyed theme, and so takes its four values and the phone's line.
    [AvaloniaFact]
    public void Every_Header_Wears_The_Shared_Theme()
    {
        var headers = Headers();
        using var scene = SceneOf(headers.Select(h => h.Control));
        var theme = Assert.IsType<ControlTheme>(scene.Window.FindResource(Key));

        Assert.Equal(typeof(ContentPresenter), theme.TargetType);
        foreach (var (control, name) in headers)
        {
            var presenter = Part(control, name);
            Assert.Same(theme, presenter.Theme);
            Assert.Equal(scene.Window.FindResource("PhoneFontFamilyNormal"), presenter.FontFamily);
            Assert.Equal(scene.Window.FindResource("TextStyleMediumFontSize"), presenter.FontSize);
            Assert.Equal(FontWeight.Normal, presenter.FontWeight);
            Assert.True(PhoneHeaderLine.GetIsEnabled(presenter), $"{control.GetType().Name}'s {name} has no phone's line");
        }
    }

    // The theme's brush is PhoneMidBrush, by its key; the ToggleSwitch's template sets its own, which wins, as the
    // phone's template's own Foreground wins over the style's. (The two keys are one colour in the phone's resources,
    // so they are told apart here by redefining both.)
    [AvaloniaFact]
    public void A_Templates_Own_Brush_Wins_Over_The_Shared_Theme()
    {
        var headers = Headers();
        using var scene = SceneOf(headers.Select(h => h.Control));
        scene.Window.Resources["PhoneMidBrush"] = new SolidColorBrush(Mid);
        scene.Window.Resources["ToggleSwitchHeaderForegroundThemeBrush"] = new SolidColorBrush(ToggleSwitchHeader);
        Dispatcher.UIThread.RunJobs();

        foreach (var (control, name) in headers)
        {
            var expected = control is ToggleSwitch ? ToggleSwitchHeader : Mid;
            Assert.Equal(expected, ColourOf(Part(control, name).Foreground));
        }
    }

    // A state's header brush (the TextBox's and the PasswordBox's :disabled, the ToggleSwitch's) wins over the part's
    // theme, and over the ToggleSwitch's own brush.
    [AvaloniaFact]
    public void A_States_Brush_Wins_Over_The_Shared_Theme()
    {
        var headers = Headers();
        foreach (var (control, _) in headers)
            control.IsEnabled = false;
        using var scene = SceneOf(headers.Select(h => h.Control));
        scene.Window.Resources["PhoneMidBrush"] = new SolidColorBrush(Mid);
        scene.Window.Resources["ToggleSwitchHeaderForegroundThemeBrush"] = new SolidColorBrush(ToggleSwitchHeader);
        Dispatcher.UIThread.RunJobs();

        foreach (var (control, name) in headers)
        {
            var key = control switch
            {
                ToggleSwitch => "ToggleSwitchDisabledForegroundThemeBrush",
                TextBox => "TextBoxDisabledHeaderForegroundThemeBrush",
                _ => "PhoneMidBrush",               // the phone's Slider has no disabled header
            };
            Assert.True(scene.Window.TryFindResource(key, scene.Window.ActualThemeVariant, out var found), key);
            var expected = ColourOf(found as IBrush);
            Assert.Equal(expected, ColourOf(Part(control, name).Foreground));
        }
    }
}
