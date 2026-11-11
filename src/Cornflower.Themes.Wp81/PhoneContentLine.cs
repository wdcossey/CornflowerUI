using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's line for the text a template's <c>ContentPresenter</c> makes of its content: what the phone's own
/// <c>ContentPresenter</c> does with a string, a number or any other object that isn't an element, putting it in a
/// <c>TextBlock</c> with no <c>LineHeight</c>, so Segoe WP's 1.33 em line (ADR 0003). Avalonia's makes a
/// <c>TextBlock</c> with Selawik's 1.2 em, so this gives it <see cref="PhoneTextLine"/>'s MaxHeight line, 1.33 × its
/// own <c>FontSize</c>.
/// </summary>
/// <remarks>
/// <para>Only the text Avalonia's own template makes. A presenter that recognises access keys makes an
/// <see cref="AccessText"/> of data that no template of the app's matches, so that is what this looks for: the
/// presenter's child is an <c>AccessText</c>, its content isn't an element, and it has no <c>ContentTemplate</c>.
/// That needs no second data template lookup. Nothing is added to the presenter's data templates, so an app's are
/// found as they would be without the theme, and what they make keeps its own line. An app's own data template whose
/// root is an <c>AccessText</c> would get the phone's line too: a known gap.</para>
/// <para>Setting <see cref="IsEnabledProperty"/> to false takes the line back off the text it was put on.</para>
/// </remarks>
internal static class PhoneContentLine
{
    /// <summary>
    /// The <c>MaxHeight</c> line height passed to <see cref="PhoneTextLine"/>: the smallest size it takes, so the line
    /// is always the phone's own, 1.33 em. WinRT's <c>TextBlock</c> with no <c>LineHeight</c> has 0, which it reads as none.
    /// </summary>
    private const double PhoneLine = double.Epsilon;

    /// <summary>Set by a theme on its template's <c>ContentPresenter</c>, with <c>RecognizesAccessKey</c>.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, bool>("IsEnabled", typeof(PhoneContentLine));

    /// <summary>The text each presenter's line was put on, to take it back.</summary>
    private static readonly ConditionalWeakTable<ContentPresenter, AccessText> Given = new();

    static PhoneContentLine()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
    }

    public static bool GetIsEnabled(ContentPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContentPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    private static void Update(ContentPresenter presenter)
    {
        var made = presenter.GetValue(IsEnabledProperty) && presenter.Child is AccessText &&
                   presenter.ContentTemplate is null && presenter.Content is not (null or Control)
            ? (AccessText)presenter.Child
            : null;

        if (Given.TryGetValue(presenter, out var previous) && !ReferenceEquals(previous, made))
        {
            previous.ClearValue(PhoneTextLine.MaxHeightLineHeightProperty);
            Given.Remove(presenter);
        }

        if (made is null || ReferenceEquals(previous, made))
            return;

        PhoneTextLine.SetMaxHeightLineHeight(made, PhoneLine);
        Given.AddOrUpdate(presenter, made);
    }
}
