using WinRtApi;

namespace Cornflower.Tests.Tools;

/// <summary>
/// The checklist generator (<c>tools/winrt-api checklist</c>), on fixtures of our own: the types in
/// <c>ChecklistFixture.cs</c>, read from this assembly's metadata as the SDK's <c>Windows.winmd</c>
/// would be, and a <c>generic.xaml</c> and <c>themeresources.xaml</c> written here.
/// </summary>
public class ChecklistTests
{
    private const string Controls = "Windows.UI.Xaml.Controls";

    private const string Generic = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:Windows.UI.Xaml.Controls">
          <Style x:Key="KeyedButton" TargetType="Button">
            <Setter Property="Padding" Value="1" />
          </Style>
          <Style TargetType="local:Button">
            <Setter Property="Background" Value="Transparent" />
            <Setter Property="Foreground" Value="{ThemeResource ForegroundBrush}" />
            <Setter Property="Padding" Value="{StaticResource ButtonPadding}" />
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Button">
                  <Grid x:Name="Grid">
                    <VisualStateManager.VisualStateGroups>
                      <VisualStateGroup x:Name="CommonStates">
                        <VisualState x:Name="Normal" />
                        <VisualState x:Name="Pressed">
                          <Storyboard>
                            <PointerDownThemeAnimation Storyboard.TargetName="Grid" />
                            <ObjectAnimationUsingKeyFrames Storyboard.TargetName="Border" Storyboard.TargetProperty="Background">
                              <DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource PressedBrush}" />
                            </ObjectAnimationUsingKeyFrames>
                            <DoubleAnimation x:Name="Fade" Storyboard.TargetName="Grid" Storyboard.TargetProperty="Opacity" To="0.5" Duration="0:0:0.1" />
                          </Storyboard>
                        </VisualState>
                        <VisualState x:Name="CheckedPointerOver">
                          <VisualState.Setters>
                            <Setter Target="Border.Opacity" Value="0.8" />
                          </VisualState.Setters>
                        </VisualState>
                        <VisualState x:Name="MouseOver" />
                        <VisualStateGroup.Transitions>
                          <VisualTransition From="Pressed" To="Normal" GeneratedDuration="0:0:0.2">
                            <Storyboard>
                              <PointerUpThemeAnimation Storyboard.TargetName="Grid" />
                            </Storyboard>
                          </VisualTransition>
                        </VisualStateGroup.Transitions>
                      </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                    <Border x:Name="Border" Background="{TemplateBinding Background}" Margin="{ThemeResource ForegroundBrush}">
                      <ContentPresenter x:Name="ContentPresenter" />
                    </Border>
                  </Grid>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>
        </ResourceDictionary>
        """;

    private const string ThemeResourcesXaml = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Default">
              <Color x:Key="ForegroundColor">#FFFFFFFF</Color>
              <SolidColorBrush x:Key="ForegroundBrush" Color="{ThemeResource ForegroundColor}" />
              <SolidColorBrush x:Key="PressedBrush" Color="#FF3E65FF" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="Light">
              <Color x:Key="ForegroundColor">#DE000000</Color>
              <SolidColorBrush x:Key="ForegroundBrush" Color="{ThemeResource ForegroundColor}" />
              <SolidColorBrush x:Key="PressedBrush" Color="#FF3E65FF" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="HighContrastBlack">
              <SolidColorBrush x:Key="ForegroundBrush" Color="Yellow" />
            </ResourceDictionary>
          </ResourceDictionary.ThemeDictionaries>
          <Thickness x:Key="ButtonPadding">9.5,0,9.5,3.5</Thickness>
        </ResourceDictionary>
        """;

    private static string Metadata => typeof(ChecklistTests).Assembly.Location;

    // ── The API, from the metadata ─────────────────────────────────────────────

    [Fact]
    public void The_Api_Runs_Down_The_Base_Types_To_Control()
    {
        using var api = new ApiReader(Metadata);

        var chain = api.Chain($"{Controls}.Button", $"{Controls}.Control");

        Assert.Equal(["Button", "ButtonBase", "ContentControl"], chain.Select(t => t.Name));
    }

    [Fact]
    public void A_Static_Property_Property_Makes_A_Dependency_Property()
    {
        using var api = new ApiReader(Metadata);

        var buttonBase = api.Chain($"{Controls}.Primitives.ButtonBase", $"{Controls}.ContentControl").Single();

        Assert.Equal(
            [("Command", true, true), ("IsPressed", false, true), ("IsDefaultish", true, false)],
            buttonBase.Properties.Select(p => (p.Name, p.Settable, p.Dependency)));
    }

    [Fact]
    public void Events_And_Visible_Methods_Are_Read_With_Their_Types()
    {
        using var api = new ApiReader(Metadata);

        var buttonBase = api.Chain($"{Controls}.Primitives.ButtonBase", $"{Controls}.ContentControl").Single();

        Assert.Equal([new ApiEvent("Click", "RoutedEventHandler")], buttonBase.Events);
        Assert.Equal(
            [new ApiMethod("OnToggle", "void OnToggle()", true), new ApiMethod("Focus", "void Focus(Int32 state)", false)],
            buttonBase.Methods);
    }

    [Fact]
    public void A_Nullable_Is_Written_With_A_Question_Mark()
    {
        using var api = new ApiReader(Metadata);

        var toggle = api.Chain($"{Controls}.Primitives.ToggleButton", $"{Controls}.Primitives.ButtonBase").Single();

        Assert.Equal("Boolean?", toggle.Properties.Single(p => p.Name == "IsChecked").Type);
    }

    [Fact]
    public void A_Short_Name_Is_Found_Under_Controls_Then_Primitives()
    {
        using var api = new ApiReader(Metadata);

        Assert.Equal($"{Controls}.Button", ChecklistCommand.Resolve(api, "Button"));
        Assert.Equal($"{Controls}.Primitives.ToggleButton", ChecklistCommand.Resolve(api, "ToggleButton"));
        Assert.Throws<ArgumentException>(() => ChecklistCommand.Resolve(api, "Pivot"));
    }

    // ── The default style, from generic.xaml ───────────────────────────────────

    [Fact]
    public void The_Default_Style_Is_The_Unkeyed_One_Whatever_Its_Prefix()
    {
        var style = TemplateReader.Read(Generic, "Button")!;

        Assert.Equal(
            [new StyleSetter("Background", "Transparent"), new StyleSetter("Foreground", "{ThemeResource ForegroundBrush}"), new StyleSetter("Padding", "{StaticResource ButtonPadding}")],
            style.Setters);
        Assert.Null(TemplateReader.Read(Generic, "ToggleButton"));
    }

    // Not the named animation in a state: 8.1 has none, later eras may.
    [Fact]
    public void Parts_Are_The_Named_Elements_Outside_The_States()
    {
        var style = TemplateReader.Read(Generic, "Button")!;

        Assert.Equal(
            [new TemplatePart("Grid", "Grid"), new TemplatePart("Border", "Border"), new TemplatePart("ContentPresenter", "ContentPresenter")],
            style.Parts);
    }

    [Fact]
    public void Each_State_Says_What_It_Changes()
    {
        var style = TemplateReader.Read(Generic, "Button")!;

        Assert.Equal(["Normal", "Pressed", "CheckedPointerOver", "MouseOver"], style.States.Select(s => s.Name));
        Assert.Empty(style.States[0].Changes);
        Assert.Equal(
            ["PointerDownThemeAnimation on Grid", "Border.Background = {ThemeResource PressedBrush}", "Grid.Opacity: DoubleAnimation To=0.5 Duration=0:0:0.1"],
            style.States[1].Changes.Select(c => c.Description));
        Assert.Equal(["Border.Opacity = 0.8"], style.States[2].Changes.Select(c => c.Description));
    }

    [Fact]
    public void Transitions_Keep_Their_States_And_Duration()
    {
        var transition = Assert.Single(TemplateReader.Read(Generic, "Button")!.Transitions);

        Assert.Equal(("CommonStates", "Pressed", "Normal", "0:0:0.2"), (transition.Group, transition.From, transition.To, transition.Duration));
        Assert.Equal(["PointerUpThemeAnimation on Grid"], transition.Changes.Select(c => c.Description));
    }

    [Fact]
    public void Resources_Are_Listed_Once_In_The_Order_First_Used() =>
        Assert.Equal(["ForegroundBrush", "ButtonPadding", "PressedBrush"], TemplateReader.Read(Generic, "Button")!.Resources);

    // ── Theme resources ────────────────────────────────────────────────────────

    [Fact]
    public void A_Theme_Resource_Has_Its_Dark_And_Light_Values_Followed_To_The_End()
    {
        var resources = new ThemeResources(ThemeResourcesXaml);

        Assert.Equal(
            ("SolidColorBrush {ThemeResource ForegroundColor} → #FFFFFFFF", "SolidColorBrush {ThemeResource ForegroundColor} → #DE000000"),
            resources["ForegroundBrush"]);
        Assert.Equal(("SolidColorBrush #FF3E65FF", "SolidColorBrush #FF3E65FF"), resources["PressedBrush"]);
    }

    [Fact]
    public void A_Resource_Outside_The_Theme_Dictionaries_Has_One_Value_And_A_Missing_One_None()
    {
        var resources = new ThemeResources(ThemeResourcesXaml);

        Assert.Equal(("9.5,0,9.5,3.5", "9.5,0,9.5,3.5"), resources["ButtonPadding"]);
        Assert.Equal((null, null), resources["Missing"]);
    }

    // ── The Avalonia column ────────────────────────────────────────────────────

    [Fact]
    public void Avalonias_Control_Members_And_Parts_Are_Found_By_Name()
    {
        var button = new AvaloniaSide("Button");

        Assert.Equal(typeof(Avalonia.Controls.Button), button.Type);
        Assert.Equal("Button.IsPressed", button.Property("IsPressed"));
        Assert.Equal("InputElement.IsPointerOver", button.Property("IsPointerOver"));
        Assert.Null(button.Property("ContentTransitions"));
        Assert.Equal("Button.Click", button.Event("Click"));
        Assert.Equal("PART_ContentPresenter", button.Part("ContentPresenter"));
        Assert.Null(button.Part("Border"));
        Assert.Equal(typeof(Avalonia.Controls.Primitives.ToggleButton), new AvaloniaSide("ToggleButton").Type);
    }

    [Theory]
    [InlineData("Normal", "")]
    [InlineData("Pressed", ":pressed")]
    [InlineData("CheckedPointerOver", ":checked:pointerover")]
    [InlineData("UncheckedNormal", ":unchecked")]
    [InlineData("PointerFocused", ":focus")]
    [InlineData("On", ":checked")]
    [InlineData("MouseOver", null)]
    public void A_State_Maps_By_The_Draft_Rules(string state, string? selector) =>
        Assert.Equal(selector, new AvaloniaSide("Button").State(state));

    [Fact]
    public void A_State_Of_A_New_Control_Is_A_Pseudoclass_Named_After_It()
    {
        var pivot = new AvaloniaSide("Pivot");

        Assert.Null(pivot.Type);
        Assert.Equal(":portrait", pivot.State("Portrait"));
        Assert.Equal(":pressed", pivot.State("Pressed"));
    }

    [Fact]
    public void A_Pseudoclass_Avalonias_Control_Does_Not_Raise_Is_Named()
    {
        Assert.Equal([":checked"], new AvaloniaSide("Button").NotRaised(":checked:pointerover"));
        Assert.Empty(new AvaloniaSide("ToggleButton").NotRaised(":checked:pointerover"));
    }

    // ── The checklist ──────────────────────────────────────────────────────────

    [Fact]
    public void The_Checklist_Has_A_Row_For_Each_Thing_With_Avalonias_Beside_It()
    {
        var markdown = Render("Button");

        Assert.Contains("WinRT `Windows.UI.Xaml.Controls.Button`; Avalonia `Avalonia.Controls.Button`.", markdown);
        Assert.Contains("| [ ] | `Flyout` | `Object` | `Button.Flyout` |  |", markdown);
        Assert.Contains("## Properties, from `ButtonBase` (3)", markdown);
        Assert.Contains("| [ ] | `IsDefaultish` (not a dependency property) | `Boolean` | none |  |", markdown);
        Assert.Contains("| [ ] | `void OnToggle()` (protected, overridable) | none |  |", markdown);
        Assert.Contains("| [ ] | `Padding` | `{StaticResource ButtonPadding}` | `TemplatedControl.Padding` |  |", markdown);
        Assert.Contains("| [ ] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` |  |", markdown);
        Assert.Contains("| `CommonStates` | `Normal` | nothing | the theme's plain setters |  |", markdown);
        Assert.Contains("`^:checked:pointerover` (:checked not raised by Avalonia's `Button`)", markdown);
        Assert.Contains("| `CommonStates` | `MouseOver` | nothing | no rule |  |", markdown);
        Assert.Contains("| [ ] | `CommonStates` | `Pressed` → `Normal` | `0:0:0.2` | `PointerUpThemeAnimation on Grid` |  |", markdown);
        Assert.Contains("| [ ] | `ForegroundBrush` | `SolidColorBrush {ThemeResource ForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource ForegroundColor} → #DE000000` | yes |  |", markdown);
        Assert.Contains("| [ ] | `ButtonPadding` | `9.5,0,9.5,3.5` | same | no |  |", markdown);
    }

    [Fact]
    public void A_Control_Without_A_Default_Style_Says_So() =>
        Assert.Contains("_`generic.xaml` has no default style for `ToggleButton`._", Render("ToggleButton"));

    [Theory]
    [InlineData("a|b", "`a\\|b`")]
    [InlineData("two\n  lines", "`two lines`")]
    [InlineData("a `tick`", "`` a `tick` ``")]
    public void A_Cell_Is_One_Line_With_Its_Pipes_Escaped(string text, string cell) =>
        Assert.Equal(cell, Checklist.Code(text));

    [Fact]
    public void Token_Keys_Are_Every_Key_In_The_Tokens_Folder()
    {
        var folder = Directory.CreateTempSubdirectory("cornflower-tokens-");
        try
        {
            Directory.CreateDirectory(Path.Combine(folder.FullName, "Nested"));
            File.WriteAllText(Path.Combine(folder.FullName, "A.axaml"), Tokens("ForegroundBrush"));
            File.WriteAllText(Path.Combine(folder.FullName, "Nested", "B.axaml"), Tokens("PressedBrush"));

            Assert.Equal(["ForegroundBrush", "PressedBrush"], Checklist.TokenKeys(folder.FullName).Order());
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static string Tokens(string key) =>
        $"""<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"><SolidColorBrush x:Key="{key}" /></ResourceDictionary>""";

    /// <summary>The whole command, on files written to a temporary design folder.</summary>
    private static string Render(string type)
    {
        var design = Directory.CreateTempSubdirectory("cornflower-design-");
        var tokens = Directory.CreateTempSubdirectory("cornflower-tokens-");
        try
        {
            File.WriteAllText(Path.Combine(design.FullName, "generic.xaml"), Generic);
            File.WriteAllText(Path.Combine(design.FullName, "themeresources.xaml"), ThemeResourcesXaml);
            File.WriteAllText(Path.Combine(tokens.FullName, "Colors.axaml"), Tokens("ForegroundBrush"));
            return ChecklistCommand.Run(type, Metadata, design.FullName, stop: null, tokens: tokens.FullName);
        }
        finally
        {
            design.Delete(recursive: true);
            tokens.Delete(recursive: true);
        }
    }
}
