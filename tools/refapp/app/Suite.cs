using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace RefApp
{
    /// <summary>
    /// Runs the catalogue: for each theme, each specimen, at rest and in every visual state its template
    /// declares, renders a stage to a PNG and dumps the tree to JSON in the app's local folder. Writes
    /// index.json and, last, done.txt, which the capture script polls for.
    ///
    /// States are forced with VisualStateManager.GoToState on a fresh instance each time, so a capture shows
    /// what the template draws in that state, not the motion into it. Motion comes from screen recordings.
    /// </summary>
    internal sealed partial class Suite
    {
        /// <summary>The phone's page margin, in logical pixels, used to place specimens as a page would.</summary>
        public const double PageMargin = 19;

        private const int StateSettleMs = 450;
        private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";

        private readonly Grid _host;
        private readonly TextBlock _log;
        private readonly List<string> _errors = new List<string>();
        private readonly Json _captures = new Json();
        private StorageFolder _out;
        private double _width;
        private int _count;
        private int _total;
        private int _holdMs;

        public Suite(Grid host, TextBlock log)
        {
            _host = host;
            _log = log;
        }

        public async Task RunAsync()
        {
            var started = await BeginAsync();
            _width = Window.Current.Bounds.Width;
            var options = await Options.LoadAsync();
            _holdMs = options.HoldMs;
            if (_holdMs > 0)
            {
                // The screen is grabbed: nothing but the stage on it.
                _log.Visibility = Visibility.Collapsed;
            }
            var specimens = options.PopupsOnly ? new List<Specimen>() : Catalog.All().Where(s => options.Matches(s.Name)).ToList();
            var popups = options.Popups ? PopupCatalog.All().Where(p => options.Matches(p.Name)).ToList() : new List<PopupSpecimen>();
            var themes = new[] { ElementTheme.Dark, ElementTheme.Light };

            // A system popup (a MessageDialog) can't be closed, and stays over every capture after it: one runs only when
            // -Only picks exactly one, alone, and then once, dark. Picked with others, the run captures nothing and says
            // why; matched with another system popup, or with no -Only, they are left out.
            var system = popups.Where(p => p.System).ToList();
            if (system.Count == 1 && !string.IsNullOrEmpty(options.Only))
            {
                if (popups.Count > 1 || specimens.Count > 0)
                {
                    _errors.Add("-Only picks the system popup " + system[0].Name + " with " + (popups.Count - 1 + specimens.Count) + " other(s); it can't be closed, so it runs alone: nothing captured");
                    popups.Clear();
                    specimens.Clear();
                }
                else
                {
                    themes = new[] { ElementTheme.Dark };
                }
            }
            else
            {
                popups.RemoveAll(p => p.System);
            }

            // Work out each specimen's states once: they come from the template and don't depend on the theme.
            var states = new Dictionary<Specimen, List<KeyValuePair<string, string>>>();
            foreach (var s in specimens)
            {
                states[s] = s.States ? await StatesOf(s) : new List<KeyValuePair<string, string>>();
            }

            // A popup gives its stage and one capture per open popup; two is the usual count.
            _total = themes.Length * (specimens.Sum(s => 1 + states[s].Count) + popups.Sum(p => p.System ? 1 : 2));

            _captures.Arr();
            foreach (var theme in themes)
            {
                foreach (var s in specimens)
                {
                    await Capture(theme, s, null, null);
                    foreach (var st in states[s])
                    {
                        await Capture(theme, s, st.Key, st.Value);
                    }
                }

                foreach (var p in popups)
                {
                    // A popup that never closes would stall the run; give each one 30 s.
                    var capture = CapturePopup(theme, p);
                    if (await Task.WhenAny(capture, Task.Delay(30000 + _holdMs)) != capture)
                    {
                        _errors.Add(Id(theme, new Specimen { Name = p.Name, Variant = p.Variant }, "popup") + ": timed out after 30 s");
                    }
                }
            }

            _captures.EndArr();
            _host.Children.Clear();

            var index = new Json().Obj();
            index.Prop("tool", "Cornflower refapp");
            index.Prop("started", started.ToString("o", CultureInfo.InvariantCulture));
            index.Prop("finished", DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
            await WriteEnvironment(index);
            index.Prop("pageMargin", PageMargin);
            index.Prop("stateSettleMs", StateSettleMs);
            index.Prop("only", options.Only);
            index.Prop("specimens", specimens.Count);
            index.Prop("popupSpecimens", popups.Count);
            index.Prop("captureCount", _count);
            WriteErrors(index);
            index.Key("captures");
            var text = index.ToString() + _captures.ToString() + "}";
            await FinishAsync("index.json", text, string.Format(CultureInfo.InvariantCulture, "captures {0} errors {1}", _count, _errors.Count));
            _log.Text = string.Format(CultureInfo.InvariantCulture, "done: {0} captures, {1} errors", _count, _errors.Count);
        }

        private async Task<List<KeyValuePair<string, string>>> StatesOf(Specimen s)
        {
            var result = new List<KeyValuePair<string, string>>();
            try
            {
                var stage = await Place(ElementTheme.Dark, s);
                var root = TemplateRoot(stage.Element);
                if (root != null)
                {
                    foreach (var group in VisualStateManager.GetVisualStateGroups(root))
                    {
                        foreach (var state in group.States)
                        {
                            result.Add(new KeyValuePair<string, string>(group.Name, state.Name));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errors.Add(Id(ElementTheme.Dark, s, "states") + ": " + ex.GetType().Name + ": " + ex.Message);
            }

            return result;
        }

        private async Task Capture(ElementTheme theme, Specimen s, string group, string state)
        {
            var name = Id(theme, s, group == null ? "base" : group + "." + state);
            _count++;
            _log.Text = string.Format(CultureInfo.InvariantCulture, "{0}/{1} {2}", _count, _total, name);
            await Write("progress.txt", name);
            try
            {
                var stage = await Place(theme, s);
                var forced = true;
                if (state != null)
                {
                    var control = stage.Element as Control;
                    forced = control != null && VisualStateManager.GoToState(control, state, false);
                    await Task.Delay(StateSettleMs);
                }

                var rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(stage.Outer);
                var pixels = (await rtb.GetPixelsAsync()).ToArray();
                var png = await _out.CreateFileAsync(name + ".png", CreationCollisionOption.ReplaceExisting);
                using (var stream = await png.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, 96, 96, pixels);
                    await encoder.FlushAsync();
                }

                var j = new Json().Obj();
                j.Prop("file", name + ".png");
                j.Prop("theme", theme.ToString());
                j.Prop("specimen", s.Name);
                j.Prop("variant", s.Variant);
                j.Prop("group", group);
                j.Prop("state", state);
                j.Prop("stateApplied", forced);
                j.Key("pixels").Arr().Val(rtb.PixelWidth).Val(rtb.PixelHeight).EndArr();
                j.Key("stage").Arr().Val(_width).Val(s.Height).EndArr();
                TreeDump.Brush(j, "stageBackground", stage.Background.Background);
                TreeDump.Bounds(j, stage.Element, stage.Outer, "elementInStage");
                j.Key("tree");
                TreeDump.Write(j, stage.Element, stage.Element);
                j.EndObj();
                await Write(name + ".json", j.ToString());

                _captures.Obj()
                    .Prop("file", name + ".png")
                    .Prop("theme", theme.ToString())
                    .Prop("specimen", s.Name)
                    .Prop("variant", s.Variant)
                    .Prop("group", group)
                    .Prop("state", state)
                    .Prop("stateApplied", forced)
                    .EndObj();
            }
            catch (Exception ex)
            {
                _errors.Add(name + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private sealed class Stage
        {
            public Grid Outer;
            public Panel Background;
            public FrameworkElement Element;
        }

        /// <summary>
        /// A stage the width of the window and the specimen's height, in the requested theme, with the page's
        /// own background drawn under it. RenderTargetBitmap sizes itself to what is drawn, so the explicit size
        /// and the opaque background are what make every capture the full stage.
        /// </summary>
        private async Task<Stage> Place(ElementTheme theme, Specimen s)
        {
            var outer = new Grid { Width = _width, Height = s.Height, RequestedTheme = theme };
            var background = (Panel)XamlReader.Load("<Grid " + Ns + " Background=\"{ThemeResource ApplicationPageBackgroundThemeBrush}\" />");
            outer.Children.Add(background);
            var slot = new Grid
            {
                Margin = s.FullBleed ? new Thickness(0) : new Thickness(PageMargin, 12, PageMargin, 0),
            };
            outer.Children.Add(slot);

            var element = s.Make();
            var loaded = Loaded(element);
            slot.Children.Add(element);
            _host.Children.Clear();
            _host.Children.Add(outer);
            await Task.WhenAny(loaded, Task.Delay(3000));
            outer.UpdateLayout();
            await Task.Delay(120);
            return new Stage { Outer = outer, Background = background, Element = element };
        }

        private static FrameworkElement TemplateRoot(FrameworkElement element)
        {
            return element is Control && VisualTreeHelper.GetChildrenCount(element) > 0
                ? VisualTreeHelper.GetChild(element, 0) as FrameworkElement
                : null;
        }

        private static Task Loaded(FrameworkElement element)
        {
            var tcs = new TaskCompletionSource<bool>();
            RoutedEventHandler handler = null;
            handler = (sender, e) =>
            {
                element.Loaded -= handler;
                tcs.TrySetResult(true);
            };
            element.Loaded += handler;
            return tcs.Task;
        }

        private static string Id(ElementTheme theme, Specimen s, string state) =>
            (theme == ElementTheme.Light ? "light" : "dark") + "__" + s.Name + "__" + s.Variant + "__" + state;

        private async Task Write(string file, string text)
        {
            var f = await _out.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(f, text);
        }

        /// <summary>Empties the app's local folder, where a run writes everything, and returns when the run started.</summary>
        private async Task<DateTimeOffset> BeginAsync()
        {
            var started = DateTimeOffset.Now;
            _out = ApplicationData.Current.LocalFolder;
            foreach (var old in await _out.GetFilesAsync())
            {
                await old.DeleteAsync();
            }

            return started;
        }

        private void WriteErrors(Json j)
        {
            j.Key("errors").Arr();
            foreach (var e in _errors)
            {
                j.Val(e);
            }

            j.EndArr();
        }

        /// <summary>Writes the run's results, then done.txt, which vm\session.ps1 waits for and quotes as the app's summary.</summary>
        private async Task FinishAsync(string file, string text, string done)
        {
            await Write(file, text);
            await Write("done.txt", done);
        }

        /// <summary>
        /// The display, as index.json has it (run.ps1 and the tokens tools read it, so its shape stays); <paramref name="more"/>
        /// adds keys after these, inside the same object.
        /// </summary>
        private static void WriteDisplay(Json j, Action<Json> more = null)
        {
            var di = DisplayInformation.GetForCurrentView();
            var bounds = Window.Current.Bounds;
            j.Obj()
                .Prop("resolutionScale", (int)di.ResolutionScale)
                .Prop("rawPixelsPerViewPixel", di.RawPixelsPerViewPixel)
                .Prop("logicalDpi", di.LogicalDpi)
                .Prop("rawDpiX", di.RawDpiX)
                .Prop("rawDpiY", di.RawDpiY)
                .Key("windowBounds").Arr().Val(bounds.Width).Val(bounds.Height).EndArr();
            if (more != null)
            {
                more(j);
            }

            j.EndObj();
        }

        private async Task WriteEnvironment(Json j)
        {
            WriteDisplay(j.Key("display"));
            j.Prop("appTheme", Application.Current.RequestedTheme.ToString());

            // Theme resources resolve against the theme of the tree they're in, so each probe is mounted first.
            var keys = new[] { "ApplicationPageBackgroundThemeBrush", "PhoneAccentBrush", "PhoneForegroundBrush", "PhoneBackgroundBrush" };
            j.Key("resources").Obj();
            foreach (var theme in new[] { ElementTheme.Dark, ElementTheme.Light })
            {
                j.Key(theme == ElementTheme.Light ? "light" : "dark").Obj();
                var probe = new StackPanel { RequestedTheme = theme };
                var borders = new Dictionary<string, Border>();
                foreach (var key in keys)
                {
                    try
                    {
                        var b = (Border)XamlReader.Load("<Border " + Ns + " Width=\"1\" Height=\"1\" Background=\"{ThemeResource " + key + "}\" />");
                        borders[key] = b;
                        probe.Children.Add(b);
                    }
                    catch (Exception ex)
                    {
                        j.Prop(key, "error: " + ex.Message);
                    }
                }

                _host.Children.Clear();
                _host.Children.Add(probe);
                await Task.Delay(200);
                foreach (var pair in borders)
                {
                    TreeDump.Brush(j, pair.Key, pair.Value.Background);
                }

                _host.Children.Clear();
                j.EndObj();
            }

            j.EndObj();

            try
            {
                var device = new EasClientDeviceInformation();
                j.Key("device").Obj()
                    .Prop("operatingSystem", device.OperatingSystem)
                    .Prop("friendlyName", device.FriendlyName)
                    .Prop("manufacturer", device.SystemManufacturer)
                    .Prop("productName", device.SystemProductName)
                    .Prop("sku", device.SystemSku)
                    .EndObj();
            }
            catch (Exception ex)
            {
                j.Prop("device", "error: " + ex.Message);
            }
        }
    }
}
