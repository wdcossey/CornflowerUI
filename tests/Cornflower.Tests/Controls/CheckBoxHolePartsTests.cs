using Avalonia.Controls;
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
/// CheckBox (probed for #31: every control type Simple themes in Avalonia.Controls and Avalonia.Dialogs, built with Simple
/// alone). Without the fallback it would take the phone's, at least 168 × 57.5 with its overhang, in the file dialog.
/// </summary>
public class CheckBoxHolePartsTests
{
    private static List<CheckBox> Plain(Control control) =>
        control.GetVisualDescendants().OfType<CheckBox>().Where(c => c.GetType() == typeof(CheckBox)).ToList();

    private static ControlTheme ThemeOf(IResourceNode theme, Type type) =>
        theme.TryGetResource(type, ThemeVariant.Dark, out var found) ? (ControlTheme)found! : throw new InvalidOperationException($"No {type.Name} theme.");

    // ManagedFileChooser shown with Simple then Wp81: its CheckBox takes Simple's CheckBox theme, not Simple's Button
    // theme and not Wp81's: as wide as with Simple alone, and far from the phone's 168 × 57.5. (Its height is its text's
    // line, which the text under Wp81 changes, as HoleTogglePartsTests says of the holes' rows.)
    [AvaloniaFact]
    public void The_File_Dialogs_CheckBox_Keeps_Simples_CheckBox_Theme()
    {
        var alone = new ManagedFileChooser();
        Avalonia.Size expected;
        using (new Scene(alone, themes: [new SimpleTheme()]))
            expected = Plain(alone).Single().Bounds.Size;

        var simple = new SimpleTheme();
        var chooser = new ManagedFileChooser();
        using var scene = new Scene(chooser, themes: [simple, new Wp81Theme()]);
        var checkBox = Plain(chooser).Single();

        Assert.Same(ThemeOf(simple, typeof(CheckBox)), checkBox.Theme);
        Assert.NotSame(ThemeOf(simple, typeof(Button)), checkBox.Theme);
        Assert.Equal(expected.Width, checkBox.Bounds.Width, 0.001);
        Assert.True(checkBox.Bounds.Height < 40, $"the CheckBox is {checkBox.Bounds.Height} tall: the phone's 57.5");
        Assert.NotEqual(168, checkBox.MinWidth);
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
        Assert.Equal(168, inner.MinWidth);
    }
}
