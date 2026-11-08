using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;

namespace Cornflower.Tests.Spikes;

/// <summary>Has a theme of its own, <c>BasedOn</c> ComboBox's (<c>SubclassThemes/DerivedComboBox.axaml</c>).</summary>
public class BasedOnComboBox : ComboBox;

/// <summary>Has no theme of its own, and asks to be styled as a ComboBox.</summary>
public class KeyedComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);
}

/// <summary>Has no theme of its own and doesn't override <c>StyleKeyOverride</c>.</summary>
public class PlainComboBox : ComboBox;

/// <summary>A <c>Button</c> subclass with no theme and no <c>StyleKeyOverride</c>.</summary>
public class PlainButton : Button;

/// <summary>
/// Spike 3: how a subclass of a themed control gets its theme. <c>SubclassThemes/BaseComboBox.axaml</c>
/// stands in for the era's <c>ComboBox</c> theme. <c>DerivedComboBox.axaml</c>, a separate file merged
/// into the same dictionary as <c>Wp81Theme</c> merges its control files, is based on it with
/// <c>BasedOn="{StaticResource {x:Type ComboBox}}"</c> and adds one setter.
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: <c>BasedOn</c> across two merged files works. <c>StyleKeyOverride</c>
/// returning the base type gets the base's theme unchanged. A subclass with neither gets no
/// <c>ControlTheme</c> at all: theme lookup is by the exact <c>StyleKey</c> and doesn't walk base
/// types. It then behaves like any unthemed control (see <c>MissingThemeTests</c>), silently.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class SubclassThemeTests
{
    private const string BaseRoot = "Border:BaseComboBoxRoot";

    private static IDisposable InstallBaseAndDerived()
    {
        var theme = new Styles();
        theme.Resources.MergedDictionaries.Add(Spike.ResourceFile("SubclassThemes/BaseComboBox.axaml"));
        theme.Resources.MergedDictionaries.Add(Spike.ResourceFile("SubclassThemes/DerivedComboBox.axaml"));
        return Spike.InApplication(theme);
    }

    [AvaloniaFact]
    public void The_Base_Theme_Themes_ComboBox()
    {
        using var installed = InstallBaseAndDerived();
        var comboBox = new ComboBox();
        using var scene = new SpikeWindow(comboBox);

        Assert.Equal([BaseRoot], Spike.TemplateRoot(comboBox));
        Assert.Equal("from-base", comboBox.Tag);
    }

    [AvaloniaFact]
    public void A_BasedOn_Theme_Gets_The_Base_Template_And_Setters_And_Adds_Its_Own()
    {
        using var installed = InstallBaseAndDerived();
        var comboBox = new BasedOnComboBox();
        using var scene = new SpikeWindow(comboBox);

        Assert.Equal([BaseRoot], Spike.TemplateRoot(comboBox));
        Assert.Equal("from-base", comboBox.Tag);
        Assert.Equal(0.5, comboBox.Opacity);
    }

    [AvaloniaFact]
    public void StyleKeyOverride_To_The_Base_Type_Gets_The_Base_Theme_Unchanged()
    {
        using var installed = InstallBaseAndDerived();
        var comboBox = new KeyedComboBox();
        using var scene = new SpikeWindow(comboBox);

        Assert.Equal([BaseRoot], Spike.TemplateRoot(comboBox));
        Assert.Equal("from-base", comboBox.Tag);
        Assert.Equal(1, comboBox.Opacity);
    }

    // Doesn't work: no fallback to the base type's theme, and no warning.
    [AvaloniaFact]
    public void A_Subclass_With_No_Theme_And_No_StyleKeyOverride_Gets_Nothing()
    {
        using var installed = InstallBaseAndDerived();
        using var log = new WarningLog();
        var comboBox = new PlainComboBox();
        using var scene = new SpikeWindow(comboBox);

        Assert.Null(comboBox.Template);
        Assert.Empty(Spike.TemplateRoot(comboBox));
        Assert.Null(comboBox.Tag);
        Assert.Empty(log.Events);
    }

    // Doesn't work either: a content control subclass gets ContentControl's bare fallback, not the
    // base theme's template, even with SimpleTheme installed. It looks half right, which is worse.
    [AvaloniaFact]
    public void A_Button_Subclass_With_No_Theme_Gets_The_Bare_Fallback_Not_Simples_Button()
    {
        var simple = new SimpleTheme();
        using var installed = Spike.InApplication(simple);
        var button = new PlainButton { Content = "x" };
        using var scene = new SpikeWindow(button);

        Assert.IsType<FuncControlTemplate>(button.Template);
        Assert.NotSame(MissingThemeTests.TemplateOf(simple, typeof(Button)), button.Template);
        Assert.Equal(["ContentPresenter:PART_ContentPresenter"], Spike.TemplateRoot(button));
    }
}
