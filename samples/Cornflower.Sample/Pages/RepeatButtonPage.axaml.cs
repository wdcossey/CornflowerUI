using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 RepeatButton: a count to hold up or down, disabled, stretched, and in the light theme.</summary>
[SamplePage("repeatbutton", Order = 30)]
public partial class RepeatButtonPage : UserControl
{
    private int _count;

    public RepeatButtonPage()
    {
        InitializeComponent();
    }

    private void OnLess(object? sender, RoutedEventArgs e) => Show(--_count);

    private void OnMore(object? sender, RoutedEventArgs e) => Show(++_count);

    private void Show(int count) => Count.Text = count.ToString(CultureInfo.InvariantCulture);
}
