using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace RefApp
{
    /// <summary>
    /// The motion probes (#24): the theme's animations, read in the app every frame. No touch drives them but one tap,
    /// which starts the sequence, so that a screen recording (run.ps1 -Record) and the log start together. Every frame,
    /// the log has each watched tree's animated values whenever they change: for every element down to a depth, its
    /// opacity under 1, a render transform or projection that isn't the identity, and its clip, under a path of child indexes, type
    /// and name. Open popups are watched the same way, each from its child, with the popup's offset and the child's
    /// bounds in the window.
    /// </summary>
    internal sealed partial class Probe
    {
        private const int TreeDepth = 10;
        private const int TreeBudget = 400;
        private readonly List<KeyValuePair<string, FrameworkElement>> _trees = new List<KeyValuePair<string, FrameworkElement>>();
        private Dictionary<string, string> _treeSeen = new Dictionary<string, string>();
        private Dictionary<string, string> _treeNow = new Dictionary<string, string>();
        private int _treeVisited;
        private readonly HashSet<string> _budgetLogged = new HashSet<string>();
        private readonly HashSet<string> _treeCut = new HashSet<string>();
        private readonly Dictionary<Popup, int> _popupIds = new Dictionary<Popup, int>();

        /// <summary>
        /// The ToggleSwitch's slide, the pickers' and flyouts' open and close, and the inline ComboBox: each opened from
        /// code, four times, held 1.2 s, then closed and waited out; the switch is turned on and off ten times. The log's "step" lines say what each is.
        /// </summary>
        private async Task Motion(Grid host)
        {
            var toggle = new ToggleSwitch { Header = "probe", IsOn = false, Margin = new Thickness(19, 40, 19, 0), VerticalAlignment = VerticalAlignment.Top };
            var opener = new Button { Content = "open", Margin = new Thickness(19, 170, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var combo6 = Combo("combo6", 6, 260);
            var combo5 = Combo("combo5", 5, 360);
            var page = await Page(host, toggle, opener, combo6, combo5);
            WatchStates(toggle);
            toggle.Toggled += (s, e) => Log("toggled", "IsOn " + toggle.IsOn);
            WatchTree("toggle", toggle);
            WatchTree("combo5", combo5);
            WatchPopups();

            Func<Task> sequence = async () =>
            {
                // Repeated, as a recording at about 40 frames a second samples each run at other moments.
                for (var i = 0; i < 10; i++)
                {
                    Log("step", "toggle on");
                    toggle.IsOn = true;
                    await Task.Delay(1000);
                    Log("step", "toggle off");
                    toggle.IsOn = false;
                    await Task.Delay(1000);
                }

                foreach (var p in MotionPopups(opener, combo6, combo5))
                {
                    for (var i = 0; i < 4; i++)
                    {
                        await OpenAndClose(p.Key, p.Value);
                    }
                }
            };
            await RunAfterTap(page, sequence);
        }

        /// <summary>What <see cref="Motion"/> opens: a name, and an action that opens it and returns the closer.</summary>
        private IEnumerable<KeyValuePair<string, Func<Action>>> MotionPopups(Button opener, ComboBox combo6, ComboBox combo5)
        {
            var list = new List<KeyValuePair<string, Func<Action>>>();
            Action<string, Func<Action>> add = (n, f) => list.Add(new KeyValuePair<string, Func<Action>>(n, f));
            add("Flyout top", () =>
            {
                var f = Events("Flyout top", new Flyout { Placement = FlyoutPlacementMode.Top, Content = new TextBlock { Text = "flyout content" } });
                f.ShowAt(opener);
                return f.Hide;
            });
            add("Flyout full", () =>
            {
                var f = Events("Flyout full", new Flyout { Placement = FlyoutPlacementMode.Full, Content = new TextBlock { Text = "flyout content" } });
                f.ShowAt(opener);
                return f.Hide;
            });
            add("MenuFlyout", () =>
            {
                var m = new MenuFlyout();
                m.Items.Add(new MenuFlyoutItem { Text = "first item" });
                m.Items.Add(new MenuFlyoutItem { Text = "second item" });
                m.Items.Add(new MenuFlyoutItem { Text = "third item" });
                Events("MenuFlyout", m).ShowAt(opener);
                return m.Hide;
            });
            add("ListPickerFlyout", () =>
            {
                var p = new ListPickerFlyout { ItemsSource = new[] { "one", "two", "three", "four", "five" } };
                p.SelectedIndex = 1;
                Events("ListPickerFlyout", p);
                var op = p.ShowAtAsync(opener);
                return p.Hide;
            });
            add("DatePickerFlyout", () =>
            {
                var d = new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) };
                Events("DatePickerFlyout", d);
                var op = d.ShowAtAsync(opener);
                return d.Hide;
            });
            add("TimePickerFlyout", () =>
            {
                var t = new TimePickerFlyout { Time = new TimeSpan(12, 30, 0) };
                Events("TimePickerFlyout", t);
                var op = t.ShowAtAsync(opener);
                return t.Hide;
            });
            add("ComboBox 6 items", () =>
            {
                combo6.IsDropDownOpen = true;
                return () => combo6.IsDropDownOpen = false;
            });
            add("ComboBox 5 items", () =>
            {
                combo5.IsDropDownOpen = true;
                return () => combo5.IsDropDownOpen = false;
            });
            add("ContentDialog", () =>
            {
                var d = new ContentDialog { Title = "content dialog", Content = "the dialog's body", PrimaryButtonText = "ok", SecondaryButtonText = "cancel" };
                d.Opened += (s, e) => Log("event", "ContentDialog Opened");
                d.Closed += (s, e) => Log("event", "ContentDialog Closed");
                var op = d.ShowAsync();
                return d.Hide;
            });
            return list;
        }

        private T Events<T>(string name, T flyout) where T : FlyoutBase
        {
            flyout.Opening += (s, e) => Log("event", name + " Opening");
            flyout.Opened += (s, e) => Log("event", name + " Opened");
            flyout.Closed += (s, e) => Log("event", name + " Closed");
            return flyout;
        }

        private ComboBox Combo(string name, int count, double top)
        {
            var c = new ComboBox { Name = name, Header = "combo box, " + count + " items", Width = 300, Margin = new Thickness(19, top, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            for (var i = 1; i <= count; i++)
            {
                c.Items.Add("item " + i);
            }

            c.SelectedIndex = 0;
            c.DropDownOpened += (s, e) => Log("event", name + " DropDownOpened");
            c.DropDownClosed += (s, e) => Log("event", name + " DropDownClosed");
            return c;
        }

        /// <summary>Opens, holds 1.2 s, closes, and waits for every popup to go (never closing one by force), then 800 ms more.</summary>
        private async Task OpenAndClose(string name, Func<Action> open)
        {
            Log("step", name + " open");
            Action close;
            try
            {
                close = open();
            }
            catch (Exception ex)
            {
                Log("error", name + " open: " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            await Task.Delay(1200);
            Log("step", name + " close");
            try
            {
                close();
            }
            catch (Exception ex)
            {
                Log("error", name + " close: " + ex.GetType().Name + ": " + ex.Message);
            }

            for (var waited = 0; waited < 3000 && VisualTreeHelper.GetOpenPopups(Window.Current).Count > 0; waited += 50)
            {
                await Task.Delay(50);
            }

            Log("step", name + " closed, popups open " + VisualTreeHelper.GetOpenPopups(Window.Current).Count);
            await Task.Delay(800);
        }

        /// <summary>
        /// The ProgressRing's Active animation (owed before #28: the suite's forced Active state captures no dots, as the
        /// dots only show while the storyboard runs). An active ring at its default size and one 120 px wide, watched for
        /// 6 s from the tap: every dot's opacity and transform each frame.
        /// </summary>
        private async Task Ring(Grid host)
        {
            var ring = new ProgressRing { Name = "ring", IsActive = true, Margin = new Thickness(19, 120, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var large = new ProgressRing { Name = "large", IsActive = true, Width = 120, Height = 120, Margin = new Thickness(19, 300, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var page = await Page(host, ring, large);
            WatchStates(ring);
            WatchStates(large);
            WatchTree("ring", ring);
            WatchTree("large", large);
            await RunAfterTap(page, () => Task.Delay(6000));
        }

        /// <summary>
        /// Calibration for the recordings of <see cref="Pages"/>: a full-window panel drawn like <see cref="ProbePage"/>
        /// (a white sheet inset 19, 60) is given known PlaneProjections, each held 400 ms, so the recording shows how a
        /// projection's angle, centre and depth come out on the screen. The log's "step" lines give each one.
        /// </summary>
        private async Task Projections(Grid host)
        {
            var sheet = new Border { Margin = new Thickness(19, 60, 19, 60), Background = new SolidColorBrush(Colors.White) };
            var panel = new Grid { Background = new SolidColorBrush(Colors.Black), Children = { sheet } };
            var projection = new PlaneProjection();
            panel.Projection = projection;
            var page = await Page(host, panel);

            Func<Task> sequence = async () =>
            {
                var steps = new List<double[]>();
                foreach (var centre in new[] { 0.0, 0.5, -0.25 })
                {
                    foreach (var angle in new[] { 10.0, 30.0, 60.0, -10.0, -30.0, -60.0 })
                    {
                        steps.Add(new[] { angle, centre, 0 });
                    }
                }

                steps.Add(new[] { 0, 0, -100.0 });
                steps.Add(new[] { 0, 0, 100.0 });
                steps.Add(new[] { 30, 0, -100.0 });
                foreach (var s in steps)
                {
                    projection.RotationY = s[0];
                    projection.CenterOfRotationX = s[1];
                    projection.GlobalOffsetZ = s[2];
                    Log("step", "rotationY " + Num(s[0]) + " centreX " + Num(s[1]) + " globalZ " + Num(s[2]));
                    await Task.Delay(400);
                    projection.RotationY = 0;
                    projection.CenterOfRotationX = 0.5;
                    projection.GlobalOffsetZ = 0;
                    Log("step", "rest");
                    await Task.Delay(200);
                }
            };
            await RunAfterTap(page, sequence);
        }

        /// <summary>
        /// Page transitions: a Frame filling the page navigates between two pages, forward and back, with a
        /// NavigationThemeTransition (what the root frame has) and the default transition info, then with each
        /// NavigationTransitionInfo the phone has. Page A is white, page B the accent blue, each on black with a 19 px
        /// margin, so a recording shows each page's outline. The theme transition doesn't show in the app's values.
        /// </summary>
        private async Task Pages(Grid host)
        {
            var root = Window.Current.Content as Frame;
            var rootTransitions = root != null ? root.ContentTransitions : null;
            Log("layout", "root frame transitions " + (rootTransitions == null ? "none" : string.Join(",", rootTransitions.Select(t => t.GetType().Name))));
            var frame = new Frame { Name = "frame" };
            frame.ContentTransitions = new TransitionCollection { new NavigationThemeTransition() };
            var page = await Page(host, frame);
            frame.Navigated += (s, e) => Log("event", "Navigated " + e.NavigationMode + " " + e.Parameter + (e.NavigationTransitionInfo != null ? " " + e.NavigationTransitionInfo.GetType().Name : ""));
            frame.Navigate(typeof(ProbePage), "A");
            await Task.Delay(1000);
            WatchTree("frame", frame);

            Func<Task> sequence = async () =>
            {
                // The default eight times, as a recording at about 40 frames a second samples each run at other moments;
                // then each NavigationTransitionInfo, the slide and continuum four times each.
                var infos = new List<Func<NavigationTransitionInfo>>();
                infos.AddRange(Enumerable.Repeat<Func<NavigationTransitionInfo>>(() => null, 8));
                infos.Add(() => new CommonNavigationTransitionInfo());
                infos.AddRange(Enumerable.Repeat<Func<NavigationTransitionInfo>>(() => new SlideNavigationTransitionInfo(), 4));
                infos.AddRange(Enumerable.Repeat<Func<NavigationTransitionInfo>>(() => new ContinuumNavigationTransitionInfo(), 4));
                foreach (var make in infos)
                {
                    var info = make();
                    var label = info == null ? "default" : info.GetType().Name;
                    Log("step", "forward " + label);
                    if (info == null)
                    {
                        frame.Navigate(typeof(ProbePage), "B");
                    }
                    else
                    {
                        frame.Navigate(typeof(ProbePage), "B", info);
                    }

                    await Task.Delay(1300);
                    Log("step", "back " + label);
                    frame.GoBack();
                    await Task.Delay(1300);
                }
            };
            await RunAfterTap(page, sequence);
        }

        /// <summary>
        /// Asks for one tap near the bottom left of the page, on the page's own background, and runs the sequence when
        /// it lands, then finishes. It starts its tap with StartTouches rather than through Ready(), whose watchdog counts
        /// touches: this one gives up 3 minutes after ready if the tap never comes, and otherwise 6 minutes after the tap.
        /// ready.txt gives vm\session.ps1 the 9 minutes of the two; it waits that long for done.txt, and a minute more.
        /// </summary>
        private async Task RunAfterTap(Grid page, Func<Task> sequence)
        {
            var started = false;
            page.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(async (s, e) =>
            {
                if (started)
                {
                    return;
                }

                started = true;
                Log("go", "tap at " + Num(e.GetCurrentPoint(null).Position.X) + " " + Num(e.GetCurrentPoint(null).Position.Y));
                await Task.Delay(1000);
                try
                {
                    await sequence();
                }
                catch (Exception ex)
                {
                    Log("error", ex.GetType().Name + ": " + ex.Message);
                }

                await Task.Delay(1000);
                await Finish("sequence done");
            }), true);

            var view = Window.Current.Bounds;
            Step("down", 30, view.Height - 30, page);
            Wait(80);
            Step("up", 30, view.Height - 30);
            Wait(1000);
            if (!await StartTouches("one tap starts the sequence;", 1, TimeSpan.FromMinutes(3 + 6).TotalMilliseconds))
            {
                return;
            }

            for (var waited = 0; !started && waited < 180; waited++)
            {
                await Task.Delay(1000);
            }

            if (!started)
            {
                await Finish("timed out: the tap never came");
                return;
            }

            await Task.WhenAny(_done.Task, Task.Delay(TimeSpan.FromMinutes(6)));
            await Finish("timed out after 6 minutes of the sequence");
        }

        /// <summary>Watches a tree every frame from this element down (see the class summary).</summary>
        private void WatchTree(string label, FrameworkElement root)
        {
            if (_trees.Count == 0)
            {
                CompositionTarget.Rendering += OnTreeFrame;
            }

            _trees.Add(new KeyValuePair<string, FrameworkElement>(label, root));
        }

        /// <summary>Watches every open popup every frame, as a tree from its child.</summary>
        private void WatchPopups()
        {
            WatchTree("popups", null);
        }

        private void OnTreeFrame(object sender, object e)
        {
            var now = _treeNow;
            now.Clear();
            _treeCut.Clear();
            foreach (var tree in _trees)
            {
                if (tree.Value != null)
                {
                    _treeVisited = 0;
                    Walk(tree.Key, tree.Value, 0, now, true);
                    continue;
                }

                var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                for (var k = 0; k < popups.Count; k++)
                {
                    var popup = popups[k];
                    // Numbered in the order first seen, so a popup keeps its label when another one closes.
                    int id;
                    if (!_popupIds.TryGetValue(popup, out id))
                    {
                        id = _popupIds.Count;
                        _popupIds[popup] = id;
                    }

                    var label = "popup" + id;
                    var child = popup.Child as FrameworkElement;
                    var transitions = popup.ChildTransitions != null ? string.Join(",", popup.ChildTransitions.Select(t => t.GetType().Name)) : "";
                    now[label] = "offset " + Num(popup.HorizontalOffset) + " " + Num(popup.VerticalOffset) + (transitions.Length > 0 ? " transitions " + transitions : "") + " child " + (child != null ? child.GetType().Name : "none");
                    if (child != null)
                    {
                        _treeVisited = 0;
                        Walk(label, child, 0, now, true);
                    }
                }
            }

            foreach (var kv in now)
            {
                string old;
                if (!_treeSeen.TryGetValue(kv.Key, out old) || old != kv.Value)
                {
                    Log("tree", kv.Key + " | " + kv.Value);
                }
            }

            foreach (var kv in _treeSeen)
            {
                if (now.ContainsKey(kv.Key))
                {
                    continue;
                }

                // A tree cut short by the budget this frame wasn't read past the cut: carry its readings, don't call them rest.
                if (_treeCut.Contains(TreeOf(kv.Key)))
                {
                    now[kv.Key] = kv.Value;
                    continue;
                }

                Log("tree", kv.Key + " | rest");
            }

            _treeNow = _treeSeen;
            _treeSeen = now;
        }

        private void Walk(string path, FrameworkElement e, int depth, Dictionary<string, string> now, bool isRoot)
        {
            if (e.Visibility == Visibility.Collapsed)
            {
                return;
            }

            if (++_treeVisited > TreeBudget)
            {
                var tree = TreeOf(path);
                _treeCut.Add(tree);
                if (_budgetLogged.Add(tree))
                {
                    Log("tree", "budget: more than " + TreeBudget + " elements under " + tree + "; the rest of that tree isn't read, and its readings past the cut carry over (logged once a tree)");
                }

                return;
            }

            var d = Animated(e);
            if (isRoot)
            {
                // A popup's child can be in the list before it's in the live tree, or after it's left, on its first and last frames.
                try
                {
                    var r = e.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, e.ActualWidth, e.ActualHeight));
                    d = "at " + Num(r.X) + " " + Num(r.Y) + " " + Num(r.Width) + " " + Num(r.Height) + (d.Length > 0 ? " " + d : "");
                }
                catch (ArgumentException)
                {
                    d = "not in the tree" + (d.Length > 0 ? " " + d : "");
                }
            }

            // The key is built only for an element with something to log: this runs on every element, every frame.
            if (d.Length > 0)
            {
                now[path + " " + e.GetType().Name + (e.Name.Length > 0 ? "#" + e.Name : "")] = d;
            }

            if (depth >= TreeDepth)
            {
                return;
            }

            var n = VisualTreeHelper.GetChildrenCount(e);
            for (var i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(e, i) as FrameworkElement;
                if (child != null)
                {
                    Walk(path + "/" + i, child, depth + 1, now, false);
                }
            }
        }

        /// <summary>The tree a path or key starts with: its label, before the first '/' or space.</summary>
        private static string TreeOf(string pathOrKey)
        {
            var end = pathOrKey.IndexOfAny(new[] { '/', ' ' });
            return end < 0 ? pathOrKey : pathOrKey.Substring(0, end);
        }

        /// <summary>The element's opacity under 1, a render transform or projection that isn't the identity, and its clip if it has one; empty if none.</summary>
        private static string Animated(UIElement e)
        {
            var sb = new StringBuilder();
            if (e.Opacity != 1)
            {
                sb.Append(" opacity ").Append(Num(e.Opacity));
            }

            var t = AnyTransform(e.RenderTransform);
            if (t.Length > 0)
            {
                sb.Append(" transform ").Append(t);
                if (e.RenderTransformOrigin.X != 0 || e.RenderTransformOrigin.Y != 0)
                {
                    sb.Append(" origin ").Append(Num(e.RenderTransformOrigin.X)).Append(' ').Append(Num(e.RenderTransformOrigin.Y));
                }
            }

            var pp = e.Projection as PlaneProjection;
            var restingPlane = pp != null && pp.RotationX == 0 && pp.RotationY == 0 && pp.RotationZ == 0
                && pp.LocalOffsetX == 0 && pp.LocalOffsetY == 0 && pp.LocalOffsetZ == 0
                && pp.GlobalOffsetX == 0 && pp.GlobalOffsetY == 0 && pp.GlobalOffsetZ == 0;
            if (e.Projection != null && !restingPlane)
            {
                sb.Append(" projection ").Append(Describe(e.Projection));
            }

            var clip = e.Clip;
            if (clip != null)
            {
                var r = clip.Rect;
                sb.Append(" clip ").Append(Num(r.X)).Append(' ').Append(Num(r.Y)).Append(' ').Append(Num(r.Width)).Append(' ').Append(Num(r.Height));
                var ct = AnyTransform(clip.Transform);
                if (ct.Length > 0)
                {
                    sb.Append(" by ").Append(ct);
                }
            }

            return sb.Length > 0 ? sb.ToString(1, sb.Length - 1) : "";
        }

        /// <summary>A transform's values, or empty for none or the identity.</summary>
        private static string AnyTransform(Transform transform)
        {
            if (transform == null)
            {
                return "";
            }

            var m = transform as MatrixTransform;
            if (m != null)
            {
                return m.Matrix.IsIdentity ? "" : Describe(m);
            }

            var tt = transform as TranslateTransform;
            if (tt != null)
            {
                return tt.X == 0 && tt.Y == 0 ? "" : "translate " + Num(tt.X) + " " + Num(tt.Y);
            }

            var rt = transform as RotateTransform;
            if (rt != null)
            {
                return rt.Angle == 0 ? "" : "rotate " + Num(rt.Angle) + " centre " + Num(rt.CenterX) + " " + Num(rt.CenterY);
            }

            var st = transform as ScaleTransform;
            if (st != null)
            {
                return st.ScaleX == 1 && st.ScaleY == 1 ? "" : Describe(st);
            }

            var ct = transform as CompositeTransform;
            if (ct != null)
            {
                var rest = ct.ScaleX == 1 && ct.ScaleY == 1 && ct.TranslateX == 0 && ct.TranslateY == 0 && ct.Rotation == 0 && ct.SkewX == 0 && ct.SkewY == 0;
                return rest ? "" : Describe(ct) + (ct.Rotation != 0 ? " rotation " + Num(ct.Rotation) : "") + (ct.SkewX != 0 || ct.SkewY != 0 ? " skew " + Num(ct.SkewX) + " " + Num(ct.SkewY) : "");
            }

            var g = transform as TransformGroup;
            if (g != null)
            {
                var parts = g.Children.Select(AnyTransform).Where(p => p.Length > 0).ToList();
                return parts.Count == 0 ? "" : "group [" + string.Join("; ", parts) + "]";
            }

            return transform.GetType().Name;
        }
    }
}
