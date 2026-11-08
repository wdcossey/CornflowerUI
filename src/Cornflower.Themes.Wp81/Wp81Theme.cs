using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Windows Phone 8.1 theme. Add to <c>Application.Styles</c> in place of <c>FluentTheme</c>.
/// It merges every file under <c>Tokens/</c> and <c>Controls/</c>, found at build time, so a
/// control's theme is added by adding its file.
/// </summary>
/// <example>
/// <code>
/// &lt;Application.Styles&gt;
///     &lt;wp81:Wp81Theme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </example>
public class Wp81Theme : Styles
{
    private static readonly Uri BaseUri = new("avares://Cornflower.Themes.Wp81/");

    public Wp81Theme(IServiceProvider? _ = null)
    {
        foreach (var path in OrderForMerge(XamlManifest.Paths))
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri(BaseUri, path) });
        }
    }

    /// <summary>Glob order differs between file systems, so merge order is fixed here.</summary>
    internal static IEnumerable<string> OrderForMerge(IEnumerable<string> paths) =>
        paths.Order(StringComparer.Ordinal);
}
