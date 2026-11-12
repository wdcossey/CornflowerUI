using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 CheckBox: default, checked, three-state, disabled, with longer content, and in the light theme.</summary>
[SamplePage("checkbox", Order = 31)]
public partial class CheckBoxPage : UserControl
{
    public CheckBoxPage()
    {
        InitializeComponent();
    }

    private void OnChanged(object? sender, RoutedEventArgs e) =>
        State.Text = Default.IsChecked switch { true => "checked", false => "unchecked", null => "indeterminate" };
}
