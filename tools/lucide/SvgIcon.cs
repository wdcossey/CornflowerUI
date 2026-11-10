using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace IconGenerator;

/// <summary>
/// One Lucide SVG as one path, in <see cref="PathData"/>'s subset, drawn by one pen.
/// </summary>
/// <remarks>
/// <para>
/// Lucide draws every icon one way: a 24-unit box, no fill, a stroke in the current colour 2 units
/// wide with round caps and joins, all set once on the root. So the elements can join into one
/// path without losing anything, which Avonic's Ionicons couldn't (588 of them have layers that
/// differ in paint). This class checks that convention and fails on anything outside it, so a
/// Lucide release that breaks it stops the generator rather than drawing wrongly.
/// </para>
/// <para>
/// The one exception in Lucide 1.71.0: 28 small circles in 12 icons have <c>fill="currentColor"</c>,
/// with a radius of 0.5 or 1. A stroke 2 wide covers a circle of radius up to 1 to its centre, so
/// the fill adds no pixel, and the circle is kept as a stroked outline. That holds for a pen of 2 or
/// more; a thinner one (the theme's resource) leaves a pin-hole in those dots: below 2 in the two of
/// radius 1, below 1 in the 26 of radius 0.5.
/// </para>
/// <para>
/// Shapes become path data as SVG defines them, after Avonic's <c>SvgArtwork.cs</c> (<c>EllipsePath</c>,
/// <c>RectPath</c>, <c>PointsPath</c>).
/// </para>
/// </remarks>
internal static class SvgIcon
{
    /// <summary>The root's attributes, which every icon must carry exactly.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Convention = new Dictionary<string, string>
    {
        ["width"] = "24",
        ["height"] = "24",
        ["viewBox"] = "0 0 24 24",
        ["fill"] = "none",
        ["stroke"] = "currentColor",
        ["stroke-width"] = "2",
        ["stroke-linecap"] = "round",
        ["stroke-linejoin"] = "round",
    };

    /// <summary>The largest filled circle the stroke covers: half of <c>stroke-width</c>.</summary>
    private const double CoveredRadius = 1;

    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    private static readonly Dictionary<string, string[]> Attributes = new()
    {
        ["path"] = ["d"],
        ["circle"] = ["cx", "cy", "r"],
        ["ellipse"] = ["cx", "cy", "rx", "ry"],
        ["rect"] = ["x", "y", "width", "height", "rx", "ry"],
        ["line"] = ["x1", "y1", "x2", "y2"],
        ["polyline"] = ["points"],
        ["polygon"] = ["points"],
    };

    /// <summary>The icon's path data. Throws <see cref="FormatException"/>, naming <paramref name="name"/>, on anything outside the convention.</summary>
    public static string ToPath(string name, string svg)
    {
        XElement root;
        try
        {
            root = XElement.Parse(svg, LoadOptions.None);
        }
        catch (XmlException e)
        {
            throw new FormatException($"{name}: not well-formed XML ({e.Message})", e);
        }

        if (root.Name != XName.Get("svg", SvgNamespace))
        {
            throw new FormatException($"{name}: the root isn't an SVG <svg>.");
        }

        foreach (var attribute in root.Attributes().Where(a => !a.IsNamespaceDeclaration))
        {
            var key = attribute.Name.LocalName;
            if (!Convention.TryGetValue(key, out var expected) || attribute.Value != expected || attribute.Name.NamespaceName.Length > 0)
            {
                throw new FormatException($"{name}: <svg {key}=\"{attribute.Value}\"> is outside Lucide's convention.");
            }
        }

        foreach (var key in Convention.Keys.Where(key => root.Attribute(key) is null))
        {
            throw new FormatException($"{name}: <svg> has no {key}.");
        }

        var paths = new List<string>();
        foreach (var node in root.Nodes())
        {
            if (node is XComment)
            {
                continue;
            }

            if (node is not XElement element || element.Name.Namespace != SvgNamespace)
            {
                throw new FormatException($"{name}: only shapes may sit inside <svg>, not {node}.");
            }

            paths.Add(PathData.Normalize(Shape(name, element)));
        }

        if (paths.Count == 0)
        {
            throw new FormatException($"{name}: no shapes.");
        }

        return string.Join(' ', paths);
    }

    private static string Shape(string name, XElement element)
    {
        var tag = element.Name.LocalName;
        if (!Attributes.TryGetValue(tag, out var allowed))
        {
            throw new FormatException($"{name}: <{tag}> isn't drawn.");
        }

        // An <animate> or <title> inside a shape would be dropped without a word: refuse it.
        if (element.Nodes().Any(node => node is not XComment && !(node is XText text && string.IsNullOrWhiteSpace(text.Value))))
        {
            throw new FormatException($"{name}: <{tag}> has something inside it, which Lucide's convention doesn't.");
        }

        foreach (var attribute in element.Attributes())
        {
            var key = attribute.Name.LocalName;
            if (allowed.Contains(key) && attribute.Name.NamespaceName.Length == 0)
            {
                continue;
            }

            var covered = tag == "circle" && key == "fill" && attribute.Value == "currentColor" && Number(name, element, "r") <= CoveredRadius;
            if (!covered)
            {
                throw new FormatException($"{name}: <{tag} {key}=\"{attribute.Value}\"> is outside Lucide's convention.");
            }
        }

        return tag switch
        {
            "path" => element.Attribute("d")?.Value ?? throw new FormatException($"{name}: a <path> without d."),
            "circle" => Ellipse(name, Number(name, element, "cx"), Number(name, element, "cy"), Number(name, element, "r"), Number(name, element, "r")),
            "ellipse" => Ellipse(name, Number(name, element, "cx"), Number(name, element, "cy"), Number(name, element, "rx"), Number(name, element, "ry")),
            "rect" => Rect(name, element),
            "line" => Invariant($"M{Number(name, element, "x1")} {Number(name, element, "y1")} L{Number(name, element, "x2")} {Number(name, element, "y2")}"),
            "polyline" => Points(name, element, closed: false),
            _ => Points(name, element, closed: true),
        };
    }

    /// <summary>Two half-turns, starting at the left: closed, so where it starts draws no cap.</summary>
    private static string Ellipse(string name, double cx, double cy, double rx, double ry) =>
        rx <= 0 || ry <= 0
            ? throw new FormatException($"{name}: a circle or ellipse with no radius.")
            : Invariant($"M{cx - rx} {cy} A{rx} {ry} 0 1 1 {cx + rx} {cy} A{rx} {ry} 0 1 1 {cx - rx} {cy} Z");

    private static string Rect(string name, XElement element)
    {
        double x = Number(name, element, "x", 0), y = Number(name, element, "y", 0);
        double width = Number(name, element, "width"), height = Number(name, element, "height");
        if (width <= 0 || height <= 0)
        {
            throw new FormatException($"{name}: a <rect> with no area.");
        }

        // An omitted radius takes the other's value, and neither can pass half the side (SVG 2, 10.2).
        var givenX = element.Attribute("rx") is null ? (double?)null : Number(name, element, "rx");
        var givenY = element.Attribute("ry") is null ? (double?)null : Number(name, element, "ry");
        var rx = Math.Min(givenX ?? givenY ?? 0, width / 2);
        var ry = Math.Min(givenY ?? givenX ?? 0, height / 2);

        if (rx <= 0 || ry <= 0)
        {
            return Invariant($"M{x} {y} H{x + width} V{y + height} H{x} Z");
        }

        return Invariant(
            $"M{x + rx} {y} H{x + width - rx} A{rx} {ry} 0 0 1 {x + width} {y + ry} V{y + height - ry} A{rx} {ry} 0 0 1 {x + width - rx} {y + height} H{x + rx} A{rx} {ry} 0 0 1 {x} {y + height - ry} V{y + ry} A{rx} {ry} 0 0 1 {x + rx} {y} Z");
    }

    /// <summary>
    /// <c>points</c> is a moveto's number grammar (SVG 2, 10.6): after an <c>M</c>, its first pair moves
    /// and the rest draw lines, and <see cref="PathData"/> reads <c>1-2</c> and <c>.5.5</c> as a browser does.
    /// </summary>
    private static string Points(string name, XElement element, bool closed)
    {
        var text = element.Attribute("points")?.Value ?? "";
        var path = "M" + text + (closed ? " Z" : "");
        try
        {
            var normalized = PathData.Normalize(path);
            return normalized.Contains(" L", StringComparison.Ordinal)
                ? normalized
                : throw new FormatException($"{name}: points=\"{text}\" has fewer than two points.");
        }
        catch (FormatException e) when (!e.Message.StartsWith(name + ":", StringComparison.Ordinal))
        {
            throw new FormatException($"{name}: points=\"{text}\" isn't a list of points ({e.Message})", e);
        }
    }

    private static double Number(string name, XElement element, string attribute, double? fallback = null)
    {
        var text = element.Attribute(attribute)?.Value;
        if (text is null)
        {
            return fallback ?? throw new FormatException($"{name}: <{element.Name.LocalName}> has no {attribute}.");
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"{name}: {attribute}=\"{text}\" isn't a number.");
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
