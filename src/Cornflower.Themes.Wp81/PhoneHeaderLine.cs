using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's line for the text a header's <c>ContentPresenter</c> makes of its content, as
/// <see cref="PhoneContentLine"/> gives it to a presenter that recognises access keys. A WinRT header doesn't
/// recognise them (<c>"fan_speed"</c> draws its underscore), so the presenter doesn't, and Avalonia's makes a plain
/// <see cref="TextBlock"/> of data: this gives that <see cref="PhoneTextLine"/>'s MaxHeight line, 1.33 × its own
/// <c>FontSize</c>, Segoe WP's (ADR 0003).
/// </summary>
/// <remarks>
/// Only the text Avalonia's own template makes: the presenter's child is a <c>TextBlock</c> (not an
/// <see cref="AccessText"/>), its content isn't an element, it has no <c>ContentTemplate</c>, and no data template in
/// the tree matches its content (a plain <c>TextBlock</c> is the commonest root of an app's own). What an app's data
/// template makes keeps its own line. Setting <see cref="IsEnabledProperty"/> to false takes the line back off.
/// </remarks>
internal static class PhoneHeaderLine
{
    /// <summary>As <see cref="PhoneContentLine"/>'s: the smallest MaxHeight line, so the line is the phone's own.</summary>
    private const double PhoneLine = double.Epsilon;

    /// <summary>Set by a theme on its template's header presenter, which doesn't recognise access keys.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, bool>("IsEnabled", typeof(PhoneHeaderLine));

    /// <summary>The text each presenter's line was put on, to take it back.</summary>
    private static readonly ConditionalWeakTable<ContentPresenter, TextBlock> Given = new();

    static PhoneHeaderLine()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Update(presenter));
    }

    public static bool GetIsEnabled(ContentPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContentPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    private static void Update(ContentPresenter presenter)
    {
        var enabled = presenter.GetValue(IsEnabledProperty);
        var given = Given.TryGetValue(presenter, out var previous);
        if (!enabled && !given)
            return;                       // every ContentPresenter's child change comes here: most are none of ours

        var made = enabled && presenter.Child is TextBlock and not AccessText &&
                   presenter.ContentTemplate is null && presenter.Content is not (null or Control) &&
                   presenter.FindDataTemplate(presenter.Content) is null
            ? (TextBlock)presenter.Child
            : null;

        if (given && previous is not null && !ReferenceEquals(previous, made))
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
