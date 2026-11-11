using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's text line, for the text styles in <c>Controls/TextBlock.axaml</c>: what WinRT's
/// <c>LineStackingStrategy</c> and <c>CharacterSpacing</c> do, which Avalonia's <c>TextBlock</c> doesn't have,
/// as values it does have, worked out from each <c>TextBlock</c>'s own font and sizes.
/// </summary>
/// <remarks>
/// <para><b>BaselineToBaseline.</b> On the phone, the first baseline is Segoe WP's ascent below the top (2210
/// of 2048 units), each later baseline <c>LineHeight</c> below the one before, and the bottom Segoe WP's share
/// of descent in a line (514 of 2724) times <c>LineHeight</c> below the last. Read from the 14 text styles the
/// reference app captured (docs/controls/TextBlock/PLAN.md): every height to within 0.01 DIP. Avalonia
/// draws each line <c>LineHeight</c> tall, with the baseline the font's own ascent below the line's top, plus
/// half of any height past the font's line (none if <c>LineHeight</c> is shorter: the line is cut from below).
/// So the difference is all above the first line and below the last, and <c>Padding</c> takes it. It reads the
/// <c>TextBlock</c>'s own face: a line whose fallback font (a script Selawik lacks) has a taller ascent moves
/// Avalonia's baseline down by the difference. With no <c>LineHeight</c> there is no phone's line to keep, and no
/// <c>Padding</c> is set.</para>
/// <para><b>MaxHeight.</b> The line is the larger of WinRT's <c>LineHeight</c> and the phone's own line,
/// 1.33 em (ADR 0003).</para>
/// <para><b>CharacterSpacing</b>, in thousandths of an em, is <c>LetterSpacing</c> n × <c>FontSize</c> / 1000
/// (docs/spikes, 5).</para>
/// <para>Each value is set at style priority, and only where nothing else sets the property, so a value the app
/// sets, by itself, a style or a <c>ControlTheme</c> based on a text style, wins. It is
/// worked out again whenever the <c>TextBlock</c>'s font or sizes change: a local <c>FontSize</c>, or a size an
/// extension pack sets through the type ramp's keys, keeps the phone's line. A class handler does this, rather
/// than a binding in each style, because bindings cost about 4 µs each per <c>TextBlock</c>
/// (<c>TypeRampCostBenchmark</c>), where a handler costs a <c>TextBlock</c> without a text style a property read
/// when its font changes.</para>
/// </remarks>
internal static class PhoneTextLine
{
    private const double SegoeWpUnitsPerEm = 2048;
    private const double SegoeWpAscent = 2210;      // hhea, and OS/2 win; the same in all six faces
    private const double SegoeWpDescent = 514;

    /// <summary>The phone's first baseline below the top, in ems.</summary>
    internal const double FirstBaseline = SegoeWpAscent / SegoeWpUnitsPerEm;

    /// <summary>The share of <c>LineHeight</c> below the phone's last baseline.</summary>
    internal const double DescentShare = SegoeWpDescent / (SegoeWpAscent + SegoeWpDescent);

    /// <summary>The phone's own line, in ems: what a style with no <c>LineHeight</c> gets (ADR 0003).</summary>
    internal const double FontLine = (SegoeWpAscent + SegoeWpDescent) / SegoeWpUnitsPerEm;

    /// <summary>WinRT's <c>LineStackingStrategy="BaselineToBaseline"</c>: sets <c>Padding</c>.</summary>
    public static readonly AttachedProperty<bool> BaselineToBaselineProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("BaselineToBaseline", typeof(PhoneTextLine));

    /// <summary>WinRT's <c>LineHeight</c> with <c>LineStackingStrategy="MaxHeight"</c>: sets <c>LineHeight</c>.</summary>
    public static readonly AttachedProperty<double> MaxHeightLineHeightProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, double>("MaxHeightLineHeight", typeof(PhoneTextLine), double.NaN);

    /// <summary>WinRT's <c>CharacterSpacing</c>, in thousandths of an em: sets <c>LetterSpacing</c>.</summary>
    public static readonly AttachedProperty<int> CharacterSpacingProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, int>("CharacterSpacing", typeof(PhoneTextLine));

    // What this has set on a TextBlock, to take back when it changes: set on each TextBlock the first time one of the
    // three is set on it, so one without it has nothing set by this, and a change of its font or sizes stops at
    // reading it. It is a property, not a ConditionalWeakTable beside the TextBlock, because its value keeps
    // Avalonia's value store unsetting a theme's values when the theme goes: the store unsets them from the property
    // registered last, and stops if a handler (Take, here) takes a value away while it is at the last one (spike 6,
    // docs/spikes). Registered after the three, it is always after them.
    // A_Value_A_Derived_ControlTheme_Sets_Wins_Over_The_Styles fails with a table.
    private static readonly AttachedProperty<Applied?> AppliedProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, Applied?>("Applied", typeof(PhoneTextLine));

    static PhoneTextLine()
    {
        BaselineToBaselineProperty.Changed.AddClassHandler<TextBlock>((b, e) =>
        {
            if (Track(b, e.GetNewValue<bool>()) is { } applied)
                UpdatePadding(b, applied);
        });
        MaxHeightLineHeightProperty.Changed.AddClassHandler<TextBlock>((b, e) =>
        {
            if (Track(b, !double.IsNaN(e.GetNewValue<double>())) is { } applied)
                UpdateLineHeight(b, applied);
        });
        CharacterSpacingProperty.Changed.AddClassHandler<TextBlock>((b, e) =>
        {
            if (Track(b, e.GetNewValue<int>() != 0) is { } applied)
                UpdateLetterSpacing(b, applied);
        });
        TextBlock.LineHeightProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            if (b.GetValue(AppliedProperty) is { } applied)
            {
                UpdateLineHeight(b, applied);
                UpdatePadding(b, applied);
            }
        });
        // Another source of a value this sets (the app's, a derived theme's) takes over, or gives it back.
        TextBlock.PaddingProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.LetterSpacingProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            if (b.GetValue(AppliedProperty) is { } applied)
                UpdateLetterSpacing(b, applied);
        });
        TextBlock.FontFamilyProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontWeightProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontStyleProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontStretchProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontSizeProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            if (b.GetValue(AppliedProperty) is { } applied)
            {
                UpdateLineHeight(b, applied);
                UpdateLetterSpacing(b, applied);
                UpdatePadding(b, applied);
            }
        });
    }

    public static bool GetBaselineToBaseline(TextBlock block) => block.GetValue(BaselineToBaselineProperty);

    public static void SetBaselineToBaseline(TextBlock block, bool value) => block.SetValue(BaselineToBaselineProperty, value);

    public static double GetMaxHeightLineHeight(TextBlock block) => block.GetValue(MaxHeightLineHeightProperty);

    public static void SetMaxHeightLineHeight(TextBlock block, double value) => block.SetValue(MaxHeightLineHeightProperty, value);

    public static int GetCharacterSpacing(TextBlock block) => block.GetValue(CharacterSpacingProperty);

    public static void SetCharacterSpacing(TextBlock block, int value) => block.SetValue(CharacterSpacingProperty, value);

    /// <summary>The padding for one line's font, size and line height, both above 0. Either edge can be negative.</summary>
    internal static Thickness Padding(double fontSize, double lineHeight, Typeface typeface)
    {
        var metrics = typeface.GlyphTypeface.Metrics;
        var scale = fontSize / metrics.DesignEmHeight;
        var ascent = -metrics.Ascent * scale;
        var line = (metrics.Descent - metrics.Ascent + metrics.LineGap) * scale;
        var baseline = ascent + Math.Max(0, (lineHeight - line) / 2);

        var top = FirstBaseline * fontSize - baseline;
        var bottom = baseline - (1 - DescentShare) * lineHeight;
        return new Thickness(0, top, 0, bottom);
    }

    /// <summary>
    /// The bookkeeping for a change of one of the three, made the first time one is set to a value; null while none
    /// has been, as then there is nothing to set or take back.
    /// </summary>
    private static Applied? Track(TextBlock block, bool set)
    {
        if (block.GetValue(AppliedProperty) is { } applied)
            return applied;
        if (!set)
            return null;

        applied = new Applied();
        block.SetValue(AppliedProperty, applied);
        return applied;
    }

    private static void UpdatePadding(TextBlock block)
    {
        if (block.GetValue(AppliedProperty) is { } applied)
            UpdatePadding(block, applied);
    }

    private static void UpdatePadding(TextBlock block, Applied applied)
    {
        var fontSize = block.FontSize;
        var lineHeight = block.LineHeight;
        if (!block.GetValue(BaselineToBaselineProperty) || !(fontSize > 0) || !(lineHeight > 0))
        {
            Take(block, applied, Slot.Padding);
            return;
        }

        var padding = Padding(fontSize, lineHeight, new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch));
        Put(block, applied, Slot.Padding, TextBlock.PaddingProperty, padding);
    }

    private static void UpdateLineHeight(TextBlock block, Applied applied)
    {
        var lineHeight = block.GetValue(MaxHeightLineHeightProperty);
        if (double.IsNaN(lineHeight))
        {
            Take(block, applied, Slot.LineHeight);
            return;
        }

        Put(block, applied, Slot.LineHeight, TextBlock.LineHeightProperty, Math.Max(lineHeight, FontLine * block.FontSize));
    }

    private static void UpdateLetterSpacing(TextBlock block, Applied applied)
    {
        var spacing = block.GetValue(CharacterSpacingProperty);
        if (spacing == 0)
        {
            Take(block, applied, Slot.LetterSpacing);
            return;
        }

        Put(block, applied, Slot.LetterSpacing, TextBlock.LetterSpacingProperty, spacing * block.FontSize / 1000);
    }

    /// <summary>
    /// Sets <paramref name="value"/> at style priority, unless something else sets the property: the app, an app's
    /// style, or a <c>ControlTheme</c> based on a text style. An immediate value at style priority would beat a
    /// <c>ControlTheme</c>'s setters, so this one gives way to any value it finds, and is put back when that goes. A new
    /// <c>Theme</c> needs no handler of its own (A_Value_A_Derived_ControlTheme_Sets_Wins_Over_The_Styles holds it).
    /// </summary>
    /// <remarks>
    /// A value of this one's that shows is replaced by setting the new one before taking the old one back, so the
    /// property changes once. One that something else hides is taken back first, and the new one set only if nothing
    /// else sets the property: set while hidden, it would go in above an app's untriggered <c>Style</c>, whose
    /// priority it shares, and the later of the two wins.
    /// </remarks>
    private static void Put<T>(TextBlock block, Applied applied, Slot slot, StyledProperty<T> property, T value)
    {
        if (applied.IsBusy(slot))
            return;

        ref var entry = ref applied[slot];
        var current = block.GetValue(property);
        if (entry.Set is not null && Equals(entry.Value, value) && Equals(current, value))
            return;

        applied.SetBusy(slot, true);
        try
        {
            var previous = entry.Set;
            if (previous is not null && Equals(current, entry.Value))
            {
                entry = (block.SetValue(property, value, BindingPriority.Style), value);
                previous.Dispose();
                return;
            }

            entry = default;
            previous?.Dispose();
            if (!block.IsSet(property))
                entry = (block.SetValue(property, value, BindingPriority.Style), value);
        }
        finally
        {
            applied.SetBusy(slot, false);
        }
    }

    private static void Take(TextBlock block, Applied applied, Slot slot)
    {
        if (applied.IsBusy(slot))
            return;

        ref var entry = ref applied[slot];
        if (entry.Set is not { } previous)
            return;

        applied.SetBusy(slot, true);
        try
        {
            entry = default;
            previous.Dispose();
        }
        finally
        {
            applied.SetBusy(slot, false);
        }
    }

    private enum Slot
    {
        Padding,
        LineHeight,
        LetterSpacing,
    }

    /// <summary>A value set at style priority, with the handle that takes it back, for each slot.</summary>
    private sealed class Applied
    {
        private readonly (IDisposable? Set, object? Value)[] _entries = new (IDisposable?, object?)[3];
        private int _busy;

        public ref (IDisposable? Set, object? Value) this[Slot slot] => ref _entries[(int)slot];

        /// <summary>
        /// While this sets or takes back a slot's value, the change that raises isn't handled again for that slot,
        /// and only for that slot: a <c>LineHeight</c> this sets or takes back still works the <c>Padding</c> out again.
        /// </summary>
        public bool IsBusy(Slot slot) => (_busy & (1 << (int)slot)) != 0;

        public void SetBusy(Slot slot, bool busy) => _busy = busy ? _busy | 1 << (int)slot : _busy & ~(1 << (int)slot);
    }
}
