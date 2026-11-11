using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace Cornflower.Tests.Repository;

/// <summary>
/// Rule 6 of CLAUDE.md, made mechanical: no Microsoft fonts, templates or SDK files in the repository.
/// A file fails if it is a font that isn't on <see cref="AllowedFonts"/> with the bytes listed there, if
/// it has the name of an SDK file we read as a specification, or if its content is one of the Microsoft files
/// in the reference folder (pulled from the VM or an emulator image, or decompiled or converted from them),
/// byte for byte or, for text, with its line endings and BOM as git stores them.
/// </summary>
internal static class MicrosoftFileGuard
{
    private static readonly HashSet<string> FontExtensions =
        new([".ttf", ".otf", ".ttc", ".woff", ".woff2", ".eot"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> SdkFileNames =
        new(["generic.xaml", "themeresources.xaml"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The fonts Cornflower bundles: repository-relative path to lowercase SHA-256 hex. Selawik 1.01, from
    /// <c>https://github.com/microsoft/Selawik/releases/download/1.01/Selawik_Release.zip</c>, unchanged.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> AllowedFonts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["src/Cornflower.Themes.Wp81/Fonts/Selawik/selawk.ttf"] = "e9d98518d8ac2817782a9a382430463a2e0793ea68350b695bb727d9a830ee1c",
        ["src/Cornflower.Themes.Wp81/Fonts/Selawik/selawkb.ttf"] = "f0db5e174a90e0956ad7d2844bdca1d5e6da92ec65b2c04e57ba9b180668c904",
        ["src/Cornflower.Themes.Wp81/Fonts/Selawik/selawkl.ttf"] = "8e19d073091a1e869b5b0d48b925e605c0a4c6ece7df7b22a364fb065314c4c5",
        ["src/Cornflower.Themes.Wp81/Fonts/Selawik/selawksb.ttf"] = "0a9e9d0549a10f24bef9b3a29e06fe6e0b5c21e7a784c503b048a307841a7783",
        ["src/Cornflower.Themes.Wp81/Fonts/SelawikSemilight/selawksl.ttf"] = "8620960344f12093482cded984a1aafa5a57d24cbf7e9299da125ccf0e9d4102",
    };

    /// <summary>Lowercase SHA-256 hex of each pulled Microsoft file, from <c>microsoft-sha256.txt</c>.</summary>
    internal static IReadOnlyDictionary<string, string> MicrosoftHashes { get; } = LoadHashes();

    /// <summary>One message per problem; empty when the files are clean.</summary>
    internal static IReadOnlyList<string> Check(
        IEnumerable<(string Path, Func<Stream> Open)> files,
        IReadOnlyDictionary<string, string> hashes,
        IReadOnlyDictionary<string, string> allowedFonts)
    {
        var problems = new List<string>();
        foreach (var (path, open) in files)
        {
            var name = Path.GetFileName(path);
            var extension = Path.GetExtension(path);

            byte[] bytes;
            using (var stream = open())
            using (var copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                bytes = copy.ToArray();
            }

            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var text = TextForm(bytes);
            var textHash = text is null ? null : Convert.ToHexStringLower(SHA256.HashData(text));

            if (FontExtensions.Contains(extension))
            {
                if (!allowedFonts.TryGetValue(path, out var allowed))
                    problems.Add($"{path}: a font that isn't one Cornflower bundles");
                else if (allowed != hash)
                    problems.Add($"{path}: a font that isn't the bytes of the one Cornflower bundles there");
            }

            if (SdkFileNames.Contains(name) || extension.Equals(".winmd", StringComparison.OrdinalIgnoreCase))
                problems.Add($"{path}: the name of a Windows Phone SDK file");

            if (hashes.TryGetValue(hash, out var original)
                || (textHash is not null && hashes.TryGetValue(textHash, out original)))
                problems.Add($"{path}: the same content as Microsoft's {original}, from the reference folder");
        }

        return problems;
    }

    /// <summary>
    /// The text form git stores under <c>* text=auto</c>, as <c>tools/guard/hash-reference.js</c> hashes it: a
    /// UTF-8 BOM dropped and CRLF turned into LF. Null for a binary file (a NUL in the first 8,000 bytes, git's test).
    /// </summary>
    internal static byte[]? TextForm(byte[] bytes)
    {
        if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8000)) >= 0)
            return null;

        var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        var output = new List<byte>(bytes.Length - start);
        for (var i = start; i < bytes.Length; i++)
            if (!(bytes[i] == 0x0D && i + 1 < bytes.Length && bytes[i + 1] == 0x0A))
                output.Add(bytes[i]);

        return output.Count == 0 ? null : [.. output];
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
