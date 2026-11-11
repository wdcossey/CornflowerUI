using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 6: when a <c>ControlTheme</c> goes, its values are unset one property at a time, and a handler of one of
/// those changes can take a value of another property back. Does every property the theme set still go back?
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: not always. The value store unsets the values left without a source from the property
/// registered last to the first, and stops when a handler leaves fewer values than the index it is at. So a handler
/// of the last such property's change that takes a value of another property away stops the rest being unset: they
/// keep the old theme's values, with no change raised, and a new theme that sets the same values raises none either.
/// A value of a property registered after them, which the theme doesn't set, keeps that from happening.
/// <c>PhoneTextLine</c> keeps its bookkeeping in such a property.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class UnsetPassTests
{
    /// <summary>
    /// The properties of one test, in this order, registered the first time it reads one (the static constructor
    /// makes that exact): after every property the probe already has a value of.
    /// </summary>
    private sealed class Properties<TTest>
    {
        public static readonly StyledProperty<int> Taken = AvaloniaProperty.Register<Border, int>($"Taken{typeof(TTest).Name}");
        public static readonly StyledProperty<int> First = AvaloniaProperty.Register<Border, int>($"First{typeof(TTest).Name}");
        public static readonly StyledProperty<int> Last = AvaloniaProperty.Register<Border, int>($"Last{typeof(TTest).Name}");
        public static readonly StyledProperty<int> Later = AvaloniaProperty.Register<Border, int>($"Later{typeof(TTest).Name}");

        static Properties()
        {
        }
    }

    private sealed class Unguarded;

    private sealed class Guarded;

    /// <summary>
    /// Swaps a theme of two values, whose last property's handler takes a third's value away, for another of the same
    /// values: the changes of the first property, and its value just after the first theme goes.
    /// </summary>
    private static (List<int> Changes, int Between) SwapThemes<TTest>(bool later)
    {
        var probe = new Border();
        using var scene = new Scene(probe);
        Dispatcher.UIThread.RunJobs();

        var (takenProperty, first, last, laterProperty) = (Properties<TTest>.Taken, Properties<TTest>.First, Properties<TTest>.Last, Properties<TTest>.Later);
        IDisposable? taken = null;
        last.Changed.AddClassHandler<Border>((_, e) =>
        {
            if (e.GetNewValue<int>() == 0)
                taken?.Dispose();
        });

        ControlTheme Theme()
        {
            var theme = new ControlTheme(typeof(Border));
            theme.Setters.Add(new Setter(first, 1));
            theme.Setters.Add(new Setter(last, 1));
            return theme;
        }

        probe.Theme = Theme();
        Dispatcher.UIThread.RunJobs();
        taken = probe.SetValue(takenProperty, 1, BindingPriority.Style);
        if (later)
            probe.SetValue(laterProperty, 1);
        var changes = new List<int>();
        probe.PropertyChanged += (_, e) =>
        {
            if (e.Property == first)
                changes.Add(e.GetNewValue<int>());
        };

        probe.Theme = Theme();
        var between = probe.GetValue(first);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, probe.GetValue(takenProperty));
        Assert.Equal(1, probe.GetValue(first));
        return (changes, between);
    }

    [AvaloniaFact]
    public void A_Handler_Of_The_Last_Property_That_Takes_A_Value_Away_Stops_The_Rest_Being_Unset()
    {
        var (changes, between) = SwapThemes<Unguarded>(later: false);

        Assert.Equal(1, between);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void A_Value_Of_A_Property_Registered_After_Them_Keeps_The_Rest_Being_Unset()
    {
        var (changes, between) = SwapThemes<Guarded>(later: true);

        Assert.Equal(0, between);
        Assert.Equal([0, 1], changes);
    }
}
