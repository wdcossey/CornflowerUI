using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Behaviours;

/// <summary>
/// <c>TemplateParts</c>, the template-part lookup <c>PhoneProgressBar</c>, <c>PhoneProgressRing</c> and
/// <c>PhoneToggleSwitch</c> share: the parts of the template already applied when a behaviour is turned on, each new
/// template's, the release before them and at the end, and only the control's own parts, never a nested templated
/// control's of the same name nor one in its content. Each behaviour's own tests turn it on after the template
/// (<c>Turned_On_After_The_Template_It_Still_Runs</c> for the bar and the ring,
/// <c>Turned_Off_It_Lets_Go_And_Turned_On_It_Takes_Hold</c> for the switch); these hold the lookup itself.
/// </summary>
public class TemplatePartsTests
{
    /// <summary>
    /// A ContentControl whose template has, in the visual tree's order, its content, a nested templated control with a
    /// <c>PART_A</c> and a <c>PART_Inner</c> of its own, then its own <c>PART_A</c>. The content is a control named
    /// <c>PART_A</c> too. Only the last is the owner's part.
    /// </summary>
    private sealed class Owner
    {
        public ContentControl Control { get; } =
            new() { Template = Template("own"), Content = new Border { Name = "PART_A", Tag = "content" } };

        public static FuncControlTemplate Template(string tag) => new((_, scope) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
            var nested = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, inner) => new StackPanel
                {
                    Children =
                    {
                        new Border { Name = "PART_A", Tag = "nested" }.RegisterInNameScope(inner),
                        new Border { Name = "PART_Inner", Tag = "nested" }.RegisterInNameScope(inner),
                    },
                }),
            };
            return new StackPanel
            {
                Children =
                {
                    presenter.RegisterInNameScope(scope),
                    nested,
                    new Border { Name = "PART_A", Tag = tag }.RegisterInNameScope(scope),
                },
            };
        });
    }

    /// <summary>What a behaviour saw, in order: each take's <c>PART_A</c> and <c>PART_Inner</c> (by tag), and each release.</summary>
    private sealed class Log
    {
        public List<string> Seen { get; } = [];

        public void Take(Func<string, Control?> find) =>
            Seen.Add($"take {find("PART_A")?.Tag ?? "none"} {find("PART_Inner")?.Tag ?? "none"} {find("PART_Missing")?.Tag ?? "none"}");

        public void Release() => Seen.Add("release");

        public TemplateParts Watch(TemplatedControl owner) => TemplateParts.Watch(owner, Take, Release);
    }

    private static Window Show(Control content)
    {
        var window = new Window { Width = 200, Height = 200, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // Turned on after the template is applied: the parts come from the template already there, at once, with no
    // release first. Its own PART_A, not the nested control's nor the content's, which come before it; the nested
    // control's PART_Inner isn't one of its parts at all.
    [AvaloniaFact]
    public void Turned_On_Late_It_Takes_The_Applied_Templates_Own_Parts()
    {
        var owner = new Owner();
        var window = Show(owner.Control);
        // In the visual tree's order, the content's PART_A comes first, then the nested control's, then the owner's.
        Assert.Equal(["content", "nested", "own"], owner.Control.GetVisualDescendants().OfType<Control>()
            .Where(c => c.Name == "PART_A").Select(c => c.Tag));
        var log = new Log();

        using var parts = log.Watch(owner.Control);

        Assert.Equal(["take own none none"], log.Seen);
        window.Close();
    }

    // The lookup of the applied template on its own: the same, and nothing at all before a template is applied.
    [AvaloniaFact]
    public void The_Applied_Lookup_Finds_Only_The_Owners_Parts()
    {
        var owner = new Owner();
        Assert.Null(TemplateParts.Applied(owner.Control)("PART_A"));

        var window = Show(owner.Control);
        var find = TemplateParts.Applied(owner.Control);
        Assert.Equal("own", find("PART_A")?.Tag);
        Assert.Same(owner.Control, find("PART_A")!.TemplatedParent);
        Assert.Null(find("PART_Inner"));
        Assert.Null(find("PART_Missing"));
        window.Close();
    }

    // Turned on before the template: nothing to take, then the template's own parts as it is applied, after a
    // release. A new template releases the old parts and takes its own.
    [AvaloniaFact]
    public void Each_New_Template_Is_Released_Then_Taken()
    {
        var owner = new Owner();
        var log = new Log();
        using var parts = log.Watch(owner.Control);
        Assert.Equal(["take none none none"], log.Seen);

        var window = Show(owner.Control);
        Assert.Equal(["take none none none", "release", "take own none none"], log.Seen);

        owner.Control.Template = Owner.Template("new");
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(["take none none none", "release", "take own none none", "release", "take new none none"], log.Seen);
        window.Close();
    }

    // Disposed (the behaviour turned off): one release, no more templates taken, and disposing again does nothing.
    [AvaloniaFact]
    public void Disposed_It_Releases_Once_And_Takes_No_More()
    {
        var owner = new Owner();
        var window = Show(owner.Control);
        var log = new Log();
        var parts = log.Watch(owner.Control);

        parts.Dispose();
        parts.Dispose();
        owner.Control.Template = Owner.Template("new");
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(["take own none none", "release"], log.Seen);
        window.Close();
    }

    // Disposed by an earlier handler of the same TemplateApplied (the behaviour turned off as a template comes): the
    // event still calls the later handler, which takes nothing and releases nothing more.
    [AvaloniaFact]
    public void Disposed_During_TemplateApplied_It_Takes_Nothing()
    {
        var owner = new Owner();
        var window = Show(owner.Control);
        var log = new Log();
        TemplateParts? parts = null;
        owner.Control.TemplateApplied += (_, _) => parts?.Dispose();
        parts = log.Watch(owner.Control);

        owner.Control.Template = Owner.Template("new");
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(["take own none none", "release"], log.Seen);
        window.Close();
    }

    // Disposed, the control lets go of it: a behaviour turned off and on again many times leaves no handler behind on
    // the control, each holding the old runner or follower alive and called at every new template.
    [AvaloniaFact]
    public void Disposed_The_Control_Holds_It_No_More()
    {
        var owner = new Owner();
        var window = Show(owner.Control);

        var released = WatchAndDispose(owner.Control);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(released.IsAlive);
        GC.KeepAlive(owner);
        window.Close();
    }

    /// <summary>Watches and disposes, out of line so that nothing of it stays on the test's stack.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference WatchAndDispose(TemplatedControl owner)
    {
        var log = new Log();
        log.Watch(owner).Dispose();
        return new WeakReference(log);
    }
}
