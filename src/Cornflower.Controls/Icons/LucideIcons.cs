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
/// All 2,046 icons of Lucide 1.71.0 are 78 KB of Brotli in the assembly (406 KB of path data once
/// decompressed; the SVGs are 824 KB). The whole set costs less than two of the theme's Selawik
/// fonts, so it isn't split into a subset or a package of its own.
/// </para>
/// <para>
/// The first icon decompresses the set and indexes its lines; the bytes are kept, and each icon is
/// decoded and parsed when first asked for, then shared. A geometry is never changed after it's
/// made, so one instance can be drawn by any number of controls.
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
        private readonly int[] _starts;
        private readonly Geometry?[] _geometries;

        public Set(byte[] data)
        {
            _data = data;
            var starts = new List<int> { 0 };
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == '\n' && i + 1 < data.Length)
                {
                    starts.Add(i + 1);
                }
            }

            _starts = [.. starts];
            _geometries = new Geometry?[_starts.Length];

            var expected = Enum.GetValues<LucideIconKind>().Length;
            if (_starts.Length != expected)
            {
                throw new InvalidOperationException($"{Resource} has {_starts.Length} icons and {nameof(LucideIconKind)} has {expected}: run tools/lucide again.");
            }
        }

        public int Count => _starts.Length;

        public string Name(LucideIconKind kind)
        {
            var (start, tab, _) = Line(kind);
            return Encoding.ASCII.GetString(_data, start, tab - start);
        }

        public string Path(LucideIconKind kind)
        {
            var (_, tab, end) = Line(kind);
            return Encoding.ASCII.GetString(_data, tab + 1, end - tab - 1);
        }

        public Geometry Geometry(LucideIconKind kind)
        {
            var index = (int)kind;
            var geometry = Volatile.Read(ref _geometries[index]);
            if (geometry is null)
            {
                // Generated data has only absolute M, L, C, Q, A and Z, which StreamGeometry reads as SVG does.
                geometry = StreamGeometry.Parse(Path(kind));
                geometry = Interlocked.CompareExchange(ref _geometries[index], geometry, null) ?? geometry;
            }

            return geometry;
        }

        private (int Start, int Tab, int End) Line(LucideIconKind kind)
        {
            var index = (int)kind;
            if ((uint)index >= (uint)_starts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a Lucide icon.");
            }

            var start = _starts[index];
            var end = Array.IndexOf(_data, (byte)'\n', start);
            end = end < 0 ? _data.Length : end;
            var tab = Array.IndexOf(_data, (byte)'\t', start, end - start);
            return (start, tab, end);
        }
    }
}
