using Avalonia.Headless.XUnit;
using Cornflower.Tests.Controls.Fixture;
using Cornflower.Tests.Tokens;

namespace Cornflower.Tests.Controls;

/// <summary>
/// An 8.1 page sets <c>Style="{StaticResource BodyTextBlockStyle}"</c>; an Avalonia one sets <c>Theme</c> the
/// same way, with <c>Wp81Theme</c> in <c>Application.Styles</c>. That changes the application, so it runs
/// with the token tests, on its own.
/// </summary>
[Collection(TokenCollection.Name)]
public class TextStyleStaticResourceTests
{
    [AvaloniaFact]
    public void A_Style_Set_By_StaticResource_Is_The_Themes()
    {
        using var _ = Token.InstallTheme(out var theme);
        var view = new StaticTextStyles();
        using var scene = new Scene(view, themes: []);

        theme.TryGetResource("BodyTextBlockStyle", null, out var body);
        Assert.Same(body, view.Body.Theme);
        Assert.Equal(24, view.Body.LineHeight);
        Assert.Equal(18.14, view.Body.FontSize);
    }
}
