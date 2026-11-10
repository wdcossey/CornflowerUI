using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Cornflower.Controls;

/// <summary>
/// One Lucide icon (lucide.dev), stroked in the control's <see cref="TemplatedControl.Foreground"/>.
/// Cornflower's stand-in for WinRT's <c>SymbolIcon</c>, whose Segoe UI Symbol glyphs can't ship:
/// <c>tools/lucide/symbols.tsv</c> gives the icon for each <c>Symbol</c> that has one.
/// </summary>
/// <remarks>
/// Behaviour only: the control turns <see cref="Kind"/> into <see cref="Data"/>, the icon's path in
/// Lucide's 24-unit box. A theme's template draws it, and decides its size and its pen.
/// </remarks>
/// <example><c>&lt;cf:LucideIcon Kind="Settings" /&gt;</c></example>
public class LucideIcon : TemplatedControl
{
    /// <summary>Defines <see cref="Kind"/>.</summary>
    public static readonly StyledProperty<LucideIconKind?> KindProperty =
        AvaloniaProperty.Register<LucideIcon, LucideIconKind?>(nameof(Kind), validate: kind => kind is null || Enum.IsDefined(kind.Value));

    /// <summary>Defines <see cref="Data"/>.</summary>
    public static readonly DirectProperty<LucideIcon, Geometry?> DataProperty =
        AvaloniaProperty.RegisterDirect<LucideIcon, Geometry?>(nameof(Data), icon => icon.Data);

    private Geometry? _data;

    /// <summary>The icon. Null, the default, draws nothing.</summary>
    public LucideIconKind? Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>The icon's path in Lucide's 24-unit box, for the template to stroke; null when <see cref="Kind"/> is.</summary>
    public Geometry? Data
    {
        get => _data;
        private set => SetAndRaise(DataProperty, ref _data, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
        {
            Data = change.GetNewValue<LucideIconKind?>() is { } kind ? LucideIcons.Get(kind) : null;
        }
    }
}
