using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 Button: default, disabled, with longer content, stretched, side by side, and in the light theme.</summary>
[SamplePage("button", Order = 27)]
public partial class ButtonPage : UserControl
{
    private int _clicks;

    public ButtonPage()
    {
        InitializeComponent();
    }

    private void OnClick(object? sender, RoutedEventArgs e) =>
        Clicks.Text = string.Create(CultureInfo.InvariantCulture, $"clicked {++_clicks} {(_clicks == 1 ? "time" : "times")}");
}
