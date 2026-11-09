using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Fonts;

/// <summary>
/// A <c>TextBlock</c> whose <c>FontFamily</c> comes from one of the theme's keys, as a template's
/// <c>{DynamicResource}</c> would give it, shown white on black in a window that has the theme.
/// The theme goes in the window's styles, not the application's, so these tests can run in parallel.
/// </summary>
internal sealed class ThemeText : IDisposable
{
    private readonly Window _window;

    public TextBlock Block { get; }

    public ThemeText(string fontKey, string text, double fontSize, FontWeight weight = FontWeight.Normal, double lineHeight = double.NaN)
    {
        Block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            LineHeight = lineHeight,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Block.Bind(TextBlock.FontFamilyProperty, Block.GetResourceObservable(fontKey));

        _window = new Window { Width = 800, Height = 200, Background = Brushes.Black };
        _window.Styles.Add(new Wp81Theme());
        _window.Content = Block;
        _window.Show();
        Frames.DrawFirstFrame(_window);

        Assert.IsType<FontFamily>(Block.FontFamily);
        Assert.Contains("avares://Cornflower.Themes.Wp81/Fonts", Block.FontFamily.Key?.Source.OriginalString);
    }

    public TextLine Line => Assert.Single(Block.TextLayout.TextLines);

    /// <summary>The face the text was shaped and drawn with.</summary>
    public GlyphTypeface Face => Line.TextRuns[0].Properties!.Typeface.GlyphTypeface;

    /// <summary>The rendered frame's bytes.</summary>
    public byte[] Pixels()
    {
        ManualRenderTimer.ForceTick();
        Dispatcher.UIThread.RunJobs();
        using var frame = _window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame");
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        return bytes;
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
