using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.TextBoxReadings;
using static Cornflower.Tests.Controls.PasswordBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 PasswordBox (<c>Controls/PasswordBox.axaml</c>, the <c>PasswordBox</c> key) against the phone's style and the
/// reference app's captures (<see cref="PasswordBoxReadings"/>, and <see cref="TextBoxReadings"/> for what the two share).
/// docs/controls/PasswordBox has the plan and the comparison.
/// </summary>
public class PasswordBoxTests
{
    /// <summary>
    /// The reference app's specimens as an app writes them: <c>&lt;TextBox Theme="{DynamicResource PasswordBox}" /&gt;</c>,
    /// with the header, the password and, for WinRT's <c>IsPasswordRevealButtonEnabled</c>, the
    /// <c>revealPasswordButton</c> class.
    /// </summary>
    internal static TextBox Specimen(string? password = Password, string? placeholder = null, bool header = true, bool reveal = false)
    {
        var box = new TextBox { Text = password, PlaceholderText = placeholder };
        box.Bind(StyledElement.ThemeProperty, box.GetResourceObservable("PasswordBox"));
        if (header) ControlHeader.SetHeader(box, PasswordBoxReadings.Header);
        if (reveal) box.Classes.Add("revealPasswordButton");
        return box;
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Border BorderOf(TextBox b) => Part<Border>(b, "BorderElement");

    private static ContentPresenter HeaderOf(TextBox b) => Part<ContentPresenter>(b, "HeaderContentPresenter");

    private static Grid ContentRowOf(TextBox b) => Part<Grid>(b, "ContentRow");

    private static TextPresenter TextOf(TextBox b) => Part<TextPresenter>(b, "PART_TextPresenter");

    private static ContentPresenter PlaceholderOf(TextBox b) => Part<ContentPresenter>(b, "PlaceholderTextContentPresenter");

    private static CheckBox RevealOf(TextBox b) => Part<CheckBox>(b, "RevealButton");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    private static string PixelHex(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y) => Hex(Frames.PixelAt(frame, x, y));

    // The key gives a TextBox the phone's PasswordBox template; a TextBox without it keeps the TextBox's, PasswordChar or
    // not (it is the phone's PasswordBox without the reveal button: the same parts and pixels).
    [AvaloniaFact]
    public void The_Key_Gives_A_TextBox_The_Phones_PasswordBox()
    {
        var keyed = Specimen();
        var plain = new TextBox { PasswordChar = '●', Text = Password };
        var panel = new StackPanel { Children = { keyed, plain } };
        using var scene = new Scene(panel);

        Assert.Same(scene.Window.FindResource("PasswordBox"), keyed.Theme);
        RevealOf(keyed);
        BorderOf(keyed);
        Assert.Null(plain.Theme);
        Assert.DoesNotContain(plain.GetVisualDescendants(), v => v is CheckBox);
        Assert.Contains(plain.GetVisualDescendants(), v => v is Border { Name: "BorderElement" });
    }

    // The style's setters (passwordbox.json's keys): the TextBox's values, PasswordChar the phone's black circle, one
    // line, no return.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        var box = Specimen();
        using var stage = new TextBoxTests.Stage(VariantOf(variant), box);

        Assert.Equal(PasswordChar, box.PasswordChar);
        Assert.Equal(PasswordChar, TextOf(box).PasswordChar);
        Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        Assert.False(box.AcceptsReturn);
        Assert.Equal(TextBoxReadings.MinWidth, box.MinWidth);
        Assert.Equal(TextBoxReadings.MinHeight, box.MinHeight);
        Assert.Equal(Black, ColourOf(box.Foreground));
        Assert.Equal(Box(dark), ColourOf(box.Background));
        Assert.Equal(Box(dark), ColourOf(box.BorderBrush));
        Assert.Equal(new Thickness(BorderThickness), box.BorderThickness);
        Assert.Equal(stage.Window.FindResource("PhoneFontFamilyNormal"), box.FontFamily);
        Assert.Equal(FontSize, box.FontSize);
        Assert.Equal(Padding, box.Padding);
        Assert.Equal(TextBoxReadings.Margin, box.Margin);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, box.VerticalAlignment);
        Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden, ScrollViewer.GetVerticalScrollBarVisibility(box));
        Assert.Equal(Accent, ColourOf(box.SelectionBrush));
        Assert.Equal(PlaceholderColour(dark), ColourOf(box.PlaceholderForeground));
        Assert.Equal(Cornflower.Themes.Wp81.PhoneTextLine.FontLine * FontSize, box.LineHeight, 0.0001);
        Assert.Null(box.FocusAdorner);
        Assert.Equal(RestOpacity, BorderOf(box).Opacity);

        // The header in the phone's HeaderContentPresenterStyle.
        var header = HeaderOf(box);
        Assert.Equal(HeaderFontSize, header.FontSize);
        Assert.Equal(HeaderColour(dark), ColourOf(header.Foreground));
        Assert.Equal(FontWeight.Normal, header.FontWeight);
        Assert.Equal(new Thickness(0, 0, 0, HeaderGap), header.Margin);
    }

    // Without the reveal button, the TextBox's bounds, as the dumps (password ButtonCollapsed, empty): 65.8333 with a
    // header, 39.1667 without; the text at 9.5833, 30, inside RichEditBoxTextThemeMargin.
    [AvaloniaFact]
    public void Without_The_Reveal_Button_It_Is_The_TextBoxs_Size()
    {
        var box = Specimen();
        using var stage = new TextBoxTests.Stage(box: box);

        Assert.Equal(TextBoxReadings.Height, box.Bounds.Height, 0.001);
        Assert.Equal(BorderTop, In(BorderOf(box), box).Y, 0.001);
        Assert.Equal(BorderHeight, BorderOf(box).Bounds.Height, 0.001);
        Assert.Equal(TextAt.X, In(TextOf(box), box).X, 0.001);
        Assert.Equal(TextAt.Y, In(TextOf(box), box).Y, 0.001);
        Assert.False(RevealOf(box).IsVisible);

        var bare = Specimen(header: false);
        using var other = new TextBoxTests.Stage(box: bare, height: 110);
        Assert.Equal(NoHeaderHeight, bare.Bounds.Height, 0.001);
    }

    // The reveal button (the class): the phone's layout, the dump's to the pixel grid: the box 108.75, BorderElement only
    // 31.25 tall, the text still at the top of its 34.1667 and past the border's bottom, the CheckBox 51.25 down, lifted
    // 6.5 into the row above, at the left. Shown on an empty box too (reveal-empty).
    [AvaloniaTheory]
    [InlineData(Password)]
    [InlineData(null)]
    public void The_Reveal_Button_Is_Below_The_Box_As_On_The_Phone(string? password)
    {
        var box = Specimen(password: password, reveal: true);
        using var stage = new TextBoxTests.Stage(box: box);
        var reveal = RevealOf(box);

        Assert.True(reveal.IsVisible);
        Assert.Equal(RevealHeight, box.Bounds.Height, 0.001);
        Assert.Equal(BorderTop, In(BorderOf(box), box).Y, 0.001);
        Assert.Equal(RevealBorderHeight, BorderOf(box).Bounds.Height, 0.001);
        Assert.Equal(ContentTop, In(ContentRowOf(box), box).Y, 0.001);
        Assert.Equal(ContentHeight, ContentRowOf(box).Bounds.Height, 0.001);
        Assert.Equal(0, In(reveal, box).X, 0.001);
        Assert.InRange(reveal.Bounds.Width, 160, 200);                        // its own width, at the left: 168.3333
        Assert.Equal(RevealTop, In(reveal, box).Y, 0.001);
        Assert.Equal(RevealButtonHeight, reveal.Bounds.Height, 0.001);
        Assert.Equal(RevealMargin, reveal.Margin);
    }

    // The border's short row is the stage's doing: the reference app's stage leaves the box 109 (79 with no header), and
    // the Grid gives its star row what the header and the CheckBox leave, on the phone and here alike. With no header,
    // on its 110 stage, the box is 78.75 and the border 27.9167, the dump's. With room (in a StackPanel), the border is
    // the TextBox's 39.1667 and the CheckBox 6.5 into its row.
    [AvaloniaFact]
    public void The_Border_Takes_What_The_Header_And_The_Reveal_Button_Leave()
    {
        var box = Specimen(header: false, reveal: true);
        using (var stage = new TextBoxTests.Stage(box: box, height: 110))
        {
            Assert.Equal(RevealNoHeaderHeight, box.Bounds.Height, 0.001);
            Assert.Equal(RevealNoHeaderBorderHeight, BorderOf(box).Bounds.Height, 0.001);
            Assert.Equal(RevealNoHeaderTop, In(RevealOf(box), box).Y, 0.001);
        }

        var roomy = Specimen(reveal: true);
        using var scene = new Scene(new StackPanel { Children = { roomy } });
        Assert.Equal(BorderHeight, BorderOf(roomy).Bounds.Height, 0.001);
        Assert.Equal(BorderTop + BorderHeight + RevealMargin.Top, In(RevealOf(roomy), roomy).Y, 0.2);   // on the raw pixel grid
    }

    // The RevealButton is Wp81's CheckBox (#31) with the phone's text, in TextBoxForegroundHeaderThemeBrush, not a tab
    // stop; checking it is Avalonia's RevealPassword, both ways.
    [AvaloniaFact]
    public void The_Reveal_Button_Is_The_Phones_CheckBox_And_Reveals_The_Password()
    {
        var box = Specimen(reveal: true);
        using var stage = new TextBoxTests.Stage(box: box);
        var reveal = RevealOf(box);

        Assert.Null(reveal.Theme);                                         // Wp81's CheckBox theme, by type
        Assert.True(Tilt.GetIsEnabled(reveal));
        Assert.Contains(reveal.GetVisualDescendants(), v => v is Border { Name: "CheckBackground" });
        Assert.Equal(RevealText, reveal.Content);
        Assert.Equal(White, ColourOf(reveal.Foreground));
        Assert.False(reveal.IsTabStop);
        Assert.False(reveal.IsChecked);
        Assert.False(box.RevealPassword);

        reveal.SetCurrentValue(Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty, true);    // as a tap sets it
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.RevealPassword);
        Assert.True(TextOf(box).RevealPassword);

        box.RevealPassword = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(reveal.IsChecked);
    }

    // The RevealButton's Foreground is the template's key, over the CheckBox theme's own: an app's value for the key
    // reaches it.
    [AvaloniaFact]
    public void The_Reveal_Buttons_Foreground_Is_The_Templates_Key()
    {
        var mine = new SolidColorBrush(Colors.Red);
        var box = Specimen(reveal: true);
        using var stage = new TextBoxTests.Stage(box: box);
        stage.Window.Resources["TextBoxForegroundHeaderThemeBrush"] = mine;
        Dispatcher.UIThread.RunJobs();

        Assert.Same(mine, RevealOf(box).Foreground);
    }

    // A tap on the CheckBox checks it and shows the password.
    [AvaloniaFact]
    public void A_Tap_On_Show_Password_Shows_It()
    {
        var box = Specimen(reveal: true);
        using var stage = new TextBoxTests.Stage(box: box);
        var square = Part<Border>(RevealOf(box), "CheckBackground");
        var at = square.TranslatePoint(new Point(square.Bounds.Width / 2, square.Bounds.Height / 2), stage.Window)!.Value;

        stage.Window.MouseDown(at, MouseButton.Left);
        stage.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(box.RevealPassword);
    }

    // Typing is masked with the phone's circle.
    [AvaloniaFact]
    public void Typing_Is_Masked()
    {
        var box = Specimen(password: null);
        using var stage = new TextBoxTests.Stage(box: box);
        Assert.True(box.Focus());
        stage.Window.KeyTextInput("abc");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("abc", box.Text);
        Assert.Equal(PasswordChar, TextOf(box).PasswordChar);
        Assert.False(TextOf(box).RevealPassword);
    }

    // The pixels, as the TextBox's: at rest 204 grey on every edge, as one colour; focused the accent round white;
    // disabled the border alone. With the reveal button the border ends at row 190, as the phone's.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Box_Is_Drawn_As_The_Phones(string variant)
    {
        var dark = variant == "Dark";
        var box = Specimen(reveal: true);
        using var stage = new TextBoxTests.Stage(VariantOf(variant), box);
        using (var rest = stage.Capture())
        {
            Near(RestPixel, Frames.PixelAt(rest, 500, BorderTopRow));
            Near(RestPixel, Frames.PixelAt(rest, 500, RevealBorderBottomRow));
            Assert.Equal(Hex(Page(dark)), PixelHex(rest, 500, RevealBorderBottomRow + 1));
        }

        Assert.True(box.Focus());
        Dispatcher.UIThread.RunJobs();
        using (var focused = stage.Capture())
        {
            Assert.Equal(Hex(Accent), PixelHex(focused, 500, BorderTopRow));
            Assert.Equal(Hex(Accent), PixelHex(focused, 500, RevealBorderBottomRow));
            Assert.Equal(Hex(White), PixelHex(focused, 900, BorderTopRow + BorderWidthPixels + 2));
        }

        box.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        using var disabled = stage.Capture();
        Assert.Equal(Hex(DisabledBorderPixel(dark)), PixelHex(disabled, 500, BorderTopRow));
        Assert.Equal(Hex(Page(dark)), PixelHex(disabled, 900, BorderTopRow + BorderWidthPixels + 2));
        Assert.Equal(DisabledText(dark), ColourOf(Part<ScrollViewer>(box, "PART_ScrollViewer").Foreground));
        Assert.Equal(DisabledBorder(dark), ColourOf(HeaderOf(box).Foreground));
    }

    // The phone's PasswordBox has a PointerOver state, empty, so Normal's 0.8 ends under a pointer: the box drawn whole.
    [AvaloniaFact]
    public void The_Pointer_Over_It_Draws_The_Box_Whole()
    {
        var box = Specimen();
        using var stage = new TextBoxTests.Stage(box: box);
        stage.Window.MouseMove(stage.At(300, BorderTop + 20));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, BorderOf(box).Opacity);
        using var frame = stage.Capture();
        Assert.Equal(Hex(White), PixelHex(frame, 900, 195));
    }

    // The placeholder is the box's size: the phone's PasswordBox sets no FontSize on its part, unlike the TextBox's.
    [AvaloniaFact]
    public void The_Placeholder_Is_The_Boxs_Size()
    {
        var box = Specimen(password: null, placeholder: Placeholder);
        using var stage = new TextBoxTests.Stage(box: box);
        var text = PlaceholderOf(box).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(FontSize, text.FontSize);
        Assert.Equal(TextAt.Y, In(text, box).Y, 0.001);

        box.FontSize = 30;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(30, text.FontSize);
        Assert.Equal(Cornflower.Themes.Wp81.PhoneTextLine.FontLine * 30, text.LineHeight, 0.0001);

        box.Text = "x";
        Dispatcher.UIThread.RunJobs();
        Assert.False(PlaceholderOf(box).IsVisible);
    }

    // Disabled, the RevealButton is disabled with the box: Avalonia's and WinRT's IsEnabled both reach a template's
    // parts. (The captures force the Disabled state alone, so their CheckBox looks enabled.)
    [AvaloniaFact]
    public void Disabled_The_Reveal_Button_Is_Too()
    {
        var box = Specimen(reveal: true);
        box.IsEnabled = false;
        using var stage = new TextBoxTests.Stage(box: box);

        Assert.False(RevealOf(box).IsEffectivelyEnabled);
    }

    // The box's text values are its text's: not the header's, the placeholder's or the RevealButton's text.
    [AvaloniaFact]
    public void The_Boxs_Text_Values_Stay_In_The_Text()
    {
        var box = Specimen(password: null, placeholder: Placeholder, reveal: true);
        box.InnerRightContent = new TextBlock { Name = "RightText", Text = "x" };
        box.TextAlignment = TextAlignment.Center;
        box.TextWrapping = TextWrapping.Wrap;
        box.LetterSpacing = 3;
        using var stage = new TextBoxTests.Stage(box: box);
        Assert.True(double.IsNaN(Part<TextBlock>(box, "RightText").LineHeight), "the box's line reached the inner content");

        var texts = new[]
        {
            HeaderOf(box).GetVisualDescendants().OfType<TextBlock>().Single(),
            PlaceholderOf(box).GetVisualDescendants().OfType<TextBlock>().Single(),
            RevealOf(box).GetVisualDescendants().OfType<TextBlock>().Single(),
            Part<TextBlock>(box, "RightText"),
        };
        foreach (var text in texts)
        {
            Assert.Equal(TextAlignment.Start, text.TextAlignment);
            Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
            Assert.Equal(0, text.LetterSpacing);
        }

        Assert.Equal(Cornflower.Themes.Wp81.PhoneTextLine.FontLine * CheckBoxReadings.FontSize, texts[2].LineHeight, 0.0001);
        Assert.Equal(TextAlignment.Center, TextOf(box).TextAlignment);
        Assert.Equal(3, TextOf(box).LetterSpacing);
    }

    // The TextBox theme's pieces: its ScrollViewer part's theme.
    [AvaloniaFact]
    public void The_ScrollViewer_Part_Is_The_TextBoxs()
    {
        var box = Specimen();
        using var stage = new TextBoxTests.Stage(box: box);

        Assert.Same(stage.Window.FindResource("Wp81TextBoxScrollViewerTheme"), Part<ScrollViewer>(box, "PART_ScrollViewer").Theme);
        Assert.Equal(new Thickness(BorderThickness), ContentRowOf(box).Margin);     // RichEditBoxTextThemeMargin
    }

    private static void Near(Color expected, Color actual)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1 &&
                    Math.Abs(expected.B - actual.B) <= 1 && actual.A == 255,
            $"{Hex(actual)} is not {Hex(expected)}, within one step a channel");
    }
}
