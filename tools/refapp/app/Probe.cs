using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// Behaviour that only input shows: a control on a page, real touches from the VM, and a log of what the control
    /// did. The app lays the page out, then writes the touches it wants as <c>gestures.txt</c>, in view pixels in
    /// window coordinates, and <c>ready.txt</c>. vm\session.ps1 pulls them and has vm\touch.ps1 play them on the
    /// emulator's display with the mouse, which the emulator turns into touches. The app logs every pointer event,
    /// visual state change and value it watches to <c>probe.txt</c>, with milliseconds since the page was ready, and
    /// writes <c>done.txt</c> once the last touch has been released and has settled, or, if the touches never come,
    /// when the timeout it gives in <c>ready.txt</c> runs out. run.ps1 -Probe picks one.
    /// </summary>
    internal sealed partial class Probe
    {
        private readonly StorageFolder _out = ApplicationData.Current.LocalFolder;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly StringBuilder _log = new StringBuilder();
        private readonly StringBuilder _gestures = new StringBuilder();
        private int _expected;
        private int _released;
        private Action _afterRelease;

        public static async Task RunAsync(Grid host, string name)
        {
            var probe = new Probe();
            foreach (var old in await probe._out.GetFilesAsync())
            {
                await old.DeleteAsync();
            }

            switch (name)
            {
                case "toggleswitch":
                    await probe.ToggleSwitchDrag(host);
                    break;
                case "tilt":
                    await probe.Tilt(host);
                    break;
                case "tiltedge":
                    await probe.TiltEdge(host);
                    break;
                case "tiltgrid":
                    await probe.TiltGrid(host);
                    break;
                case "motion":
                    await probe.Motion(host);
                    break;
                case "pages":
                    await probe.Pages(host);
                    break;
                case "projections":
                    await probe.Projections(host);
                    break;
                case "ring":
                    await probe.Ring(host);
                    break;
                default:
                    await probe.Finish("unknown probe " + name);
                    break;
            }
        }

        /// <summary>
        /// The ToggleSwitch's drag threshold (#25): from Off, press on the knob, move right by a distance, and lift
        /// when the press has lasted a given time. A grid of distances and press times, then distances near the
        /// knob's halfway point, then near each limit the grid found. After each, the switch is put back Off. The log
        /// shows, for each touch, the ToggleStates changes, the thumb's drag, the knob's transform every frame, and
        /// whether it toggled. What it found is in README.md, "Probes".
        /// </summary>
        private async Task ToggleSwitchDrag(Grid host)
        {
            var toggle = new ToggleSwitch { Header = "probe", IsOn = false, Margin = new Thickness(19, 120, 19, 0), VerticalAlignment = VerticalAlignment.Top };
            await Page(host, toggle);

            var knob = Find(toggle, "SwitchKnob");
            var knobTransform = knob != null ? knob.RenderTransform as TranslateTransform : null;
            var thumb = Find(toggle, "SwitchThumb") as Windows.UI.Xaml.Controls.Primitives.Thumb;
            if (knob == null)
            {
                await Finish("no SwitchKnob in the ToggleSwitch's template");
                return;
            }

            var bounds = knob.TransformToVisual(null).TransformBounds(new Rect(0, 0, knob.ActualWidth, knob.ActualHeight));
            Log("layout", "knob " + Num(bounds.X) + " " + Num(bounds.Y) + " " + Num(bounds.Width) + " " + Num(bounds.Height));
            Watch(toggle);
            toggle.Toggled += (s, e) => Log("toggled", "IsOn " + toggle.IsOn);
            WatchValue("knobX", raw => { if (knobTransform != null) raw.Add(knobTransform.X); }, () => knobTransform != null ? Num(knobTransform.X) : "none");
            if (thumb != null)
            {
                thumb.DragStarted += (s, e) => Log("thumb", "DragStarted " + Num(e.HorizontalOffset) + " " + Num(e.VerticalOffset));
                thumb.DragDelta += (s, e) => Log("thumb", "DragDelta " + Num(e.HorizontalChange) + " " + Num(e.VerticalChange));
                thumb.DragCompleted += (s, e) => Log("thumb", "DragCompleted " + Num(e.HorizontalChange) + " " + Num(e.VerticalChange) + (e.Canceled ? " canceled" : ""));
            }
            else
            {
                Log("layout", "no SwitchThumb");
            }
            _afterRelease = () => toggle.IsOn = false;

            var x = bounds.X + bounds.Width / 2;
            var y = bounds.Y + bounds.Height / 2;
            // Distance and press time apart: each distance is moved quickly, 10 ms a step, and the press is then held
            // until it has lasted the given time. Then the drags at a steady pace, for where position takes over.
            foreach (var ms in new[] { 400, 900, 1100, 1500 })
            {
                foreach (var d in new[] { 0, 4, 12, 20, 26 })
                {
                    Timed(knob, x, y, d, ms);
                }
            }

            foreach (var d in new[] { 27, 28, 29, 30 })
            {
                Timed(knob, x, y, d, 500);
            }

            // Closer to each limit: the distance between 16 and 20, the press time between 1000 and 1100 ms.
            foreach (var d in new[] { 16, 17, 18, 19 })
            {
                Timed(knob, x, y, d, 500);
            }

            foreach (var ms in new[] { 1000, 1025, 1050, 1075 })
            {
                Timed(knob, x, y, 0, ms);
            }

            await Ready();
        }

        /// <summary>
        /// The tilt (#22): a Button 300 x 120 view pixels, pressed and held for 600 ms at its centre, each corner and
        /// the middle of each edge, 12 pixels in, then pressed at the centre and dragged off it. Every frame, the log
        /// has the template root's projection and transform whenever they change, so the timeline of the press and
        /// of the release, and how the tilt follows the press point, can be read to the frame.
        /// </summary>
        private async Task Tilt(Grid host)
        {
            var button = await TiltButton(host, "tilt");
            if (button == null)
            {
                return;
            }

            var b = button.Bounds;
            const double inset = 12;
            var xs = new[] { b.X + inset, b.X + b.Width / 2, b.X + b.Width - inset };
            var ys = new[] { b.Y + inset, b.Y + b.Height / 2, b.Y + b.Height - inset };
            Hold(button.Element, xs[1], ys[1]);
            foreach (var y in ys)
            {
                foreach (var x in xs)
                {
                    if (x != xs[1] || y != ys[1])
                    {
                        Hold(button.Element, x, y);
                    }
                }
            }

            Touch(button.Element, xs[1], ys[1], 0, 200);
            await Ready();
        }

        /// <summary>
        /// The tilt probes' Button, 300 x 120 view pixels at 75, 200, alone on its page, watched as <see cref="WatchTilt"/>
        /// says, with its clicks logged. Null, with the run finished, when it has no template root.
        /// </summary>
        private async Task<Placed> TiltButton(Grid host, string content)
        {
            var button = new Button { Content = content, Width = 300, Height = 120, MinWidth = 0, MinHeight = 0, Margin = new Thickness(75, 200, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            await Page(host, button);
            var placed = await WatchTilt(button);
            if (placed != null)
            {
                button.Click += (s, e) => Log("click", "");
            }

            return placed;
        }

        /// <summary>
        /// The tilt's dependence on the press point and the button's size (#22): four buttons of different sizes on
        /// one page, each pressed and held still at a 5 x 5 grid of points (5%, 25%, 50%, 75% and 95% across and
        /// down). The log names the button on each pointer event and logs every button's projection and transform
        /// every frame, so each press's settled values and timing can be read per point. Laid out for the reference
        /// profile's 450 x 800 view; on a narrower one, C is the view's width less the margins, so that it fits (the
        /// 720p profile's 400 x 711.11 view takes the whole grid). A press that would still miss its button, on a
        /// shorter view, stops the run before it starts.
        /// </summary>
        private async Task TiltGrid(Grid host)
        {
            var sizes = new[]
            {
                new { Name = "A", W = 120.0, H = 60.0, X = 19.0, Y = 120.0 },
                new { Name = "B", W = 300.0, H = 120.0, X = 19.0, Y = 210.0 },
                new { Name = "C", W = Math.Min(412.0, Window.Current.Bounds.Width - 38), H = 80.0, X = 19.0, Y = 360.0 },
                new { Name = "D", W = 200.0, H = 200.0, X = 19.0, Y = 470.0 },
            };
            var buttons = sizes.Select(size => new Button { Name = size.Name, Content = "tilt " + size.Name, Width = size.W, Height = size.H, MinWidth = 0, MinHeight = 0, Margin = new Thickness(size.X, size.Y, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }).ToArray();
            await Page(host, buttons);

            var fractions = new[] { 0.05, 0.25, 0.5, 0.75, 0.95 };
            foreach (var button in buttons)
            {
                var placed = await WatchTilt(button);
                if (placed == null)
                {
                    return;
                }

                var b = placed.Bounds;
                foreach (var fy in fractions)
                {
                    foreach (var fx in fractions)
                    {
                        Hold(button, b.X + b.Width * fx, b.Y + b.Height * fy, 500, 1200);
                    }
                }
            }

            await Ready();
        }

        /// <summary>
        /// Puts the controls on a dark page in the host and waits until each has its template (see <see cref="Settle"/>),
        /// then reads the view the touches are planned in. Returns the page.
        /// </summary>
        private Task<Grid> Page(Grid host, params UIElement[] controls)
        {
            return Page(host, true, controls);
        }

        /// <summary>As above; <paramref name="settle"/> false for a page with no templated control, which Settle would wait 10 s for.</summary>
        private async Task<Grid> Page(Grid host, bool settle, params UIElement[] controls)
        {
            var page = new Grid { RequestedTheme = ElementTheme.Dark, Background = new SolidColorBrush(Colors.Black) };
            foreach (var control in controls)
            {
                page.Children.Add(control);
            }

            host.HorizontalAlignment = HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            host.Children.Add(page);
            if (settle)
            {
                await Settle(page);
            }
            else
            {
                await Task.Delay(500);
            }

            _view = Window.Current.Bounds;
            return page;
        }

        /// <summary>An element on the page and its bounds in the window.</summary>
        private sealed class Placed
        {
            public FrameworkElement Element;
            public Rect Bounds;
        }

        /// <summary>
        /// Logs a button's bounds, its pointer events and states, and its template root's projection and transform every
        /// frame, named by the button's Name if it has one. Returns the button and its bounds in the window, or null,
        /// with the run finished, when the button has no template root.
        /// </summary>
        private async Task<Placed> WatchTilt(Button button)
        {
            var tag = Tag(button);
            var root = VisualTreeHelper.GetChildrenCount(button) > 0 ? VisualTreeHelper.GetChild(button, 0) as FrameworkElement : null;
            if (root == null)
            {
                await Finish("button " + tag + "has no template root");
                return null;
            }

            var b = button.TransformToVisual(null).TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
            Log("layout", "button " + tag + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " root " + root.GetType().Name);
            Watch(button);
            WatchValue(("projection " + tag).TrimEnd(), raw => Raw(root.Projection, raw), () => Describe(root.Projection));
            WatchValue(("transform " + tag).TrimEnd(), raw => Raw(root.RenderTransform, raw), () => Describe(root.RenderTransform));
            return new Placed { Element = button, Bounds = b };
        }

        private Rect _view;
        private double _plannedMs;
        private int _stepCount;
        private readonly List<string> _misplaced = new List<string>();

        /// <summary>
        /// A gesture step at a point in view pixels, in window coordinates. A step outside the view, or, when a target is
        /// given, a press that wouldn't land on it, is kept in <see cref="_misplaced"/>, which stops the run in
        /// <see cref="StartTouches"/>.
        /// </summary>
        private void Step(string verb, double x, double y, UIElement target = null)
        {
            var n = ++_stepCount;
            if (x < 0 || y < 0 || x >= _view.Width || y >= _view.Height)
            {
                _misplaced.Add("step " + n + ", " + verb + " at " + Num(x) + ", " + Num(y) + ", is outside the " + Num(_view.Width) + " x " + Num(_view.Height) + " view");
            }
            else if (target != null && !VisualTreeHelper.FindElementsInHostCoordinates(new Point(x, y), target).Any())
            {
                var element = target as FrameworkElement;
                var name = element != null && element.Name.Length > 0 ? element.Name : target.GetType().Name;
                _misplaced.Add("step " + n + ", " + verb + " at " + Num(x) + ", " + Num(y) + ", misses " + name);
            }

            _gestures.AppendLine(verb + " " + Num(x) + " " + Num(y));
        }

        private void Wait(double ms)
        {
            _plannedMs += ms;
            _gestures.AppendLine("wait " + Num(ms));
        }

        /// <summary>
        /// Whether a tap near a Button's edge clicks (for #22): a Button 300 x 120 view pixels, tapped (150 ms) at the
        /// middle of each edge, 1, 2 and 3 px inside it, three times each. The log has each press's position in the
        /// button, as the phone got it, and each Click.
        /// </summary>
        private async Task TiltEdge(Grid host)
        {
            var button = await TiltButton(host, "edge");
            if (button == null)
            {
                return;
            }

            var b = button.Bounds;
            for (var round = 0; round < 3; round++)
            {
                foreach (var inset in new[] { 1.0, 2.0, 3.0 })
                {
                    Hold(button.Element, b.X + b.Width / 2, b.Y + inset, 150, 1000);
                    Hold(button.Element, b.X + b.Width / 2, b.Bottom - inset, 150, 1000);
                    Hold(button.Element, b.X + inset, b.Y + b.Height / 2, 150, 1000);
                    Hold(button.Element, b.Right - inset, b.Y + b.Height / 2, 150, 1000);
                }
            }

            await Ready();
        }

        /// <summary>A touch on the target held still at (x, y) for 600 ms.</summary>
        private void Hold(UIElement target, double x, double y)
        {
            Hold(target, x, y, 600, 2000);
        }

        /// <summary>A touch on the target held still at (x, y) for holdMs, then a pause of gapMs before the next.</summary>
        private void Hold(UIElement target, double x, double y, int holdMs, int gapMs)
        {
            _expected++;
            Step("down", x, y, target);
            Wait(holdMs);
            Step("up", x, y);
            Wait(gapMs);
        }

        private static string Describe(Projection projection)
        {
            var p = projection as PlaneProjection;
            if (p == null)
            {
                return projection == null ? "none" : projection.GetType().Name;
            }

            return "rot " + Num(p.RotationX) + " " + Num(p.RotationY) + " " + Num(p.RotationZ)
                + " centre " + Num(p.CenterOfRotationX) + " " + Num(p.CenterOfRotationY) + " " + Num(p.CenterOfRotationZ)
                + " local " + Num(p.LocalOffsetX) + " " + Num(p.LocalOffsetY) + " " + Num(p.LocalOffsetZ)
                + " global " + Num(p.GlobalOffsetX) + " " + Num(p.GlobalOffsetY) + " " + Num(p.GlobalOffsetZ);
        }

        private static string Describe(Transform transform)
        {
            var t = transform as CompositeTransform;
            if (t != null)
            {
                return "composite scale " + Num(t.ScaleX) + " " + Num(t.ScaleY) + " translate " + Num(t.TranslateX) + " " + Num(t.TranslateY) + " centre " + Num(t.CenterX) + " " + Num(t.CenterY);
            }

            var s = transform as ScaleTransform;
            if (s != null)
            {
                return "scale " + Num(s.ScaleX) + " " + Num(s.ScaleY) + " centre " + Num(s.CenterX) + " " + Num(s.CenterY);
            }

            var m = transform as MatrixTransform;
            if (m != null)
            {
                var v = m.Matrix;
                return "matrix " + Num(v.M11) + " " + Num(v.M12) + " " + Num(v.M21) + " " + Num(v.M22) + " " + Num(v.OffsetX) + " " + Num(v.OffsetY);
            }

            return transform == null ? "none" : transform.GetType().Name;
        }

        /// <summary>A touch on the target at (x, y) that moves right by dx, a view pixel every 10 ms, and lifts when it has lasted totalMs.</summary>
        private void Timed(UIElement target, double x, double y, double dx, int totalMs)
        {
            _expected++;
            Step("down", x, y, target);
            Wait(100);
            var steps = (int)Math.Ceiling(dx);
            for (var i = 1; i <= steps; i++)
            {
                Step("move", x + dx * i / steps, y);
                Wait(10);
            }

            Wait(Math.Max(0, totalMs - 100 - steps * 10));
            Step("up", x + dx, y);
            Wait(2000);
        }

        /// <summary>A touch on the target at (x, y) that moves by (dx, dy) one view pixel at a time, 30 ms apart, holds, and lifts.</summary>
        private void Touch(UIElement target, double x, double y, double dx, double dy)
        {
            _expected++;
            Step("down", x, y, target);
            Wait(150);
            var steps = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)));
            for (var i = 1; i <= steps; i++)
            {
                Step("move", x + dx * i / steps, y + dy * i / steps);
                Wait(30);
            }

            Wait(300);
            Step("up", x + dx, y + dy);
            Wait(2000);
        }

        /// <summary>Logs the pointer events and every visual state change of the control's template.</summary>
        private void Watch(Control control)
        {
            control.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((s, e) => Pointer("pressed", control, e)), true);
            control.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((s, e) => Pointer("moved", control, e)), true);
            control.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((s, e) => Released(control, e)), true);
            control.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((s, e) => Pointer("capturelost", control, e)), true);
            control.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler((s, e) => Pointer("canceled", control, e)), true);
            WatchStates(control);
        }

        /// <summary>Logs every visual state change of the control's template (without <see cref="Watch"/>'s pointer handlers, whose release count ends a touch probe).</summary>
        private void WatchStates(Control control)
        {
            var root = VisualTreeHelper.GetChildrenCount(control) > 0 ? VisualTreeHelper.GetChild(control, 0) as FrameworkElement : null;
            if (root == null)
            {
                return;
            }

            foreach (var group in VisualStateManager.GetVisualStateGroups(root))
            {
                var g = group;
                g.CurrentStateChanged += (s, e) => Log("state", Tag(control) + g.Name + " " + (e.OldState != null ? e.OldState.Name : "-") + " > " + (e.NewState != null ? e.NewState.Name : "-"));
            }
        }

        /// <summary>The control's Name and a space, for log lines on pages with several controls; empty if it has none.</summary>
        private static string Tag(FrameworkElement control) => control.Name.Length > 0 ? control.Name + " " : "";

        private void Pointer(string what, Control control, PointerRoutedEventArgs e)
        {
            var inWindow = e.GetCurrentPoint(null).Position;
            var inControl = e.GetCurrentPoint(control).Position;
            Log(what, Tag(control) + Num(inWindow.X) + " " + Num(inWindow.Y) + " in-control " + Num(inControl.X) + " " + Num(inControl.Y) + " " + e.Pointer.PointerDeviceType + (e.Handled ? " handled" : ""));
        }

        private async void Released(Control control, PointerRoutedEventArgs e)
        {
            Pointer("released", control, e);
            _released++;
            await Task.Delay(800);
            Log("settled", "touch " + _released);
            if (_afterRelease != null)
            {
                _afterRelease();
                await Task.Delay(200);
            }

            if (_released >= _expected)
            {
                await Finish("touches " + _released);
            }
        }

        private sealed class Watched
        {
            public string Name;
            public Action<List<double>> Read;
            public Func<string> Format;
            public List<double> Now = new List<double>();
            public List<double> Last;
            public string Logged;
        }

        private readonly List<Watched> _watched = new List<Watched>();

        /// <summary>
        /// Logs a value every frame, when it changes. One Rendering handler reads them all: each frame it reads the raw
        /// numbers (<paramref name="read"/> adds them to the list it is given), and only when they differ from the last
        /// frame's does it format the value, and log it if the text differs too.
        /// </summary>
        private void WatchValue(string name, Action<List<double>> read, Func<string> format)
        {
            if (_watched.Count == 0)
            {
                CompositionTarget.Rendering += OnFrame;
            }

            _watched.Add(new Watched { Name = name, Read = read, Format = format });
        }

        private void OnFrame(object sender, object e)
        {
            foreach (var w in _watched)
            {
                w.Now.Clear();
                w.Read(w.Now);
                if (w.Last != null && Same(w.Now, w.Last))
                {
                    continue;
                }

                var previous = w.Last ?? new List<double>();
                w.Last = w.Now;
                w.Now = previous;
                var text = w.Format();
                if (text != w.Logged)
                {
                    w.Logged = text;
                    Log("value", w.Name + " " + text);
                }
            }
        }

        private static bool Same(List<double> a, List<double> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The numbers <see cref="Describe(Projection)"/> formats: the type, then a PlaneProjection's values.</summary>
        private static void Raw(Projection projection, List<double> raw)
        {
            raw.Add(projection == null ? 0 : projection.GetType().GetHashCode());
            var p = projection as PlaneProjection;
            if (p != null)
            {
                raw.Add(p.RotationX); raw.Add(p.RotationY); raw.Add(p.RotationZ);
                raw.Add(p.CenterOfRotationX); raw.Add(p.CenterOfRotationY); raw.Add(p.CenterOfRotationZ);
                raw.Add(p.LocalOffsetX); raw.Add(p.LocalOffsetY); raw.Add(p.LocalOffsetZ);
                raw.Add(p.GlobalOffsetX); raw.Add(p.GlobalOffsetY); raw.Add(p.GlobalOffsetZ);
            }
        }

        /// <summary>The numbers <see cref="Describe(Transform)"/> formats: the type, then the transform's values.</summary>
        private static void Raw(Transform transform, List<double> raw)
        {
            raw.Add(transform == null ? 0 : transform.GetType().GetHashCode());
            var t = transform as CompositeTransform;
            var s = transform as ScaleTransform;
            var m = transform as MatrixTransform;
            if (t != null)
            {
                raw.Add(t.ScaleX); raw.Add(t.ScaleY); raw.Add(t.TranslateX); raw.Add(t.TranslateY); raw.Add(t.CenterX); raw.Add(t.CenterY);
            }
            else if (s != null)
            {
                raw.Add(s.ScaleX); raw.Add(s.ScaleY); raw.Add(s.CenterX); raw.Add(s.CenterY);
            }
            else if (m != null)
            {
                var v = m.Matrix;
                raw.Add(v.M11); raw.Add(v.M12); raw.Add(v.M21); raw.Add(v.M22); raw.Add(v.OffsetX); raw.Add(v.OffsetY);
            }
        }

        /// <summary>Starts the planned touches (see <see cref="StartTouches"/>) and waits for them to settle.</summary>
        private async Task Ready()
        {
            // A run whose touches never arrive still ends, with its log: after the touches' planned time and 3 minutes for
            // the VM to pull gestures.txt and start playing it; never before 5 minutes. vm\touch.ps1 plays each step at
            // its planned time from its start, so a step played late doesn't delay the ones after it. ready.txt gives the
            // timeout to vm\session.ps1, which waits that long for done.txt when touch.ps1 fails.
            var timeout = Math.Max(5 * 60000, _plannedMs + 3 * 60000);
            if (!await StartTouches("touches " + _expected, _expected, timeout))
            {
                return;
            }

            await Task.WhenAny(_done.Task, Task.Delay(TimeSpan.FromMilliseconds(timeout)));
            await Finish("timed out after " + _released + " of " + _expected + " touches");
        }

        /// <summary>
        /// Writes gestures.txt, starts the clock, logs the ready line and writes ready.txt: the touches expected and the
        /// milliseconds after which the app finishes whether they came or not. When a step is outside the view or misses
        /// its target, it writes gestures.txt and finishes, naming every such step, so that no touch is played, and
        /// returns false.
        /// </summary>
        private async Task<bool> StartTouches(string ready, int touches, double timeoutMs)
        {
            await Write("gestures.txt", "view " + Num(_view.Width) + " " + Num(_view.Height) + "\n" + _gestures);
            if (_misplaced.Count > 0)
            {
                await Finish("the probe's touches don't fit, " + _misplaced.Count + " step(s): " + string.Join("; ", _misplaced));
                return false;
            }

            var display = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            _clock.Start();
            Log("ready", ready + " raw-per-view " + Num(display.RawPixelsPerViewPixel));
            await Write("ready.txt", "touches " + touches + "\ntimeout " + Num(timeoutMs) + "\n");
            return true;
        }

        private bool _finished;
        private readonly TaskCompletionSource<bool> _done = new TaskCompletionSource<bool>();

        private async Task Finish(string summary)
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            CompositionTarget.Rendering -= OnFrame;
            _done.TrySetResult(true);
            await Write("probe.txt", _log.ToString());
            await Write("done.txt", summary);
        }

        private void Log(string what, string details)
        {
            _log.Append(_clock.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append('\t').Append(what).Append('\t').Append(details).Append('\n');
        }

        /// <summary>
        /// Waits until the page has laid out and every visible control placed on it has its template, up to 10 s, then
        /// 500 ms more. The controls are found at any depth under the page's panels and other elements, but not inside a
        /// control: the controls in its template are the template's.
        /// </summary>
        private static async Task Settle(Panel page)
        {
            for (var waited = 0; waited < 10000; waited += 100)
            {
                page.UpdateLayout();
                var controls = new List<Control>();
                CollectControls(page, controls);
                if (page.ActualWidth > 0 && controls.Count > 0 && controls.All(c => VisualTreeHelper.GetChildrenCount(c) > 0))
                {
                    break;
                }

                await Task.Delay(100);
            }

            await Task.Delay(500);
        }

        /// <summary>The visible controls under the element, outside any control's template.</summary>
        private static void CollectControls(DependencyObject element, List<Control> controls)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var visual = child as UIElement;
                if (visual != null && visual.Visibility != Visibility.Visible)
                {
                    continue;
                }

                var control = child as Control;
                if (control != null)
                {
                    controls.Add(control);
                }
                else
                {
                    CollectControls(child, controls);
                }
            }
        }

        private static FrameworkElement Find(DependencyObject root, string name)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var element = node as FrameworkElement;
                if (element != null && element.Name == name)
                {
                    return element;
                }

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return null;
        }

        private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private async Task Write(string file, string text)
        {
            var f = await _out.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(f, text);
        }
    }
}
