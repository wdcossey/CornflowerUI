using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 ToggleSwitch: with a header and its state shown, on, with no header, with its own words, disabled, and in the light theme.</summary>
[SamplePage("toggleswitch", Order = 33)]
public partial class ToggleSwitchPage : UserControl
{
    public ToggleSwitchPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnIsCheckedChanged(object? sender, RoutedEventArgs e) => ShowState();

    /// <summary>
    /// The first switch's state, read from it. The XAML can raise the change during <c>InitializeComponent</c>, before
    /// the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || WiFi is null) return;
        State.Text = WiFi.IsChecked == true ? "wi-fi is on" : "wi-fi is off";
    }
}
