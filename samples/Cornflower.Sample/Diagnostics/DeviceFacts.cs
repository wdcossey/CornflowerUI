using System.Globalization;
using Avalonia.Controls;

namespace Cornflower.Sample.Diagnostics;

/// <summary>
/// What the device reports for a page: the theme page shows it, and <see cref="SampleShell"/> writes it in each
/// page's ready line.
/// </summary>
internal static class DeviceFacts
{
    /// <summary>
    /// <c>scaling=… view=WxH raw=WxH safe=l,t,r,b page=WxH</c>: <c>view</c> is <paramref name="top"/>'s client
    /// area, in view pixels and raw pixels; <c>page</c> is <paramref name="page"/>'s own size, which a shell's
    /// chrome would take from.
    /// </summary>
    public static string Of(TopLevel top, Control page)
    {
        var size = top.ClientSize;
        var safe = top.InsetsManager?.SafeAreaPadding ?? default;
        return string.Format(CultureInfo.InvariantCulture,
            "scaling={0:0.###} view={1:0.##}x{2:0.##} raw={3:0}x{4:0} safe={5:0.##},{6:0.##},{7:0.##},{8:0.##} page={9:0.##}x{10:0.##}",
            top.RenderScaling, size.Width, size.Height,
            size.Width * top.RenderScaling, size.Height * top.RenderScaling,
            safe.Left, safe.Top, safe.Right, safe.Bottom, page.Bounds.Width, page.Bounds.Height);
    }
}
