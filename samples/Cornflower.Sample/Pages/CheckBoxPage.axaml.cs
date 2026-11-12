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
        ShowState();
    }

    private void OnChanged(object? sender, RoutedEventArgs e) => ShowState();

    /// <summary>
    /// The default CheckBox's state, read from it. Were the XAML to set its <c>IsChecked</c>, the change would come during
    /// <c>InitializeComponent</c>, before the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || Default is null) return;
        State.Text = Default.IsChecked switch { true => "checked", false => "unchecked", null => "indeterminate" };
    }
}
