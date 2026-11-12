using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What a masked text box tells the platform keyboard: that it holds a password (<c>TextInputOptions.ContentType</c>
/// <c>Password</c>), that the text is sensitive (<c>IsSensitive</c>) and that it wants no suggestions
/// (<c>ShowSuggestions</c> false), so a keyboard neither suggests nor learns from it. Avalonia's <c>TextBox</c> tells it
/// none of these for a <c>PasswordChar</c>, and on Android Gboard suggested words from a typed password (#104; with
/// this, the Pixel 8's Gboard shows no suggestions, masked or shown). This is the platform's keyboard, not the phone's:
/// whether the phone's own keyboard suggests in a PasswordBox is unmeasured.
/// </summary>
/// <remarks>
/// <para>Turned on by the TextBox and PasswordBox themes' own setter. While the box has a <c>PasswordChar</c>, each of
/// the three is set at style priority unless something else sets it on the box: the app, an app's style, or a
/// <c>ControlTheme</c> based on one of these (a PIN box's <c>Pin</c>). While the box has none, or once turned off, they
/// are taken away entirely, so the box has what it would have had: Avalonia's defaults, or an ancestor's. A style
/// setter can't do this: one whose binding gives no value still hides an ancestor's (spike 8). The three inherit
/// (spike 8), so they reach what the box's template holds, an app's <c>InnerLeftContent</c>, <c>InnerRightContent</c>
/// and header included.</para>
/// <para>As <see cref="PhoneTextLine"/>: before setting one, its own value is taken away, as only then can it see
/// whether anything else sets the property; when a value goes rather than comes (<c>Unset</c> or <c>Inherited</c>
/// priority: a theme going, a value cleared), the pass runs from the dispatcher, not inside Avalonia's unsetting of a
/// theme's values (spike 6; no test reaches that trap here, as a TextBox has values of properties registered later,
/// but the order is Avalonia's to change). A derived theme's setters are in by the time its base's turns this on, so a PIN box's
/// <c>ContentType</c> is seen at once.</para>
/// <para>The keyboard reads the options when the box takes focus: a <c>PasswordChar</c> set or cleared on a focused box
/// reaches it at the next focus (a known gap).</para>
/// </remarks>
internal static class PhonePasswordKeyboard
{
    /// <summary>Set by a theme: a masked box tells the keyboard it holds a password.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsEnabled", typeof(PhonePasswordKeyboard));

    private static readonly ConditionalWeakTable<TextBox, Applied> Bookkeeping = new();

    static PhonePasswordKeyboard()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextBox>(OnChanged);
        TextBox.PasswordCharProperty.Changed.AddClassHandler<TextBox>(OnChanged);
        TextInputOptions.ContentTypeProperty.Changed.AddClassHandler<TextBox>(OnChanged);
        TextInputOptions.IsSensitiveProperty.Changed.AddClassHandler<TextBox>(OnChanged);
        TextInputOptions.ShowSuggestionsProperty.Changed.AddClassHandler<TextBox>(OnChanged);
    }

    public static bool GetIsEnabled(TextBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    private static void OnChanged(TextBox box, AvaloniaPropertyChangedEventArgs change)
    {
        if (!Bookkeeping.TryGetValue(box, out var applied))
        {
            if (change.Property != IsEnabledProperty || !change.GetNewValue<bool>())
                return;
            applied = new Applied();
            Bookkeeping.Add(box, applied);
        }

        if (change.Property == applied.Setting)
            return;
        if (applied.Updating)
        {
            // Another's change, made in the middle of a pass: worked out again after it.
            applied.Dirty = true;
            return;
        }

        if (change.Priority is BindingPriority.Unset or BindingPriority.Inherited)
            applied.Post(box);
        else
            Update(box, applied, look: change.Property != TextBox.PasswordCharProperty);
    }

    /// <summary>
    /// Sets or takes the three. <paramref name="look"/>: take its own value away first, even when it holds, to see
    /// whether anything else now sets the property.
    /// </summary>
    private static void Update(TextBox box, Applied applied, bool look)
    {
        applied.Updating = true;
        try
        {
            for (var pass = 0; pass < 4; pass++)
            {
                applied.Dirty = false;
                var masked = GetIsEnabled(box) && box.PasswordChar != default;
                Put(box, applied, 0, TextInputOptions.ContentTypeProperty, TextInputContentType.Password, masked, look);
                Put(box, applied, 1, TextInputOptions.IsSensitiveProperty, true, masked, look);
                Put(box, applied, 2, TextInputOptions.ShowSuggestionsProperty, (bool?)false, masked, look);
                if (!applied.Dirty)
                    break;
                look = true;
            }
        }
        finally
        {
            applied.Updating = false;
            applied.Dirty = false;
        }
    }

    private static void Put<T>(TextBox box, Applied applied, int slot, StyledProperty<T> property, T value, bool masked,
        bool look)
    {
        var own = applied.Set[slot];
        if (masked && own is not null && !look && Equals(box.GetValue(property), value))
            return;

        applied.Setting = property;
        try
        {
            applied.Set[slot] = null;
            own?.Dispose();
            if (masked && !box.IsSet(property))
                applied.Set[slot] = box.SetValue(property, value, BindingPriority.Style);
        }
        finally
        {
            applied.Setting = null;
        }
    }

    private sealed class Applied
    {
        public readonly IDisposable?[] Set = new IDisposable?[3];
        /// <summary>The property this is setting or taking back: its own change, not another's.</summary>
        public AvaloniaProperty? Setting;
        public bool Updating;
        public bool Dirty;
        private bool _posted;

        public void Post(TextBox box)
        {
            if (_posted)
                return;
            _posted = true;
            Dispatcher.UIThread.Post(() =>
            {
                _posted = false;
                Update(box, this, look: true);
            }, DispatcherPriority.Normal);
        }
    }
}
