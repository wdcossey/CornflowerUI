using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The Buttons in the templates of controls Wp81 doesn't theme yet (the holes) keep the theme under Wp81: with
/// Simple then Wp81, each is laid out as with Simple alone, and none is Wp81's Button. In Avalonia 12.1.4, Simple's
/// templates for CalendarItem (in Calendar), CalendarDatePicker, CommandBar, NavigationPage, PipsPager and the managed
/// file dialogs hold plain Buttons with no theme of their own (read from each of Simple's templates, built on its own).
/// </summary>
public class HolePartsTests
{
    /// <summary>Every hole that can be shown on its own in a window; a top level or a popup's host can't.</summary>
    private static IEnumerable<Type> ShowableHoles() =>
        Holes.TemplatedControls(new SimpleTheme())
            .Where(type => Holes.Listed().Contains(type.Name))
            .Where(type => !typeof(TopLevel).IsAssignableFrom(type) && type != typeof(OverlayPopupHost) && type.GetConstructor(Type.EmptyTypes) is not null);

    /// <summary>The plain Buttons in <paramref name="control"/>'s templates, nested ones included, in tree order.</summary>
    private static List<Button> PlainButtons(Control control) =>
        control.GetVisualDescendants().OfType<Button>().Where(b => b.GetType() == typeof(Button)).ToList();

    private static ControlTheme ButtonThemeOf(IResourceNode theme) =>
        theme.TryGetResource(typeof(Button), ThemeVariant.Dark, out var found) ? (ControlTheme)found! : throw new InvalidOperationException("No Button theme.");

    // Every hole shown with Simple then Wp81: each plain Button in its templates has Simple's Button theme, as with
    // Simple alone, and as many of them.
    [AvaloniaFact]
    public void Every_Holes_Buttons_Keep_The_Theme_Under_Wp81()
    {
        var problems = new List<string>();
        var holes = ShowableHoles().ToList();
        var withButtons = 0;
        var fellBack = new List<string>();

        foreach (var type in holes)
        {
            var alone = (Control)Activator.CreateInstance(type)!;
            int expected;
            using (new Scene(alone, themes: [new SimpleTheme()]))
                expected = PlainButtons(alone).Count;

            var simple = new SimpleTheme();
            var control = (Control)Activator.CreateInstance(type)!;
            using var scene = new Scene(control, themes: [simple, new Wp81Theme()]);
            var buttons = PlainButtons(control);
            if (buttons.Count > 0) withButtons++;

            if (buttons.Count != expected)
                problems.Add($"{type.Name}: {buttons.Count} plain Buttons, {expected} with Simple alone");
            // Simple's own templates give some of their Buttons a named theme (the pickers' flyout buttons, SplitButton's
            // halves); the rest have none, and take the Button theme: Simple's, not Wp81's.
            foreach (var button in buttons.Where(b => b.Theme is null || CfTilt.GetIsEnabled(b)))
                problems.Add($"{type.Name}: {button.Name} has Wp81's theme");
            foreach (var button in buttons.Where(b => ReferenceEquals(b.Theme, ButtonThemeOf(simple))))
                fellBack.Add($"{type.Name}.{button.Name}");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        Assert.True(holes.Count > 40, $"only {holes.Count} holes shown");
        Assert.True(withButtons >= 5, $"only {withButtons} holes showed plain Buttons");
        // Calendar's three, CalendarDatePicker's, CommandBar's overflow, NavigationPage's back, PipsPager's two, the dialogs'.
        Assert.Contains("Calendar.PART_HeaderButton", fellBack);
        Assert.Contains("CommandBar.PART_OverflowButton", fellBack);
        Assert.Contains("NavigationPage.PART_BackButton", fellBack);
        Assert.Contains("PipsPager.PART_NextButton", fellBack);
    }

    // A Calendar's header and arrows, laid out as with Simple alone: as wide (the text, Wp81's font through Simple's
    // keys, makes the header 1.25 shorter either way, so heights aren't compared), and narrower than the phone's minimum.
    [AvaloniaFact]
    public void A_Calendars_Header_And_Arrows_Keep_Simples_Size()
    {
        var alone = new Calendar();
        using var simple = new Scene(alone, themes: [new SimpleTheme()]);
        var expected = PlainButtons(alone).Select(b => (b.Name, b.Bounds.Width)).ToList();

        var calendar = new Calendar();
        using var scene = new Scene(calendar);
        var got = PlainButtons(calendar).Select(b => (b.Name, b.Bounds.Width)).ToList();

        Assert.Equal(["PART_PreviousButton", "PART_HeaderButton", "PART_NextButton"], got.Select(b => b.Name));
        Assert.Equal(expected, got);
        Assert.All(got, b => Assert.True(b.Width < 109, $"{b.Name} is {b.Width} wide: the phone's minimum"));
    }

    // An app's own templated control isn't a hole: a plain Button in its template is the phone's.
    [AvaloniaFact]
    public void A_Button_In_An_Apps_Own_Template_Is_Wp81s()
    {
        var mine = new AppControl
        {
            Template = new FuncControlTemplate<AppControl>((_, _) => new Border { Child = new Button { Name = "Inner", Content = "inner" } }),
        };
        using var scene = new Scene(mine);

        var inner = PlainButtons(mine).Single();
        Assert.True(CfTilt.GetIsEnabled(inner));
        Assert.Equal(109, inner.MinWidth);
    }

    // With nothing under Wp81, a hole's Button keeps Wp81's: there is no other.
    [AvaloniaFact]
    public void With_Nothing_Under_Wp81_A_Holes_Button_Keeps_Wp81s()
    {
        var host = new AppControl
        {
            Template = new FuncControlTemplate<AppControl>((_, _) => new Button { Name = "Inner", Content = "inner" }),
        };
        var theme = new ControlTheme(typeof(AppControl));
        var under = new Styles { Resources = { [typeof(AppControl)] = theme } };
        using var scene = new Scene(host, themes: [new Wp81Theme(), under]);   // AppControl's theme is above Wp81, not under it

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(host).Single()));
    }

    [AvaloniaFact]
    public void A_Holes_Button_Takes_The_Theme_Under_Wp81_Not_Another()
    {
        var host = new AppControl
        {
            Template = new FuncControlTemplate<AppControl>((_, _) => new Button { Name = "Inner", Content = "inner" }),
        };
        var underButton = new ControlTheme(typeof(Button))
        {
            Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((_, _) => new Border { Name = "Under" })) },
        };
        var under = new Styles { Resources = { [typeof(AppControl)] = new ControlTheme(typeof(AppControl)), [typeof(Button)] = underButton } };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);
        Dispatcher.UIThread.RunJobs();

        var inner = PlainButtons(host).Single();
        Assert.Same(underButton, inner.Theme);
        Assert.Contains(inner.GetVisualDescendants(), v => v is Border { Name: "Under" });
    }

    // A control Wp81 themes isn't a hole, even where the theme under Wp81 themes it too: its parts keep Wp81's Button.
    [AvaloniaFact]
    public void A_Button_In_A_Control_Wp81_Themes_Keeps_Wp81s()
    {
        var host = new AppControl();
        var template = new FuncControlTemplate<AppControl>((_, _) => new Button { Name = "Inner", Content = "inner" });
        var wp81 = new Wp81Theme();
        wp81.Resources[typeof(AppControl)] = new ControlTheme(typeof(AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } };
        var under = new SimpleTheme();
        under.Resources[typeof(AppControl)] = new ControlTheme(typeof(AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } };
        using var scene = new Scene(host, themes: [under, wp81]);

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(host).Single()));
    }

    /// <summary>A templated control of an app's own, which no theme knows.</summary>
    public class AppControl : TemplatedControl;
}
