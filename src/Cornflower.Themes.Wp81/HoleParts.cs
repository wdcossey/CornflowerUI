using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// Keeps Wp81's Button out of the templates of controls Wp81 doesn't theme yet, the holes
/// (<c>tests/Cornflower.Tests/Completeness/Holes.txt</c>). Simple's templates for CalendarItem, CalendarDatePicker,
/// CommandBar, NavigationPage, PipsPager and the managed file dialogs hold plain Buttons with no theme of their own,
/// so they would take Wp81's, keyed by type: the phone's 109 × 57.5 minimum, its overhang, the accent and the tilt,
/// in chrome laid out for Simple's. A Button whose templated parent is a hole takes the Button theme of the
/// theme under Wp81 instead, the one its parent's template was written for.
/// </summary>
/// <remarks>
/// <para>Wp81's Button theme turns this on with a setter, so it costs only Buttons, and no global rule
/// (<c>StyleBudgetTests</c>). For a Button an app places, with no templated parent, it returns at once.</para>
/// <para>A templated parent is a hole when Wp81 has no <c>ControlTheme</c> for its type or a base type and the
/// themes under Wp81 do. An app's own templated control, which the theme under Wp81 knows nothing of, keeps
/// Wp81's Button in its template. "Under Wp81" is the lookup order Avalonia uses: the styles before
/// <see cref="Wp81Theme"/> in the host that holds it, then each outer host's resources and styles. With nothing
/// under Wp81, the Button keeps Wp81's theme: a hole has no template then anyway.</para>
/// </remarks>
internal static class HoleParts
{
    /// <summary>Set by Wp81's Button theme: look for a hole as the templated parent.</summary>
    public static readonly AttachedProperty<bool> FallBackProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("FallBack", typeof(HoleParts));

    static HoleParts()
    {
        FallBackProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            if (e.GetNewValue<bool>()) Check(button);
        });
    }

    public static bool GetFallBack(Button button) => button.GetValue(FallBackProperty);

    public static void SetFallBack(Button button, bool value) => button.SetValue(FallBackProperty, value);

    private static void Check(Button button)
    {
        if (button.TemplatedParent is not StyledElement parent || button.IsSet(StyledElement.ThemeProperty))
            return;

        var variant = button.ActualThemeVariant;
        if (Under(button) is not { } under || !IsHole(parent.GetType(), under, variant))
            return;

        if (Find(under.Below, typeof(Button), variant) is { } fallback)
            button.Theme = fallback;
    }

    private static bool IsHole(Type type, (Wp81Theme Wp81, IReadOnlyList<IResourceNode> Below) under, ThemeVariant variant)
    {
        for (var t = type; t is not null && t != typeof(StyledElement); t = t.BaseType)
        {
            if (Has(under.Wp81, t, variant)) return false;
            if (Find(under.Below, t, variant) is not null) return true;
        }

        return false;
    }

    /// <summary>The <see cref="Wp81Theme"/> <paramref name="element"/> is styled by, and what lies under it, nearest first.</summary>
    private static (Wp81Theme Wp81, IReadOnlyList<IResourceNode> Below)? Under(StyledElement element)
    {
        Wp81Theme? wp81 = null;
        var below = new List<IResourceNode>();

        for (IStyleHost? host = element; host is not null; host = host.StylingParent)
        {
            if (wp81 is not null)
            {
                // An outer host: its resources, then its styles, as Avalonia looks them up.
                if (host is IResourceNode outer) below.Add(outer);
                continue;
            }

            var index = host.IsStylesInitialized ? LastIndexOfWp81(host.Styles) : -1;
            if (index < 0)
                continue;

            wp81 = (Wp81Theme)host.Styles[index];
            for (var i = index - 1; i >= 0; i--)
            {
                if (host.Styles[i] is IResourceNode node) below.Add(node);
            }
        }

        return wp81 is null ? null : (wp81, below);
    }

    private static int LastIndexOfWp81(Styles styles)
    {
        for (var i = styles.Count - 1; i >= 0; i--)
        {
            if (styles[i] is Wp81Theme) return i;
        }

        return -1;
    }

    private static ControlTheme? Find(IReadOnlyList<IResourceNode> nodes, Type type, ThemeVariant variant)
    {
        foreach (var node in nodes)
        {
            if (node.TryGetResource(type, variant, out var found) && found is ControlTheme theme && theme.TargetType == type)
                return theme;
        }

        return null;
    }

    private static bool Has(IResourceNode node, Type type, ThemeVariant variant) =>
        node.TryGetResource(type, variant, out var found) && found is ControlTheme { TargetType: var target } && target == type;
}
