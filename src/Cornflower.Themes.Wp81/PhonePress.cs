using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// Whether the finger pressing a ToggleButton or RepeatButton is off it, as the 8.1 phone tells, and what the phone
/// does then. Read in the emulator (run <c>20261112-0755-dev11</c>): a checked ToggleButton pressed
/// (<c>CheckedPressed</c>, which draws the unchecked look) went back to <c>Checked</c> as the finger slid off it; a
/// RepeatButton held at the centre, slid 90 view pixels down and held there for a second, then slid back, clicked every
/// 60 ms until the finger left it, not once while it was off, and again 10 ms after it came back, with no second
/// delay. Turned on by those two themes' own setters.
/// </summary>
/// <remarks>
/// <para><b>Off</b> is where Avalonia's Button itself decides a release doesn't click: no part of the button under the
/// pointer that pressed it. The overhang, the template's transparent root, is on it. Only that pointer counts: another
/// moving over the button changes nothing. <see cref="IsOffProperty"/> says it, for the ToggleButton theme's
/// selectors, from the press until the release. Avalonia's own <c>:pointerover</c> can't: it isn't set under a
/// touch (tested), and a press from the keyboard has no pointer. With no pointer press, nothing is off.</para>
/// <para><b>A RepeatButton's repeats</b> are Clicks that Avalonia's own timer raises, with no way in to stop the timer.
/// So a class handler marks a RepeatButton's Click handled while the pressing pointer is off it, judged afresh at the
/// Click from where that pointer was last seen, so a button that moves away from a finger held still stops too. A
/// class handler runs before the app's handlers, and a handled Click doesn't run the button's
/// <see cref="Button.Command"/>. The timer keeps running, as the phone's does, so the next tick after the finger comes
/// back clicks. A Click from a control in the button's content goes on.</para>
/// <para>It costs a pressed control a hit test per move, and no global rule. A held-back repeat is a Click marked
/// handled: a handler added with <c>handledEventsToo</c> still sees it, and a Flyout on the button still opens and
/// closes with it, as Avalonia's Button does that before it raises Click.</para>
/// </remarks>
internal static class PhonePress
{
    /// <summary>Set by Wp81's ToggleButton and RepeatButton themes: whether the press is followed.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsEnabled", typeof(PhonePress));

    /// <summary>Whether the pointer pressing the button is off it. Read by the ToggleButton theme's selectors.</summary>
    public static readonly AttachedProperty<bool> IsOffProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsOff", typeof(PhonePress));

    /// <summary>The pointer pressing the button and where it was last seen; none once it is released.</summary>
    private static readonly AttachedProperty<Press?> PressProperty =
        AvaloniaProperty.RegisterAttached<Button, Press?>("Press", typeof(PhonePress));

    static PhonePress()
    {
        // Button handles the press and holds the pointer, so its events arrive handled: the handlers take them too.
        // They run after Button's own, so IsPressed is already set.
        InputElement.PointerPressedEvent.AddClassHandler<Button>(OnPressed, handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<Button>(OnMoved, handledEventsToo: true);
        Button.ClickEvent.AddClassHandler<RepeatButton>(OnRepeat);
        Button.IsPressedProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            if (e.GetNewValue<bool>() || !button.IsSet(PressProperty))
                return;
            button.ClearValue(PressProperty);
            button.ClearValue(IsOffProperty);
        });
    }

    public static bool GetIsEnabled(Button button) => button.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Button button, bool value) => button.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// Follows the press that made the button pressed: a left press (a touch, a pen tip, the mouse's left button)
    /// inside the button's own tree, not one in a popup it owns, which is another tree, and only the first: a second
    /// finger, or a press while Space holds it, takes nothing over.
    /// </summary>
    private static void OnPressed(Button button, PointerPressedEventArgs e)
    {
        if (!button.IsPressed || !button.GetValue(IsEnabledProperty) || button.IsSet(PressProperty) ||
            !e.GetCurrentPoint(button).Properties.IsLeftButtonPressed ||
            e.Source is not Visual source || (source != button && !button.IsVisualAncestorOf(source)) ||
            TopLevel.GetTopLevel(button) is not { } top)
            return;

        var press = new Press(e.Pointer) { At = e.GetPosition(top) };
        button.SetValue(PressProperty, press);
        Judge(button, press, top);
    }

    private static void OnMoved(Button button, PointerEventArgs e)
    {
        if (button.GetValue(PressProperty) is not { } press || press.Pointer != e.Pointer || TopLevel.GetTopLevel(button) is not { } top)
            return;

        press.At = e.GetPosition(top);
        Judge(button, press, top);
    }

    private static void OnRepeat(RepeatButton button, RoutedEventArgs e)
    {
        // Only the button's own Click: one from a control in its content goes on, wherever this one's pointer is.
        if (e.Source == button && button.IsPressed && button.GetValue(PressProperty) is { } press &&
            TopLevel.GetTopLevel(button) is { } top && Judge(button, press, top))
            e.Handled = true;
    }

    /// <summary>Sets whether <paramref name="press"/>'s pointer is off the button, and says it: true if it is.</summary>
    private static bool Judge(Button button, Press press, TopLevel top)
    {
        var off = top.TranslatePoint(press.At, button) is { } at && !button.GetVisualsAt(at).Any();
        button.SetValue(IsOffProperty, off);
        return off;
    }

    /// <summary>A pointer pressing a button, and where it was last seen in the top level: one per press, kept up to date.</summary>
    private sealed class Press(IPointer pointer)
    {
        public IPointer Pointer { get; } = pointer;

        public Point At { get; set; }
    }
}
