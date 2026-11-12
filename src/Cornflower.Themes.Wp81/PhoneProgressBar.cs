using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Themes.Wp81.PhoneKeyframeTrack;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 ProgressBar's <c>Indeterminate</c> storyboard, run frame by frame on <see cref="CornflowerClock"/> while the
/// bar is indeterminate: the dots' grid, each dot's two slides and each dot's opacity, as the template's keyframes and
/// the phone's <c>TemplateSettings</c> give them. Internal to the theme, which turns it on with its own setter.
/// </summary>
/// <remarks>
/// <para>
/// The keyframes are the 8.1 Update 1 runtime's (<c>progressbar.json</c>, <c>CommonStates</c>, <c>Indeterminate</c>,
/// equal to the SDK's <c>generic.xaml</c>); docs/controls/ProgressBar/PLAN.md lists them. Five dots, <c>PART_E1</c> to
/// <c>PART_E5</c>, each in a border, <c>PART_B1</c> to <c>PART_B5</c>, all in <c>PART_EllipseGrid</c>; each of the
/// eleven has a <see cref="TranslateTransform"/> whose <c>X</c> the storyboard moves:
/// </para>
/// <list type="bullet">
/// <item>the grid, from <c>ContainerAnimationStartPosition</c> to <c>ContainerAnimationEndPosition</c> in a straight line
/// over 3.917 s;</item>
/// <item>each B, from −40 to 0, holding, then on to 80, in straight lines, each 0.167 s after the one before;</item>
/// <item>each E, from 0 to <c>EllipseAnimationWellPosition</c> and, a second later, on to
/// <c>EllipseAnimationEndPosition</c>, each over a second through the <c>KeySpline</c> 0.4,0,0.6,1, each 0.167 s after
/// the one before; and each E shown from then for 3 s.</item>
/// </list>
/// <para>
/// The template binds those four values to the bar's <c>TemplateSettings</c>, which the phone works out in its own code
/// from the bar's width. Avalonia's <c>ProgressBar.TemplateSettings</c> has other values, worked out another way, so
/// the storyboard works the phone's out here, as the reference app read them on all seven profiles (native.json): the
/// start −63, the end the width over 9 for each of the 3.917 s on from it, the well a third of the width and the end two
/// thirds (<see cref="TemplateSettings"/>). The tracks below hold the grid's way from its start to its end, and each
/// E's stops (0, the well, the end), so one set of tracks serves every width.
/// </para>
/// <para>
/// The storyboard is as long as its longest track, the grid's 3.917 s, and repeats forever. It runs from the moment the
/// bar is indeterminate, attached to a window and has its template, at the storyboard's start, as the phone's
/// <c>VisualStateManager.GoToState</c> starts it; it stops, and gives the parts back their own values, when any of
/// those ends. While the bar isn't visible it asks for no frames, and looks again four times a second. A vertical bar
/// (Avalonia's, not the phone's) doesn't run it: the template's dots run only across.
/// </para>
/// </remarks>
internal static class PhoneProgressBar
{
    /// <summary>Whether the bar's template is animated by this storyboard. Set by the theme.</summary>
    public static readonly AttachedProperty<bool> IsAnimatedProperty =
        AvaloniaProperty.RegisterAttached<ProgressBar, bool>("IsAnimated", typeof(PhoneProgressBar));

    /// <summary><c>TemplateSettings.ContainerAnimationStartPosition</c>: −63 at every width read.</summary>
    internal const double ContainerStart = -63;

    /// <summary>The grid's animation: its <c>Duration</c>, and the storyboard's length.</summary>
    internal static readonly TimeSpan ContainerDuration = Seconds(3.917);

    /// <summary>
    /// How far the grid travels each second, in widths of the bar: <c>ContainerAnimationEndPosition</c> less the start
    /// is 0.43522 of the width on all seven profiles, which is the width over 9 for each of the 3.917 s.
    /// </summary>
    internal const double ContainerWidthsPerSecond = 1.0 / 9;

    /// <summary>The phone's <c>TemplateSettings</c> for a bar this wide, as the template's storyboard binds them.</summary>
    internal static (double ContainerStart, double ContainerEnd, double EllipseWell, double EllipseEnd) TemplateSettings(double width) =>
        (ContainerStart, ContainerStart + width * ContainerWidthsPerSecond * ContainerDuration.TotalSeconds, width / 3, width * 2 / 3);

    /// <summary>The E tracks' spline, into the well and into the end.</summary>
    internal static readonly KeySplineCurve Ease = new(0.4, 0, 0.6, 1);

    /// <summary>When each E leaves 0, and shows: 0.167 s after the one before.</summary>
    internal static readonly TimeSpan[] DotBegins = [Seconds(0), Seconds(0.167), Seconds(0.333), Seconds(0.5), Seconds(0.667)];

    /// <summary>When each B reaches 0, leaves it, and reaches 80: its keys after the first, −40 at 0.</summary>
    internal static readonly TimeSpan[][] SlideTimes =
    [
        [Seconds(0.5), Seconds(2), Seconds(3)],
        [Seconds(0.667), Seconds(2.167), Seconds(3.167)],
        [Seconds(0.833), Seconds(2.333), Seconds(3.333)],
        [Seconds(1), Seconds(2.5), Seconds(3.5)],
        [Seconds(1.167), Seconds(2.667), Seconds(3.667)],
    ];

    /// <summary>
    /// The grid's way from <c>ContainerAnimationStartPosition</c> (0) to <c>ContainerAnimationEndPosition</c> (1): a
    /// straight line over 3.917 s, the template's <c>DoubleAnimation</c>.
    /// </summary>
    internal static readonly PhoneKeyframeTrack Container = new(Kind.Spline, TimeSpan.Zero,
        new Key(TimeSpan.Zero, 0), new Key(ContainerDuration, 1));

    /// <summary>
    /// B1 to B5: −40, 0, 0, 80, in straight lines. The template's keys are <c>EasingDoubleKeyFrame</c>s with no easing,
    /// which are straight lines.
    /// </summary>
    internal static readonly PhoneKeyframeTrack[] Slides = SlideTimes
        .Select(times => new PhoneKeyframeTrack(Kind.Spline, TimeSpan.Zero,
            new Key(TimeSpan.Zero, -40), new Key(times[0], 0), new Key(times[1], 0), new Key(times[2], 80)))
        .ToArray();

    /// <summary>
    /// E1 to E5, as stops, from each dot's begin: 0, 1 (<c>EllipseAnimationWellPosition</c>) a second later, held for a
    /// second, 2 (<c>EllipseAnimationEndPosition</c>) a second after that; <see cref="MoveAt"/> turns a stop into a
    /// position. Before its begin the template's track holds 0, as <see cref="MoveAt"/> does. The template eases into
    /// the well and the end through <see cref="Ease"/>; its other keys carry an <c>ExponentialEase</c>, but each joins
    /// two equal values, so it moves nothing.
    /// </summary>
    internal static readonly PhoneKeyframeTrack[] Dots = DotBegins
        .Select(begin => new PhoneKeyframeTrack(Kind.Spline, begin,
            new Key(TimeSpan.Zero, 0), new Key(Seconds(1), 1, Ease), new Key(Seconds(2), 1), new Key(Seconds(3), 2, Ease)))
        .ToArray();

    /// <summary>
    /// E1 to E5's opacity, from each dot's begin: 1, then 0 from 3 s after. Before its begin the template's track holds
    /// 0, as <see cref="OpacityAt"/> does. The template writes each step as two keys at one time, which is a step at that
    /// time; here each is a discrete key.
    /// </summary>
    internal static readonly PhoneKeyframeTrack[] Opacities = DotBegins
        .Select(begin => new PhoneKeyframeTrack(Kind.Discrete, begin, new Key(TimeSpan.Zero, 1), new Key(Seconds(3), 0)))
        .ToArray();

    /// <summary>The storyboard's length, its longest track's end: the grid's 3.917 s. It repeats from there.</summary>
    internal static readonly TimeSpan Duration = Slides.Concat(Dots).Concat(Opacities).Append(Container).Max(t => t.End);

    /// <summary>How often a hidden bar looks again whether it is shown: no frame is asked for while nothing is drawn.</summary>
    internal static readonly TimeSpan HiddenCheck = TimeSpan.FromMilliseconds(250);

    private static readonly ConditionalWeakTable<ProgressBar, Runner> Runners = new();

    static PhoneProgressBar()
    {
        IsAnimatedProperty.Changed.AddClassHandler<ProgressBar>((bar, e) =>
        {
            if (e.GetOldValue<bool>() == e.GetNewValue<bool>())
            {
                return;
            }

            if (Runners.TryGetValue(bar, out var old))
            {
                // Stopped now, so that a runner made in the same pass reads the parts' own values, not the old one's.
                Runners.Remove(bar);
                old.Detach();
            }

            if (e.GetNewValue<bool>())
            {
                var runner = new Runner(bar);
                Runners.Add(bar, runner);
                runner.Attach();
            }
        });
        // Whether the storyboard runs: indeterminate and horizontal.
        ProgressBar.IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>(Refresh);
        ProgressBar.OrientationProperty.Changed.AddClassHandler<ProgressBar>(Refresh);
    }

    private static void Refresh(ProgressBar bar, AvaloniaPropertyChangedEventArgs _)
    {
        if (Runners.TryGetValue(bar, out var runner))
        {
            runner.Update();
        }
    }

    /// <summary>Gets <see cref="IsAnimatedProperty"/>.</summary>
    public static bool GetIsAnimated(ProgressBar bar) => bar.GetValue(IsAnimatedProperty);

    /// <summary>Sets <see cref="IsAnimatedProperty"/>.</summary>
    public static void SetIsAnimated(ProgressBar bar, bool value) => bar.SetValue(IsAnimatedProperty, value);

    /// <summary>The storyboard's time at <paramref name="elapsed"/> since it started, repeating forever.</summary>
    internal static TimeSpan StoryboardTime(TimeSpan elapsed) => TimeSpan.FromTicks(elapsed.Ticks % Duration.Ticks);

    /// <summary>The grid's <c>X</c> at a storyboard time, for a bar this wide.</summary>
    internal static double ContainerAt(TimeSpan time, double width)
    {
        var settings = TemplateSettings(width);
        return settings.ContainerStart + (settings.ContainerEnd - settings.ContainerStart) * Container.ValueAt(time)!.Value;
    }

    /// <summary>A dot's opacity at a storyboard time: 0 before its begin, as the template's track holds.</summary>
    internal static double OpacityAt(int dot, TimeSpan time) => Opacities[dot].ValueAt(time) ?? 0;

    /// <summary>A dot's <c>X</c> at a storyboard time, for a bar this wide: its track's stop between 0, the well and the end.</summary>
    internal static double MoveAt(int dot, TimeSpan time, double width)
    {
        var settings = TemplateSettings(width);
        var stop = Dots[dot].ValueAt(time) ?? 0;
        return stop <= 1
            ? settings.EllipseWell * stop
            : settings.EllipseWell + (settings.EllipseEnd - settings.EllipseWell) * (stop - 1);
    }

    /// <summary>Runs one bar's storyboard.</summary>
    private sealed class Runner(ProgressBar bar)
    {
        private const int Count = 5;
        private readonly Rectangle?[] _dots = new Rectangle?[Count];
        private readonly TranslateTransform?[] _slides = new TranslateTransform?[Count];
        private readonly TranslateTransform?[] _moves = new TranslateTransform?[Count];
        private readonly double[] _ownSlides = new double[Count];
        private readonly double[] _ownMoves = new double[Count];
        private readonly IDisposable?[] _shown = new IDisposable?[Count];
        private readonly double?[] _opacity = new double?[Count];
        private TranslateTransform? _container;
        private double _ownContainer;
        private IDisposable? _pending;
        private TemplateParts? _parts;
        private TimeSpan _start;
        private bool _running;
        private bool _attached;

        /// <summary>
        /// Starts following the bar: its window, and its template's parts, the one already applied (a theme that turns
        /// this on after it gets no <c>TemplateApplied</c> for it) and each new one, stopping before a new one's.
        /// </summary>
        public void Attach()
        {
            bar.AttachedToVisualTree += OnAttached;
            bar.DetachedFromVisualTree += OnDetached;
            _attached = bar.IsAttachedToVisualTree();
            _parts = TemplateParts.Watch(bar, TakeParts, Stop);
        }

        /// <summary>Stops following the bar, and stops the storyboard.</summary>
        public void Detach()
        {
            bar.AttachedToVisualTree -= OnAttached;
            bar.DetachedFromVisualTree -= OnDetached;
            _parts?.Dispose();
        }

        private void TakeParts(Func<string, Control?> find)
        {
            FindParts(find);
            Update();
        }

        /// <summary>The grid's, the borders' and the dots' transforms, each one's own <c>X</c> to give back, and the dots.</summary>
        private void FindParts(Func<string, Control?> find)
        {
            _container = find("PART_EllipseGrid")?.RenderTransform as TranslateTransform;
            _ownContainer = _container?.X ?? 0;
            for (var i = 0; i < Count; i++)
            {
                _slides[i] = find($"PART_B{i + 1}")?.RenderTransform as TranslateTransform;
                _ownSlides[i] = _slides[i]?.X ?? 0;
                _dots[i] = find($"PART_E{i + 1}") as Rectangle;
                _moves[i] = _dots[i]?.RenderTransform as TranslateTransform;
                _ownMoves[i] = _moves[i]?.X ?? 0;
            }
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _attached = true;
            Update();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _attached = false;
            Update();
        }

        /// <summary>Starts or stops the storyboard as the bar's state says.</summary>
        public void Update()
        {
            var run = bar.IsIndeterminate && bar.Orientation == Orientation.Horizontal && _attached && (_container is not null || _dots.Any(d => d is not null));
            if (run && !_running)
            {
                _running = true;
                _start = CornflowerClock.Now;
                Step();
            }
            else if (!run && _running)
            {
                Stop();
            }
        }

        /// <summary>
        /// One frame: every part at the storyboard's time now, for the bar's width now, then the next frame. Hidden (the
        /// bar or a parent not visible), the storyboard's time runs on but nothing is set, and it looks again after
        /// <see cref="HiddenCheck"/>.
        /// </summary>
        private void Step()
        {
            _pending = null;
            if (!_running)
            {
                return;
            }

            if (!bar.IsEffectivelyVisible)
            {
                _pending = CornflowerClock.Schedule(Step, HiddenCheck);
                return;
            }

            var time = StoryboardTime(CornflowerClock.Now - _start);
            var width = bar.Bounds.Width;
            if (_container is { } container)
            {
                container.X = ContainerAt(time, width);
            }

            for (var i = 0; i < Count; i++)
            {
                if (_slides[i] is { } slide)
                {
                    slide.X = Slides[i].ValueAt(time)!.Value;
                }

                if (_moves[i] is { } move)
                {
                    move.X = MoveAt(i, time, width);
                }

                Show(i, OpacityAt(i, time));
            }

            _pending = CornflowerClock.RequestFrame(bar, Step);
        }

        /// <summary>Sets a dot's opacity at animation priority when it changes.</summary>
        private void Show(int i, double? opacity)
        {
            if (_opacity[i] == opacity)
            {
                return;
            }

            _opacity[i] = opacity;
            _shown[i]?.Dispose();
            _shown[i] = opacity is { } value && _dots[i] is { } dot
                ? dot.SetValue(Visual.OpacityProperty, value, BindingPriority.Animation)
                : null;
        }

        /// <summary>
        /// Stops the frames and gives every part its own <c>X</c> back at once, and each dot its own opacity after the
        /// dispatcher's pass. An <c>X</c> is a value set on a transform, safe in any pass. Taking the opacity's animation
        /// value away isn't: this can run inside a theme's unset pass (the theme leaving, or a value of its going),
        /// where taking another value away stops the rest (CLAUDE.md, <c>PhoneTextLine</c>).
        /// </summary>
        public void Stop()
        {
            _running = false;
            _pending?.Dispose();
            _pending = null;
            if (_container is { } container)
            {
                container.X = _ownContainer;
            }

            for (var i = 0; i < Count; i++)
            {
                if (_slides[i] is { } slide)
                {
                    slide.X = _ownSlides[i];
                }

                if (_moves[i] is { } move)
                {
                    move.X = _ownMoves[i];
                }
            }

            var shown = (IDisposable?[])_shown.Clone();
            Array.Clear(_shown);
            Array.Clear(_opacity);
            if (shown.Any(s => s is not null))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var value in shown)
                    {
                        value?.Dispose();
                    }
                });
            }
        }
    }
}
