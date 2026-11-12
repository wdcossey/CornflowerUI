using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Threading;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What <see cref="PhoneTextLine"/> and <see cref="PhonePasswordKeyboard"/> share about when they work their values out
/// on one control: a pass, run now or from the dispatcher; a change another handler makes in the middle of it, worked
/// out again after it; and the change a pass makes itself, left alone. What a pass sets, and which properties it looks
/// at, is each behaviour's own, in <see cref="Pass"/>.
/// </summary>
/// <remarks>
/// <para><b>When a value goes rather than comes</b> (a change at <c>Unset</c> or <c>Inherited</c> priority: a theme
/// going, a value cleared), the pass runs from the dispatcher, not inside the change. That is when Avalonia's value store
/// is unsetting a theme's values one by one, and a value taken away inside it can stop it unsetting the rest (spike 6,
/// docs/spikes). It is posted at <c>Normal</c>, above <c>Render</c> and every priority layout and render run at in
/// Avalonia 12.1.4, so the next layout sees it done. One pass is posted at a time, by one cached action: a change while
/// it waits adds what it moves to it, and a change after it was aborted posts again.</para>
/// <para><typeparamref name="TSlots"/> is the behaviour's own <c>[Flags]</c> enum over <c>int</c>: what a change can
/// move, or asks to look at again. A pass is given what it is to work out; changes that come in the middle of it are
/// gathered for the next, a few passes at most.</para>
/// </remarks>
internal abstract class DeferredPass<TSlots> where TSlots : struct, Enum
{
    private const int MaxPasses = 4;

    private readonly Action _run;

    // Not private: TextBlockTests.A_Pass_That_Never_Ran_Doesnt_Stop_The_Next reads it by reflection, from the derived
    // type, which doesn't see a base's private fields.
    private protected DispatcherOperation? _posted;
    private TSlots _deferred;

    // While a pass runs, the changes it raises itself aren't handled again, and other code's are gathered here.
    private bool _updating;
    private bool _again;
    private TSlots _dirty;

    protected DeferredPass() => _run = () =>
    {
        var slots = _deferred;
        _deferred = default;
        Run(slots);
    };

    /// <summary>The property the pass is setting or taking back, whose change is its own: set around each.</summary>
    public AvaloniaProperty? Setting;

    /// <summary>Works out <paramref name="slots"/>: one pass of the behaviour's own, for <see cref="Run"/>.</summary>
    protected abstract void Pass(TSlots slots);

    /// <summary>
    /// Whether a change of <paramref name="property"/>, which moves <paramref name="slots"/>, came in the middle of a pass,
    /// and so is dealt with: the pass's own change is left alone, and another's is worked out again after it.
    /// </summary>
    public bool InPass(AvaloniaProperty property, TSlots slots)
    {
        if (!_updating)
            return false;

        if (property != Setting)
        {
            _dirty = Or(_dirty, slots);
            _again = true;
        }

        return true;
    }

    /// <summary>Works <paramref name="slots"/> out now, or from the dispatcher when a value goes (<c>Unset</c> or <c>Inherited</c>).</summary>
    public void Handle(BindingPriority priority, TSlots slots)
    {
        if (priority is BindingPriority.Unset or BindingPriority.Inherited)
            Defer(slots);
        else
            Run(slots);
    }

    /// <summary>
    /// Works <paramref name="slots"/> out in a pass from the dispatcher, at <c>Normal</c> priority, which runs before
    /// layout and render. One pass at a time; a later change posts again if the last one was aborted.
    /// </summary>
    private void Defer(TSlots slots)
    {
        _deferred = Or(_deferred, slots);
        if (_posted is not { Status: DispatcherOperationStatus.Pending })
            _posted = Dispatcher.UIThread.InvokeAsync(_run, DispatcherPriority.Normal);
    }

    /// <summary>
    /// Works out <paramref name="slots"/>; again for what a change made by other code in the middle of it moved, a few
    /// times at most.
    /// </summary>
    private void Run(TSlots slots)
    {
        _updating = true;
        try
        {
            for (var pass = 0; pass < MaxPasses; pass++)
            {
                _again = false;
                _dirty = default;
                Pass(slots);
                if (!_again)
                    break;
                slots = _dirty;
            }
        }
        finally
        {
            _updating = false;
            _again = false;
            _dirty = default;
        }
    }

    private static TSlots Or(TSlots a, TSlots b) =>
        Unsafe.BitCast<int, TSlots>(Unsafe.BitCast<TSlots, int>(a) | Unsafe.BitCast<TSlots, int>(b));
}
