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
            // Nothing to take away, and nothing to set until the box is masked: no bookkeeping for a plain box.
            if (!IsMasked(box))
                return;
            applied = new Applied(box);
            Bookkeeping.Add(box, applied);
        }

        if (change.Property == applied.Setting)
            return;

        var look = LookAt(change.Property);
        if (applied.Updating)
        {
            // Another's change, made in the middle of a pass: worked out again after it.
            applied.Dirty |= look | Slots.Pass;
            return;
        }

        if (!IsMasked(box) && !applied.HoldsAny)
            return;

        if (change.Priority is BindingPriority.Unset or BindingPriority.Inherited)
            applied.Defer(look);
        else
            Update(box, applied, look);
    }

    private static bool IsMasked(TextBox box) => GetIsEnabled(box) && box.PasswordChar != default;

    /// <summary>
    /// Which of its values a change asks this to look at again, taking its own away to see whether anything else sets
    /// the property: the option that changed, or all three when the switch did; none for a <c>PasswordChar</c>, which
    /// only masks or unmasks.
    /// </summary>
    private static Slots LookAt(AvaloniaProperty property) =>
        property == TextInputOptions.ContentTypeProperty ? Slots.ContentType
        : property == TextInputOptions.IsSensitiveProperty ? Slots.IsSensitive
        : property == TextInputOptions.ShowSuggestionsProperty ? Slots.ShowSuggestions
        : property == IsEnabledProperty ? Slots.All
        : Slots.None;

    /// <summary>Sets or takes the three, looking again at those in <paramref name="look"/>.</summary>
    private static void Update(TextBox box, Applied applied, Slots look)
    {
        applied.Updating = true;
        try
        {
            for (var pass = 0; pass < 4; pass++)
            {
                applied.Dirty = Slots.None;
                var masked = IsMasked(box);
                Put(box, applied, Slots.ContentType, TextInputOptions.ContentTypeProperty, TextInputContentType.Password, masked, look);
                Put(box, applied, Slots.IsSensitive, TextInputOptions.IsSensitiveProperty, true, masked, look);
                Put(box, applied, Slots.ShowSuggestions, TextInputOptions.ShowSuggestionsProperty, (bool?)false, masked, look);
                if (applied.Dirty == Slots.None)
                    break;
                look = applied.Dirty;
            }
        }
        finally
        {
            applied.Updating = false;
            applied.Dirty = Slots.None;
        }
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
        All = ContentType | IsSensitive | ShowSuggestions,

        /// <summary>A change in the middle of a pass that asks for another pass but no value looked at again.</summary>
        Pass = 8,
    }

    private sealed class Applied
    {
        public readonly IDisposable?[] Set = new IDisposable?[3];

        /// <summary>The property this is setting or taking back, whose change is its own.</summary>
        public AvaloniaProperty? Setting;

        /// <summary>While this works the values out, the changes it raises itself aren't handled again.</summary>
        public bool Updating;

        /// <summary>What a change made by other code during a pass asks to look at again.</summary>
        public Slots Dirty;

        private readonly Action _run;
        private DispatcherOperation? _posted;
        private Slots _deferred;

        public Applied(TextBox box) => _run = () =>
        {
            var look = _deferred;
            _deferred = Slots.None;
            Update(box, this, look);
        };

        public bool HoldsAny => Set[0] is not null || Set[1] is not null || Set[2] is not null;

        /// <summary>A pass from the dispatcher, after Avalonia's unsetting (spike 6); one at a time, as PhoneTextLine's.</summary>
        public void Defer(Slots look)
        {
            _deferred |= look;
            if (_posted is not { Status: DispatcherOperationStatus.Pending })
                _posted = Dispatcher.UIThread.InvokeAsync(_run, DispatcherPriority.Normal);
        }
    }
}
