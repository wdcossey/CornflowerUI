using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The holes test: every templated control without a Wp81 theme is listed in
/// <c>Completeness/Holes.txt</c>, and nothing else is. Its count is the measure of era 1's
/// completeness (ADR 0002). The first test checks the real theme; the others prove each part of
/// the check on themes, controls and lists made here.
/// </summary>
public class HolesTests
{
    [AvaloniaFact]
    public void Holes_Txt_Lists_Every_Templated_Control_Without_A_Wp81_Theme()
    {
        var holes = Holes.In(new Wp81Theme(), Holes.TemplatedControls(new SimpleTheme(), typeof(CornflowerClock).Assembly));

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
    public void Cornflowers_Own_Templated_Controls_Count_By_Their_Style_Key()
    {
        var controls = Holes.TemplatedControls(new SimpleTheme(), typeof(HolesTests).Assembly);

        Assert.Contains(typeof(OurControl), controls);              // Simple has no theme for it
        Assert.DoesNotContain(typeof(OurStyledAsBase), controls);   // its theme is OurControl's
    }

    // Simple's side only has to show that Avalonia templates a control, so either variant will do.
    [AvaloniaFact]
    public void A_Control_Simple_Themes_In_One_Variant_Only_Is_Templated() =>
        Assert.True(Holes.HasThemeInEither(DarkOnly(typeof(Button)), typeof(Button)));

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
    public void A_ControlTheme_In_One_Variant_Only_Leaves_A_Hole() =>
        Assert.Equal(["Button"], Holes.In(DarkOnly(typeof(Button)), [typeof(Button)]));

    // A theme copied from another control's file, with its key changed and its TargetType not.
    [AvaloniaFact]
    public void A_ControlTheme_Targeting_Another_Type_Leaves_A_Hole() =>
        Assert.Equal(["ToggleButton"],
            Holes.In(ThemeOf(typeof(ToggleButton), new ControlTheme(typeof(Button))), [typeof(ToggleButton)]));

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
        Assert.Equal(["Holes.txt isn't in ordinal order: TabStripItem comes before TabbedPage."],
            Holes.Compare(["Button", "TabbedPage", "TabStripItem"], ["Button", "TabStripItem", "TabbedPage"]));
        Assert.Equal(["Button is listed more than once in Holes.txt."], Holes.Compare(["Button", "Button"], ["Button"]));
    }

    [Fact]
    public void Comments_And_Blank_Lines_Are_Not_Listed() =>
        Assert.Equal(["Button", "Slider"], Holes.Parse("# The holes\r\n\r\nButton  # a note\r\n  # another\nSlider\n"));

    private static Styles DarkOnly(Type control)
    {
        var theme = new Styles();
        theme.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [control] = new ControlTheme(control),
        };
        return theme;
    }

    private static Styles ThemeOf(Type key, object value)
    {
        var theme = new Styles();
        theme.Resources[key] = value;
        return theme;
    }

    /// <summary>A templated control of the kind <c>Cornflower.Controls</c> will hold.</summary>
    public class OurControl : TemplatedControl;

    /// <summary>A subclass that keeps its base's theme.</summary>
    public class OurStyledAsBase : OurControl
    {
        protected override Type StyleKeyOverride => typeof(OurControl);
    }
}
