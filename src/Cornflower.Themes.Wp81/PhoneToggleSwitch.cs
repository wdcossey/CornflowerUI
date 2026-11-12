using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 phone's tap and drag on a ToggleSwitch, as the emulator measured them (tools/refapp/README.md, "ToggleSwitch:
/// tap or drag" and "the hold limit, the distance and the halfway line"), on Avalonia's own ToggleSwitch. Turned on by
/// the Wp81 ToggleSwitch theme's own setter.
/// </summary>
/// <remarks>
/// <para><b>Where.</b> Only a press on the template's <c>SwitchThumb</c> moves the switch: the phone's transparent
/// <c>Thumb</c>, the switch with 13.5 either side, 15.5 above and 6.5 below. A press anywhere else on the template does
/// nothing, as on the phone, where only that Thumb takes the drag and the tap: it is marked handled before it reaches
/// the ToggleSwitch, so Avalonia's Button never presses or clicks. A press a control in the content handles is that
/// control's.</para>
/// <para><b>The drag.</b> The knob follows the finger one for one from the first move, with no distance first, kept
/// between its two rests (<c>PART_MovingKnobs</c>' <c>Canvas.Left</c> from 0 to <c>PART_SwitchKnob</c>'s width, which
/// is the phone's <c>KnobOnToOffOffset</c>). The knob's own transitions are off while it follows.</para>
/// <para><b>The release.</b> A tap toggles: a press that moved less than <see cref="TapDistance"/> from where it
/// started and saw no pointer event <see cref="HoldTime"/> or more after it, the release included. A tap that moved the
/// knob puts it back at its rest at once, then slides it, as the phone's <c>Dragging</c> to <c>Off</c> then <c>Off</c>
/// to <c>On</c> does. Otherwise a press that moved decides by where the knob is: past half the travel from where it
/// started, it toggles. The knob then slides to its rest on the theme's <c>KnobTransitions</c>. A tap toggles as
/// Avalonia's click does, through the switch's toggle pattern, so <c>Click</c> and <c>Command</c> run; a drag sets
/// <c>IsChecked</c>, as Avalonia's own drag does. A lost capture puts the knob back.</para>
/// <para>It reads the template's parts by name and sets only <c>Canvas.Left</c>, the moving part's <c>Transitions</c>
/// and <c>IsChecked</c>, all public. Avalonia's own drag handlers are on <c>PART_MovingKnobs</c>, under the thumb,
/// so they never see a press. The time is <see cref="CornflowerClock"/>'s, read at each pointer event.</para>
/// </remarks>
internal static class PhoneToggleSwitch
{
    /// <summary>
    /// How far a tap can move, in view pixels: 2.7 mm at the reference profile's 167.9 view pixels an inch (403 dpi at
    /// 2.4). The emulator's farthest taps moved 2.63 to 2.64 mm and its nearest misses 2.75 to 2.79 mm, on three
    /// profiles; 17.47 view px tapped on the reference profile and 18.33 didn't.
    /// </summary>
    internal const double TapDistance = 2.7 / 25.4 * 403 / 2.4;

    /// <summary>A press that sees a pointer event this long after it is a hold, not a tap: 1,000 ms (64 ticks of 15.625 ms).</summary>
    internal static readonly TimeSpan HoldTime = TimeSpan.FromMilliseconds(1000);

    /// <summary>Set by the Wp81 ToggleSwitch theme: the phone's tap and drag.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, bool>("IsEnabled", typeof(PhoneToggleSwitch));

    private static readonly ConditionalWeakTable<ToggleSwitch, Follower> Followers = new();

    static PhoneToggleSwitch()
    {
        IsEnabledProperty.Changed.AddClassHandler<ToggleSwitch>((toggle, change) =>
        {
            if (Followers.TryGetValue(toggle, out var old))
            {
                old.Detach();
                Followers.Remove(toggle);
            }

            if (change.GetNewValue<bool>())
            {
                var follower = new Follower(toggle);
                Followers.Add(toggle, follower);
                follower.Attach();
            }
        });
    }

    public static bool GetIsEnabled(ToggleSwitch toggle) => toggle.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ToggleSwitch toggle, bool value) => toggle.SetValue(IsEnabledProperty, value);

    /// <summary>Follows one ToggleSwitch's presses on its template's parts.</summary>
    private sealed class Follower(ToggleSwitch toggle)
    {
        private Control? _root;
        private Control? _thumb;
        private Panel? _knob;
        private Panel? _track;
        private Press? _press;

        public void Attach()
        {
            toggle.TemplateApplied += OnTemplateApplied;
            // The template may already be applied: a theme that turns this on after it gets no TemplateApplied for it.
            var named = toggle.GetVisualDescendants().OfType<Control>()
                .Where(c => c.Name is not null && c.TemplatedParent == toggle)
                .GroupBy(c => c.Name!).ToDictionary(g => g.Key, g => g.First());
            Hook(toggle.GetVisualChildren().FirstOrDefault() as Control, name => named.GetValueOrDefault(name));
        }

        public void Detach()
        {
            toggle.TemplateApplied -= OnTemplateApplied;
            Hook(null, _ => null);
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) =>
            Hook(toggle.GetVisualChildren().FirstOrDefault() as Control, name => e.NameScope.Find<Control>(name));

        /// <summary>Lets go of the old template's parts and takes the new one's: the root, the thumb and the two knob panels.</summary>
        private void Hook(Control? root, Func<string, Control?> find)
        {
            if (_root is not null)
            {
                _root.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
                _root.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
                _root.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            }

            _thumb?.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _press = null;

            _root = root;
            _thumb = find("SwitchThumb");
            _knob = find("PART_MovingKnobs") as Panel;
            _track = find("PART_SwitchKnob") as Panel;
            if (_root is null || _thumb is null || _knob is null || _track is null)
            {
                _root = null;
                _thumb = null;
                return;
            }

            _root.AddHandler(InputElement.PointerPressedEvent, OnPressed);
            _root.AddHandler(InputElement.PointerMovedEvent, OnMoved);
            _root.AddHandler(InputElement.PointerReleasedEvent, OnReleased);
            _thumb.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
        }

        /// <summary>The knob's travel: <c>PART_SwitchKnob</c>'s width, the phone's <c>KnobOnToOffOffset</c>.</summary>
        private double Travel => _track!.Bounds.Width;

        private double RestOf(bool on) => on ? Travel : 0;

        /// <summary>
        /// A left press (a touch, a pen tip, the mouse's left button) that no control in the content took. On the thumb,
        /// the first one starts a press; anywhere else on the template, nothing, and no other press either.
        /// </summary>
        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(toggle).Properties.IsLeftButtonPressed)
                return;

            e.Handled = true;
            if (_press is not null || e.Source is not Visual source || !(source == _thumb || _thumb!.IsVisualAncestorOf(source)) ||
                TopLevel.GetTopLevel(toggle) is not { } top)
                return;

            // From where the knob rests, or is sliding to: the phone's slide runs in the compositor and leaves the knob's
            // translation at its end, so a press mid-slide takes the knob from there.
            var on = toggle.IsChecked == true;
            var left = _knob!.GetBaseValue(Canvas.LeftProperty).GetValueOrDefault(double.NaN);
            _press = new Press(e.Pointer, e.GetPosition(toggle), e.GetPosition(top), CornflowerClock.Now, on,
                double.IsNaN(left) ? RestOf(on) : left);
            _knob!.Transitions = null;
            e.Pointer.Capture(_thumb);
        }

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (_press is not { } press || press.Pointer != e.Pointer)
                return;

            e.Handled = true;
            Follow(press, e, CornflowerClock.Now);
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_press is not { } press || press.Pointer != e.Pointer)
                return;

            e.Handled = true;
            Follow(press, e, CornflowerClock.Now);
            _press = null;

            var tap = !press.Held && press.Farthest < TapDistance;
            if (tap)
            {
                // The phone puts the knob back at once (Dragging to Off or On), then slides it from there as it toggles.
                Canvas.SetLeft(_knob!, RestOf(press.WasOn));
                _knob!.Transitions = toggle.KnobTransitions;
                if (toggle.IsEffectivelyEnabled)
                    (ControlAutomationPeer.CreatePeerForElement(toggle).GetProvider<IToggleProvider>())?.Toggle();
                return;
            }

            _knob!.Transitions = toggle.KnobTransitions;
            var half = Travel / 2;
            var at = press.StartLeft + press.Moved;           // where the finger has the knob, past the ends too
            var toggled = press.HasMoved && (press.WasOn ? at <= half : at >= half);
            if (toggled)
                toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, !press.WasOn);
            else
                Canvas.SetLeft(_knob!, RestOf(press.WasOn));
        }

        /// <summary>The knob under the finger, how far the finger has gone and whether the press is a hold yet.</summary>
        private void Follow(Press press, PointerEventArgs e, TimeSpan now)
        {
            if (now - press.Started >= HoldTime)
                press.Held = true;

            if (TopLevel.GetTopLevel(toggle) is { } top)
            {
                var screen = e.GetPosition(top) - press.OnScreen;
                press.Farthest = Math.Max(press.Farthest, Math.Sqrt(screen.X * screen.X + screen.Y * screen.Y));
            }

            var moved = e.GetPosition(toggle).X - press.InSwitch.X;
            if (moved == press.Moved)
                return;

            press.Moved = moved;
            press.HasMoved = true;
            Canvas.SetLeft(_knob!, Math.Clamp(press.StartLeft + moved, 0, Travel));
        }

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (_press is not { } press || press.Pointer != e.Pointer)
                return;

            _press = null;
            _knob!.Transitions = toggle.KnobTransitions;
            Canvas.SetLeft(_knob, RestOf(press.WasOn));
        }
    }

    /// <summary>
    /// One press on the thumb: its pointer, where it started in the switch and on the screen, when, whether the switch
    /// was on, where the knob was, how far the finger has moved it, the farthest the finger has gone on the screen, and
    /// whether it is a hold.
    /// </summary>
    private sealed class Press(IPointer pointer, Point inSwitch, Point onScreen, TimeSpan started, bool wasOn, double startLeft)
    {
        public IPointer Pointer { get; } = pointer;

        public Point InSwitch { get; } = inSwitch;

        public Point OnScreen { get; } = onScreen;

        public TimeSpan Started { get; } = started;

        public bool WasOn { get; } = wasOn;

        public double StartLeft { get; } = startLeft;

        public double Moved { get; set; }

        public bool HasMoved { get; set; }

        public double Farthest { get; set; }

        public bool Held { get; set; }
    }
}
