using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.CheckBoxReadings;
using CfTilt = Cornflower.Controls.Tilt;
using Path = Avalonia.Controls.Shapes.Path;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 CheckBox (<c>Controls/CheckBox.axaml</c>) against the phone's style and the reference app's captures
/// (<see cref="CheckBoxReadings"/>). docs/controls/CheckBox has the plan and the comparison.
/// </summary>
public class CheckBoxTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>
    /// A CheckBox on the reference app's stage: 450 × 100, the page's background, the CheckBox in a slot 19 in and 12 down.
    /// <see cref="CheckBoxFrames"/> saves the comparison's frames on it too.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, CheckBox? checkBox = null)
        {
            CheckBox = checkBox ?? new CheckBox { Content = "check box" };
            var slot = new Grid { Margin = Slot, Children = { CheckBox } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
        }

        public CheckBox CheckBox { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the CheckBox, in the window.</summary>
        public Point At(double x, double y) => CheckBox.TranslatePoint(new Point(x, y), Window)!.Value;

        /// <summary>The box's centre, in the window.</summary>
        public Point Box => At(Column / 2, GridTop + Column / 2);

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(CheckBox checkBox, string name) where T : Control =>
        checkBox.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Border BoxOf(CheckBox c) => Part<Border>(c, "CheckBackground");

    private static Rectangle RectangleOf(CheckBox c) => Part<Rectangle>(c, "NormalRectangle");

    private static Path TickOf(CheckBox c) => Part<Path>(c, "CheckGlyph");

    private static ContentPresenter PresenterOf(CheckBox c) => Part<ContentPresenter>(c, "PART_ContentPresenter");

    private static TextBlock TextOf(CheckBox c) => PresenterOf(c).GetVisualDescendants().OfType<TextBlock>().Single();

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    // The style's setters, as the dump read them (checkbox.json's keys, themeresources.json's values), and the parts'.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var checkBox = stage.CheckBox;

        Assert.Equal(Transparent, ColourOf(checkBox.Background));
        Assert.Equal(Foreground(dark), ColourOf(checkBox.BorderBrush));
        Assert.Equal(Foreground(dark), ColourOf(checkBox.Foreground));
        Assert.Equal(BorderThickness, checkBox.BorderThickness);
        Assert.Equal(FontSize, checkBox.FontSize);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), checkBox.FontFamily);
        Assert.Equal(HorizontalAlignment.Left, checkBox.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, checkBox.VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Left, checkBox.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, checkBox.VerticalAlignment);
        Assert.Equal(Padding, checkBox.Padding);
        Assert.Equal(MinWidth, checkBox.MinWidth);

        Assert.Equal(Overhang, Part<Grid>(checkBox, "Grid").Margin);
        Assert.Equal(Transparent, ColourOf(BoxOf(checkBox).Background));
        Assert.Equal(Foreground(dark), ColourOf(BoxOf(checkBox).BorderBrush));
        Assert.Equal(BorderThickness, BoxOf(checkBox).BorderThickness);
        Assert.Equal(Transparent, ColourOf(RectangleOf(checkBox).Fill));
        Assert.Equal(Foreground(dark), ColourOf(TickOf(checkBox).Fill));
        Assert.Same(stage.Window.FindResource("Wp81.Glyph.CheckMark"), TickOf(checkBox).Data);
        Assert.Equal(Padding, PresenterOf(checkBox).Margin);
    }

    // The phone's ContentPresenter sets its own font: PhoneFontFamilyNormal at TextStyleLargeFontSize, Normal. So the
    // CheckBox's own FontSize, FontFamily and FontWeight don't reach its text.
    [AvaloniaFact]
    public void The_Texts_Font_Is_The_Presenters_Own_As_On_The_Phone()
    {
        var checkBox = new CheckBox
        {
            Content = "check box", FontSize = 30, FontWeight = FontWeight.Bold, FontFamily = new FontFamily("Courier New"),
        };
        using var stage = new Stage(checkBox: checkBox);
        var text = TextOf(checkBox);

        Assert.Equal(FontSize, text.FontSize);
        Assert.Equal(FontWeight.Normal, text.FontWeight);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), text.FontFamily);
    }

    // The dump's bounds: the CheckBox, its Grid inside the overhang, the box, the tick, the rectangle and the content.
    // Where Avalonia rounds a size up and the phone to the nearest (ADR 0003), ours is the rounded-up size.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding()
    {
        using var stage = new Stage(checkBox: new CheckBox { Content = "check box", IsChecked = true });
        var checkBox = stage.CheckBox;
        var inStage = In(checkBox, (Visual)stage.Window.Content!);

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(OurWidth, checkBox.Bounds.Width, 0.001);
        Assert.Equal(Height, checkBox.Bounds.Height, 0.001);

        var grid = Part<Grid>(checkBox, "Grid");
        Assert.Equal(GridTop, In(grid, checkBox).Y, 0.001);
        Assert.Equal(Column, grid.ColumnDefinitions[0].ActualWidth, 0.001);

        var box = BoxOf(checkBox);
        Assert.Equal(0, In(box, checkBox).X, 0.001);
        Assert.Equal(GridTop, In(box, checkBox).Y, 0.001);
        Assert.Equal(OurBox, box.Bounds.Width, 0.001);
        Assert.Equal(OurBox, box.Bounds.Height, 0.001);

        var tick = TickOf(checkBox);
        var tickInBox = In(tick, box);
        Assert.Equal(OurGlyphLeft, tickInBox.X, 0.001);
        Assert.Equal(GlyphTop, tickInBox.Y, 0.001);
        Assert.Equal(Glyph.Width, tick.Bounds.Width, 0.001);
        Assert.Equal(Glyph.Height, tick.Bounds.Height, 0.001);

        var presenter = PresenterOf(checkBox);
        var at = In(presenter, checkBox);
        Assert.Equal(ContentAt.X, at.X, 0.001);
        Assert.Equal(ContentAt.Y, at.Y, 0.001);
        Assert.Equal(ContentHeight, presenter.Bounds.Height, 0.001);
    }

    // A string's line is the phone's, 1.33 em (ADR 0003), through PhoneContentLine, as in Button.
    [AvaloniaFact]
    public void A_Strings_Line_Is_The_Phones()
    {
        using var stage = new Stage();

        Assert.Equal(1.33 * FontSize, TextOf(stage.CheckBox).LineHeight, 0.01);
    }

    // Avalonia's access keys, as its own CheckBox themes have them (the phone had none): "_check" draws "check", and
    // Alt+C toggles it.
    [AvaloniaFact]
    public void An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Toggles_It()
    {
        using var stage = new Stage(checkBox: new CheckBox { Content = "_check" });
        var text = TextOf(stage.CheckBox);

        Assert.IsType<AccessText>(text);
        stage.Window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.CheckBox.IsChecked);
    }

    // The app's own ContentTemplate decides how its content is shown, through the phone's TemplateBinding.
    [AvaloniaFact]
    public void The_Apps_ContentTemplate_Is_Used()
    {
        var checkBox = new CheckBox
        {
            Content = "check box",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((s, _) => new TextBlock { Name = "Apps", Text = s }),
        };
        using var stage = new Stage(checkBox: checkBox);

        Assert.Equal("Apps", TextOf(checkBox).Name);
    }

    // Every pixel of the box's border where the PNG has it, and the page just outside and inside: the left and top
    // sides exactly; the right and bottom one raw pixel further out, as the box is 62 raw pixels to the phone's 61.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Box_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        AssertBox(frame, Foreground(dark), Page(dark));
    }

    private static void AssertBox(WriteableBitmap frame, Color ink, Color page)
    {
        var misses = new List<string>();
        void Expect(int x, int y, Color expected)
        {
            var got = Frames.PixelAt(frame, x, y);
            if (got != expected) misses.Add($"({x}, {y}): {Hex(got)}, expected {Hex(expected)}");
        }

        var right = PhoneRightColumns.Select(x => x + 1).ToArray();
        var bottom = PhoneBottomRows.Select(y => y + 1).ToArray();
        foreach (var x in LeftColumns.Concat(right)) Expect(x, ReadRow, ink);
        foreach (var y in TopRows.Concat(bottom)) Expect(ReadColumn, y, ink);
        foreach (var x in new[] { LeftColumns[0] - 1, LeftColumns[^1] + 1, right[0] - 1, right[^1] + 1 }) Expect(x, ReadRow, page);
        foreach (var y in new[] { TopRows[0] - 1, TopRows[^1] + 1, bottom[0] - 1, bottom[^1] + 1 }) Expect(ReadColumn, y, page);

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    private static void AssertTick(WriteableBitmap frame, Color? ink, Color page)
    {
        foreach (var p in TickInk)
            Assert.Equal(Hex(ink ?? page), Hex(Frames.PixelAt(frame, p.X, p.Y)));
        Assert.Equal(Hex(page), Hex(Frames.PixelAt(frame, ClearOfTheTick.X, ClearOfTheTick.Y)));
    }

    // CheckStates.Checked shows the tick, in the foreground; Unchecked shows none.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Checked_Shows_The_Tick_And_Unchecked_None(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new CheckBox { Content = "check box", IsChecked = true });

        Assert.True(TickOf(stage.CheckBox).IsVisible);
        Assert.False(RectangleOf(stage.CheckBox).IsVisible);
        using (var frame = stage.Capture())
        {
            AssertTick(frame, Foreground(dark), Page(dark));
            AssertBox(frame, Foreground(dark), Page(dark));
        }

        stage.CheckBox.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(TickOf(stage.CheckBox).IsVisible);
        using (var frame = stage.Capture())
            AssertTick(frame, null, Page(dark));
    }

    // CheckStates.Indeterminate shows NormalRectangle, 13 square in the box's middle, filled with
    // CheckBoxBackgroundThemeBrush: transparent, so the phone draws an indeterminate CheckBox as an unchecked one,
    // pixel for pixel (the captures' hashes are the same). Ours too.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Indeterminate_Shows_A_Transparent_Rectangle_So_It_Looks_Unchecked(string variant)
    {
        using var unchecked_ = new Stage(VariantOf(variant));
        using var plain = unchecked_.Capture();
        using var stage = new Stage(VariantOf(variant), new CheckBox { Content = "check box", IsThreeState = true, IsChecked = null });
        var rectangle = RectangleOf(stage.CheckBox);

        Assert.True(rectangle.IsVisible);
        Assert.False(TickOf(stage.CheckBox).IsVisible);
        Assert.Equal(Transparent, ColourOf(rectangle.Fill));
        Assert.Equal(OurRectangle, rectangle.Bounds.Width, 0.001);
        var centre = In(rectangle, BoxOf(stage.CheckBox)) + new Point(rectangle.Bounds.Width / 2, rectangle.Bounds.Height / 2);
        Assert.Equal(Column / 2, centre.X, 0.25);
        using var frame = stage.Capture();
        Assert.True(Frames.SameAs(plain, frame), "the indeterminate CheckBox isn't drawn as the unchecked one");
    }

    // An app's CheckBoxBackgroundThemeBrush fills the rectangle, as on the phone. The key is the box's background
    // too, so the box is given its own here: the rectangle is drawn in the middle, and the box round it is clear.
    [AvaloniaFact]
    public void An_Apps_Background_Key_Fills_The_Rectangle()
    {
        var checkBox = new CheckBox { Content = "check box", IsThreeState = true, IsChecked = null, Background = Brushes.Transparent };
        checkBox.Resources["CheckBoxBackgroundThemeBrush"] = new SolidColorBrush(Accent);
        using var stage = new Stage(checkBox: checkBox);

        Assert.Equal(Accent, ColourOf(RectangleOf(checkBox).Fill));
        using var frame = stage.Capture();
        Assert.Equal(Hex(Accent), Hex(Frames.PixelAt(frame, 76, 97)));                              // the box's middle
        Assert.Equal(Hex(Page(dark: true)), Hex(Frames.PixelAt(frame, ClearOfTheTick.X, ClearOfTheTick.Y)));   // the box, outside the rectangle
    }

    // CommonStates.Pressed: the box fills with the accent, the tick and the rectangle over it as the phone's are; the
    // border and the text stay the foreground. Back on release, toggled.
    [AvaloniaTheory]
    [InlineData("Dark", true)]
    [InlineData("Dark", false)]
    [InlineData("Dark", null)]
    [InlineData("Light", true)]
    [InlineData("Light", false)]
    [InlineData("Light", null)]
    public void Pressed_Fills_The_Box_With_The_Accent(string variant, bool? isChecked)
    {
        var dark = variant == "Dark";
        using var clock = new TestClock();
        using var stage = new Stage(VariantOf(variant), new CheckBox { Content = "check box", IsThreeState = isChecked is null, IsChecked = isChecked });
        var checkBox = stage.CheckBox;

        stage.Window.MouseDown(stage.Box, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(checkBox.IsPressed);
        Assert.Equal(Accent, ColourOf(BoxOf(checkBox).Background));
        Assert.Equal(OverAccent, ColourOf(TickOf(checkBox).Fill));
        Assert.Equal(Accent, ColourOf(RectangleOf(checkBox).Fill));
        Assert.Equal(Foreground(dark), ColourOf(BoxOf(checkBox).BorderBrush));
        Assert.Equal(Foreground(dark), ColourOf(PresenterOf(checkBox).Foreground));

        stage.Window.MouseUp(stage.Box, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Transparent, ColourOf(BoxOf(checkBox).Background));
        Assert.Equal(Foreground(dark), ColourOf(TickOf(checkBox).Fill));
        Assert.Equal(Transparent, ColourOf(RectangleOf(checkBox).Fill));
        Assert.Equal(isChecked == false, checkBox.IsChecked);      // two-state: checked to unchecked and back; indeterminate to unchecked
    }

    // The pressed box drawn, untilted: the accent inside the border, round the white tick.
    [AvaloniaFact]
    public void Pressed_Draws_The_Accent_Round_The_Tick()
    {
        var checkBox = new CheckBox { Content = "check box", IsChecked = true };
        using var stage = new Stage(checkBox: checkBox);
        CfTilt.SetIsEnabled(checkBox, false);                  // the colours, not the tilt
        stage.Window.MouseDown(stage.Box, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        using var frame = stage.Capture();
        AssertTick(frame, OverAccent, Accent);
    }

    // CommonStates.Disabled: the border, the tick and the text in PhoneBaseLowColor; the rectangle transparent.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Border_The_Tick_And_The_Text(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new CheckBox { Content = "check box", IsChecked = true, IsEnabled = false });
        var checkBox = stage.CheckBox;

        Assert.Equal(Disabled(dark), ColourOf(BoxOf(checkBox).BorderBrush));
        Assert.Equal(Disabled(dark), ColourOf(TickOf(checkBox).Fill));
        Assert.Equal(Transparent, ColourOf(RectangleOf(checkBox).Fill));
        Assert.Equal(Disabled(dark), ColourOf(PresenterOf(checkBox).Foreground));
        using var frame = stage.Capture();
        AssertBox(frame, DisabledInk(dark), Page(dark));
        AssertTick(frame, DisabledInk(dark), Page(dark));
    }

    // The phone's Disabled state leaves CheckBackground's Background alone (unlike Button's): an app's background stays.
    [AvaloniaFact]
    public void Disabled_Keeps_The_Apps_Background()
    {
        using var stage = new Stage(checkBox: new CheckBox { Content = "check box", Background = Brushes.Red, IsEnabled = false });

        Assert.Equal(Colors.Red, ColourOf(BoxOf(stage.CheckBox).Background));
    }

    // CommonStates.PointerOver and MouseOver change nothing on the phone: their captures are the base's, pixel for pixel.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Pointer_Over_It_Changes_Nothing(string variant)
    {
        using var stage = new Stage(VariantOf(variant), new CheckBox { Content = "check box", IsChecked = true });
        using var before = stage.Capture();

        stage.Window.MouseMove(stage.Box);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.CheckBox.IsPointerOver);
        using var after = stage.Capture();
        Assert.True(Frames.SameAs(before, after), "the pointer over the CheckBox changed what is drawn");
    }

    // A tap toggles, and the marks follow: unchecked, checked, and with IsThreeState, indeterminate, then unchecked:
    // WinRT ToggleButton's order, which Avalonia's keeps.
    [AvaloniaFact]
    public void A_Tap_Toggles_And_The_Marks_Follow()
    {
        using var clock = new TestClock();
        using var stage = new Stage(checkBox: new CheckBox { Content = "check box", IsThreeState = true });
        var checkBox = stage.CheckBox;
        var seen = new List<string>();
        var time = 0.0;

        for (var i = 0; i < 3; i++)
        {
            var touch = stage.Window.TouchBegin(stage.Box);
            clock.At(time += SettleMs);
            stage.Window.TouchEnd(touch, stage.Box);
            clock.At(time += 1000);
            seen.Add($"{checkBox.IsChecked?.ToString() ?? "null"} tick {TickOf(checkBox).IsVisible} rectangle {RectangleOf(checkBox).IsVisible}");
        }

        Assert.Equal(["True tick True rectangle False", "null tick False rectangle True", "False tick False rectangle False"], seen);
    }

    // The touch target is the whole CheckBox, the overhang above and below the box included, as the phone's
    // transparent root Grid is, and the text beside the box.
    [AvaloniaTheory]
    [InlineData(12, 4)]           // above the box, in the overhang
    [InlineData(12, 54)]          // below it
    [InlineData(100, 28)]         // on the text
    public void A_Tap_Anywhere_On_It_Toggles(double x, double y)
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var at = stage.At(x, y);

        var touch = stage.Window.TouchBegin(at);
        clock.At(SettleMs);
        stage.Window.TouchEnd(touch, at);
        clock.At(SettleMs + 1000);

        Assert.True(stage.CheckBox.IsChecked);
        Assert.Equal(TiltState.Rest, CfTilt.Current(stage.CheckBox));
    }

    // The phone's PointerDownThemeAnimation targets its Grid, inside the overhang: the box and the text tilt together,
    // inside a root that doesn't.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Grid_Inside_An_Untilted_Root()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var checkBox = stage.CheckBox;
        var root = (Control)checkBox.GetVisualChildren().Single();
        var grid = Part<Grid>(checkBox, "Grid");

        stage.Window.MouseDown(stage.Box, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(CfTilt.GetIsEnabled(checkBox));
        Assert.Same(grid, CfTilt.TargetOf(checkBox));
        Assert.Equal(checkBox.Bounds.Width, grid.Bounds.Width);
        Assert.Equal(GridTop, grid.Bounds.Top, 0.001);                 // its layout slot in the untilted root, not where it is drawn
        Assert.Equal(OurBox, grid.Bounds.Height, 0.001);
        Assert.IsType<MatrixTransform>(grid.RenderTransform);
        Assert.Null(root.RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(checkBox));
    }

    // The phone's template has no FocusStates group: keyboard focus draws nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new Stage();
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.CheckBox.IsFocused, "Tab didn't reach the CheckBox");
        var layer = AdornerLayer.GetAdornerLayer(stage.CheckBox);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // ThemeResource: the colours follow a change of variant.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark, new CheckBox { Content = "check box", IsChecked = true });
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Foreground(dark: false), ColourOf(stage.CheckBox.Foreground));
        using var frame = stage.Capture();
        AssertBox(frame, Foreground(dark: false), Page(dark: false));
        AssertTick(frame, Foreground(dark: false), Page(dark: false));
    }

    // The pressed box is CheckBoxPressedBackgroundThemeBrush itself, the accent brush, so it follows an app's accent
    // (SystemColorControlAccentColor; Tokens/TokenTests holds the accent brushes to it).
    [AvaloniaFact]
    public void The_Pressed_Box_Is_The_Accent_Brush()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        stage.Window.MouseDown(stage.Box, MouseButton.Left);
        clock.At(SettleMs);

        Assert.Same(stage.Window.FindResource(stage.Window.ActualThemeVariant, "CheckBoxPressedBackgroundThemeBrush"), BoxOf(stage.CheckBox).Background);
    }
}
