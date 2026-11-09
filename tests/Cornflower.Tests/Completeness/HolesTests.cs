using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The holes test: every templated Avalonia control without a Wp81 theme is listed in
/// <c>Completeness/Holes.txt</c>, and nothing else is. Its count is the measure of era 1's
/// completeness (ADR 0002). The first test checks the real theme; the others prove each part of
/// the check on themes and lists made in memory.
/// </summary>
public class HolesTests
{
    [AvaloniaFact]
    public void Holes_Txt_Lists_Every_Templated_Control_Without_A_Wp81_Theme()
    {
        var holes = Holes.In(new Wp81Theme(), Holes.TemplatedControls(new SimpleTheme()));

        var problems = Holes.Compare(Holes.Listed(), holes);

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [AvaloniaFact]
    public void The_Templated_Controls_Are_The_Ones_Simple_Themes()
    {
        var controls = Holes.TemplatedControls(new SimpleTheme());

        Assert.Contains(typeof(Button), controls);
        Assert.Contains(typeof(TextBox), controls);
        Assert.Contains(typeof(PopupRoot), controls);              // no parameterless constructor: found by type, not made
        Assert.Contains(typeof(ManagedFileChooser), controls);     // in Avalonia.Dialogs, not Avalonia.Controls
        Assert.DoesNotContain(typeof(ContentControl), controls);   // templated, but Simple leaves it the bare fallback
        Assert.DoesNotContain(typeof(UserControl), controls);
        Assert.DoesNotContain(typeof(MaskedTextBox), controls);    // styled as a TextBox
    }

    [AvaloniaFact]
    public void A_Control_Without_A_ControlTheme_Is_A_Hole()
    {
        Assert.Equal(["Button"], Holes.In(new Styles(), [typeof(Button)]));
        Assert.Equal(["Button"], Holes.In(ThemeOf(typeof(Button), "not a theme"), [typeof(Button)]));
    }

    [AvaloniaFact]
    public void A_Control_With_A_ControlTheme_Is_Not_A_Hole() =>
        Assert.Empty(Holes.In(ThemeOf(typeof(Button), new ControlTheme(typeof(Button))), [typeof(Button)]));

    [AvaloniaFact]
    public void A_ControlTheme_In_One_Variant_Only_Leaves_A_Hole()
    {
        var darkOnly = new Styles();
        darkOnly.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [typeof(Button)] = new ControlTheme(typeof(Button)),
        };

        Assert.Equal(["Button"], Holes.In(darkOnly, [typeof(Button)]));
    }

    [Fact]
    public void A_Hole_Not_Listed_Fails() =>
        Assert.Equal(["Slider has no Wp81 theme: theme it, or add it to Holes.txt."],
            Holes.Compare(["Button"], ["Button", "Slider"]));

    [Fact]
    public void A_Listed_Control_That_Is_Not_A_Hole_Fails() =>
        Assert.Equal(["Button isn't a hole: delete its line from Holes.txt."],
            Holes.Compare(["Button", "Slider"], ["Slider"]));

    [Fact]
    public void A_List_Out_Of_Order_Or_Repeating_Fails()
    {
        Assert.Equal(["Holes.txt isn't in ordinal order."], Holes.Compare(["Slider", "Button"], ["Button", "Slider"]));
        Assert.Equal(["Button is listed more than once in Holes.txt."], Holes.Compare(["Button", "Button"], ["Button"]));
    }

    [Fact]
    public void Comments_And_Blank_Lines_Are_Not_Listed() =>
        Assert.Equal(["Button", "Slider"], Holes.Parse("# The holes\r\n\r\nButton\r\n  # a note\nSlider\n"));

    private static Styles ThemeOf(Type key, object value)
    {
        var theme = new Styles();
        theme.Resources[key] = value;
        return theme;
    }
}
