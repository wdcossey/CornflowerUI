using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>Header</c> and <c>HeaderTemplate</c>, for a control of Avalonia's that lacks them: the content shown
/// above the control, and the template it is shown with. They hold data only; a theme's template shows them, as the
/// Windows Phone 8.1 Slider's does in its <c>HeaderContentPresenter</c>. A template that doesn't show them ignores
/// them.
/// </summary>
/// <example>
/// <code>&lt;Slider cf:ControlHeader.Header="volume" /&gt;</code>
/// </example>
public static class ControlHeader
{
    /// <summary>The header's content: WinRT's <c>Header</c>. None by default, and then no header is shown.</summary>
    public static readonly AttachedProperty<object?> HeaderProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("Header", typeof(ControlHeader));

    /// <summary>The template the header's content is shown with: WinRT's <c>HeaderTemplate</c>.</summary>
    public static readonly AttachedProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.RegisterAttached<Control, IDataTemplate?>("HeaderTemplate", typeof(ControlHeader));

    public static object? GetHeader(Control control) => control.GetValue(HeaderProperty);

    public static void SetHeader(Control control, object? value) => control.SetValue(HeaderProperty, value);

    public static IDataTemplate? GetHeaderTemplate(Control control) => control.GetValue(HeaderTemplateProperty);

    public static void SetHeaderTemplate(Control control, IDataTemplate? value) =>
        control.SetValue(HeaderTemplateProperty, value);
}
