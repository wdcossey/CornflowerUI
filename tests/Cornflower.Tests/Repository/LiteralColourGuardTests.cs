using System.Text;

namespace Cornflower.Tests.Repository;

/// <summary>
/// No literal colours outside a theme's <c>Tokens/</c> (rule 5). The last test scans the real repository; the
/// others prove each check on files made in memory.
/// </summary>
public class LiteralColourGuardTests
{
    private static IReadOnlyList<string> Check(string path, string content) =>
        LiteralColourGuard.Check([(path, () => new MemoryStream(Encoding.UTF8.GetBytes(content)))]);

    private const string ControlTheme = "src/Cornflower.Themes.Wp81/Controls/Button.axaml";

    private static string Xaml(string body) =>
        $"""<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">{body}</ResourceDictionary>""";

    [Theory]
    [InlineData("""<Border Background="#FF3E65FF" />""")]
    [InlineData("""<Border Background="#FFF" />""")]
    [InlineData("""<Border BorderBrush="Red" />""")]
    [InlineData("""<Color>#40000000</Color>""")]
    public void A_Colour_In_A_Control_Theme_Fails(string body) =>
        Assert.Contains("literal colour", Assert.Single(Check(ControlTheme, Xaml(body))));

    [Theory]
    [InlineData("""<Border Background="{DynamicResource PhoneAccentBrush}" />""")]
    [InlineData("""<Border Background="Transparent" />""")]
    [InlineData("""<Border Width="100" HorizontalAlignment="Left" />""")]
    [InlineData("""<!-- Pressed: #FF3E65FF, read on the phone --><Border />""")]
    public void A_Resource_Transparent_Or_Comment_Passes(string body) =>
        Assert.Empty(Check(ControlTheme, Xaml(body)));

    [Fact]
    public void A_Colour_In_Tokens_Passes() =>
        Assert.Empty(Check("src/Cornflower.Themes.Wp81/Tokens/Colors.axaml", Xaml("""<Color x:Key="K">#FF000000</Color>""")));

    [Theory]
    [InlineData("""var c = Color.Parse("#FF3E65FF");""")]
    [InlineData("""var c = Color.FromRgb(1, 2, 3);""")]
    [InlineData("""Background = Brushes.Red;""")]
    [InlineData("""var c = Colors.White;""")]
    public void A_Colour_In_Code_Fails(string line) =>
        Assert.NotEmpty(Check("src/Cornflower.Controls/Tilt.cs", line));

    [Theory]
    [InlineData("""Background = Brushes.Transparent;""")]
    [InlineData("""// See #19 and #123.""")]
    public void Transparent_And_Issue_Numbers_Pass_In_Code(string line) =>
        Assert.Empty(Check("src/Cornflower.Controls/Tilt.cs", line));

    [Fact]
    public void Tests_And_Tools_Are_Not_Checked() =>
        Assert.Empty(Check("tests/Cornflower.Tests/Spikes/ThemeSwap/EraA.axaml", Xaml("""<Border Background="#FFFF0000" />""")));

    [Fact]
    public void The_Shipped_Code_Holds_No_Literal_Colour_Outside_Tokens() =>
        Assert.Empty(LiteralColourGuard.Check(MicrosoftFileGuard.RepositoryFiles()));
}
