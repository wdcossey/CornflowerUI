using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// One of the phone's text lines tall, at its inherited <c>FontSize</c>, and nothing wide; it draws nothing. The 8.1
/// RadioButton template's empty <c>TextBlock</c>, whose only effect is its height: Segoe WP's line, 1.33 em
/// (<see cref="PhoneTextLine.FontLine"/>, ADR 0003). In a cell beside a larger shape, it makes the cell as tall as a
/// line of the control's text when that is taller, so the shape, centred in the cell, is centred on the first line.
/// </summary>
/// <remarks>
/// Not a <c>TextBlock</c>, as an app's style for <c>TextBlock</c> would reach one in the template (a margin, a size, a
/// line height) and move the shape; the phone's empty <c>TextBlock</c> takes only the control's font.
/// </remarks>
internal sealed class PhoneLineSpacer : Control
{
    static PhoneLineSpacer()
    {
        AffectsMeasure<PhoneLineSpacer>(TextElement.FontSizeProperty);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(0, PhoneTextLine.FontLine * TextElement.GetFontSize(this));
}
