using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ToggleSwitchReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ToggleSwitch (<c>Controls/ToggleSwitch.axaml</c>, with <c>PhoneToggleSwitch</c>) against the phone's style,
/// the reference app's captures and the touch probes' measurements (<see cref="ToggleSwitchReadings"/>).
/// docs/controls/ToggleSwitch has the plan and the comparison.
/// </summary>
public class ToggleSwitchTests
{
    /// <summary>
    /// A ToggleSwitch on the reference app's stage: 450 wide, 140 tall (110 without a header), the page's background,
    /// the switch in a slot 19 in, 12 down and 19 from the right. <see cref="ToggleSwitchFrames"/> saves the
    /// comparison's frames on it too.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, ToggleSwitch? toggle = null, double? height = null)
        {
            Toggle = toggle ?? Specimen();
            Sliding.Add(Toggle);
            var slot = new Grid { Margin = Slot, Children = { Toggle } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth,
                height: height ?? (ControlHeader.GetHeader(Toggle) is null ? NoHeaderStageHeight : StageHeight));
        }

        public ToggleSwitch Toggle { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the switch, in the window.</summary>
        public Point At(double x, double y) => Toggle.TranslatePoint(new Point(x, y), Window)!.Value;

        /// <summary>The knob's middle, in the window.</summary>
        public Point Knob() => KnobOf(Toggle).TranslatePoint(new Point(OurKnobWidth / 2, OurSwitchHeight / 2), Window)!.Value;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose()
        {
            _scene.Dispose();
            Sliding.Remove(Toggle);
        }
    }

    /// <summary>The reference app's specimens: <c>new ToggleSwitch { Header = "toggle switch", IsOn = on }</c>, or with no header.</summary>
    internal static ToggleSwitch Specimen(bool on = false, bool header = true)
    {
        var toggle = new ToggleSwitch { IsChecked = on };
        if (header)
            ControlHeader.SetHeader(toggle, Header);
        return toggle;
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Panel KnobsOf(ToggleSwitch t) => Part<Panel>(t, "PART_MovingKnobs");

    private static Rectangle KnobOf(ToggleSwitch t) => Part<Rectangle>(t, "SwitchKnob");

    private static Rectangle CurtainOf(ToggleSwitch t) => Part<Rectangle>(t, "SwitchCurtain");

    private static Border OuterBorderOf(ToggleSwitch t) => Part<Border>(t, "OuterBorder");

    private static ContentPresenter HeaderOf(ToggleSwitch t) => Part<ContentPresenter>(t, "HeaderContentPresenter");

    private static ContentPresenter ContentOf(ToggleSwitch t) => Part<ContentPresenter>(t, "PART_ContentPresenter");

    private static ContentPresenter OffOf(ToggleSwitch t) => Part<ContentPresenter>(t, "PART_OffContentPresenter");

    private static ContentPresenter OnOf(ToggleSwitch t) => Part<ContentPresenter>(t, "PART_OnContentPresenter");

    /// <summary>Where the knob is: <c>PART_MovingKnobs</c>' <c>Canvas.Left</c>, its animated value if it is sliding.</summary>
    private static double KnobAt(ToggleSwitch t) => Canvas.GetLeft(KnobsOf(t));

    /// <summary>
    /// Where the knob comes to rest: <c>Canvas.Left</c>'s base value, which a slide runs to. A theme transition is tested
    /// at its end value (ADR 0003): Avalonia's run on real time.
    /// </summary>
    private static double RestAt(ToggleSwitch t) => KnobsOf(t).GetBaseValue(Canvas.LeftProperty).GetValueOrDefault(double.NaN);

    /// <summary>Every value <c>Canvas.Left</c> takes while <paramref name="act"/> runs and for a slide's length after, with its priority.</summary>
    private static List<(double Value, BindingPriority Priority)> Watch(ToggleSwitch t, Action act)
    {
        var seen = new List<(double, BindingPriority)>();
        void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Canvas.LeftProperty)
                seen.Add(((double)e.NewValue!, e.Priority));
        }

        KnobsOf(t).PropertyChanged += OnChanged;
        act();
        Settle();
        KnobsOf(t).PropertyChanged -= OnChanged;
        return seen;
    }

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    /// <summary>The values after the last time the knob was at <paramref name="at"/>, where the finger left it.</summary>
    private static List<double> After(List<(double Value, BindingPriority Priority)> seen, double at)
    {
        var release = seen.FindLastIndex(v => Math.Abs(v.Value - at) < 1e-6);
        Assert.True(release >= 0, $"the knob never followed the finger to {at}");
        return seen.Skip(release + 1).Select(v => v.Value).ToList();
    }

    /// <summary>
    /// Runs Avalonia's frames until every knob on the window has come to rest: its transitions move on the render ticks,
    /// by real time, so this ticks until each knob is where its slide runs to, or two seconds pass.
    /// </summary>
    private static void Settle()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        do
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        while (watch.ElapsedMilliseconds < 2000 && Sliding.Any(t => t.IsAttachedToVisualTree() && Math.Abs(KnobAt(t) - RestAt(t)) > 1e-9));

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The switches <see cref="Settle"/> waits on: every one a <see cref="Stage"/> or <see cref="Watch"/> has seen.</summary>
    private static readonly List<ToggleSwitch> Sliding = [];

    // A press on the knob, moved across by each of dx, and released at the last, by the mouse; on the clock if given.
    private static void Drag(Stage stage, params double[] dx)
    {
        var start = stage.Knob();
        stage.Window.MouseDown(start, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        foreach (var x in dx)
        {
            stage.Window.MouseMove(start + new Point(x, 0));
            Dispatcher.UIThread.RunJobs();
        }

        stage.Window.MouseUp(start + new Point(dx.Length > 0 ? dx[^1] : 0, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The style and the template.

    // The style's setters, as the dump read them (toggleswitch.json's keys, themeresources.json's values), and the parts'.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), Specimen(on: true));
        var t = stage.Toggle;
        // The window's own font is Selawik too, so give it another: the switch's is the style's, not inherited.
        stage.Window.FontFamily = new FontFamily("Courier New");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Foreground(dark), ColourOf(t.Foreground));
        Assert.Equal(HorizontalAlignment.Stretch, t.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, t.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Left, t.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, t.VerticalContentAlignment);
        Assert.Equal(stage.Window.FindResource("ContentControlThemeFontFamily"), t.FontFamily);
        Assert.Equal(FontWeight.SemiBold, t.FontWeight);
        Assert.Equal(12, t.FontSize);
        Assert.Equal(new Thickness(0), t.Margin);
        Assert.Equal(Padding, t.Padding);
        Assert.Null(t.FocusAdorner);

        Assert.Equal(Foreground(dark), ColourOf(OuterBorderOf(t).BorderBrush));
        Assert.Equal(new Thickness(BorderThickness), OuterBorderOf(t).BorderThickness);
        var inner = Part<Border>(t, "InnerBorder");
        Assert.Equal(Transparent, ColourOf(inner.BorderBrush));
        Assert.Equal(new Thickness(BorderThickness), inner.BorderThickness);
        Assert.Equal(Accent, ColourOf(CurtainOf(t).Fill));
        Assert.Equal(Foreground(dark), ColourOf(KnobOf(t).Fill));
        Assert.Equal(Background(dark), ColourOf(KnobOf(t).Stroke));
        Assert.Equal(KnobStrokeThickness, KnobOf(t).StrokeThickness);

        Assert.Equal(HeaderInk(dark), ColourOf(HeaderOf(t).Foreground));
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), HeaderOf(t).FontFamily);
        Assert.Equal(HeaderFontSize, HeaderOf(t).FontSize);
        Assert.Equal(FontWeight.Normal, HeaderOf(t).FontWeight);
        foreach (var content in new[] { OffOf(t), OnOf(t) })
        {
            Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), content.FontFamily);
            Assert.Equal(ContentFontSize, content.FontSize);
            Assert.Equal(FontWeight.Normal, content.FontWeight);
            Assert.Equal(Foreground(dark), ColourOf(content.Foreground));
        }
    }

    // The bounds, as the dump has them, but for Avalonia's rounding: the control, its rows, the switch's column, the
    // borders and the curtain's clip.
    [AvaloniaFact]
    public void The_Parts_Are_Where_The_Phone_Has_Them()
    {
        using var stage = new Stage();
        var t = stage.Toggle;

        Assert.Equal(InStage.X, In(t, stage.Window).X, 0.001);
        Assert.Equal(InStage.Y, In(t, stage.Window).Y, 0.001);
        Assert.Equal(OurWidth, t.Bounds.Width, 0.001);
        Assert.Equal(Height, t.Bounds.Height, 0.001);

        Assert.Equal(HeaderHeight, HeaderOf(t).Bounds.Height, 0.001);
        Assert.Equal(OurWidth - SwitchWidth - 13.5 - Padding.Right, HeaderOf(t).Bounds.Width, 0.5);
        Assert.Equal(HeaderHeight, In(OffOf(t), t).Y, 0.001);
        Assert.Equal(ContentHeight, OffOf(t).Bounds.Height, 0.001);

        var bounds = Part<Grid>(t, "SwitchKnobBounds");
        Assert.Equal(SwitchWidth, bounds.Bounds.Width, 0.001);
        Assert.Equal(OurWidth - SwitchWidth, In(bounds, t).X, 0.001);
        Assert.Equal(OurSwitchHeight, bounds.Bounds.Height, 0.001);
        Assert.Equal(OurSwitchTop, In(bounds, t).Y, 0.001);
        Assert.Equal(HeaderHeight + ContentHeight, In(bounds, t).Y + bounds.Bounds.Height, 0.001);   // bottom-aligned over the two rows

        Assert.Equal(OuterBorderAt.X, In(OuterBorderOf(t), bounds).X, 0.001);
        Assert.Equal(OuterBorderAt.Y, In(OuterBorderOf(t), bounds).Y, 0.001);
        Assert.Equal(OuterBorderWidth, OuterBorderOf(t).Bounds.Width, 0.001);
        var curtainBounds = Part<Panel>(t, "SwitchCurtainBounds");
        Assert.Equal(CurtainBoundsWidth, curtainBounds.Bounds.Width, 0.001);
        Assert.True(curtainBounds.ClipToBounds);
        Assert.Equal(OurKnobWidth, KnobOf(t).Bounds.Width, 0.001);
        Assert.Equal(OurSwitchHeight, KnobOf(t).Bounds.Height, 0.001);
    }

    // Without a header the first row is still half the switch's height, as the phone's no-header capture has it
    // (17.9167 of its 35.8333; ours half our 36.25).
    [AvaloniaFact]
    public void Without_A_Header_The_First_Row_Is_Half_The_Switch()
    {
        using var stage = new Stage(toggle: Specimen(header: false));
        var t = stage.Toggle;

        Assert.Equal(OurEmptyHeaderRow, In(OffOf(t), t).Y, 0.001);
        Assert.Equal(OurEmptyHeaderRow + ContentHeight + BottomRow, t.Bounds.Height, 0.001);
        Assert.Equal(PhoneEmptyHeaderRow, PhoneSwitchHeight / 2, 0.001);
        Assert.Empty(HeaderOf(t).GetVisualChildren());
    }

    // The knob's travel is the phone's KnobOnToOffOffset: off at 0, on at 56.6667, 136 raw px.
    [AvaloniaFact]
    public void The_Knob_Travels_The_Phones_Offset()
    {
        using var stage = new Stage(toggle: Specimen(on: false));
        var t = stage.Toggle;
        Assert.Equal(0, KnobAt(t));
        Assert.Equal(KnobOnToOffOffset, Part<Canvas>(t, "PART_SwitchKnob").Bounds.Width, 0.0001);

        using var on = new Stage(toggle: Specimen(on: true));
        Assert.Equal(KnobOnToOffOffset, KnobAt(on.Toggle), 0.0001);   // shown on: no slide in
        Assert.Equal(KnobOnToOffOffset, In(KnobOf(on.Toggle), Part<Grid>(on.Toggle, "SwitchKnobBounds")).X, 0.0001);
    }

    // The pixels of row 180, through the switch, dark and light, off and on, against the PNGs, a raw pixel further left.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Switch_Is_Drawn_As_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using (var stage = new Stage(VariantOf(variant), Specimen(on: false)))
        using (var frame = stage.Capture())
        {
            Run(frame, OurOffKnobLeft, OurOffKnobRight, Foreground(dark));
            Assert.Equal(Page(dark), Frames.PixelAt(frame, OurOffKnobLeft - 1, Row));
            Assert.Equal(Page(dark), Frames.PixelAt(frame, OurOffKnobRight + 1, Row));
            Run(frame, OurOffKnobRight + 1, OurBorderRightLeft - 1, Page(dark));     // the knob's stroke, then no curtain
            Run(frame, OurBorderRightLeft, OurBorderRightRight, Foreground(dark));
            Assert.Equal(Page(dark), Frames.PixelAt(frame, OurBorderRightRight + 1, Row));
            Assert.Equal(PhoneOffKnobRight - PhoneOffKnobLeft + 1, OurOffKnobRight - OurOffKnobLeft);   // one wider: 50 raw px, not 49
        }

        using (var stage = new Stage(VariantOf(variant), Specimen(on: true)))
        using (var frame = stage.Capture())
        {
            Run(frame, OurBorderLeftLeft, OurBorderLeftRight, Foreground(dark));
            Run(frame, OurBorderLeftRight + 1, OurCurtainLeft - 1, Page(dark));
            Run(frame, OurCurtainLeft, OurCurtainRight, Accent);
            Run(frame, OurCurtainRight + 1, OurOnKnobLeft - 1, Page(dark));             // the knob's stroke
            Run(frame, OurOnKnobLeft, OurOnKnobRight, Foreground(dark));
            Assert.Equal(Page(dark), Frames.PixelAt(frame, OurOnKnobRight + 1, Row));
        }
    }

    private static void Run(WriteableBitmap frame, int from, int to, Color expected)
    {
        for (var x = from; x <= to; x++)
            Assert.True(expected == Frames.PixelAt(frame, x, Row), $"({x}, {Row}) is {Frames.PixelAt(frame, x, Row)}, not {expected}");
    }

    // Disabled: the header, the content, the knob, the outer border and the curtain take the keys the phone's state sets.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Takes_The_Phones_Keys(string variant)
    {
        var dark = variant == "Dark";
        var toggle = Specimen(on: true);
        toggle.IsEnabled = false;
        using var stage = new Stage(VariantOf(variant), toggle);
        var t = stage.Toggle;

        Assert.Equal(DisabledInk(dark), ColourOf(HeaderOf(t).Foreground));
        Assert.Equal(DisabledInk(dark), ColourOf(OffOf(t).Foreground));
        Assert.Equal(DisabledInk(dark), ColourOf(OnOf(t).Foreground));
        Assert.Equal(DisabledInk(dark), ColourOf(OuterBorderOf(t).BorderBrush));
        Assert.Equal(DisabledFill(dark), ColourOf(KnobOf(t).Fill));
        Assert.Equal(Background(dark), ColourOf(KnobOf(t).Stroke));
        Assert.Equal(DisabledFill(dark), ColourOf(CurtainOf(t).Fill));

        using var frame = stage.Capture();
        Run(frame, OurCurtainLeft, OurCurtainRight, DisabledFill(dark));
        Run(frame, OurOnKnobLeft, OurOnKnobRight, DisabledFill(dark));
    }

    // The thumb's border key is its own in the Disabled state: shown by overriding it, as both variants give it the
    // page's colour.
    [AvaloniaFact]
    public void Disabled_Sets_The_Knobs_Border_By_Its_Own_Key()
    {
        var toggle = Specimen();
        toggle.IsEnabled = false;
        toggle.Resources["ToggleSwitchThumbDisabledBorderThemeBrush"] = new SolidColorBrush(Colors.Red);
        using var stage = new Stage(toggle: toggle);

        Assert.Equal(Colors.Red, ColourOf(KnobOf(stage.Toggle).Stroke));
    }

    // ContentStates: OffContent shows the off presenter, OnContent the on one; the other is hidden and isn't hit.
    [AvaloniaFact]
    public void The_Content_Shown_Is_The_States()
    {
        using var stage = new Stage();
        var t = stage.Toggle;

        Assert.Equal(1, OffOf(t).Opacity);
        Assert.True(OffOf(t).IsHitTestVisible);
        Assert.Equal(0, OnOf(t).Opacity);
        Assert.False(OnOf(t).IsHitTestVisible);
        Assert.Equal("Off", OffOf(t).GetVisualDescendants().OfType<TextBlock>().Single().Text);

        t.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, OffOf(t).Opacity);
        Assert.False(OffOf(t).IsHitTestVisible);
        Assert.Equal(1, OnOf(t).Opacity);
        Assert.True(OnOf(t).IsHitTestVisible);
        Assert.Equal("On", OnOf(t).GetVisualDescendants().OfType<TextBlock>().Single().Text);
    }

    // OnContent and OffContent are shown with their templates, aligned by the content alignments, in the phone's line
    // (1.33 em of 25.5, 33.915).
    [AvaloniaFact]
    public void The_Content_Is_The_Switchs_In_The_Phones_Line()
    {
        var toggle = Specimen();
        toggle.OffContent = 7;
        toggle.OffContentTemplate = new FuncDataTemplate<int>((value, _) => new TextBlock { Text = $"#{value}" });
        toggle.OnContent = 42;
        toggle.OnContentTemplate = new FuncDataTemplate<int>((value, _) => new TextBlock { Text = $"#{value}" });
        toggle.HorizontalContentAlignment = HorizontalAlignment.Right;
        using var stage = new Stage(toggle: toggle);
        var t = stage.Toggle;

        Assert.Equal("#7", OffOf(t).GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.Equal("#42", OnOf(t).GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.Equal(HorizontalAlignment.Right, OffOf(t).HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, OffOf(t).VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Right, OnOf(t).HorizontalAlignment);
        // Right-aligned, the words end where the header does: the Padding's 25.5 short of the 13.5 column.
        Assert.Equal(HeaderOf(t).Bounds.Width, In(OffOf(t), t).X + OffOf(t).Bounds.Width, 0.001);

        t.OnContentTemplate = null;
        t.OnContent = "yes";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(33.915, OnOf(t).GetVisualDescendants().OfType<TextBlock>().Single().LineHeight, 0.01);
    }

    // WinRT's Header: the attached ControlHeader.Header, in the phone's line, with its template; no access keys.
    [AvaloniaFact]
    public void The_Header_Is_ControlHeaders()
    {
        var toggle = Specimen();
        ControlHeader.SetHeader(toggle, "wi_fi");
        using var stage = new Stage(toggle: toggle);
        var t = stage.Toggle;

        var text = HeaderOf(t).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("wi_fi", text.Text);
        Assert.IsNotType<AccessText>(text);
        Assert.Equal(21.28, text.LineHeight, 0.01);

        ControlHeader.SetHeaderTemplate(t, new FuncDataTemplate<object>((value, _) => new Border { Height = 40, Name = "Made" }));
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(HeaderOf(t).GetVisualDescendants(), v => v is Border { Name: "Made" });
        Assert.Equal(40, HeaderOf(t).Bounds.Height, 0.001);
    }

    // Avalonia's Content, which Avalonia's own themes show above the switch, is shown in the header's place and style
    // when there is no Header; with a Header it isn't.
    [AvaloniaFact]
    public void Avalonias_Content_Stands_In_For_A_Missing_Header()
    {
        var toggle = Specimen(header: false);
        toggle.Content = "wi-fi";
        using var stage = new Stage(toggle: toggle, height: StageHeight);
        var t = stage.Toggle;

        var text = ContentOf(t).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("wi-fi", text.Text);
        Assert.True(ContentOf(t).IsVisible);
        Assert.Equal(HeaderInk(dark: true), ColourOf(ContentOf(t).Foreground));
        Assert.Equal(HeaderFontSize, ContentOf(t).FontSize);
        Assert.Equal(21.28, text.LineHeight, 0.01);
        Assert.Equal(Height, t.Bounds.Height, 0.001);

        t.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(DisabledInk(dark: true), ColourOf(ContentOf(t).Foreground));

        ControlHeader.SetHeader(t, Header);
        Dispatcher.UIThread.RunJobs();
        Assert.False(ContentOf(t).IsVisible);
        Assert.Equal(Header, HeaderOf(t).GetVisualDescendants().OfType<TextBlock>().Single().Text);
    }

    // The theme's KnobTransitions are the phone's RepositionThemeAnimation as measured (#66): Canvas.Left, 370 ms,
    // KeySpline 0.1,0.9 0.2,1.
    [AvaloniaFact]
    public void The_Knob_Slides_On_The_Phones_Curve()
    {
        using var stage = new Stage();
        var transition = Assert.IsType<DoubleTransition>(Assert.Single(stage.Toggle.KnobTransitions));
        Assert.Equal(Canvas.LeftProperty, transition.Property);
        Assert.Equal(SlideDuration, transition.Duration);
        var spline = Assert.IsType<SplineEasing>(transition.Easing);
        Assert.Equal(SlideSpline, (spline.X1, spline.Y1, spline.X2, spline.Y2));
    }

    // Set from code, the knob slides (it is mid-way a frame in) and comes to rest at the other end; the curtain is as wide
    // as the knob is far across, so its edge is always under the knob.
    [AvaloniaFact]
    public void Set_From_Code_It_Slides_To_The_Other_Rest()
    {
        using var stage = new Stage();
        var t = stage.Toggle;

        var widths = new List<double>();
        CurtainOf(t).PropertyChanged += (_, e) =>
        {
            if (e.Property == Layoutable.WidthProperty)
                widths.Add((double)e.NewValue!);
        };
        var seen = Watch(t, () =>
        {
            t.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
        });

        // Slid: values on the way, at animation priority, each the curtain's width too.
        var slide = seen.Where(v => v.Priority == BindingPriority.Animation).Select(v => v.Value).ToList();
        Assert.Contains(slide, v => v > 0 && v < KnobOnToOffOffset);
        Assert.All(slide.Where(v => v > 0), v => Assert.Contains(v, widths));
        Assert.Equal(KnobOnToOffOffset, KnobAt(t), 0.0001);
        Assert.Equal(KnobOnToOffOffset, CurtainOf(t).Bounds.Width, 0.0001);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The tap and the drag (tools/refapp/README.md, "ToggleSwitch: tap or drag" and "the hold limit, the distance and
    // the halfway line").

    // A tap on the knob toggles, by the mouse and by touch, both ways, and runs the switch's Click and Command, as
    // Avalonia's click does.
    [AvaloniaFact]
    public void A_Tap_Toggles()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var clicks = 0;
        var command = new CountingCommand();
        t.Click += (_, _) => clicks++;
        t.Command = command;

        Drag(stage);
        Assert.True(t.IsChecked);
        Assert.Equal(KnobOnToOffOffset, RestAt(t), 0.0001);

        var knob = stage.Knob();
        var touch = stage.Window.TouchBegin(knob);
        Dispatcher.UIThread.RunJobs();
        stage.Window.TouchEnd(touch, knob);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
        Assert.Equal(0, RestAt(t));
        Assert.Equal(2, clicks);
        Assert.Equal(2, command.Runs);
    }

    // The knob follows the finger one for one from the first move, with no distance before it moves, and stays between
    // its rests.
    [AvaloniaFact]
    public void The_Knob_Follows_The_Finger_From_The_First_Move()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(0.5, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, KnobAt(t), 0.0001);
        stage.Window.MouseMove(start + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(40, KnobAt(t), 0.0001);
        Assert.Equal(40, CurtainOf(t).Width, 0.0001);
        stage.Window.MouseMove(start + new Point(200, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(KnobOnToOffOffset, KnobAt(t), 0.0001);
        stage.Window.MouseMove(start + new Point(-50, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, KnobAt(t));
        stage.Window.MouseUp(start + new Point(-50, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    // A press that moved less than 2.7 mm is a tap and toggles; the emulator's 17.47 view px tapped and 18.33 didn't.
    [AvaloniaFact]
    public void A_Tap_Can_Move_Under_Three_Millimetres()
    {
        Assert.InRange(PhoneToggleSwitch.TapDistance, TappedMove, NotTappedMove);

        using var stage = new Stage();
        var t = stage.Toggle;
        Drag(stage, 5, TappedMove);
        Assert.True(t.IsChecked);

        Drag(stage, -NotTappedMove);
        Assert.True(t.IsChecked);      // not a tap, and not past half way: back on
        Assert.Equal(KnobOnToOffOffset, RestAt(t), 0.0001);

        // Up and down count too: a tap is a distance on the screen, not across the switch.
        var start = stage.Knob();
        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(0, NotTappedMove));
        stage.Window.MouseUp(start, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);
    }

    // A tap that moved the knob puts it back at once, then slides it over, as the phone's Dragging to Off, then Off to
    // On, does: the slide starts at the rest, not where the finger left the knob.
    [AvaloniaFact]
    public void A_Tap_That_Moved_The_Knob_Slides_From_Its_Rest()
    {
        using var stage = new Stage();
        var t = stage.Toggle;

        var seen = Watch(t, () => Drag(stage, 12));
        Assert.True(t.IsChecked);
        var release = seen.FindLastIndex(v => v.Priority == BindingPriority.LocalValue && Math.Abs(v.Value - 12) < 1e-6);
        Assert.True(release >= 0, "the knob didn't follow the finger to 12");
        Assert.Equal((0.0, BindingPriority.LocalValue), seen[release + 1]);                  // back at once
        var slide = seen.Skip(release + 2).Where(v => v.Priority == BindingPriority.Animation).Select(v => v.Value).ToList();
        Assert.NotEmpty(slide);
        Assert.All(slide, v => Assert.InRange(v, 0, KnobOnToOffOffset));
        Assert.Equal(KnobOnToOffOffset, RestAt(t), 0.0001);
    }

    // A drag released short of half way slides back from where it is; past half way, over. The line is half the knob's
    // offset: on device 11 a release at 28.32 went back Off and one at 28.35 went On. A drag sets IsChecked, as
    // Avalonia's own drag does, without a Click.
    [AvaloniaFact]
    public void A_Drag_Goes_Over_Past_Half_Way()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var clicks = 0;
        t.Click += (_, _) => clicks++;

        var back = Watch(t, () => Drag(stage, 20, BackOff));
        Assert.False(t.IsChecked);
        Assert.Equal(0, RestAt(t));
        // The slide back starts where the knob is: every value after the release between it and the rest.
        var slide = After(back, BackOff);
        Assert.Contains(slide, v => v > 0 && v < BackOff);
        Assert.All(slide, v => Assert.InRange(v, 0, BackOff));

        var over = Watch(t, () => Drag(stage, 20, GoesOn));
        Assert.True(t.IsChecked);
        Assert.Equal(KnobOnToOffOffset, RestAt(t), 0.0001);
        slide = After(over, GoesOn);
        Assert.Contains(slide, v => v > GoesOn && v < KnobOnToOffOffset);
        Assert.All(slide, v => Assert.InRange(v, GoesOn, KnobOnToOffOffset));
        Assert.Equal(0, clicks);

        // From on, the same line, the other way: as far back from on. (No probe dragged from on; WinRT's code puts its
        // line 0.17 nearer, from the template's 56.5: COMPARISON.md.)
        Drag(stage, -20, -BackOff);
        Assert.True(t.IsChecked);
        Drag(stage, -20, -GoesOn);
        Assert.False(t.IsChecked);
    }

    // The finger past the end and back: the knob follows from where the finger has it, as the phone's translation adds
    // up the drag's moves.
    [AvaloniaFact]
    public void Past_The_End_The_Finger_Must_Come_Back_Before_The_Knob_Does()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(-30, 0));
        stage.Window.MouseMove(start + new Point(-10, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, KnobAt(t));
        stage.Window.MouseMove(start + new Point(10, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(10, KnobAt(t), 0.0001);
        stage.Window.MouseUp(start + new Point(10, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    // The hold: a press that sees a pointer event 1,000 ms or more after it isn't a tap; the emulator's latest event that
    // still tapped came 984 ms in. The release counts too (Decided: Avalonia's pointers needn't report a still press).
    [AvaloniaFact]
    public void A_Press_Held_A_Second_Is_Not_A_Tap()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        clock.At(TappedEventMs);
        stage.Window.MouseMove(start + new Point(1, 0));
        clock.At(TappedEventMs + 10);
        stage.Window.MouseUp(start + new Point(1, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);

        clock.At(5000);
        stage.Window.MouseDown(start, MouseButton.Left);
        clock.At(5000 + HeldEventMs);
        stage.Window.MouseMove(start + new Point(-1, 0));
        clock.At(5000 + HeldEventMs + 1);
        stage.Window.MouseUp(start + new Point(-1, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);

        clock.At(10000);
        stage.Window.MouseDown(start, MouseButton.Left);
        clock.At(10000 + HeldEventMs);
        stage.Window.MouseUp(start, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);

        clock.At(15000);
        stage.Window.MouseDown(start, MouseButton.Left);
        clock.At(15000 + HeldEventMs - 1);
        stage.Window.MouseUp(start, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
    }

    // A hold that moved past half way still goes over: the knob's place decides, as for any drag.
    [AvaloniaFact]
    public void A_Held_Drag_Still_Goes_By_The_Knob()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(GoesOn, 0));
        clock.At(1500);
        stage.Window.MouseUp(start + new Point(GoesOn, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);
    }

    // Only the phone's Thumb takes the finger: the switch, 13.5 either side, 15.5 above and 6.5 below. A press on the
    // header or the content, or beside the Thumb, does nothing.
    [AvaloniaFact]
    public void Only_The_Switch_Takes_The_Finger()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var bounds = Part<Grid>(t, "SwitchKnobBounds");
        var left = In(bounds, t).X;
        var top = In(bounds, t).Y;
        var bottom = top + bounds.Bounds.Height;
        var right = left + bounds.Bounds.Width;

        void Tap(double x, double y)
        {
            var at = stage.At(x, y);
            stage.Window.MouseDown(at, MouseButton.Left);
            stage.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        // Past the control's right edge the phone's Thumb reaches 13.5 further, but Avalonia hit-tests nothing outside
        // the control's bounds: a deliberate difference (COMPARISON.md).
        // The template's grid is transparent, as the phone's: an empty part of the switch is still the switch's.
        using (stage.Capture())
            Assert.Contains(t, stage.Window.GetVisualsAt(stage.At(200, 40)).SelectMany(v => v.GetSelfAndVisualAncestors()));

        // The right button: no drag, no tap.
        var knob = stage.Knob();
        stage.Window.MouseDown(knob, MouseButton.Right);
        stage.Window.MouseMove(knob + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, KnobAt(t));
        stage.Window.MouseUp(knob + new Point(40, 0), MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked, "a right click toggled");

        foreach (var (x, y) in new[] { (10.0, 10.0), (10.0, 40.0), (100.0, 40.0), (left - ThumbOverhang.Left - 0.5, 40.0), (left + 10, top - ThumbOverhang.Top - 0.5), (right + 0.5, 40.0) })
        {
            Tap(x, y);
            Assert.False(t.IsChecked, $"a tap at ({x}, {y}) toggled");
        }

        var on = false;
        foreach (var (x, y) in new[] { (left - ThumbOverhang.Left + 0.5, 40.0), (left + 10, top - ThumbOverhang.Top + 0.5), (left + 40, bottom + ThumbOverhang.Bottom - 0.5), (right - 0.5, 40.0) })
        {
            Tap(x, y);
            on = !on;
            Assert.True(on == t.IsChecked, $"a tap at ({x}, {y}) didn't toggle");
        }
    }

    // A control in the content still takes its own press.
    [AvaloniaFact]
    public void A_Control_In_The_Content_Takes_Its_Own_Press()
    {
        var button = new Button { Content = "x", Width = 60, Height = 30, Theme = new ControlTheme(typeof(Button)) { Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent })) } } };
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var toggle = Specimen();
        toggle.OffContent = button;
        using var stage = new Stage(toggle: toggle);

        var at = button.TranslatePoint(new Point(30, 15), stage.Window)!.Value;
        stage.Window.MouseDown(at, MouseButton.Left);
        stage.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
        Assert.False(stage.Toggle.IsChecked);
    }

    // A second finger takes nothing over: the first press's pointer moves the knob, and its release decides.
    [AvaloniaFact]
    public void A_Second_Finger_Changes_Nothing()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        var first = stage.Window.TouchBegin(start);
        stage.Window.TouchMove(first, start + new Point(10, 0));
        var second = stage.Window.TouchBegin(start + new Point(-5, 0));
        stage.Window.TouchMove(second, start + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(10, KnobAt(t), 0.0001);
        stage.Window.TouchEnd(second, start + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
        Assert.Equal(10, KnobAt(t), 0.0001);
        stage.Window.TouchEnd(first, start + new Point(10, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);                 // the first finger's tap
    }

    // Turned off, the switch is Avalonia's again: a click on the words toggles it. Turned back on, with the template
    // already applied, it is the phone's.
    [AvaloniaFact]
    public void Turned_Off_It_Lets_Go_And_Turned_On_It_Takes_Hold()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var words = stage.At(10, 40);

        void Click(Point at)
        {
            stage.Window.MouseDown(at, MouseButton.Left);
            stage.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        PhoneToggleSwitch.SetIsEnabled(t, false);
        Click(words);
        Assert.True(t.IsChecked);

        PhoneToggleSwitch.SetIsEnabled(t, true);
        Click(words);
        Assert.True(t.IsChecked);
        Click(stage.Knob());
        Assert.False(t.IsChecked);
    }

    // IsChecked set by code during a press: the release rests the knob where IsChecked now has it, short of half way or
    // past it, and nothing toggles it back.
    [AvaloniaFact]
    public void A_Change_From_Code_During_A_Press_Is_Where_The_Knob_Rests()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(20, 0));
        t.IsChecked = true;
        stage.Window.MouseMove(start + new Point(22, 0));      // not a tap, short of half way
        stage.Window.MouseUp(start + new Point(22, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);
        Assert.Equal(KnobOnToOffOffset, RestAt(t), 0.0001);

        Settle();
        start = stage.Knob();
        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(-40, 0));
        t.IsChecked = false;
        stage.Window.MouseMove(start + new Point(-45, 0));
        stage.Window.MouseUp(start + new Point(-45, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
        Assert.Equal(0, RestAt(t));
    }

    // Disabled during a press, the release toggles nothing, and the knob goes back.
    [AvaloniaFact]
    public void Disabled_During_A_Press_Nothing_Toggles()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(40, 0));
        t.IsEnabled = false;
        stage.Window.MouseUp(start + new Point(40, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
        Assert.Equal(0, RestAt(t));
    }

    // Another of the mouse's buttons let go during a left drag decides nothing: Avalonia's mouse lets the capture go at
    // any button's release, so the drag ends as a lost capture does, the knob back at its rest, and the left button's
    // own release after it toggles nothing.
    [AvaloniaFact]
    public void Another_Button_Let_Go_Decides_No_Drag()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(40, 0));
        stage.Window.MouseDown(start + new Point(40, 0), MouseButton.Right);
        stage.Window.MouseUp(start + new Point(40, 0), MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
        Assert.Equal(0, RestAt(t));
        stage.Window.MouseUp(start + new Point(40, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
    }

    // Turned off during a press, the knob goes back to its rest with its slide, and later changes slide too.
    [AvaloniaFact]
    public void Turned_Off_During_A_Press_The_Knob_Goes_Back()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Null(KnobsOf(t).Transitions);
        PhoneToggleSwitch.SetIsEnabled(t, false);
        Assert.Equal(0, RestAt(t));
        Assert.Same(t.KnobTransitions, KnobsOf(t).Transitions);
        stage.Window.MouseUp(start + new Point(40, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    // While the knob follows the finger, Avalonia's own ToggleSwitch, which doesn't know of the press, can't take it back:
    // a change of the switch's size would put it at its rest, and a change of its transitions would make it lag.
    [AvaloniaFact]
    public void Avalonias_Own_Handlers_Leave_A_Followed_Knob_Alone()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(40, 0));
        t.Margin = new Thickness(0, 0, 20, 0);
        t.KnobTransitions = [new DoubleTransition { Property = Canvas.LeftProperty, Duration = TimeSpan.FromSeconds(1) }];
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(40, KnobAt(t), 0.0001);
        Assert.Null(KnobsOf(t).Transitions);
        stage.Window.MouseMove(start + new Point(45, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(45, KnobAt(t), 0.0001);
        stage.Window.MouseUp(start + new Point(45, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(t.IsChecked);
        Assert.Same(t.KnobTransitions, KnobsOf(t).Transitions);
    }

    // A still press mid-slide leaves the slide running: the knob is the finger's from its first move.
    [AvaloniaFact]
    public void A_Still_Press_Mid_Slide_Leaves_The_Slide()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var knob = stage.Knob();

        Drag(stage);
        Assert.True(t.IsChecked);
        stage.Window.MouseDown(knob, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(t.KnobTransitions, KnobsOf(t).Transitions);
        stage.Window.MouseMove(knob + new Point(-3, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Null(KnobsOf(t).Transitions);
        Assert.Equal(KnobOnToOffOffset - 3, KnobAt(t), 0.0001);
        stage.Window.MouseUp(knob + new Point(-3, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
    }

    // A subclass whose automation peer has no toggle pattern is still toggled by a tap.
    [AvaloniaFact]
    public void A_Tap_Toggles_A_Switch_Without_A_Toggle_Pattern()
    {
        var toggle = new PlainPeerSwitch();
        ControlHeader.SetHeader(toggle, Header);
        using var stage = new Stage(toggle: toggle);

        Drag(stage);
        Assert.True(stage.Toggle.IsChecked);
    }

    private sealed class PlainPeerSwitch : ToggleSwitch
    {
        protected override Type StyleKeyOverride => typeof(ToggleSwitch);

        protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
            new Avalonia.Automation.Peers.ControlAutomationPeer(this);
    }

    // A lost capture is no release: the knob goes back, and nothing toggles.
    [AvaloniaFact]
    public void A_Lost_Capture_Puts_The_Knob_Back()
    {
        using var stage = new Stage();
        var t = stage.Toggle;
        var start = stage.Knob();
        IPointer? pointer = null;
        Part<Panel>(t, "SwitchThumb").AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer,
            RoutingStrategies.Bubble, handledEventsToo: true);

        stage.Window.MouseDown(start, MouseButton.Left);
        stage.Window.MouseMove(start + new Point(40, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(40, KnobAt(t), 0.0001);
        Assert.NotNull(pointer);
        pointer.Capture(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, RestAt(t));
        stage.Window.MouseUp(start + new Point(40, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(t.IsChecked);
    }

    // A touch drag on the switch never scrolls its page (the phone's ManipulationMode None); one on the header does.
    [AvaloniaFact]
    public void A_Drag_On_The_Switch_Never_Scrolls_Its_Page()
    {
        var toggle = Specimen();
        var page = new Border { Height = 2000, Background = Brushes.Transparent };
        var scroller = new ScrollViewer { Content = new StackPanel { Children = { toggle, page } } };
        using var scene = new Scene(scroller);

        void Swipe(Point from)
        {
            var touch = scene.Window.TouchBegin(from);
            for (var step = 1; step <= 10; step++)
            {
                scene.Window.TouchMove(touch, from + new Point(0, -15 * step));
                Dispatcher.UIThread.RunJobs();
            }

            scene.Window.TouchEnd(touch, from + new Point(0, -150));
            Dispatcher.UIThread.RunJobs();
        }

        var knob = KnobOf(toggle);
        Swipe(knob.TranslatePoint(new Point(10, 30), scene.Window)!.Value);
        Assert.Equal(0, scroller.Offset.Y);
        Assert.False(toggle.IsChecked);

        Swipe(toggle.TranslatePoint(new Point(50, 10), scene.Window)!.Value);
        Assert.True(scroller.Offset.Y > 0, "a drag on the header didn't scroll the page");
    }

    // Nothing changes under a pointer or a press that doesn't move: the phone's PointerOver and Pressed set nothing.
    [AvaloniaFact]
    public void Pointer_Over_And_Press_Change_No_Pixel()
    {
        using var stage = new Stage();
        using var rest = stage.Capture();

        stage.Window.MouseMove(stage.Knob());
        using (var over = stage.Capture())
            Assert.True(Frames.SameAs(rest, over), "the pointer over it changed a pixel");

        stage.Window.MouseDown(stage.Knob(), MouseButton.Left);
        using (var pressed = stage.Capture())
            Assert.True(Frames.SameAs(rest, pressed), "a press changed a pixel");
        stage.Window.MouseMove(stage.At(10, 10));
        stage.Window.MouseUp(stage.At(10, 10), MouseButton.Left);    // far off: not a tap
        Dispatcher.UIThread.RunJobs();
        Settle();
        Assert.False(stage.Toggle.IsChecked);
        stage.Window.MouseMove(new Point(1, 1));
        using (var after = stage.Capture())
            Assert.True(Frames.SameAs(rest, after), "the press left a pixel changed");
    }

    // Keyboard focus draws nothing: the phone's template has no focus state, and the theme clears the FocusAdorner,
    // which would otherwise be Simple's default, a dotted rectangle.
    [AvaloniaFact]
    public void Keyboard_Focus_Changes_No_Pixel()
    {
        using var stage = new Stage();
        using var rest = stage.Capture();

        stage.Toggle.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Toggle.IsFocused);
        using var focused = stage.Capture();
        Assert.True(Frames.SameAs(rest, focused), "keyboard focus changed a pixel");
    }

    // The colours follow a change of variant at run time, through the keys.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark, Specimen(on: true));
        var t = stage.Toggle;
        Assert.Equal(Foreground(true), ColourOf(KnobOf(t).Fill));

        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Foreground(false), ColourOf(KnobOf(t).Fill));
        Assert.Equal(Background(false), ColourOf(KnobOf(t).Stroke));
        Assert.Equal(HeaderInk(false), ColourOf(HeaderOf(t).Foreground));
    }

    private sealed class CountingCommand : ICommand
    {
        public int Runs { get; private set; }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Runs++;
    }
}
