using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 2: what does a control with no <c>ControlTheme</c> do, and does a theme for one control
/// sit cleanly on top of <c>SimpleTheme</c>? <c>TestApp</c> installs no theme.
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: <c>ContentControl</c>'s <c>Template</c> property has a default value,
/// a bare <c>ContentPresenter</c>, so <c>Button</c>, <c>Window</c> and other content controls still
/// show their content, unstyled. Other templated controls (<c>TextBox</c>, <c>ComboBox</c>, <c>ListBox</c>)
/// get no template: no visual children and no height. Nothing throws and nothing is logged at
/// <c>Warning</c> or above. A theme added after <c>SimpleTheme</c> wins for its own controls, and the
/// rest fall through to Simple.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class MissingThemeTests
{
    private const string BarePresenter = "ContentPresenter:PART_ContentPresenter";

    [AvaloniaFact]
    public void With_No_Theme_A_Button_Gets_A_Bare_ContentPresenter()
    {
        var button = new Button { Content = "x" };
        using var scene = new SpikeWindow(new StackPanel { Children = { button } });

        Assert.False(button.IsSet(TemplatedControl.TemplateProperty));     // it's the property's default
        Assert.IsType<FuncControlTemplate>(button.Template);
        Assert.Equal([BarePresenter], Spike.TemplateRoot(button));
        Assert.True(button.DesiredSize.Height > 0);                         // the text is measured
        Assert.Null(button.Background);
    }

    [AvaloniaFact]
    public void With_No_Theme_A_Window_Still_Presents_Its_Content()
    {
        var content = new Border();
        using var scene = new SpikeWindow(content, width: 300, height: 200);

        Assert.IsType<FuncControlTemplate>(scene.Window.Template);
        Assert.Equal([BarePresenter], Spike.TemplateRoot(scene.Window));
        Assert.Equal(new Rect(0, 0, 300, 200), content.Bounds);
    }

    // Doesn't work: a TextBox with no theme is there, but empty and zero high.
    [AvaloniaFact]
    public void With_No_Theme_A_TextBox_Has_No_Template_And_No_Height()
    {
        var textBox = new TextBox { Text = "hello" };
        using var scene = new SpikeWindow(new StackPanel { Children = { textBox } });

        Assert.Null(textBox.Template);
        Assert.Empty(Spike.TemplateRoot(textBox));
        Assert.Equal(new Size(0, 0), textBox.DesiredSize);
        Assert.Equal(0, textBox.Bounds.Height);
    }

    [AvaloniaFact]
    public void With_No_Theme_Nothing_Throws_Or_Logs_A_Warning()
    {
        using var log = new WarningLog();

        using (new SpikeWindow(new StackPanel { Children = { new Button { Content = "x" }, new TextBox { Text = "hello" } } }))
        {
        }

        Assert.Empty(log.Events);
    }

    [AvaloniaFact]
    public void Over_Simple_Our_Button_Theme_Wins_And_Other_Controls_Fall_Through()
    {
        var simple = new SimpleTheme();
        using var installed = Spike.InApplication(simple, Spike.StylesFile("ThemeSwap/EraA.axaml"));
        var button = new Button { Content = "x" };
        var textBox = new TextBox { Text = "hello" };
        using var scene = new SpikeWindow(new StackPanel { Children = { button, textBox } });

        Assert.Equal(["Border:EraA"], Spike.TemplateRoot(button));
        Assert.Same(TemplateOf(simple, typeof(TextBox)), textBox.Template);
        Assert.Same(TemplateOf(simple, typeof(Window)), scene.Window.Template);
        Assert.True(textBox.Bounds.Height > 0);
    }

    // Order matters: the later Styles wins. A theme put in front of Simple loses to it.
    [AvaloniaFact]
    public void Under_Simple_Our_Button_Theme_Loses()
    {
        var simple = new SimpleTheme();
        using var installed = Spike.InApplication(Spike.StylesFile("ThemeSwap/EraA.axaml"), simple);
        var button = new Button { Content = "x" };
        using var scene = new SpikeWindow(button);

        Assert.Same(TemplateOf(simple, typeof(Button)), button.Template);
    }

    /// <summary>The template a theme's <c>ControlTheme</c> for <paramref name="type"/> sets.</summary>
    internal static object? TemplateOf(Styles theme, Type type)
    {
        Assert.True(theme.TryGetResource(type, null, out var found));
        return ((ControlTheme)found!).Setters.OfType<Setter>()
            .Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
    }
}
