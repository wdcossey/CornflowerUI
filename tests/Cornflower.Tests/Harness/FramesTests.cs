using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Cornflower.Tests.Harness;

public class FramesTests
{
    // Skia's frames are RGBA. Red and blue read the wrong way round would still pass on a grey.
    [AvaloniaFact]
    public void PixelAt_Reads_A_Skia_Frame_In_Its_Own_Order()
    {
        using var scene = new Scene(new Border { Background = new SolidColorBrush(Color.FromRgb(0xE5, 0x14, 0x00)) });
        using var frame = scene.Capture();

        Assert.Equal(PixelFormat.Rgba8888, frame.Format);
        Assert.Equal(Color.FromRgb(0xE5, 0x14, 0x00), Frames.PixelAt(frame, 540, 960));
    }

    [AvaloniaFact]
    public void PixelAt_Reads_A_Bgra_Frame_In_Its_Own_Order()
    {
        using var frame = Bitmap(PixelFormat.Bgra8888, [0x11, 0x22, 0x33, 0xFF]);

        Assert.Equal(Color.FromArgb(0xFF, 0x33, 0x22, 0x11), Frames.PixelAt(frame, 0, 0));
    }

    [AvaloniaFact]
    public void PixelAt_Refuses_A_Format_It_Does_Not_Read()
    {
        using var frame = Bitmap(PixelFormat.Rgb565, [0x00, 0xF8, 0x00, 0xF8]);

        Assert.Throws<NotSupportedException>(() => Frames.PixelAt(frame, 0, 0));
    }

    // Outside the frame would read another row's pixel, or memory that isn't the frame's.
    [AvaloniaTheory]
    [InlineData(1080, 0)]
    [InlineData(-1, 1)]
    public void PixelAt_Refuses_A_Point_Outside_The_Frame(int x, int y)
    {
        using var scene = new Scene(new Border());
        using var frame = scene.Capture();

        Assert.Throws<ArgumentOutOfRangeException>(() => Frames.PixelAt(frame, x, y));
    }

    // The control tests compare a state's frame with the resting one: a change anywhere must count.
    [AvaloniaFact]
    public void SameAs_Holds_Two_Captures_Of_One_Scene()
    {
        using var scene = new Scene(new Border { Background = Brushes.Teal });
        using var first = scene.Capture();
        using var second = scene.Capture();

        Assert.True(Frames.SameAs(first, second));
    }

    [AvaloniaFact]
    public void SameAs_Finds_One_Pixel_Changed_In_The_Last_Row()
    {
        using var a = Bitmap(PixelFormat.Rgba8888, [0x11, 0x22, 0x33, 0xFF]);
        using var b = Bitmap(PixelFormat.Rgba8888, [0x11, 0x22, 0x33, 0xFF]);
        Assert.True(Frames.SameAs(a, b));
        using (var buffer = b.Lock())
            System.Runtime.InteropServices.Marshal.WriteByte(buffer.Address, buffer.RowBytes + 4 + 2, 0x01);

        Assert.False(Frames.SameAs(a, b));
    }

    [AvaloniaFact]
    public void SameAs_Tells_Sizes_And_Orders_Apart()
    {
        using var rgba = Bitmap(PixelFormat.Rgba8888, [0x11, 0x22, 0x33, 0xFF]);
        using var bgra = Bitmap(PixelFormat.Bgra8888, [0x11, 0x22, 0x33, 0xFF]);
        using var larger = new WriteableBitmap(new PixelSize(3, 2), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);

        Assert.False(Frames.SameAs(rgba, bgra));
        Assert.False(Frames.SameAs(rgba, larger));
    }

    [AvaloniaFact]
    public void SameAs_Refuses_A_Format_It_Does_Not_Read()
    {
        using var frame = Bitmap(PixelFormat.Rgb565, [0x00, 0xF8, 0x00, 0xF8]);

        Assert.Throws<NotSupportedException>(() => Frames.SameAs(frame, frame));
    }

    private static WriteableBitmap Bitmap(PixelFormat format, byte[] firstPixel)
    {
        var bitmap = new WriteableBitmap(new PixelSize(2, 2), new Vector(96, 96), format, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        System.Runtime.InteropServices.Marshal.Copy(firstPixel, 0, buffer.Address, firstPixel.Length);
        return bitmap;
    }
}
