using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Cornflower.Controls;

/// <summary>
/// An indeterminate progress indicator, WinRT's <c>ProgressRing</c>: while <see cref="IsActive"/>, a theme's template
/// shows that something is happening without saying how far along it is.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour only, as WinRT's control is: it raises its states as pseudoclasses and keeps
/// <see cref="TemplateSettings"/> up to date, and the theme's template draws and animates the ring. The states are
/// WinRT's two groups, each a pair of pseudoclasses: <c>:active</c> or <c>:inactive</c> (<c>ActiveStates</c>), and
/// <c>:large</c> or <c>:small</c> (<c>SizeStates</c>).
/// </para>
/// <para>
/// The size state is the one rule the phone's control decides in its own code, read from the 8.1 Update 1 runtime
/// (<c>-Probe ringsize</c>, run <c>20261111-2316-dev11</c>, <c>tools/refapp/README.md</c>): <c>Large</c> when the
/// ring's smaller laid-out side is over <see cref="LargeSideThreshold"/>, 60, else <c>Small</c>, with no
/// hysteresis. The 8.1 template shows a sixth dot when <c>Large</c>.
/// </para>
/// </remarks>
/// <example><c>&lt;cf:ProgressRing IsActive="{Binding IsBusy}" /&gt;</c></example>
[PseudoClasses(ActivePseudoClass, InactivePseudoClass, LargePseudoClass, SmallPseudoClass)]
public class ProgressRing : TemplatedControl
{
    /// <summary>Defines <see cref="IsActive"/>.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsActive));

    /// <summary>Defines <see cref="HorizontalContentAlignment"/>.</summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<ProgressRing>();

    /// <summary>Defines <see cref="VerticalContentAlignment"/>.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<ProgressRing>();

    /// <summary>
    /// The smaller side, in device-independent pixels, above which the ring is <c>Large</c>: the phone's, measured on
    /// the reference profile.
    /// </summary>
    internal const double LargeSideThreshold = 60;

    private const string ActivePseudoClass = ":active";
    private const string InactivePseudoClass = ":inactive";
    private const string LargePseudoClass = ":large";
    private const string SmallPseudoClass = ":small";

    /// <summary>Initialises a new ring: inactive, and small until it is laid out.</summary>
    public ProgressRing()
    {
        UpdateActive(false);
        UpdateSize(default);
    }

    /// <summary>Whether the ring shows progress. False by default, as WinRT's is.</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Where the template places the ring across the control, as WinRT's <c>Control.HorizontalContentAlignment</c>,
    /// which Avalonia's <see cref="TemplatedControl"/> doesn't have.
    /// </summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>Where the template places the ring down the control, as WinRT's <c>Control.VerticalContentAlignment</c>.</summary>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <summary>The values the template binds to, from the ring's laid-out size.</summary>
    public ProgressRingTemplateSettings TemplateSettings { get; } = new();

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsActiveProperty)
        {
            UpdateActive(change.GetNewValue<bool>());
        }
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateSize(e.NewSize);
    }

    private void UpdateActive(bool active)
    {
        PseudoClasses.Set(ActivePseudoClass, active);
        PseudoClasses.Set(InactivePseudoClass, !active);
    }

    private void UpdateSize(Size size)
    {
        TemplateSettings.Update(size);
        var large = TemplateSettings.MaxSideLength > LargeSideThreshold;
        PseudoClasses.Set(LargePseudoClass, large);
        PseudoClasses.Set(SmallPseudoClass, !large);
    }
}
