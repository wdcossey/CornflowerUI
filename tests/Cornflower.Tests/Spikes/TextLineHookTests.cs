using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 7: can the phone's line (<c>PhoneTextLine</c>: WinRT's <c>BaselineToBaseline</c>, its <c>MaxHeight</c> line and
/// <c>CharacterSpacing</c>) be applied while a <c>TextBlock</c> is laid out, or worked out by the value store, rather than
/// written as <c>Padding</c>, <c>LineHeight</c> and <c>LetterSpacing</c> from property-changed handlers (#81)?
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does, on plain Avalonia (no theme): only a subclass can override what a <c>TextBlock</c>'s layout
/// reads; a presenter makes its text from the tree's data templates first, its own before an ancestor's; the layout events
/// come after a pass, though a parent's measure can set a child's values in the same pass; a coercion sees the priority
/// before the value comes in, and every priority reset in a reevaluation, while a later re-coercion sees the real one;
/// a coerced default stays in the store; and the only per-target bindings are Avalonia's own. docs/spikes, 7, has what
/// each means for the options. The coercion probes are on properties of their own, so <c>TextBlock</c>'s metadata in
/// the test process isn't changed.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class TextLineHookTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static MethodInfo Method(Type type, string name) => type.GetMethods(Instance).Single(m => m.Name == name);

    // ── A subclass (options 1 and 3) ─────────────────────────────────────────────────────────

    // Another assembly can override MeasureOverride, ArrangeOverride, CreateTextLayout and RenderTextLayout, but not
    // RenderCore (private protected) or Render (sealed); and TextLayout's height and drawing aren't virtual, so a layout
    // can't carry the first baseline's offset itself.
    [AvaloniaFact]
    public void Only_A_Subclass_Can_Override_What_A_TextBlocks_Layout_Reads()
    {
        foreach (var name in new[] { "CreateTextLayout", "MeasureOverride", "ArrangeOverride", "RenderTextLayout" })
        {
            var method = Method(typeof(TextBlock), name);
            Assert.True(method.IsFamily && method.IsVirtual && !method.IsFinal, name);
        }

        var renderCore = Method(typeof(TextBlock), "RenderCore");
        Assert.True(renderCore.IsFamilyAndAssembly && renderCore.IsVirtual);
        Assert.True(Method(typeof(TextBlock), "Render").IsFinal);

        Assert.False(typeof(TextLayout).GetProperty(nameof(TextLayout.Height))!.GetMethod!.IsVirtual);
        Assert.False(typeof(TextLayout).GetMethod(nameof(TextLayout.Draw))!.IsVirtual);
    }

    // A subclass that builds its own layout can't reach what TextBlock's own does with it: its text run cache is
    // private, and the test for inlines internal.
    [AvaloniaFact]
    public void A_Subclass_Building_Its_Own_Layout_Has_No_Way_To_The_Run_Cache_Or_The_Inlines_Test()
    {
        Assert.True(typeof(TextBlock).GetField("_textRunCache", Instance)!.IsPrivate);
        Assert.True(typeof(TextBlock).GetProperty("HasComplexContent", Instance)!.GetMethod!.IsAssembly);
        Assert.True(Method(typeof(TextBlock), "EnsureTextRuns").IsFamilyAndAssembly);
    }

    /// <summary>Applies a letter spacing of its own in its layout, as a subclass would apply the phone's.</summary>
    private sealed class SpacedTextBlock : TextBlock
    {
        public const double Spacing = 2;

        protected override Type StyleKeyOverride => typeof(TextBlock);

        protected override TextLayout CreateTextLayout(string? text)
        {
            var run = new GenericTextRunProperties(new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize,
                TextDecorations, Foreground, null, BaselineAlignment.Baseline, null, FontFeatures);
            var paragraph = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false, run, TextWrapping,
                LineHeight, 0, Spacing);
            return new TextLayout(new SimpleTextSource(text ?? "", run), paragraph, TextTrimming, double.PositiveInfinity,
                double.PositiveInfinity, MaxLines);
        }
    }

    // What a subclass puts in its layout is what it draws (RenderTextLayout draws TextLayout): ten characters are
    // 10 × 2 DIP wider, with no property set.
    [AvaloniaFact]
    public void A_Subclasss_Own_Spacing_Is_In_The_Layout_It_Draws()
    {
        var plain = new TextBlock { Text = "HHHHHHHHHH" };
        var spaced = new SpacedTextBlock { Text = "HHHHHHHHHH" };
        using var window = new SpikeWindow(new StackPanel { Children = { plain, spaced } });

        Assert.Equal(10 * SpacedTextBlock.Spacing,
            spaced.TextLayout.WidthIncludingTrailingWhitespace - plain.TextLayout.WidthIncludingTrailingWhitespace, 3);
    }

    /// <summary>The text a theme's own data template would make.</summary>
    private sealed class ThemedText : AccessText;

    private static Control TextOf(object content, bool accessKeys, Action<ContentPresenter, Border>? templates = null)
    {
        var presenter = new ContentPresenter { RecognizesAccessKey = accessKeys };
        var host = new Border { Child = presenter };
        templates?.Invoke(presenter, host);
        using var window = new SpikeWindow(host);
        presenter.Content = content;
        Spike.Settle();
        return presenter.Child!;
    }

    // A presenter makes its text from the tree's data templates first, its own before an ancestor's, and only with none
    // falls back to Avalonia's: an AccessText when it recognises access keys, a TextBlock when not. So a theme's own
    // template on its presenter would make the theme's own text, but would beat an app's template for the same data.
    [AvaloniaFact]
    public void A_Presenter_Makes_Its_Text_From_The_Trees_Data_Templates_First()
    {
        Assert.Equal(typeof(AccessText), TextOf("_Text", accessKeys: true).GetType());
        Assert.Equal(typeof(TextBlock), TextOf("Text", accessKeys: false).GetType());

        var themed = new FuncDataTemplate<string>((text, _) => new ThemedText { Text = text });
        var apps = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text, Tag = "app" });
        Assert.Equal(typeof(ThemedText), TextOf("_Text", true, (_, host) => host.DataTemplates.Add(themed)).GetType());
        Assert.Equal(typeof(ThemedText), TextOf("_Text", true, (presenter, host) =>
        {
            presenter.DataTemplates.Add(themed);
            host.DataTemplates.Add(apps);
        }).GetType());
    }

    // ── A layout hook (option 2) ─────────────────────────────────────────────────────────────

    // The layout events Layoutable declares are raised after a pass, so an attached behaviour has nothing to hook before
    // a TextBlock is measured, and a value set in LayoutUpdated lays it out again.
    [AvaloniaFact]
    public void The_Layout_Events_Come_After_The_Pass_And_A_Value_Set_There_Lays_Out_Again()
    {
        Assert.Equal(["EffectiveViewportChanged", "LayoutUpdated"],
            typeof(Layoutable).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(e => e.Name).Order());

        var block = new TextBlock { Text = "x" };
        using var window = new SpikeWindow(block);
        var passes = 0;
        var heights = new List<double>();
        block.LayoutUpdated += (_, _) =>
        {
            passes++;
            heights.Add(block.DesiredSize.Height);
            if (passes == 1)
                block.SetValue(TextBlock.PaddingProperty, new Thickness(0, 10, 0, 0), BindingPriority.Style);
        };

        block.FontSize = 20;
        Spike.Settle();

        Assert.Equal(2, passes);
        Assert.Equal(10, heights[1] - heights[0], 1);
    }

    /// <summary>A part around a template's text that sets its line before measuring it (option 2b).</summary>
    private sealed class LineDecorator : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Child is TextBlock text && text.Padding != new Thickness(0, 10, 0, 0))
                text.SetValue(TextBlock.PaddingProperty, new Thickness(0, 10, 0, 0), BindingPriority.Style);
            return base.MeasureOverride(availableSize);
        }
    }

    // A parent's own measure can set its child's values in the same pass: a part the theme's template places around its
    // text could apply the line in layout. It is still a value written to the child, and only text inside a template of
    // the theme's has such a parent.
    [AvaloniaFact]
    public void A_Parents_Measure_Can_Set_Its_Childs_Values_In_The_Same_Pass()
    {
        var plain = new TextBlock { Text = "x" };
        var lined = new TextBlock { Text = "x" };
        using var window = new SpikeWindow(new StackPanel { Children = { plain, new LineDecorator { Child = lined } } });
        var passes = 0;
        lined.LayoutUpdated += (_, _) => passes++;

        lined.FontSize = plain.FontSize = 20;
        Spike.Settle();

        Assert.Equal(1, passes);
        Assert.Equal(10, lined.DesiredSize.Height - plain.DesiredSize.Height, 1);
    }

    // ── Coercion (option 4) ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Properties with a coercion function that, while <see cref="Applies"/>, gives a value of its own where it finds none
    /// set, as <c>PhoneTextLine</c> would through <c>TextBlock</c>'s metadata.
    /// </summary>
    private sealed class Coerced : Border
    {
        public const double Own = 5;

        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<Coerced, double>("Spacing", coerce: CoerceSpacing);

        public static readonly StyledProperty<double> LineProperty =
            AvaloniaProperty.Register<Coerced, double>("Line", inherits: true, coerce: CoerceLine);

        public bool Applies { get; set; } = true;

        public List<(double Value, BindingPriority Priority)> Seen { get; } = [];

        private static double CoerceSpacing(AvaloniaObject target, double value)
        {
            var probe = (Coerced)target;
            var priority = probe.GetDiagnostic(SpacingProperty).Priority;
            probe.Seen.Add((value, priority));
            return probe.Applies && priority is BindingPriority.Unset or BindingPriority.Inherited ? Own : value;
        }

        private static double CoerceLine(AvaloniaObject target, double value)
        {
            var probe = (Coerced)target;
            return probe.Applies && probe.GetDiagnostic(LineProperty).Priority is BindingPriority.Unset or BindingPriority.Inherited ? Own : value;
        }
    }

    // While a value comes in, the store still reports the priority before it, so the first value a theme sets is seen as
    // none and loses. A coercion asked for again once the value is in sees its priority and keeps it.
    [AvaloniaFact]
    public void A_Coercion_Sees_The_Priority_Before_The_Value_And_A_Later_One_Sees_It_After()
    {
        var theme = new ControlTheme(typeof(Coerced));
        theme.Setters.Add(new Setter(Coerced.SpacingProperty, 1.0));
        var probe = new Coerced { Theme = theme };
        using var window = new SpikeWindow(probe);

        Assert.Equal((1.0, BindingPriority.Unset), probe.Seen[0]);
        Assert.Equal(Coerced.Own, probe.GetValue(Coerced.SpacingProperty));

        probe.CoerceValue(Coerced.SpacingProperty);

        Assert.Equal((1.0, BindingPriority.Style), probe.Seen[^1]);
        Assert.Equal(1, probe.GetValue(Coerced.SpacingProperty));
    }

    // When styles are applied again (here, an app's Style added to the window), the store resets every priority before it
    // works the values out, so each coercion sees none set. Where the coercion's answer is the value already there, no
    // change is raised: no handler hears that the app's value came, so nothing asks for the coercion again.
    [AvaloniaFact]
    public void In_A_Reevaluation_Every_Coercion_Is_Blind_And_An_Unchanged_Answer_Raises_Nothing()
    {
        var probe = new Coerced();
        using var window = new SpikeWindow(probe);
        probe.CoerceValue(Coerced.SpacingProperty);
        Assert.Equal(Coerced.Own, probe.GetValue(Coerced.SpacingProperty));
        var changes = 0;
        probe.PropertyChanged += (_, e) => changes += e.Property == Coerced.SpacingProperty ? 1 : 0;

        var apps = new Style(x => x.OfType<Coerced>());
        apps.Setters.Add(new Setter(Coerced.SpacingProperty, 1.0));
        window.Window.Styles.Add(apps);
        Spike.Settle();

        Assert.Equal((1.0, BindingPriority.Unset), probe.Seen[^1]);
        Assert.Equal(BindingPriority.Style, probe.GetDiagnostic(Coerced.SpacingProperty).Priority);
        Assert.Equal(Coerced.Own, probe.GetValue(Coerced.SpacingProperty));
        Assert.Equal(0, changes);

        probe.CoerceValue(Coerced.SpacingProperty);
        Assert.Equal(1, probe.GetValue(Coerced.SpacingProperty));
    }

    // A coerced default is a value in the store: when the coercion gives the default back it stays, IsSet, and ClearValue
    // doesn't take it out. On an inherited property it keeps the value it was made with: the parent's next value isn't
    // inherited.
    [AvaloniaFact]
    public void A_Coerced_Default_Stays_In_The_Store_And_Stops_Inheritance()
    {
        var child = new Coerced();
        var parent = new Coerced { Applies = false, Child = child };
        using var window = new SpikeWindow(parent);

        child.CoerceValue(Coerced.SpacingProperty);
        Assert.True(child.IsSet(Coerced.SpacingProperty));
        child.Applies = false;
        child.CoerceValue(Coerced.SpacingProperty);
        child.ClearValue(Coerced.SpacingProperty);
        Assert.Equal(0, child.GetValue(Coerced.SpacingProperty));
        Assert.True(child.IsSet(Coerced.SpacingProperty));

        parent.SetValue(Coerced.LineProperty, 7.0);
        child.Applies = true;
        child.CoerceValue(Coerced.LineProperty);
        Assert.Equal(Coerced.Own, child.GetValue(Coerced.LineProperty));
        child.Applies = false;
        child.CoerceValue(Coerced.LineProperty);
        Assert.Equal(7, child.GetValue(Coerced.LineProperty));

        parent.SetValue(Coerced.LineProperty, 9.0);
        Assert.Equal(7, child.GetValue(Coerced.LineProperty));
    }

    // ── A binding in each style (option 4) ───────────────────────────────────────────────────

    /// <summary>One value, pushed to every subscriber.</summary>
    private sealed class Source(double value) : IObservable<double>
    {
        private readonly List<IObserver<double>> _observers = [];

        public IDisposable Subscribe(IObserver<double> observer)
        {
            _observers.Add(observer);
            observer.OnNext(value);
            return new Spike.Undo(() => _observers.Remove(observer));
        }

        public void Push(double next)
        {
            value = next;
            foreach (var observer in _observers.ToList())
                observer.OnNext(next);
        }
    }

    // A binding of one's own can't be written: BindingBase's one member is internal. An observable made into a binding
    // is one source for every target, so it can't work a value out for each TextBlock.
    [AvaloniaFact]
    public void A_Binding_Of_Ones_Own_Cant_Be_Made_And_An_Observable_Gives_Every_Target_The_Same_Value()
    {
        Assert.True(Method(typeof(BindingBase), "CreateInstance").IsAssembly);

        var source = new Source(3);
        var theme = new ControlTheme(typeof(TextBlock));
        theme.Setters.Add(new Setter(TextBlock.LetterSpacingProperty, source.ToBinding()));
        var small = new TextBlock { Text = "x", FontSize = 10, Theme = theme };
        var large = new TextBlock { Text = "x", FontSize = 40, Theme = theme };
        using var window = new SpikeWindow(new StackPanel { Children = { small, large } });

        source.Push(4);
        Assert.Equal(4, small.LetterSpacing);
        Assert.Equal(4, large.LetterSpacing);
    }

    // A binding to the TextBlock itself, with a converter, is per target, but it is worked out once: a later FontSize
    // isn't followed. One with a path follows that one property only, so anything else it needs is the converter's
    // parameter, fixed in the style, not a DynamicResource an extension pack can change.
    [AvaloniaFact]
    public void A_Binding_To_Itself_Is_Per_Target_But_Doesnt_Follow_Its_Font()
    {
        var fromSelf = new ControlTheme(typeof(TextBlock));
        fromSelf.Setters.Add(new Setter(TextBlock.LetterSpacingProperty, new ReflectionBinding
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            Converter = new FuncValueConverter<TextBlock, double>(b => -3 * b!.FontSize / 1000),
        }));
        var fromSize = new ControlTheme(typeof(TextBlock));
        fromSize.Setters.Add(new Setter(TextBlock.LetterSpacingProperty, new ReflectionBinding(nameof(TextBlock.FontSize))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            Converter = new FuncValueConverter<double, double, double>((size, spacing) => spacing * size / 1000),
            ConverterParameter = -3.0,
        }));
        var small = new TextBlock { Text = "x", FontSize = 10, Theme = fromSelf };
        var large = new TextBlock { Text = "x", FontSize = 40, Theme = fromSelf };
        var sized = new TextBlock { Text = "x", FontSize = 10, Theme = fromSize };
        using var window = new SpikeWindow(new StackPanel { Children = { small, large, sized } });
        Assert.Equal(-0.03, small.LetterSpacing, 9);
        Assert.Equal(-0.12, large.LetterSpacing, 9);

        small.FontSize = sized.FontSize = 20;
        Spike.Settle();

        Assert.Equal(-0.03, small.LetterSpacing, 9);
        Assert.Equal(-0.06, sized.LetterSpacing, 9);
    }
}
