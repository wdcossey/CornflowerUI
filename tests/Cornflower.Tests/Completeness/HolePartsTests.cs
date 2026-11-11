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

    // A Calendar's arrows as wide as with Simple alone, and its header, arrows and all narrower than the phone's
    // minimum and in Simple's Button theme. (The header's width is its text's, in whichever font each scene has, so
    // it isn't compared across scenes.)
    [AvaloniaFact]
    public void A_Calendars_Header_And_Arrows_Keep_Simples_Button()
    {
        var alone = new Calendar();
        using var only = new Scene(alone, themes: [new SimpleTheme()]);
        var arrows = PlainButtons(alone).Where(b => b.Name != "PART_HeaderButton").Select(b => (b.Name, b.Bounds.Size)).ToList();

        var simple = new SimpleTheme();
        var calendar = new Calendar();
        using var scene = new Scene(calendar, themes: [simple, new Wp81Theme()]);
        var buttons = PlainButtons(calendar);

        Assert.Equal(["PART_PreviousButton", "PART_HeaderButton", "PART_NextButton"], buttons.Select(b => b.Name));
        Assert.Equal(arrows, buttons.Where(b => b.Name != "PART_HeaderButton").Select(b => (b.Name, b.Bounds.Size)).ToList());
        Assert.All(buttons, b =>
        {
            Assert.Same(ButtonThemeOf(simple), b.Theme);
            Assert.True(b.Bounds.Width < 109, $"{b.Name} is {b.Bounds.Width} wide: the phone's minimum");
        });
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

    /// <summary>A template with one plain Button in it, named Inner.</summary>
    private static FuncControlTemplate InnerButton() => new((_, _) => new Border { Child = new Button { Name = "Inner", Content = "inner" } });

    /// <summary>A <c>ControlTheme</c> for <paramref name="type"/> that sets <paramref name="template"/>.</summary>
    private static ControlTheme ThemeWith(Type type, IControlTemplate template) =>
        new(type) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } };

    // With nothing under Wp81, a Button keeps Wp81's: there is no other. Here AppControl's theme is above Wp81.
    [AvaloniaFact]
    public void With_Nothing_Under_Wp81_A_Button_Keeps_Wp81s()
    {
        var host = new AppControl();
        var above = new Styles { Resources = { [typeof(AppControl)] = ThemeWith(typeof(AppControl), InnerButton()) } };
        using var scene = new Scene(host, themes: [new Wp81Theme(), above]);

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(host).Single()));
    }

    [AvaloniaFact]
    public void A_Button_In_A_Template_From_Under_Wp81_Takes_The_Button_Theme_Under_Wp81()
    {
        var host = new AppControl();
        var underButton = ThemeWith(typeof(Button), new FuncControlTemplate<Button>((_, _) => new Border { Name = "Under" }));
        var under = new Styles { Resources = { [typeof(AppControl)] = ThemeWith(typeof(AppControl), InnerButton()), [typeof(Button)] = underButton } };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);
        Dispatcher.UIThread.RunJobs();

        var inner = PlainButtons(host).Single();
        Assert.Same(underButton, inner.Theme);
        Assert.Contains(inner.GetVisualDescendants(), v => v is Border { Name: "Under" });
    }

    // A control Wp81 themes, with a template Wp81 sets, keeps Wp81's Button, even where the theme under Wp81 sets the same.
    [AvaloniaFact]
    public void A_Button_In_A_Template_Wp81_Sets_Keeps_Wp81s()
    {
        var host = new AppControl();
        var template = InnerButton();
        var wp81 = new Wp81Theme();
        wp81.Resources[typeof(AppControl)] = ThemeWith(typeof(AppControl), template);
        var under = new SimpleTheme();
        under.Resources[typeof(AppControl)] = ThemeWith(typeof(AppControl), template);
        using var scene = new Scene(host, themes: [under, wp81]);

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(host).Single()));
    }

    // A theme under Wp81 keyed by Button but targeting another type (copied from another control's file) is no
    // Button theme: the Button keeps Wp81's rather than take it.
    [AvaloniaFact]
    public void A_Mis_Keyed_Theme_Under_Wp81_Isnt_Taken()
    {
        var host = new AppControl();
        var under = new Styles
        {
            Resources = { [typeof(AppControl)] = ThemeWith(typeof(AppControl), InnerButton()), [typeof(Button)] = new ControlTheme(typeof(ToggleButton)) },
        };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);

        var inner = PlainButtons(host).Single();
        Assert.Null(inner.Theme);
        Assert.True(CfTilt.GetIsEnabled(inner));
    }

    // An app's subclass of a control Simple themes, with a theme and template of its own: its Button is the phone's.
    [AvaloniaFact]
    public void An_Apps_Subclass_With_Its_Own_Theme_Keeps_Wp81s_Button()
    {
        var card = new Card { Theme = ThemeWith(typeof(Card), InnerButton()) };
        using var scene = new Scene(card);

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(card).Single()));
    }

    // An app's own theme for a control Wp81 doesn't theme, above Wp81: the template is the app's, so its Button is Wp81's.
    [AvaloniaFact]
    public void An_Apps_Theme_Above_Wp81_For_A_Hole_Keeps_Wp81s_Button()
    {
        var pager = new PipsPager();
        var window = new Styles { Resources = { [typeof(PipsPager)] = ThemeWith(typeof(PipsPager), InnerButton()) } };
        using var scene = new Scene(pager, themes: [new SimpleTheme(), new Wp81Theme(), window]);

        Assert.True(CfTilt.GetIsEnabled(PlainButtons(pager).Single()));
    }

    // The parent's theme changing at run time makes new parts, checked again: Simple's template falls back, the app's
    // doesn't, and Simple's again does.
    [AvaloniaFact]
    public void A_Change_Of_The_Parents_Theme_Checks_Its_Buttons_Again()
    {
        var simple = new SimpleTheme();
        var pager = new PipsPager();
        using var scene = new Scene(pager, themes: [simple, new Wp81Theme()]);
        Assert.All(PlainButtons(pager), b => Assert.Same(ButtonThemeOf(simple), b.Theme));

        pager.Theme = ThemeWith(typeof(PipsPager), InnerButton());
        Dispatcher.UIThread.RunJobs();
        Assert.True(CfTilt.GetIsEnabled(PlainButtons(pager).Single()));

        pager.Theme = null;
        Dispatcher.UIThread.RunJobs();
        var back = PlainButtons(pager);
        Assert.Equal(2, back.Count);
        Assert.All(back, b => Assert.Same(ButtonThemeOf(simple), b.Theme));
    }

    // Wp81 in a group with the theme under it, as an app may write it: the group is walked.
    [AvaloniaFact]
    public void Wp81_In_A_Styles_Group_Finds_The_Theme_Under_It()
    {
        var simple = new SimpleTheme();
        var pager = new PipsPager();
        using var scene = new Scene(pager, themes: [new Styles { simple, new Wp81Theme() }]);

        var buttons = PlainButtons(pager);
        Assert.Equal(2, buttons.Count);
        Assert.All(buttons, b => Assert.Same(ButtonThemeOf(simple), b.Theme));
    }

    /// <summary>An app's subclass of a control Simple themes.</summary>
    public class Card : HeaderedContentControl;

    /// <summary>A templated control of an app's own, which no theme knows.</summary>
    public class AppControl : TemplatedControl;
}
