using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 HyperlinkButton: default, disabled, with a NavigateUri, stretched, and in the light theme.</summary>
[SamplePage("hyperlinkbutton", Order = 29)]
public partial class HyperlinkButtonPage : UserControl
{
    private int _clicks;

    public HyperlinkButtonPage()
    {
        InitializeComponent();
    }

    private void OnClick(object? sender, RoutedEventArgs e) =>
        Clicks.Text = string.Create(CultureInfo.InvariantCulture, $"clicked {++_clicks} {(_clicks == 1 ? "time" : "times")}");
}
