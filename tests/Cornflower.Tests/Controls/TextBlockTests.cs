using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Tests.Performance;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 text styles, the type ramp (<c>Controls/TextBlock.axaml</c>), against the reference app's captures
/// (<see cref="TextStyleReadings"/>), and the hooks an extension pack overrides them by (ADR 0004).
/// docs/controls/TextBlock has the plan and the comparison.
/// </summary>
public class TextBlockTests
{
    private const string Specimen = "Agjy 0123 cornflower";
    private const double Pixel = 1 / Scene.ReferenceScaling;

    /// <summary>A text style on the reference profile, placed as the reference app places it.</summary>
    private sealed class Specimens : IDisposable
    {
        private readonly Scene _scene;

        public TextBlock Block { get; }

        public Specimens(string key, ThemeVariant? variant = null, string text = Specimen, double width = double.NaN)
        {
            Block = new TextBlock
            {
                Text = text,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            // The reference app's stage: the page's background and foreground, and a slot 19 in and 12 down.
            var stage = new Border { Child = new StackPanel { Margin = new Thickness(19, 12, 19, 0), Children = { Block } } };
            stage.Bind(Border.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            stage.Bind(TextElement.ForegroundProperty, stage.GetResourceObservable("PhoneForegroundBrush"));
            _scene = new Scene(stage, variant);
            Block.Theme = (ControlTheme)_scene.Window.FindResource(key)!;
            Dispatcher.UIThread.RunJobs();
        }

        public Window Window => _scene.Window;

        /// <summary>The <c>TextBlock</c>'s top left in the frame, in DIPs.</summary>
        public Point Origin => Block.TranslatePoint(default, _scene.Window)!.Value;

        public Ink Capture() => new(_scene.Capture(), Origin);

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>A frame's pixels, read once, measured from a <c>TextBlock</c>'s top left.</summary>
    private sealed class Ink
    {
        private readonly byte[] _grey;
        private readonly int _width, _height, _x0, _y0;

        public Ink(WriteableBitmap frame, Point origin)
        {
            using (frame)
            using (var buffer = frame.Lock())
            {
                _width = buffer.Size.Width;
                _height = buffer.Size.Height;
                var bytes = new byte[buffer.RowBytes * _height];
                Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
                _grey = new byte[_width * _height];
                for (var y = 0; y < _height; y++)
                for (var x = 0; x < _width; x++)
                    _grey[y * _width + x] = bytes[y * buffer.RowBytes + x * 4 + 1];  // G, the same in RGBA and BGRA
                Background = _grey[2 * _width + 2];
            }

            _x0 = (int)Math.Round(origin.X * Scene.ReferenceScaling);
            _y0 = (int)Math.Round(origin.Y * Scene.ReferenceScaling);
        }

        public byte Background { get; }

        private bool IsInk(int x, int y) => Math.Abs(_grey[y * _width + x] - Background) > 128;

        private IEnumerable<int> Rows => Enumerable.Range(Math.Max(0, _y0 - 48), Math.Min(_height, _y0 + 300) - Math.Max(0, _y0 - 48));

        /// <summary>The columns of the first glyph's ink.</summary>
        private List<int> FirstGlyph()
        {
            var columns = new List<int>();
            for (var x = Math.Max(0, _x0 - 8); x < _width; x++)
            {
                if (Rows.Any(y => IsInk(x, y)))
                    columns.Add(x);
                else if (columns.Count > 0)
                    break;
            }

            return columns;
        }

        /// <summary>The bottom of the first glyph's ink below the origin, in DIPs.</summary>
        public double FirstGlyphBottom()
        {
            var columns = FirstGlyph();
            return (Rows.Last(y => columns.Any(x => IsInk(x, y))) + 1 - _y0) * Pixel;
        }

        /// <summary>The bottom of all the ink below the origin, in DIPs.</summary>
        public double Bottom() => (Rows.Last(y => Enumerable.Range(0, _width).Any(x => IsInk(x, y))) + 1 - _y0) * Pixel;

        /// <summary>The ink pixel farthest from the background: the text's colour, where a stem covers a pixel.</summary>
        public byte Extreme() => Rows.SelectMany(y => Enumerable.Range(0, _width).Select(x => _grey[y * _width + x]))
            .MaxBy(g => Math.Abs(g - Background));
    }

    private static Thickness Padding(TextBlock block) => block.Padding;

    /// <summary>The text's height before layout rounds it to the pixel grid.</summary>
    private static double Unrounded(TextBlock block) => block.Padding.Top + block.TextLayout.Height + block.Padding.Bottom;

    private static double FirstBaseline(TextBlock block) => block.Padding.Top + block.TextLayout.TextLines[0].Baseline;

    // ── What each style sets ─────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [MemberData(nameof(TextStyleReadings.Keys), MemberType = typeof(TextStyleReadings))]
    public void Each_Style_Sets_What_The_Phone_Read(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key);
        var block = specimen.Block;

        Assert.Equal(phone.FontSize, block.FontSize);
        Assert.Same(specimen.Window.FindResource(phone.FamilyKey), block.FontFamily);
        Assert.Equal(phone.Weight, block.FontWeight);
        Assert.Equal(phone.Wrapping, block.TextWrapping);
        Assert.Equal(TextTrimming.None, block.TextTrimming);
        Assert.Equal(phone.CharacterSpacing * phone.FontSize / 1000, block.LetterSpacing, precision: 9);
        Assert.Equal(phone.BaselineToBaseline ? phone.LineHeight : Math.Max(phone.LineHeight, PhoneLine.FontLine * phone.FontSize), block.LineHeight, precision: 9);
    }

    // The phone's Semilight family has one face, so GroupHeader's Normal draws it too; Title's SemiBold is Semibold.
    [AvaloniaTheory]
    [MemberData(nameof(TextStyleReadings.Keys), MemberType = typeof(TextStyleReadings))]
    public void Each_Style_Draws_The_Face_The_Phone_Drew(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key);

        var face = specimen.Block.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.GlyphTypeface;
        var expected = phone.FamilyKey == "PhoneFontFamilySemiLight" ? ("Selawik Semilight", FontWeight.Light)
            : phone.Weight == FontWeight.SemiBold ? ("Selawik Semibold", FontWeight.SemiBold)
            : ("Selawik", FontWeight.Normal);
        Assert.Equal(expected, (face.FamilyName, face.Weight));
    }

    // ── The phone's line ─────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [MemberData(nameof(TextStyleReadings.Keys), MemberType = typeof(TextStyleReadings))]
    public void Each_Style_Is_As_Tall_As_On_The_Phone(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key);
        var block = specimen.Block;

        Assert.Equal(phone.Height, Unrounded(block), 0.01);
        // Layout rounds the padding and the size up to the pixel grid, where the phone rounds to the nearest (ADR 0003).
        Assert.InRange(block.Bounds.Height - phone.Height, -Pixel / 2, 1.25 * Pixel);
    }

    [AvaloniaTheory]
    [MemberData(nameof(TextStyleReadings.Keys), MemberType = typeof(TextStyleReadings))]
    public void Each_Styles_First_Baseline_Is_The_Phones(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key);

        var expected = PhoneLine.FirstBaseline * phone.FontSize;
        if (phone.BaselineToBaseline)
            Assert.Equal(expected, FirstBaseline(specimen.Block), 0.001);
        else
            Assert.Equal(0.025, (expected - FirstBaseline(specimen.Block)) / phone.FontSize, 0.001);  // ADR 0003's recorded deviation
    }

    [AvaloniaTheory]
    [MemberData(nameof(TextStyleReadings.CapturedKeys), MemberType = typeof(TextStyleReadings))]
    public void Each_Style_Draws_Its_Baseline_Where_The_Phone_Does(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key);
        var ink = specimen.Capture();

        Assert.InRange(ink.FirstGlyphBottom() - phone.ABottom, -1.01 * Pixel, 1.01 * Pixel);
    }

    // On the phone, a line's descenders draw below the TextBlock where LineHeight leaves them no room:
    // Header's "gjy" reach 75.417 DIP below its top, 2.5 past its 72.899 (dark__TextBlock__HeaderTextBlockStyle__base.png).
    [AvaloniaFact]
    public void Descenders_Draw_Below_A_Short_Line_As_On_The_Phone()
    {
        using var specimen = new Specimens("HeaderTextBlockStyle");
        var ink = specimen.Capture();

        Assert.False(specimen.Block.ClipToBounds);
        Assert.InRange(ink.Bottom() - 75.417, -1.01 * Pixel, 1.01 * Pixel);
        Assert.True(ink.Bottom() > specimen.Block.Bounds.Height + 2);
    }

    // BaselineToBaseline: each baseline LineHeight below the one before (the styles' LineStackingStrategy).
    [AvaloniaTheory]
    [InlineData("BodyTextBlockStyle")]
    [InlineData("GroupHeaderTextBlockStyle")]
    [InlineData("SubheaderTextBlockStyle")]
    public void Wrapped_Lines_Are_LineHeight_Apart(string key)
    {
        var phone = TextStyleReadings.Get(key);
        using var specimen = new Specimens(key, text: "cornflower cornflower cornflower", width: 160);
        var lines = specimen.Block.TextLayout.TextLines;

        Assert.Equal(3, lines.Count);
        Assert.All(lines, line => Assert.Equal(phone.LineHeight, line.Height, 9));
        Assert.Equal(PhoneLine.Height(phone.FontSize, phone.LineHeight, 3), Unrounded(specimen.Block), 0.01);
    }

    // WinRT's WrapWholeWords breaks only between words: a word longer than the line runs past it.
    [AvaloniaFact]
    public void A_Word_Longer_Than_The_Line_Is_Not_Broken()
    {
        using var specimen = new Specimens("BodyTextBlockStyle", text: "cornflowercornflower cornflower", width: 100);

        Assert.Equal(2, specimen.Block.TextLayout.TextLines.Count);
    }

    // ── Colour, dark and light ───────────────────────────────────────────────────────────────

    public static TheoryData<string, string> CapturedKeysByVariant()
    {
        var data = new TheoryData<string, string>();
        foreach (var key in TextStyleReadings.All.Where(r => r.Captured).Select(r => r.Key))
        {
            data.Add(key, "Dark");
            data.Add(key, "Light");
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(CapturedKeysByVariant))]
    public void Each_Style_Draws_In_The_Phones_Colour(string key, string variant)
    {
        var phone = TextStyleReadings.Get(key);
        var dark = variant == "Dark";
        using var specimen = new Specimens(key, dark ? ThemeVariant.Dark : ThemeVariant.Light);

        var ink = specimen.Capture();

        Assert.Equal(dark ? 0 : 255, ink.Background);
        Assert.Equal(Color.FromUInt32(dark ? phone.Foreground.Dark : phone.Foreground.Light), ((ISolidColorBrush)specimen.Block.Foreground!).Color);
        // At 16 DIP, Selawik's stems cover no whole pixel, so the farthest ink falls up to 3 short of the colour.
        var expected = dark ? phone.DarkInk : phone.LightInk;
        var slack = phone.FontSize < 18 ? 3 : 1;
        Assert.InRange(ink.Extreme(), expected - (dark ? slack : 1), expected + (dark ? 1 : slack));
        if (phone.Opacity is { } opacity)
            Assert.Equal(dark ? opacity.Dark : opacity.Light, specimen.Block.Opacity);
    }

    [AvaloniaFact]
    public void The_Colour_Follows_A_Change_Of_Variant()
    {
        using var specimen = new Specimens("ControlHeaderTextBlockStyle");

        specimen.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Color.FromUInt32(0x73000000), ((ISolidColorBrush)specimen.Block.Foreground!).Color);
        Assert.InRange(specimen.Capture().Extreme(), 139, 143);
    }

    // ── A TextBlock's own values ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Local_FontSize_Keeps_The_Phones_Line()
    {
        using var specimen = new Specimens("BodyTextBlockStyle");
        specimen.Block.FontSize = 24;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(PhoneLine.FirstBaseline * 24, FirstBaseline(specimen.Block), 0.001);
        Assert.Equal(PhoneLine.Height(24, 24), Unrounded(specimen.Block), 0.01);
    }

    [AvaloniaFact]
    public void A_Local_FontSize_Keeps_The_Letter_Spacing_And_The_MaxHeight_Line()
    {
        using var spaced = new Specimens("GroupHeaderTextBlockStyle");
        using var listed = new Specimens("ListViewItemSubheaderTextBlockStyle");
        spaced.Block.FontSize = 40;
        listed.Block.FontSize = 30;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(-3 * 40 / 1000.0, spaced.Block.LetterSpacing, 9);
        Assert.Equal(PhoneLine.FontLine * 30, listed.Block.LineHeight, 9);
    }

    [AvaloniaFact]
    public void A_Value_The_App_Sets_Wins_Over_The_Styles()
    {
        using var specimen = new Specimens("GroupHeaderTextBlockStyle");
        specimen.Block.Padding = new Thickness(3);
        specimen.Block.LetterSpacing = 1;
        specimen.Block.FontSize = 30;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Thickness(3), Padding(specimen.Block));
        Assert.Equal(1, specimen.Block.LetterSpacing);

        specimen.Block.ClearValue(TextBlock.PaddingProperty);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneLine.FirstBaseline * 30, FirstBaseline(specimen.Block), 0.001);
    }

    [AvaloniaFact]
    public void Taking_The_Style_Away_Takes_Its_Values_Away()
    {
        using var spaced = new Specimens("GroupHeaderTextBlockStyle");
        using var listed = new Specimens("ListViewItemSubheaderTextBlockStyle");

        spaced.Block.Theme = null;
        listed.Block.Theme = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(default, Padding(spaced.Block));
        Assert.Equal(0, spaced.Block.LetterSpacing);
        Assert.True(double.IsNaN(listed.Block.LineHeight));
    }

    // ── The hooks (ADR 0004) ─────────────────────────────────────────────────────────────────

    /// <summary>Every resource a style reads, with the property it sets, through its BasedOn.</summary>
    private static IEnumerable<(string Style, AvaloniaProperty Property, string Key)> Hooks()
    {
        var theme = new Wp81Theme();
        foreach (var style in TextStyleReadings.All.Select(r => r.Key))
        {
            theme.TryGetResource(style, null, out var found);
            var chain = new List<ControlTheme>();
            for (var t = (ControlTheme?)found; t is not null; t = t.BasedOn)
                chain.Insert(0, t);
            var setters = new Dictionary<AvaloniaProperty, Setter>();
            foreach (var setter in chain.SelectMany(t => t.Setters).OfType<Setter>())
                setters[setter.Property!] = setter;
            foreach (var setter in setters.Values)
            {
                if (setter.Value is DynamicResourceExtension { ResourceKey: string key })
                    yield return (style, setter.Property!, key);
            }
        }
    }

    // A StaticResource is fixed from the theme's own tokens when it loads, so it can't be a hook: only fixed
    // values that aren't resources (enums, a flag) may be set without a DynamicResource.
    [AvaloniaFact]
    public void Every_Resource_A_Style_Reads_Is_Reached_By_DynamicResource()
    {
        var literal = new AvaloniaProperty[]
        {
            TextBlock.TextTrimmingProperty, TextBlock.TextWrappingProperty, TextBlock.FontWeightProperty,
            Visual.ClipToBoundsProperty, PhoneTextLine.BaselineToBaselineProperty,
        };
        var theme = new Wp81Theme();
        foreach (var style in TextStyleReadings.All.Select(r => r.Key))
        {
            theme.TryGetResource(style, null, out var found);
            foreach (var setter in ((ControlTheme)found!).Setters.OfType<Setter>())
            {
                Assert.True(setter.Value is DynamicResourceExtension || literal.Contains(setter.Property!),
                    $"{style} sets {setter.Property!.Name} to {setter.Value}, not by DynamicResource");
            }
        }
    }

    public static TheoryData<string, string> HookCases() => [.. Hooks().Select(h => (h.Style, h.Key))];

    /// <summary>Another value of the same kind, for a key to be overridden with.</summary>
    private static object Other(object value) => value switch
    {
        double d => d / 2,
        int i => i + 20,
        Thickness => new Thickness(5),
        FontFamily f => f.Name.Contains("Light", StringComparison.Ordinal) ? FontFamily.Parse("avares://Cornflower.Themes.Wp81/Fonts/Selawik#Selawik") : FontFamily.Parse("avares://Cornflower.Themes.Wp81/Fonts/Selawik#Selawik Light"),
        ISolidColorBrush b => new SolidColorBrush(b.Color, b.Opacity / 2),
        _ => throw new NotSupportedException(value.GetType().Name),
    };

    // An extension pack puts its values in a window's Resources: here, in a window wider than the phone.
    [AvaloniaTheory]
    [MemberData(nameof(HookCases))]
    public void A_Key_In_A_Wider_Windows_Resources_Changes_The_Style(string style, string key)
    {
        var property = Hooks().Single(h => h.Style == style && h.Key == key).Property;
        var block = new TextBlock { Text = Specimen };
        using var scene = new Scene(block, width: 1280, height: 800, scaling: 1);
        block.Theme = (ControlTheme)scene.Window.FindResource(style)!;
        Dispatcher.UIThread.RunJobs();
        var before = block.GetValue(property)!;

        var other = Other(scene.Window.FindResource(key)!);
        scene.Window.Resources[key] = other;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(other, block.GetValue(property));
        Assert.NotEqual(before, block.GetValue(property));
    }

    [AvaloniaFact]
    public void Every_Key_In_TextStyles_Is_Read_By_A_Style()
    {
        var file = new Wp81Theme().Resources.MergedDictionaries.OfType<ResourceInclude>()
            .Single(i => i.Source!.AbsolutePath.EndsWith("Tokens/TextStyles.axaml", StringComparison.Ordinal));
        var keys = ((ResourceDictionary)file.Loaded).Keys.Cast<string>().Order();

        Assert.Equal(keys, Hooks().Select(h => h.Key).Where(keys.Contains).Distinct().Order());
        Assert.Equal(19, keys.Count());
    }

    // Two windows of one app: the wide one gets a pack's size, and the phone keeps the phone's.
    [AvaloniaFact]
    public void A_Wider_Windows_Sizes_Leave_Another_Window_Alone()
    {
        var wide = new TextBlock { Text = Specimen };
        using var desktop = new Scene(wide, width: 1280, height: 800, scaling: 1);
        desktop.Window.Resources["TextStyleLargeFontSize"] = 24.0;
        wide.Theme = (ControlTheme)desktop.Window.FindResource("BodyTextBlockStyle")!;
        using var phone = new Specimens("BodyTextBlockStyle");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(24, wide.FontSize);
        Assert.Equal(PhoneLine.FirstBaseline * 24, FirstBaseline(wide), 0.001);
        Assert.Equal(PhoneLine.Height(24, 24), Unrounded(wide), 0.01);
        Assert.Equal(18.14, phone.Block.FontSize);
    }

    // No fixed phone width (ADR 0004): no style sets one, and wrapped text takes the width it is given.
    [AvaloniaFact]
    public void No_Style_Sets_A_Width_And_Text_Takes_A_Wider_Windows_Width()
    {
        Assert.DoesNotContain(Hooks(), h => h.Property == Layoutable.WidthProperty || h.Property == Layoutable.MinWidthProperty || h.Property == Layoutable.MaxWidthProperty);
        var theme = new Wp81Theme();
        foreach (var style in TextStyleReadings.All.Select(r => r.Key))
        {
            theme.TryGetResource(style, null, out var found);
            for (var t = (ControlTheme?)found; t is not null; t = t.BasedOn)
            {
                Assert.DoesNotContain(t.Setters.OfType<Setter>(), s =>
                    s.Property == Layoutable.WidthProperty || s.Property == Layoutable.MinWidthProperty || s.Property == Layoutable.MaxWidthProperty);
            }
        }

        var text = string.Join(' ', Enumerable.Repeat("cornflower", 40));
        var wide = new TextBlock { Text = text };
        using var desktop = new Scene(new Border { Padding = new Thickness(19, 0), Child = wide }, width: 1280, height: 800, scaling: 1);
        wide.Theme = (ControlTheme)desktop.Window.FindResource("BodyTextBlockStyle")!;
        using var phone = new Specimens("BodyTextBlockStyle", text: text, width: 412);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1280 - 2 * 19, wide.Bounds.Width);
        Assert.All(wide.TextLayout.TextLines.SkipLast(1), line => Assert.InRange(line.WidthIncludingTrailingWhitespace, 1100, 1242));
        Assert.True(wide.TextLayout.TextLines.Count * 2 < phone.Block.TextLayout.TextLines.Count);
    }

    // ── What the benchmark compares ──────────────────────────────────────────────────────────

    // TypeRampCostBenchmark times the theme against the same values fixed: they have to draw the same.
    [AvaloniaTheory]
    [InlineData("BodyTextBlockStyle")]
    [InlineData("GroupHeaderTextBlockStyle")]
    public void The_Benchmarks_Ways_Draw_The_Same_Text(string key)
    {
        new Wp81Theme().TryGetResource(key, null, out var found);
        var ways = TypeRampCostBenchmark.Variants((ControlTheme)found!);
        Assert.Equal(TypeRampCostBenchmark.Ways.Length, ways.Length);

        var drawn = ways.Skip(1).Select(way =>
        {
            var block = new TextBlock { Text = Specimen, Theme = way };
            using var scene = new Scene(block);
            return (block.FontSize, block.FontFamily, block.LineHeight, block.LetterSpacing, block.Padding, block.Bounds, ((ISolidColorBrush)block.Foreground!).Color);
        }).ToList();

        Assert.All(drawn, d => Assert.Equal(drawn[^1], d));
    }
}
