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
        var chain = api.Chain(fullName, stop ?? DefaultStop);

        var generic = Path.Combine(design, "generic.xaml");
        var themeResources = Path.Combine(design, "themeresources.xaml");
        if (!File.Exists(generic))
        {
            throw new ArgumentException($"No generic.xaml in {design}.");
        }

        var style = TemplateReader.Read(File.ReadAllText(generic), ApiReader.Short(fullName));
        var resources = File.Exists(themeResources) ? new ThemeResources(File.ReadAllText(themeResources)) : null;
        var tokenKeys = Checklist.TokenKeys(tokens ?? FindTokens());
        var command = $"dotnet run --project tools/winrt-api -- checklist {ApiReader.Short(fullName)} <Windows.winmd> <design folder>";
        return Checklist.Render(fullName, chain, style, resources, tokenKeys, new AvaloniaSide(ApiReader.Short(fullName)), command);
    }

    /// <summary>A full name as given, or the name under Windows.UI.Xaml.Controls, then .Primitives.</summary>
    internal static string Resolve(ApiReader api, string type)
    {
        foreach (var candidate in new[] { type, $"{Controls}.{type}", $"{Controls}.Primitives.{type}" })
        {
            if (api.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new ArgumentException($"{type} isn't in the metadata, as a full name or under {Controls} or its Primitives.");
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
