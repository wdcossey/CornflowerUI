using System.IO.Compression;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;

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
/// controls. A geometry is an Avalonia object, so it's made on the UI thread only: <see cref="Get"/> checks,
/// as Avalonia does (the headless test platform lets any thread pass, so no test can hold it). Should two
/// threads get past the check anyway, both get the one instance published first. The names and path data
/// can be read from any thread.
/// </para>
/// </remarks>
internal static class LucideIcons
{
    /// <summary>The embedded data's logical name.</summary>
    internal const string Resource = "Cornflower.Controls.Icons.lucide.br";

    private static readonly Lazy<Set> All = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The icon's geometry, in Lucide's 24-unit box. On the UI thread only.</summary>
    public static Geometry Get(LucideIconKind kind)
    {
        Dispatcher.UIThread.VerifyAccess();
        return All.Value.Geometry(kind);
    }

    /// <summary>Whether <paramref name="kind"/> names an icon in this version's data.</summary>
    public static bool Has(LucideIconKind kind) => All.Value.Has(kind);

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
        using var bytes = new MemoryStream(512 * 1024);
        brotli.CopyTo(bytes);
        // The stream's own buffer, not a copy of it: the set reads only its first Length bytes.
        return new Set(bytes.GetBuffer(), (int)bytes.Length);
    }

    private sealed class Set
    {
        private readonly byte[] _data;
        private readonly int _length;
        private readonly (int Name, int Path, int End)[] _lines; // by the enum's value; Name 0 for no line
        private readonly Geometry?[] _geometries;

        public Set(byte[] data, int length)
        {
            _data = data;
            _length = length;
            var defined = Enum.GetValues<LucideIconKind>();
            _lines = new (int, int, int)[defined.Length == 0 ? 0 : (int)defined.Max() + 1];
            _geometries = new Geometry?[_lines.Length];

            for (var start = 0; start < length;)
            {
                var end = Array.IndexOf(data, (byte)'\n', start, length - start);
                end = end < 0 ? length : end;
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

        public bool Has(LucideIconKind kind) => (uint)(int)kind < (uint)_lines.Length && _lines[(int)kind].Name != 0;

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
        public Geometry Geometry(LucideIconKind kind)
        {
            var index = Index(kind);
            return Volatile.Read(ref _geometries[index])
                ?? Interlocked.CompareExchange(ref _geometries[index], StreamGeometry.Parse(Path(kind)), null)
                ?? _geometries[index]!;
        }

        private (int Name, int Path, int End) Line(LucideIconKind kind) => _lines[Index(kind)];

        private int Index(LucideIconKind kind)
        {
            var index = (int)kind;
            return Has(kind)
                ? index
                : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a Lucide icon.");
        }

        private static InvalidOperationException Stale() =>
            new($"{Resource} and {nameof(LucideIconKind)} don't match: run tools/lucide again.");
    }
}
