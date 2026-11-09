using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;

namespace Cornflower.Tests.Harness;

/// <summary>
/// <see cref="ManualRenderTimer"/>: a window's frame in flight (a change committed to the
/// compositor and not yet composed) finishes only when the test ticks, however long it waits and
/// however often the dispatcher runs. Avalonia's MediaContext runs frame callbacks in its own
/// render pass, a dispatcher job; while a frame is in flight, that pass waits for it.
/// </summary>
public class ManualRenderTimerTests
{
    [AvaloniaFact]
    public async Task A_Frame_In_Flight_Finishes_Only_On_A_Forced_Tick()
    {
        var content = new Border { Background = Brushes.Red };
        using var scene = new Scene(content);
        content.Background = Brushes.Blue;
        Dispatcher.UIThread.RunJobs();
        var ran = false;
        scene.Window.RequestAnimationFrame(_ => ran = true);

        // Six of the platform timer's frame intervals, with the dispatcher running throughout.
        await Task.Delay(100);
        Dispatcher.UIThread.RunJobs();
        Assert.False(ran);

        ManualRenderTimer.ForceTick();
        Dispatcher.UIThread.RunJobs();
        Assert.True(ran);
    }

    [AvaloniaFact]
    public void A_Tick_Draws_The_Change()
    {
        var content = new Border { Background = Brushes.Red };
        using var scene = new Scene(content);
        content.Background = Brushes.Blue;

        using var frame = scene.Capture();

        Assert.Equal(Colors.Blue, Frames.PixelAt(frame, 10, 10));
    }
}
