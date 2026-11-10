using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
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
/// it has no template. Its transform goes on the root's <c>RenderTransform</c> at animation
/// priority, over the root's own value, which is back untouched at rest. The control's own space
/// isn't tilted, so <c>GetPosition</c> on it, and its layout, are unchanged.
/// </para>
/// <para>
/// Avalonia hit-tests through a <c>RenderTransform</c>. Pressed within about 3 px of an edge, the
/// tilted element is drawn sunk away from under the finger, so if it is the template's root, a
/// <c>Button</c> doesn't click there. The phone's does (run <c>20261110-0822-dev11</c>). So a
/// template marks the part to tilt with <see cref="IsTargetProperty"/>, inside a root that stays
/// untilted and is hit across its bounds (a <c>Transparent</c> background): the finger is always
/// over the root, and the control decides as it would without the tilt.
/// </para>
/// <para>
/// The geometry is in view pixels, with the screen taken as the control's <see cref="TopLevel"/>:
/// a phone's single view. Under a scaling ancestor, or in a desktop popup with a window of its own,
/// the eye isn't where the phone's would be.
/// </para>
/// </remarks>
public static class Tilt
{
    /// <summary>Whether the control tilts when pressed. Off by default.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(Tilt));

    /// <summary>
    /// Marks the part of a control's template that the tilt is drawn on, in place of the template's
    /// root. For a template whose root stays untilted and takes the hits; see <see cref="Tilt"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> IsTargetProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsTarget", typeof(Tilt));

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

    /// <summary>Gets <see cref="IsTargetProperty"/>.</summary>
    public static bool GetIsTarget(Control control) => control.GetValue(IsTargetProperty);

    /// <summary>Sets <see cref="IsTargetProperty"/>.</summary>
    public static void SetIsTarget(Control control, bool value) => control.SetValue(IsTargetProperty, value);

    /// <summary>The tilt showing on <paramref name="control"/> now, or <see cref="TiltState.Rest"/>. For tests.</summary>
    internal static TiltState Current(Control control) => control.GetValue(TrackerProperty)?.State ?? TiltState.Rest;

    /// <summary>
    /// The visual the tilt is drawn on: the part of the control's own template marked
    /// <see cref="IsTargetProperty"/>, else the template's root, else the control itself.
    /// </summary>
    internal static Visual TargetOf(Control control)
    {
        if (control is not TemplatedControl || control.GetVisualChildren().FirstOrDefault() is not Control root)
            return control;

        return MarkedPart(control, root) ?? root;
    }

    /// <summary>The first template part marked <see cref="IsTargetProperty"/>, not looking into the content.</summary>
    private static Control? MarkedPart(Control control, Control part)
    {
        if (!ReferenceEquals(part.TemplatedParent, control)) return null;
        if (GetIsTarget(part)) return part;

        foreach (var child in part.GetVisualChildren())
        {
            if (child is Control c && MarkedPart(control, c) is { } found) return found;
        }

        return null;
    }

    /// <summary>Whether a tilt-enabled control nearer the press than <paramref name="control"/> will tilt instead: only the nearest tilts, as the toolkit's.</summary>
    private static bool NearerTiltBelow(Control control, object? source)
    {
        for (var v = source as Visual; v is not null && !ReferenceEquals(v, control); v = v.GetVisualParent())
        {
            if (v is Control c && GetIsEnabled(c)) return true;
        }

        return false;
    }

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
        private Point _eye;            // the point under the eye, in the target's space, for this press
        private TimeSpan _released;
        private TiltState _held;
        private IDisposable? _pending;
        private bool _stepPending;
        private Visual? _target;
        private MatrixTransform? _transform;
        private IDisposable? _shown;   // the transform on the target, above its own values; disposed, they're back

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
            if (!e.GetCurrentPoint(_control).Properties.IsLeftButtonPressed || NearerTiltBelow(_control, e.Source))
                return;
            if (_phase is Phase.Pressed or Phase.Paused && !Lost())
                return;
            if (TargetOf(_control) is not { } target || target.GetVisualParent() is not { } parent)
                return;
            if (target.Bounds.Width <= 0 || target.Bounds.Height <= 0 || EyeFor(target, parent) is not { } eye)
                return;

            if (!ReferenceEquals(target, _target))
            {
                ToRest();
                _target = target;
            }

            _phase = Phase.Pressed;
            _pointer = e.Pointer;
            _press = e.GetPosition(parent);
            _eye = eye;
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
            // On the control: its own bounds, untilted, edges included. A control drawn tilted
            // itself (no template) has only its bounds in its parent.
            var on = ReferenceEquals(_target, _control)
                ? _target.Bounds.Contains(press)
                : new Rect(_control.Bounds.Size).Contains(e.GetPosition(_control));
            if (!on)
            {
                if (_phase == Phase.Pressed) StartReturn(Phase.Paused);
                return;
            }

            _phase = Phase.Pressed;
            _press = press;
            if (!_stepPending) Step();               // else the frame already asked for steps from here
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer == _pointer && e.InitialPressMouseButton == MouseButton.Left) LetGo();
        }

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (e.Pointer == _pointer) LetGo();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => ToRest();

        /// <summary>
        /// Whether the press being followed was lost without a word: its pointer no longer captured
        /// within the control, as when the part it was captured by left the tree. Then a new press
        /// starts afresh rather than being ignored.
        /// </summary>
        private bool Lost() =>
            _pointer?.Captured is not Visual captured ||
            !(ReferenceEquals(captured, _control) || _control.IsVisualAncestorOf(captured));

        /// <summary>The target is still this control's: not swapped out by a new template, and laid out.</summary>
        private bool TargetStands() =>
            _target is not null && (ReferenceEquals(_target, _control) || _control.IsVisualAncestorOf(_target)) &&
            _target.Bounds.Width > 0 && _target.Bounds.Height > 0;

        /// <summary>One frame of a press: the tilt for the finger as the current tilt shows it. Steps again next frame until it settles.</summary>
        private void Step()
        {
            Cancel();
            if (!TargetStands())
            {
                ToRest();
                return;
            }

            var bounds = _target!.Bounds;
            var next = TiltModel.Step(State, bounds.Size, _press - bounds.Position, _eye);
            var moved = Math.Abs(next.RotationX - State.RotationX) + Math.Abs(next.RotationY - State.RotationY) +
                        Math.Abs(next.OffsetZ - State.OffsetZ);
            Show(next, bounds.Size);

            if (moved > TiltModel.Settled)
            {
                _pending = CornflowerClock.RequestFrame(_control, Step);
                _stepPending = true;
            }
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
            if (factor <= 0 || !TargetStands())
            {
                if (_phase == Phase.Paused) Clear();
                else ToRest();
                return;
            }

            Show(_held.Scale(factor), _target!.Bounds.Size);
            _pending = CornflowerClock.RequestFrame(_control, Return);
        }

        /// <summary>
        /// The point under the eye, in the target's space: <see cref="TiltModel.EyeShift"/> of the way
        /// from its centre to the centre of its <see cref="TopLevel"/>, which on a phone is the
        /// screen. Null outside a window.
        /// </summary>
        private Point? EyeFor(Visual target, Visual parent)
        {
            if (TopLevel.GetTopLevel(_control) is not { } top) return null;

            var bounds = target.Bounds;
            var screenCentre = top.TranslatePoint(new Point(top.ClientSize.Width / 2, top.ClientSize.Height / 2), parent);
            return screenCentre is { } centre ? TiltModel.Eye(bounds.Size, centre - bounds.Center) : null;
        }

        private void Show(TiltState state, Size size)
        {
            State = state;
            if (_target is null) return;

            _transform ??= new MatrixTransform();

            // Avalonia draws a RenderTransform about RenderTransformOrigin; the model's matrix is about
            // the top left, so it's moved there and back.
            var origin = _target.RenderTransformOrigin.ToPixels(size);
            _transform.Matrix = Matrix.CreateTranslation(origin) * TiltModel.ToMatrix(state, size, _eye) *
                                Matrix.CreateTranslation(-origin);
            Put();
        }

        /// <summary>
        /// Puts the tilt's transform on the target at animation priority: above the target's own
        /// value, local or styled, which is back untouched once the tilt's is taken off.
        /// </summary>
        private void Put()
        {
            if (_shown is null && _target is not null && _transform is not null)
                _shown = _target.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
        }

        private void Cancel()
        {
            _pending?.Dispose();
            _pending = null;
            _stepPending = false;
        }

        /// <summary>No tilt showing: the target's own transform back, nothing pending. A press still down stays down.</summary>
        private void Clear()
        {
            Cancel();
            State = TiltState.Rest;
            _shown?.Dispose();
            _shown = null;
            _transform = null;
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
