using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The holes: every templated Avalonia control that a theme leaves without a <c>ControlTheme</c>.
/// A templated control is one that Avalonia's Simple theme themes, keyed by its own type, so the
/// list follows Avalonia: an upgrade that adds a control adds a hole.
/// </summary>
/// <remarks>
/// A subclass that keeps its base's style key (<c>MaskedTextBox</c> is a <c>TextBox</c> to a theme)
/// is themed with its base, so only the base is listed. Simple's named themes (the ones keyed by a
/// string, used inside its own templates) aren't controls an app places, and aren't counted.
/// </remarks>
internal static class Holes
{
    /// <summary>The list <c>HolesTests</c> holds the theme to: <c>Completeness/Holes.txt</c>.</summary>
    internal const string ListResource = "Cornflower.Tests.Completeness.Holes.txt";

    /// <summary>
    /// Every public, concrete <see cref="TemplatedControl"/> in the Avalonia assemblies that
    /// <paramref name="simple"/>'s own assembly refers to, that <paramref name="simple"/> themes by its type.
    /// </summary>
    internal static IReadOnlyList<Type> TemplatedControls(Styles simple) =>
        simple.GetType().Assembly.GetReferencedAssemblies()
            .Where(name => name.Name!.StartsWith("Avalonia.", StringComparison.Ordinal))
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsSubclassOf(typeof(TemplatedControl)) && !type.IsAbstract && !type.ContainsGenericParameters)
            .Where(type => HasTheme(simple, type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Whether <paramref name="theme"/> has a <c>ControlTheme</c> keyed by <paramref name="control"/>
    /// in dark and in light. One in a single variant's dictionary leaves the other a hole.
    /// </summary>
    internal static bool HasTheme(IResourceNode theme, Type control) =>
        HasTheme(theme, control, ThemeVariant.Dark) && HasTheme(theme, control, ThemeVariant.Light);

    private static bool HasTheme(IResourceNode theme, Type control, ThemeVariant variant) =>
        theme.TryGetResource(control, variant, out var found) && found is ControlTheme;

    /// <summary>The names of <paramref name="controls"/> that <paramref name="theme"/> doesn't theme.</summary>
    internal static IReadOnlyList<string> In(IResourceNode theme, IEnumerable<Type> controls)
    {
        var names = controls.Where(control => !HasTheme(theme, control)).Select(control => control.Name).ToList();
        var shared = Repeated(names);
        if (shared.Count > 0)
        {
            throw new InvalidOperationException(
                $"Templated controls share a name ({string.Join(", ", shared)}): Holes.txt needs namespaces to tell them apart.");
        }
        return names;
    }

    /// <summary>The names in <c>Holes.txt</c>, as written, without blank lines and <c>#</c> comments.</summary>
    internal static IReadOnlyList<string> Listed()
    {
        using var stream = typeof(Holes).Assembly.GetManifestResourceStream(ListResource)
            ?? throw new InvalidOperationException($"{ListResource} isn't embedded.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    internal static IReadOnlyList<string> Parse(string text) =>
        text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();

    /// <summary>
    /// What's wrong with <paramref name="listed"/> as the list of <paramref name="holes"/>, one line
    /// each, saying what to do; empty when they match.
    /// </summary>
    internal static IReadOnlyList<string> Compare(IReadOnlyList<string> listed, IReadOnlyList<string> holes)
    {
        var problems = new List<string>();
        foreach (var name in holes.Except(listed).Order(StringComparer.Ordinal))
        {
            problems.Add($"{name} has no Wp81 theme: theme it, or add it to Holes.txt.");
        }
        foreach (var name in listed.Except(holes).Order(StringComparer.Ordinal))
        {
            problems.Add($"{name} isn't a hole: delete its line from Holes.txt.");
        }
        foreach (var name in Repeated(listed))
        {
            problems.Add($"{name} is listed more than once in Holes.txt.");
        }
        if (!listed.SequenceEqual(listed.Order(StringComparer.Ordinal)))
        {
            problems.Add("Holes.txt isn't in ordinal order.");
        }
        return problems;
    }

    private static List<string> Repeated(IEnumerable<string> names) =>
        names.GroupBy(name => name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
}
