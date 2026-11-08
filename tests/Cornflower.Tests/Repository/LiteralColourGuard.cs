using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Avalonia.Media;

namespace Cornflower.Tests.Repository;

/// <summary>
/// Rule 5 of CLAUDE.md, made mechanical: no literal colours outside a theme's <c>Tokens/</c>. It checks the
/// shipped code, everything under <c>src/</c>; tests and tools may hold colours of their own.
/// </summary>
/// <remarks>
/// In XAML, an attribute value or element text that Avalonia parses as a colour (<c>#RRGGBB</c>, <c>Red</c>)
/// is a literal colour. In C#, a hex string, <c>Color.FromRgb</c> and its kin, <c>Color.Parse</c>, and a
/// <c>Colors</c> or <c>Brushes</c> member are. <c>Transparent</c> is allowed: it is no colour, and templates set
/// it to make an area hit-testable. Comments are ignored in XAML, not in C#.
/// </remarks>
internal static partial class LiteralColourGuard
{
    private static readonly HashSet<string> XamlExtensions = new([".axaml", ".xaml"], StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^src/Cornflower\.Themes\.[^/]+/Tokens/")]
    private static partial Regex TokensFolder();

    [GeneratedRegex("""
        "\#[0-9A-Fa-f]{3,8}"
        | \bColor\.(?:FromRgb|FromArgb|FromUInt32|FromHsl|FromHsv|Parse|TryParse)\s*\(
        | \b(?:Colors|Brushes)\.(?!Transparent\b)[A-Z]\w*
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex CSharpColour();

    /// <summary>One message per literal colour; empty when there are none.</summary>
    internal static IReadOnlyList<string> Check(IEnumerable<(string Path, Func<Stream> Open)> files)
    {
        var problems = new List<string>();
        foreach (var (path, open) in files)
        {
            if (!path.StartsWith("src/", StringComparison.Ordinal) || TokensFolder().IsMatch(path))
                continue;

            var extension = Path.GetExtension(path);
            if (XamlExtensions.Contains(extension))
            {
                using var stream = open();
                foreach (var (line, value) in XamlColours(stream))
                    problems.Add($"{path}:{line}: the literal colour \"{value}\"; colours belong in Tokens/");
            }
            else if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(open());
                var number = 0;
                while (reader.ReadLine() is { } text)
                {
                    number++;
                    foreach (Match match in CSharpColour().Matches(text))
                        problems.Add($"{path}:{number}: the literal colour \"{match.Value}\"; colours belong in Tokens/");
                }
            }
        }

        return problems;
    }

    private static IEnumerable<(int Line, string Value)> XamlColours(Stream stream)
    {
        var document = XDocument.Load(stream, LoadOptions.SetLineInfo);
        foreach (var element in document.Descendants())
        {
            var line = ((IXmlLineInfo)element).LineNumber;
            var values = element.Attributes().Where(a => !a.IsNamespaceDeclaration).Select(a => a.Value)
                .Concat(element.Nodes().OfType<XText>().Select(t => t.Value));
            foreach (var raw in values)
            {
                var value = raw.Trim();
                if (IsColour(value))
                    yield return (line, value);
            }
        }
    }

    private static bool IsColour(string value) =>
        value.Length > 0
        && !value.StartsWith('{')
        && !value.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
        && Color.TryParse(value, out _);
}
