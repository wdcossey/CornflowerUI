using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 1: can the era be swapped at run time? <c>ThemeSwap/EraA.axaml</c> and <c>EraB.axaml</c>
/// each give <c>Button</c> a template whose root is named after the era, and each define
/// <c>SpikeAccentBrush</c> (red in A, blue in B), which the theme sets as a <c>DynamicResource</c>.
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: a control picks its <c>ControlTheme</c> once, when it joins a logical
/// tree. Replacing the <c>Styles</c> that hold it doesn't re-template a control already shown, but
/// <c>DynamicResource</c> brushes follow at once. Taking the control out of the tree and putting it
/// back re-templates it with the new theme.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class ThemeSwapTests
{
    private static Color? ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    /// <summary>Where the era's styles live: <c>Application.Styles</c> or the window's own.</summary>
    public enum Host { Application, Window }

    private sealed class Fixture : IDisposable
    {
        private readonly bool _inApplication;
        private readonly SpikeWindow _scene;

        public Button Button { get; } = new() { Content = "x" };
        public Border ResourceBorder { get; } = new();
        public StackPanel Root { get; }
        public Window Window => _scene.Window;
        public Styles StylesHost { get; }

        public Fixture(Host host)
        {
            ResourceBorder.Bind(Border.BackgroundProperty, ResourceBorder.GetResourceObservable("SpikeAccentBrush"));
            Root = new StackPanel { Children = { Button, ResourceBorder } };

            if (host == Host.Application)
            {
                _inApplication = true;
                StylesHost = Application.Current!.Styles;
                StylesHost.Add(Spike.StylesFile("ThemeSwap/EraA.axaml"));
                _scene = new SpikeWindow(Root);
            }
            else
            {
                // The styles go in before the content: see Styles_Added_After_The_Content_Do_Not_Theme_It.
                _scene = new SpikeWindow(Root, w => w.Styles.Add(Spike.StylesFile("ThemeSwap/EraA.axaml")));
                StylesHost = _scene.Window.Styles;
            }
        }

        /// <summary>Replaces era A with era B in place, at the same index.</summary>
        public void SwapToB()
        {
            var index = StylesHost.Count - 1;
            StylesHost[index] = Spike.StylesFile("ThemeSwap/EraB.axaml");
        }

        public void Dispose()
        {
            _scene.Dispose();
            if (_inApplication) StylesHost.RemoveAt(StylesHost.Count - 1);   // whichever era is there now
        }
    }

    [AvaloniaTheory]
    [InlineData(Host.Application)]
    [InlineData(Host.Window)]
    public void The_First_Era_Themes_The_Button(Host host)
    {
        using var f = new Fixture(host);

        Assert.Equal(["Border:EraA"], Spike.TemplateRoot(f.Button));
        Assert.Equal(Colors.Red, ColorOf(f.Button.Background));
    }

    // Doesn't work: a shown control keeps the template of the theme it was given.
    [AvaloniaTheory]
    [InlineData(Host.Application)]
    [InlineData(Host.Window)]
    public void Replacing_The_Era_Does_Not_Retemplate_A_Shown_Control(Host host)
    {
        using var f = new Fixture(host);

        f.SwapToB();
        Spike.Settle();
        f.Window.UpdateLayout();

        Assert.Equal(["Border:EraA"], Spike.TemplateRoot(f.Button));
    }

    [AvaloniaTheory]
    [InlineData(Host.Application)]
    [InlineData(Host.Window)]
    public void Replacing_The_Era_Moves_DynamicResource_Brushes_At_Once(Host host)
    {
        using var f = new Fixture(host);

        f.SwapToB();

        // No dispatcher jobs run: the change is synchronous. The button's brush comes from era A's
        // theme setter, still applied, now resolving the key in era B's resources.
        Assert.Equal(Colors.Blue, ColorOf(f.Button.Background));
        Assert.Equal(Colors.Blue, ColorOf(f.ResourceBorder.Background));
    }

    [AvaloniaTheory]
    [InlineData(Host.Application)]
    [InlineData(Host.Window)]
    public void Replacing_The_Era_Themes_Controls_Added_Afterwards(Host host)
    {
        using var f = new Fixture(host);

        f.SwapToB();
        var later = new Button();
        f.Root.Children.Add(later);
        f.Window.UpdateLayout();

        Assert.Equal(["Border:EraB"], Spike.TemplateRoot(later));
    }

    // The nudge: leaving the logical tree and joining it again makes a control look its theme up again.
    [AvaloniaTheory]
    [InlineData(Host.Application)]
    [InlineData(Host.Window)]
    public void Detaching_And_Reattaching_The_Content_Retemplates_With_The_New_Era(Host host)
    {
        using var f = new Fixture(host);

        f.SwapToB();
        f.Window.Content = null;
        f.Window.Content = f.Root;
        Spike.Settle();

        Assert.Equal(["Border:EraB"], Spike.TemplateRoot(f.Button));
        Assert.Equal(Colors.Blue, ColorOf(f.Button.Background));
    }

    // Doesn't work: the same rule seen from the other side. A control that joined the tree before
    // any theme existed keeps Avalonia's bare fallback, even though the window isn't shown yet.
    [AvaloniaFact]
    public void Styles_Added_After_The_Content_Do_Not_Theme_It()
    {
        var button = new Button { Content = "x" };
        var window = new Window { Content = button };
        window.Styles.Add(Spike.StylesFile("ThemeSwap/EraA.axaml"));
        window.Show();
        Spike.Settle();
        try
        {
            Assert.Equal(["ContentPresenter:PART_ContentPresenter"], Spike.TemplateRoot(button));
            Assert.Null(button.Background);
        }
        finally
        {
            window.Close();
        }
    }
}
