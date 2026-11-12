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
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.RadioButtonReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 RadioButton (<c>Controls/RadioButton.axaml</c>) against the phone's style and the reference app's captures
/// (<see cref="RadioButtonReadings"/>). docs/controls/RadioButton has the plan and the comparison.
/// </summary>
public class RadioButtonTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>
    /// A RadioButton on the reference app's stage: 450 × 100, the page's background, the RadioButton in a slot 19 in and
    /// 12 down. <see cref="RadioButtonFrames"/> saves the comparison's frames on it too.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, RadioButton? radioButton = null, double scaling = Scene.ReferenceScaling)
        {
            RadioButton = radioButton ?? new RadioButton { Content = "radio button" };
            var slot = new Grid { Margin = Slot, Children = { RadioButton } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: StageHeight, scaling: scaling);
        }

        public RadioButton RadioButton { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the RadioButton, in the window.</summary>
        public Point At(double x, double y) => RadioButton.TranslatePoint(new Point(x, y), Window)!.Value;

        /// <summary>The circle's centre, in the window.</summary>
        public Point Circle => At(Column / 2, ContainerTop + Column / 2);

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(RadioButton radioButton, string name) where T : Control =>
        radioButton.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Ellipse CircleOf(RadioButton r) => Part<Ellipse>(r, "CheckBackground");

    private static Ellipse DotOf(RadioButton r) => Part<Ellipse>(r, "CheckMark");

    private static Grid ContainerOf(RadioButton r) => Part<Grid>(r, "Container");

    /// <summary>The circle's cell: the phone's unnamed Grid holding the circle, the dot and the empty TextBlock (the spacer).</summary>
    private static Panel CellOf(RadioButton r) => (Panel)CircleOf(r).GetVisualParent()!;

    /// <summary>The phone's empty TextBlock in the circle's cell, as <see cref="PhoneLineSpacer"/>.</summary>
    private static PhoneLineSpacer SpacerOf(RadioButton r) => CellOf(r).Children.OfType<PhoneLineSpacer>().Single();

    private static ContentPresenter PresenterOf(RadioButton r) => Part<ContentPresenter>(r, "PART_ContentPresenter");

    private static TextBlock TextOf(RadioButton r) => PresenterOf(r).GetVisualDescendants().OfType<TextBlock>().Single();

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    /// <summary>Presses and lifts a finger at <paramref name="at"/>, settling after each.</summary>
    private static double Tap(Stage stage, TestClock clock, Point at, double time)
    {
        var touch = stage.Window.TouchBegin(at);
        clock.At(time += SettleMs);
        stage.Window.TouchEnd(touch, at);
        clock.At(time += 1000);
        return time;
    }

    // The style's setters, as the dump read them (radiobutton.json's keys, themeresources.json's values), and the parts'.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var radio = stage.RadioButton;
        // The window's own font is Selawik too (Wp81's ContentControlThemeFontFamily), so give it another: the
        // RadioButton's is the style's, not inherited.
        stage.Window.FontFamily = new FontFamily("Courier New");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Transparent, ColourOf(radio.Background));
        Assert.Equal(Foreground(dark), ColourOf(radio.BorderBrush));
        Assert.Equal(Foreground(dark), ColourOf(radio.Foreground));
        Assert.Equal(FontSize, radio.FontSize);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), radio.FontFamily);
        Assert.Equal(HorizontalAlignment.Left, radio.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, radio.VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Left, radio.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, radio.VerticalAlignment);
        Assert.Equal(Padding, radio.Padding);
        Assert.Equal(MinWidth, radio.MinWidth);

        Assert.Equal(Overhang, ContainerOf(radio).Margin);
        Assert.Equal(Transparent, ColourOf(CircleOf(radio).Fill));
        Assert.Equal(Foreground(dark), ColourOf(CircleOf(radio).Stroke));
        Assert.Equal(StrokeThickness, CircleOf(radio).StrokeThickness);
        Assert.Equal(Foreground(dark), ColourOf(DotOf(radio).Fill));
        Assert.Equal(Padding, PresenterOf(radio).Margin);
        Assert.Equal(Foreground(dark), ColourOf(PresenterOf(radio).Foreground));
    }

    // The phone's ContentPresenter takes its font from the RadioButton (TemplateBinding FontFamily and FontSize; ours
    // inherits it), unlike the phone's CheckBox's: so the RadioButton's own FontSize and FontFamily reach its text.
    [AvaloniaFact]
    public void The_Texts_Font_Is_The_RadioButtons()
    {
        var radio = new RadioButton { Content = "radio button", FontSize = 30, FontFamily = new FontFamily("Courier New") };
        using var stage = new Stage(radioButton: radio);
        var text = TextOf(radio);

        Assert.Equal(30, text.FontSize);
        Assert.Equal(radio.FontFamily, text.FontFamily);
        Assert.Equal(30, PresenterOf(radio).FontSize);
    }

    // The dump's bounds: the RadioButton, its Container inside the overhang, the circle, the dot, the empty TextBlock's
    // line and the content. Where Avalonia rounds a size up and the phone to the nearest (ADR 0003), ours is the rounded-up size.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding()
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "radio button", IsChecked = true });
        var radio = stage.RadioButton;
        var inStage = In(radio, (Visual)stage.Window.Content!);

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(OurWidth, radio.Bounds.Width, 0.001);
        Assert.Equal(Height, radio.Bounds.Height, 0.001);

        var container = ContainerOf(radio);
        Assert.Equal(ContainerTop, In(container, radio).Y, 0.001);
        Assert.Equal(Column, container.ColumnDefinitions[0].ActualWidth, 0.001);

        var circle = CircleOf(radio);
        Assert.Equal(0, In(circle, radio).X, 0.001);
        Assert.Equal(ContainerTop, In(circle, radio).Y, 0.001);
        Assert.Equal(OurCircle, circle.Bounds.Width, 0.001);
        Assert.Equal(OurCircle, circle.Bounds.Height, 0.001);

        var dot = DotOf(radio);
        var dotInCircle = In(dot, circle);
        Assert.Equal(DotIn, dotInCircle.X, 0.001);
        Assert.Equal(DotIn, dotInCircle.Y, 0.001);
        Assert.Equal(Dot, dot.Bounds.Width, 0.001);
        Assert.Equal(Dot, dot.Bounds.Height, 0.001);

        // The empty TextBlock: one of the phone's lines tall, rounded up to the raw pixel grid, and nothing wide; the cell
        // is the circle's size.
        Assert.Equal(EmptyLine, SpacerOf(radio).DesiredSize.Height, 1 / 2.4);
        Assert.Equal(0, SpacerOf(radio).DesiredSize.Width);
        Assert.Equal(OurCircle, CellOf(radio).Bounds.Width, 0.001);
        Assert.Equal(OurCircle, CellOf(radio).Bounds.Height, 0.001);

        var presenter = PresenterOf(radio);
        var at = In(presenter, radio);
        Assert.Equal(ContentAt.X, at.X, 0.001);
        Assert.Equal(ContentAt.Y, at.Y, 0.001);
        Assert.Equal(ContentHeight, presenter.Bounds.Height, 0.001);
    }

    // The phone's empty TextBlock in the circle's cell is one line of the RadioButton's font: with a larger FontSize
    // its line is taller than the circle, the cell grows to it, and the circle (centred in the cell) is centred on the
    // text's first line. At 30 the phone's line is 39.9.
    [AvaloniaFact]
    public void A_Larger_Font_Centres_The_Circle_On_The_Texts_First_Line()
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "radio button", FontSize = 30, IsChecked = true });
        var radio = stage.RadioButton;
        var line = 1.33 * 30;

        Assert.Equal(line, CellOf(radio).Bounds.Height, 1 / 2.4);
        Assert.Equal(line, TextOf(radio).Bounds.Height, 1 / 2.4);
        var circleMiddle = In(CircleOf(radio), radio).Y + CircleOf(radio).Bounds.Height / 2;
        var textMiddle = In(TextOf(radio), radio).Y + TextOf(radio).Bounds.Height / 2;
        Assert.Equal(textMiddle, circleMiddle, 1 / 2.4);
        var dotMiddle = In(DotOf(radio), radio).Y + Dot / 2;
        Assert.Equal(circleMiddle, dotMiddle, 1 / 2.4);

        radio.FontSize = FontSize;                         // back to the default: the cell is the circle again
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(OurCircle, CellOf(radio).Bounds.Height, 0.001);
    }

    // A deliberate difference (PLAN.md, "Decided on the user's behalf"), not a reading of the phone's: an app's style for
    // TextBlock (Selector="TextBlock") doesn't reach the circle's cell, as the phone's empty TextBlock is a spacer here,
    // so a margin or size for TextBlocks doesn't move the circle off the text.
    [AvaloniaFact]
    public void An_Apps_TextBlock_Style_Doesnt_Move_The_Circle()
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "radio button", IsChecked = true });
        stage.Window.Styles.Add(new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, 20)), new Setter(TextBlock.FontSizeProperty, 40.0) },
        });
        Dispatcher.UIThread.RunJobs();
        var radio = stage.RadioButton;

        Assert.Equal(OurCircle, CellOf(radio).Bounds.Height, 0.001);
        Assert.Equal(ContainerTop, In(CircleOf(radio), radio).Y, 0.001);
    }

    // The dot is centred on the circle across and down alike at any scale. Avalonia rounds the 25.5 column to the
    // nearest raw pixel and the 25.5 circle up, so they can differ by one; the cell is the circle's size, not the
    // column's. At 3.0 the column is 76 raw pixels and the circle 77.
    [AvaloniaTheory]
    [InlineData(2.4)]
    [InlineData(3.0)]
    [InlineData(1.75)]
    public void The_Dot_Is_Centred_On_The_Circle_At_Any_Scale(double scaling)
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "radio button", IsChecked = true }, scaling: scaling);
        var dot = In(DotOf(stage.RadioButton), CircleOf(stage.RadioButton));

        Assert.Equal(dot.X, dot.Y, 0.001);
    }

    // A string's line is the phone's, 1.33 em (ADR 0003), through PhoneContentLine, as in Button.
    [AvaloniaFact]
    public void A_Strings_Line_Is_The_Phones()
    {
        using var stage = new Stage();

        Assert.Equal(1.33 * FontSize, TextOf(stage.RadioButton).LineHeight, 0.01);
    }

    // Avalonia's access keys, as its own RadioButton themes have them (the phone had none): "_radio" draws "radio", and
    // Alt+R checks it.
    [AvaloniaFact]
    public void An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Checks_It()
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "_radio" });
        var text = TextOf(stage.RadioButton);

        Assert.IsType<AccessText>(text);
        stage.Window.KeyPressQwerty(PhysicalKey.R, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.RadioButton.IsChecked);
    }

    // The app's own ContentTemplate decides how its content is shown, through the phone's TemplateBinding.
    [AvaloniaFact]
    public void The_Apps_ContentTemplate_Is_Used()
    {
        var radio = new RadioButton
        {
            Content = "radio button",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((s, _) => new TextBlock { Name = "Apps", Text = s }),
        };
        using var stage = new Stage(radioButton: radio);

        Assert.Equal("Apps", TextOf(radio).Name);
    }

    // The ring on the circle's middle row and column where the PNG has it, and the page just outside and inside: the
    // left and top sides exactly; the right and bottom one raw pixel further out, as the circle is 62 raw pixels to the
    // phone's 61.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Ring_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        AssertRing(frame, Foreground(dark), Page(dark));
        AssertDot(frame, null, Page(dark));
    }

    /// <summary>Pixels of a frame against their expected colours, every miss listed at once.</summary>
    private sealed class PixelCheck(WriteableBitmap frame)
    {
        private readonly List<string> _misses = [];

        public void Expect(int x, int y, Color expected)
        {
            var got = Frames.PixelAt(frame, x, y);
            if (got != expected) _misses.Add($"({x}, {y}): {Hex(got)}, expected {Hex(expected)}");
        }

        public void AssertNone() => Assert.True(_misses.Count == 0, string.Join("\n", _misses));
    }

    private static void AssertRing(WriteableBitmap frame, Color ink, Color page, Color? inside = null)
    {
        var check = new PixelCheck(frame);
        var Expect = check.Expect;

        foreach (var x in LeftColumns.Concat(OurRightColumns)) Expect(x, MiddleRow, ink);
        foreach (var y in TopRows.Concat(OurBottomRows)) Expect(MiddleColumn, y, ink);
        foreach (var x in new[] { LeftColumns[0] - 1, OurRightColumns[^1] + 1 }) Expect(x, MiddleRow, page);
        foreach (var y in new[] { TopRows[0] - 1, OurBottomRows[^1] + 1 }) Expect(MiddleColumn, y, page);
        foreach (var x in new[] { LeftColumns[^1] + 1, OurRightColumns[0] - 1 }) Expect(x, MiddleRow, inside ?? page);
        foreach (var y in new[] { TopRows[^1] + 1, OurBottomRows[0] - 1 }) Expect(MiddleColumn, y, inside ?? page);
        check.AssertNone();
    }

    /// <summary>
    /// The dot's ends on the middle row and column, and a pixel of the page just past each, against the PNG; with no
    /// <paramref name="ink"/>, no dot: the circle's inside is <paramref name="inside"/> there.
    /// </summary>
    private static void AssertDot(WriteableBitmap frame, Color? ink, Color inside)
    {
        var check = new PixelCheck(frame);
        var Expect = check.Expect;

        // The phone's ends on its middle row are a little short of whole; ours, on the same pixels, are whole.
        foreach (var x in new[] { DotLeft, MiddleColumn, DotRight }) Expect(x, MiddleRow, ink ?? inside);
        foreach (var y in new[] { DotTop, MiddleRow, DotBottom }) Expect(MiddleColumn, y, ink ?? inside);
        foreach (var x in new[] { DotLeft - 1, DotRight + 1 }) Expect(x, MiddleRow, inside);
        foreach (var y in new[] { DotTop - 1, DotBottom + 1 }) Expect(MiddleColumn, y, inside);
        check.AssertNone();
    }

    // CheckStates.Checked shows the dot, in the foreground, on the phone's pixels; Unchecked shows none.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Checked_Shows_The_Dot_And_Unchecked_None(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new RadioButton { Content = "radio button", IsChecked = true });

        Assert.True(DotOf(stage.RadioButton).IsVisible);
        using (var frame = stage.Capture())
        {
            AssertDot(frame, Foreground(dark), Page(dark));
            AssertRing(frame, Foreground(dark), Page(dark));
        }

        stage.RadioButton.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(DotOf(stage.RadioButton).IsVisible);
        using (var frame = stage.Capture())
            AssertDot(frame, null, Page(dark));
    }

    // CheckStates.Indeterminate sets nothing on the phone, so an indeterminate RadioButton is drawn as an unchecked one:
    // the phone's Indeterminate captures are its Unchecked ones, byte for byte. Ours too.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Indeterminate_Is_Drawn_As_Unchecked(string variant)
    {
        using var unchecked_ = new Stage(VariantOf(variant));
        using var plain = unchecked_.Capture();
        using var stage = new Stage(VariantOf(variant), new RadioButton { Content = "radio button", IsThreeState = true, IsChecked = null });

        Assert.False(DotOf(stage.RadioButton).IsVisible);
        using var frame = stage.Capture();
        Assert.True(Frames.SameAs(plain, frame), "the indeterminate RadioButton isn't drawn as the unchecked one");
    }

    // CommonStates.Pressed: the circle fills with ButtonPressedBackgroundThemeBrush, the accent, the dot over it in
    // ButtonPressedForegroundThemeBrush, white; the ring and the text stay the foreground. Back on release, checked.
    [AvaloniaTheory]
    [InlineData("Dark", true)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Light", false)]
    public void Pressed_Fills_The_Circle_With_The_Accent(string variant, bool isChecked)
    {
        var dark = variant == "Dark";
        using var clock = new TestClock();
        using var stage = new Stage(VariantOf(variant), new RadioButton { Content = "radio button", IsChecked = isChecked });
        var radio = stage.RadioButton;

        stage.Window.MouseDown(stage.Circle, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(radio.IsPressed);
        Assert.Equal(Accent, ColourOf(CircleOf(radio).Fill));
        Assert.Equal(OverAccent, ColourOf(DotOf(radio).Fill));
        Assert.Equal(isChecked, DotOf(radio).IsVisible);
        Assert.Equal(Foreground(dark), ColourOf(CircleOf(radio).Stroke));
        Assert.Equal(Foreground(dark), ColourOf(PresenterOf(radio).Foreground));

        stage.Window.MouseUp(stage.Circle, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Transparent, ColourOf(CircleOf(radio).Fill));
        Assert.Equal(Foreground(dark), ColourOf(DotOf(radio).Fill));
        Assert.True(radio.IsChecked);                    // a RadioButton is checked by a tap, and stays checked
        Assert.True(DotOf(radio).IsVisible);
    }

    // The pressed circle drawn, untilted: the accent inside the ring, round the white dot; unchecked, the accent alone.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pressed_Draws_The_Accent_Round_The_Dot(bool isChecked)
    {
        var radio = new RadioButton { Content = "radio button", IsChecked = isChecked };
        using var stage = new Stage(radioButton: radio);
        CfTilt.SetIsEnabled(radio, false);                  // the colours, not the tilt
        stage.Window.MouseDown(stage.Circle, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        using var frame = stage.Capture();
        AssertDot(frame, isChecked ? OverAccent : null, Accent);
        AssertRing(frame, Foreground(dark: true), Page(dark: true), inside: Accent);
    }

    // The pressed circle is ButtonPressedBackgroundThemeBrush itself, the accent brush, so it follows an app's accent
    // (SystemColorControlAccentColor; Tokens/TokenTests holds the accent brushes to it).
    [AvaloniaFact]
    public void The_Pressed_Circle_Is_The_Accent_Brush()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        stage.Window.MouseDown(stage.Circle, MouseButton.Left);
        clock.At(SettleMs);

        Assert.Same(stage.Window.FindResource(stage.Window.ActualThemeVariant, "ButtonPressedBackgroundThemeBrush"), CircleOf(stage.RadioButton).Fill);
    }

    // CommonStates.Disabled: the ring and the dot in ButtonDisabledBorderThemeBrush, the text in
    // ButtonDisabledForegroundThemeBrush, both PhoneBaseLowColor: #FF737373 and #FFBFBFBF on the page.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Ring_The_Dot_And_The_Text(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new RadioButton { Content = "radio button", IsChecked = true, IsEnabled = false });
        var radio = stage.RadioButton;

        Assert.Equal(Disabled(dark), ColourOf(CircleOf(radio).Stroke));
        Assert.Equal(Disabled(dark), ColourOf(DotOf(radio).Fill));
        Assert.Equal(Disabled(dark), ColourOf(PresenterOf(radio).Foreground));
        Assert.Equal(Transparent, ColourOf(CircleOf(radio).Fill));
        using var frame = stage.Capture();
        AssertRing(frame, DisabledInk(dark), Page(dark));
        AssertDot(frame, DisabledInk(dark), Page(dark));
    }

    // The phone's Disabled state leaves CheckBackground's Fill alone: an app's background stays.
    [AvaloniaFact]
    public void Disabled_Keeps_The_Apps_Background()
    {
        using var stage = new Stage(radioButton: new RadioButton { Content = "radio button", Background = Brushes.Red, IsEnabled = false });

        Assert.Equal(Colors.Red, ColourOf(CircleOf(stage.RadioButton).Fill));
    }

    // CommonStates.PointerOver and MouseOver change nothing on the phone: their captures are the base's, pixel for pixel.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Pointer_Over_It_Changes_Nothing(string variant)
    {
        using var stage = new Stage(VariantOf(variant), new RadioButton { Content = "radio button", IsChecked = true });
        using var before = stage.Capture();

        stage.Window.MouseMove(stage.Circle);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.RadioButton.IsPointerOver);
        using var after = stage.Capture();
        Assert.True(Frames.SameAs(before, after), "the pointer over the RadioButton changed what is drawn");
    }

    // A tap checks it, and a second tap leaves it checked: Avalonia's RadioButton, as WinRT's, never unchecks itself.
    [AvaloniaFact]
    public void A_Tap_Checks_It_And_Another_Leaves_It_Checked()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var radio = stage.RadioButton;

        var time = Tap(stage, clock, stage.Circle, 0);
        Assert.True(radio.IsChecked);
        Assert.True(DotOf(radio).IsVisible);

        Tap(stage, clock, stage.Circle, time);
        Assert.True(radio.IsChecked);
        Assert.True(DotOf(radio).IsVisible);
    }

    // The touch target is the whole RadioButton, the overhang above and below the circle included, as the phone's
    // transparent root Grid is, and the text beside the circle.
    [AvaloniaTheory]
    [InlineData(12, 4)]           // above the circle, in the overhang
    [InlineData(12, 54)]          // below it
    [InlineData(100, 28)]         // on the text
    public void A_Tap_Anywhere_On_It_Checks_It(double x, double y)
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        Tap(stage, clock, stage.At(x, y), 0);

        Assert.True(stage.RadioButton.IsChecked);
        Assert.Equal(TiltState.Rest, CfTilt.Current(stage.RadioButton));
    }

    // The phone's PointerDownThemeAnimation targets its Container, inside the overhang: the circle and the text tilt
    // together, inside a root that doesn't.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Container_Inside_An_Untilted_Root()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var radio = stage.RadioButton;
        var root = (Control)radio.GetVisualChildren().Single();
        var container = ContainerOf(radio);

        stage.Window.MouseDown(stage.Circle, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(CfTilt.GetIsEnabled(radio));
        Assert.Same(container, CfTilt.TargetOf(radio));
        Assert.Equal(radio.Bounds.Width, container.Bounds.Width);
        Assert.Equal(ContainerTop, container.Bounds.Top, 0.001);           // its layout slot in the untilted root, not where it is drawn
        Assert.Equal(OurCircle, container.Bounds.Height, 0.001);
        Assert.IsType<MatrixTransform>(container.RenderTransform);
        Assert.Null(root.RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(radio));
    }

    // The phone's template has no FocusStates group: keyboard focus draws nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new Stage();
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.RadioButton.IsFocused, "Tab didn't reach the RadioButton");
        var layer = AdornerLayer.GetAdornerLayer(stage.RadioButton);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // ThemeResource: the colours follow a change of variant.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark, new RadioButton { Content = "radio button", IsChecked = true });
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Foreground(dark: false), ColourOf(stage.RadioButton.Foreground));
        using var frame = stage.Capture();
        AssertRing(frame, Foreground(dark: false), Page(dark: false));
        AssertDot(frame, Foreground(dark: false), Page(dark: false));
    }

    // One checked in a group is Avalonia's RadioButton's own, as it is WinRT's: RadioButtons with no GroupName are a
    // group by their parent; a GroupName makes a group across parents. Under the theme a tap on another checks it, and
    // the one checked before loses its dot.
    [AvaloniaFact]
    public void A_Tap_On_Another_In_Its_Parent_Moves_The_Dot()
    {
        using var clock = new TestClock();
        RadioButton Make(string text, bool isChecked = false) => new() { Content = text, IsChecked = isChecked };
        var first = Make("first", isChecked: true);
        var second = Make("second");
        var elsewhere = Make("elsewhere", isChecked: true);
        var panel = new StackPanel { Children = { new StackPanel { Children = { first, second } }, new StackPanel { Children = { elsewhere } } } };
        using var scene = new Scene(panel);

        var at = second.TranslatePoint(new Point(Column / 2, ContainerTop + Column / 2), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at);
        clock.At(SettleMs);
        scene.Window.TouchEnd(touch, at);
        clock.At(SettleMs + 1000);

        Assert.Equal([false, true, true], new[] { first.IsChecked, second.IsChecked, elsewhere.IsChecked });
        Assert.False(DotOf(first).IsVisible);
        Assert.True(DotOf(second).IsVisible);
        Assert.True(DotOf(elsewhere).IsVisible);          // another parent, another group
    }

    // A GroupName groups RadioButtons across parents, and leaves those of another group alone.
    [AvaloniaFact]
    public void A_GroupName_Makes_A_Group_Across_Parents()
    {
        var a1 = new RadioButton { Content = "a1", GroupName = "a", IsChecked = true };
        var a2 = new RadioButton { Content = "a2", GroupName = "a" };
        var b1 = new RadioButton { Content = "b1", GroupName = "b", IsChecked = true };
        var panel = new StackPanel { Children = { new StackPanel { Children = { a1, b1 } }, new StackPanel { Children = { a2 } } } };
        using var scene = new Scene(panel);

        a2.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([false, true, true], new[] { a1.IsChecked, a2.IsChecked, b1.IsChecked });
        Assert.False(DotOf(a1).IsVisible);
        Assert.True(DotOf(a2).IsVisible);
        Assert.True(DotOf(b1).IsVisible);
    }
}
