using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 ProgressBar: at 40% and stepped by a button, indeterminate and turned off and on, disabled, and in the light theme.</summary>
[SamplePage("progressbar", Order = 35)]
public partial class ProgressBarPage : UserControl
{
    public ProgressBarPage()
    {
        InitializeComponent();
    }

    private void OnStep(object? sender, RoutedEventArgs e)
    {
        Determinate.Value = Determinate.Value >= 100 ? 0 : Determinate.Value + 10;
    }

    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        Indeterminate.IsIndeterminate = !Indeterminate.IsIndeterminate;
        Toggle.Content = Indeterminate.IsIndeterminate ? "stop" : "start";
    }
}
