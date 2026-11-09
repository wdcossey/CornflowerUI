using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Cornflower.Tests;

/// <summary>Drawing a window's frames, and reading a captured frame.</summary>
internal static class Frames
{
    private const int MaxTicksToFirstFrame = 5;

    /// <summary>
    /// Draws a newly shown window's first frame, with as many render ticks as it takes. A window
    /// at the default scaling takes one; a <see cref="Scene"/>, whose scaling is set before it is
    /// shown, takes two. After the first frame, a change shows on the next tick.
    /// </summary>
    public static void DrawFirstFrame(TopLevel top)
    {
        for (var tick = 0; tick < MaxTicksToFirstFrame; tick++)
        {
            Dispatcher.UIThread.RunJobs();
            ManualRenderTimer.ForceTick();
            Dispatcher.UIThread.RunJobs();
            using var frame = top.CaptureRenderedFrame();
            if (frame is not null)
            {
                return;
            }
        }

        throw new InvalidOperationException($"The window drew no frame in {MaxTicksToFirstFrame} render ticks.");
    }

    /// <summary>
    /// The pixel at (<paramref name="x"/>, <paramref name="y"/>) in the frame's own pixels, which
    /// in a <see cref="Scene"/> at the reference scaling are the emulator capture's pixels. Read in
    /// whichever order the frame holds them: Skia's frames are RGBA, which a grey wouldn't show.
    /// The value is as stored, so a translucent pixel is premultiplied.
    /// </summary>
    public static Color PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var rgba = buffer.Format == PixelFormat.Rgba8888;
        if (!rgba && buffer.Format != PixelFormat.Bgra8888)
            throw new NotSupportedException($"A frame in {buffer.Format} isn't read.");
        if ((uint)x >= (uint)buffer.Size.Width || (uint)y >= (uint)buffer.Size.Height)
            throw new ArgumentOutOfRangeException(null, $"({x}, {y}) is outside the {buffer.Size.Width} × {buffer.Size.Height} frame.");

        var value = (uint)Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        byte b0 = (byte)value, b1 = (byte)(value >> 8), b2 = (byte)(value >> 16), b3 = (byte)(value >> 24);

        return rgba ? Color.FromArgb(b3, b0, b1, b2) : Color.FromArgb(b3, b2, b1, b0);
    }
}
