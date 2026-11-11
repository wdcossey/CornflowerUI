using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.ButtonReadings;
using static Cornflower.Tests.Behaviours.TiltReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Button (<c>Controls/Button.axaml</c>) against the phone's style and the reference app's captures
/// (<see cref="ButtonReadings"/>), and its tilt against the emulator's readings (<see cref="TiltReadings"/>).
/// docs/controls/Button has the plan and the comparison.
/// </summary>
public class ButtonTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>A Button on the reference app's stage: 450 × 100, the page's background, the Button in a slot 19 in and 12 down.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, Button? button = null)
        {
            Button = button ?? new Button { Content = "button" };
            var slot = new Grid { Margin = Slot, Children = { Button } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
        }

        public Button Button { get; }

        public Window Window => _scene.Window;

        /// <summary>The Button's centre, in the window.</summary>
        public Point Centre => Button.TranslatePoint(new Point(Button.Bounds.Width / 2, Button.Bounds.Height / 2), Window)!.Value;

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(Button button, string name) where T : Control =>
        button.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Border BorderOf(Button button) => Part<Border>(button, "Border");

    private static ContentPresenter PresenterOf(Button button) => Part<ContentPresenter>(button, "PART_ContentPresenter");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    // The style's setters, as the dump read them (button.json's keys, themeresources.json's values).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var button = stage.Button;

        Assert.Equal(Padding, button.Padding);
        Assert.Equal(BorderThickness, button.BorderThickness);
        Assert.Equal(MinWidth, button.MinWidth);
        Assert.Equal(MinHeight, button.MinHeight);
        Assert.Equal(FontSize, button.FontSize);
        Assert.Equal(Weight, button.FontWeight);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), button.FontFamily);
        Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
        Assert.Equal(Transparent, ColourOf(button.Background));
        Assert.Equal(Foreground(dark), ColourOf(button.Foreground));
        Assert.Equal(Foreground(dark), ColourOf(button.BorderBrush));
        Assert.Equal(Overhang, BorderOf(button).Margin);
        Assert.Equal(Padding, PresenterOf(button).Margin);
    }

    // FontFamily is the style's, not whatever the Button sits in: the window's family is the same key's here.
    [AvaloniaFact]
    public void The_Font_Is_The_Phones_Whatever_It_Sits_In()
    {
        var button = new Button { Content = "button" };
        var parent = new ContentControl { FontFamily = new FontFamily("Courier New"), Content = button };
        using var scene = new Scene(parent);

        Assert.Equal(scene.Window.FindResource("PhoneFontFamilyNormal"), button.FontFamily);
    }

    // The dump's bounds: the Button, its Border inside the overhang, and the content's top and height.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new Stage();
        var button = stage.Button;
        var inStage = button.TranslatePoint(default, (Visual)stage.Window.Content!)!.Value;

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(InStage.Width, button.Bounds.Width, 0.001);
        Assert.Equal(InStage.Height, button.Bounds.Height, 0.001);

        var border = BorderOf(button);
        var borderInButton = border.TranslatePoint(default, button)!.Value;
        Assert.Equal(BorderBounds.Y, borderInButton.Y, 0.001);
        Assert.Equal(BorderBounds.Height, border.Bounds.Height, 0.001);
        Assert.Equal(BorderBounds.Width, border.Bounds.Width, 0.001);

        var presenter = PresenterOf(button);
        Assert.Equal(ContentTop, presenter.TranslatePoint(default, button)!.Value.Y, 0.001);
        Assert.Equal(ContentHeight, presenter.Bounds.Height, 0.001);
    }

    // HorizontalContentAlignment and VerticalContentAlignment Center: the content in the middle of the space the
    // padding leaves, as the dump's ContentPresenter is (its middle 54.79 across, the Border's 54.58; the font's width
    // decides the rest).
    [AvaloniaFact]
    public void The_Content_Is_Centred_In_The_Padding()
    {
        using var stage = new Stage(button: new Button { Content = "button", Width = 300 });
        var border = BorderOf(stage.Button);
        var presenter = PresenterOf(stage.Button);
        var box = new Rect(presenter.TranslatePoint(default, border)!.Value, presenter.Bounds.Size);

        Assert.Equal(border.Bounds.Width / 2, box.Center.X, 0.5);
        Assert.True(box.Width < 150, $"the content is {box.Width} wide: stretched, not centred");
        Assert.Equal(ContentTop - BorderBounds.Y, box.Y, 0.001);
    }

    // A string's line is the phone's, 1.33 em, at the Button's own size too (ADR 0003).
    [AvaloniaFact]
    public void A_Strings_Line_Is_The_Phones_At_Any_Size()
    {
        using var stage = new Stage();
        var text = PresenterOf(stage.Button).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(1.33 * FontSize, text.LineHeight, 0.01);

        stage.Button.FontSize = 30;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1.33 * 30, text.LineHeight, 0.02);
    }

    // The phone's ContentPresenter puts any content that isn't an element in a TextBlock with Segoe WP's line: a
    // number as much as a string.
    [AvaloniaFact]
    public void A_Numbers_Line_Is_The_Phones_Too()
    {
        using var stage = new Stage(button: new Button { Content = 42 });
        var text = PresenterOf(stage.Button).GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.Equal("42", text.Text);
        Assert.Equal(1.33 * FontSize, text.LineHeight, 0.01);
        Assert.Equal(ContentHeight, PresenterOf(stage.Button).Bounds.Height, 0.001);
    }

    // The theme adds no data template, so a string DataTemplate the application defines is found as without it, and
    // what it makes keeps its own line.
    [AvaloniaFact]
    public void An_Applications_String_DataTemplate_Wins()
    {
        var template = new FuncDataTemplate<string>((s, _) => new TextBlock { Name = "Apps", Text = s });
        var templates = Application.Current!.DataTemplates;
        templates.Add(template);
        try
        {
            using var stage = new Stage();
            var text = PresenterOf(stage.Button).GetVisualDescendants().OfType<TextBlock>().Single();

            Assert.Equal("Apps", text.Name);
            Assert.True(double.IsNaN(text.LineHeight), $"the app's TextBlock was given a LineHeight of {text.LineHeight}");
        }
        finally
        {
            templates.Remove(template);
        }
    }

    // Content that is an element is the app's: a TextBlock in a Button keeps its own line.
    [AvaloniaFact]
    public void A_TextBlock_In_The_Content_Keeps_Its_Own_Line()
    {
        var own = new TextBlock { Text = "own" };
        using var stage = new Stage(button: new Button { Content = own });

        Assert.True(double.IsNaN(own.LineHeight));
    }

    // The app's own ContentTemplate still decides how its content is shown.
    [AvaloniaFact]
    public void The_Apps_ContentTemplate_Wins_Over_The_Strings_Line()
    {
        var button = new Button
        {
            Content = "button",
            ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Name = "Apps", Text = s }),
        };
        using var stage = new Stage(button: button);

        var text = PresenterOf(button).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("Apps", text.Name);
    }

    // Every pixel of the border where the PNG has it, in its colour, and the page just outside and inside.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Border_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        AssertBorder(frame, Foreground(dark), Page(dark));
    }

    private static void AssertBorder(Avalonia.Media.Imaging.WriteableBitmap frame, Color ink, Color page)
    {
        var misses = new List<string>();
        void Expect(int x, int y, Color expected)
        {
            var got = Frames.PixelAt(frame, x, y);
            if (got != expected) misses.Add($"({x}, {y}): {Hex(got)}, the phone's {Hex(expected)}");
        }

        foreach (var x in BorderColumns) Expect(x, ReadRow, ink);
        foreach (var y in BorderRows) Expect(ReadColumn, y, ink);
        foreach (var x in PageColumns) Expect(x, ReadRow, page);
        foreach (var y in PageRows) Expect(ReadColumn, y, page);

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    // CommonStates.Disabled: the border and the text in PhoneBaseLowColor, the background transparent.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new Button { Content = "button", IsEnabled = false });

        Assert.Equal(Disabled(dark), ColourOf(BorderOf(stage.Button).BorderBrush));
        Assert.Equal(Transparent, ColourOf(BorderOf(stage.Button).Background));
        Assert.Equal(Disabled(dark), ColourOf(PresenterOf(stage.Button).Foreground));
        using var frame = stage.Capture();
        AssertBorder(frame, DisabledInk(dark), Page(dark));
    }

    // ButtonDisabledBackgroundThemeBrush: disabled, the background is the phone's, not the one the app gave the Button.
    [AvaloniaFact]
    public void Disabled_Takes_The_Apps_Background_Away()
    {
        using var stage = new Stage(button: new Button { Content = "button", Background = Brushes.Red, IsEnabled = false });

        Assert.Equal(Transparent, ColourOf(BorderOf(stage.Button).Background));
    }

    // CommonStates.Pressed: the accent behind the text over the accent; the border stays the foreground.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Pressed_Fills_With_The_Accent_And_Draws_The_Text_Over_It(string variant)
    {
        var dark = variant == "Dark";
        using var clock = new TestClock();
        using var stage = new Stage(VariantOf(variant));
        var button = stage.Button;

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(SettleMs);

        Assert.Equal(Accent, ColourOf(BorderOf(button).Background));
        Assert.Equal(OverAccent, ColourOf(PresenterOf(button).Foreground));
        Assert.Equal(Foreground(dark), ColourOf(BorderOf(button).BorderBrush));
        using (var frame = stage.Capture())
            Assert.Equal(Accent, Frames.PixelAt(frame, 75, ReadRow));   // inside the border, left of the text

        stage.Window.MouseUp(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Transparent, ColourOf(BorderOf(button).Background));
        Assert.Equal(Foreground(dark), ColourOf(PresenterOf(button).Foreground));
    }

    // CommonStates.PointerOver changes nothing on the phone: its capture is the base's, pixel for pixel.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Pointer_Over_It_Changes_Nothing(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));

        stage.Window.MouseMove(stage.Centre);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsPointerOver);
        Assert.Equal(Transparent, ColourOf(BorderOf(stage.Button).Background));
        Assert.Equal(Foreground(dark), ColourOf(PresenterOf(stage.Button).Foreground));
        using var frame = stage.Capture();
        AssertBorder(frame, Foreground(dark), Page(dark));
    }

    // The phone's template has no FocusStates group: keyboard focus draws nothing, where Avalonia would put the
    // control's FocusAdorner (left unset, Simple's under Wp81 draws a rectangle) in the window's adorner layer.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new Stage();
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsFocused, "Tab didn't reach the Button");
        var layer = AdornerLayer.GetAdornerLayer(stage.Button);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // ThemeResource: the colours follow a change of variant.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark);
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Foreground(dark: false), ColourOf(stage.Button.BorderBrush));
        using var frame = stage.Capture();
        AssertBorder(frame, Foreground(dark: false), Page(dark: false));
    }

    // The phone's PointerDownThemeAnimation targets its Grid, the whole Button: the theme's Grid tilts,
    // inside a root that doesn't, and nothing else does.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Grid_Inside_An_Untilted_Root()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var button = stage.Button;
        var root = (Control)button.GetVisualChildren().Single();
        var grid = Part<Panel>(button, "Grid");

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(CfTilt.GetIsEnabled(button));
        Assert.Same(grid, CfTilt.TargetOf(button));
        Assert.Equal(button.Bounds.Size, grid.Bounds.Size);
        Assert.IsType<MatrixTransform>(grid.RenderTransform);
        Assert.Null(root.RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
    }

    /// <summary>The probe's Buttons, with the theme's Button, on the reference view.</summary>
    private static (Scene Scene, Button Button) ProbeButton(Layout layout)
    {
        var button = new Button { Content = layout.Name, Width = layout.Width, Height = layout.Height };
        Canvas.SetLeft(button, layout.Left);
        Canvas.SetTop(button, layout.Top);
        return (new Scene(new Canvas { Children = { button } }), button);
    }

    // Run 20261109-2307-dev11: Button A (120 × 60), pressed at 25 points. The theme's tilt settles at each reading.
    [AvaloniaFact]
    public void A_Held_Press_Settles_At_The_Phones_Tilt_For_Its_Point()
    {
        using var clock = new TestClock();
        var layout = GridButtons.Single(b => b.Name == "A");
        var (scene, button) = ProbeButton(layout);
        using var _ = scene;
        var misses = new List<string>();
        var time = 0.0;

        foreach (var press in GridPresses.Where(p => p.Button == "A"))
        {
            var at = layout.Bounds.TopLeft + press.Point;
            var touch = scene.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            var got = CfTilt.Current(button);
            if (Math.Abs(got.RotationX - press.RotationX) > 0.04 || Math.Abs(got.RotationY - press.RotationY) > 0.04 ||
                Math.Abs(got.OffsetZ - press.OffsetZ) > 0.085)
                misses.Add(string.Create(CultureInfo.InvariantCulture,
                    $"at {press.X}, {press.Y}: read ({press.RotationX}, {press.RotationY}, {press.OffsetZ}), got ({got.RotationX:F3}, {got.RotationY:F3}, {got.OffsetZ:F3})"));
            scene.Window.TouchEnd(touch, at);
            clock.At(time += 1000);
        }

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    // Run 20261110-0822-dev11: every tap 0.84, 1.7 and 3.35 px inside an edge of a tilted Button clicked.
    [AvaloniaFact]
    public void A_Tap_Just_Inside_Each_Edge_Clicks_As_On_The_Phone()
    {
        using var clock = new TestClock();
        var (scene, button) = ProbeButton(FirstButton);
        using var _ = scene;
        var clicked = new List<Point>();
        var time = 0.0;

        foreach (var tap in EdgeTaps)
        {
            var clicks = 0;
            void Count(object? sender, RoutedEventArgs e) => clicks++;
            button.Click += Count;
            var at = FirstButton.Bounds.TopLeft + tap;
            var touch = scene.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
            scene.Window.TouchEnd(touch, at);
            button.Click -= Count;
            if (clicks == 1) clicked.Add(tap);
            clock.At(time += 1000);
        }

        Assert.Equal(EdgeTaps, clicked);
    }

    // A new theme mid-press (an app swapping its Button's theme) takes the tilted Grid away without a release: the
    // tilt goes to rest, the old Grid keeps no transform, and the next press tilts the new template.
    [AvaloniaFact]
    public void A_New_Theme_Mid_Press_Puts_The_Tilt_At_Rest_And_The_Next_Press_Tilts()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var button = stage.Button;
        var grid = Part<Panel>(button, "Grid");

        var touch = stage.Window.TouchBegin(stage.Centre);
        clock.At(SettleMs);
        Assert.IsType<MatrixTransform>(grid.RenderTransform);

        button.Theme = new Avalonia.Themes.Simple.SimpleTheme().TryGetResource(typeof(Button), ThemeVariant.Dark, out var simple)
            ? (ControlTheme)simple! : throw new InvalidOperationException();
        Dispatcher.UIThread.RunJobs();
        clock.At(2 * SettleMs);
        Assert.Equal(TiltState.Rest, CfTilt.Current(button));
        Assert.Null(grid.RenderTransform);
        stage.Window.TouchEnd(touch, stage.Centre);

        CfTilt.SetIsEnabled(button, true);                         // Simple's theme doesn't turn it on
        stage.Window.TouchBegin(stage.Centre);
        clock.At(3 * SettleMs);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(button));
        Assert.NotSame(grid, CfTilt.TargetOf(button));
    }

    // The touch target is the whole Button, the overhang above and below the border included, as the phone's Grid is.
    [AvaloniaFact]
    public void A_Tap_On_The_Overhang_Clicks()
    {
        using var clock = new TestClock();                                                  // the tilt's frames, under the test's control
        using var stage = new Stage();
        var clicks = 0;
        stage.Button.Click += (_, _) => clicks++;
        var above = stage.Button.TranslatePoint(new Point(54, 4), stage.Window)!.Value;     // 4 DIP down: above the border's 9.58

        stage.Window.MouseDown(above, MouseButton.Left);
        clock.At(SettleMs);
        stage.Window.MouseUp(above, MouseButton.Left);

        Assert.Equal(1, clicks);
        clock.At(SettleMs + 1000);
        Assert.Equal(TiltState.Rest, CfTilt.Current(stage.Button));
    }
}
