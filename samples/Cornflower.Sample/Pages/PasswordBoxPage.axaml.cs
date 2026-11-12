using System.Globalization;
using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 PasswordBox: with "Show Password" and the length typed shown, plain with no header, disabled, and in the light theme.</summary>
[SamplePage("passwordbox", Order = 37)]
public partial class PasswordBoxPage : UserControl
{
    public PasswordBoxPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e) => ShowState();

    /// <summary>
    /// How many characters the first box holds, read from it, never the password itself. The XAML can raise
    /// <c>TextChanged</c> during <c>InitializeComponent</c>, before the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || Secret is null) return;
        State.Text = (Secret.Text?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + " characters";
    }
}
