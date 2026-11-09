using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cornflower.Sample.Diagnostics;

/// <summary>
/// Counts the global style rules. Avalonia checks every one against every element, and a comma makes a rule cost as much
/// as that many rules. See docs/performance/README.md.
/// </summary>
public static class StyleCensus
{
    /// <summary>The rules in <paramref name="root"/>, and the alternatives between their commas.</summary>
    public static (int Rules, int Alternatives) Count(IStyle root)
    {
        var rules = Rules(root);
        return (rules.Count, rules.Sum(rule => SplitAlternatives(rule.ToString()).Count));
    }

    /// <summary>
    /// The global rules in <paramref name="root"/>: what Avalonia checks against every element. A rule's
    /// <c>ToString()</c> is its full selector; a selector alone prints a nested rule's parent as ^, which
    /// hides its commas.
    /// </summary>
    public static IReadOnlyList<Style> Rules(IStyle root)
    {
        var rules = new List<Style>();
        Visit(root);
        return rules;

        void Visit(IStyle style)
        {
            // Its Children are its content's, so visiting both would count each rule twice.
            if (style is StyleInclude { Loaded: { } loaded })
            {
                Visit(loaded);
                return;
            }

            // What Avalonia skips: a rule with neither is only there to hold its children.
            if (style is Style { Selector: not null } rule && (rule.Setters.Count > 0 || rule.Animations.Count > 0))
            {
                rules.Add(rule);
            }

            foreach (var child in style.Children)
            {
                Visit(child);
            }
        }
    }

    /// <summary>Splits at the commas between alternatives, not those inside <c>:is()</c>, <c>:not()</c> or <c>[Prop=value]</c>.</summary>
    public static IReadOnlyList<string> SplitAlternatives(string selector)
    {
        var parts = new List<string>();
        var depth = 0;
        var inValue = false;
        var start = 0;
        for (var i = 0; i < selector.Length; i++)
        {
            switch (selector[i])
            {
                case '[':
                    inValue = true;
                    break;
                case ']':
                    inValue = false;
                    break;
                case '(' when !inValue:
                    depth++;
                    break;
                case ')' when !inValue:
                    depth--;
                    break;
                case ',' when depth == 0 && !inValue:
                    parts.Add(selector[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        parts.Add(selector[start..].Trim());
        return parts;
    }
}
