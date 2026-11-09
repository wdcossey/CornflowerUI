using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;

namespace Cornflower.Tests.Harness;

/// <summary>
/// <see cref="ManualRenderTimer"/>: in a headless test, frames move only on a forced render tick
/// (<c>AvaloniaHeadlessPlatform.ForceRenderTimerTick</c>, which Avalonia's input helpers and
/// <c>CaptureRenderedFrame</c> also call), however long the test waits and however often the
/// dispatcher runs. Frame callbacks run in MediaContext's render pass, a dispatcher job, which waits
/// for a tick while a frame is in flight (a change committed and not yet composed) and while a
/// callback asked for from another is waiting.
/// </summary>
public class ManualRenderTimerTests
{
    // Six of a 60 Hz timer's frame intervals, with the dispatcher running throughout.
    private static readonly TimeSpan LongerThanAFrame = TimeSpan.FromMilliseconds(100);

    [AvaloniaFact]
    public async Task A_Frame_In_Flight_Finishes_Only_On_A_Forced_Tick()
    {
        var content = new Border { Background = Brushes.Red };
        using var scene = new Scene(content);
        content.Background = Brushes.Blue;
        Dispatcher.UIThread.RunJobs();
        var ran = false;
        scene.Window.RequestAnimationFrame(_ => ran = true);

        await Task.Delay(LongerThanAFrame);
        Dispatcher.UIThread.RunJobs();
        Assert.False(ran);

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.True(ran);
    }

    [AvaloniaFact]
    public async Task A_Frame_Asked_For_From_A_Frame_Runs_Only_On_A_Forced_Tick()
    {
        using var scene = new Scene(new Border());
        using (scene.Capture())
        {
            // Drawn: nothing in flight.
        }

        var first = false;
        var second = false;
        scene.Window.RequestAnimationFrame(_ =>
        {
            first = true;
            scene.Window.RequestAnimationFrame(_ => second = true);
        });
        Dispatcher.UIThread.RunJobs();
        Assert.True(first);

        await Task.Delay(LongerThanAFrame);
        Dispatcher.UIThread.RunJobs();
        Assert.False(second);

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.True(second);
    }

    [AvaloniaFact]
    public void A_Capture_Shows_The_Latest_Change_With_A_Frame_In_Flight()
    {
        var content = new Border { Background = Brushes.Red };
        using var scene = new Scene(content);
        content.Background = Brushes.Blue;
        Dispatcher.UIThread.RunJobs();
        content.Background = Brushes.Green;

        using var frame = scene.Capture();

        Assert.Equal(Colors.Green, Frames.PixelAt(frame, 10, 10));
    }
}
