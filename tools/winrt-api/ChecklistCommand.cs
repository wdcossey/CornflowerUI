namespace WinRtApi;

/// <summary>The <c>checklist</c> command: reads the three spec files and the theme's tokens, and renders one control's checklist.</summary>
internal static class ChecklistCommand
{
    private const string Controls = "Windows.UI.Xaml.Controls";
    private const string DefaultStop = Controls + ".Control";

    public static string Run(string type, string winmd, string design, string? stop, string? tokens)
    {
        using var api = new ApiReader(winmd);
        var fullName = Resolve(api, type);
        var stopAt = stop is null ? DefaultStop : Resolve(api, stop);
        var chain = api.Chain(fullName, stopAt);

        var generic = Path.Combine(design, "generic.xaml");
        var themeResources = Path.Combine(design, "themeresources.xaml");
        if (!File.Exists(generic))
        {
            throw new ArgumentException($"No generic.xaml in {design}.");
        }

        var style = TemplateReader.Read(File.ReadAllText(generic), ApiReader.Short(fullName));
        var resources = File.Exists(themeResources) ? new ThemeResources(File.ReadAllText(themeResources)) : null;
        var tokenKeys = Checklist.TokenKeys(tokens ?? FindTokens());
        // As given, unless a short name would find another type: then in full. A --tokens folder is a local path, and left out.
        var named = Resolve(api, ApiReader.Short(fullName), throwIfMissing: false) == fullName ? ApiReader.Short(fullName) : fullName;
        var command = $"dotnet run --project tools/winrt-api -- checklist {named} <Windows.winmd> <design folder>"
            + (stop is null ? "" : $" --stop {stop}");
        return Checklist.Render(fullName, chain, style, resources, tokenKeys, new AvaloniaSide(ApiReader.Short(fullName)), command);
    }

    /// <summary>The options after the three arguments: <c>--stop</c> and <c>--tokens</c>, each with a value, at most once.</summary>
    internal static IReadOnlyDictionary<string, string> Options(IReadOnlyList<string> options)
    {
        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < options.Count; i += 2)
        {
            var name = options[i];
            if (name is not ("--stop" or "--tokens"))
            {
                throw new ArgumentException($"Unknown option {name}: the options are --stop <Type> and --tokens <folder>.");
            }

            if (i + 1 >= options.Count || options[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"{name} needs a value.");
            }

            if (!parsed.TryAdd(name, options[i + 1]))
            {
                throw new ArgumentException($"{name} is given twice.");
            }
        }

        return parsed;
    }

    /// <summary>A full name as given, or the name under Windows.UI.Xaml.Controls, then .Primitives.</summary>
    internal static string Resolve(ApiReader api, string type) => Resolve(api, type, throwIfMissing: true)!;

    private static string? Resolve(ApiReader api, string type, bool throwIfMissing)
    {
        foreach (var candidate in new[] { type, $"{Controls}.{type}", $"{Controls}.Primitives.{type}" })
        {
            if (api.Contains(candidate))
            {
                return candidate;
            }
        }

        return !throwIfMissing ? null : throw new ArgumentException($"{type} isn't in the metadata, as a full name or under {Controls} or its Primitives.");
    }

    /// <summary>The theme's Tokens folder, from the repo this runs in.</summary>
    private static string FindTokens()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Cornflower.sln")))
            {
                return Path.Combine(dir.FullName, "src", "Cornflower.Themes.Wp81", "Tokens");
            }
        }

        throw new ArgumentException("Cornflower.sln not found above the current folder: give --tokens.");
    }
}
