using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's PasswordBox <c>ButtonStates</c>: going to <c>ButtonVisible</c> or <c>ButtonCollapsed</c> unticks the
/// RevealButton, so a password shown by it is hidden again whenever the button shows or goes. Set by the PasswordBox
/// theme on its template's CheckBox, whose <c>IsChecked</c> is the box's <c>RevealPassword</c>: when the CheckBox shows
/// or hides, this clears the box's <c>RevealPassword</c>. So an app that takes the <c>revealPasswordButton</c> class away
/// (or makes the box read only) while the password shows doesn't leave it shown with nothing to hide it.
/// </summary>
/// <remarks>
/// It clears with <c>SetCurrentValue</c>, as a tap on the CheckBox sets it, so the template's two-way binding stays; a
/// <c>RevealPassword</c> an app sets locally after the button shows stands, as on the phone, where only the states'
/// changes untick it.
/// </remarks>
internal static class PhoneRevealButton
{
    /// <summary>Set by the PasswordBox theme on its RevealButton.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, bool>("IsEnabled", typeof(PhoneRevealButton));

    public static bool GetIsEnabled(CheckBox button) => button.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(CheckBox button, bool value) => button.SetValue(IsEnabledProperty, value);

    static PhoneRevealButton()
    {
        Visual.IsVisibleProperty.Changed.AddClassHandler<CheckBox>((button, _) =>
        {
            if (button.GetValue(IsEnabledProperty) && button.TemplatedParent is TextBox box)
                box.SetCurrentValue(TextBox.RevealPasswordProperty, false);
        });
    }
}
