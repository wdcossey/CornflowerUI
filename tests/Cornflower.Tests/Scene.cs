using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests;

/// <summary>
/// A shown headless window at the reference profile, closed and emptied when the test ends, so
/// controls detach, their timers stop, nothing leaks into the next test on the shared UI thread,
/// and the content can be shown again in another scene.
/// </summary>
/// <remarks>
/// <para>
/// The reference profile is ADR 0001's: a 450 × 800 view at a render scaling of 2.4. So a frame
/// is 1080 × 1920, the emulator capture's own pixels, and layout rounds to the same raw pixel grid
/// as the phone's.
/// </para>
/// <para>
/// <c>SimpleTheme</c> then <c>Wp81Theme</c> go in the window's <c>Styles</c> before the content
/// is set: a control picks its <c>ControlTheme</c> once, when it joins a logical tree, and the
/// later <c>Styles</c> wins (docs/spikes, 1 and 2). Simple renders what Wp81 doesn't theme yet
/// (ADR 0002). The themes are the window's, not the application's, so a scene changes no
/// process-wide state.
/// </para>
/// </remarks>
internal sealed class Scene : IDisposable
{
    /// <summary>The reference view's width, in view pixels (ADR 0001).</summary>
    public const double ReferenceWidth = 450;

    /// <summary>The reference view's height, in view pixels (ADR 0001).</summary>
    public const double ReferenceHeight = 800;

    /// <summary>The reference profile's <c>RawPixelsPerViewPixel</c> (ADR 0001).</summary>
    public const double ReferenceScaling = 2.4;

    public Window Window { get; }

    /// <param name="content">What to show.</param>
    /// <param name="variant">Dark or light. Dark, the phone's default, unless given.</param>
    /// <param name="width">The view's width: another profile's, for a second reading.</param>
    /// <param name="height">The view's height: another profile's, for a second reading.</param>
    /// <param name="scaling">Raw pixels per view pixel: another profile's, for a second reading.</param>
    public Scene(
        Control? content = null,
        ThemeVariant? variant = null,
        double width = ReferenceWidth,
        double height = ReferenceHeight,
        double scaling = ReferenceScaling)
    {
        Window = new Window
        {
            Width = width,
            Height = height,
            RequestedThemeVariant = variant ?? ThemeVariant.Dark,
        };
        Window.Styles.Add(new SimpleTheme());
        Window.Styles.Add(new Wp81Theme());
        Window.SetRenderScaling(scaling);
        try
        {
            Window.Content = content;
            Window.Show();
            Frames.DrawFirstFrame(Window);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Renders the scene as it is now, with one render tick (<see cref="ManualRenderTimer"/>), which
    /// also runs any frame callbacks due. The caller disposes the frame.
    /// </summary>
    public WriteableBitmap Capture()
    {
        Dispatcher.UIThread.RunJobs();
        ManualRenderTimer.ForceTick();
        Dispatcher.UIThread.RunJobs();
        return Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The window rendered no frame.");
    }

    public void Dispose()
    {
        Window.Close();
        Window.Content = null;
        Dispatcher.UIThread.RunJobs();
    }
}
