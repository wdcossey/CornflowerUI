using Avalonia;

namespace Cornflower.Controls;

/// <summary>
/// The values a <see cref="ProgressRing"/>'s template binds to, worked out from the ring's laid-out size, as WinRT's
/// <c>ProgressRingTemplateSettings</c> are. Read-only to everything but the ring.
/// </summary>
/// <remarks>
/// The rule is the one the 8.1 Update 1 runtime follows on all seven of its emulator profiles (the reference app's
/// native readings, <c>tools/refapp/README.md</c>, "ProgressRing"): <see cref="MaxSideLength"/> is the smaller of the
/// ring's width and height, <see cref="EllipseDiameter"/> a tenth of it, and <see cref="EllipseOffset"/> a
/// <see cref="Thickness"/> whose top is four tenths of it and whose other sides are 0.
/// </remarks>
public sealed class ProgressRingTemplateSettings : AvaloniaObject
{
    /// <summary>Defines <see cref="EllipseDiameter"/>.</summary>
    public static readonly DirectProperty<ProgressRingTemplateSettings, double> EllipseDiameterProperty =
        AvaloniaProperty.RegisterDirect<ProgressRingTemplateSettings, double>(nameof(EllipseDiameter), s => s.EllipseDiameter);

    /// <summary>Defines <see cref="EllipseOffset"/>.</summary>
    public static readonly DirectProperty<ProgressRingTemplateSettings, Thickness> EllipseOffsetProperty =
        AvaloniaProperty.RegisterDirect<ProgressRingTemplateSettings, Thickness>(nameof(EllipseOffset), s => s.EllipseOffset);

    /// <summary>Defines <see cref="MaxSideLength"/>.</summary>
    public static readonly DirectProperty<ProgressRingTemplateSettings, double> MaxSideLengthProperty =
        AvaloniaProperty.RegisterDirect<ProgressRingTemplateSettings, double>(nameof(MaxSideLength), s => s.MaxSideLength);

    private double _ellipseDiameter;
    private Thickness _ellipseOffset;
    private double _maxSideLength;

    internal ProgressRingTemplateSettings()
    {
    }

    /// <summary>A tenth of <see cref="MaxSideLength"/>.</summary>
    public double EllipseDiameter
    {
        get => _ellipseDiameter;
        private set => SetAndRaise(EllipseDiameterProperty, ref _ellipseDiameter, value);
    }

    /// <summary>Four tenths of <see cref="MaxSideLength"/> at the top, 0 on the other sides.</summary>
    public Thickness EllipseOffset
    {
        get => _ellipseOffset;
        private set => SetAndRaise(EllipseOffsetProperty, ref _ellipseOffset, value);
    }

    /// <summary>The smaller of the ring's laid-out width and height; 0 before it is laid out.</summary>
    public double MaxSideLength
    {
        get => _maxSideLength;
        private set => SetAndRaise(MaxSideLengthProperty, ref _maxSideLength, value);
    }

    /// <summary>Sets every value from the ring's laid-out size.</summary>
    internal void Update(Size size)
    {
        var side = Math.Max(0, Math.Min(size.Width, size.Height));
        MaxSideLength = side;
        EllipseDiameter = side / 10;
        EllipseOffset = new Thickness(0, side * 0.4, 0, 0);
    }
}
