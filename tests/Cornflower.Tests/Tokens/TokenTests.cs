using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Tokens;

/// <summary>
/// The tokens in <c>src/Cornflower.Themes.Wp81/Tokens/</c>: each key resolves to its value, in both theme
/// variants, with the theme installed as an app installs it. The expected values are in
/// <see cref="TokenExpectations"/>, each with its source.
/// </summary>
[Collection(TokenCollection.Name)]
public class TokenTests
{
    /// <summary>The token files this suite covers. Fonts.axaml is the fonts work's, with its own tests.</summary>
    private static readonly string[] TokenFiles =
        ["Tokens/Accent.axaml", "Tokens/Colors.axaml", "Tokens/Sizes.axaml", "Tokens/Typography.axaml"];

    private static readonly ThemeVariant[] Variants = [ThemeVariant.Dark, ThemeVariant.Light];

    private static string Resolve(string key, ThemeVariant variant, TokenKind kind) =>
        Token.Describe(Application.Current!.TryGetResource(key, variant, out var value) ? value : null, kind);

    [AvaloniaFact]
    public void Every_Themed_Token_Resolves_To_Its_Value_In_Dark_And_Light()
    {
        using var _ = Token.InstallTheme(out var _);

        var wrong = new List<string>();
        foreach (var t in TokenExpectations.Themed)
        {
            var dark = Resolve(t.Key, ThemeVariant.Dark, t.Kind);
            var light = Resolve(t.Key, ThemeVariant.Light, t.Kind);
            if (dark != t.Dark) wrong.Add($"{t.Key} (Dark): {dark}, expected {t.Dark} from {t.Source}");
            if (light != t.Light) wrong.Add($"{t.Key} (Light): {light}, expected {t.Light} from {t.Source}");
        }

        Assert.Empty(wrong);
        Assert.Equal(319, TokenExpectations.Themed.Length);
    }

    [AvaloniaFact]
    public void Every_Shared_Token_Resolves_To_Its_Value_In_Dark_And_Light()
    {
        using var _ = Token.InstallTheme(out var _);

        var wrong = new List<string>();
        foreach (var t in TokenExpectations.Shared)
        foreach (var variant in Variants)
        {
            var actual = Resolve(t.Key, variant, t.Kind);
            if (actual != t.Value) wrong.Add($"{t.Key} ({variant}): {actual}, expected {t.Value} from {t.Source}");
        }

        Assert.Empty(wrong);
    }

    /// <summary>A key added to a token file without an expected value fails here, so nothing goes untested.</summary>
    [AvaloniaFact]
    public void The_Token_Files_Hold_Exactly_The_Expected_Keys()
    {
        var theme = new Themes.Wp81.Wp81Theme();
        var dictionaries = theme.Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .Where(include => TokenFiles.Contains(include.Source!.AbsolutePath.TrimStart('/')))
            .Select(include => (ResourceDictionary)include.Loaded)
            .ToList();
        Assert.Equal(TokenFiles.Length, dictionaries.Count);

        var themed = new Dictionary<ThemeVariant, HashSet<string>>();
        var shared = new HashSet<string>();
        foreach (var dictionary in dictionaries)
        {
            shared.UnionWith(dictionary.Keys.Cast<string>());
            foreach (var (variant, themeDictionary) in dictionary.ThemeDictionaries)
            {
                var keys = themed.TryGetValue(variant, out var k) ? k : themed[variant] = [];
                keys.UnionWith(((ResourceDictionary)themeDictionary).Keys.Cast<string>());
            }
        }

        Assert.Equal(Variants.ToHashSet(), themed.Keys.ToHashSet());
        var expectedThemed = TokenExpectations.Themed.Select(t => t.Key).Order(StringComparer.Ordinal);
        Assert.Equal(expectedThemed, themed[ThemeVariant.Dark].Order(StringComparer.Ordinal));
        Assert.Equal(expectedThemed, themed[ThemeVariant.Light].Order(StringComparer.Ordinal));
        Assert.Equal(TokenExpectations.Shared.Select(t => t.Key).Order(StringComparer.Ordinal), shared.Order(StringComparer.Ordinal));
    }

    // index.json of the run: the four brushes the reference app read in each theme.
    [AvaloniaTheory]
    [InlineData("PhoneForegroundBrush", "#FFFFFFFF", "#FF000000")]
    [InlineData("PhoneBackgroundBrush", "#FF000000", "#FFFFFFFF")]
    [InlineData("ApplicationPageBackgroundThemeBrush", "#FF000000", "#FFFFFFFF")]
    [InlineData("PhoneAccentBrush", "#FF3E65FF", "#FF3E65FF")]
    public void A_DynamicResource_Brush_Follows_The_Application_Theme_Variant(string key, string dark, string light)
    {
        using var _ = Token.InstallTheme(out var _);
        var app = Application.Current!;
        var border = new Border();
        border[!Border.BackgroundProperty] = new DynamicResourceExtension(key);
        using var window = new TokenWindow(border);

        app.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(dark, BackgroundOf(border));

        app.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(light, BackgroundOf(border));

        app.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(dark, BackgroundOf(border));
    }

    // A light page inside a dark app gets the light brushes, with the light colours.
    // dark__ and light__Button__default__CommonStates.Disabled.json: ButtonDisabledForegroundThemeBrush
    // #73FFFFFF in dark, #40000000 in light.
    [AvaloniaTheory]
    [InlineData("PhoneForegroundBrush", "#FFFFFFFF", "#FF000000")]
    [InlineData("ButtonDisabledForegroundThemeBrush", "#73FFFFFF", "#40000000")]
    public void A_ThemeVariantScope_Gets_Its_Own_Variants_Brushes(string key, string dark, string light)
    {
        using var _ = Token.InstallTheme(out var _);
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        var outer = new Border();
        var inner = new Border();
        outer[!Border.BackgroundProperty] = new DynamicResourceExtension(key);
        inner[!Border.BackgroundProperty] = new DynamicResourceExtension(key);
        var scope = new ThemeVariantScope { RequestedThemeVariant = ThemeVariant.Light, Child = inner };
        using var window = new TokenWindow(new StackPanel { Children = { outer, scope } });

        Assert.Equal(dark, BackgroundOf(outer));
        Assert.Equal(light, BackgroundOf(inner));
    }

    // The accent is one colour. An app picks one of the named accents by defining SystemColorControlAccentColor
    // in Application.Resources, and every accent brush follows, in both variants, while shown.
    [AvaloniaTheory]
    [InlineData("PhoneAccentBrush")]
    [InlineData("ButtonPressedBackgroundThemeBrush")]
    [InlineData("ProgressBarBackgroundThemeBrush")]
    public void Accent_Brushes_Follow_An_App_Defined_Accent(string key)
    {
        using var _ = Token.InstallTheme(out var _);
        var app = Application.Current!;
        var border = new Border();
        border[!Border.BackgroundProperty] = new DynamicResourceExtension(key);
        using var window = new TokenWindow(border);
        Assert.True(app.TryGetResource("CornflowerAccentRedColor", null, out var red));

        try
        {
            app.RequestedThemeVariant = ThemeVariant.Dark;
            Assert.Equal("#FF3E65FF", ColorOf(border));

            app.Resources["SystemColorControlAccentColor"] = red;
            Assert.Equal("#FFE51400", ColorOf(border));

            app.RequestedThemeVariant = ThemeVariant.Light;
            Assert.Equal("#FFE51400", ColorOf(border));
        }
        finally
        {
            app.Resources.Remove("SystemColorControlAccentColor");
        }

        Assert.Equal("#FF3E65FF", ColorOf(border));
    }

    private static string BackgroundOf(Border border) => Token.Describe(border.Background, TokenKind.Brush);

    private static string ColorOf(Border border) =>
        Token.Hex(Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color);

    /// <summary>A shown headless window, closed on dispose.</summary>
    private sealed class TokenWindow : IDisposable
    {
        private readonly Window _window;

        public TokenWindow(Control content)
        {
            _window = new Window { Width = 450, Height = 800, Content = content };
            _window.Show();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            _window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
