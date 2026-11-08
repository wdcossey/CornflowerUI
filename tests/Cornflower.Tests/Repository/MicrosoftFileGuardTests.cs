using System.Security.Cryptography;
using System.Text;

namespace Cornflower.Tests.Repository;

/// <summary>
/// The repository holds no Microsoft fonts, templates or SDK files (rule 6). The last test scans the
/// real repository. The others prove each check catches what it is for, on files made in memory, so
/// no file with a forbidden name or forbidden bytes has to exist in the repository.
/// </summary>
public class MicrosoftFileGuardTests
{
    private static readonly Dictionary<string, string> NoHashes = [];

    private static (string, Func<Stream>) InMemory(string path, string content = "ours") =>
        (path, () => new MemoryStream(Encoding.UTF8.GetBytes(content)));

    private static IReadOnlyList<string> Check(string path) =>
        MicrosoftFileGuard.Check([InMemory(path)], NoHashes, NoHashes);

    private static string Sha256(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    [Fact]
    public void An_Ordinary_File_Passes() =>
        Assert.Empty(Check("src/Cornflower.Themes.Wp81/Controls/Button.axaml"));

    [Theory]
    [InlineData("assets/SegoeWP.ttf")]
    [InlineData("assets/Font.OTF")]
    [InlineData("assets/font.woff2")]
    public void A_Font_Fails(string path) =>
        Assert.Contains("font", Assert.Single(Check(path)));

    [Fact]
    public void A_Bundled_Font_Passes()
    {
        var allowed = new Dictionary<string, string> { ["src/Cornflower.Fonts/Selawik.ttf"] = Sha256("ours") };

        Assert.Empty(MicrosoftFileGuard.Check([InMemory("src/Cornflower.Fonts/Selawik.ttf")], NoHashes, allowed));
    }

    [Fact]
    public void Other_Bytes_At_A_Bundled_Fonts_Path_Fail()
    {
        var allowed = new Dictionary<string, string> { ["src/Cornflower.Fonts/Selawik.ttf"] = Sha256("ours") };

        var problem = Assert.Single(MicrosoftFileGuard.Check([InMemory("src/Cornflower.Fonts/Selawik.ttf", "theirs")], NoHashes, allowed));
        Assert.Contains("isn't the bytes", problem);
    }

    // The real list: Selawik's files are allowed only at their own paths, and nothing else beside them.
    [Theory]
    [InlineData("src/Cornflower.Themes.Wp81/Fonts/Selawik/SegoeWP.ttf")]
    [InlineData("src/Cornflower.Themes.Wp81/Fonts/Selawik/selawksl.ttf")]
    [InlineData("src/Cornflower.Themes.Wp81/Fonts/selawk.ttf")]
    [InlineData("src/Cornflower.Themes.Wp81/Fonts/Selawik/selawk.woff2")]
    public void A_Font_Beside_The_Bundled_Ones_Fails(string path) =>
        Assert.Contains("isn't one Cornflower bundles",
            Assert.Single(MicrosoftFileGuard.Check([InMemory(path)], NoHashes, MicrosoftFileGuard.AllowedFonts)));

    [Fact]
    public void The_Allowed_Fonts_Are_Selawik_And_Nothing_Else()
    {
        Assert.Equal(5, MicrosoftFileGuard.AllowedFonts.Count);
        Assert.All(MicrosoftFileGuard.AllowedFonts, font =>
        {
            Assert.StartsWith("src/Cornflower.Themes.Wp81/Fonts/Selawik", font.Key);
            Assert.Matches(@"/selawk(|b|l|sb|sl)\.ttf$", font.Key);
            Assert.Matches("^[0-9a-f]{64}$", font.Value);
        });
    }

    [Theory]
    [InlineData("ref/generic.xaml")]
    [InlineData("ref/Generic.xaml")]
    [InlineData("ref/ThemeResources.xaml")]
    [InlineData("ref/Windows.winmd")]
    [InlineData("ref/Windows.Foundation.WinMD")]
    public void An_Sdk_File_Name_Fails(string path) =>
        Assert.Contains("SDK file", Assert.Single(Check(path)));

    [Fact]
    public void A_Pulled_File_Fails_Under_Any_Name()
    {
        const string content = "bytes of a pulled file";
        var hashes = new Dictionary<string, string> { [Sha256(content)] = "generic.xaml" };

        var problem = Assert.Single(
            MicrosoftFileGuard.Check([InMemory("docs/notes.txt", content)], hashes, NoHashes));
        Assert.Contains("Microsoft's generic.xaml", problem);
    }

    [Fact]
    public void The_Hash_List_Is_Embedded_And_Well_Formed()
    {
        Assert.True(MicrosoftFileGuard.MicrosoftHashes.Count > 100);
        Assert.All(MicrosoftFileGuard.MicrosoftHashes.Keys, h => Assert.Matches("^[0-9a-f]{64}$", h));
    }

    [Fact]
    public void The_Repository_Holds_No_Microsoft_Files() =>
        Assert.Empty(MicrosoftFileGuard.Check(
            MicrosoftFileGuard.RepositoryFiles(), MicrosoftFileGuard.MicrosoftHashes, MicrosoftFileGuard.AllowedFonts));
}
