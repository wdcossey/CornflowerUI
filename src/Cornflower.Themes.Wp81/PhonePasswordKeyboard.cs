using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input.TextInput;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What a masked text box tells the platform keyboard: that it holds a password (<c>TextInputOptions.ContentType</c>
/// <c>Password</c>), that the text is sensitive (<c>IsSensitive</c>) and that it wants no suggestions
/// (<c>ShowSuggestions</c> false), so a keyboard neither suggests nor learns from it. Avalonia's <c>TextBox</c> tells it
/// none of these for a <c>PasswordChar</c>, and on Android Gboard suggested words from a typed password (#104). This is
/// the platform's keyboard, not the phone's: whether the phone's own keyboard suggests in a PasswordBox is unmeasured.
/// </summary>
/// <remarks>
/// Turned on by the TextBox and PasswordBox themes' own setter. While the box has a <c>PasswordChar</c>, the three are
/// set at style priority, so an app's own values on the box win; while it has none, or once turned off, they are taken
/// away entirely, so the box has what it would have had: Avalonia's defaults, or an ancestor's (the three inherit). A
/// style setter can't do this: one whose binding gives no value still hides an ancestor's. Inheriting, the three reach
/// what the box's template holds, an app's <c>InnerLeftContent</c>, <c>InnerRightContent</c> and header included.
/// </remarks>
internal static class PhonePasswordKeyboard
{
    /// <summary>Set by a theme: a masked box tells the keyboard it holds a password.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsEnabled", typeof(PhonePasswordKeyboard));

    private static readonly ConditionalWeakTable<TextBox, IDisposable?[]> Set = new();

    static PhonePasswordKeyboard()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextBox>((box, _) => Update(box));
        TextBox.PasswordCharProperty.Changed.AddClassHandler<TextBox>((box, _) => Update(box));
    }

    public static bool GetIsEnabled(TextBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    private static void Update(TextBox box)
    {
        var masked = GetIsEnabled(box) && box.PasswordChar != default;
        var isSet = Set.TryGetValue(box, out var values);
        if (masked == isSet)
            return;
        if (masked)
        {
            Set.Add(box,
            [
                box.SetValue(TextInputOptions.ContentTypeProperty, TextInputContentType.Password, BindingPriority.Style),
                box.SetValue(TextInputOptions.IsSensitiveProperty, true, BindingPriority.Style),
                box.SetValue(TextInputOptions.ShowSuggestionsProperty, false, BindingPriority.Style),
            ]);
        }
        else
        {
            Set.Remove(box);
            foreach (var value in values!)
                value?.Dispose();
        }
    }
}
