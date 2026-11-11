using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's line for the text a template's <c>ContentPresenter</c> makes of its content: what the phone's own
/// <c>ContentPresenter</c> does with a string, a number or any other object that isn't an element, putting it in a
/// <c>TextBlock</c> with no <c>LineHeight</c>, so Segoe WP's 1.33 em line (ADR 0003). Avalonia's makes the same
/// <c>TextBlock</c> with Selawik's 1.2 em, so this gives it <see cref="PhoneTextLine"/>'s MaxHeight line, 1.33 ×
/// its own <c>FontSize</c>.
/// </summary>
/// <remarks>
/// Only the <c>TextBlock</c> Avalonia's default template makes: not content that is an element, and not what an
/// app's own <c>ContentTemplate</c> or a <c>DataTemplate</c> it defines (on a control or the application) makes of it.
/// Nothing is added to the presenter's data templates, so an app's are found as they would be without the theme.
/// </remarks>
internal static class PhoneContentLine
{
    /// <summary>Set by a theme on its template's <c>ContentPresenter</c>.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, bool>("IsEnabled", typeof(PhoneContentLine));

    static PhoneContentLine()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
    }

    public static bool GetIsEnabled(ContentPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContentPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    private static void Update(ContentPresenter presenter)
    {
        if (presenter.Child is not TextBlock text || !presenter.GetValue(IsEnabledProperty))
            return;

        var content = presenter.Content;
        if (content is null or Control || presenter.FindDataTemplate(content, presenter.ContentTemplate) is not null)
            return;

        // Avalonia's default template made this TextBlock for data: the phone's line, at its own size.
        // The smallest size PhoneTextLine takes (since #77 it ignores 0), so the line is always its own, 1.33 em.
        PhoneTextLine.SetMaxHeightLineHeight(text, double.Epsilon);
    }
}
