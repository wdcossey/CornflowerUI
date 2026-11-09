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
        var rules = 0;
        var alternatives = 0;
        Visit(root);
        return (rules, alternatives);

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
                rules++;
                // The rule's, because a selector alone prints a nested rule's parent as ^, which hides its commas.
                alternatives += SplitAlternatives(rule.ToString()).Count;
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
