using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 RadioButton: a group by its parent, a group by name across panels, disabled, with longer content, and in the light theme.</summary>
[SamplePage("radiobutton", Order = 32)]
public partial class RadioButtonPage : UserControl
{
    public RadioButtonPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnChanged(object? sender, RoutedEventArgs e) => ShowState();

    /// <summary>
    /// Which of the first group is checked, read from them. The XAML checks one, whose change comes during
    /// <c>InitializeComponent</c>, before the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || Colours is null) return;
        var chosen = Colours.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
        State.Text = chosen?.Content as string ?? "none";
    }
}
