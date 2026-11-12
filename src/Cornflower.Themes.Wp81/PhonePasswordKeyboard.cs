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
/// theme's values (spike 6; no test reaches that trap through this, as a TextBox has values of properties registered
/// later, but the order is Avalonia's to change). Both share that, and the second pass for a change made in the middle
/// of one, in <see cref="DeferredPass{TSlots}"/>, whose deferral <c>PhoneTextLine</c>'s tests hold. A derived theme's
/// setters are in by the time its base's turns this on, so a PIN box's <c>ContentType</c> is seen at once.</para>
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
            // Nothing to take away, and nothing to set until the box is masked: no bookkeeping for a plain box.
            if (!IsMasked(box))
                return;
            applied = new Applied(box);
            Bookkeeping.Add(box, applied);
        }

        var look = LookAt(change.Property);
        if (applied.InPass(change.Property, look))
            return;

        if (!IsMasked(box) && !applied.HoldsAny)
            return;

        applied.Handle(change.Priority, look);
    }

    private static bool IsMasked(TextBox box) => GetIsEnabled(box) && box.PasswordChar != default;

    /// <summary>
    /// Which of its values a change asks this to look at again, taking its own away to see whether anything else sets
    /// the property: the option that changed; none for the switch or a <c>PasswordChar</c>, which only mask or unmask (when
    /// the switch comes on, this holds no values yet; when it goes, it takes them all).
    /// </summary>
    private static Slots LookAt(AvaloniaProperty property) =>
        property == TextInputOptions.ContentTypeProperty ? Slots.ContentType
        : property == TextInputOptions.IsSensitiveProperty ? Slots.IsSensitive
        : property == TextInputOptions.ShowSuggestionsProperty ? Slots.ShowSuggestions
        : Slots.None;

    /// <summary>
    /// Sets or takes the three, looking again at those in <paramref name="look"/>: one pass
    /// (<see cref="DeferredPass{TSlots}"/> runs another after a change made by other code in the middle of it, looking
    /// again at what that change asks).
    /// </summary>
    private static void WorkOut(TextBox box, Applied applied, Slots look)
    {
        var masked = IsMasked(box);
        Put(box, applied, Slots.ContentType, TextInputOptions.ContentTypeProperty, TextInputContentType.Password, masked, look);
        Put(box, applied, Slots.IsSensitive, TextInputOptions.IsSensitiveProperty, true, masked, look);
        Put(box, applied, Slots.ShowSuggestions, TextInputOptions.ShowSuggestionsProperty, (bool?)false, masked, look);
    }

    private static void Put<T>(TextBox box, Applied applied, Slots slot, StyledProperty<T> property, T value, bool masked,
        Slots look)
    {
        var index = slot switch { Slots.ContentType => 0, Slots.IsSensitive => 1, _ => 2 };
        var own = applied.Set[index];
        if (masked && own is not null && (look & slot) == 0 && Equals(box.GetValue(property), value))
            return;
        if (!masked && own is null)
            return;

        applied.Setting = property;
        try
        {
            applied.Set[index] = null;
            own?.Dispose();
            if (masked && !box.IsSet(property))
                applied.Set[index] = box.SetValue(property, value, BindingPriority.Style);
        }
        finally
        {
            applied.Setting = null;
        }
    }

    [Flags]
    private enum Slots
    {
        None = 0,
        ContentType = 1,
        IsSensitive = 2,
        ShowSuggestions = 4,
    }

    /// <summary>What this has set on one box; its passes, and the one it has waiting, are the base's.</summary>
    private sealed class Applied(TextBox box) : DeferredPass<Slots>
    {
        public readonly IDisposable?[] Set = new IDisposable?[3];

        public bool HoldsAny => Set[0] is not null || Set[1] is not null || Set[2] is not null;

        protected override void Pass(Slots look) => WorkOut(box, this, look);
    }
}
