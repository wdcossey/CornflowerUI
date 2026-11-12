using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 TextBox: with a header and a placeholder and what is typed shown, with text, wrapped, disabled, and in the light theme.</summary>
[SamplePage("textbox", Order = 36)]
public partial class TextBoxPage : UserControl
{
    public TextBoxPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e) => ShowState();

    /// <summary>
    /// What the first TextBox holds, read from it, or a dash when it holds nothing. The XAML can raise
    /// <c>TextChanged</c> during <c>InitializeComponent</c>, before the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || YourName is null) return;
        State.Text = string.IsNullOrEmpty(YourName.Text) ? "-" : YourName.Text;
    }
}
