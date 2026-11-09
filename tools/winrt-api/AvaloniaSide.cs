using System.Reflection;

namespace WinRtApi;

/// <summary>
/// The Avalonia column: the Avalonia control of the same name, if there is one, and for each WinRT
/// member, part and state, its Avalonia equivalent by the mapping rules in docs/ARCHITECTURE.md.
/// Read by reflection from the Avalonia this tool is built against.
/// </summary>
internal sealed class AvaloniaSide
{
    /// <summary>Avalonia's assembly of controls.</summary>
    private static readonly Assembly Controls = typeof(Avalonia.Controls.Control).Assembly;

    /// <summary>
    /// The draft rules for visual states (docs/ARCHITECTURE.md, "Visual states"): a state, or each
    /// part of a combined one (<c>CheckedPointerOver</c>), to a pseudoclass; an empty string for a
    /// state that is the theme's plain setters.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> StateRules = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Normal"] = "",
        ["PointerOver"] = ":pointerover",
        ["Pressed"] = ":pressed",
        ["Disabled"] = ":disabled",
        ["Checked"] = ":checked",
        ["Unchecked"] = ":unchecked",
        ["Indeterminate"] = ":indeterminate",
        ["Focused"] = ":focus-visible",
        ["PointerFocused"] = ":focus",
        ["Unfocused"] = "",
        ["Selected"] = ":selected",
        ["Unselected"] = "",
        ["On"] = ":checked",
        ["Off"] = ":unchecked",
        ["Dragging"] = ":dragging",
    };

    private readonly Type? _type;
    private readonly HashSet<string> _pseudoClasses = new(StringComparer.Ordinal);
    private readonly List<string> _parts = [];

    /// <param name="name">The WinRT control's name without its namespace.</param>
    public AvaloniaSide(string name)
    {
        _type = FindType(name);
        for (var type = _type; type is not null; type = type.BaseType)
        {
            foreach (var attribute in type.GetCustomAttributesData())
            {
                switch (attribute.AttributeType.Name)
                {
                    case "PseudoClassesAttribute":
                        foreach (var argument in attribute.ConstructorArguments)
                        {
                            if (argument.Value is IEnumerable<CustomAttributeTypedArgument> values)
                            {
                                _pseudoClasses.UnionWith(values.Select(v => (string)v.Value!));
                            }
                        }

                        break;
                    case "TemplatePartAttribute" when attribute.ConstructorArguments.Count > 0:
                        _parts.Add((string)attribute.ConstructorArguments[0].Value!);
                        break;
                }
            }
        }
    }

    /// <summary>Avalonia's type of that name in <c>Avalonia.Controls</c>, then <c>.Primitives</c>.</summary>
    private static Type? FindType(string name) =>
        Controls.GetExportedTypes()
            .Where(t => t.Name == name && t.Namespace is "Avalonia.Controls" or "Avalonia.Controls.Primitives")
            .OrderBy(t => t.Namespace == "Avalonia.Controls" ? 0 : 1)
            .FirstOrDefault();

    /// <summary>Avalonia's control, or null for a control Avalonia doesn't have.</summary>
    public Type? Type => _type;

    /// <summary>The pseudoclasses Avalonia's control declares it sets, its base types' included.</summary>
    public IReadOnlyCollection<string> PseudoClasses => _pseudoClasses;

    /// <summary>The template parts Avalonia's control declares it looks for.</summary>
    public IReadOnlyList<string> Parts => _parts;

    /// <summary>
    /// <c>Button.IsPressed</c> if Avalonia's control has a public property of that name, its base types'
    /// included; for an attached property (<c>ScrollViewer.HorizontalScrollBarVisibility</c>), the same
    /// on Avalonia's type of the owner's name, as a static <c>…Property</c> field; else null.
    /// </summary>
    public string? Property(string name)
    {
        var dot = name.IndexOf('.');
        if (dot > 0)
        {
            var owner = FindType(name[..dot]);
            var attached = name[(dot + 1)..];
            return owner?.GetField(attached + "Property", BindingFlags.Public | BindingFlags.Static) is not null
                ? $"{owner.Name}.{attached} (attached)"
                : null;
        }

        return _type?.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { } property ? $"{property.DeclaringType!.Name}.{property.Name}" : null;
    }

    /// <summary><c>Button.Click</c> if Avalonia's control has a public event of that name; else null.</summary>
    public string? Event(string name) =>
        _type?.GetEvent(name, BindingFlags.Public | BindingFlags.Instance) is { } found ? $"{found.DeclaringType!.Name}.{found.Name}" : null;

    /// <summary><c>Button.OnClick()</c> if Avalonia's control has a public or protected method of that name; else null.</summary>
    public string? Method(string name) =>
        _type?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == name && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly)) is { } method
            ? $"{method.DeclaringType!.Name}.{method.Name}()"
            : null;

    /// <summary>The part Avalonia's control looks for under the WinRT part's name, as is or as <c>PART_</c> and the name.</summary>
    public string? Part(string name) =>
        _parts.FirstOrDefault(p => p == name || p == "PART_" + name);

    /// <summary>
    /// A state's pseudoclasses by <see cref="StateRules"/>, joined (<c>:checked:pointerover</c>);
    /// empty for plain setters; null when no rule covers it. A state of a control Avalonia doesn't
    /// have becomes a pseudoclass named after it, in lower case, when no rule covers it.
    /// </summary>
    public string? State(string state)
    {
        if (Decompose(state) is { } parts)
        {
            return string.Concat(parts);
        }

        return _type is null ? ":" + state.ToLowerInvariant() : null;
    }

    /// <summary>The pseudoclasses in <paramref name="selector"/> that Avalonia's control doesn't declare it sets.</summary>
    public IReadOnlyList<string> NotRaised(string selector) =>
        _type is null
            ? []
            : selector.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(p => ":" + p).Where(p => !_pseudoClasses.Contains(p)).ToList();

    /// <summary>The state as a run of rule names, longest first, or null if it isn't one.</summary>
    private static List<string>? Decompose(string state)
    {
        if (state.Length == 0)
        {
            return [];
        }

        foreach (var rule in StateRules.Keys.OrderByDescending(k => k.Length))
        {
            if (state.StartsWith(rule, StringComparison.Ordinal) && Decompose(state[rule.Length..]) is { } rest)
            {
                return [StateRules[rule], .. rest];
            }
        }

        return null;
    }
}
