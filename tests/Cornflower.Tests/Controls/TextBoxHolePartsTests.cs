using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Dialogs;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using Cornflower.Tests.Completeness;

namespace Cornflower.Tests.Controls;

/// <summary>
/// A plain TextBox in the template of a control Wp81 doesn't theme yet (a hole) keeps the theme under Wp81 for TextBox,
/// through <c>HoleParts</c>, as <c>CheckBoxHolePartsTests</c> shows for the file dialog's CheckBox. In Avalonia 12.1.4,
/// Simple's templates for AutoCompleteBox, CalendarDatePicker, ComboBox (its editable box), the managed file dialog and
/// NumericUpDown hold one or more, and pass them Simple's brushes, or none: with the phone's theme they would draw the
/// phone's black text and white box in chrome laid out for Simple's (CalendarDatePicker's and NumericUpDown's text
/// black on Simple's dark background, ComboBox's editable box white when focused).
/// </summary>
public class TextBoxHolePartsTests
{
    private static List<TextBox> Plain(Control control) =>
        control.GetVisualDescendants().OfType<TextBox>().Where(c => c.GetType() == typeof(TextBox)).ToList();

    private static ControlTheme ThemeOf(IResourceNode theme, Type type) =>
        theme.TryGetResource(type, ThemeVariant.Dark, out var found) ? (ControlTheme)found! : throw new InvalidOperationException($"No {type.Name} theme.");

    /// <summary>Every hole that can be shown on its own in a window, as <c>CheckBoxHolePartsTests</c> shows them.</summary>
    private static IEnumerable<Type> ShowableHoles() =>
        Holes.TemplatedControls(new SimpleTheme())
            .Where(type => Holes.Listed().Contains(type.Name))
            .Where(type => !typeof(TopLevel).IsAssignableFrom(type) && type != typeof(OverlayPopupHost) && type.GetConstructor(Type.EmptyTypes) is not null);

    // Every hole shown with Simple then Wp81: as many plain TextBoxes as with Simple alone, each in Simple's TextBox
    // theme, none in Wp81's. So a template that gains one in an Avalonia upgrade is checked too.
    [AvaloniaFact]
    public void Every_Holes_TextBoxes_Keep_The_Theme_Under_Wp81()
    {
        var problems = new List<string>();
        var found = new SortedSet<string>(StringComparer.Ordinal);

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
                problems.Add($"{type.Name}: {parts.Count} plain TextBoxes, {expected} with Simple alone");
            foreach (var part in parts)
            {
                if (!ReferenceEquals(part.Theme, ThemeOf(simple, typeof(TextBox))))
                    problems.Add($"{type.Name}: TextBox {part.Name} has {(part.Theme is null ? "Wp81's theme" : $"the {part.Theme.TargetType?.Name} theme")}");
                found.Add(type.Name);
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        Assert.Equal(
            [nameof(AutoCompleteBox), nameof(CalendarDatePicker), nameof(ComboBox), nameof(ManagedFileChooser), nameof(NumericUpDown)],
            found);
    }

    // NumericUpDown's TextBox with Simple then Wp81: Simple's theme, so its text is Simple's foreground, not the phone's
    // black on Simple's dark spinner, and it has none of the phone's 9.5 margins or header row.
    [AvaloniaFact]
    public void NumericUpDowns_TextBox_Keeps_Simples_Look()
    {
        var simple = new SimpleTheme();
        var spinner = new NumericUpDown { Value = 3 };
        using var scene = new Scene(spinner, themes: [simple, new Wp81Theme()]);
        var box = Plain(spinner).Single();

        Assert.Same(ThemeOf(simple, typeof(TextBox)), box.Theme);
        Assert.NotEqual(TextBoxReadings.Black, ((ISolidColorBrush)box.Foreground!).Color);
        Assert.DoesNotContain(box.GetVisualDescendants(), v => v is Control { Name: "BorderElement" });

        // None of Wp81's values stays behind when its theme goes: no 9.5 margins, 34 minimum or phone's line.
        Assert.NotEqual(TextBoxReadings.Margin, box.Margin);
        Assert.NotEqual(TextBoxReadings.MinHeight, box.MinHeight);
        Assert.NotEqual(TextBoxReadings.MinWidth, box.MinWidth);
        Assert.True(double.IsNaN(box.LineHeight), $"the line {box.LineHeight} stayed");
    }

    // An app's own templated control isn't a hole: a TextBox in its template is the phone's.
    [AvaloniaFact]
    public void In_An_Apps_Own_Template_It_Is_Wp81s()
    {
        var mine = new HolePartsTests.AppControl
        {
            Template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new TextBox { Name = "Inner" }),
        };
        using var scene = new Scene(mine);

        var inner = Plain(mine).Single();
        Assert.Null(inner.Theme);
        Assert.Contains(inner.GetVisualDescendants(), v => v is Control { Name: "BorderElement" });
    }

    // The parent's template decides, not its type: an app's theme for a hole, above Wp81, is the app's template, so a
    // TextBox in it is the phone's.
    [AvaloniaFact]
    public void An_Apps_Theme_Above_Wp81_For_A_Hole_Keeps_Wp81s_TextBox()
    {
        var spinner = new NumericUpDown();
        var template = new FuncControlTemplate<NumericUpDown>((_, _) => new TextBox { Name = "Inner" });
        var app = new Styles
        {
            Resources =
            {
                [typeof(NumericUpDown)] = new ControlTheme(typeof(NumericUpDown)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } },
            },
        };
        using var scene = new Scene(spinner, themes: [new SimpleTheme(), new Wp81Theme(), app]);

        var inner = Plain(spinner).Single();
        Assert.Null(inner.Theme);
        Assert.Contains(inner.GetVisualDescendants(), v => v is Control { Name: "BorderElement" });
    }
}
