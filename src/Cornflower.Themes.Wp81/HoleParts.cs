using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// Keeps Wp81's Button, ToggleButton, RepeatButton, CheckBox and TextBox out of templates written for the theme under
/// Wp81. Simple's templates for CalendarItem, CalendarDatePicker, CommandBar, NavigationPage, PipsPager and the managed file
/// dialogs hold plain Buttons with no theme of their own, ComboBox's and DrawerPage's plain ToggleButtons, ScrollBar's
/// and the menu ScrollViewer's plain RepeatButtons, the managed file dialog's plain CheckBox, and the plain TextBoxes of
/// AutoCompleteBox, CalendarDatePicker, ComboBox, the managed file dialog and NumericUpDown, so they would take
/// Wp81's, keyed by type: the phone's sizes (a Button's 109 × 57.5 minimum), its overhang, the accent and, for a Button
/// or a CheckBox, the tilt, in chrome laid out for Simple's; for a TextBox, the phone's black text and white box over
/// the brushes those templates pass it. Such a control takes the theme under Wp81 for its own style key instead: a
/// ToggleButton the ToggleButton theme, not the Button theme.
/// </summary>
/// <remarks>
/// <para><b>Which templates.</b> The templated parent's own template decides: a control falls back when that template
/// is one a <c>ControlTheme</c> under Wp81 sets (in its setters or its nested styles) and none of Wp81's does. So it
/// is the theme the parent wears that counts, not its type: an app's subclass with a theme of its own, an app's theme
/// for a control Wp81 doesn't theme, and a control Wp81 themes all keep Wp81's Button (or TextBox) in their templates, and a
/// control given a theme from under Wp81 explicitly falls back. A new template (a new theme, or the theme stack
/// changing at run time) makes new parts, which are checked as they are styled.</para>
/// <para><b>Under Wp81</b> is Avalonia's lookup order: the styles before <see cref="Wp81Theme"/> in the
/// <see cref="Styles"/> that holds it, a nested group's included, then what holds that group, then each outer host's
/// resources and styles. With nothing under Wp81, nothing falls back. Wp81's own templates are checked first, so a
/// Button in a template Wp81 sets goes no further. The templates each theme (a <see cref="Styles"/> subclass) sets
/// are read once and kept beside it; a plain group of styles and a host's own resources, which an app can change as
/// it runs, are read each time.</para>
/// <para><b>On and off.</b> Only Wp81's themes set <see cref="FallBackProperty"/> (Button's, ToggleButton's,
/// RepeatButton's, CheckBox's and TextBox's): it is internal, with no accessor. It goes false when that theme leaves
/// the control, which is what falling back does itself, so false undoes nothing; the control keeps the theme it was
/// given. It costs only the controls whose theme sets it, and no global rule (<c>StyleBudgetTests</c>); one with no
/// templated parent returns at once.</para>
/// </remarks>
internal static class HoleParts
{
    /// <summary>
    /// Set by Wp81's themes for a Button and its subclasses, and a TextBox: whether the control is in a template written
    /// for the theme under Wp81.
    /// </summary>
    public static readonly AttachedProperty<bool> FallBackProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("FallBack", typeof(HoleParts));

    /// <summary>Each theme's templates, read once, kept beside the theme.</summary>
    private static readonly ConditionalWeakTable<object, HashSet<object>> Templates = new();

    static HoleParts()
    {
        FallBackProperty.Changed.AddClassHandler<TemplatedControl>((control, e) =>
        {
            if (e.GetNewValue<bool>()) Check(control);
        });
    }

    private static void Check(TemplatedControl control)
    {
        if (control.TemplatedParent is not TemplatedControl { Template: { } template } || control.IsSet(StyledElement.ThemeProperty))
            return;

        var (wp81, below) = Under(control);
        if (wp81 is null || TemplatesOf(wp81).Contains(template))
            return;

        var variant = control.ActualThemeVariant;
        foreach (var node in below)
        {
            if (!TemplatesOf(node).Contains(template))
                continue;

            if (ThemeFor(control.StyleKey, below, variant) is { } fallback)
                control.Theme = fallback;
            return;
        }
    }

    /// <summary>
    /// The first <c>ControlTheme</c> under Wp81 keyed by <paramref name="key"/>, the control's style key, and targeting
    /// it: for a ToggleButton the ToggleButton theme, never the Button theme.
    /// </summary>
    private static ControlTheme? ThemeFor(Type key, List<object> below, ThemeVariant variant)
    {
        foreach (var node in below)
        {
            if (node is IResourceNode resources && resources.TryGetResource(key, variant, out var found) &&
                found is ControlTheme { TargetType: var target } theme && target == key)
                return theme;
        }

        return null;
    }

    /// <summary>The <see cref="Wp81Theme"/> <paramref name="element"/> is styled by, and what lies under it, nearest first.</summary>
    private static (Wp81Theme? Wp81, List<object> Below) Under(StyledElement element)
    {
        var below = new List<object>();
        Wp81Theme? wp81 = null;

        for (IStyleHost? host = element; host is not null; host = host.StylingParent)
        {
            if (wp81 is not null)
            {
                // An outer host: its resources, then its styles, as Avalonia looks them up.
                if (host is IResourceNode outer) below.Add(outer);
                if (host.IsStylesInitialized) below.Add(host.Styles);
                continue;
            }

            if (host.IsStylesInitialized)
                wp81 = Find(host.Styles, below);
        }

        return (wp81, below);
    }

    /// <summary>
    /// The last <see cref="Wp81Theme"/> in <paramref name="styles"/>, nested groups included; and into
    /// <paramref name="below"/>, everything before it, nearest first.
    /// </summary>
    private static Wp81Theme? Find(IReadOnlyList<IStyle> styles, List<object> below)
    {
        for (var i = styles.Count - 1; i >= 0; i--)
        {
            var found = styles[i] switch
            {
                Wp81Theme theme => theme,
                Styles group => Find(group, below),
                _ => null,
            };
            if (found is null)
                continue;

            for (var j = i - 1; j >= 0; j--)
                below.Add(styles[j]);
            return found;
        }

        return null;
    }

    /// <summary>Every template a <c>ControlTheme</c> in <paramref name="node"/> sets, read once per node.</summary>
    private static HashSet<object> TemplatesOf(object node)
    {
        if (Templates.TryGetValue(node, out var known))
            return known;

        var found = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Collect(node, found, new HashSet<object>(ReferenceEqualityComparer.Instance));

        // A theme (Wp81Theme, SimpleTheme, any Styles subclass) is read once. What an app holds and can change
        // as it runs, a plain group of styles or a host's own resources, is read each time: it is small.
        if (node is Styles theme && theme.GetType() != typeof(Styles))
            Templates.AddOrUpdate(node, found);
        return found;
    }

    private static void Collect(object? node, HashSet<object> found, HashSet<object> seen)
    {
        if (node is null || !seen.Add(node))
            return;

        switch (node)
        {
            case Wp81Theme when seen.Count > 1:
                return;                                          // another Wp81 inside what is under this one
            case ResourceInclude include:
                Collect(include.Loaded, found, seen);
                return;
            case ControlTheme theme:
                CollectTemplates(theme, found);
                return;
            case IResourceDictionary dictionary:
                foreach (var key in dictionary.Keys)
                {
                    if (dictionary.TryGetValue(key, out var value)) Collect(value, found, seen);
                }

                foreach (var variant in dictionary.ThemeDictionaries.Values)
                    Collect(variant, found, seen);
                foreach (var merged in dictionary.MergedDictionaries)
                    Collect(merged, found, seen);
                return;
            case Styles styles:
                Collect(styles.Resources, found, seen);
                foreach (var style in styles)
                    Collect(style, found, seen);
                return;
            case StyledElement element:
                Collect(element.Resources, found, seen);
                return;
            case Application application:
                Collect(application.Resources, found, seen);
                return;
            case StyleBase style:
                Collect(style.Resources, found, seen);
                return;
        }
    }

    private static void CollectTemplates(StyleBase style, HashSet<object> found)
    {
        foreach (var setter in style.Setters)
        {
            if (setter is Setter { Value: IControlTemplate template } templateSetter && templateSetter.Property == TemplatedControl.TemplateProperty)
                found.Add(template);
        }

        foreach (var child in style.Children)
        {
            if (child is StyleBase nested) CollectTemplates(nested, found);
        }
    }
}
