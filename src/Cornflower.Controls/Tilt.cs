using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>
/// The tilt: a pressed element leans in under the finger and sinks, then eases back once the finger
/// lets go. An attached behaviour for any control, stock ones included; a theme turns it on with a
/// setter, <c>&lt;Setter Property="cf:Tilt.IsEnabled" Value="True" /&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Measured on Windows Phone 8.1 (#60): the rule, the eye and the timing are in
/// <see cref="TiltModel"/>. A press jumps to the tilt for its point, which settles over the next
/// frames as the phone's does; a move follows the finger; a release, a lost capture or the finger
/// leaving the control holds the tilt for 180 ms, then eases it back over 195 ms. A finger that
/// comes back onto the control before it lets go tilts it again, as the toolkit's TiltEffect does.
/// The tilt runs frame by frame on <see cref="CornflowerClock"/>, not on Avalonia's transitions, so
/// a test can step it. One pointer at a time: the first down is the one that tilts.
/// </para>
/// <para>
/// The tilt is drawn on the template's root, where the phone puts it, or on the control itself if
/// it has no template. Its <c>RenderTransform</c> is replaced while the tilt shows and put back at
/// rest. The control's own space isn't tilted, so <c>GetPosition</c> on it, and its layout, are
/// unchanged; hit testing goes through the tilt, as on the phone.
/// </para>
/// </remarks>
public static class Tilt
{
    /// <summary>Whether the control tilts when pressed. Off by default.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(Tilt));

    private static readonly AttachedProperty<Tracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<Control, Tracker?>("Tracker", typeof(Tilt));

    static Tilt()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                if (control.GetValue(TrackerProperty) is null)
                    control.SetValue(TrackerProperty, new Tracker(control));
            }
            else if (control.GetValue(TrackerProperty) is { } tracker)
            {
                tracker.Detach();
                control.ClearValue(TrackerProperty);
            }
        });
    }

    /// <summary>Gets <see cref="IsEnabledProperty"/>.</summary>
    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    /// <summary>Sets <see cref="IsEnabledProperty"/>.</summary>
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    /// <summary>The tilt showing on <paramref name="control"/> now, or <see cref="TiltState.Rest"/>. For tests.</summary>
    internal static TiltState Current(Control control) => control.GetValue(TrackerProperty)?.State ?? TiltState.Rest;

    /// <summary>The visual the tilt is drawn on: the template's root, or the control itself.</summary>
    internal static Visual TargetOf(Control control) =>
        control is TemplatedControl && control.GetVisualChildren().FirstOrDefault() is Control root ? root : control;

    private enum Phase
    {
        Rest,
        /// <summary>Down on the control: the tilt follows the finger.</summary>
        Pressed,
        /// <summary>Still down, but moved off the control: the tilt returns, and comes back if the finger does.</summary>
        Paused,
        /// <summary>Let go: the tilt returns.</summary>
        Returning,
    }

    /// <summary>One control's tilt: its handlers, its press and its frame loop.</summary>
    private sealed class Tracker
    {
        private readonly Control _control;
        private Phase _phase;
        private IPointer? _pointer;
        private Point _press;          // the finger, in the target's parent's space
        private TimeSpan _released;
        private TiltState _held;
        private IDisposable? _pending;
        private Visual? _target;
        private ITransform? _savedTransform;
        private MatrixTransform? _transform;

        public Tracker(Control control)
        {
            _control = control;
            control.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct, handledEventsToo: true);
            control.DetachedFromVisualTree += OnDetached;
        }

        public TiltState State { get; private set; }

        public void Detach()
        {
            _control.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _control.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            _control.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _control.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _control.DetachedFromVisualTree -= OnDetached;
            ToRest();
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_phase is Phase.Pressed or Phase.Paused || !e.GetCurrentPoint(_control).Properties.IsLeftButtonPressed)
                return;
            if (TargetOf(_control) is not { } target || target.GetVisualParent() is not { } parent)
                return;
            if (target.Bounds.Width <= 0 || target.Bounds.Height <= 0)
                return;

            if (!ReferenceEquals(target, _target))
            {
                ToRest();
                _target = target;
            }

            _phase = Phase.Pressed;
            _pointer = e.Pointer;
            _press = e.GetPosition(parent);
            Step();
        }

        /// <summary>
        /// Follows the finger while it's on the control. Off it, in the untilted bounds, the tilt holds
        /// and returns as from a release (run 2118's drag); back on, it follows again, as the
        /// toolkit's TiltEffect does (not measured on 8.1).
        /// </summary>
        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (_phase is not (Phase.Pressed or Phase.Paused) || e.Pointer != _pointer || _target?.GetVisualParent() is not { } parent)
                return;

            var press = e.GetPosition(parent);
            var bounds = _target.Bounds;
            var inside = press.X >= bounds.Left && press.X <= bounds.Right && press.Y >= bounds.Top && press.Y <= bounds.Bottom;
            if (!inside)
            {
                if (_phase == Phase.Pressed) StartReturn(Phase.Paused);
                return;
            }

            _phase = Phase.Pressed;
            _press = press;
            Step();
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer == _pointer) LetGo();
        }

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (e.Pointer == _pointer) LetGo();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => ToRest();

        /// <summary>One frame of a press: the tilt for the finger as the current tilt shows it. Steps again next frame until it settles.</summary>
        private void Step()
        {
            Cancel();
            if (Eye() is not { } eye) return;

            var bounds = _target!.Bounds;
            var next = TiltModel.Step(State, bounds.Size, _press - bounds.Position, eye);
            var moved = Math.Abs(next.RotationX - State.RotationX) + Math.Abs(next.RotationY - State.RotationY) +
                        Math.Abs(next.OffsetZ - State.OffsetZ);
            Show(next, bounds.Size, eye);

            if (moved > TiltModel.Settled)
                _pending = CornflowerClock.RequestFrame(_control, Step);
        }

        /// <summary>The finger is up, or the pointer was taken: the tilt returns, if it isn't already, and the press is over.</summary>
        private void LetGo()
        {
            _pointer = null;
            if (_phase == Phase.Pressed) StartReturn(Phase.Returning);
            else if (_phase == Phase.Paused && _pending is null) ToRest();     // off the control and already back
            else if (_phase == Phase.Paused) _phase = Phase.Returning;
        }

        private void StartReturn(Phase phase)
        {
            _phase = phase;
            _held = State;
            _released = CornflowerClock.Now;
            Cancel();
            _pending = CornflowerClock.Schedule(Return, TiltModel.Hold);
        }

        /// <summary>One frame of the return: the held tilt, eased back by the time since the release.</summary>
        private void Return()
        {
            _pending = null;
            var factor = TiltModel.ReturnFactor(CornflowerClock.Now - _released);
            if (factor <= 0 || Eye() is not { } eye)
            {
                if (_phase == Phase.Paused) Clear();
                else ToRest();
                return;
            }

            Show(_held.Scale(factor), _target!.Bounds.Size, eye);
            _pending = CornflowerClock.RequestFrame(_control, Return);
        }

        /// <summary>The point under the eye, in the target's space, or null if the target isn't in a window.</summary>
        private Point? Eye()
        {
            if (_target?.GetVisualParent() is not { } parent || TopLevel.GetTopLevel(_control) is not { } top)
                return null;

            var bounds = _target.Bounds;
            var screenCentre = top.TranslatePoint(new Point(top.ClientSize.Width / 2, top.ClientSize.Height / 2), parent);
            return screenCentre is { } centre ? TiltModel.Eye(bounds.Size, centre - bounds.Center) : null;
        }

        private void Show(TiltState state, Size size, Point eye)
        {
            State = state;
            if (_target is null) return;

            if (_transform is null)
            {
                _savedTransform = _target.RenderTransform;
                _transform = new MatrixTransform();
                _target.RenderTransform = _transform;
            }

            // Avalonia draws a RenderTransform about RenderTransformOrigin; the model's matrix is about
            // the top left, so it's moved there and back.
            var origin = _target.RenderTransformOrigin.ToPixels(size);
            _transform.Matrix = Matrix.CreateTranslation(origin) * TiltModel.ToMatrix(state, size, eye) *
                                Matrix.CreateTranslation(-origin);
        }

        private void Cancel()
        {
            _pending?.Dispose();
            _pending = null;
        }

        /// <summary>No tilt showing: the target's own transform back, nothing pending. A press still down stays down.</summary>
        private void Clear()
        {
            Cancel();
            State = TiltState.Rest;
            if (_target is not null && _transform is not null && ReferenceEquals(_target.RenderTransform, _transform))
                _target.RenderTransform = _savedTransform;
            _transform = null;
            _savedTransform = null;
        }

        /// <summary>Back to rest at once, and the press forgotten.</summary>
        private void ToRest()
        {
            Clear();
            _phase = Phase.Rest;
            _pointer = null;
            _target = null;
        }
    }
}
