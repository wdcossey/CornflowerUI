using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// One <c>DoubleAnimationUsingKeyFrames</c> of an 8.1 template's storyboard, evaluated as WinRT does: the value of
/// its target at a time in the storyboard. A theme's timeline runs it frame by frame on <c>CornflowerClock</c>,
/// because Avalonia's own animations run on a clock a test can't replace (ADR 0003).
/// </summary>
/// <remarks>
/// <para>
/// WinRT's rules, as its <c>DoubleAnimationUsingKeyFrames</c> documents them:
/// </para>
/// <list type="bullet">
/// <item>Before the track's <see cref="BeginTime"/> it has no value: its target shows its own (<c>null</c> here).</item>
/// <item>A <see cref="Kind.Discrete"/> key holds its value from its key time until the next key's.</item>
/// <item>A <see cref="Kind.Spline"/> key is reached from the key before it over the time between them, through its
/// <c>KeySpline</c> (<see cref="KeySplineCurve"/>, solved exactly; none is a straight line); the first key, at 0, is the value at the start.</item>
/// <item>After the last key the value holds (<c>FillBehavior="HoldEnd"</c>, WinRT's default).</item>
/// </list>
/// <para>Times are kept in ticks, so a key time compares exactly with a time made the same way.</para>
/// </remarks>
internal sealed class PhoneKeyframeTrack
{
    /// <summary>How a track's keys move between them.</summary>
    public enum Kind
    {
        /// <summary><c>DiscreteDoubleKeyFrame</c>: a step at each key time.</summary>
        Discrete,

        /// <summary><c>SplineDoubleKeyFrame</c>: a curve into each key.</summary>
        Spline,
    }

    /// <summary>One key: its time after the track's begin time, its value, and the curve into it (spline keys only).</summary>
    public readonly record struct Key(TimeSpan Time, double Value, KeySplineCurve? Curve = null);

    private readonly Key[] _keys;

    /// <summary>A track of keys in time order, the first at 0.</summary>
    public PhoneKeyframeTrack(Kind kind, TimeSpan beginTime, params Key[] keys)
    {
        if (keys.Length == 0 || keys[0].Time != TimeSpan.Zero)
        {
            throw new ArgumentException("A track starts with a key at 0.", nameof(keys));
        }

        for (var i = 1; i < keys.Length; i++)
        {
            if (keys[i].Time <= keys[i - 1].Time)
            {
                throw new ArgumentException("A track's keys are in time order, each after the last.", nameof(keys));
            }
        }

        KeyKind = kind;
        BeginTime = beginTime;
        _keys = keys.ToArray();
    }

    /// <summary>Discrete or spline.</summary>
    public Kind KeyKind { get; }

    /// <summary>When the track starts, in the storyboard's time.</summary>
    public TimeSpan BeginTime { get; }

    /// <summary>When its last key is reached, in the storyboard's time: the track's part of the storyboard's length.</summary>
    public TimeSpan End => BeginTime + _keys[^1].Time;

    /// <summary>The track's value at <paramref name="time"/> in the storyboard, or null before it begins.</summary>
    public double? ValueAt(TimeSpan time)
    {
        var local = time - BeginTime;
        if (local < TimeSpan.Zero)
        {
            return null;
        }

        if (KeyKind == Kind.Discrete)
        {
            var value = _keys[0].Value;
            foreach (var key in _keys)
            {
                if (key.Time > local)
                {
                    break;
                }

                value = key.Value;
            }

            return value;
        }

        for (var i = 1; i < _keys.Length; i++)
        {
            var to = _keys[i];
            if (local > to.Time)
            {
                continue;
            }

            var from = _keys[i - 1];
            var progress = (double)(local - from.Time).Ticks / (to.Time - from.Time).Ticks;
            var eased = to.Curve is { } curve ? curve.Ease(progress) : progress;
            return from.Value + (to.Value - from.Value) * eased;
        }

        return _keys[^1].Value;
    }

    /// <summary>A time from seconds, to the tick, as a template writes key times.</summary>
    public static TimeSpan Seconds(double seconds) => TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond));
}
