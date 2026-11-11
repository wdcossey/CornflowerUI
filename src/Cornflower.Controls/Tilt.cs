using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
/// leaving the control holds the tilt for 182.6 ms, then eases it back over 167 ms. A finger that
/// comes back onto the control before it lets go tilts it again, as the toolkit's TiltEffect does.
/// The tilt runs frame by frame on <see cref="CornflowerClock"/>, not on Avalonia's transitions, so
/// a test can step it. One pointer at a time: the first down is the one that tilts.
/// </para>
/// <para>
/// The tilt is drawn on the template's root, where the phone puts it, or on the control itself if
/// it has no template. Its transform goes on the root's <c>RenderTransform</c> at animation
/// priority, over the root's own value, which is back untouched at rest. The control's own space
/// isn't tilted, so <c>GetPosition</c> on it, and its layout, are unchanged. If a template gives
/// the tilted part a transform of its own, the tilt is drawn after it, as the phone draws a
/// <c>PlaneProjection</c> after a <c>RenderTransform</c>, and the finger's point is taken back
//// through both. That transform changing in place (an animation on its scale, say) shows at once,
/// and a press steps again for the point now under the finger. A new value for it, local or
/// styled, set while a press has settled, shows at the next move or the return: Avalonia 12.1.4
/// raises no change for a value set under an animation-priority one. Only the local or styled
/// value is composed. One at animation priority, as Avalonia's <c>Animation</c> and
/// <c>Transitions</c> set it (a pressed scale with a <c>TransformOperationsTransition</c>, say),
/// isn't the base value and can't be read under the tilt's: the later of the two shows, so one
/// started before the press is hidden while the tilt shows, and one started during it hides the
/// tilt. A template that animates the tilted part's transform puts it on another element.
/// </para>
/// <para>
/// A press in a popup the control owns is routed through the control but isn't on it, so it
/// doesn't tilt it. A new template ends a press: the old part goes back to rest, and the next press
/// tilts the new one.
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

    /// <summary>Every tracker's transform, so that an inner tilt's eye can leave an outer tilt out.</summary>
    private static readonly ConditionalWeakTable<MatrixTransform, Tracker> TiltTransforms = new();

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

    /// <summary>
    /// Whether a tilt-enabled control nearer the press than <paramref name="control"/> will tilt
    /// instead: only the nearest tilts, as the toolkit's. One that can't tilt at all (its target has
    /// no size) leaves the press to the next one out; one that is busy with another finger still
    /// keeps it from the outer one, so a second finger never tilts two at once.
    /// </summary>
    private static bool NearerTiltBelow(Control control, object? source)
    {
        for (var v = source as Visual; v is not null && !ReferenceEquals(v, control); v = v.GetVisualParent())
        {
            if (v is Control c && c.GetValue(TrackerProperty) is { CanTilt: true }) return true;
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
        private readonly MatrixTransform _transform = new();     // the tilt's, one for the tracker's life
        private Phase _phase;
        private IPointer? _pointer;
        private Point _press;          // the finger, in the target's parent's space
        private Point _eye;            // the point under the eye, in the target's space, as of the last frame
        private int _steps;            // steps taken for this point of the finger
        private TimeSpan _released;
        private TiltState _held;
        private IDisposable? _pending; // in Pressed, a step; in Paused or Returning, the return
        private Visual? _target;
        private ITransform? _own;      // the target's own transform under the tilt, watched while shown
        private IDisposable? _shown;   // the transform on the target, above its own values; disposed, they're back

        public Tracker(Control control)
        {
            _control = control;
            TiltTransforms.Add(_transform, this);
            control.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
            control.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct, handledEventsToo: true);
            control.DetachedFromVisualTree += OnDetached;
        }

        public TiltState State { get; private set; }

        /// <summary>Whether the control has anything to tilt: a target with a size. Cheap: no eye.</summary>
        public bool CanTilt => TargetOf(_control).Bounds is { Width: > 0, Height: > 0 };

        public void Detach()
        {
            _control.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _control.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            _control.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _control.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _control.DetachedFromVisualTree -= OnDetached;
            ToRest();
        }

        /// <summary>
        /// Whether a press from <paramref name="source"/> would tilt this control, and what it would
        /// tilt. It has to be on the control: a press in a popup the control owns is routed through
        /// it, but is in another visual tree. And the control has to be free, with a target that has
        /// a size, in a window.
        /// </summary>
        public bool Takes(object? source, [NotNullWhen(true)] out Visual? target)
        {
            target = null;
            if (source is not Visual hit || !(ReferenceEquals(hit, _control) || _control.IsVisualAncestorOf(hit)))
                return false;
            if (_phase is Phase.Pressed or Phase.Paused)
                return false;

            var part = TargetOf(_control);
            if (part.GetVisualParent() is not { } parent || part.Bounds.Width <= 0 || part.Bounds.Height <= 0 ||
                EyeFor(part, parent) is null)
                return false;

            target = part;
            return true;
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(_control).Properties.IsLeftButtonPressed || NearerTiltBelow(_control, e.Source) ||
                !Takes(e.Source, out var target))
                return;

            if (!ReferenceEquals(target, _target))
            {
                ToRest();
                Retarget(target);
            }

            _phase = Phase.Pressed;
            _pointer = e.Pointer;
            _press = e.GetPosition(target.GetVisualParent());
            _steps = 0;
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

            var stepping = _phase == Phase.Pressed && _pending is not null;
            _phase = Phase.Pressed;
            _press = press;
            _steps = 0;
            if (!stepping) Step();                   // else the frame already asked for steps from here
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

        /// <summary>The target left the tree: a new template took it away. The press is over, and the next one tilts the new target.</summary>
        private void OnTargetDetached(object? sender, VisualTreeAttachmentEventArgs e) => ToRest();

        /// <summary>The target's own transform changed in place.</summary>
        private void OnOwnChanged(object? sender, EventArgs e) => Redraw();

        private void Retarget(Visual? target)
        {
            if (_target is not null) _target.DetachedFromVisualTree -= OnTargetDetached;
            _target = target;
            if (_target is not null) _target.DetachedFromVisualTree += OnTargetDetached;
        }

        /// <summary>The target is laid out. (One a new template took away has already put the tracker at rest.)</summary>
        private bool TargetStands() => _target is not null && _target.Bounds.Width > 0 && _target.Bounds.Height > 0;

        /// <summary>
        /// One frame of a press: the tilt for the finger as the current tilt shows it. Steps again
        /// next frame until it settles, or for <see cref="TiltModel.MaxSteps"/> frames at one point,
        /// after which it takes the rule for the point as it is, untilted, and stops.
        /// </summary>
        private void Step()
        {
            Cancel();
            if (!TargetStands())
            {
                ToRest();
                return;
            }

            var bounds = _target!.Bounds;
            Look();
            var own = OwnMatrix(bounds.Size);
            var last = ++_steps >= TiltModel.MaxSteps;
            var next = TiltModel.Step(last ? TiltState.Rest : State, bounds.Size, _press - bounds.Position, _eye, own);
            var moved = Math.Abs(next.RotationX - State.RotationX) + Math.Abs(next.RotationY - State.RotationY) +
                        Math.Abs(next.OffsetZ - State.OffsetZ);
            Show(next, bounds.Size, own);

            if (moved > TiltModel.Settled && !last)
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
            if (factor <= 0 || !TargetStands())
            {
                if (_phase == Phase.Paused) Clear();
                else ToRest();
                return;
            }

            Look();
            var size = _target!.Bounds.Size;
            Show(_held.Scale(factor), size, OwnMatrix(size));
            _pending = CornflowerClock.RequestFrame(_control, Return);
        }

        /// <summary>The eye for this frame: the control may have moved, or its window been resized, since the last.</summary>
        private void Look()
        {
            if (_target?.GetVisualParent() is { } parent && EyeFor(_target, parent) is { } eye) _eye = eye;
        }

        /// <summary>
        /// The point under the eye, in the target's space: <see cref="TiltModel.EyeShift"/> of the way
        /// from its centre to the centre of its <see cref="TopLevel"/>, which on a phone is the
        /// screen. Null outside a window.
        /// </summary>
        /// <remarks>
        /// The screen's centre is taken into the parent's space as <c>TranslatePoint</c> takes it, by
        /// each ancestor's offset and <c>RenderTransform</c>, but an ancestor's tilt is left out: a
        /// tilting item round a pressed Button would move the Button's eye every frame of its own
        /// return, and the Button would never settle while it moved.
        /// </remarks>
        private Point? EyeFor(Visual target, Visual parent)
        {
            if (TopLevel.GetTopLevel(_control) is not { } top) return null;

            var toTop = Matrix.Identity;
            Visual? v = parent;
            for (; v is not null && !ReferenceEquals(v, top); v = v.GetVisualParent())
            {
                var transform = v.RenderTransform;
                if (transform is MatrixTransform shown && TiltTransforms.TryGetValue(shown, out _))
                    transform = v.GetBaseValue(Visual.RenderTransformProperty).GetValueOrDefault();
                if (transform is not null)
                {
                    var origin = v.RenderTransformOrigin.ToPixels(v.Bounds.Size);
                    toTop *= Matrix.CreateTranslation(-origin) * transform.Value * Matrix.CreateTranslation(origin);
                }

                toTop *= Matrix.CreateTranslation(v.Bounds.Position);
            }

            if (v is null || !toTop.TryInvert(out var fromTop)) return null;

            var bounds = target.Bounds;
            var centre = fromTop.Transform(new Point(top.ClientSize.Width / 2, top.ClientSize.Height / 2));
            return TiltModel.Eye(bounds.Size, centre - bounds.Center);
        }

        /// <summary>
        /// The target's own transform, its local or styled value under the tilt's, as drawn on its
        /// space (origin top left), or identity. Watched for changes in place while the tilt shows.
        /// </summary>
        private Matrix OwnMatrix(Size size)
        {
            var own = _target!.GetBaseValue(Visual.RenderTransformProperty).GetValueOrDefault();
            if (!ReferenceEquals(own, _own))
            {
                if (_own is Transform before) before.Changed -= OnOwnChanged;
                _own = own;
                if (_own is Transform after) after.Changed += OnOwnChanged;
            }

            if (own is null) return Matrix.Identity;

            // Avalonia draws a RenderTransform about RenderTransformOrigin.
            var origin = _target.RenderTransformOrigin.ToPixels(size);
            return Matrix.CreateTranslation(-origin) * own.Value * Matrix.CreateTranslation(origin);
        }

        /// <param name="state">The tilt to show.</param>
        /// <param name="size">The target's size.</param>
        /// <param name="own">The target's own transform, from <see cref="OwnMatrix"/>.</param>
        private void Show(TiltState state, Size size, Matrix own)
        {
            State = state;
            if (_target is null) return;

            // Avalonia draws a RenderTransform about RenderTransformOrigin; the model's matrix is about
            // the top left, so it's moved there and back. The target's own transform, about its origin,
            // is drawn first, then the tilt: (own about the origin) × tilt, moved to the origin.
            var origin = _target.RenderTransformOrigin.ToPixels(size);
            _transform.Matrix = Matrix.CreateTranslation(origin) * own * TiltModel.ToMatrix(state, size, _eye) *
                                Matrix.CreateTranslation(-origin);
            Put();
        }

        /// <summary>
        /// The target's own transform changed in place while the tilt shows. Pressed, a different
        /// point of it is under the finger now, so the press steps again from here. Returning, the
        /// same tilt is drawn over the new transform.
        /// </summary>
        private void Redraw()
        {
            if (_shown is null || _target is null) return;

            if (_phase == Phase.Pressed)
            {
                _steps = 0;
                Step();
                return;
            }

            Look();
            var size = _target.Bounds.Size;
            Show(State, size, OwnMatrix(size));
        }

        /// <summary>
        /// Puts the tilt's transform on the target at animation priority: above the target's own
        /// value, local or styled, which is back untouched once the tilt's is taken off.
        /// </summary>
        private void Put()
        {
            if (_shown is null && _target is not null)
                _shown = _target.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
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
            var shown = _shown;
            _shown = null;
            shown?.Dispose();
            if (_own is Transform own) own.Changed -= OnOwnChanged;
            _own = null;
        }

        /// <summary>Back to rest at once, and the press forgotten.</summary>
        private void ToRest()
        {
            Clear();
            _phase = Phase.Rest;
            _pointer = null;
            Retarget(null);
        }
    }
}
