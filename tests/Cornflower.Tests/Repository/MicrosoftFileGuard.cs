using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace Cornflower.Tests.Repository;

/// <summary>
/// Rule 6 of CLAUDE.md, made mechanical: no Microsoft fonts, templates or SDK files in the repository.
/// A file fails if it is a font not on <see cref="AllowedFonts"/>, if it has the name of an SDK file
/// we read as a specification, or if its bytes are one of the files pulled from the reference VM.
/// </summary>
internal static class MicrosoftFileGuard
{
    private static readonly HashSet<string> FontExtensions =
        new([".ttf", ".otf", ".ttc", ".woff", ".woff2", ".eot"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> SdkFileNames =
        new(["generic.xaml", "themeresources.xaml"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Repository-relative paths of the fonts Cornflower bundles. None yet.</summary>
    internal static readonly HashSet<string> AllowedFonts = new(StringComparer.Ordinal);

    /// <summary>Lowercase SHA-256 hex of each pulled Microsoft file, from <c>microsoft-sha256.txt</c>.</summary>
    internal static IReadOnlyDictionary<string, string> MicrosoftHashes { get; } = LoadHashes();

    /// <summary>One message per problem; empty when the files are clean.</summary>
    internal static IReadOnlyList<string> Check(
        IEnumerable<(string Path, Func<Stream> Open)> files,
        IReadOnlyDictionary<string, string> hashes,
        ISet<string> allowedFonts)
    {
        var problems = new List<string>();
        foreach (var (path, open) in files)
        {
            var name = Path.GetFileName(path);
            var extension = Path.GetExtension(path);

            if (FontExtensions.Contains(extension) && !allowedFonts.Contains(path))
                problems.Add($"{path}: a font that isn't one Cornflower bundles");

            if (SdkFileNames.Contains(name) || extension.Equals(".winmd", StringComparison.OrdinalIgnoreCase))
                problems.Add($"{path}: the name of a Windows Phone SDK file");

            using var stream = open();
            var hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            if (hashes.TryGetValue(hash, out var original))
                problems.Add($"{path}: the same bytes as Microsoft's {original}, pulled from the reference VM");
        }

        return problems;
    }

    /// <summary>The files git would commit: tracked, plus untracked files that aren't ignored.</summary>
    internal static IEnumerable<(string Path, Func<Stream> Open)> RepositoryFiles()
    {
        var root = FindRoot();
        var git = new ProcessStartInfo("git", "ls-files -z --cached --others --exclude-standard")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(git) ?? throw new InvalidOperationException("git didn't start");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git ls-files exited with {process.ExitCode}");

        foreach (var path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var full = Path.Combine(root, path);
            // A tracked file deleted from the working tree is listed but isn't there to commit.
            if (File.Exists(full))
                yield return (path, () => File.OpenRead(full));
        }
    }

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Cornflower.sln")))
                return dir.FullName;

        throw new InvalidOperationException("Cornflower.sln not found above " + AppContext.BaseDirectory);
    }

    private static Dictionary<string, string> LoadHashes()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Cornflower.Tests.Repository.microsoft-sha256.txt")
            ?? throw new InvalidOperationException("microsoft-sha256.txt isn't embedded");
        using var reader = new StreamReader(stream);

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var parts = line.Split("  ", 2);
            hashes[parts[0]] = parts[1];
        }

        return hashes;
    }
}
