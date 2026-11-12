using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's PasswordBox <c>ButtonStates</c>: going to <c>ButtonVisible</c> or <c>ButtonCollapsed</c> unticks the
/// RevealButton, so a password it showed is hidden again whenever the button comes or goes. Set by the PasswordBox
/// theme on the box: when what decides the button changes (the <c>revealPasswordButton</c> class, <c>IsReadOnly</c>,
/// <c>AcceptsReturn</c>, the theme's gate), this clears the box's <c>RevealPassword</c>, which the template's CheckBox
/// shows. So an app that takes the class away, or makes the box read only, while the password shows doesn't leave it
/// shown with nothing to hide it.
/// </summary>
/// <remarks>
/// It reacts to the decision, not to the CheckBox's visibility, so a template applied again, or the box leaving and
/// joining the tree, changes nothing: a <c>RevealPassword</c> an app sets, or binds, stands until the button comes or
/// goes. It clears with <c>SetCurrentValue</c>, as a tap on the CheckBox sets it, so a binding on it stays.
/// </remarks>
internal static class PhoneRevealButton
{
    /// <summary>The class that asks for the button, Avalonia's own (Simple's and Fluent's).</summary>
    internal const string Class = "revealPasswordButton";

    /// <summary>Set by the PasswordBox theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsEnabled", typeof(PhoneRevealButton));

    /// <summary>Each box's handler on its classes and whether the button showed, while this is on.</summary>
    private static readonly ConditionalWeakTable<TextBox, Watch> Watched = new();

    static PhoneRevealButton()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextBox>((box, change) =>
        {
            if (Watched.TryGetValue(box, out var old))
            {
                box.Classes.CollectionChanged -= old.Handler;
                Watched.Remove(box);
            }

            if (!change.GetNewValue<bool>())
                return;

            var watch = new Watch(box);
            box.Classes.CollectionChanged += watch.Handler;
            Watched.Add(box, watch);
        });

        TextBox.IsReadOnlyProperty.Changed.AddClassHandler<TextBox>((box, _) => Check(box));
        TextBox.AcceptsReturnProperty.Changed.AddClassHandler<TextBox>((box, _) => Check(box));
    }

    /// <summary>Whether the theme shows the button: the class, and a box that can be typed in, on one line.</summary>
    internal static bool Shows(TextBox box) => box.Classes.Contains(Class) && !box.IsReadOnly && !box.AcceptsReturn;

    private static void Check(TextBox box)
    {
        if (!Watched.TryGetValue(box, out var watch))
            return;                                       // most boxes: not the PasswordBox theme's

        var shows = Shows(box);
        if (shows == watch.Shows)
            return;

        watch.Shows = shows;
        box.SetCurrentValue(TextBox.RevealPasswordProperty, false);
    }

    private sealed class Watch
    {
        public Watch(TextBox box)
        {
            Shows = PhoneRevealButton.Shows(box);
            Handler = (_, _) => Check(box);
        }

        public bool Shows { get; set; }

        public NotifyCollectionChangedEventHandler Handler { get; }
    }
}
