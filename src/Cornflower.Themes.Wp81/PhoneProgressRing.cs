using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;
using Cornflower.Controls;
using static Cornflower.Themes.Wp81.PhoneKeyframeTrack;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 ProgressRing's <c>Active</c> storyboard, run frame by frame on <see cref="CornflowerClock"/> while the
/// ring is active: each dot's rotation and opacity, as the template's keyframes give them. Internal to the theme,
/// which turns it on with its own setter.
/// </summary>
/// <remarks>
/// <para>
/// The keyframes are the 8.1 Update 1 runtime's (<c>progressring.json</c>, <c>ActiveStates</c>, <c>Active</c>, equal to
/// the SDK's <c>generic.xaml</c>), docs/controls/ProgressRing/PLAN.md lists them. Six dots, <c>PART_E1</c> to
/// <c>PART_E6</c>, each in a canvas, <c>PART_E1R</c> to <c>PART_E5R</c> and <c>PART_SixthCircle</c>, whose
/// <see cref="RotateTransform"/> turns it about the ring's centre. Each dot begins 0.167 s after the one before; its
/// angle runs through seven spline keys over 3.217 s, and it shows from its begin time until 3.22 s after. The
/// storyboard repeats forever, and is as long as its longest track: the last dot's 0.835 s and 3.47 s, 4.305 s.
/// </para>
/// <para>
/// It runs from the moment the ring is active, attached to a window and has its template, at the storyboard's start,
/// as the phone's <c>VisualStateManager.GoToState</c> starts it; it stops, and gives the dots back their own values,
/// when any of those ends. The emulator's own clock runs about 4% fast against real time (B6, run
/// <c>20261111-2316-dev11</c>), so the phone's ring is timed by its keyframes, not by the emulator's 4.13 s.
/// </para>
/// </remarks>
internal static class PhoneProgressRing
{
    /// <summary>Whether the ring's template is animated by this storyboard. Set by the theme.</summary>
    public static readonly AttachedProperty<bool> IsAnimatedProperty =
        AvaloniaProperty.RegisterAttached<ProgressRing, bool>("IsAnimated", typeof(PhoneProgressRing));

    /// <summary>The begin times of the six dots' tracks, from <c>progressring.json</c>.</summary>
    internal static readonly TimeSpan[] BeginTimes =
        [Seconds(0), Seconds(0.167), Seconds(0.334), Seconds(0.501), Seconds(0.668), Seconds(0.835)];

    /// <summary>Each dot's angle keys, in degrees: E1R to E6R. Their times and splines are <see cref="AngleTimes"/> and <see cref="AngleSplines"/>.</summary>
    internal static readonly double[][] AngleValues =
    [
        [-110, 10, 93, 205, 357, 439, 585],
        [-116, 4, 87, 199, 351, 433, 579],
        [-122, -2, 81, 193, 345, 427, 573],
        [-128, -8, 75, 187, 339, 421, 567],
        [-134, -14, 69, 181, 331, 415, 561],
        [-140, -20, 63, 175, 325, 409, 555],
    ];

    /// <summary>The angle keys' times, the same for every dot.</summary>
    internal static readonly TimeSpan[] AngleTimes =
        [Seconds(0), Seconds(0.433), Seconds(1.2), Seconds(1.617), Seconds(2.017), Seconds(2.783), Seconds(3.217)];

    /// <summary>The angle keys' <c>KeySpline</c>s, the same for every dot; null where the key has none (a straight line).</summary>
    internal static readonly Spline?[] AngleSplines =
    [
        new(0.13, 0.21, 0.1, 0.7),
        new(0.02, 0.33, 0.38, 0.77),
        null,
        new(0.57, 0.17, 0.95, 0.75),
        new(0, 0.19, 0.07, 0.72),
        null,
        new(0, 0, 0.95, 0.37),
    ];

    /// <summary>Each dot's opacity keys, discrete: shown at its begin, still at 3.21 s, hidden from 3.22 s, still at 3.47 s.</summary>
    internal static readonly Key[] OpacityKeys =
        [new(Seconds(0), 1), new(Seconds(3.21), 1), new(Seconds(3.22), 0), new(Seconds(3.47), 0)];

    /// <summary>The dots' angle tracks, E1R to E6R.</summary>
    internal static readonly PhoneKeyframeTrack[] Angles = BeginTimes
        .Select((begin, dot) => new PhoneKeyframeTrack(Kind.Spline, begin,
            AngleTimes.Select((time, i) => new Key(time, AngleValues[dot][i], AngleSplines[i])).ToArray()))
        .ToArray();

    /// <summary>The dots' opacity tracks, E1 to E6.</summary>
    internal static readonly PhoneKeyframeTrack[] Opacities = BeginTimes
        .Select(begin => new PhoneKeyframeTrack(Kind.Discrete, begin, OpacityKeys))
        .ToArray();

    /// <summary>The storyboard's length, its longest track's end: 4.305 s. It repeats from there.</summary>
    internal static readonly TimeSpan Duration = Angles.Concat(Opacities).Max(t => t.End);

    private static readonly string[] DotNames = ["PART_E1", "PART_E2", "PART_E3", "PART_E4", "PART_E5", "PART_E6"];
    private static readonly string[] CanvasNames = ["PART_E1R", "PART_E2R", "PART_E3R", "PART_E4R", "PART_E5R", "PART_SixthCircle"];

    private static readonly ConditionalWeakTable<ProgressRing, Runner> Runners = new();

    static PhoneProgressRing()
    {
        IsAnimatedProperty.Changed.AddClassHandler<ProgressRing>((ring, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                Runners.GetValue(ring, r => new Runner(r)).Attach();
            }
            else if (Runners.TryGetValue(ring, out var runner))
            {
                runner.Detach();
                Runners.Remove(ring);
            }
        });
    }

    /// <summary>Gets <see cref="IsAnimatedProperty"/>.</summary>
    public static bool GetIsAnimated(ProgressRing ring) => ring.GetValue(IsAnimatedProperty);

    /// <summary>Sets <see cref="IsAnimatedProperty"/>.</summary>
    public static void SetIsAnimated(ProgressRing ring, bool value) => ring.SetValue(IsAnimatedProperty, value);

    /// <summary>The storyboard's time at <paramref name="elapsed"/> since it started, repeating forever.</summary>
    internal static TimeSpan StoryboardTime(TimeSpan elapsed) => TimeSpan.FromTicks(elapsed.Ticks % Duration.Ticks);

    /// <summary>Runs one ring's storyboard.</summary>
    private sealed class Runner(ProgressRing ring)
    {
        private readonly Ellipse?[] _dots = new Ellipse?[6];
        private readonly RotateTransform?[] _rotations = new RotateTransform?[6];
        private readonly IDisposable?[] _shown = new IDisposable?[6];
        private readonly double?[] _opacity = new double?[6];
        private IDisposable? _pending;
        private TimeSpan _start;
        private bool _running;
        private bool _attached;

        public void Attach()
        {
            ring.TemplateApplied += OnTemplateApplied;
            ring.AttachedToVisualTree += OnAttached;
            ring.DetachedFromVisualTree += OnDetached;
            ring.PropertyChanged += OnPropertyChanged;
            _attached = TopLevel.GetTopLevel(ring) is not null;
            Update();
        }

        public void Detach()
        {
            ring.TemplateApplied -= OnTemplateApplied;
            ring.AttachedToVisualTree -= OnAttached;
            ring.DetachedFromVisualTree -= OnDetached;
            ring.PropertyChanged -= OnPropertyChanged;
            Stop();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            Stop();
            for (var i = 0; i < 6; i++)
            {
                _dots[i] = e.NameScope.Find<Ellipse>(DotNames[i]);
                _rotations[i] = e.NameScope.Find<Control>(CanvasNames[i])?.RenderTransform as RotateTransform;
            }

            Update();
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

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ProgressRing.IsActiveProperty)
            {
                Update();
            }
        }

        /// <summary>Starts or stops the storyboard as the ring's state says.</summary>
        private void Update()
        {
            var run = ring.IsActive && _attached && _dots.Any(d => d is not null);
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

        /// <summary>One frame: every dot at the storyboard's time now, then the next frame.</summary>
        private void Step()
        {
            _pending = null;
            if (!_running)
            {
                return;
            }

            var time = StoryboardTime(CornflowerClock.Now - _start);
            for (var i = 0; i < 6; i++)
            {
                if (_rotations[i] is { } rotation)
                {
                    rotation.Angle = Angles[i].ValueAt(time) ?? 0;
                }

                Show(i, Opacities[i].ValueAt(time));
            }

            _pending = CornflowerClock.RequestFrame(ring, Step);
        }

        /// <summary>Sets a dot's opacity at animation priority when it changes; null gives it back its own.</summary>
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

        private void Stop()
        {
            _running = false;
            _pending?.Dispose();
            _pending = null;
            for (var i = 0; i < 6; i++)
            {
                Show(i, null);
                if (_rotations[i] is { } rotation)
                {
                    rotation.Angle = 0;
                }
            }
        }
    }
}
