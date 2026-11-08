using System.Buffers.Binary;
using System.Text;
using Avalonia.Platform;

namespace Cornflower.Tests.Fonts;

/// <summary>
/// The few numbers the font tests need, read straight from a TrueType file's tables, so that what
/// Avalonia draws can be checked against the file itself rather than against Avalonia's own reading.
/// </summary>
internal sealed class OpenTypeFile
{
    private readonly byte[] _data;
    private readonly Dictionary<string, (int Offset, int Length)> _tables = new(StringComparer.Ordinal);

    private OpenTypeFile(byte[] data)
    {
        _data = data;
        int count = U16(4);
        for (var i = 0; i < count; i++)
        {
            var record = 12 + 16 * i;
            _tables[Encoding.ASCII.GetString(data, record, 4)] = ((int)U32(record + 8), (int)U32(record + 12));
        }
    }

    /// <summary>A font the theme bundles, by its path under <c>Fonts/</c>.</summary>
    public static OpenTypeFile Bundled(string path)
    {
        using var stream = AssetLoader.Open(new Uri("avares://Cornflower.Themes.Wp81/Fonts/" + path));
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return new OpenTypeFile(copy.ToArray());
    }

    public int UnitsPerEm => U16(Table("head") + 18);

    public int HheaAscender => S16(Table("hhea") + 4);
    public int HheaDescender => S16(Table("hhea") + 6);     // negative, below the baseline
    public int HheaLineGap => S16(Table("hhea") + 8);

    public int WeightClass => U16(Table("OS/2") + 4);
    public int FsSelection => U16(Table("OS/2") + 62);
    public int WinAscent => U16(Table("OS/2") + 74);
    public int WinDescent => U16(Table("OS/2") + 76);      // positive, below the baseline

    /// <summary>Name ID 1, the legacy family name, in Windows English.</summary>
    public string FamilyName => Name(1);

    /// <summary>Name ID 5.</summary>
    public string Version => Name(5);

    /// <summary>Line height in ems by the <c>hhea</c> table: ascender − descender + line gap.</summary>
    public double HheaLine => (HheaAscender - HheaDescender + HheaLineGap) / (double)UnitsPerEm;

    /// <summary>Line height in ems by the <c>OS/2</c> win metrics.</summary>
    public double WinLine => (WinAscent + WinDescent) / (double)UnitsPerEm;

    /// <summary>The horizontal advance of a character's glyph, in font units.</summary>
    public int Advance(char c)
    {
        var glyph = Glyph(c);
        var hmtx = Table("hmtx");
        int metrics = U16(Table("hhea") + 34);
        return U16(hmtx + 4 * Math.Min(glyph, metrics - 1));
    }

    private int Glyph(char c)
    {
        var cmap = Table("cmap");
        int count = U16(cmap + 2);
        for (var i = 0; i < count; i++)
        {
            var record = cmap + 4 + 8 * i;
            if (U16(record) != 3 || U16(record + 2) != 1)
                continue;

            var sub = cmap + (int)U32(record + 4);
            if (U16(sub) != 4)
                continue;

            int segments = U16(sub + 6) / 2;
            int ends = sub + 14, starts = ends + 2 * segments + 2, deltas = starts + 2 * segments, ranges = deltas + 2 * segments;
            for (var s = 0; s < segments; s++)
            {
                if (c > U16(ends + 2 * s) || c < U16(starts + 2 * s))
                    continue;

                int range = U16(ranges + 2 * s);
                int glyph = range == 0 ? c : U16(ranges + 2 * s + range + 2 * (c - U16(starts + 2 * s)));
                return (glyph + S16(deltas + 2 * s)) & 0xFFFF;
            }
        }

        throw new InvalidOperationException($"No glyph for '{c}'");
    }

    private string Name(int id)
    {
        var name = Table("name");
        int count = U16(name + 2), strings = name + U16(name + 4);
        for (var i = 0; i < count; i++)
        {
            var record = name + 6 + 12 * i;
            if (U16(record) == 3 && U16(record + 2) == 1 && U16(record + 4) == 0x409 && U16(record + 6) == id)
                return Encoding.BigEndianUnicode.GetString(_data, strings + U16(record + 10), U16(record + 8));
        }

        throw new InvalidOperationException($"No name {id}");
    }

    private int Table(string tag) => _tables[tag].Offset;
    private int U16(int at) => BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan(at));
    private int S16(int at) => BinaryPrimitives.ReadInt16BigEndian(_data.AsSpan(at));
    private uint U32(int at) => BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(at));
}
