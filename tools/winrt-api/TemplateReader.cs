using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinRtApi;

/// <summary>A setter in a default style: the property and its value as written.</summary>
internal sealed record StyleSetter(string Property, string Value);

/// <summary>A named element in a template: its name and its element type.</summary>
internal sealed record TemplatePart(string Name, string Element);

/// <summary>What a state does: one animation or setter, as written (<c>ContentPresenter.Foreground = {ThemeResource …}</c>).</summary>
internal sealed record StateChange(string Description);

/// <summary>A visual state, in its group, and what it changes.</summary>
internal sealed record VisualStateInfo(string Group, string Name, IReadOnlyList<StateChange> Changes);

/// <summary>A transition between two states of a group.</summary>
internal sealed record TransitionInfo(string Group, string From, string To, string Duration, IReadOnlyList<StateChange> Changes);

/// <summary>A control's default style, read from <c>generic.xaml</c>.</summary>
internal sealed record DefaultStyle(
    string TargetType,
    IReadOnlyList<StyleSetter> Setters,
    IReadOnlyList<TemplatePart> Parts,
    IReadOnlyList<VisualStateInfo> States,
    IReadOnlyList<TransitionInfo> Transitions,
    IReadOnlyList<string> Resources);

/// <summary>
/// Reads a control's default (unkeyed) style out of a WinRT <c>generic.xaml</c>, as data only: its
/// setters, the named parts of its template, the visual state groups and states with what each
/// changes, the transitions, and every resource key it uses, in the order first used.
/// </summary>
internal static partial class TemplateReader
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Elements that are a template's machinery, not its parts, though they carry an <c>x:Name</c>.</summary>
    private static readonly HashSet<string> NotParts = new(StringComparer.Ordinal)
    {
        "VisualStateGroup", "VisualState", "VisualTransition", "Storyboard",
    };

    [GeneratedRegex(@"\{(?:ThemeResource|StaticResource) ([^}\s]+)\}")]
    private static partial Regex ResourceUse();

    /// <summary>The default style for <paramref name="typeName"/> (its name without a namespace), or null if there's none.</summary>
    public static DefaultStyle? Read(string xaml, string typeName)
    {
        var root = XDocument.Parse(xaml).Root!;
        var style = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Style"
                && e.Attribute(X + "Key") is null
                && TypeName((string?)e.Attribute("TargetType")) == typeName);
        if (style is null)
        {
            return null;
        }

        var setters = new List<StyleSetter>();
        XElement? template = null;
        foreach (var setter in style.Elements().Where(e => e.Name.LocalName == "Setter"))
        {
            var property = (string?)setter.Attribute("Property") ?? "";
            var valueElement = setter.Elements().FirstOrDefault(e => e.Name.LocalName == "Setter.Value")?.Elements().FirstOrDefault();
            if (property == "Template")
            {
                template = valueElement;
                continue;
            }

            setters.Add(new StyleSetter(property, (string?)setter.Attribute("Value") ?? (valueElement is null ? "" : Describe(valueElement))));
        }

        var parts = new List<TemplatePart>();
        var states = new List<VisualStateInfo>();
        var transitions = new List<TransitionInfo>();
        if (template is not null)
        {
            foreach (var element in template.Descendants())
            {
                if (element.Attribute(X + "Name") is { } name && !NotParts.Contains(element.Name.LocalName) && !IsInsideStates(element))
                {
                    parts.Add(new TemplatePart(name.Value, element.Name.LocalName));
                }
            }

            foreach (var group in template.Descendants().Where(e => e.Name.LocalName == "VisualStateGroup"))
            {
                var groupName = (string?)group.Attribute(X + "Name") ?? "";
                foreach (var state in group.Elements().Where(e => e.Name.LocalName == "VisualState"))
                {
                    states.Add(new VisualStateInfo(groupName, (string?)state.Attribute(X + "Name") ?? "", Changes(state)));
                }

                foreach (var transition in group.Descendants().Where(e => e.Name.LocalName == "VisualTransition"))
                {
                    transitions.Add(new TransitionInfo(
                        groupName,
                        (string?)transition.Attribute("From") ?? "*",
                        (string?)transition.Attribute("To") ?? "*",
                        (string?)transition.Attribute("GeneratedDuration") ?? "",
                        Changes(transition)));
                }
            }
        }

        var resources = ResourceUse().Matches(style.ToString())
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new DefaultStyle(typeName, setters, parts, states, transitions, resources);
    }

    private static bool IsInsideStates(XElement element) =>
        element.Ancestors().Any(a => a.Name.LocalName is "VisualStateManager.VisualStateGroups" or "VisualStateGroup");

    /// <summary>A <c>TargetType</c> without its <c>prefix:</c>.</summary>
    private static string TypeName(string? targetType) =>
        targetType is null ? "" : targetType[(targetType.IndexOf(':') + 1)..];

    /// <summary>What a state or transition does: each animation in its storyboard, and each of its setters.</summary>
    private static IReadOnlyList<StateChange> Changes(XElement owner)
    {
        var changes = new List<StateChange>();
        foreach (var storyboard in owner.Descendants().Where(e => e.Name.LocalName == "Storyboard"))
        {
            foreach (var animation in storyboard.Elements())
            {
                changes.Add(new StateChange(DescribeAnimation(animation)));
            }
        }

        foreach (var setter in owner.Elements().Where(e => e.Name.LocalName == "VisualState.Setters").Elements())
        {
            changes.Add(new StateChange($"{(string?)setter.Attribute("Target")} = {(string?)setter.Attribute("Value")}"));
        }

        return changes;
    }

    private static string DescribeAnimation(XElement animation)
    {
        var kind = animation.Name.LocalName;
        var target = (string?)animation.Attribute("Storyboard.TargetName") ?? (string?)animation.Attribute("TargetName") ?? "";
        var property = (string?)animation.Attribute("Storyboard.TargetProperty");
        var subject = property is null ? target : $"{target}.{property}";
        if (kind.EndsWith("ThemeAnimation", StringComparison.Ordinal))
        {
            var extra = string.Join(" ", animation.Attributes()
                .Where(a => a.Name.Namespace != X && a.Name.LocalName is not "TargetName" && !a.Name.LocalName.StartsWith("Storyboard.", StringComparison.Ordinal))
                .Select(a => $"{a.Name.LocalName}={a.Value}"));
            return $"{kind} on {target}{(extra.Length > 0 ? $" ({extra})" : "")}";
        }

        var frames = animation.Elements().Where(e => e.Name.LocalName.EndsWith("KeyFrame", StringComparison.Ordinal)).ToList();
        if (frames.Count == 1 && (string?)frames[0].Attribute("KeyTime") is null or "0" or "0:0:0")
        {
            return $"{subject} = {FrameValue(frames[0])}";
        }

        if (frames.Count > 0)
        {
            return $"{subject}: {kind} " + string.Join(", ", frames.Select(f => $"{(string?)f.Attribute("KeyTime")} → {FrameValue(f)}"));
        }

        var settings = string.Join(" ", animation.Attributes()
            .Where(a => a.Name.Namespace != X && !a.Name.LocalName.StartsWith("Storyboard.", StringComparison.Ordinal))
            .Select(a => $"{a.Name.LocalName}={a.Value}"));
        return $"{subject}: {kind} {settings}".TrimEnd();
    }

    /// <summary>A key frame's value, as an attribute or as an element (<c>&lt;DiscreteObjectKeyFrame.Value&gt;</c>).</summary>
    private static string FrameValue(XElement frame) =>
        (string?)frame.Attribute("Value")
        ?? (frame.Elements().FirstOrDefault(e => e.Name.LocalName.EndsWith(".Value", StringComparison.Ordinal))?.Elements().FirstOrDefault() is { } value
            ? Describe(value)
            : "");

    /// <summary>An element as a value: its type and attributes, on one line.</summary>
    internal static string Describe(XElement element)
    {
        var attributes = string.Join(" ", element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .Select(a => $"{a.Name.LocalName}={a.Value}"));
        var text = element.HasElements ? "" : element.Value.Trim();
        return $"{element.Name.LocalName}{(attributes.Length > 0 ? " " + attributes : "")}{(text.Length > 0 ? " " + text : "")}";
    }
}

/// <summary>
/// The values of a WinRT <c>themeresources.xaml</c>: each key's value in the dark (<c>Default</c>) and
/// light theme dictionaries, or its one value outside them.
/// </summary>
internal sealed partial class ThemeResources
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private readonly Dictionary<string, string> _dark = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _light = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _shared = new(StringComparer.Ordinal);

    public ThemeResources(string xaml)
    {
        var root = XDocument.Parse(xaml).Root!;
        foreach (var dictionary in root.Elements().Where(e => e.Name.LocalName == "ResourceDictionary.ThemeDictionaries").Elements())
        {
            var target = (string?)dictionary.Attribute(X + "Key") switch
            {
                "Default" or "Dark" => _dark,
                "Light" => _light,
                _ => null,
            };
            if (target is null)
            {
                continue;
            }

            foreach (var entry in dictionary.Elements())
            {
                if ((string?)entry.Attribute(X + "Key") is { } key)
                {
                    target[key] = Value(entry);
                }
            }
        }

        foreach (var entry in root.Elements())
        {
            if ((string?)entry.Attribute(X + "Key") is { } key)
            {
                _shared[key] = Value(entry);
            }
        }
    }

    /// <summary>
    /// The key's dark and light values; the same value twice for a key outside the theme dictionaries;
    /// null where it has none. A value that refers to another resource, as a <c>ThemeResource</c> or a
    /// <c>StaticResource</c>, is followed, in the same theme, to what that resolves to:
    /// <c>SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF</c>.
    /// </summary>
    public (string? Dark, string? Light) this[string key] => (Resolve(key, _dark), Resolve(key, _light));

    private string? Resolve(string key, Dictionary<string, string> theme, int depth = 0)
    {
        var value = _shared.TryGetValue(key, out var shared) ? shared : theme.GetValueOrDefault(key);
        if (value is null || depth > 4 || ReferenceUse().Match(value) is not { Success: true } use)
        {
            return value;
        }

        return Resolve(use.Groups[1].Value, theme, depth + 1) is { } next && next != value ? $"{value} → {Last(next)}" : value;
    }

    /// <summary>The end of a chain already followed.</summary>
    private static string Last(string resolved) => resolved[(resolved.LastIndexOf(" → ", StringComparison.Ordinal) is var arrow and >= 0 ? arrow + 3 : 0)..];

    [System.Text.RegularExpressions.GeneratedRegex(@"\{(?:ThemeResource|StaticResource) ([^}\s]+)\}")]
    private static partial System.Text.RegularExpressions.Regex ReferenceUse();

    private static string Value(XElement entry)
    {
        var kind = entry.Name.LocalName;
        var attributes = entry.Attributes()
            .Where(a => !a.IsNamespaceDeclaration && a.Name != X + "Key")
            .ToList();
        if (!entry.HasElements && attributes.Count == 0)
        {
            return entry.Value.Trim();
        }

        if (kind == "SolidColorBrush" && attributes.Count == 1 && attributes[0].Name.LocalName == "Color")
        {
            return $"SolidColorBrush {attributes[0].Value}";
        }

        return string.Join(" ", new[] { kind }.Concat(attributes.Select(a => $"{a.Name.LocalName}={a.Value}")));
    }
}
