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
/// Avalonia's baseline down by the difference.</para>
/// <para><b>MaxHeight.</b> The line is the larger of WinRT's <c>LineHeight</c> and the phone's own line,
/// 1.33 em (ADR 0003).</para>
/// <para><b>CharacterSpacing</b>, in thousandths of an em, is <c>LetterSpacing</c> n × <c>FontSize</c> / 1000
/// (docs/spikes, 5).</para>
/// <para>Each value is set at style priority, and only where nothing else sets the property, so a value the app
/// sets, by itself, a style or a <c>ControlTheme</c> based on a text style, wins. It is
/// worked out again whenever the <c>TextBlock</c>'s font or sizes change: a local <c>FontSize</c>, or a size an
/// extension pack sets through the type ramp's keys, keeps the phone's line. A class handler does this, rather
/// than a binding in each style, because bindings cost about 4 µs each per <c>TextBlock</c>
/// (<c>TypeRampCostBenchmark</c>), where a handler costs a <c>TextBlock</c> without a text style a property read or two when its font changes.</para>
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

    // What each has set, to take back when it changes.
    private static readonly AttachedProperty<Applied?> AppliedProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, Applied?>("Applied", typeof(PhoneTextLine));

    static PhoneTextLine()
    {
        BaselineToBaselineProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.LineHeightProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            UpdateLineHeight(b);
            UpdatePadding(b);
        });
        // Another source of a value this sets (the app's, a derived theme's) takes over, or gives it back.
        TextBlock.PaddingProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.LetterSpacingProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdateLetterSpacing(b));
        StyledElement.ThemeProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            UpdateLineHeight(b);
            UpdateLetterSpacing(b);
            UpdatePadding(b);
        });
        TextBlock.FontFamilyProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontWeightProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontStyleProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        TextBlock.FontStretchProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdatePadding(b));
        MaxHeightLineHeightProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdateLineHeight(b));
        CharacterSpacingProperty.Changed.AddClassHandler<TextBlock>((b, _) => UpdateLetterSpacing(b));
        TextBlock.FontSizeProperty.Changed.AddClassHandler<TextBlock>((b, _) =>
        {
            UpdateLineHeight(b);
            UpdateLetterSpacing(b);
            UpdatePadding(b);
        });
    }

    public static bool GetBaselineToBaseline(TextBlock block) => block.GetValue(BaselineToBaselineProperty);

    public static void SetBaselineToBaseline(TextBlock block, bool value) => block.SetValue(BaselineToBaselineProperty, value);

    public static double GetMaxHeightLineHeight(TextBlock block) => block.GetValue(MaxHeightLineHeightProperty);

    public static void SetMaxHeightLineHeight(TextBlock block, double value) => block.SetValue(MaxHeightLineHeightProperty, value);

    public static int GetCharacterSpacing(TextBlock block) => block.GetValue(CharacterSpacingProperty);

    public static void SetCharacterSpacing(TextBlock block, int value) => block.SetValue(CharacterSpacingProperty, value);

    /// <summary>The padding for one line's font, size and line height. Either edge can be negative.</summary>
    internal static Thickness Padding(double fontSize, double lineHeight, Typeface typeface)
    {
        if (!(fontSize > 0) || !(lineHeight > 0))
            return default;

        var metrics = typeface.GlyphTypeface.Metrics;
        var scale = fontSize / metrics.DesignEmHeight;
        var ascent = -metrics.Ascent * scale;
        var line = (metrics.Descent - metrics.Ascent + metrics.LineGap) * scale;
        var baseline = ascent + Math.Max(0, (lineHeight - line) / 2);

        var top = FirstBaseline * fontSize - baseline;
        var bottom = baseline - (1 - DescentShare) * lineHeight;
        return new Thickness(0, top, 0, bottom);
    }

    private static void UpdatePadding(TextBlock block)
    {
        if (!block.GetValue(BaselineToBaselineProperty))
        {
            Take(block, Slot.Padding);
            return;
        }

        var padding = Padding(block.FontSize, block.LineHeight, new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch));
        Put(block, Slot.Padding, TextBlock.PaddingProperty, padding);
    }

    private static void UpdateLineHeight(TextBlock block)
    {
        var lineHeight = block.GetValue(MaxHeightLineHeightProperty);
        if (double.IsNaN(lineHeight))
        {
            Take(block, Slot.LineHeight);
            return;
        }

        Put(block, Slot.LineHeight, TextBlock.LineHeightProperty, Math.Max(lineHeight, FontLine * block.FontSize));
    }

    private static void UpdateLetterSpacing(TextBlock block)
    {
        var spacing = block.GetValue(CharacterSpacingProperty);
        if (spacing == 0)
        {
            Take(block, Slot.LetterSpacing);
            return;
        }

        Put(block, Slot.LetterSpacing, TextBlock.LetterSpacingProperty, spacing * block.FontSize / 1000);
    }

    /// <summary>
    /// Sets <paramref name="value"/> at style priority, unless something else sets the property: the app, an app's
    /// style, or a <c>ControlTheme</c> based on a text style. An immediate value at style priority would beat a
    /// <c>ControlTheme</c>'s setters, so this one gives way to any value it finds, and is put back when that goes.
    /// </summary>
    private static void Put<T>(TextBlock block, Slot slot, StyledProperty<T> property, T value)
    {
        var applied = block.GetValue(AppliedProperty);
        if (applied is null)
        {
            applied = new Applied();
            block.SetValue(AppliedProperty, applied);
        }

        if (applied.Busy)
            return;

        ref var entry = ref applied[slot];
        if (entry.Set is not null && Equals(entry.Value, value) && Equals(block.GetValue(property), value))
            return;

        applied.Busy = true;
        try
        {
            var previous = entry.Set;
            entry = default;
            previous?.Dispose();
            if (!block.IsSet(property))
                entry = (block.SetValue(property, value, BindingPriority.Style), value);
        }
        finally
        {
            applied.Busy = false;
        }
    }

    private static void Take(TextBlock block, Slot slot)
    {
        if (block.GetValue(AppliedProperty) is not { Busy: false } applied)
            return;

        ref var entry = ref applied[slot];
        if (entry.Set is not { } previous)
            return;

        applied.Busy = true;
        try
        {
            entry = default;
            previous.Dispose();
        }
        finally
        {
            applied.Busy = false;
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

        /// <summary>While this sets or takes back a value, the change it raises isn't handled again.</summary>
        public bool Busy { get; set; }

        public ref (IDisposable? Set, object? Value) this[Slot slot] => ref _entries[(int)slot];
    }
}
