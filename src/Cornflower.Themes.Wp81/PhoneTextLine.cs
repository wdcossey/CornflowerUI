using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;

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
/// sets, by itself, a style or a <c>ControlTheme</c> based on a text style, wins. They are worked out again
/// whenever the <c>TextBlock</c>'s font or sizes change: a local <c>FontSize</c>, or a size an
/// extension pack sets through the type ramp's keys, keeps the phone's line. A class handler does this, rather
/// than a binding in each style, because bindings cost about 4 µs each per <c>TextBlock</c>
/// (<c>TypeRampCostBenchmark</c>), where a handler costs a <c>TextBlock</c> without a text style a table lookup
/// when its font changes.</para>
/// <para><b>When a value goes rather than comes</b> (a change at <c>Unset</c> or <c>Inherited</c> priority: a
/// theme going, a value cleared), the pass runs from the dispatcher, not inside the change. That is when Avalonia's
/// value store is unsetting a theme's values one by one, and a value taken away inside it can stop it unsetting the
/// rest (spike 6, docs/spikes). Until the pass runs, the values this set are stale: read in the same job (a
/// <c>Measure</c> right after <c>Theme = null</c>), they are the old style's. It is posted at <c>Normal</c>, above
/// <c>Render</c> and every priority layout and render run at in Avalonia 12.1.4, so the next layout sees it done
/// (Taking_The_Style_Away_Settles_In_One_Run_Of_Jobs).</para>
/// <para>It writes the three properties because they are the line: a <c>TextBlock</c>'s own layout reads them, and nothing
/// outside it can apply the line during layout instead; a coercion can't tell a value set from none (spike 7,
/// docs/spikes; #81).</para>
/// <para>A pass works out only what the change can move (a <c>CharacterSpacing</c> change, the letter spacing), and
/// looks the face's metrics up only when the face changes. A change other code makes in the middle of a pass is
/// worked out again after it.</para>
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

    // What this has set on each TextBlock that has had one of the three set to a value, to take back when it changes.
    // Beside the TextBlock, not in its value store, so it costs the TextBlock no property; one with no entry has
    // nothing set by this, so a change of its font or sizes stops at the lookup.
    private static readonly ConditionalWeakTable<TextBlock, Applied> Bookkeeping = new();

    static PhoneTextLine()
    {
        AvaloniaProperty[] read =
        [
            BaselineToBaselineProperty, MaxHeightLineHeightProperty, CharacterSpacingProperty,
            TextBlock.FontSizeProperty, TextBlock.LineHeightProperty, TextBlock.FontFamilyProperty,
            TextBlock.FontWeightProperty, TextBlock.FontStyleProperty, TextBlock.FontStretchProperty,
            // Another source of a value this sets (the app's, a derived theme's) takes over, or gives it back.
            TextBlock.PaddingProperty, TextBlock.LetterSpacingProperty,
        ];
        foreach (var property in read)
            property.Changed.AddClassHandler<TextBlock>(OnChanged);
    }

    public static bool GetBaselineToBaseline(TextBlock block) => block.GetValue(BaselineToBaselineProperty);

    public static void SetBaselineToBaseline(TextBlock block, bool value) => block.SetValue(BaselineToBaselineProperty, value);

    public static double GetMaxHeightLineHeight(TextBlock block) => block.GetValue(MaxHeightLineHeightProperty);

    public static void SetMaxHeightLineHeight(TextBlock block, double value) => block.SetValue(MaxHeightLineHeightProperty, value);

    public static int GetCharacterSpacing(TextBlock block) => block.GetValue(CharacterSpacingProperty);

    public static void SetCharacterSpacing(TextBlock block, int value) => block.SetValue(CharacterSpacingProperty, value);

    /// <summary>The padding for one line's font, size and line height; none unless both are sizes. Either edge can be negative.</summary>
    internal static Thickness Padding(double fontSize, double lineHeight, Typeface typeface) =>
        IsSize(fontSize) && IsSize(lineHeight) ? Padding(fontSize, lineHeight, typeface.GlyphTypeface.Metrics) : default;

    private static Thickness Padding(double fontSize, double lineHeight, FontMetrics metrics)
    {
        var scale = fontSize / metrics.DesignEmHeight;
        var ascent = -metrics.Ascent * scale;
        var line = (metrics.Descent - metrics.Ascent + metrics.LineGap) * scale;
        var baseline = ascent + Math.Max(0, (lineHeight - line) / 2);

        var top = FirstBaseline * fontSize - baseline;
        var bottom = baseline - (1 - DescentShare) * lineHeight;
        return new Thickness(0, top, 0, bottom);
    }

    /// <summary>A font size or line height this works with: finite and above 0. NaN is none.</summary>
    private static bool IsSize(double value) => double.IsFinite(value) && value > 0;

    /// <summary>The values a change of <paramref name="property"/> can move.</summary>
    private static Slots SlotsFor(AvaloniaProperty property) =>
        property == TextBlock.FontSizeProperty ? Slots.All
        : property == MaxHeightLineHeightProperty || property == TextBlock.LineHeightProperty ? Slots.LineHeight | Slots.Padding
        : property == CharacterSpacingProperty || property == TextBlock.LetterSpacingProperty ? Slots.LetterSpacing
        : Slots.Padding;

    private static void OnChanged(TextBlock block, AvaloniaPropertyChangedEventArgs change)
    {
        if (!Bookkeeping.TryGetValue(block, out var applied))
        {
            if (!IsSetToAValue(change))
                return;

            applied = new Applied(block);
            Bookkeeping.Add(block, applied);
        }

        var slots = SlotsFor(change.Property);
        if (applied.Updating)
        {
            // Not the change this is making: something else's, raised in the middle of it. Worked out again after it.
            if (change.Property != applied.Setting)
                applied.Dirty |= slots;
            return;
        }

        if (change.Priority is BindingPriority.Unset or BindingPriority.Inherited)
        {
            applied.Defer(slots);
            return;
        }

        Update(block, applied, slots);
    }

    /// <summary>Whether <paramref name="change"/> sets one of the three to something this acts on.</summary>
    private static bool IsSetToAValue(AvaloniaPropertyChangedEventArgs change) =>
        change.Property == BaselineToBaselineProperty ? change.GetNewValue<bool>()
        : change.Property == MaxHeightLineHeightProperty ? IsSize(change.GetNewValue<double>())
        : change.Property == CharacterSpacingProperty && change.GetNewValue<int>() != 0;

    /// <summary>
    /// Works out the values in <paramref name="slots"/>, the line height before the padding, which reads it; again for
    /// what a change made by other code in the middle of it moved, a few times at most.
    /// </summary>
    private static void Update(TextBlock block, Applied applied, Slots slots)
    {
        applied.Updating = true;
        try
        {
            for (var pass = 0; slots != Slots.None && pass < 4; pass++)
            {
                applied.Dirty = Slots.None;
                var fontSize = block.FontSize;

                if ((slots & Slots.LineHeight) != 0)
                {
                    var maxHeight = block.GetValue(MaxHeightLineHeightProperty);
                    if (IsSize(maxHeight))
                        Put(block, applied, ref applied.LineHeight, TextBlock.LineHeightProperty, Math.Max(maxHeight, FontLine * fontSize));
                    else
                        Take(applied, ref applied.LineHeight, TextBlock.LineHeightProperty);
                }

                if ((slots & Slots.LetterSpacing) != 0)
                {
                    var spacing = block.GetValue(CharacterSpacingProperty);
                    if (spacing != 0)
                        Put(block, applied, ref applied.LetterSpacing, TextBlock.LetterSpacingProperty, spacing * fontSize / 1000);
                    else
                        Take(applied, ref applied.LetterSpacing, TextBlock.LetterSpacingProperty);
                }

                if ((slots & Slots.Padding) != 0)
                {
                    var lineHeight = block.LineHeight;
                    if (block.GetValue(BaselineToBaselineProperty) && IsSize(fontSize) && IsSize(lineHeight))
                        Put(block, applied, ref applied.Padding, TextBlock.PaddingProperty, Padding(fontSize, lineHeight, applied.MetricsOf(block)));
                    else
                        Take(applied, ref applied.Padding, TextBlock.PaddingProperty);
                }

                slots = applied.Dirty;
            }
        }
        finally
        {
            applied.Updating = false;
            applied.Dirty = Slots.None;
        }
    }

    /// <summary>
    /// Sets <paramref name="value"/> at style priority, unless something else sets the property: the app, an app's
    /// style, or a <c>ControlTheme</c> based on a text style. An immediate value at style priority would beat a
    /// <c>ControlTheme</c>'s setters, so this one gives way to any value it finds, and is put back when that goes. A new
    /// <c>Theme</c> needs no handler of its own (A_Value_A_Derived_ControlTheme_Sets_Wins_Over_The_Styles holds it).
    /// </summary>
    /// <remarks>
    /// The old value is taken back before the new one is set: only with its own value gone can this see whether
    /// anything else sets the property. Avalonia shows neither which source a value is from nor what is below it, and
    /// an app's untriggered <c>Style</c> with an equal value, or a <c>ControlTheme</c>'s below, would otherwise keep
    /// losing to it. So for a moment the property has the value below (a known gap: a transition on it starts again
    /// from there).
    /// </remarks>
    private static void Put<T>(TextBlock block, Applied applied, ref Entry entry, StyledProperty<T> property, T value)
    {
        if (entry.Set is not null && Equals(entry.Value, value) && Equals(block.GetValue(property), value))
            return;

        Take(applied, ref entry, property);
        if (block.IsSet(property))
            return;

        applied.Setting = property;
        entry = new Entry(block.SetValue(property, value, BindingPriority.Style), value);
        applied.Setting = null;
    }

    private static void Take(Applied applied, ref Entry entry, AvaloniaProperty property)
    {
        if (entry.Set is not { } previous)
            return;

        entry = default;
        applied.Setting = property;
        previous.Dispose();
        applied.Setting = null;
    }

    [Flags]
    private enum Slots
    {
        None = 0,
        LineHeight = 1,
        LetterSpacing = 2,
        Padding = 4,
        All = LineHeight | LetterSpacing | Padding,
    }

    /// <summary>A value set at style priority, with the handle that takes it back.</summary>
    private readonly record struct Entry(IDisposable? Set, object? Value);

    /// <summary>What this has set on one <c>TextBlock</c>, and the pass it has waiting.</summary>
    private sealed class Applied
    {
        private readonly Action _run;
        private DispatcherOperation? _posted;
        private Slots _deferred;
        private (FontFamily Family, FontStyle Style, FontWeight Weight, FontStretch Stretch) _face;
        private FontMetrics _metrics;

        public Entry LineHeight;
        public Entry LetterSpacing;
        public Entry Padding;

        public Applied(TextBlock block) => _run = () =>
        {
            var slots = _deferred;
            _deferred = Slots.None;
            Update(block, this, slots);
        };

        /// <summary>While this works the values out, the changes it raises itself aren't handled again.</summary>
        public bool Updating;

        /// <summary>The property this is setting or taking back, whose change is its own.</summary>
        public AvaloniaProperty? Setting;

        /// <summary>The values a change made by other code during a pass moved.</summary>
        public Slots Dirty;

        /// <summary>
        /// Works <paramref name="slots"/> out in a pass from the dispatcher, at <c>Normal</c> priority, which runs before
        /// layout and render. One pass at a time; a later change posts again if the last one was aborted.
        /// </summary>
        public void Defer(Slots slots)
        {
            _deferred |= slots;
            if (_posted is not { Status: DispatcherOperationStatus.Pending })
                _posted = Dispatcher.UIThread.InvokeAsync(_run, DispatcherPriority.Normal);
        }

        /// <summary>The metrics of the <c>TextBlock</c>'s face, looked up again only when the face changes.</summary>
        public FontMetrics MetricsOf(TextBlock block)
        {
            var face = (block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch);
            if (face != _face)
            {
                _metrics = new Typeface(face.FontFamily, face.FontStyle, face.FontWeight, face.FontStretch).GlyphTypeface.Metrics;
                _face = face;
            }

            return _metrics;
        }
    }
}
