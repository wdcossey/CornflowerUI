using System.IO.Compression;
using System.Text;

namespace IconGenerator;

/// <summary>What the generator writes, and where: the names are parameters, so another library can use it unchanged.</summary>
/// <param name="Folder">The folder the three files go in.</param>
/// <param name="Namespace">The generated enum's namespace.</param>
/// <param name="EnumName">The generated enum's name; its file is <c>&lt;EnumName&gt;.g.cs</c>.</param>
/// <param name="DataName">The data file's name: Brotli over the icons' lines.</param>
internal sealed record Output(string Folder, string Namespace, string EnumName, string DataName);

/// <summary>A release's icons, read from its zip: each SVG's name (its file name without <c>.svg</c>) and text, sorted by name.</summary>
internal sealed record Release(string Version, IReadOnlyList<(string Name, string Svg)> Icons, string Licence);

/// <summary>
/// Writes a Lucide release as an enum of its icons and a data file of their paths, and copies its
/// licence beside them.
/// </summary>
/// <remarks>
/// <para>
/// The data file holds one line per icon, <c>name&lt;TAB&gt;path data</c>, in ordinal order of name, which
/// is the enum's order, so the enum's value is the line's index. It is Brotli-compressed at the
/// highest quality; see <c>LucideIcons</c> in Cornflower.Controls for the sizes.
/// </para>
/// <para>
/// The output depends only on the release, so the same pin gives the same files. Brotli's encoder is
/// deterministic for one input and quality; a test checks the committed data decompresses to what the
/// generator makes, so an encoder that changed its bytes would still be caught by content.
/// </para>
/// </remarks>
internal static class Generator
{
    /// <summary>Reads the icons and the licence out of a release zip (<c>icons/*.svg</c>).</summary>
    public static Release Read(string version, Stream zip, string licence)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read);
        var icons = new List<(string, string)>();
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".svg", StringComparison.Ordinal))
            {
                continue;
            }

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            icons.Add((Path.GetFileNameWithoutExtension(entry.Name), reader.ReadToEnd()));
        }

        return new Release(version, icons.OrderBy(icon => icon.Item1, StringComparer.Ordinal).ToList(), licence);
    }

    /// <summary>The three files' contents, by file name.</summary>
    public static IReadOnlyDictionary<string, byte[]> Generate(Release release, Output output)
    {
        if (release.Icons.Count == 0)
        {
            throw new FormatException("The release has no icons.");
        }

        var members = new List<(string Name, string Member)>();
        var lines = new StringBuilder();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, svg) in release.Icons.OrderBy(icon => icon.Name, StringComparer.Ordinal))
        {
            var member = Member(name);
            if (!seen.TryAdd(member, name))
            {
                throw new FormatException($"{name} and {seen[member]} are both {member}.");
            }

            members.Add((name, member));
            lines.Append(name).Append('\t').Append(SvgIcon.ToPath(name, svg)).Append('\n');
        }

        return new Dictionary<string, byte[]>
        {
            [output.EnumName + ".g.cs"] = Encoding.UTF8.GetBytes(EnumSource(release, output, members)),
            [output.DataName] = Compress(Encoding.UTF8.GetBytes(lines.ToString())),
            ["LICENSE"] = Encoding.UTF8.GetBytes(release.Licence),
        };
    }

    /// <summary>Writes the files into <see cref="Output.Folder"/>, replacing what's there.</summary>
    public static void Write(IReadOnlyDictionary<string, byte[]> files, Output output)
    {
        Directory.CreateDirectory(output.Folder);
        foreach (var (name, bytes) in files)
        {
            File.WriteAllBytes(Path.Combine(output.Folder, name), bytes);
        }
    }

    /// <summary>Lucide's own PascalCase name for an icon, as its React and other packages spell it: <c>arrow-down-0-1</c> is <c>ArrowDown01</c>.</summary>
    internal static string Member(string name)
    {
        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-') || !char.IsAsciiLetterLower(name[0]))
        {
            throw new FormatException($"{name}: an icon name is lower-case letters, digits and hyphens, starting with a letter.");
        }

        return string.Concat(name.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    internal static byte[] Compress(byte[] raw)
    {
        using var encoder = new BrotliEncoder(quality: 11, window: 24);
        var buffer = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        if (encoder.Compress(raw, buffer, out _, out var written, isFinalBlock: true) != System.Buffers.OperationStatus.Done)
        {
            throw new InvalidOperationException("Brotli couldn't compress the icons.");
        }

        return buffer[..written];
    }

    private static string EnumSource(Release release, Output output, IReadOnlyList<(string Name, string Member)> members)
    {
        var source = new StringBuilder();
        source.Append("// <auto-generated>\n");
        source.Append($"// By tools/lucide from Lucide {release.Version} (ISC, with icons from Feather under MIT: see LICENSE beside this file).\n");
        source.Append("// Don't edit: change the pin in tools/lucide/pin.json and run the generator again.\n");
        source.Append("// </auto-generated>\n\n");
        source.Append($"namespace {output.Namespace};\n\n");
        source.Append($"/// <summary>The {members.Count} icons of Lucide {release.Version} (lucide.dev), by Lucide's own PascalCase names: <c>arrow-down-0-1</c> is <c>ArrowDown01</c>.</summary>\n");
        source.Append($"public enum {output.EnumName}\n{{\n");
        foreach (var (_, member) in members)
        {
            source.Append($"    {member},\n");
        }

        source.Append("}\n");
        return source.ToString();
    }
}
