using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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
using static Cornflower.Tests.Controls.TextBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 TextBox (<c>Controls/TextBox.axaml</c>) against the phone's style and the reference app's captures
/// (<see cref="TextBoxReadings"/>). docs/controls/TextBox has the plan and the comparison.
/// </summary>
public class TextBoxTests
{
    /// <summary>
    /// A TextBox on the reference app's stage: 450 × 140, the page's background, the TextBox in a slot 19 in, 12 down
    /// and 19 from the right. <see cref="TextBoxFrames"/> saves the comparison's frames on it too.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, TextBox? box = null, double height = StageHeight,
            IEnumerable<IStyle>? themes = null)
        {
            Box = box ?? Specimen(text: Text);
            var slot = new Grid { Margin = Slot, Children = { Box } };
            Root = new Grid { Children = { slot } };
            Root.Bind(Panel.BackgroundProperty, Root.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(Root, variant, width: StageWidth, height: height, themes: themes);
        }

        public TextBox Box { get; }

        public Grid Root { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the TextBox, in the window.</summary>
        public Point At(double x, double y) => Box.TranslatePoint(new Point(x, y), Window)!.Value;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>
    /// The reference app's specimens: <c>new TextBox { Header = "text box", Text = "Agjy 0123" }</c>, or with
    /// <c>PlaceholderText = "placeholder"</c>, or with no header.
    /// </summary>
    internal static TextBox Specimen(string? text = null, string? placeholder = null, bool header = true)
    {
        var box = new TextBox { Text = text, PlaceholderText = placeholder };
        if (header) ControlHeader.SetHeader(box, Header);
        return box;
    }

    /// <summary>The wrapped specimen of run 20261112-1319-dev11.</summary>
    internal static TextBox WrappedSpecimen()
    {
        var box = Specimen(text: Wrapped);
        box.TextWrapping = TextWrapping.Wrap;
        box.AcceptsReturn = true;
        return box;
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static Border BorderOf(TextBox b) => Part<Border>(b, "BorderElement");

    private static ContentPresenter HeaderOf(TextBox b) => Part<ContentPresenter>(b, "HeaderContentPresenter");

    private static ScrollViewer ScrollViewerOf(TextBox b) => Part<ScrollViewer>(b, "PART_ScrollViewer");

    private static TextPresenter TextOf(TextBox b) => Part<TextPresenter>(b, "PART_TextPresenter");

    private static ContentPresenter PlaceholderOf(TextBox b) => Part<ContentPresenter>(b, "PlaceholderTextContentPresenter");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    private static void Focus(TextBox box)
    {
        Assert.True(box.Focus(), "the TextBox took no focus");
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A raw pixel of the frame, in hex.</summary>
    private static string PixelHex(WriteableBitmap frame, int x, int y) => Hex(Frames.PixelAt(frame, x, y));

    // A TextBox and a MaskedTextBox (a TextBox to a theme) get the phone's template: no theme, no template, and a
    // TextBox measures 0 × 0 (CLAUDE.md).
    [AvaloniaFact]
    public void A_TextBox_And_A_MaskedTextBox_Have_The_Phones_Template()
    {
        var masked = new MaskedTextBox { Mask = "000" };
        var panel = new StackPanel { Children = { new TextBox(), masked } };
        using var scene = new Scene(panel);

        foreach (var box in panel.Children.OfType<TextBox>())
        {
            Assert.Null(box.Theme);
            BorderOf(box);
            Assert.Same(ScrollViewerOf(box), TextOf(box).GetVisualAncestors().OfType<ScrollViewer>().First());
            Assert.Equal(NoHeaderHeight, box.Bounds.Height, 0.001);
        }
    }

    // The style's setters, as the dump read them (textbox.json's keys, themeresources.json's values).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var box = stage.Box;

        Assert.Equal(MinWidth, box.MinWidth);
        Assert.Equal(MinHeight, box.MinHeight);
        Assert.Equal(Black, ColourOf(box.Foreground));
        Assert.Equal(Box(dark), ColourOf(box.Background));
        Assert.Equal(Box(dark), ColourOf(box.BorderBrush));
        Assert.Equal(new Thickness(BorderThickness), box.BorderThickness);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), box.FontFamily);
        Assert.Equal(FontSize, box.FontSize);
        Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        Assert.Equal(Padding, box.Padding);
        Assert.Equal(Margin, box.Margin);
        Assert.Equal(VerticalAlignment.Top, box.VerticalAlignment);
        Assert.Equal(ScrollBarVisibility.Hidden, ScrollViewer.GetHorizontalScrollBarVisibility(box));
        Assert.Equal(ScrollBarVisibility.Hidden, ScrollViewer.GetVerticalScrollBarVisibility(box));
        Assert.False(ScrollViewer.GetIsDeferredScrollingEnabled(box));

        // WinRT's SelectionHighlightColor is Avalonia's SelectionBrush; the placeholder's brush is the box's
        // PlaceholderForeground.
        Assert.Equal(Accent, ColourOf(box.SelectionBrush));
        Assert.Equal(PlaceholderColour(dark), ColourOf(box.PlaceholderForeground));

        // The template's parts.
        Assert.Equal(RestOpacity, BorderOf(box).Opacity);
        Assert.Equal(Box(dark), ColourOf(BorderOf(box).Background));
        Assert.Equal(Box(dark), ColourOf(BorderOf(box).BorderBrush));
        Assert.Equal(new Thickness(BorderThickness), BorderOf(box).BorderThickness);
        Assert.Equal(Padding, ScrollViewerOf(box).Padding);
        Assert.Equal(ScrollBarVisibility.Hidden, ScrollViewerOf(box).HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Hidden, ScrollViewerOf(box).VerticalScrollBarVisibility);
        Assert.Equal(Accent, ColourOf(TextOf(box).SelectionBrush));
        Assert.Null(box.FocusAdorner);
    }

    // The style's font and wrapping are its own: a window's font and an ancestor's TextWrapping (which inherits) don't
    // reach the box.
    [AvaloniaFact]
    public void Its_Font_And_Wrapping_Are_The_Styles()
    {
        var box = Specimen(text: Text);
        var holder = new Border { Child = box };
        TextBlock.SetTextWrapping(holder, TextWrapping.Wrap);
        using var scene = new Scene(holder);
        scene.Window.FontFamily = new FontFamily("Courier New");
        scene.Window.FontSize = 40;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(scene.Window.FindResource("PhoneFontFamilyNormal"), box.FontFamily);
        Assert.Equal(FontSize, box.FontSize);
        Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
    }

    // The bounds, from the dump: the header 21.6667, BorderElement 26.6667 down and 39.1667 tall, the text view at
    // [9.5833, 30], 33.3333 tall, the box 65.8333; 411.6667 wide where the phone's is 412.0833 (ADR 0003, readings).
    [AvaloniaFact]
    public void The_Bounds_Are_The_Dumps()
    {
        using var stage = new Stage();
        var box = stage.Box;

        Assert.Equal(InStage.X, In(box, stage.Root).X, 0.001);
        Assert.Equal(InStage.Y, In(box, stage.Root).Y, 0.001);
        Assert.Equal(OurWidth, box.Bounds.Width, 0.001);
        Assert.Equal(Height, box.Bounds.Height, 0.001);
        Assert.Equal(HeaderHeight, HeaderOf(box).Bounds.Height, 0.001);
        Assert.Equal(BorderTop, In(BorderOf(box), box).Y, 0.001);
        Assert.Equal(BorderHeight, BorderOf(box).Bounds.Height, 0.001);
        Assert.Equal(TextAt.X, In(TextOf(box), box).X, 0.001);
        Assert.Equal(TextAt.Y, In(TextOf(box), box).Y, 0.001);
        Assert.Equal(TextHeight, TextOf(box).Bounds.Height, 0.001);
    }

    // With no header (run 20261112-1319-dev11), the phone collapses HeaderContentPresenter, its 4.8 margin too: the box
    // is BorderElement alone, 39.1667, the text 3.3333 down.
    [AvaloniaFact]
    public void With_No_Header_The_Header_Takes_No_Room()
    {
        using var stage = new Stage(box: Specimen(text: Text, header: false));
        var box = stage.Box;

        Assert.False(HeaderOf(box).IsVisible);
        Assert.Equal(NoHeaderHeight, box.Bounds.Height, 0.001);
        Assert.Equal(0, In(BorderOf(box), box).Y, 0.001);
        Assert.Equal(TextAt.Y - BorderTop, In(TextOf(box), box).Y, 0.001);
    }

    // A header that is an empty string is a header: one empty line and the gap, as an empty TextBlock is a line on
    // the phone. Taking it away takes the room.
    [AvaloniaFact]
    public void An_Empty_Header_Is_A_Line_And_Null_Is_None()
    {
        var box = Specimen(text: Text);
        ControlHeader.SetHeader(box, "");
        using var stage = new Stage(box: box);
        Assert.Equal(Height, box.Bounds.Height, 0.001);

        ControlHeader.SetHeader(box, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(NoHeaderHeight, box.Bounds.Height, 0.001);
    }

    // The header in the phone's HeaderContentPresenterStyle: Segoe WP's key, 16, Normal, PhoneMidBrush, its line 1.33 em
    // (21.28, which rounds to 21.6667), 4.8 above the box; the box's own font doesn't reach it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Header_Is_The_Phones(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        var header = HeaderOf(stage.Box);
        stage.Box.FontFamily = new FontFamily("Courier New");
        stage.Box.FontSize = 40;
        stage.Box.FontWeight = FontWeight.Bold;
        Dispatcher.UIThread.RunJobs();
        var text = header.GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.Equal(Header, text.Text);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), header.FontFamily);
        Assert.Equal(HeaderFontSize, text.FontSize);
        Assert.Equal(FontWeight.Normal, text.FontWeight);
        Assert.Equal(HeaderColour(dark), ColourOf(header.Foreground));
        Assert.Equal(21.28, text.LineHeight, 0.01);
        Assert.Equal(new Thickness(0, 0, 0, HeaderGap), header.Margin);
        Assert.Equal(HeaderHeight, header.Bounds.Height, 0.001);
    }

    // WinRT's header doesn't recognise access keys: an underscore is drawn. And a HeaderTemplate is used.
    [AvaloniaFact]
    public void The_Header_Keeps_An_Underscore_And_Takes_A_HeaderTemplate()
    {
        var box = Specimen(text: Text);
        ControlHeader.SetHeader(box, "user_name");
        using var stage = new Stage(box: box);
        Assert.Equal("user_name", HeaderOf(box).GetVisualDescendants().OfType<TextBlock>().Single().Text);

        ControlHeader.SetHeaderTemplate(box, new FuncDataTemplate<string>((s, _) => new Border { Name = "Mine", Height = 30 }));
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(HeaderOf(box).GetVisualDescendants(), v => v is Border { Name: "Mine" });
        Assert.Equal(30, HeaderOf(box).Bounds.Height, 0.001);
    }

    // The wrapped specimen: two lines of the phone's, the text view 54.1667 (1.33 em twice to the grid), the box
    // 86.6667.
    [AvaloniaFact]
    public void Wrapped_Text_Takes_The_Phones_Lines()
    {
        using var stage = new Stage(box: WrappedSpecimen(), height: 220);
        var box = stage.Box;

        Assert.Equal(2, TextOf(box).TextLayout.TextLines.Count);
        Assert.Equal(WrappedTextHeight, TextOf(box).Bounds.Height, 0.001);
        Assert.Equal(WrappedHeight, box.Bounds.Height, 0.001);
    }

    // The line is 1.33 em of the box's FontSize, whatever it is, and the app's LineHeight wins.
    [AvaloniaFact]
    public void The_Line_Follows_The_FontSize_And_An_Apps_LineHeight_Wins()
    {
        using var stage = new Stage();
        var box = stage.Box;
        Assert.Equal(PhoneTextLine.FontLine * FontSize, TextOf(box).LineHeight, 0.0001);

        box.FontSize = 30;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneTextLine.FontLine * 30, box.LineHeight, 0.0001);
        Assert.Equal(PhoneTextLine.FontLine * 30, TextOf(box).LineHeight, 0.0001);

        box.LineHeight = 50;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(50, TextOf(box).LineHeight);
        Assert.Equal(50, TextOf(box).TextLayout.Height, 0.001);
    }

    // Avalonia's MaxLines measures lines of the box's LineHeight, so the phone's: two lines show two of the phone's.
    [AvaloniaFact]
    public void MaxLines_Counts_The_Phones_Lines()
    {
        var box = Specimen(text: "one\ntwo\nthree\nfour");
        box.AcceptsReturn = true;
        box.MaxLines = 2;
        using var stage = new Stage(box: box, height: 220);

        // Avalonia's measure leaves the second line's bottom 0.56 short (53.33 of 53.89: COMPARISON.md); with Selawik's
        // own line it would show 48.6 and some.
        var viewport = ScrollViewerOf(box).Viewport.Height;
        Assert.InRange(viewport, 2 * PhoneTextLine.FontLine * FontSize - 1, 2.5 * PhoneTextLine.FontLine * FontSize);
    }

    // At rest: the box and its border one colour, the box's at 0.8 over the page (204 grey in both variants: the
    // PNGs), drawn as one layer, so the border's band is no brighter where the background meets it; the page outside.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void At_Rest_The_Box_Is_Drawn_At_Eight_Tenths(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant));
        using var frame = stage.Capture();

        // One step a channel: Skia blends #BFBFBF at 0.8 over white to 203, the phone to 204 (203.8).
        for (var i = 0; i < BorderWidthPixels + 2; i++)
        {
            Near(RestPixel, Frames.PixelAt(frame, 500, BorderTopRow + i));          // the top band, and inside it
            Near(RestPixel, Frames.PixelAt(frame, BorderLeftColumn + i, 150));      // the left band
            Near(RestPixel, Frames.PixelAt(frame, 500, BorderBottomRow - i));       // the bottom band
        }

        Near(RestPixel, Frames.PixelAt(frame, 900, 195));
        Assert.Equal(Hex(Page(dark)), PixelHex(frame, 500, BorderTopRow - 1));
        Assert.Equal(Hex(Page(dark)), PixelHex(frame, BorderLeftColumn - 1, 150));
        Assert.Equal(Hex(Page(dark)), PixelHex(frame, 500, BorderBottomRow + 1));
    }

    // Focused (keyboard or a tap: Avalonia's :focus): the accent round white, at full opacity; the placeholder hidden.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Focused_Is_The_Accent_Round_White(string variant)
    {
        using var stage = new Stage(VariantOf(variant), Specimen(placeholder: Placeholder));
        var box = stage.Box;
        Focus(box);

        Assert.Equal(1, BorderOf(box).Opacity);
        Assert.Equal(Accent, ColourOf(BorderOf(box).BorderBrush));
        Assert.Equal(White, ColourOf(BorderOf(box).Background));
        Assert.Equal(0, PlaceholderOf(box).Opacity);

        using var frame = stage.Capture();
        for (var i = 0; i < BorderWidthPixels; i++)
        {
            Assert.Equal(Hex(Accent), PixelHex(frame, 500, BorderTopRow + i));
            Assert.Equal(Hex(Accent), PixelHex(frame, 500, BorderBottomRow - i));
        }

        Assert.Equal(Hex(White), PixelHex(frame, 500, BorderTopRow + BorderWidthPixels));
        Assert.Equal(Hex(White), PixelHex(frame, 900, 195));

        // Focus gone, the rest look and the placeholder come back.
        var other = new Button { VerticalAlignment = VerticalAlignment.Bottom };
        stage.Root.Children.Add(other);
        Dispatcher.UIThread.RunJobs();
        Assert.True(other.Focus());
        Dispatcher.UIThread.RunJobs();
        Assert.False(box.IsFocused);
        Assert.Equal(RestOpacity, BorderOf(box).Opacity);
        Assert.Equal(1, PlaceholderOf(box).Opacity);
    }

    // Disabled: no background, the border TextBoxDisabledBorderThemeBrush at full opacity (115 grey over black, 191 over
    // white: the PNGs); the text, the placeholder and the header dimmed.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Is_The_Border_Alone_And_Dim_Text(string variant)
    {
        var dark = variant == "Dark";
        var box = Specimen(text: Text);
        box.IsEnabled = false;
        using var stage = new Stage(VariantOf(variant), box);

        Assert.Equal(1, BorderOf(box).Opacity);
        Assert.Equal(Transparent, ColourOf(BorderOf(box).Background));
        Assert.Equal(DisabledBorder(dark), ColourOf(BorderOf(box).BorderBrush));
        Assert.Equal(DisabledText(dark), ColourOf(ScrollViewerOf(box).Foreground));
        Assert.Equal(DisabledText(dark), ColourOf(TextOf(box).GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(DisabledText(dark), ColourOf(PlaceholderOf(box).Foreground));
        Assert.Equal(DisabledBorder(dark), ColourOf(HeaderOf(box).Foreground));

        using var frame = stage.Capture();
        for (var i = 0; i < BorderWidthPixels; i++)
            Assert.Equal(Hex(DisabledBorderPixel(dark)), PixelHex(frame, 500, BorderTopRow + i));
        Assert.Equal(Hex(Page(dark)), PixelHex(frame, 500, BorderTopRow + BorderWidthPixels));
        Assert.Equal(Hex(Page(dark)), PixelHex(frame, 900, 195));
    }

    // The placeholder: PlaceholderText where the text starts, in TextBoxPlaceholderTextThemeBrush at ContentControlFontSize
    // in the phone's line (26.9458, 27.0833 on the grid), only while the box is empty.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Placeholder_Shows_While_The_Box_Is_Empty(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(VariantOf(variant), Specimen(placeholder: Placeholder));
        var box = stage.Box;
        var placeholder = PlaceholderOf(box);
        var text = placeholder.GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.True(placeholder.IsVisible);
        Assert.Equal(Placeholder, text.Text);
        Assert.Equal(PlaceholderColour(dark), ColourOf(placeholder.Foreground));
        Assert.Equal(FontSize, text.FontSize);
        Assert.Equal(PhoneTextLine.FontLine * FontSize, text.LineHeight, 0.0001);
        Assert.Equal(TextAt.X, In(text, box).X, 0.001);
        Assert.Equal(TextAt.Y, In(text, box).Y, 0.001);

        box.Text = "a";
        Dispatcher.UIThread.RunJobs();
        Assert.False(placeholder.IsVisible);

        box.Text = "";
        Dispatcher.UIThread.RunJobs();
        Assert.True(placeholder.IsVisible);
    }

    // The placeholder's size is ContentControlFontSize whatever the box's, as the phone's part sets it; its brush is
    // the box's PlaceholderForeground, an app's too, but disabled it is the Disabled state's.
    [AvaloniaFact]
    public void The_Placeholder_Keeps_Its_Size_And_Takes_An_Apps_Brush()
    {
        var mine = new SolidColorBrush(Colors.Red);
        var box = Specimen(placeholder: Placeholder);
        box.FontSize = 30;
        box.PlaceholderForeground = mine;
        using var stage = new Stage(box: box);
        var placeholder = PlaceholderOf(box);

        Assert.Equal(FontSize, placeholder.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
        Assert.Same(mine, placeholder.Foreground);

        box.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(DisabledText(true), ColourOf(placeholder.Foreground));
    }

    // Typing goes into the box through the template's presenter, and the placeholder goes.
    [AvaloniaFact]
    public void Typing_Puts_Text_In_The_Box()
    {
        using var stage = new Stage(box: Specimen(placeholder: Placeholder));
        var box = stage.Box;
        Focus(box);

        stage.Window.KeyTextInput("hello");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("hello", box.Text);
        Assert.Equal("hello", TextOf(box).Text);
        Assert.False(PlaceholderOf(box).IsVisible);
    }

    // The selection is drawn in the accent (WinRT's SelectionHighlightColor), behind the selected text.
    [AvaloniaFact]
    public void The_Selection_Is_The_Accent()
    {
        var box = Specimen(text: "MMMMMMMMMMMMMMMMMMMM");
        using var stage = new Stage(box: box);
        Focus(box);
        box.SelectAll();
        Dispatcher.UIThread.RunJobs();
        using var frame = stage.Capture();

        // Between two lines of ink the selection shows: the text view's top row, 30 down, plus a pixel.
        var x = (int)((InStage.X + TextAt.X + 2) * Scene.ReferenceScaling);
        var y = (int)((InStage.Y + TextAt.Y) * Scene.ReferenceScaling) + 1;
        Assert.Equal(Hex(Accent), PixelHex(frame, x, y));
    }

    // Text scrolled sideways is clipped inside the Padding, as the phone's ScrollContentPresenter is inset by it.
    [AvaloniaFact]
    public void Scrolled_Text_Is_Clipped_Inside_The_Padding()
    {
        var box = Specimen(text: string.Concat(Enumerable.Repeat("MMMMMMMMMM ", 10)));
        using var stage = new Stage(box: box);
        Focus(box);
        box.CaretIndex = box.Text!.Length;
        Dispatcher.UIThread.RunJobs();
        Assert.True(ScrollViewerOf(box).Offset.X > 0, "the text didn't scroll");
        using var frame = stage.Capture();

        // In the padding's 7, left of the text's start: the box's white, on every row of the line.
        var x = (int)((InStage.X + BorderThickness + 3) * Scene.ReferenceScaling);
        for (var row = (int)((InStage.Y + TextAt.Y) * Scene.ReferenceScaling); row < (InStage.Y + TextAt.Y + 26) * Scene.ReferenceScaling; row++)
            Assert.Equal(Hex(White), PixelHex(frame, x, row));
    }

    // A touch drag across long text pans it, as the phone's ContentElement pans (its scroll modes Auto): the theme's
    // ScrollViewer keeps the gesture recogniser Avalonia's ScrollViewer themes have.
    [AvaloniaFact]
    public void A_Touch_Drag_Pans_Long_Text()
    {
        var box = Specimen(text: string.Concat(Enumerable.Repeat("MMMMMMMMMM ", 10)));
        using var stage = new Stage(box: box);
        var start = stage.At(350, BorderTop + 15);

        var touch = stage.Window.TouchBegin(start);
        for (var step = 1; step <= 10; step++)
        {
            stage.Window.TouchMove(touch, start + new Point(-25 * step, 0));
            Dispatcher.UIThread.RunJobs();
        }
        stage.Window.TouchEnd(touch, start + new Point(-250, 0));
        Dispatcher.UIThread.RunJobs();

        Assert.True(ScrollViewerOf(box).Offset.X > 100, $"the text panned {ScrollViewerOf(box).Offset.X}");
    }

    // What the box passes its ScrollViewer and its presenter, as Simple's template does: Avalonia's own properties, an
    // app's values reach the parts.
    [AvaloniaFact]
    public void The_Boxs_Values_Reach_Its_Parts()
    {
        var caret = new SolidColorBrush(Colors.Red);
        var selected = new SolidColorBrush(Colors.Green);
        var box = Specimen(text: "Agjy 0123");
        ScrollViewer.SetIsScrollChainingEnabled(box, false);
        ScrollViewer.SetBringIntoViewOnFocusChange(box, false);
        ScrollViewer.SetAllowAutoHide(box, false);
        ScrollViewer.SetIsDeferredScrollingEnabled(box, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(box, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Visible);
        box.CaretBrush = caret;
        box.CaretBlinkInterval = TimeSpan.FromSeconds(2);
        box.SelectionForegroundBrush = selected;
        box.LetterSpacing = 3;
        box.PasswordChar = '*';
        box.RevealPassword = true;
        box.TextAlignment = TextAlignment.Right;
        using var stage = new Stage(box: box);
        box.CaretIndex = 4;
        box.SelectionStart = 1;
        box.SelectionEnd = 3;
        Dispatcher.UIThread.RunJobs();

        var viewer = ScrollViewerOf(box);
        Assert.False(viewer.IsScrollChainingEnabled);
        Assert.False(viewer.BringIntoViewOnFocusChange);
        Assert.False(viewer.AllowAutoHide);
        Assert.True(viewer.IsDeferredScrollingEnabled);
        Assert.Equal(ScrollBarVisibility.Disabled, viewer.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Visible, viewer.VerticalScrollBarVisibility);

        var text = TextOf(box);
        Assert.Same(caret, text.CaretBrush);
        Assert.Equal(TimeSpan.FromSeconds(2), text.CaretBlinkInterval);
        Assert.Same(selected, text.SelectionForegroundBrush);
        Assert.Equal(3, text.LetterSpacing);
        Assert.Equal('*', text.PasswordChar);
        Assert.True(text.RevealPassword);
        Assert.Equal(TextAlignment.Right, text.TextAlignment);
        Assert.Equal(1, text.SelectionStart);
        Assert.Equal(3, text.SelectionEnd);
        Assert.Equal(box.CaretIndex, text.CaretIndex);
        box.CaretIndex = 6;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(6, text.CaretIndex);
    }

    // The Focused state is the focus look: keyboard focus draws no adorner, where Avalonia would put the control's
    // FocusAdorner (left unset, Simple's under Wp81 draws a rectangle) in the window's adorner layer.
    [AvaloniaFact]
    public void Keyboard_Focus_Draws_No_Adorner()
    {
        using var stage = new Stage();
        stage.Window.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Box.IsFocused, "Tab didn't reach the TextBox");
        var layer = AdornerLayer.GetAdornerLayer(stage.Box);
        Assert.True(layer is null || layer.Children.Count == 0, $"{layer?.Children.Count} adorners drawn");
    }

    // The phone's TextBox has no PointerOver state: the pointer over it changes no pixel.
    [AvaloniaFact]
    public void The_Pointer_Over_It_Changes_Nothing()
    {
        using var stage = new Stage();
        using var rest = stage.Capture();

        stage.Window.MouseMove(stage.At(300, BorderTop + 20));
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Box.IsPointerOver);
        using var over = stage.Capture();

        Assert.True(SameFrame(rest, over), "the pointer over it changed a pixel");
    }

    // ContentElement wears the theme's own ScrollViewer theme: no scroll bar, and an app's implicit ScrollViewer theme
    // doesn't reach it.
    [AvaloniaFact]
    public void The_ScrollViewer_Is_The_Themes_Own()
    {
        var app = new Styles
        {
            Resources =
            {
                [typeof(ScrollViewer)] = new ControlTheme(typeof(ScrollViewer))
                {
                    Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ScrollViewer>((_, _) => new Border { Name = "AppsScrollViewer" })) },
                },
            },
        };
        using var stage = new Stage(themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Wp81Theme(), app]);
        var viewer = ScrollViewerOf(stage.Box);

        Assert.Same(stage.Window.FindResource("Wp81TextBoxScrollViewerTheme"), viewer.Theme);
        Assert.DoesNotContain(viewer.GetVisualDescendants(), v => v is ScrollBar || v is Border { Name: "AppsScrollViewer" });
        Assert.Contains(viewer.GetVisualDescendants(), v => v is ScrollContentPresenter);
        Assert.Equal(Padding, Part<ScrollContentPresenter>(viewer, "PART_ContentPresenter").Margin);
    }

    // Avalonia's InnerLeftContent and InnerRightContent sit inside the border either side of the text.
    [AvaloniaFact]
    public void Inner_Content_Sits_Either_Side_Of_The_Text()
    {
        var box = Specimen(text: Text);
        box.InnerLeftContent = new Border { Name = "Left", Width = 20 };
        box.InnerRightContent = new Border { Name = "Right", Width = 30 };
        using var stage = new Stage(box: box);

        var left = Part<Border>(box, "Left");
        var right = Part<Border>(box, "Right");
        Assert.Equal(BorderThickness, In(left, box).X, 0.001);
        // On the raw pixel grid: 29.5 is 70.8 raw pixels, laid out at 71.
        Assert.Equal(BorderThickness + 20 + Padding.Left, In(TextOf(box), box).X, 0.1);
        Assert.Equal(box.Bounds.Width - BorderThickness - 30, In(right, box).X, 0.1);
    }

    // The colours follow the variant: the box switches with the app.
    [AvaloniaFact]
    public void The_Colours_Follow_The_Variant()
    {
        using var stage = new Stage(ThemeVariant.Dark);
        Assert.Equal(Box(true), ColourOf(stage.Box.Background));

        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Box(false), ColourOf(stage.Box.Background));
        Assert.Equal(HeaderColour(false), ColourOf(HeaderOf(stage.Box).Foreground));
    }

    private static void Near(Color expected, Color actual)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1 &&
                    Math.Abs(expected.B - actual.B) <= 1 && actual.A == 255,
            $"{Hex(actual)} is not {Hex(expected)}, within one step a channel");
    }

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
