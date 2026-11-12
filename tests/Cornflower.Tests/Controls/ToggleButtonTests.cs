using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ToggleButtonReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>A Button of any kind on the reference app's stage: 450 × 100, the page's background, a slot 19 in and 12 down.</summary>
internal sealed class ButtonStage<T> : IDisposable where T : Button
{
    private readonly Scene _scene;

    public ButtonStage(T button, ThemeVariant? variant = null)
    {
        Button = button;
        var stage = new Grid { Children = { new Grid { Margin = ButtonReadings.Slot, Children = { button } } } };
        stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
        _scene = new Scene(stage, variant, width: ButtonReadings.StageWidth, height: ButtonReadings.StageHeight);
    }

    public T Button { get; }

    public Window Window => _scene.Window;

    /// <summary>The Button's centre, in the window.</summary>
    public Point Centre => At(Button.Bounds.Width / 2, Button.Bounds.Height / 2);

    /// <summary>A point given in the Button's own coordinates, in the window.</summary>
    public Point At(double x, double y) => Button.TranslatePoint(new Point(x, y), Window)!.Value;

    public WriteableBitmap Capture() => _scene.Capture();

    public void Dispose() => _scene.Dispose();
}

/// <summary>What the ToggleButton and RepeatButton tests share: parts, colours and the captures' pixels.</summary>
internal static class ButtonFamily
{
    public static T Part<T>(Control button, string name) where T : Control =>
        button.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    public static ContentPresenter PresenterOf(Control button) => Part<ContentPresenter>(button, "PART_ContentPresenter");

    public static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");

    public static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>
    /// The border where the PNGs draw it, in <paramref name="ink"/>: the left columns on the read row, the top and
    /// bottom rows on the read column; the page just outside it and <paramref name="fill"/> just inside. Within
    /// <paramref name="tolerance"/> per channel, for a colour composited at an opacity.
    /// </summary>
    public static void AssertBorder(WriteableBitmap frame, Color ink, Color page, Color fill, int tolerance = 0)
    {
        var misses = new List<string>();
        void Expect(int x, int y, Color expected)
        {
            var got = Frames.PixelAt(frame, x, y);
            if (Math.Abs(got.R - expected.R) > tolerance || Math.Abs(got.G - expected.G) > tolerance ||
                Math.Abs(got.B - expected.B) > tolerance || got.A != expected.A)
                misses.Add($"({x}, {y}): {Hex(got)}, the phone's {Hex(expected)}");
        }

        foreach (var x in LeftColumns) Expect(x, ReadRow, ink);
        foreach (var y in Rows) Expect(ReadColumn, y, ink);
        foreach (var x in OutsideColumns) Expect(x, ReadRow, page);
        foreach (var y in OutsideRows) Expect(ReadColumn, y, page);
        foreach (var x in InsideColumns) Expect(x, ReadRow, fill);
        foreach (var y in InsideRows) Expect(ReadColumn, y, fill);

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    /// <summary>The style's setters the phone's Button, ToggleButton and RepeatButton share, as the dumps read them.</summary>
    public static void AssertButtonSetters(Button button, bool dark, Window window)
    {
        Assert.Equal(ButtonReadings.Padding, button.Padding);
        Assert.Equal(ButtonReadings.BorderThickness, button.BorderThickness);
        Assert.Equal(ButtonReadings.MinWidth, button.MinWidth);
        Assert.Equal(ButtonReadings.MinHeight, button.MinHeight);
        Assert.Equal(ButtonReadings.FontSize, button.FontSize);
        Assert.Equal(ButtonReadings.Weight, button.FontWeight);
        Assert.Equal(window.FindResource("PhoneFontFamilyNormal"), button.FontFamily);
        Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
        Assert.Equal(ButtonReadings.Transparent, ColourOf(button.Background));
        Assert.Equal(ButtonReadings.Foreground(dark), ColourOf(button.Foreground));
        Assert.Equal(ButtonReadings.Foreground(dark), ColourOf(button.BorderBrush));
        Assert.Equal(ButtonReadings.Padding, PresenterOf(button).Margin);
    }

    /// <summary>The dumps' bounds: the control at 19.1667, 27.5, 57.5 tall; its border 9.5833 down, 38.3333 tall; the content 15 down, 24.1667 tall.</summary>
    public static void AssertBounds(Button button, Border border, Window window)
    {
        var inStage = button.TranslatePoint(default, (Visual)window.Content!)!.Value;
        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(ButtonReadings.InStage.Height, button.Bounds.Height, 0.001);

        Assert.Equal(ButtonReadings.BorderBounds.Y, border.TranslatePoint(default, button)!.Value.Y, 0.001);
        Assert.Equal(ButtonReadings.BorderBounds.Height, border.Bounds.Height, 0.001);
        Assert.Equal(button.Bounds.Width, border.Bounds.Width, 0.001);

        var presenter = PresenterOf(button);
        Assert.Equal(ButtonReadings.ContentTop, presenter.TranslatePoint(default, button)!.Value.Y, 0.001);
        Assert.Equal(ButtonReadings.ContentHeight, presenter.Bounds.Height, 0.001);
    }
}

/// <summary>
/// The 8.1 ToggleButton (<c>Controls/ToggleButton.axaml</c>) against the phone's style, the reference app's captures
/// (<see cref="ToggleButtonReadings"/>) and the probe of run <c>20261112-0755-dev11</c>. docs/controls/ToggleButton
/// has the plan and the comparison.
/// </summary>
public class ToggleButtonTests
{
    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ToggleButton Toggle(bool? isChecked = false, bool enabled = true) =>
        new() { Content = "toggle button", IsChecked = isChecked, IsEnabled = enabled };

    private static Border BorderOf(ToggleButton toggle) => ButtonFamily.Part<Border>(toggle, "EnabledBackground");

    private static Color Fill(ToggleButton toggle) => ButtonFamily.ColourOf(BorderOf(toggle).Background);

    private static Color Text(ToggleButton toggle) => ButtonFamily.ColourOf(ButtonFamily.PresenterOf(toggle).Foreground);

    // togglebutton.json's twelve setters, the content alignment WinRT has by default, the overhang and the padding;
    // and what the probe read: ClickMode Release, IsThreeState false, unchecked.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new ButtonStage<ToggleButton>(new ToggleButton { Content = "toggle button" }, ButtonFamily.VariantOf(variant));
        var toggle = stage.Button;

        ButtonFamily.AssertButtonSetters(toggle, variant == "Dark", stage.Window);
        Assert.Equal(ButtonReadings.Overhang, BorderOf(toggle).Margin);
        Assert.Equal(ClickMode.Release, toggle.ClickMode);
        Assert.False(toggle.IsThreeState);
        Assert.False(toggle.IsChecked);
    }

    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true));
        ButtonFamily.AssertBounds(stage.Button, BorderOf(stage.Button), stage.Window);
    }

    // Unchecked at rest (base, Normal): the border in the foreground, the page inside it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Unchecked_The_Border_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ButtonStage<ToggleButton>(Toggle(), ButtonFamily.VariantOf(variant));
        using var frame = stage.Capture();

        ButtonFamily.AssertBorder(frame, ButtonReadings.Foreground(dark), ButtonReadings.Page(dark), ButtonReadings.Page(dark));
    }

    // Checked (and the checked base capture): the accent inside the border, the text over it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Checked_Fills_With_The_Accent_And_Draws_The_Text_Over_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true), ButtonFamily.VariantOf(variant));

        Assert.Equal(Accent, Fill(stage.Button));
        Assert.Equal(OverAccent, Text(stage.Button));
        Assert.Equal(ButtonReadings.Foreground(dark), ButtonFamily.ColourOf(BorderOf(stage.Button).BorderBrush));
        using var frame = stage.Capture();
        ButtonFamily.AssertBorder(frame, ButtonReadings.Foreground(dark), ButtonReadings.Page(dark), Accent);
    }

    // The accent is the app's: PhoneAccentBrush follows SystemColorControlAccentColor.
    [AvaloniaFact]
    public void Checked_Follows_The_Apps_Accent()
    {
        var crimson = Color.FromUInt32(0xFFA20025);
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true));
        stage.Window.Resources["SystemColorControlAccentColor"] = crimson;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(crimson, Fill(stage.Button));
    }

    // Pressed and CheckedPressed set nothing: a press shows the unchecked look, whether checked or not, and the
    // release toggles (run 0755: Pressed, then Checked; CheckedPressed, then Normal).
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_Press_Shows_The_Unchecked_Look_And_The_Release_Toggles(bool isChecked)
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked));
        var toggle = stage.Button;

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsPressed);
        Assert.Equal(isChecked, toggle.IsChecked);
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));
        Assert.Equal(ButtonReadings.Foreground(dark: true), Text(toggle));

        stage.Window.MouseUp(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(!isChecked, toggle.IsChecked);
        Assert.Equal(isChecked ? ButtonReadings.Transparent : Accent, Fill(toggle));
    }

    // Run 0755: a checked ToggleButton pressed (CheckedPressed), the finger slid 90 below and lifted there. The phone
    // went back to Checked as the finger left it, and the lift didn't toggle it.
    [AvaloniaFact]
    public void Sliding_Off_A_Pressed_Checked_One_Shows_It_Checked_And_The_Lift_Off_It_Keeps_It()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true));
        var toggle = stage.Button;
        var off = stage.At(toggle.Bounds.Width / 2, toggle.Bounds.Height + 30);       // below it, on the stage

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));

        stage.Window.MouseMove(off);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsPressed);
        Assert.Equal(Accent, Fill(toggle));
        Assert.Equal(OverAccent, Text(toggle));

        stage.Window.MouseMove(stage.Centre);                                          // and back: CheckedPressed again
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));

        stage.Window.MouseMove(off);
        stage.Window.MouseUp(off, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsChecked);
        Assert.Equal(Accent, Fill(toggle));

        // The lift ends it: a press from the keyboard next is on it, CheckedPressed.
        toggle.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));
        stage.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
    }

    // The same under a finger, as on the phone (Avalonia's touch pointer is over the control while it touches it, and
    // not once it has slid off).
    [AvaloniaFact]
    public void Under_A_Finger_Checked_Looks_Unchecked_Until_It_Slides_Off()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true));
        var toggle = stage.Button;
        var off = stage.At(toggle.Bounds.Width / 2, toggle.Bounds.Height + 30);

        var finger = stage.Window.TouchBegin(stage.Centre);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsPressed);
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));

        stage.Window.TouchMove(finger, off);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Accent, Fill(toggle));

        stage.Window.TouchEnd(finger, off);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsChecked);
        Assert.Equal(Accent, Fill(toggle));
    }

    // A press from the keyboard is a press like any other, with no pointer off it: CheckedPressed, the unchecked look,
    // wherever the mouse is.
    [AvaloniaFact]
    public void Held_Space_On_A_Checked_One_Shows_The_Unchecked_Look()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true));
        var toggle = stage.Button;
        stage.Window.MouseMove(stage.At(toggle.Bounds.Width / 2, toggle.Bounds.Height + 30));
        toggle.Focus();

        stage.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(toggle.IsPressed);
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));

        stage.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.False(toggle.IsChecked);
    }

    // PointerOver and CheckedPointerOver: as Normal and Checked.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_Pointer_Over_It_Changes_Nothing(bool isChecked)
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked));
        stage.Window.MouseMove(stage.Centre);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsPointerOver);
        Assert.Equal(isChecked ? Accent : ButtonReadings.Transparent, Fill(stage.Button));
    }

    // Disabled (and IndeterminateDisabled): the border and the text in PhoneBaseLowColor, transparent inside whatever the
    // app set, as the phone's DisabledBackground draws them: #FF737373 and #FFBFBFBF on the page.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Border_And_The_Text_As_The_Phone_Does(string variant)
    {
        var dark = variant == "Dark";
        var toggle = Toggle(enabled: false);
        toggle.Background = Brushes.Red;
        using var stage = new ButtonStage<ToggleButton>(toggle, ButtonFamily.VariantOf(variant));

        Assert.Equal(ButtonReadings.Disabled(dark), ButtonFamily.ColourOf(BorderOf(toggle).BorderBrush));
        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));
        Assert.Equal(ButtonReadings.Disabled(dark), Text(toggle));
        Assert.Equal(1, BorderOf(toggle).Opacity);
        using var frame = stage.Capture();
        ButtonFamily.AssertBorder(frame, ButtonReadings.DisabledInk(dark), ButtonReadings.Page(dark), ButtonReadings.Page(dark));
    }

    // CheckedDisabled: the checked look, border, accent and text together at 0.4 on the page (#FF666666 and #FF192866
    // dark, #FF999999 and #FFB2C1FF light), within a step of rounding.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Checked_And_Disabled_Is_The_Checked_Look_At_Four_Tenths(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true, enabled: false), ButtonFamily.VariantOf(variant));
        var toggle = stage.Button;

        Assert.Equal(CheckedDisabledOpacity, BorderOf(toggle).Opacity);
        Assert.Equal(Accent, Fill(toggle));
        Assert.Equal(OverAccent, Text(toggle));
        Assert.Equal(ButtonReadings.Foreground(dark), ButtonFamily.ColourOf(BorderOf(toggle).BorderBrush));
        using var frame = stage.Capture();
        ButtonFamily.AssertBorder(frame, CheckedDisabledBorderInk(dark), ButtonReadings.Page(dark), CheckedDisabledFillInk(dark), tolerance: 1);
    }

    // Indeterminate and its PointerOver and Pressed set nothing: the unchecked look. IndeterminateDisabled is Disabled's.
    [AvaloniaFact]
    public void Indeterminate_Looks_Unchecked_And_Disabled_Looks_Disabled()
    {
        using var stage = new ButtonStage<ToggleButton>(new ToggleButton { Content = "toggle button", IsThreeState = true, IsChecked = null });
        var toggle = stage.Button;

        Assert.Equal(ButtonReadings.Transparent, Fill(toggle));
        Assert.Equal(ButtonReadings.Foreground(dark: true), Text(toggle));

        toggle.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ButtonReadings.Disabled(dark: true), ButtonFamily.ColourOf(BorderOf(toggle).BorderBrush));
        Assert.Equal(ButtonReadings.Disabled(dark: true), Text(toggle));
        Assert.Equal(1, BorderOf(toggle).Opacity);
    }

    // ThemeResource: the colours follow a change of variant, checked too.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle(isChecked: true), ThemeVariant.Dark);
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        using var frame = stage.Capture();
        ButtonFamily.AssertBorder(frame, ButtonReadings.Foreground(dark: false), ButtonReadings.Page(dark: false), Accent);
    }

    // The phone's ToggleButton runs no theme animation: no tilt, and a press transforms nothing.
    [AvaloniaFact]
    public void A_Press_Doesnt_Tilt()
    {
        using var clock = new TestClock();
        using var stage = new ButtonStage<ToggleButton>(Toggle());
        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(160);

        Assert.False(CfTilt.GetIsEnabled(stage.Button));
        Assert.All(stage.Button.GetVisualDescendants().OfType<Visual>(), v => Assert.Null(v.RenderTransform));
    }

    // The template's transparent root takes a press on the overhang above the border, as the phone's Grid does.
    [AvaloniaFact]
    public void A_Tap_On_The_Overhang_Toggles()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle());
        var above = stage.At(54, 4);

        stage.Window.MouseDown(above, MouseButton.Left);
        stage.Window.MouseUp(above, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsChecked);
    }

    // No FocusStates on the phone: keyboard focus draws nothing; Space toggles, as Avalonia's does.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new ButtonStage<ToggleButton>(Toggle());
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Button.IsFocused, "Tab didn't reach the ToggleButton");
        var layer = AdornerLayer.GetAdornerLayer(stage.Button);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // Data's line is the phone's 1.33 em (PhoneContentLine), and access keys are Avalonia's: Alt+T toggles "_Toggle".
    [AvaloniaFact]
    public void Datas_Line_Is_The_Phones_And_An_Access_Key_Toggles()
    {
        using var stage = new ButtonStage<ToggleButton>(new ToggleButton { Content = "_Toggle" });
        var text = ButtonFamily.PresenterOf(stage.Button).GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.IsType<AccessText>(text);
        Assert.Equal(1.33 * ButtonReadings.FontSize, text.LineHeight, 0.01);

        stage.Window.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Button.IsChecked);
    }

    /// <summary>Every concrete subclass of ToggleButton in Avalonia's assemblies and Cornflower's.</summary>
    public static TheoryData<string> Subclasses() => new(SubclassTypes().Select(t => t.FullName!));

    private static IEnumerable<Type> SubclassTypes() =>
        new[] { typeof(ToggleButton).Assembly, typeof(CfTilt).Assembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(ToggleButton)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

    // Avalonia finds a ControlTheme by the style key, the control's own type, so CheckBox, RadioButton, ToggleSwitch and
    // the rest keep their own theme (or none) and never take the plain ToggleButton's.
    [AvaloniaTheory]
    [MemberData(nameof(Subclasses))]
    public void A_Subclass_Doesnt_Take_The_ToggleButton_Theme(string name)
    {
        var wp81 = new Wp81Theme();
        Assert.True(wp81.TryGetResource(typeof(ToggleButton), ThemeVariant.Dark, out var found));
        var template = ((ControlTheme)found!).Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;

        var control = (ToggleButton)Activator.CreateInstance(SubclassTypes().Single(t => t.FullName == name))!;
        using var scene = new Scene(control, themes: [new SimpleTheme(), wp81]);

        Assert.NotSame(template, control.Template);
        Assert.DoesNotContain(control.GetVisualDescendants(), v => v is Border { Name: "EnabledBackground" });
    }

    [AvaloniaFact]
    public void The_Subclasses_Include_The_Ones_That_Matter()
    {
        var names = SubclassTypes().Select(t => t.Name).ToList();
        Assert.Contains("CheckBox", names);
        Assert.Contains("RadioButton", names);
        Assert.Contains("ToggleSwitch", names);
    }
}
