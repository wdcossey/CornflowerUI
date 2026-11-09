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
    internal sealed class Probe
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
                default:
                    await probe.Write("done.txt", "unknown probe " + name);
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
            var page = new Grid { RequestedTheme = ElementTheme.Dark, Background = new SolidColorBrush(Colors.Black) };
            page.Children.Add(toggle);
            host.HorizontalAlignment = HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            host.Children.Add(page);
            await Settle(page);

            var knob = Find(toggle, "SwitchKnob");
            var knobTransform = knob != null ? knob.RenderTransform as TranslateTransform : null;
            var thumb = Find(toggle, "SwitchThumb") as Windows.UI.Xaml.Controls.Primitives.Thumb;
            if (knob == null)
            {
                await Write("done.txt", "no SwitchKnob in the ToggleSwitch's template");
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
            var page = new Grid { RequestedTheme = ElementTheme.Dark, Background = new SolidColorBrush(Colors.Black) };
            page.Children.Add(button);
            host.HorizontalAlignment = HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            host.Children.Add(page);
            await Settle(page);

            var root = VisualTreeHelper.GetChildrenCount(button) > 0 ? VisualTreeHelper.GetChild(button, 0) as FrameworkElement : null;
            if (root == null)
            {
                await Write("done.txt", "the Button has no template root");
                return;
            }

            var b = button.TransformToVisual(null).TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
            Log("layout", "button " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " root " + root.GetType().Name);
            Watch(button);
            button.Click += (s, e) => Log("click", "");
            WatchValue("projection", () => Describe(root.Projection));
            WatchValue("transform", () => Describe(root.RenderTransform));

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

        /// <summary>A touch held still at (x, y) for 600 ms.</summary>
        private void Hold(double x, double y)
        {
            _expected++;
            _gestures.AppendLine("down " + Num(x) + " " + Num(y));
            _gestures.AppendLine("wait 600");
            _gestures.AppendLine("up " + Num(x) + " " + Num(y));
            _gestures.AppendLine("wait 2000");
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
            _gestures.AppendLine("down " + Num(x) + " " + Num(y));
            _gestures.AppendLine("wait 100");
            var steps = (int)Math.Ceiling(dx);
            for (var i = 1; i <= steps; i++)
            {
                _gestures.AppendLine("move " + Num(x + dx * i / steps) + " " + Num(y));
                _gestures.AppendLine("wait 10");
            }

            _gestures.AppendLine("wait " + Math.Max(0, totalMs - 100 - steps * 10));
            _gestures.AppendLine("up " + Num(x + dx) + " " + Num(y));
            _gestures.AppendLine("wait 2000");
        }

        /// <summary>A touch at (x, y) that moves by (dx, dy) one view pixel at a time, 30 ms apart, holds, and lifts.</summary>
        private void Touch(double x, double y, double dx, double dy)
        {
            _expected++;
            _gestures.AppendLine("down " + Num(x) + " " + Num(y));
            _gestures.AppendLine("wait 150");
            var steps = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)));
            for (var i = 1; i <= steps; i++)
            {
                _gestures.AppendLine("move " + Num(x + dx * i / steps) + " " + Num(y + dy * i / steps));
                _gestures.AppendLine("wait 30");
            }

            _gestures.AppendLine("wait 300");
            _gestures.AppendLine("up " + Num(x + dx) + " " + Num(y + dy));
            _gestures.AppendLine("wait 2000");
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
                        g.CurrentStateChanged += (s, e) => Log("state", g.Name + " " + (e.OldState != null ? e.OldState.Name : "-") + " > " + (e.NewState != null ? e.NewState.Name : "-"));
                    }
                }
            }
        }

        private void Pointer(string what, UIElement control, PointerRoutedEventArgs e)
        {
            var inWindow = e.GetCurrentPoint(null).Position;
            var inControl = e.GetCurrentPoint(control).Position;
            Log(what, Num(inWindow.X) + " " + Num(inWindow.Y) + " in-control " + Num(inControl.X) + " " + Num(inControl.Y) + " " + e.Pointer.PointerDeviceType + (e.Handled ? " handled" : ""));
        }

        private async void Released(UIElement control, PointerRoutedEventArgs e)
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

        /// <summary>Logs a value every frame, when it changes.</summary>
        private void WatchValue(string name, Func<string> read)
        {
            string last = null;
            EventHandler<object> onFrame = (s, e) =>
            {
                var v = read();
                if (v != last)
                {
                    last = v;
                    Log("value", name + " " + v);
                }
            };
            CompositionTarget.Rendering += onFrame;
        }

        private async Task Ready()
        {
            var display = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            var b = Window.Current.Bounds;
            await Write("gestures.txt", "view " + Num(b.Width) + " " + Num(b.Height) + "\n" + _gestures);
            _clock.Start();
            Log("ready", "touches " + _expected + " raw-per-view " + Num(display.RawPixelsPerViewPixel));
            await Write("ready.txt", "touches " + _expected);

            // A run whose touches never arrive still ends, with the log.
            await Task.WhenAny(_done.Task, Task.Delay(TimeSpan.FromMinutes(5)));
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
            _done.TrySetResult(true);
            await Write("probe.txt", _log.ToString());
            await Write("done.txt", summary);
        }

        private void Log(string what, string details)
        {
            _log.Append(_clock.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture)).Append('\t').Append(what).Append('\t').Append(details).Append('\n');
        }

        /// <summary>Waits until the page has laid out and every control on it has its template, up to 10 s, then 500 ms more.</summary>
        private static async Task Settle(FrameworkElement page)
        {
            for (var waited = 0; waited < 10000; waited += 100)
            {
                page.UpdateLayout();
                var control = Find<Control>(page);
                if (page.ActualWidth > 0 && control != null && VisualTreeHelper.GetChildrenCount(control) > 0)
                {
                    break;
                }

                await Task.Delay(100);
            }

            await Task.Delay(500);
        }

        private static T Find<T>(DependencyObject root) where T : class
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var found = child as T ?? Find<T>(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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
