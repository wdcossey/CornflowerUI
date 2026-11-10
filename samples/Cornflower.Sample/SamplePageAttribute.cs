using System.Reflection;
using Avalonia.Controls;

namespace Cornflower.Sample;

/// <summary>
/// Lists a page in the sample's catalogue, with no other registration: a control's page is a file
/// added under the sample, and <see cref="SampleShell"/> finds it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SamplePageAttribute(string title) : Attribute
{
    /// <summary>What the catalogue calls the page: the control's name, in lower case, as the phone writes it.</summary>
    public string Title { get; } = title;

    /// <summary>
    /// Where the page goes in the catalogue: the control's issue number, so parallel PRs don't have
    /// to coordinate. A page that omits it sorts last rather than jumping to the top.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;
}

/// <summary>One page in the catalogue: its title, and how to make a new one.</summary>
public sealed record SamplePageEntry(string Title, Func<Control> Create);

/// <summary>Finds the <see cref="SamplePageAttribute"/> pages.</summary>
public static class SamplePageCatalog
{
    /// <summary>Every <see cref="SamplePageAttribute"/> page in <paramref name="assembly"/>, by order then title.</summary>
    public static IReadOnlyList<SamplePageEntry> Discover(Assembly assembly) => Discover(assembly.GetTypes());

    /// <summary>Every <see cref="SamplePageAttribute"/> page among <paramref name="types"/>, by order then title.</summary>
    /// <exception cref="InvalidOperationException">
    /// A type with the attribute isn't a control that can be made with no arguments, or two pages have one title.
    /// </exception>
    public static IReadOnlyList<SamplePageEntry> Discover(IEnumerable<Type> types)
    {
        var pages = types
            .Select(type => (Type: type, Page: type.GetCustomAttribute<SamplePageAttribute>()))
            .Where(found => found.Page is not null)
            .Select(found => (found.Type, Page: found.Page!, Create: Maker(found.Type)))
            .OrderBy(found => found.Page.Order)
            .ThenBy(found => found.Page.Title, StringComparer.Ordinal)
            .ToArray();

        // A page is opened by its title, on the list and by tools/real-window/drive.ps1.
        var twice = pages.GroupBy(found => found.Page.Title, StringComparer.Ordinal).FirstOrDefault(title => title.Count() > 1);
        if (twice is not null)
            throw new InvalidOperationException($"Two pages have the title '{twice.Key}': {string.Join(", ", twice.Select(found => found.Type.FullName))}.");

        return pages.Select(found => new SamplePageEntry(found.Page.Title, found.Create)).ToArray();
    }

    // Checked when the catalogue is built, so a page that can't be shown fails at start, not when it's opened.
    private static Func<Control> Maker(Type type)
    {
        if (!typeof(Control).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException($"{type.FullName} has [SamplePage] but isn't a control with a public constructor that takes no arguments.");

        return () => (Control)Activator.CreateInstance(type)!;
    }
}
