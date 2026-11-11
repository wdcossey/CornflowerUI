using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Cornflower.Sample;
using Xunit;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The shell times every page it opens, from navigation to the page's first drawn frame, and writes the page's
/// <c>CORNFLOWER ready</c> line (D-page-timing): a page has no timing code of its own. <c>tools/android/run.ps1</c> and
/// <c>tools/real-window/drive.ps1</c> read the line, so its format is held here.
/// </summary>
public class PageTimingTests
{
    /// <summary>Longer than a page this small takes to make, so that a time that includes it can be told apart.</summary>
    private const int SlowMs = 60;

    /// <summary>The shell's rail for Back, beside the page.</summary>
    private const double RailWidth = 40;

    /// <summary>A page with no timing code, as a control's page is.</summary>
    [SamplePage("plain", Order = 1)]
    private sealed class Plain : Border
    {
        // Hit-testable, so that a point on the page finds it.
        public Plain() => Background = Brushes.Transparent;
    }

    /// <summary>A page that takes <see cref="SlowMs"/> to make, and no timing code either.</summary>
    [SamplePage("slow page", Order = 2)]
    private sealed class Slow : Border
    {
        public Slow() => Thread.Sleep(SlowMs);
    }

    [AvaloniaFact]
    public void Each_Page_Opened_Gets_One_Ready_Line_With_Its_Title_And_Time_To_Open()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);

        shell.Open("plain");
        Draw();
        shell.ShowIndex();
        Draw();
        shell.Open("slow page");
        Draw();
        Draw();

        Assert.Collection(lines,
            line => Assert.Matches("^CORNFLOWER ready open=[0-9]+ms scaling=2.4 view=490x800 raw=1176x1920 safe=0,0,0,0 page=450x800 title=plain$", line),
            line => Assert.Matches("^CORNFLOWER ready open=[0-9]+ms scaling=2.4 view=490x800 raw=1176x1920 safe=0,0,0,0 page=450x800 title=slow page$", line));
    }

    // The button on the list goes the same way as Open.
    [AvaloniaFact]
    public void A_Page_Opened_From_The_List_Gets_Its_Ready_Line()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.FindControl<StackPanel>("PageList")!.Children.OfType<Button>().Last().RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Draw();

        Assert.EndsWith(" title=slow page", Assert.Single(lines), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_With_No_Timing_Code_Is_Timed_From_Before_It_Is_Made()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("slow page");
        Draw();

        Assert.True(OpenMs(Assert.Single(lines)) >= SlowMs - 10, lines[0]);
    }

    // Until the frame that first draws the page has been drawn, the page hasn't opened.
    [AvaloniaFact]
    public void The_Line_Waits_For_The_Pages_First_Frame()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(lines);

        Draw();
        Assert.Single(lines);
    }

    // The theme page, the device run's page and drive.ps1's, at the reference view beside the rail.
    [AvaloniaFact]
    public void The_Theme_Page_Is_Timed_By_The_Shell_With_The_Facts_It_Shows()
    {
        var lines = new List<string>();
        var shell = new SampleShell { Report = lines.Add };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);

        shell.Open("theme");
        Draw();

        var view = Assert.IsType<MainView>(shell.Shown);
        var line = Assert.Single(lines);
        Assert.Matches("^CORNFLOWER ready open=[0-9]+ms scaling=2.4 view=490x800 raw=1176x1920 safe=0,0,0,0 page=450x800 title=theme$", line);
        Assert.EndsWith($" {view.DeviceFacts} title=theme", line, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_That_Leaves_Before_Its_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        shell.ShowIndex();
        Draw();

        Assert.Empty(lines);
    }

    [AvaloniaFact]
    public void A_Page_Left_For_Another_Before_Its_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        shell.Open("slow page");
        Draw();

        Assert.EndsWith(" title=slow page", Assert.Single(lines), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Shell_That_Leaves_Its_Window_Before_The_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        // In and out with no dispatcher work between, so no render pass can run the frame requests before it leaves.
        shell.Open("plain");
        scene.Window.Content = null;
        Draw();

        Assert.Empty(lines);
    }

    // As on a phone: App opens the start page before Avalonia puts the shell in its view.
    [AvaloniaFact]
    public void A_Page_Opened_Before_The_Shell_Is_In_A_Window_Reports_Once_Drawn()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.Open("slow page");
        Assert.Empty(lines);

        using var scene = new Scene(shell);
        Draw();

        var line = Assert.Single(lines);
        Assert.EndsWith(" title=slow page", line, StringComparison.Ordinal);
        Assert.True(OpenMs(line) >= SlowMs - 10, line);
    }

    // A Debug build shows what the page cost to open, along the bottom, with the shell's time: the line and the
    // page's ready line agree. It's drawn over the page, which keeps its whole view, and takes none of its input.
    [AvaloniaFact]
    public void The_Debug_Line_Reads_The_Shells_Time_And_Goes_With_The_Page()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        var shown = shell.FindControl<TextBlock>("PerformanceLine")!;

        shell.Open("plain");
        Draw();

#if DEBUG
        Assert.True(shown.IsVisible);
        Assert.StartsWith(string.Create(CultureInfo.InvariantCulture, $"plain: {OpenMs(Assert.Single(lines))} ms, "), shown.Text, StringComparison.Ordinal);
        var page = Assert.IsType<Plain>(shell.Shown);
        Assert.Equal(new Size(Scene.ReferenceWidth, Scene.ReferenceHeight), page.Bounds.Size);
        var underTheLine = shown.TranslatePoint(new Point(shown.Bounds.Width / 2, shown.Bounds.Height / 2), scene.Window)!.Value;
        Assert.Same(page, scene.Window.InputHitTest(underTheLine));

        shell.ShowIndex();
        Assert.False(shown.IsVisible);
#else
        Assert.False(shown.IsVisible);
#endif
    }

    private static SampleShell ShellOf(List<string> lines) =>
        new(SamplePageCatalog.Discover([typeof(Plain), typeof(Slow)])) { Report = lines.Add };

    /// <summary>Enough frames for a page to be drawn and the frame after it to come.</summary>
    private static void Draw()
    {
        for (var frame = 0; frame < 4; frame++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static int OpenMs(string line) =>
        int.Parse(Regex.Match(line, "^CORNFLOWER ready open=([0-9]+)ms ").Groups[1].Value, CultureInfo.InvariantCulture);
}
