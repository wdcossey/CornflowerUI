using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Cornflower.Tests;

/// <summary>Reading a captured frame.</summary>
internal static class Frames
{
    /// <summary>
    /// The pixel at (<paramref name="x"/>, <paramref name="y"/>) in the frame's own pixels, which
    /// in a <see cref="Scene"/> at the reference scaling are the emulator capture's pixels. Read in
    /// whichever order the frame holds them: Skia's frames are RGBA, which a grey wouldn't show.
    /// The value is as stored, so a translucent pixel is premultiplied.
    /// </summary>
    public static Color PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        if ((uint)x >= (uint)buffer.Size.Width || (uint)y >= (uint)buffer.Size.Height)
            throw new ArgumentOutOfRangeException(null, $"({x}, {y}) is outside the {buffer.Size.Width} × {buffer.Size.Height} frame.");

        var value = (uint)Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        byte b0 = (byte)value, b1 = (byte)(value >> 8), b2 = (byte)(value >> 16), b3 = (byte)(value >> 24);

        if (buffer.Format == PixelFormat.Rgba8888) return Color.FromArgb(b3, b0, b1, b2);
        if (buffer.Format == PixelFormat.Bgra8888) return Color.FromArgb(b3, b2, b1, b0);
        throw new NotSupportedException($"A frame in {buffer.Format} isn't read.");
    }
}
