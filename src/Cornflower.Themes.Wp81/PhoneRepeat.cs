using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 RepeatButton's repeats stop while the finger that pressed it is off it, and go on at the next tick when
/// it comes back; Avalonia's go on wherever the pointer is. Read in the emulator (run <c>20261112-0755-dev11</c>): a
/// press held at the centre, slid 90 view pixels down and held there for a second, then slid back, clicked every 60
/// ms until the finger left the button, not once while it was off, and again 10 ms after it came back, with no
/// second delay. Turned on by the RepeatButton theme's own setter.
/// </summary>
/// <remarks>
/// <para>A repeat is a <see cref="Button.Click"/> that Avalonia's own timer raises, with no way in to stop the timer.
/// So a class handler marks a Click handled while the pressing pointer is off the button: a class handler runs before
/// the app's handlers, and a handled Click doesn't run the button's <see cref="Button.Command"/>. The timer keeps
/// running, as the phone's does, so the next tick after the finger comes back clicks.</para>
/// <para>Off the button is where Avalonia's Button itself decides a release doesn't click: no part of the button
/// under the pointer. The overhang, the template's transparent root, is on it. A Click from the keyboard, or from
/// the press itself, is never held back: only one raised while a pointer pressed the button and was last seen off
/// it. It costs a pointer move on a pressed RepeatButton a hit test, and no global rule.</para>
/// </remarks>
internal static class PhoneRepeat
{
    /// <summary>Set by Wp81's RepeatButton theme: whether the repeats stop while the finger is off the button.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<RepeatButton, bool>("IsEnabled", typeof(PhoneRepeat));

    /// <summary>Whether the pointer pressing the button was last seen off it.</summary>
    private static readonly AttachedProperty<bool> IsOffProperty =
        AvaloniaProperty.RegisterAttached<RepeatButton, bool>("IsOff", typeof(PhoneRepeat));

    static PhoneRepeat()
    {
        // Pressed and moved arrive handled (Button handles the press), so the handlers take handled events too.
        InputElement.PointerPressedEvent.AddClassHandler<RepeatButton>((button, _) => button.ClearValue(IsOffProperty), handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<RepeatButton>(OnMoved, handledEventsToo: true);
        Button.ClickEvent.AddClassHandler<RepeatButton>(OnClick);
        Button.IsPressedProperty.Changed.AddClassHandler<RepeatButton>((button, e) =>
        {
            if (!e.GetNewValue<bool>()) button.ClearValue(IsOffProperty);
        });
    }

    public static bool GetIsEnabled(RepeatButton button) => button.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(RepeatButton button, bool value) => button.SetValue(IsEnabledProperty, value);

    private static void OnMoved(RepeatButton button, PointerEventArgs e)
    {
        if (!button.IsPressed || !button.GetValue(IsEnabledProperty))
            return;

        var on = button.GetVisualsAt(e.GetPosition(button)).Any(v => v == button || button.IsVisualAncestorOf(v));
        button.SetValue(IsOffProperty, !on);
    }

    private static void OnClick(RepeatButton button, RoutedEventArgs e)
    {
        if (e.Source == button && button.IsPressed && button.GetValue(IsOffProperty) && button.GetValue(IsEnabledProperty))
            e.Handled = true;
    }
}
