using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 ToggleButton: unchecked, checked, with three states, disabled both ways, stretched, and in the light theme.</summary>
[SamplePage("togglebutton", Order = 30)]
public partial class ToggleButtonPage : UserControl
{
    public ToggleButtonPage()
    {
        InitializeComponent();
    }

    private void OnChanged(object? sender, RoutedEventArgs e) => State.Text = Default.IsChecked == true ? "checked" : "unchecked";
}
