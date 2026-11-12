using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The line of a text box's text, from its <c>FontSize</c>: the phone's own, 1.33 em (<see cref="PhoneTextLine.FontLine"/>,
/// Segoe WP's line, ADR 0003). On the phone a TextBox's text view draws each line 1.33 em below the last, its first
/// baseline where a TextBlock's would be at the same top (the reference app's wrapped TextBox, run
/// <c>20261112-1319-dev11</c>: baselines 65 raw pixels apart at 20.26, 1.33 em on the pixel grid, the first on the
/// placeholder's). Avalonia's <c>TextPresenter</c> draws its font's own line, Selawik's 1.2 em, unless given a
/// <c>LineHeight</c>, so the TextBox theme binds the box's own <c>LineHeight</c> to its <c>FontSize</c> through this, at
/// style priority: an app's <c>LineHeight</c> wins, and Avalonia's <c>MinLines</c> and <c>MaxLines</c>, which measure
/// lines of the box's <c>LineHeight</c>, measure the phone's.
/// </summary>
internal sealed class PhoneTextBoxLine : IValueConverter
{
    public static readonly PhoneTextBoxLine Converter = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double fontSize && double.IsFinite(fontSize) && fontSize > 0
            ? PhoneTextLine.FontLine * fontSize
            : AvaloniaProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
