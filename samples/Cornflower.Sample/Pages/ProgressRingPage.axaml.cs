using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 ProgressRing: at its default size, large, stretched, turned off and on, and in the light theme.</summary>
[SamplePage("progressring", Order = 28)]
public partial class ProgressRingPage : UserControl
{
    public ProgressRingPage()
    {
        InitializeComponent();
    }

    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        Default.IsActive = !Default.IsActive;
        Toggle.Content = Default.IsActive ? "stop" : "start";
    }
}
