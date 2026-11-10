using System.IO.Compression;
using System.Text;
using Avalonia.Media;

namespace Cornflower.Controls;

/// <summary>
/// The Lucide icons' geometry, from <c>Icons/lucide.br</c>, which <c>tools/lucide</c> generates: one
/// path per icon in Lucide's 24-unit box, to be stroked, never filled.
/// </summary>
/// <remarks>
/// <para>
/// All 2,046 icons of Lucide 1.71.0 are 82 KB of Brotli in the assembly (about 415 KB of lines once
/// decompressed; the SVGs are 824 KB). The whole set costs less than two of the theme's Selawik
/// fonts, so it isn't split into a subset or a package of its own.
/// </para>
/// <para>
/// The first icon decompresses the set and indexes its lines by the enum's value, which each line
/// starts with; the bytes are kept, and each icon is decoded and parsed when first asked for, then
/// shared. A geometry is never changed after it's made, so one instance can be drawn by any number of
/// controls. Icons are asked for on the UI thread, by <see cref="LucideIcon"/>.
/// </para>
/// </remarks>
internal static class LucideIcons
{
    /// <summary>The embedded data's logical name.</summary>
    internal const string Resource = "Cornflower.Controls.Icons.lucide.br";

    private static readonly Lazy<Set> All = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The icon's geometry, in Lucide's 24-unit box.</summary>
    public static Geometry Get(LucideIconKind kind) => All.Value.Geometry(kind);

    /// <summary>The icon's Lucide name, <c>arrow-down-0-1</c> for <see cref="LucideIconKind.ArrowDown01"/>.</summary>
    internal static string NameOf(LucideIconKind kind) => All.Value.Name(kind);

    /// <summary>The icon's path data, as generated.</summary>
    internal static string PathOf(LucideIconKind kind) => All.Value.Path(kind);

    /// <summary>The number of icons in the data, which must be the enum's.</summary>
    internal static int Count => All.Value.Count;

    private static Set Load()
    {
        using var stream = typeof(LucideIcons).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"{Resource} isn't embedded.");
        using var brotli = new BrotliStream(stream, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        brotli.CopyTo(bytes);
        return new Set(bytes.ToArray());
    }

    private sealed class Set
    {
        private readonly byte[] _data;
        private readonly (int Name, int Path, int End)[] _lines; // by the enum's value; Name 0 for no line
        private readonly Geometry?[] _geometries;

        public Set(byte[] data)
        {
            _data = data;
            var defined = Enum.GetValues<LucideIconKind>();
            _lines = new (int, int, int)[defined.Length == 0 ? 0 : (int)defined.Max() + 1];
            _geometries = new Geometry?[_lines.Length];

            for (var start = 0; start < data.Length;)
            {
                var end = Array.IndexOf(data, (byte)'\n', start);
                end = end < 0 ? data.Length : end;
                var nameTab = Array.IndexOf(data, (byte)'\t', start, end - start);
                var pathTab = nameTab < 0 ? -1 : Array.IndexOf(data, (byte)'\t', nameTab + 1, end - nameTab - 1);
                if (pathTab < 0 || !int.TryParse(data.AsSpan(start, nameTab - start), out var value) || (uint)value >= (uint)_lines.Length || _lines[value].Name != 0)
                {
                    throw Stale();
                }

                _lines[value] = (nameTab + 1, pathTab + 1, end);
                Count++;
                start = end + 1;
            }

            if (Count != defined.Length || defined.Any(kind => _lines[(int)kind].Name == 0))
            {
                throw Stale();
            }
        }

        public int Count { get; }

        public string Name(LucideIconKind kind)
        {
            var line = Line(kind);
            return Encoding.ASCII.GetString(_data, line.Name, line.Path - 1 - line.Name);
        }

        public string Path(LucideIconKind kind)
        {
            var line = Line(kind);
            return Encoding.ASCII.GetString(_data, line.Path, line.End - line.Path);
        }

        // Generated data has only absolute M, L, C, Q, A and Z, which StreamGeometry reads as SVG does.
        public Geometry Geometry(LucideIconKind kind) =>
            _geometries[Index(kind)] ??= StreamGeometry.Parse(Path(kind));

        private (int Name, int Path, int End) Line(LucideIconKind kind) => _lines[Index(kind)];

        private int Index(LucideIconKind kind)
        {
            var index = (int)kind;
            return (uint)index < (uint)_lines.Length && _lines[index].Name != 0
                ? index
                : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a Lucide icon.");
        }

        private static InvalidOperationException Stale() =>
            new($"{Resource} and {nameof(LucideIconKind)} don't match: run tools/lucide again.");
    }
}
