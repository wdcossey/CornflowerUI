using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Dialogs;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using Cornflower.Tests.Completeness;

namespace Cornflower.Tests.Controls;

/// <summary>
/// A plain CheckBox in the template of a control Wp81 doesn't theme yet (a hole) keeps the theme under Wp81 for
/// CheckBox, through <c>HoleParts</c>, as <c>HoleTogglePartsTests</c> shows for ToggleButtons and RepeatButtons. In
/// Avalonia 12.1.4, of Simple's templates only <see cref="ManagedFileChooser"/>'s holds one: its "show hidden files"
/// CheckBox. Without the fallback it would take the phone's, at least 168 × 57.5 with its overhang, in the file dialog.
/// </summary>
public class CheckBoxHolePartsTests
{
    private static List<CheckBox> Plain(Control control) =>
        control.GetVisualDescendants().OfType<CheckBox>().Where(c => c.GetType() == typeof(CheckBox)).ToList();

    private static ControlTheme ThemeOf(IResourceNode theme, Type type) =>
        theme.TryGetResource(type, ThemeVariant.Dark, out var found) ? (ControlTheme)found! : throw new InvalidOperationException($"No {type.Name} theme.");

    /// <summary>Every hole that can be shown on its own in a window, as <c>HoleTogglePartsTests</c> shows them.</summary>
    private static IEnumerable<Type> ShowableHoles() =>
        Holes.TemplatedControls(new SimpleTheme())
            .Where(type => Holes.Listed().Contains(type.Name))
            .Where(type => !typeof(TopLevel).IsAssignableFrom(type) && type != typeof(OverlayPopupHost) && type.GetConstructor(Type.EmptyTypes) is not null);

    // Every hole shown with Simple then Wp81: as many plain CheckBoxes as with Simple alone, each in Simple's CheckBox
    // theme, none in Wp81's. So a template that gains one in an Avalonia upgrade is checked too. Today, only the file
    // dialog's.
    [AvaloniaFact]
    public void Every_Holes_CheckBoxes_Keep_The_Theme_Under_Wp81()
    {
        var problems = new List<string>();
        var found = new List<string>();

        foreach (var type in ShowableHoles())
        {
            var alone = (Control)Activator.CreateInstance(type)!;
            int expected;
            using (new Scene(alone, themes: [new SimpleTheme()]))
                expected = Plain(alone).Count;

            var simple = new SimpleTheme();
            var control = (Control)Activator.CreateInstance(type)!;
            using var scene = new Scene(control, themes: [simple, new Wp81Theme()]);
            var parts = Plain(control);

            if (parts.Count != expected)
                problems.Add($"{type.Name}: {parts.Count} plain CheckBoxes, {expected} with Simple alone");
            foreach (var part in parts)
            {
                if (!ReferenceEquals(part.Theme, ThemeOf(simple, typeof(CheckBox))))
                    problems.Add($"{type.Name}: CheckBox {part.Name} has {(part.Theme is null ? "Wp81's theme" : $"the {part.Theme.TargetType?.Name} theme")}");
                found.Add(type.Name);
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        Assert.Equal([nameof(ManagedFileChooser)], found);
    }

    // The file dialog's CheckBox with Simple then Wp81: Simple's CheckBox theme, so smaller than the phone's minimum
    // height and without the tilt. (Its size is its text's, which the text under Wp81 and each platform's fallback
    // font change, so it isn't compared with Simple alone's.)
    [AvaloniaFact]
    public void The_File_Dialogs_CheckBox_Keeps_Simples_CheckBox_Theme()
    {
        var simple = new SimpleTheme();
        var chooser = new ManagedFileChooser();
        using var scene = new Scene(chooser, themes: [simple, new Wp81Theme()]);
        var checkBox = Plain(chooser).Single();

        Assert.Same(ThemeOf(simple, typeof(CheckBox)), checkBox.Theme);
        Assert.True(checkBox.Bounds.Height < CheckBoxReadings.Height, $"the CheckBox is {checkBox.Bounds.Height} tall, the phone's at least {CheckBoxReadings.Height}");
        Assert.NotEqual(CheckBoxReadings.MinWidth, checkBox.MinWidth);
        Assert.False(Cornflower.Controls.Tilt.GetIsEnabled(checkBox));
    }

    // An app's own templated control isn't a hole: a CheckBox in its template is the phone's.
    [AvaloniaFact]
    public void In_An_Apps_Own_Template_It_Is_Wp81s()
    {
        var mine = new HolePartsTests.AppControl
        {
            Template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new CheckBox { Name = "Inner", Content = "c" }),
        };
        using var scene = new Scene(mine);

        var inner = Plain(mine).Single();
        Assert.Null(inner.Theme);
        Assert.Equal(CheckBoxReadings.MinWidth, inner.MinWidth);
    }

    // The parent's template decides, not its type: an app's theme for a hole, above Wp81, is the app's template, so a
    // CheckBox in it is the phone's. (PipsPager, as HolePartsTests uses: the file dialog's template must have its parts.)
    [AvaloniaFact]
    public void An_Apps_Theme_Above_Wp81_For_A_Hole_Keeps_Wp81s_CheckBox()
    {
        var pager = new PipsPager();
        var template = new FuncControlTemplate<PipsPager>((_, _) => new CheckBox { Name = "Inner", Content = "c" });
        var app = new Styles
        {
            Resources =
            {
                [typeof(PipsPager)] = new ControlTheme(typeof(PipsPager)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } },
            },
        };
        using var scene = new Scene(pager, themes: [new SimpleTheme(), new Wp81Theme(), app]);

        var inner = Plain(pager).Single();
        Assert.Null(inner.Theme);
        Assert.True(Cornflower.Controls.Tilt.GetIsEnabled(inner));
    }
}
