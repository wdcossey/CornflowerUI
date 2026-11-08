using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Discovery;

/// <summary>
/// Theme files are found from a build-time manifest, not a registration list. A broken manifest
/// would silently unstyle every control, so these tests make it fail CI.
/// </summary>
public class DiscoveryTests
{
    private static readonly Uri TestsBaseUri = new("avares://Cornflower.Tests/");

    [Fact]
    public void Manifest_Lists_Every_Matching_File_With_Forward_Slashes() =>
        Assert.Equal(
            ["Discovery/Fixture/First.axaml", "Discovery/Fixture/Nested/Second.axaml"],
            Cornflower.Tests.XamlManifest.Paths.Order(StringComparer.Ordinal));

    [AvaloniaFact]
    public void Every_Manifest_Path_Loads_As_Compiled_Xaml()
    {
        foreach (var path in Cornflower.Tests.XamlManifest.Paths)
        {
            var include = new ResourceInclude(TestsBaseUri) { Source = new Uri(TestsBaseUri, path) };
            Assert.NotEmpty(include.Loaded.Keys);
        }
    }

    [Fact]
    public void Theme_Merge_Order_Is_Ordinal()
    {
        string[] paths = ["Tokens/Z.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Controls/B.axaml"];

        Assert.Equal(
            ["Controls/B.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Tokens/Z.axaml"],
            Wp81Theme.OrderForMerge(paths));
    }

    [AvaloniaFact]
    public void Theme_Merges_Exactly_Its_Manifest_In_Order()
    {
        var sources = new Wp81Theme().Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .Select(include => include.Source!.AbsolutePath.TrimStart('/'));

        Assert.Equal(
            Wp81Theme.OrderForMerge(Cornflower.Themes.Wp81.XamlManifest.Paths),
            sources);
    }
}
