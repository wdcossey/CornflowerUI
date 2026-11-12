using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Cornflower.Sample.Pages;

/// <summary>The 8.1 Slider: with a header and its value shown, with ticks, reversed, disabled, vertical, and in the light theme.</summary>
[SamplePage("slider", Order = 34)]
public partial class SliderPage : UserControl
{
    public SliderPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e) => ShowState();

    /// <summary>
    /// The first Slider's value, read from it, to the nearest whole number. The XAML sets its value during
    /// <c>InitializeComponent</c>, before the fields are set, so it waits for the call after it.
    /// </summary>
    private void ShowState()
    {
        if (State is null || Volume is null) return;
        State.Text = Math.Round(Volume.Value).ToString(CultureInfo.InvariantCulture);
    }
}
