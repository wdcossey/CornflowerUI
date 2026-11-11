using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 7: can the phone's line (<c>PhoneTextLine</c>: WinRT's <c>BaselineToBaseline</c>, its <c>MaxHeight</c> line and
/// <c>CharacterSpacing</c>) be applied while a <c>TextBlock</c> is laid out, or worked out by the value store, rather than
/// written as <c>Padding</c>, <c>LineHeight</c> and <c>LetterSpacing</c> from property-changed handlers (#81)?
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: not from outside the <c>TextBlock</c>. Its own measure, arrange and render read the
/// three properties, and only a subclass can change what they read; the <c>AccessText</c> a <c>ContentPresenter</c>
/// makes of data is always Avalonia's own type; the only layout events are raised after the pass; a coercion function
/// can't tell a value set to the default from no value; and a binding of one's own can't be written, while one
/// observable made into a binding gives every target the same value. docs/spikes, 7, has what each means for the options.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class TextLineHookTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static MethodInfo Method(Type type, string name) => type.GetMethods(Instance).Single(m => m.Name == name);

    // ── A subclass (options 1 and 3) ─────────────────────────────────────────────────────────

    // TextBlock reads Padding in MeasureOverride, ArrangeOverride and RenderCore, and LineHeight and LetterSpacing in
    // CreateTextLayout. Another assembly can override the first two, CreateTextLayout and RenderTextLayout, but not
    // RenderCore (private protected) or Render (sealed); and TextLayout's height and drawing aren't virtual, so a
    // layout can't carry the first baseline's offset itself.
    [AvaloniaFact]
    public void Only_A_Subclass_Reaches_What_A_TextBlocks_Layout_Reads()
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
        var cache = typeof(TextBlock).GetField("_textRunCache", Instance)!;
        Assert.True(cache.IsPrivate);
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

    // What a subclass applies in its layout is drawn, but the properties don't show it, and they are what the type
    // ramp's tests and an app read: LetterSpacing is still 0.
    [AvaloniaFact]
    public void A_Subclasss_Own_Spacing_Is_Drawn_But_LetterSpacing_Doesnt_Show_It()
    {
        var plain = new TextBlock { Text = "HHHHHHHHHH" };
        var spaced = new SpacedTextBlock { Text = "HHHHHHHHHH" };
        using var scene = new Scene(new StackPanel { Children = { plain, spaced } });

        Assert.Equal(10 * SpacedTextBlock.Spacing, spaced.DesiredSize.Width - plain.DesiredSize.Width, 3);
        Assert.Equal(0, spaced.LetterSpacing);
    }

    // The text a presenter makes of data is Avalonia's own AccessText: CreateChild is private, so neither a TextBlock
    // subclass nor a presenter subclass chooses it. PhoneContentLine's text can't be a subclass.
    [AvaloniaFact]
    public void A_Presenter_Makes_Avalonias_Own_AccessText()
    {
        var presenter = new ContentPresenter { RecognizesAccessKey = true, Content = "_Text" };
        using var scene = new Scene(presenter);

        Assert.Equal(typeof(AccessText), presenter.Child!.GetType());
        var create = typeof(ContentPresenter).GetMethods(Instance).Single(m => m.Name == "CreateChild");
        Assert.True(create.IsPrivate);
    }

    // ── A layout hook (option 2) ─────────────────────────────────────────────────────────────

    // The layout events Layoutable declares are raised after a pass, so there is nothing to hook before a TextBlock is
    // measured, and a value set in LayoutUpdated lays it out again.
    [AvaloniaFact]
    public void The_Only_Layout_Events_Come_After_The_Pass_And_A_Value_Set_There_Lays_Out_Again()
    {
        Assert.Equal(["EffectiveViewportChanged", "LayoutUpdated"],
            typeof(Layoutable).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(e => e.Name).Order());

        var block = new TextBlock { Text = "x" };
        using var scene = new Scene(block);
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
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, passes);
        Assert.Equal(10, heights[1] - heights[0], 1);
    }

    // ── Coercion (option 4) ──────────────────────────────────────────────────────────────────

    /// <summary>A property with a coercion function that gives a value of its own where it finds none set.</summary>
    private sealed class Coerced : Border
    {
        public const double Own = 5;

        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<Coerced, double>("Spacing", coerce: Coerce);

        public List<(double Value, BindingPriority Priority)> Seen { get; } = [];

        private static double Coerce(AvaloniaObject target, double value)
        {
            var probe = (Coerced)target;
            var priority = probe.GetDiagnostic(SpacingProperty).Priority;
            probe.Seen.Add((value, priority));
            return priority is BindingPriority.Unset or BindingPriority.Inherited ? Own : value;
        }
    }

    // While a value comes in, the store still reports the priority before it: the first value a theme sets is seen as
    // none. And by its value alone, a value set to the default (here 0, as a ControlTheme based on a text style sets
    // LetterSpacing to 0 to take the phone's spacing off) can't be told from none. Either way the theme's value is lost.
    [AvaloniaFact]
    public void A_Coercion_Cant_Tell_A_Value_Set_From_None()
    {
        var theme = new ControlTheme(typeof(Coerced));
        theme.Setters.Add(new Setter(Coerced.SpacingProperty, 0.0));
        var probe = new Coerced { Theme = theme };
        using var scene = new Scene(probe);

        Assert.Contains((0.0, BindingPriority.Unset), probe.Seen);
        Assert.Equal(BindingPriority.Style, probe.GetDiagnostic(Coerced.SpacingProperty).Priority);
        Assert.Equal(Coerced.Own, probe.GetValue(Coerced.SpacingProperty));
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
            return new Unsubscriber(() => _observers.Remove(observer));
        }

        public void Push(double next)
        {
            value = next;
            foreach (var observer in _observers.ToList())
                observer.OnNext(next);
        }

        private sealed class Unsubscriber(Action undo) : IDisposable
        {
            public void Dispose() => undo();
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
        using var scene = new Scene(new StackPanel { Children = { small, large } });

        source.Push(4);
        Assert.Equal(4, small.LetterSpacing);
        Assert.Equal(4, large.LetterSpacing);
    }
}
