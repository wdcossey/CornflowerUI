using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Tokens;

/// <summary>What a token is, so a value of the wrong type fails as surely as a wrong value.</summary>
internal enum TokenKind { Color, Brush, Double, Int32, Thickness, FontWeight }

/// <summary>A key in the theme dictionaries, with its value in each variant.</summary>
internal sealed record ThemedToken(string Key, TokenKind Kind, string Dark, string Light, string Source);

/// <summary>A key outside the theme dictionaries, the same in both variants.</summary>
internal sealed record SharedToken(string Key, TokenKind Kind, string Value, string Source);

/// <summary>
/// The token tests change <c>Application.Styles</c>, <c>Application.Resources</c> and the requested theme
/// variant, so they run on their own, after the parallel tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TokenCollection
{
    public const string Name = "Tokens";
}

internal static class Token
{
    /// <summary>
    /// A resolved value written as the expectations write it: <c>#AARRGGBB</c>, a brush's opacity after
    /// <c>@</c>, a thickness as left,top,right,bottom, a font weight as its number. The kind is prefixed when it
    /// isn't the expected one, so a double where a Thickness belongs doesn't pass as "2.5".
    /// </summary>
    public static string Describe(object? value, TokenKind expected)
    {
        var (kind, text) = value switch
        {
            Color c => (TokenKind.Color, Hex(c)),
            ISolidColorBrush b => (TokenKind.Brush, Hex(b.Color) + (b.Opacity == 1 ? "" : "@" + Number(b.Opacity))),
            double d => (TokenKind.Double, Number(d)),
            int i => (TokenKind.Int32, i.ToString(CultureInfo.InvariantCulture)),
            Thickness t => (TokenKind.Thickness, $"{Number(t.Left)},{Number(t.Top)},{Number(t.Right)},{Number(t.Bottom)}"),
            FontWeight w => (TokenKind.FontWeight, ((int)w).ToString(CultureInfo.InvariantCulture)),
            null => ((TokenKind?)null, "(missing)"),
            _ => (null, value.GetType().Name + " " + value),
        };
        return kind == expected ? text : $"{kind?.ToString() ?? "?"} {text}";
    }

    public static string Hex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string Number(double d) => d.ToString("G", CultureInfo.InvariantCulture);

    /// <summary>Installs the theme in <c>Application.Styles</c> and takes it out again on dispose.</summary>
    public static IDisposable InstallTheme(out Wp81Theme theme)
    {
        var app = Application.Current!;
        var installed = new Wp81Theme();
        var variant = app.RequestedThemeVariant;
        app.Styles.Add(installed);
        theme = installed;
        return new Undo(() =>
        {
            app.Styles.Remove(installed);
            app.RequestedThemeVariant = variant;
        });
    }

    private sealed class Undo(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }
}
