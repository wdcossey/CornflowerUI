using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// The spikes change process-wide state (<c>Application.Styles</c>, <c>Logger.Sink</c>), so they
/// run on their own, after the parallel tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SpikeCollection
{
    public const string Name = "Spikes";
}

internal static class Spike
{
    private static readonly Uri BaseUri = new("avares://Cornflower.Tests/");

    /// <summary>A compiled <c>Styles</c> file from this folder, as an app would include it.</summary>
    public static IStyle StylesFile(string path) =>
        new StyleInclude(BaseUri) { Source = new Uri(BaseUri, "Spikes/" + path) };

    /// <summary>A compiled <c>ResourceDictionary</c> file from this folder, as <c>Wp81Theme</c> merges it.</summary>
    public static ResourceInclude ResourceFile(string path) =>
        new(BaseUri) { Source = new Uri(BaseUri, "Spikes/" + path) };

    /// <summary>Adds styles to <c>Application.Styles</c> and takes them out again on dispose.</summary>
    public static IDisposable InApplication(params IStyle[] styles)
    {
        var app = Application.Current!;
        foreach (var style in styles) app.Styles.Add(style);
        return new Undo(() => { foreach (var style in styles) app.Styles.Remove(style); });
    }

    /// <summary>Names of a templated control's direct visual children, i.e. its template's root.</summary>
    public static string[] TemplateRoot(Control control) =>
        control.GetVisualChildren().Select(v => $"{v.GetType().Name}:{(v as Control)?.Name}").ToArray();

    public static void Settle() => Dispatcher.UIThread.RunJobs();

    private sealed class Undo(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }
}

/// <summary>A shown headless window, closed and emptied on dispose so its content can be reused.</summary>
internal sealed class SpikeWindow : IDisposable
{
    public Window Window { get; }

    public SpikeWindow(Control? content = null, Action<Window>? beforeContent = null, double width = 400, double height = 400)
    {
        Window = new Window { Width = width, Height = height };
        beforeContent?.Invoke(Window);
        Window.Content = content;
        Window.Show();
        Spike.Settle();
    }

    public void Dispose()
    {
        Window.Close();
        Window.Content = null;
        Spike.Settle();
    }
}

/// <summary>Collects every log event at <c>Warning</c> or above while it's installed.</summary>
internal sealed class WarningLog : ILogSink, IDisposable
{
    private readonly ILogSink? _previous = Logger.Sink;

    public List<string> Events { get; } = [];

    public WarningLog() => Logger.Sink = this;

    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Log(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (level >= LogEventLevel.Warning)
            Events.Add($"{level} {area} {source?.GetType().Name}: {messageTemplate}");
    }

    public void Dispose() => Logger.Sink = _previous;
}
