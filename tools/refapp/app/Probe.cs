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
    /// writes <c>done.txt</c> once the last touch has been released and has settled. run.ps1 -Probe picks one.
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
            WatchValue("knobX", () => knobTransform != null ? Num(knobTransform.X) : "none");
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
                    Timed(x, y, d, ms);
                }
            }

            foreach (var d in new[] { 27, 28, 29, 30 })
            {
                Timed(x, y, d, 500);
            }

            // Closer to each limit: the distance between 16 and 20, the press time between 1000 and 1100 ms.
            foreach (var d in new[] { 16, 17, 18, 19 })
            {
                Timed(x, y, d, 500);
            }

            foreach (var ms in new[] { 1000, 1025, 1050, 1075 })
            {
                Timed(x, y, 0, ms);
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
            var button = new Button { Content = "tilt", Width = 300, Height = 120, Margin = new Thickness(75, 200, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            await Page(host, button);
            var watched = await WatchTilt(button);
            if (watched == null)
            {
                return;
            }

            var b = watched.Value;

            button.Click += (s, e) => Log("click", "");

            const double inset = 12;
            var xs = new[] { b.X + inset, b.X + b.Width / 2, b.X + b.Width - inset };
            var ys = new[] { b.Y + inset, b.Y + b.Height / 2, b.Y + b.Height - inset };
            Hold(xs[1], ys[1]);
            foreach (var y in ys)
            {
                foreach (var x in xs)
                {
                    if (x != xs[1] || y != ys[1])
                    {
                        Hold(x, y);
                    }
                }
            }

            Touch(xs[1], ys[1], 0, 200);
            await Ready();
        }

        /// <summary>
        /// The tilt's dependence on the press point and the button's size (#22): four buttons of different sizes on
        /// one page, each pressed and held still at a 5 x 5 grid of points (5%, 25%, 50%, 75% and 95% across and
        /// down). The log names the button on each pointer event and logs every button's projection and transform
        /// every frame, so each press's settled values and timing can be read per point. Laid out for the reference
        /// profile's 450 x 800 view; on a narrower one, C is the view's width less the margins, so that it fits.
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

            // The layout is the reference profile's, a 450 x 800 view: on a smaller one Ready stops the run, as presses would miss.
            var fractions = new[] { 0.05, 0.25, 0.5, 0.75, 0.95 };
            foreach (var button in buttons)
            {
                var watched = await WatchTilt(button);
                if (watched == null)
                {
                    return;
                }

                var b = watched.Value;

                foreach (var fy in fractions)
                {
                    foreach (var fx in fractions)
                    {
                        Hold(b.X + b.Width * fx, b.Y + b.Height * fy, 500, 1200);
                    }
                }
            }

            await Ready();
        }

        /// <summary>Puts the controls on a dark page in the host, and waits until each has its template.</summary>
        private static async Task Page(Grid host, params UIElement[] controls)
        {
            var page = new Grid { RequestedTheme = ElementTheme.Dark, Background = new SolidColorBrush(Colors.Black) };
            foreach (var control in controls)
            {
                page.Children.Add(control);
            }

            host.HorizontalAlignment = HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            host.Children.Add(page);
            await Settle(page);
        }

        /// <summary>
        /// Logs a button's bounds, its pointer events and states, and its template root's projection and transform every
        /// frame, named by the button's Name if it has one. Returns the bounds in the window, or null, with the run
        /// finished, when the button has no template root.
        /// </summary>
        private async Task<Rect?> WatchTilt(Button button)
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
            WatchValue(("projection " + tag).TrimEnd(), () => Describe(root.Projection));
            WatchValue(("transform " + tag).TrimEnd(), () => Describe(root.RenderTransform));
            return b;
        }

        private double _plannedMs;
        private int _steps;
        private string _outside;

        /// <summary>A gesture step at a point in view pixels, in window coordinates. The first one outside the view is kept, and stops the run in <see cref="Ready"/>.</summary>
        private void Step(string verb, double x, double y)
        {
            var b = Window.Current.Bounds;
            if (_outside == null && (x < 0 || y < 0 || x > b.Width || y > b.Height))
            {
                _outside = verb + " at " + Num(x) + ", " + Num(y) + " is outside the " + Num(b.Width) + " x " + Num(b.Height) + " view";
            }

            _steps++;
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
            var button = new Button { Content = "edge", Width = 300, Height = 120, MinWidth = 0, MinHeight = 0, Margin = new Thickness(75, 200, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            await Page(host, button);
            var watched = await WatchTilt(button);
            if (watched == null)
            {
                return;
            }

            var b = watched.Value;
            button.Click += (s, e) => Log("click", "");
            for (var round = 0; round < 3; round++)
            {
                foreach (var inset in new[] { 1.0, 2.0, 3.0 })
                {
                    Hold(b.X + b.Width / 2, b.Y + inset, 150, 1000);
                    Hold(b.X + b.Width / 2, b.Bottom - inset, 150, 1000);
                    Hold(b.X + inset, b.Y + b.Height / 2, 150, 1000);
                    Hold(b.Right - inset, b.Y + b.Height / 2, 150, 1000);
                }
            }

            await Ready();
        }

        /// <summary>A touch held still at (x, y) for 600 ms.</summary>
        private void Hold(double x, double y)
        {
            Hold(x, y, 600, 2000);
        }

        /// <summary>A touch held still at (x, y) for holdMs, then a pause of gapMs before the next.</summary>
        private void Hold(double x, double y, int holdMs, int gapMs)
        {
            _expected++;
            Step("down", x, y);
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

        /// <summary>A touch at (x, y) that moves right by dx, a view pixel every 10 ms, and lifts when it has lasted totalMs.</summary>
        private void Timed(double x, double y, double dx, int totalMs)
        {
            _expected++;
            Step("down", x, y);
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

        /// <summary>A touch at (x, y) that moves by (dx, dy) one view pixel at a time, 30 ms apart, holds, and lifts.</summary>
        private void Touch(double x, double y, double dx, double dy)
        {
            _expected++;
            Step("down", x, y);
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
            if (VisualTreeHelper.GetChildrenCount(control) > 0)
            {
                var root = VisualTreeHelper.GetChild(control, 0) as FrameworkElement;
                if (root != null)
                {
                    foreach (var group in VisualStateManager.GetVisualStateGroups(root))
                    {
                        var g = group;
                        g.CurrentStateChanged += (s, e) => Log("state", Tag(control) + g.Name + " " + (e.OldState != null ? e.OldState.Name : "-") + " > " + (e.NewState != null ? e.NewState.Name : "-"));
                    }
                }
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
            public Func<string> Read;
            public string Last;
        }

        private readonly List<Watched> _watched = new List<Watched>();

        /// <summary>Logs a value every frame, when it changes. One Rendering handler reads them all.</summary>
        private void WatchValue(string name, Func<string> read)
        {
            if (_watched.Count == 0)
            {
                CompositionTarget.Rendering += OnFrame;
            }

            _watched.Add(new Watched { Name = name, Read = read });
        }

        private void OnFrame(object sender, object e)
        {
            foreach (var w in _watched)
            {
                var v = w.Read();
                if (v != w.Last)
                {
                    w.Last = v;
                    Log("value", w.Name + " " + v);
                }
            }
        }

        private async Task Ready()
        {
            if (_outside != null)
            {
                await Finish("the probe doesn't fit this view: " + _outside);
                return;
            }

            var display = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            var b = Window.Current.Bounds;
            await Write("gestures.txt", "view " + Num(b.Width) + " " + Num(b.Height) + "\n" + _gestures);
            _clock.Start();
            Log("ready", "touches " + _expected + " raw-per-view " + Num(display.RawPixelsPerViewPixel));
            await Write("ready.txt", "touches " + _expected);

            // A run whose touches never arrive still ends, with the log: after the touches' planned waits, 50 ms a step for
            // touch.ps1 to play it, and 3 minutes for the VM to pull gestures.txt and start; never before 5 minutes.
            var timeout = Math.Max(5 * 60000, _plannedMs + 50 * _steps + 3 * 60000);
            await Task.WhenAny(_done.Task, Task.Delay(TimeSpan.FromMilliseconds(timeout)));
            await Finish("timed out after " + _released + " of " + _expected + " touches");
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

        /// <summary>Waits until the page has laid out and every control placed on it (its direct children) has its template, up to 10 s, then 500 ms more.</summary>
        private static async Task Settle(Panel page)
        {
            for (var waited = 0; waited < 10000; waited += 100)
            {
                page.UpdateLayout();
                var controls = page.Children.OfType<Control>().ToList();
                if (page.ActualWidth > 0 && controls.Count > 0 && controls.All(c => VisualTreeHelper.GetChildrenCount(c) > 0))
                {
                    break;
                }

                await Task.Delay(100);
            }

            await Task.Delay(500);
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
