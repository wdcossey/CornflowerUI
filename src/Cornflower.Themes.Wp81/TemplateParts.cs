using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// A theme behaviour's hold on its control's template parts, found by name: the parts of the template already applied
/// when the behaviour is turned on, then each new template's as it is applied, until it is disposed. Used by
/// <see cref="PhoneProgressBar"/>, <see cref="PhoneProgressRing"/> and <see cref="PhoneToggleSwitch"/>.
/// </summary>
/// <remarks>
/// <para>The behaviour gives two things. <c>take</c> reads the parts it needs through a lookup by name, which gives
/// null for a name the template doesn't hold. <c>release</c> lets go of what it took: it runs before each new
/// template's parts are taken, and once when this is disposed. It doesn't run before the first take.</para>
/// <para>A new template's parts come from <see cref="TemplateAppliedEventArgs.NameScope"/>, the template's own. A theme
/// that turns the behaviour on after the template is applied (one theme replacing another, or the behaviour turned off
/// and on) gets no <c>TemplateApplied</c> for it, so the parts of the template already applied are read from the
/// control's visual descendants (<see cref="Applied"/>; <c>GetTemplateChildren</c> isn't public in Avalonia 12.1.4).</para>
/// </remarks>
internal sealed class TemplateParts : IDisposable
{
    private readonly TemplatedControl _owner;
    private readonly Action<Func<string, Control?>> _take;
    private readonly Action _release;
    private bool _disposed;

    private TemplateParts(TemplatedControl owner, Action<Func<string, Control?>> take, Action release)
    {
        _owner = owner;
        _take = take;
        _release = release;
    }

    /// <summary>
    /// Takes the parts of <paramref name="owner"/>'s template now, if it has one applied (with none, every name gives
    /// null), and each new template's as it is applied, releasing the old ones first.
    /// </summary>
    public static TemplateParts Watch(TemplatedControl owner, Action<Func<string, Control?>> take, Action release)
    {
        var parts = new TemplateParts(owner, take, release);
        owner.TemplateApplied += parts.OnTemplateApplied;
        take(Applied(owner));
        return parts;
    }

    /// <summary>
    /// A lookup of the template already applied to <paramref name="owner"/>: its visual descendants that it is the
    /// <see cref="StyledElement.TemplatedParent"/> of, the first in the visual tree's order for each name. A control in
    /// a nested templated control's own template, or in the owner's content, isn't one of the owner's parts, whatever
    /// its name, as it isn't in the template's name scope.
    /// </summary>
    internal static Func<string, Control?> Applied(TemplatedControl owner)
    {
        var named = new Dictionary<string, Control>();
        foreach (var control in owner.GetVisualDescendants().OfType<Control>())
        {
            if (control.Name is { } name && control.TemplatedParent == owner)
            {
                named.TryAdd(name, control);
            }
        }

        return name => named.GetValueOrDefault(name);
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        // Disposed by an earlier handler of this same TemplateApplied: the event still calls this one.
        if (_disposed)
        {
            return;
        }

        _release();
        _take(name => e.NameScope.Find<Control>(name));
    }

    /// <summary>Stops watching for new templates and releases the parts taken. Disposing again does nothing.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _owner.TemplateApplied -= OnTemplateApplied;
        _release();
    }
}
