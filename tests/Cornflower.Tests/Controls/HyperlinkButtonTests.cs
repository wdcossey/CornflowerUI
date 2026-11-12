using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.HyperlinkButtonReadings;
using static Cornflower.Tests.Behaviours.TiltReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 HyperlinkButton (<c>Controls/HyperlinkButton.axaml</c>) against the phone's style and the reference app's
/// captures (<see cref="HyperlinkButtonReadings"/>), and its tilt, on its <c>ContentPresenter</c>, against the
/// emulator's readings (<see cref="TiltReadings"/>). docs/controls/HyperlinkButton has the plan and the comparison.
/// </summary>
public class HyperlinkButtonTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>A HyperlinkButton on the reference app's stage: 450 × 100, the page's background, in a slot 19 in and 12 down.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, HyperlinkButton? link = null)
        {
            Link = link ?? new HyperlinkButton { Content = "hyperlink button" };
            var slot = new Grid { Margin = Slot, Children = { Link } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: StageHeight);
        }

        public HyperlinkButton Link { get; }

        public Window Window => _scene.Window;

        /// <summary>The control's centre, in the window.</summary>
        public Point Centre => Link.TranslatePoint(new Point(Link.Bounds.Width / 2, Link.Bounds.Height / 2), Window)!.Value;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Border BorderOf(HyperlinkButton link) => Part<Border>(link, "Border");

    private static ContentPresenter PresenterOf(HyperlinkButton link) => Part<ContentPresenter>(link, "PART_ContentPresenter");

    private static TextBlock TextOf(HyperlinkButton link) => PresenterOf(link).GetVisualDescendants().OfType<TextBlock>().Single();

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>Every pixel of a frame, read once: [y, x].</summary>
    private static Color[,] Pixels(WriteableBitmap frame)
    {
        using var buffer = frame.Lock();
        var rgba = buffer.Format == PixelFormat.Rgba8888;
        Assert.True(rgba || buffer.Format == PixelFormat.Bgra8888, $"A frame in {buffer.Format} isn't read.");
        var (w, h) = (buffer.Size.Width, buffer.Size.Height);
        var row = new int[w];
        var pixels = new Color[h, w];
        for (var y = 0; y < h; y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, w);
            for (var x = 0; x < w; x++)
            {
                var v = (uint)row[x];
                byte b0 = (byte)v, b1 = (byte)(v >> 8), b2 = (byte)(v >> 16), b3 = (byte)(v >> 24);
                pixels[y, x] = rgba ? Color.FromArgb(b3, b0, b1, b2) : Color.FromArgb(b3, b2, b1, b0);
            }
        }

        return pixels;
    }

    /// <summary>The commonest colour that isn't the page's: the inside of the glyphs, the text's colour on the page.</summary>
    private static Color CommonestInk(Color[,] pixels, Color page) =>
        pixels.Cast<Color>().Where(c => c != page).GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;

    private static bool IsGrey(Color c) => c.R == c.G && c.G == c.B;

    // The style's setters, as the dump read them (hyperlinkbutton.json's keys and literals, themeresources.json's values).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var link = stage.Link;

        Assert.Equal(Padding, link.Padding);
        Assert.Equal(BorderThickness, link.BorderThickness);
        Assert.Equal(FontSize, link.FontSize);
        Assert.Equal(Weight, link.FontWeight);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilySemiLight"), link.FontFamily);
        Assert.Equal(HorizontalAlignment.Left, link.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, link.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, link.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, link.VerticalContentAlignment);
        Assert.Equal(0, link.MinWidth);
        Assert.Equal(0, link.MinHeight);
        Assert.Equal(Foreground(dark), ColourOf(link.Foreground));
        Assert.Equal(Transparent, ColourOf(link.Background));
        Assert.Equal(Transparent, ColourOf(link.BorderBrush));
        Assert.Equal(Padding, PresenterOf(link).Margin);
        Assert.Equal(Foreground(dark), ColourOf(TextOf(link).Foreground));
    }

    // FontFamily is the style's, not whatever the control sits in.
    [AvaloniaFact]
    public void The_Font_Is_The_Phones_Whatever_It_Sits_In()
    {
        var link = new HyperlinkButton { Content = "hyperlink button" };
        var parent = new ContentControl { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeight.Bold, Content = link };
        using var scene = new Scene(parent);

        Assert.Equal(scene.Window.FindResource("PhoneFontFamilySemiLight"), link.FontFamily);
        Assert.Equal(Weight, link.FontWeight);
    }

    // The dump's bounds: the control on the stage (its width is the font's), Grid and Border filling it, and the
    // content's top and height.
    [AvaloniaFact]
    public void Its_Bounds_Are_The_Phones()
    {
        using var stage = new Stage();
        var link = stage.Link;
        var inStage = link.TranslatePoint(default, (Visual)stage.Window.Content!)!.Value;

        Assert.Equal(InStage.X, inStage.X, 0.001);
        Assert.Equal(InStage.Y, inStage.Y, 0.001);
        Assert.Equal(InStage.Height, link.Bounds.Height, 0.001);

        Assert.Equal(link.Bounds.Size, Part<Panel>(link, "Grid").Bounds.Size);
        Assert.Equal(link.Bounds.Size, BorderOf(link).Bounds.Size);

        var presenter = PresenterOf(link);
        Assert.Equal(ContentTop, presenter.TranslatePoint(default, link)!.Value.Y, 0.001);
        Assert.Equal(ContentHeight, presenter.Bounds.Height, 0.001);
        Assert.Equal(link.Bounds.Width, presenter.Bounds.Width, 0.001);
    }

    // HorizontalContentAlignment and VerticalContentAlignment Center, WinRT's defaults, which the dump reads.
    [AvaloniaFact]
    public void The_Content_Is_Centred_In_A_Larger_Control()
    {
        using var stage = new Stage(link: new HyperlinkButton { Content = "link", Width = 400, Height = 80 });
        var presenter = PresenterOf(stage.Link);
        var box = new Rect(presenter.TranslatePoint(default, stage.Link)!.Value, presenter.Bounds.Size);

        Assert.Equal(200, box.Center.X, 0.5);
        Assert.True(box.Width < 200, $"the content is {box.Width} wide: stretched, not centred");
        Assert.Equal(Padding.Top + (80 - Padding.Top - Padding.Bottom) / 2, box.Center.Y, 0.5);   // in the middle of the padding's space
        Assert.Equal(ContentHeight, box.Height, 0.001);
    }

    // The phone's TextBlock is 39.7167 tall: Segoe WP's 1.33 em line at 29.86 (ADR 0003).
    [AvaloniaFact]
    public void A_Strings_Line_Is_The_Phones()
    {
        using var stage = new Stage();

        Assert.Equal(1.33 * FontSize, TextOf(stage.Link).LineHeight, 0.01);
    }

    // The PNGs draw the text alone: no underline (the longest ink on a row is 31 px of 497), where Simple's
    // HyperlinkButton underlines it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void There_Is_No_Underline(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();
        var pixels = Pixels(frame);
        var page = Page(dark);

        var (left, right, longest) = (int.MaxValue, 0, 0);
        for (var y = 0; y < pixels.GetLength(0); y++)
        {
            var run = 0;
            for (var x = 0; x < pixels.GetLength(1); x++)
            {
                if (pixels[y, x] == page) { run = 0; continue; }
                (left, right, longest) = (Math.Min(left, x), Math.Max(right, x), Math.Max(longest, ++run));
            }
        }

        var width = right - left + 1;
        Assert.True(width > 400, $"the text is {width} px wide");
        Assert.True(longest < width / 8, $"a row has {longest} px of ink in a run, of the text's {width}: an underline");
        Assert.True(TextOf(stage.Link).TextDecorations is null or { Count: 0 });
    }

    // The text in the foreground: the glyphs' insides are #FFFFFFFF dark, #FF000000 light, as in the PNGs.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Text_Is_Drawn_In_The_Foreground(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        Assert.Equal(Hex(Ink(dark)), Hex(CommonestInk(Pixels(frame), Page(dark))));
    }

    // CommonStates.Disabled: the text in HyperlinkDisabledThemeBrush, PhoneDisabledColor (#66), drawn #FF666666 dark and
    // #FF999999 light.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Text_As_The_Phone_Does(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), new HyperlinkButton { Content = "hyperlink button", IsEnabled = false });

        Assert.Equal(Disabled(dark), ColourOf(PresenterOf(stage.Link).Foreground));
        using var frame = stage.Capture();
        Assert.Equal(Hex(DisabledInk(dark)), Hex(CommonestInk(Pixels(frame), Page(dark))));
    }

    // The phone's Border binds the control's Background, BorderBrush and BorderThickness: an app's are drawn.
    [AvaloniaFact]
    public void The_Apps_Background_And_Border_Are_Drawn()
    {
        var link = new HyperlinkButton { Content = "hyperlink button", Background = Brushes.Red, BorderBrush = Brushes.Lime, BorderThickness = new Thickness(2) };
        using var stage = new Stage(link: link);
        var border = BorderOf(link);

        Assert.Equal(Colors.Red, ColourOf(border.Background));
        Assert.Equal(Colors.Lime, ColourOf(border.BorderBrush));
        Assert.Equal(new Thickness(2), border.BorderThickness);
        using var frame = stage.Capture();
        var corner = link.TranslatePoint(new Point(0.5, 0.5), stage.Window)!.Value * Scene.ReferenceScaling;
        Assert.Equal(Colors.Lime, Frames.PixelAt(frame, (int)corner.X, (int)corner.Y));
    }

    // The phone's Disabled sets only the text's colour: an app's background stays, where a Button's goes.
    [AvaloniaFact]
    public void Disabled_Keeps_The_Apps_Background()
    {
        using var stage = new Stage(link: new HyperlinkButton { Content = "hyperlink button", Background = Brushes.Red, IsEnabled = false });

        Assert.Equal(Colors.Red, ColourOf(BorderOf(stage.Link).Background));
    }

    // CommonStates.Pressed only tilts: the dump's colours are the rest's, and the PNG has no colour but greys.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Pressed_Changes_No_Colour(string variant)
    {
        var dark = variant == "Dark";
        using var clock = new TestClock();
        using var stage = new Stage(VariantOf(variant));
        var link = stage.Link;

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(link.IsPressed);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(link));
        Assert.Equal(Foreground(dark), ColourOf(TextOf(link).Foreground));
        Assert.Equal(Transparent, ColourOf(BorderOf(link).Background));
        using var frame = stage.Capture();
        var pixels = Pixels(frame);
        var colours = pixels.Cast<Color>().Where(c => !IsGrey(c)).Distinct().Take(3).Select(Hex).ToList();
        Assert.True(colours.Count == 0, $"coloured pixels: {string.Join(", ", colours)}");
        Assert.Equal(Hex(Ink(dark)), Hex(CommonestInk(pixels, Page(dark))));
    }

    // CommonStates.PointerOver changes nothing on the phone: its capture is the base's, byte for byte. So is ours.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Pointer_Over_It_Changes_Nothing(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        using var before = stage.Capture();
        var rest = Pixels(before);

        stage.Window.MouseMove(stage.Centre);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Link.IsPointerOver);
        using var after = stage.Capture();

        Assert.Equal(rest.Cast<Color>(), Pixels(after).Cast<Color>());
    }

    // The phone has no visited state: Avalonia's :visited, which Simple draws in another colour, draws nothing new.
    [AvaloniaFact]
    public void Visited_Changes_Nothing()
    {
        using var stage = new Stage();
        using var before = stage.Capture();
        var rest = Pixels(before);

        stage.Link.IsVisited = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":visited", stage.Link.Classes);
        using var after = stage.Capture();

        Assert.Equal(rest.Cast<Color>(), Pixels(after).Cast<Color>());
    }

    // The phone's template has no FocusStates group: keyboard focus draws nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_Nothing()
    {
        using var stage = new Stage();
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Link.IsFocused, "Tab didn't reach the control");
        var layer = AdornerLayer.GetAdornerLayer(stage.Link);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // ThemeResource: the colours follow a change of variant.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark);
        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Foreground(dark: false), ColourOf(stage.Link.Foreground));
        using var frame = stage.Capture();
        Assert.Equal(Hex(Ink(dark: false)), Hex(CommonestInk(Pixels(frame), Page(dark: false))));
    }

    // Avalonia's access keys, as in the Button's theme: "_Open" draws "Open", and Alt+O clicks it.
    [AvaloniaFact]
    public void An_Access_Key_Is_Drawn_Without_Its_Underscore_And_Alt_Clicks_It()
    {
        var link = new HyperlinkButton { Content = "_Open" };
        using var stage = new Stage(link: link);
        var clicks = 0;
        link.Click += (_, _) => clicks++;

        Assert.IsType<AccessText>(TextOf(link));
        Assert.Equal(1.33 * FontSize, TextOf(link).LineHeight, 0.01);

        stage.Window.KeyPressQwerty(PhysicalKey.O, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    // The phone's PointerDownThemeAnimation targets its ContentPresenter, not the Grid: the presenter tilts, and the
    // Grid and Border around it don't.
    [AvaloniaFact]
    public void A_Press_Tilts_The_ContentPresenter_Alone()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var link = stage.Link;
        var presenter = PresenterOf(link);

        stage.Window.MouseDown(stage.Centre, MouseButton.Left);
        clock.At(SettleMs);

        Assert.True(CfTilt.GetIsEnabled(link));
        Assert.Same(presenter, CfTilt.TargetOf(link));
        Assert.IsType<MatrixTransform>(presenter.RenderTransform);
        Assert.Null(Part<Panel>(link, "Grid").RenderTransform);
        Assert.Null(BorderOf(link).RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(link));
    }

    /// <summary>
    /// A HyperlinkButton whose content presenter, the part that tilts, has the bounds of a probe's Button: no padding,
    /// content of the Button's size, at the Button's place on the reference view.
    /// </summary>
    private static (Scene Scene, HyperlinkButton Link) ProbeLink(Layout layout)
    {
        var link = new HyperlinkButton { Padding = default, Content = new Border { Width = layout.Width, Height = layout.Height } };
        Canvas.SetLeft(link, layout.Left);
        Canvas.SetTop(link, layout.Top);
        return (new Scene(new Canvas { Children = { link } }), link);
    }

    // Run 20261109-2307-dev11's rule is the element's, whatever it is: a presenter with Button A's bounds (120 × 60),
    // pressed at its 25 points, settles at each reading.
    [AvaloniaFact]
    public void A_Held_Press_Settles_At_The_Phones_Tilt_For_Its_Point()
    {
        using var clock = new TestClock();
        var layout = GridButtons.Single(b => b.Name == "A");
        var (scene, link) = ProbeLink(layout);
        using var _ = scene;
        var presenterAt = PresenterOf(link).TranslatePoint(default, scene.Window)!.Value;
        Assert.Equal(layout.Bounds.X, presenterAt.X, 0.01);           // the layout's 19.167, laid out on the pixel grid
        Assert.Equal(layout.Bounds.Y, presenterAt.Y, 0.01);
        Assert.Equal(layout.Bounds.Size, PresenterOf(link).Bounds.Size);
        var misses = new List<string>();
        var time = 0.0;

        foreach (var press in GridPresses.Where(p => p.Button == "A"))
        {
            var at = layout.Bounds.TopLeft + press.Point;
            var touch = scene.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            var got = CfTilt.Current(link);
            if (Math.Abs(got.RotationX - press.RotationX) > 0.04 || Math.Abs(got.RotationY - press.RotationY) > 0.04 ||
                Math.Abs(got.OffsetZ - press.OffsetZ) > 0.085)
                misses.Add(string.Create(CultureInfo.InvariantCulture,
                    $"at {press.X}, {press.Y}: read ({press.RotationX}, {press.RotationY}, {press.OffsetZ}), got ({got.RotationX:F3}, {got.RotationY:F3}, {got.OffsetZ:F3})"));
            scene.Window.TouchEnd(touch, at);
            clock.At(time += 1000);
        }

        Assert.True(misses.Count == 0, string.Join("\n", misses));
    }

    // The phone's Grid and Border stay untilted and take the hits, so a tap just inside a tilted edge clicks: run
    // 20261110-0822-dev11's taps, on a presenter of the first Button's bounds.
    [AvaloniaFact]
    public void A_Tap_Just_Inside_Each_Edge_Clicks()
    {
        using var clock = new TestClock();
        var (scene, link) = ProbeLink(FirstButton);
        using var _ = scene;
        var clicked = new List<Point>();
        var time = 0.0;

        foreach (var tap in EdgeTaps)
        {
            var clicks = 0;
            void Count(object? sender, RoutedEventArgs e) => clicks++;
            link.Click += Count;
            var at = FirstButton.Bounds.TopLeft + tap;
            var touch = scene.Window.TouchBegin(at);
            clock.At(time += SettleMs);
            Assert.NotEqual(TiltState.Rest, CfTilt.Current(link));
            scene.Window.TouchEnd(touch, at);
            link.Click -= Count;
            if (clicks == 1) clicked.Add(tap);
            clock.At(time += 1000);
        }

        Assert.Equal(EdgeTaps, clicked);
    }

    // The padding above the text is the control's too: a tap there clicks, as the phone's Border is hit across it.
    [AvaloniaFact]
    public void A_Tap_In_The_Padding_Clicks()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        var clicks = 0;
        stage.Link.Click += (_, _) => clicks++;
        var above = stage.Link.TranslatePoint(new Point(100, 1), stage.Window)!.Value;   // above the presenter's 2.92

        stage.Window.MouseDown(above, MouseButton.Left);
        clock.At(SettleMs);
        stage.Window.MouseUp(above, MouseButton.Left);

        Assert.Equal(1, clicks);
        clock.At(SettleMs + 1000);
        Assert.Equal(TiltState.Rest, CfTilt.Current(stage.Link));
    }
}
