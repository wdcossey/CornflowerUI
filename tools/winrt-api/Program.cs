// Lists the XAML control types in a WinRT metadata file (Windows.winmd).
// Usage: dotnet run --project tools/winrt-api -- <Windows.winmd> [generic.xaml]
// Reads the file as data only. Prints Markdown: every public, non-abstract type under
// Windows.UI.Xaml.Controls (and .Primitives) with its base type, and whether generic.xaml
// carries a default style for it. Needs the .NET SDK; System.Reflection.Metadata ships in it.
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: <Windows.winmd> [generic.xaml]"); return 1; }

var styled = new HashSet<string>();
if (args.Length > 1)
{
    var xaml = File.ReadAllText(args[1]);
    foreach (Match m in Regex.Matches(xaml, "<Style [^>]*TargetType=\"(?:[a-z]+:)?([A-Za-z0-9_.]+)\""))
    {
        var line = m.Value;
        if (!line.Contains("x:Key") && !line.Contains("x:Name")) styled.Add(m.Groups[1].Value);
    }
}

using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var md = pe.GetMetadataReader();

string Name(EntityHandle h)
{
    switch (h.Kind)
    {
        case HandleKind.TypeDefinition:
            var td = md.GetTypeDefinition((TypeDefinitionHandle)h);
            return md.GetString(td.Namespace) + "." + md.GetString(td.Name);
        case HandleKind.TypeReference:
            var tr = md.GetTypeReference((TypeReferenceHandle)h);
            return md.GetString(tr.Namespace) + "." + md.GetString(tr.Name);
        default:
            return "?";
    }
}

var rows = new List<(string ns, string name, string baseType, bool isAbstract, bool sealedType)>();
foreach (var handle in md.TypeDefinitions)
{
    var t = md.GetTypeDefinition(handle);
    var ns = md.GetString(t.Namespace);
    if (!ns.StartsWith("Windows.UI.Xaml.Controls")) continue;
    var attrs = t.Attributes;
    if ((attrs & TypeAttributes.VisibilityMask) != TypeAttributes.Public) continue;
    if ((attrs & TypeAttributes.Interface) != 0) continue;
    var name = md.GetString(t.Name);
    if (name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1]) && name.Contains("Statics")) continue;
    var baseType = t.BaseType.IsNil ? "" : Name(t.BaseType);
    if (baseType is "System.Enum" or "System.ValueType" or "System.MulticastDelegate" or "System.Attribute") continue;
    rows.Add((ns, name, baseType, (attrs & TypeAttributes.Abstract) != 0, (attrs & TypeAttributes.Sealed) != 0));
}

// Walk up to decide whether a type is a Control (templated) or something else.
var byName = rows.ToDictionary(r => r.ns + "." + r.name, r => r.baseType);
string Kind(string full)
{
    var seen = 0;
    while (full != "" && seen++ < 20)
    {
        if (full == "Windows.UI.Xaml.Controls.Control") return "Control";
        if (full == "Windows.UI.Xaml.Controls.Panel") return "Panel";
        if (full == "Windows.UI.Xaml.FrameworkElement") return "FrameworkElement";
        if (full == "Windows.UI.Xaml.DependencyObject") return "DependencyObject";
        if (!byName.TryGetValue(full, out var b)) return full.Contains("Windows.UI.Xaml") ? full.Replace("Windows.UI.Xaml.", "") : "other";
        full = b;
    }
    return "other";
}

Console.WriteLine($"Types under Windows.UI.Xaml.Controls in `{Path.GetFileName(args[0])}`: {rows.Count}");
Console.WriteLine();
Console.WriteLine("| Namespace | Type | Base | Kind | Default style |");
Console.WriteLine("|---|---|---|---|---|");
foreach (var r in rows.OrderBy(r => r.ns).ThenBy(r => r.name))
{
    var kind = Kind(r.ns + "." + r.name);
    var style = styled.Contains(r.name) ? "yes" : "";
    Console.WriteLine($"| {r.ns.Replace("Windows.UI.Xaml.Controls", "Controls")} | {r.name}{(r.isAbstract ? " (abstract)" : "")} | {r.baseType.Replace("Windows.UI.Xaml.", "")} | {kind} | {style} |");
}
Console.WriteLine();
var controls = rows.Where(r => Kind(r.ns + "." + r.name) == "Control" && !r.isAbstract).ToList();
Console.WriteLine($"Concrete controls (derive from Control): {controls.Count}; with a default style in generic.xaml: {controls.Count(c => styled.Contains(c.name))}");
Console.WriteLine("Controls without a default style: " + string.Join(", ", controls.Where(c => !styled.Contains(c.name)).Select(c => c.name).OrderBy(n => n)));
Console.WriteLine("Default styles whose target is not a public control type here: " + string.Join(", ", styled.Where(s => !rows.Any(r => r.name == s)).OrderBy(n => n)));
return 0;
