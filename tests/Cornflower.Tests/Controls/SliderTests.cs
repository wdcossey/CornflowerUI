using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.SliderReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Slider (<c>Controls/Slider.axaml</c>) against the phone's style and the reference app's captures
/// (<see cref="SliderReadings"/>), and the phone's interaction as WinUI's source has it. docs/controls/Slider has the
/// plan and the comparison.
/// </summary>
public class SliderTests
{
    /// <summary>
    /// A Slider on the reference app's stage: 450 × 140, the page's background, the Slider in a slot 19 in, 12 down and
    /// 19 from the right, stretched. <see cref="SliderFrames"/> saves the comparison's frames on it too.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, Slider? slider = null, double height = StageHeight,
            IEnumerable<IStyle>? themes = null)
        {
            Slider = slider ?? Specimen();
            var slot = new Grid { Margin = Slot, Children = { Slider } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: height, themes: themes);
        }

        public Slider Slider { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the Slider, in the window.</summary>
        public Point At(double x, double y) => Slider.TranslatePoint(new Point(x, y), Window)!.Value;

        /// <summary>A point on the track, <paramref name="x"/> across the Slider, in the window.</summary>
        public Point OnTrack(double x) => At(x, TrackTop + TrackHeight / 2);

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>The reference app's specimen: <c>new Slider { Header = "slider", Value = 30 }</c>.</summary>
    internal static Slider Specimen()
    {
        var slider = new Slider { Value = Value };
        ControlHeader.SetHeader(slider, Header);
        return slider;
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static ContentPresenter HeaderOf(Slider s) => Part<ContentPresenter>(s, "HeaderContentPresenter");

    private static Rectangle TrackRectOf(Slider s) => Part<Rectangle>(s, "HorizontalTrackRect");

    private static Rectangle DecreaseRectOf(Slider s) => Part<Rectangle>(s, "HorizontalDecreaseRect");

    private static Thumb ThumbOf(Slider s) => Part<Thumb>(s, "HorizontalThumb");

    private static Border ThumbBorderOf(Thumb t) => Part<Border>(t, "ThumbBorder");

    private static Rectangle BorderOf(Slider s) => Part<Rectangle>(s, "HorizontalBorder");

    private static RepeatButton DecreaseOf(Slider s) => Part<RepeatButton>(s, "PART_DecreaseButton");

    private static RepeatButton IncreaseOf(Slider s) => Part<RepeatButton>(s, "PART_IncreaseButton");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    /// <summary>
    /// The value the phone gives a press at <paramref name="x"/> across a horizontal Slider (WinUI's
    /// <c>Slider::MoveThumbToPoint</c>): the thumb's middle under the finger, so the track less the thumb is the range.
    /// </summary>
    private static double ValueAt(Slider s, double x)
    {
        var thumb = ThumbOf(s).Bounds.Width;
        var fraction = Math.Clamp((x - thumb / 2) / (s.Bounds.Width - thumb), 0, 1);
        return s.Minimum + fraction * (s.Maximum - s.Minimum);
    }

    // The style's setters, as the dump read them (slider.json's keys, themeresources.json's values), and the parts'.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var slider = stage.Slider;
        // The window's own font is Selawik too, so give it another: the Slider's is the style's, not inherited.
        stage.Window.FontFamily = new FontFamily("Courier New");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Track(dark), ColourOf(slider.Background));
        Assert.Equal(Transparent, ColourOf(slider.BorderBrush));
        Assert.Equal(new Thickness(0), slider.BorderThickness);
        Assert.Equal(Accent, ColourOf(slider.Foreground));
        Assert.Equal(stage.Window.FindResource("ContentControlThemeFontFamily"), slider.FontFamily);
        Assert.Equal(12, slider.FontSize);

        Assert.Equal(Track(dark), ColourOf(TrackRectOf(slider).Fill));
        Assert.Equal(Track(dark), ColourOf(TrackRectOf(slider).Stroke));
        Assert.Equal(Accent, ColourOf(DecreaseRectOf(slider).Fill));
        var thumb = ThumbOf(slider);
        Assert.Equal(Thumb(dark), ColourOf(thumb.Background));
        Assert.Equal(Thumb(dark), ColourOf(thumb.BorderBrush));
        Assert.Equal(new Thickness(ThumbBorderThickness), thumb.BorderThickness);
        Assert.Equal(Thumb(dark), ColourOf(ThumbBorderOf(thumb).Background));
        Assert.Equal(Transparent, ColourOf(BorderOf(slider).Stroke));

        var header = HeaderOf(slider);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), header.FontFamily);
        Assert.Equal(HeaderFontSize, header.FontSize);
        Assert.Equal(FontWeight.Normal, header.FontWeight);
        Assert.Equal(HeaderInk(dark), ColourOf(header.Foreground));
    }

    // The dump's bounds: the header, the container below it, the track and its border, the decrease rect three tenths
    // of the track less the thumb, and the thumb. Where Avalonia rounds and the phone doesn't the same (ADR 0003), ours.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones_Within_Avalonias_Rounding()
    {
        using var stage = new Stage();
        var slider = stage.Slider;
        var inStage = In(slider, (Visual)stage.Window.Content!);

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(OurWidth, slider.Bounds.Width, 0.001);
        Assert.Equal(Height, slider.Bounds.Height, 0.001);

        Assert.Equal(HeaderHeight, HeaderOf(slider).Bounds.Height, 0.001);
        var container = Part<Grid>(slider, "SliderContainer");
        Assert.Equal(ContainerTop, In(container, slider).Y, 0.001);
        Assert.Equal(ContainerHeight, container.Bounds.Height, 0.001);

        var track = TrackRectOf(slider);
        Assert.Equal(0, In(track, slider).X, 0.001);
        Assert.Equal(TrackTop, In(track, slider).Y, 0.001);
        Assert.Equal(OurWidth, track.Bounds.Width, 0.001);
        Assert.Equal(TrackHeight, track.Bounds.Height, 0.001);

        var decrease = DecreaseRectOf(slider);
        Assert.Equal(0, In(decrease, slider).X, 0.001);
        Assert.Equal(TrackTop, In(decrease, slider).Y, 0.001);
        Assert.Equal(DecreaseWidth, decrease.Bounds.Width, 0.001);
        Assert.Equal(TrackHeight, decrease.Bounds.Height, 0.001);

        var thumb = ThumbBorderOf(ThumbOf(slider));
        Assert.Equal(ThumbAt.X, In(thumb, slider).X, 0.001);
        Assert.Equal(ThumbAt.Y, In(thumb, slider).Y, 0.001);
        Assert.Equal(ThumbWidth, thumb.Bounds.Width, 0.001);
        Assert.Equal(ThumbHeight, thumb.Bounds.Height, 0.001);

        var border = BorderOf(slider);
        Assert.Equal(ContainerTop, In(border, slider).Y, 0.001);
        Assert.Equal(BorderHeight, border.Bounds.Height, 0.001);
        Assert.Equal(1, border.StrokeThickness);
    }

    // The PNGs' pixels through the track and the thumb (SliderReadings), dark and light: every one the phone's, but the
    // track's right end, one raw pixel short (ADR 0003's rounding of the slot's margins). Within one step a channel: the
    // track's 1-wide stroke, the fill's own colour, blends a pixel at its inner edge to #FF1E1E1E (#FFDDDDDD light).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Track_And_Thumb_Are_Drawn_Where_The_Phone_Draws_Them(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        for (var x = 0; x < 1080; x++)
        {
            var expected = x < DecreaseLeft ? Page(dark)
                : x <= DecreaseRight ? Accent
                : x <= ThumbRight ? Thumb(dark)
                : x <= OurTrackRight ? Track(dark)
                : Page(dark);
            Assert.True(Near(expected, Frames.PixelAt(frame, x, TrackRow)),
                $"{x},{TrackRow}: {Hex(Frames.PixelAt(frame, x, TrackRow))}, not {Hex(expected)}");
        }

        for (var y = 90; y < 200; y++)
        {
            var track = y is >= TrackTopRow and <= TrackBottomRow ? Accent : Page(dark);
            Assert.True(Near(track, Frames.PixelAt(frame, TrackColumn, y)), $"{TrackColumn},{y}: {Hex(Frames.PixelAt(frame, TrackColumn, y))}");
            var thumb = y is >= ThumbTopRow and <= ThumbBottomRow ? Thumb(dark) : Page(dark);
            Assert.True(Near(thumb, Frames.PixelAt(frame, ThumbColumn, y)), $"{ThumbColumn},{y}: {Hex(Frames.PixelAt(frame, ThumbColumn, y))}");
        }
    }

    // The phone's Disabled state: the decrease rect, the thumb and its border dimmed, the track as it was, and the
    // header untouched (the state doesn't name it). The dumps and the PNGs.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Decrease_Rect_And_The_Thumb(string variant)
    {
        var dark = variant == "Dark";
        var slider = Specimen();
        slider.IsEnabled = false;
        using var stage = new Stage(VariantOf(variant), slider);

        Assert.Equal(DisabledDecrease(dark), ColourOf(DecreaseRectOf(slider).Fill));
        Assert.Equal(DisabledThumb(dark), ColourOf(ThumbOf(slider).Background));
        Assert.Equal(DisabledThumb(dark), ColourOf(ThumbOf(slider).BorderBrush));
        Assert.Equal(Track(dark), ColourOf(TrackRectOf(slider).Fill));
        Assert.Equal(Track(dark), ColourOf(TrackRectOf(slider).Stroke));
        Assert.Equal(Transparent, ColourOf(BorderOf(slider).Stroke));
        Assert.Equal(HeaderInk(dark), ColourOf(HeaderOf(slider).Foreground));

        using var frame = stage.Capture();
        Assert.Equal(DisabledDecrease(dark), Frames.PixelAt(frame, 200, TrackRow));
        Assert.Equal(DisabledThumb(dark), Frames.PixelAt(frame, ThumbColumn, TrackRow));
        Assert.Equal(DisabledThumb(dark), Frames.PixelAt(frame, ThumbColumn, ThumbTopRow));
        Assert.Equal(Track(dark), Frames.PixelAt(frame, 600, TrackRow));
    }

    // The Disabled state sets the track's fill and stroke and the border's stroke by key, over an app's Background and
    // BorderBrush, as the phone's storyboard does; enabled, the app's show.
    [AvaloniaFact]
    public void Disabled_Sets_The_Track_And_Border_Over_An_Apps_Brushes()
    {
        var slider = Specimen();
        slider.Background = Brushes.Red;
        slider.BorderBrush = Brushes.Lime;
        using var stage = new Stage(slider: slider);
        Assert.Equal(Colors.Red, ColourOf(TrackRectOf(slider).Fill));
        Assert.Equal(Colors.Lime, ColourOf(BorderOf(slider).Stroke));

        slider.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Track(true), ColourOf(TrackRectOf(slider).Fill));
        Assert.Equal(Track(true), ColourOf(TrackRectOf(slider).Stroke));
        Assert.Equal(Transparent, ColourOf(BorderOf(slider).Stroke));
    }

    // The template's only states are Normal and Disabled: the pointer over it, and a press that moves nothing (on the
    // thumb), change no pixel. The PNGs: CommonStates.Normal is base, byte for byte.
    [AvaloniaFact]
    public void The_Pointer_Over_It_And_A_Press_Change_Nothing()
    {
        using var stage = new Stage();
        using var rest = stage.Capture();
        var onThumb = stage.At(ThumbAt.X + ThumbWidth / 2, ThumbAt.Y + ThumbHeight / 2);

        stage.Window.MouseMove(onThumb);
        Dispatcher.UIThread.RunJobs();
        using (var over = stage.Capture())
            Assert.True(SameFrame(rest, over), "the pointer over it changed a pixel");

        stage.Window.MouseDown(onThumb, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        using (var pressed = stage.Capture())
            Assert.True(SameFrame(rest, pressed), "a press on the thumb changed a pixel");
        Assert.Equal(Value, stage.Slider.Value);
        stage.Window.MouseUp(onThumb, MouseButton.Left);
    }

    // WinRT's Header: the attached ControlHeader.Header is shown above the track, a string as text in the phone's line
    // (1.33 em of 16, 21.28, which rounds to 21.6667).
    [AvaloniaFact]
    public void The_Header_Is_Shown_Above_The_Track()
    {
        using var stage = new Stage();
        var text = HeaderOf(stage.Slider).GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.Equal(Header, text.Text);
        Assert.Equal(HeaderFontSize, text.FontSize);
        Assert.Equal(21.28, text.LineHeight, 0.01);
    }

    // With no header, the presenter is empty and takes no height: the track is 17.5 from the Slider's top, and the
    // Slider is the template's three rows tall wherever it isn't stretched: 57.1667, which the phone rounds to 137 raw
    // pixels (57.0833) and Avalonia, its 30.5 row up to 74, to 138 (57.5; ADR 0003).
    [AvaloniaFact]
    public void With_No_Header_The_Track_Is_At_The_Top()
    {
        var slider = new Slider { Value = Value, VerticalAlignment = VerticalAlignment.Top };
        using var stage = new Stage(slider: slider);

        Assert.Equal(0, HeaderOf(slider).Bounds.Height);
        Assert.Equal(TrackTop - ContainerTop, In(TrackRectOf(slider), slider).Y, 0.001);
        Assert.Equal(OurUnstretchedHeight, slider.Bounds.Height, 0.001);
    }

    // WinRT's HeaderTemplate: the attached ControlHeader.HeaderTemplate shows the header.
    [AvaloniaFact]
    public void The_HeaderTemplate_Is_Used()
    {
        var slider = Specimen();
        ControlHeader.SetHeaderTemplate(slider, new FuncDataTemplate<string>((text, _) => new Border { Name = "Made", Child = new TextBlock { Text = text.ToUpperInvariant() } }));
        using var stage = new Stage(slider: slider);

        var made = Part<Border>(HeaderOf(slider), "Made");
        Assert.Equal("SLIDER", ((TextBlock)made.Child!).Text);
    }

    // A tap on the track moves the thumb's middle to the finger (WinUI's Slider::MoveThumbToPoint), on either side of
    // the thumb.
    [AvaloniaFact]
    public void A_Tap_On_The_Track_Moves_The_Thumbs_Middle_To_The_Finger()
    {
        using var stage = new Stage();
        var slider = stage.Slider;

        foreach (var x in new[] { 300.0, 60.0, 4.0, 411.0 })
        {
            stage.Window.MouseDown(stage.OnTrack(x), MouseButton.Left);
            stage.Window.MouseUp(stage.OnTrack(x), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ValueAt(slider, x), slider.Value, 0.001);
            var thumb = ThumbOf(slider);
            Assert.Equal(Math.Clamp(x, thumb.Bounds.Width / 2, slider.Bounds.Width - thumb.Bounds.Width / 2),
                In(thumb, slider).X + thumb.Bounds.Width / 2, 0.5);
        }
    }

    // The phone's SliderContainer is transparent and takes the hits across the whole area below the header, however
    // tall the Slider is (the capture's is stretched to 127.9): a tap above the track, under it, and far below its
    // rows moves the thumb. A tap on the header doesn't.
    [AvaloniaFact]
    public void A_Tap_Anywhere_Below_The_Header_Moves_The_Thumb()
    {
        using var stage = new Stage();
        var slider = stage.Slider;

        foreach (var (x, y) in new[] { (300.0, ContainerTop + 1), (100.0, ContainerTop + 50), (250.0, Height - 1) })
        {
            stage.Window.MouseDown(stage.At(x, y), MouseButton.Left);
            stage.Window.MouseUp(stage.At(x, y), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ValueAt(slider, x), slider.Value, 0.001);
        }

        var before = slider.Value;
        stage.Window.MouseDown(stage.At(350, HeaderHeight / 2), MouseButton.Left);
        stage.Window.MouseUp(stage.At(350, HeaderHeight / 2), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(before, slider.Value);
    }

    // After a press on the track, the thumb follows the finger (WinUI's Slider::OnPointerMoved), past either end held
    // at the end, and stops when it lifts; a touch does the same.
    [AvaloniaFact]
    public void A_Drag_From_The_Track_Follows_The_Finger()
    {
        using var stage = new Stage();
        var slider = stage.Slider;

        stage.Window.MouseDown(stage.OnTrack(200), MouseButton.Left);
        foreach (var x in new[] { 250.0, 320.0, 500.0, 100.0, -50.0, 150.0 })
        {
            stage.Window.MouseMove(stage.At(x, Height + 30));            // off the Slider, below it: the drag goes on
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ValueAt(slider, x), slider.Value, 0.001);
        }

        stage.Window.MouseUp(stage.OnTrack(150), MouseButton.Left);
        stage.Window.MouseMove(stage.OnTrack(300));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ValueAt(slider, 150), slider.Value, 0.001);

        var touch = stage.Window.TouchBegin(stage.OnTrack(80));
        stage.Window.TouchMove(touch, stage.OnTrack(260));
        stage.Window.TouchEnd(touch, stage.OnTrack(260));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ValueAt(slider, 260), slider.Value, 0.001);
    }

    // A drag that starts on the thumb follows the finger too; as Avalonia's Slider does it, the thumb's middle goes to
    // the finger on the first move (a recorded difference: WinRT's moves the thumb by the finger's travel).
    [AvaloniaFact]
    public void A_Drag_From_The_Thumb_Follows_The_Finger()
    {
        using var stage = new Stage();
        var slider = stage.Slider;
        var middle = ThumbAt.X + ThumbWidth / 2;

        stage.Window.MouseDown(stage.At(middle, ThumbAt.Y + 2), MouseButton.Left);
        stage.Window.MouseMove(stage.At(middle + 100, ThumbAt.Y + 2));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ValueAt(slider, middle + 100), slider.Value, 0.001);
        stage.Window.MouseUp(stage.At(middle + 100, ThumbAt.Y + 2), MouseButton.Left);
    }

    // The Slider's own Foreground is the decrease rect's fill, and its Background the track's, as the phone's template
    // binds them; the default Foreground is the accent, so an app's accent reaches it.
    [AvaloniaFact]
    public void The_Sliders_Brushes_Reach_The_Track()
    {
        var slider = Specimen();
        slider.Foreground = Brushes.Orange;
        slider.Background = Brushes.Navy;
        using var stage = new Stage(slider: slider);

        Assert.Equal(Colors.Orange, ColourOf(DecreaseRectOf(slider).Fill));
        Assert.Equal(Colors.Navy, ColourOf(TrackRectOf(slider).Fill));
        using var frame = stage.Capture();
        Assert.Equal(Colors.Orange, Frames.PixelAt(frame, 200, TrackRow));
        Assert.Equal(Colors.Navy, Frames.PixelAt(frame, 600, TrackRow));
    }

    // The phone's HorizontalBorder binds StrokeThickness to BorderThickness, a Thickness where a double is wanted, so
    // it draws 1 wide whatever BorderThickness is (the dump: strokeThickness 1 under SliderBorderThemeThickness 0).
    [AvaloniaFact]
    public void The_Border_Is_One_Wide_Whatever_BorderThickness_Says()
    {
        var slider = Specimen();
        slider.BorderBrush = Brushes.Lime;
        slider.BorderThickness = new Thickness(6);
        using var stage = new Stage(slider: slider);
        using var frame = stage.Capture();

        Assert.Equal(1, BorderOf(slider).StrokeThickness);
        // The border's top edge, 21.6667 down (52 raw pixels): 1 wide is 2.4 raw pixels, so 52 and 53 are wholly lime,
        // and 55 is the page.
        Assert.Equal(Colors.Lime, Frames.PixelAt(frame, 600, 29 + 52));
        Assert.Equal(Colors.Lime, Frames.PixelAt(frame, 600, 29 + 53));
        Assert.Equal(Page(true), Frames.PixelAt(frame, 600, 29 + 55));
    }

    // Padding is the template root's margin, as the phone's TemplateBinding Padding.
    [AvaloniaFact]
    public void Padding_Insets_The_Template()
    {
        var slider = Specimen();
        slider.Padding = new Thickness(12, 6, 0, 0);
        using var stage = new Stage(slider: slider);

        // To the raw pixel grid: 28.8 and 14.4 raw pixels are 29 and 14.
        Assert.Equal(12, In(HeaderOf(slider), slider).X, 0.25);
        Assert.Equal(6, In(HeaderOf(slider), slider).Y, 0.25);
        Assert.Equal(12, In(TrackRectOf(slider), slider).X, 0.25);
        Assert.Equal(TrackTop + 6, In(TrackRectOf(slider), slider).Y, 0.25);
    }

    // IsDirectionReversed: the value's part of the track is drawn from the right, and a tap counts from the right.
    [AvaloniaFact]
    public void Reversed_The_Value_Fills_From_The_Right()
    {
        var slider = Specimen();
        slider.IsDirectionReversed = true;
        using var stage = new Stage(slider: slider);
        var decrease = DecreaseRectOf(slider);

        Assert.Equal(DecreaseWidth, decrease.Bounds.Width, 0.001);
        Assert.Equal(OurWidth - DecreaseWidth, In(decrease, slider).X, 0.001);

        stage.Window.MouseDown(stage.OnTrack(100), MouseButton.Left);
        stage.Window.MouseUp(stage.OnTrack(100), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(100 - ValueAt(slider, 100), slider.Value, 0.001);
    }

    // The vertical template, from slider.json (no capture: the reference app has no vertical Slider): the columns 13.5,
    // the track's 9 and 13.5; the track the full height; the 9 × 9 thumb in the track's column; the value's part
    // below the thumb, from the bottom. A tap moves the thumb's middle to the finger, counting from the bottom.
    [AvaloniaFact]
    public void Vertical_It_Is_The_Phones_Vertical_Template()
    {
        var slider = new Slider { Orientation = Orientation.Vertical, Value = Value, Height = 300, HorizontalAlignment = HorizontalAlignment.Left };
        using var stage = new Stage(slider: slider, height: 400);

        Assert.Equal(VerticalSide * 2 + VerticalTrack, slider.Bounds.Width, 0.5);
        var track = Part<Rectangle>(slider, "VerticalTrackRect");
        Assert.Equal(VerticalSide, In(track, slider).X, 0.5);
        Assert.Equal(VerticalTrack, track.Bounds.Width, 0.5);
        Assert.Equal(300, track.Bounds.Height, 0.5);

        var thumb = Part<Thumb>(slider, "VerticalThumb");
        var thumbBorder = ThumbBorderOf(thumb);
        Assert.Equal(VerticalSide, In(thumbBorder, slider).X, 0.5);
        Assert.Equal(VerticalTrack, thumbBorder.Bounds.Width, 0.5);
        Assert.Equal(VerticalTrack, thumbBorder.Bounds.Height, 0.5);
        var thumbTop = (300 - thumb.Bounds.Height) * (1 - Value / 100);
        Assert.Equal(thumbTop, In(thumb, slider).Y, 0.5);

        var decrease = Part<Rectangle>(slider, "VerticalDecreaseRect");
        Assert.Equal(VerticalSide, In(decrease, slider).X, 0.5);
        Assert.Equal(VerticalTrack, decrease.Bounds.Width, 0.5);
        Assert.Equal(thumbTop + thumb.Bounds.Height, In(decrease, slider).Y, 0.5);
        Assert.Equal(300, In(decrease, slider).Y + decrease.Bounds.Height, 0.5);
        Assert.Equal(Accent, ColourOf(decrease.Fill));

        var y = 60.0;
        stage.Window.MouseDown(stage.At(30, y), MouseButton.Left);        // in the right-hand column: the container takes it
        stage.Window.MouseUp(stage.At(30, y), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        var expected = 100 * Math.Clamp((300 - y - thumb.Bounds.Height / 2) / (300 - thumb.Bounds.Height), 0, 1);
        Assert.Equal(expected, slider.Value, 0.001);
    }

    // Vertical and disabled, the phone's Disabled state: the decrease rect and the thumb dimmed.
    [AvaloniaFact]
    public void Vertical_Disabled_Dims_The_Decrease_Rect_And_The_Thumb()
    {
        var slider = new Slider { Orientation = Orientation.Vertical, Value = Value, Height = 300, IsEnabled = false };
        using var stage = new Stage(slider: slider, height: 400);

        Assert.Equal(DisabledDecrease(true), ColourOf(Part<Rectangle>(slider, "VerticalDecreaseRect").Fill));
        Assert.Equal(DisabledThumb(true), ColourOf(Part<Thumb>(slider, "VerticalThumb").Background));
    }

    // The outside tick bars show by TickPlacement, as WinRT's TopLeft, BottomRight and Outside show them: 5 tall, in
    // SliderTickmarkOutsideBackgroundThemeBrush, the horizontal ones 4.5 past each end of the track, in the rows above
    // and below it; the vertical ones 1.5 off the track. None, the default, shows none.
    [AvaloniaTheory]
    [InlineData(TickPlacement.None, false, false)]
    [InlineData(TickPlacement.TopLeft, true, false)]
    [InlineData(TickPlacement.BottomRight, false, true)]
    [InlineData(TickPlacement.Outside, true, true)]
    public void TickPlacement_Shows_The_Phones_Tick_Bars(TickPlacement placement, bool first, bool second)
    {
        var slider = Specimen();
        slider.TickPlacement = placement;
        slider.TickFrequency = 10;
        var vertical = new Slider { Orientation = Orientation.Vertical, Height = 200, TickPlacement = placement, TickFrequency = 10 };
        using var stage = new Stage(slider: slider);
        using var verticalStage = new Stage(slider: vertical, height: 300);

        var top = Part<TickBar>(slider, "TopTickBar");
        var bottom = Part<TickBar>(slider, "BottomTickBar");
        Assert.Equal(first, top.IsVisible);
        Assert.Equal(second, bottom.IsVisible);
        Assert.Equal(first, Part<TickBar>(vertical, "LeftTickBar").IsVisible);
        Assert.Equal(second, Part<TickBar>(vertical, "RightTickBar").IsVisible);
        if (!first) return;

        Assert.Equal(Tick(true), ColourOf(top.Fill));
        Assert.Equal(TickBarHeight, top.Bounds.Height, 0.5);
        Assert.Equal(-4.5, In(top, slider).X, 0.5);
        Assert.Equal(OurWidth + 9, top.Bounds.Width, 0.5);
        Assert.Equal(ContainerTop + (17.5 - TickBarHeight) / 2, In(top, slider).Y, 0.5);
        Assert.Equal(10, top.TickFrequency);
        Assert.Equal(100, top.Maximum);
        Assert.Equal(ThumbOf(slider).Bounds, top.ReservedSpace);

        var left = Part<TickBar>(vertical, "LeftTickBar");
        Assert.Equal(VerticalSide - 1.5 - TickBarHeight, In(left, vertical).X, 0.5);
        Assert.Equal(TickBarHeight, left.Bounds.Width, 0.5);
    }

    // The ticks are drawn: TickFrequency 25 draws five, for 0, 25, 50, 75 and 100, in the tick brush. Where: the
    // template's tick bar runs 4.5 past each end of the track, and WinUI's TickBar (TickBar_Partial.cpp) puts the first
    // tick half a thumb in from its start and the last half a thumb from its end, so they fall at the track's ends,
    // 0.25 in at 2.4 here, and the rest evenly between. Avalonia's TickBar does the same with the thumb as its
    // ReservedSpace. Disabled, the brush is SliderTickMarkOutsideDisabledForegroundThemeBrush, the same colour.
    [AvaloniaFact]
    public void The_Ticks_Are_Drawn_From_End_To_End_Of_The_Track()
    {
        var slider = Specimen();
        slider.TickPlacement = TickPlacement.TopLeft;
        slider.TickFrequency = 25;
        using var stage = new Stage(slider: slider);
        using var frame = stage.Capture();
        var bar = Part<TickBar>(slider, "TopTickBar");
        var y = (int)Math.Round(stage.At(0, In(bar, slider).Y + TickBarHeight - 0.5).Y * 2.4);

        var inked = Enumerable.Range(0, 1080).Where(x => Frames.PixelAt(frame, x, y) != Page(true)).ToList();
        Assert.Equal(5, inked.Where((x, i) => i == 0 || inked[i - 1] != x - 1).Count());
        var first = -4.5 + ThumbWidth / 2;
        var span = OurWidth + 9 - ThumbWidth;
        foreach (var step in new[] { 0, 1, 2, 3, 4 })
        {
            var raw = stage.At(first + step * span / 4, 0).X * 2.4;
            Assert.Contains(inked, x => Math.Abs(x + 0.5 - raw) <= 1.5);
        }
        // A tick is 1 wide, 2.4 raw pixels: its whole pixels are the brush, half white, on the black page.
        Assert.Contains(inked, x => Near(Color.FromRgb(0x80, 0x80, 0x80), Frames.PixelAt(frame, x, y)));

        slider.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Tick(true), ColourOf(bar.Fill));
    }

    // The track's RepeatButtons and the Thumb take the Slider's own themes, never Wp81's RepeatButton theme (keyed by
    // type, #30) nor an app's implicit RepeatButton or Thumb theme: the parts keep the phone's track, not 57.5-tall
    // bordered buttons with the accent pressed.
    [AvaloniaFact]
    public void The_Tracks_Parts_Keep_The_Sliders_Themes()
    {
        var implicitThemes = new ResourceDictionary
        {
            [typeof(RepeatButton)] = new ControlTheme(typeof(RepeatButton)) { Setters = { new Setter(TemplatedControl.BackgroundProperty, Brushes.Red), new Setter(Layoutable.MinHeightProperty, 200.0) } },
            [typeof(Thumb)] = new ControlTheme(typeof(Thumb)) { Setters = { new Setter(TemplatedControl.BackgroundProperty, Brushes.Red) } },
        };
        var app = new Styles { Resources = implicitThemes };
        foreach (var themes in new IStyle[][] { [new SimpleTheme(), new Wp81Theme()], [new SimpleTheme(), new Wp81Theme(), app] })
        {
            using var stage = new Stage(themes: themes);
            var slider = stage.Slider;
            var wp81RepeatButton = stage.Window.FindResource(typeof(RepeatButton));

            Assert.Same(stage.Window.FindResource("Wp81SliderHorizontalDecreaseTheme"), DecreaseOf(slider).Theme);
            Assert.Same(stage.Window.FindResource("Wp81SliderIncreaseTheme"), IncreaseOf(slider).Theme);
            Assert.Same(stage.Window.FindResource("Wp81SliderHorizontalThumbTheme"), ThumbOf(slider).Theme);
            Assert.NotSame(wp81RepeatButton, DecreaseOf(slider).Theme);
            Assert.Equal(0, DecreaseOf(slider).MinHeight);
            Assert.Equal(DecreaseWidth, DecreaseRectOf(slider).Bounds.Width, 0.001);
            Assert.Equal(Thumb(true), ColourOf(ThumbOf(slider).Background));
            using var frame = stage.Capture();
            Assert.Equal(Accent, Frames.PixelAt(frame, 200, TrackRow));
            Assert.Equal(Track(true), Frames.PixelAt(frame, 600, TrackRow));
        }

        // Wp81's own RepeatButton theme is there and keyed by type, so this holds something.
        using var plain = new Stage();
        Assert.IsType<ControlTheme>(plain.Window.FindResource(typeof(RepeatButton)));
        Assert.Equal(typeof(RepeatButton), ((ControlTheme)plain.Window.FindResource(typeof(RepeatButton))!).TargetType);
    }

    // The phone's template has no focus states: keyboard focus draws nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new Stage();
        using var rest = stage.Capture();

        stage.Slider.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Slider.IsFocused);
        using var focused = stage.Capture();
        Assert.True(SameFrame(rest, focused), "keyboard focus changed a pixel");
    }

    // The colours follow a change of variant at run time, through the keys.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark);
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Track(false), ColourOf(TrackRectOf(stage.Slider).Fill));
        Assert.Equal(Thumb(false), ColourOf(ThumbOf(stage.Slider).Background));
        Assert.Equal(HeaderInk(false), ColourOf(HeaderOf(stage.Slider).Foreground));
    }

    /// <summary>Whether two colours are within one step a channel.</summary>
    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 1 && Math.Abs(a.R - b.R) <= 1 && Math.Abs(a.G - b.G) <= 1 && Math.Abs(a.B - b.B) <= 1;

    /// <summary>Whether two frames of the stage are the same, pixel for pixel.</summary>
    private static bool SameFrame(WriteableBitmap a, WriteableBitmap b)
    {
        using var x = a.Lock();
        using var y = b.Lock();
        if (x.Size != y.Size || x.Format != y.Format) return false;
        var row = new byte[x.Size.Width * 4];
        var other = new byte[row.Length];
        for (var r = 0; r < x.Size.Height; r++)
        {
            System.Runtime.InteropServices.Marshal.Copy(x.Address + r * x.RowBytes, row, 0, row.Length);
            System.Runtime.InteropServices.Marshal.Copy(y.Address + r * y.RowBytes, other, 0, other.Length);
            if (!row.AsSpan().SequenceEqual(other)) return false;
        }

        return true;
    }
}
