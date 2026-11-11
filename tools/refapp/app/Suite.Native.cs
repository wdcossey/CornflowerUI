using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// The values the phone computes in native code, read through its public API (run.ps1 -Native): the display's scale
    /// and DPI; the system tray's occluded rectangle and opacity, and the view's visible bounds against the window's,
    /// with the tray shown and hidden, under both bounds modes, in portrait and both landscapes; and the
    /// TemplateSettings of ComboBox, ProgressBar, ProgressRing and ToggleSwitch, for the catalogue's specimens at rest
    /// and in every state their templates declare, and for ComboBoxes opened inline. Writes native.json, then
    /// done.txt. Captures nothing, and leaves the tray as the suite has it (hidden) before the TemplateSettings.
    ///
    /// Every reading is built in a writer of its own and added to the output only once it is whole, and every step
    /// that can throw records its error in <c>errors</c> and lets the run go on, so native.json and done.txt are always
    /// written, and the app is asked back into portrait whatever happened.
    /// </summary>
    internal sealed partial class Suite
    {
        /// <summary>The longest a tray, bounds-mode or rotation change is given to settle before it is read anyway.</summary>
        private const int TraySettleLimitMs = 3000;

        private static readonly string[] SettingsControls = { "ComboBox", "ProgressBar", "ProgressRing", "ToggleSwitch" };

        public async Task RunNativeAsync()
        {
            var started = await BeginAsync();
            _log.Visibility = Visibility.Collapsed;
            string display = null;
            Entry(null, "display", e => WriteDisplay(e, d =>
            {
                var di = DisplayInformation.GetForCurrentView();
                d.Prop("resolutionScaleName", di.ResolutionScale.ToString())
                    .Prop("nativeOrientation", di.NativeOrientation.ToString())
                    .Prop("currentOrientation", di.CurrentOrientation.ToString());
            }), text => display = text);

            var tray = new List<string>();
            var settings = new List<string>();
            var stageWidth = double.NaN;
            try
            {
                await TrayReadings(tray);
            }
            catch (Exception ex)
            {
                Error("tray", ex);
            }

            // Back to what the suite runs in: portrait, the visible bounds, the tray hidden. The stage is the window's
            // width, so the TemplateSettings are only read once the phone is back in portrait.
            var portrait = false;
            try
            {
                await Step("restore: bounds mode", () =>
                {
                    ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
                    return Task.FromResult(true);
                });
                await Step("restore: hide the tray", async () => await StatusBar.GetForCurrentView().HideAsync());
                portrait = await Rotate(DisplayOrientations.Portrait) || await Rotate(DisplayOrientations.Portrait);
                var window = Window.Current.Bounds;
                portrait = portrait && window.Height > window.Width;
                if (!portrait)
                {
                    _errors.Add(string.Format(CultureInfo.InvariantCulture, "restore: the phone isn't back in portrait ({0}, window {1} x {2}); TemplateSettings not read",
                        DisplayInformation.GetForCurrentView().CurrentOrientation, window.Width, window.Height));
                }
            }
            catch (Exception ex)
            {
                Error("restore", ex);
            }

            if (portrait)
            {
                _width = Window.Current.Bounds.Width;
                stageWidth = _width;
                try
                {
                    await SettingsReadings(settings);
                }
                catch (Exception ex)
                {
                    Error("templateSettings", ex);
                }
            }

            try
            {
                _host.Children.Clear();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
            }
            catch (Exception ex)
            {
                Error("cleanup", ex);
            }

            var j = new Json().Obj();
            j.Prop("tool", "Cornflower refapp native");
            j.Prop("started", started.ToString("o", CultureInfo.InvariantCulture));
            j.Key("display");
            if (display != null)
            {
                j.Raw(display);
            }
            else
            {
                j.Val((string)null);
            }

            WriteArray(j, "tray", tray);
            j.Prop("stageWidth", stageWidth);
            WriteArray(j, "templateSettings", settings);
            WriteErrors(j);
            j.Prop("finished", DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
            j.EndObj();
            await FinishAsync("native.json", j.ToString(), string.Format(CultureInfo.InvariantCulture, "native readings {0}, errors {1}", _count, _errors.Count));
        }

        /// <summary>
        /// The tray as the app launched, before any call to it; then, in each orientation and each bounds mode, shown and
        /// hidden. Each change is read once it has settled (<see cref="SettleTray"/>).
        /// </summary>
        private async Task TrayReadings(List<string> into)
        {
            await Write("progress.txt", "tray launch");
            var settled = await SettleTray(() => true);
            Entry(into, "tray launch", e => Tray(e, null, null, null, null, null, settled));
            foreach (var orientation in new[] { DisplayOrientations.Portrait, DisplayOrientations.Landscape, DisplayOrientations.LandscapeFlipped })
            {
                await Write("progress.txt", "tray " + orientation);
                var rotated = await Rotate(orientation);
                foreach (var mode in new[] { ApplicationViewBoundsMode.UseVisible, ApplicationViewBoundsMode.UseCoreWindow })
                {
                    bool? accepted = null;
                    await Step("tray " + orientation + " " + mode, () =>
                    {
                        accepted = ApplicationView.GetForCurrentView().SetDesiredBoundsMode(mode);
                        return Task.FromResult(true);
                    });
                    foreach (var shown in new[] { true, false })
                    {
                        var what = "tray " + orientation + " " + mode + (shown ? " shown" : " hidden");
                        var statusBar = StatusBar.GetForCurrentView();
                        if (!await Step(what, async () => await (shown ? statusBar.ShowAsync() : statusBar.HideAsync())))
                        {
                            continue;
                        }

                        settled = await SettleTray(() => (statusBar.OccludedRect.Height > 0) == shown);
                        Entry(into, what, e => Tray(e, orientation, shown, mode, accepted, rotated, settled));
                    }
                }
            }
        }

        private async Task SettingsReadings(List<string> into)
        {
            // The catalogue's ComboBox has 3 items; these open inline too (5 or fewer), with other selections.
            var specimens = Catalog.All().Where(s => SettingsControls.Contains(s.Name)).ToList();
            specimens.Add(OpenCombo(4, 0));
            specimens.Add(OpenCombo(5, 0));
            specimens.Add(OpenCombo(5, 2));
            specimens.Add(OpenCombo(5, 4));

            // The catalogue's ring stretches to its slot; the ring probe's two are its default size and 120 square.
            specimens.Add(new Specimen { Name = "ProgressRing", Variant = "default-size", Height = 120, Make = () => new ProgressRing { IsActive = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } });
            specimens.Add(new Specimen { Name = "ProgressRing", Variant = "120", Height = 140, Make = () => new ProgressRing { IsActive = true, Width = 120, Height = 120, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } });
            foreach (var s in specimens)
            {
                await Write("progress.txt", "templateSettings " + s.Name + " " + s.Variant);
                await Settings(into, s);
            }
        }

        /// <summary>A ComboBox as the popup catalogue opens it inline (5 items or fewer), with a chosen item selected.</summary>
        private static Specimen OpenCombo(int count, int selected)
        {
            return new Specimen
            {
                Name = "ComboBox",
                Variant = "items-" + count + (selected == 0 ? "" : "-selected-" + selected),
                Height = 400,
                States = false,
                Make = () =>
                {
                    var c = new ComboBox { Header = "combo box" };
                    for (var i = 1; i <= count; i++)
                    {
                        c.Items.Add("item " + i);
                    }

                    c.SelectedIndex = selected;
                    return c;
                },
            };
        }

        /// <summary>
        /// Asks for one orientation and waits up to 5 s for the display to report it, then for the layout to settle:
        /// the phone rotates an app to its only preferred orientation. Returns whether it did.
        /// </summary>
        private async Task<bool> Rotate(DisplayOrientations orientation)
        {
            var rotated = false;
            await Step("rotate to " + orientation, async () =>
            {
                var display = DisplayInformation.GetForCurrentView();
                DisplayInformation.AutoRotationPreferences = orientation;
                for (var waited = 0; waited < 5000 && display.CurrentOrientation != orientation; waited += 100)
                {
                    await Task.Delay(100);
                }

                rotated = display.CurrentOrientation == orientation;
            });
            await SettleTray(() => true);
            return rotated;
        }

        /// <summary>
        /// Waits, polling every 100 ms up to <see cref="TraySettleLimitMs"/>, until <paramref name="ready"/> holds and the
        /// window, visible bounds, tray and page content have read the same three times running. Returns the
        /// milliseconds waited, or -1 if it never settled (the reading is taken anyway, and says so).
        /// </summary>
        private static async Task<int> SettleTray(Func<bool> ready)
        {
            string last = null;
            var same = 0;
            for (var waited = 0; waited <= TraySettleLimitMs; waited += 100)
            {
                var now = LayoutSnapshot();
                same = now == last ? same + 1 : 0;
                last = now;
                if (same >= 2 && ready())
                {
                    return waited;
                }

                await Task.Delay(100);
            }

            return -1;
        }

        private static string LayoutSnapshot()
        {
            var content = PageContent();
            var at = content != null ? content.TransformToVisual(null).TransformPoint(new Point(0, 0)) : new Point();
            return string.Join(" ", Window.Current.Bounds, ApplicationView.GetForCurrentView().VisibleBounds,
                StatusBar.GetForCurrentView().OccludedRect, at, content != null ? content.ActualWidth : 0, content != null ? content.ActualHeight : 0);
        }

        private static Page CurrentPage() => (Window.Current.Content as Frame)?.Content as Page;

        private static FrameworkElement PageContent() => CurrentPage()?.Content as FrameworkElement;

        /// <summary>
        /// One tray reading. <paramref name="asked"/>, <paramref name="shown"/>, <paramref name="mode"/>,
        /// <paramref name="accepted"/> and <paramref name="rotated"/> are null for the reading at launch, which asked for
        /// nothing, and are then left out; <c>currentOrientation</c> is always what the display reports.
        /// </summary>
        private void Tray(Json j, DisplayOrientations? asked, bool? shown, ApplicationViewBoundsMode? mode, bool? accepted, bool? rotated, int settledMs)
        {
            var view = ApplicationView.GetForCurrentView();
            var statusBar = StatusBar.GetForCurrentView();
            j.Obj();
            j.Prop("orientation", asked == null ? "launch" : asked.Value.ToString());
            j.Prop("currentOrientation", DisplayInformation.GetForCurrentView().CurrentOrientation.ToString());
            if (rotated != null)
            {
                j.Prop("rotated", rotated.Value);
            }

            j.Prop("tray", shown == null ? "as launched" : shown.Value ? "shown" : "hidden");
            if (mode != null)
            {
                j.Prop("boundsModeAsked", mode.Value.ToString());
            }

            if (accepted != null)
            {
                j.Prop("boundsModeAccepted", accepted.Value);
            }

            j.Prop("boundsMode", view.DesiredBoundsMode.ToString());
            j.Prop("viewOrientation", view.Orientation.ToString());
            j.Prop("settledMs", settledMs);
            WriteRect(j, "windowBounds", Window.Current.Bounds);
            WriteRect(j, "coreWindowBounds", Window.Current.CoreWindow.Bounds);
            WriteRect(j, "visibleBounds", view.VisibleBounds);
            var content = Window.Current.Content as FrameworkElement;
            if (content != null)
            {
                j.Key("content").Arr().Val(content.ActualWidth).Val(content.ActualHeight).EndArr();

                // Where the window's content sits under the root of the visual tree, and what holds it: the tray's
                // overlap shows here if the phone moves or shrinks the content rather than the window.
                var origin = content.TransformToVisual(null).TransformPoint(new Point(0, 0));
                j.Key("contentOrigin").Arr().Val(origin.X).Val(origin.Y).EndArr();
                j.Key("contentParents").Arr();
                for (var parent = VisualTreeHelper.GetParent(content); parent != null; parent = VisualTreeHelper.GetParent(parent))
                {
                    var fe = parent as FrameworkElement;
                    j.Obj().Prop("type", parent.GetType().FullName);
                    if (fe != null)
                    {
                        WriteBounds(j, "bounds", fe);
                    }

                    j.EndObj();
                }

                j.EndArr();
                var stage = _host.TransformToVisual(null).TransformPoint(new Point(0, 0));
                j.Key("stageHostOrigin").Arr().Val(stage.X).Val(stage.Y).EndArr();
                var page = CurrentPage();
                if (page != null)
                {
                    WriteBounds(j, "page", page);
                    var root = page.Content as FrameworkElement;
                    if (root != null)
                    {
                        WriteBounds(j, "pageContent", root);
                    }
                }
            }

            WriteRect(j, "occludedRect", statusBar.OccludedRect);
            j.Prop("backgroundOpacity", statusBar.BackgroundOpacity);
            j.Prop("backgroundColor", statusBar.BackgroundColor.HasValue ? TreeDump.Colour(statusBar.BackgroundColor.Value) : null);
            j.Prop("foregroundColor", statusBar.ForegroundColor.HasValue ? TreeDump.Colour(statusBar.ForegroundColor.Value) : null);
            j.EndObj();
        }

        /// <summary>
        /// One specimen's TemplateSettings: at rest; for a ComboBox, opening (straight after IsDropDownOpen), open and
        /// closed again; for a ToggleSwitch, straight after toggling and once the slide is over; then, on a fresh
        /// instance each, in every state the template declares, forced as the suite forces it. A step that fails is
        /// recorded and the rest still run.
        /// </summary>
        private async Task Settings(List<string> into, Specimen s)
        {
            var name = "templateSettings " + s.Name + " " + s.Variant;
            FrameworkElement element = null;
            await Step(name + " rest", async () =>
            {
                element = (await Place(ElementTheme.Dark, s)).Element;
            });
            if (element != null)
            {
                Entry(into, name + " rest", e => Reading(e, s, "rest", null, element));
                var combo = element as ComboBox;
                if (combo != null)
                {
                    await Step(name + " open", async () =>
                    {
                        combo.IsDropDownOpen = true;
                        Entry(into, name + " opening", e => Reading(e, s, "opening", null, combo));
                        await Task.Delay(800);
                        Entry(into, name + " open", e => Reading(e, s, "open", null, combo));
                        combo.IsDropDownOpen = false;
                        await Task.Delay(800);
                        Entry(into, name + " closed again", e => Reading(e, s, "closed again", null, combo));
                    });
                }

                var toggle = element as ToggleSwitch;
                if (toggle != null)
                {
                    await Step(name + " toggle", async () =>
                    {
                        toggle.IsOn = !toggle.IsOn;
                        Entry(into, name + " toggled", e => Reading(e, s, "toggled", null, toggle));
                        await Task.Delay(800);
                        Entry(into, name + " toggled, settled", e => Reading(e, s, "toggled, settled", null, toggle));
                    });
                }
            }

            var states = new List<KeyValuePair<string, string>>();
            if (s.States)
            {
                await Step(name + " states", async () => states = await StatesOf(s));
            }

            foreach (var st in states)
            {
                var state = st.Key + "." + st.Value;
                await Step(name + " " + state, async () =>
                {
                    var control = (Control)(await Place(ElementTheme.Dark, s)).Element;
                    var applied = VisualStateManager.GoToState(control, st.Value, false);
                    await Task.Delay(StateSettleMs);
                    Entry(into, name + " " + state, e => Reading(e, s, state, applied, control));
                });
            }
        }

        private static void Reading(Json j, Specimen s, string step, bool? stateApplied, FrameworkElement element)
        {
            j.Obj();
            j.Prop("specimen", s.Name);
            j.Prop("variant", s.Variant);
            j.Prop("step", step);
            if (stateApplied != null)
            {
                j.Prop("stateApplied", stateApplied.Value);
            }

            j.Key("size").Arr().Val(element.ActualWidth).Val(element.ActualHeight).EndArr();
            var combo = element as ComboBox;
            if (combo != null)
            {
                var t = combo.TemplateSettings;
                j.Prop("isDropDownOpen", combo.IsDropDownOpen);
                j.Prop("selectedIndex", combo.SelectedIndex);
                j.Prop("items", combo.Items.Count);
                j.Key("settings").Obj()
                    .Prop("DropDownClosedHeight", t.DropDownClosedHeight)
                    .Prop("DropDownOffset", t.DropDownOffset)
                    .Prop("DropDownOpenedHeight", t.DropDownOpenedHeight)
                    .Prop("SelectedItemDirection", t.SelectedItemDirection.ToString())
                    .EndObj();
            }

            var bar = element as ProgressBar;
            if (bar != null)
            {
                var t = bar.TemplateSettings;
                j.Prop("isIndeterminate", bar.IsIndeterminate);
                j.Prop("value", bar.Value);
                j.Key("settings").Obj()
                    .Prop("ContainerAnimationEndPosition", t.ContainerAnimationEndPosition)
                    .Prop("ContainerAnimationStartPosition", t.ContainerAnimationStartPosition)
                    .Prop("EllipseAnimationEndPosition", t.EllipseAnimationEndPosition)
                    .Prop("EllipseAnimationWellPosition", t.EllipseAnimationWellPosition)
                    .Prop("EllipseDiameter", t.EllipseDiameter)
                    .Prop("EllipseOffset", t.EllipseOffset)
                    .Prop("IndicatorLengthDelta", t.IndicatorLengthDelta)
                    .EndObj();
            }

            var ring = element as ProgressRing;
            if (ring != null)
            {
                var t = ring.TemplateSettings;
                var o = t.EllipseOffset;
                j.Prop("isActive", ring.IsActive);
                j.Key("settings").Obj()
                    .Prop("EllipseDiameter", t.EllipseDiameter)
                    .Key("EllipseOffset").Arr().Val(o.Left).Val(o.Top).Val(o.Right).Val(o.Bottom).EndArr()
                    .Prop("MaxSideLength", t.MaxSideLength)
                    .EndObj();
            }

            var toggle = element as ToggleSwitch;
            if (toggle != null)
            {
                var t = toggle.TemplateSettings;
                j.Prop("isOn", toggle.IsOn);
                j.Key("settings").Obj()
                    .Prop("CurtainCurrentToOffOffset", t.CurtainCurrentToOffOffset)
                    .Prop("CurtainCurrentToOnOffset", t.CurtainCurrentToOnOffset)
                    .Prop("CurtainOffToOnOffset", t.CurtainOffToOnOffset)
                    .Prop("CurtainOnToOffOffset", t.CurtainOnToOffOffset)
                    .Prop("KnobCurrentToOffOffset", t.KnobCurrentToOffOffset)
                    .Prop("KnobCurrentToOnOffset", t.KnobCurrentToOnOffset)
                    .Prop("KnobOffToOnOffset", t.KnobOffToOnOffset)
                    .Prop("KnobOnToOffOffset", t.KnobOnToOffOffset)
                    .EndObj();
            }

            j.EndObj();
        }

        /// <summary>
        /// Builds one reading in a writer of its own and, only if that finished, adds it to <paramref name="into"/> (or
        /// hands it to <paramref name="done"/>). A getter that throws leaves no half-written object behind: it is recorded
        /// in <c>errors</c> instead.
        /// </summary>
        private void Entry(List<string> into, string what, Action<Json> build, Action<string> done = null)
        {
            var e = new Json();
            try
            {
                build(e);
            }
            catch (Exception ex)
            {
                Error(what, ex);
                return;
            }

            _count++;
            if (into != null)
            {
                into.Add(e.ToString());
            }

            if (done != null)
            {
                done(e.ToString());
            }
        }

        /// <summary>Runs one step; if it throws, records the error and returns false, so the run goes on.</summary>
        private async Task<bool> Step(string what, Func<Task> act)
        {
            try
            {
                await act();
                return true;
            }
            catch (Exception ex)
            {
                Error(what, ex);
                return false;
            }
        }

        private void Error(string what, Exception ex)
        {
            _errors.Add(what + ": " + ex.GetType().Name + ": " + ex.Message);
        }

        private static void WriteArray(Json j, string key, List<string> items)
        {
            j.Key(key).Arr();
            foreach (var item in items)
            {
                j.Raw(item);
            }

            j.EndArr();
        }

        private static void WriteBounds(Json j, string key, FrameworkElement fe)
        {
            var at = fe.TransformToVisual(null).TransformPoint(new Point(0, 0));
            j.Key(key).Arr().Val(at.X).Val(at.Y).Val(fe.ActualWidth).Val(fe.ActualHeight).EndArr();
        }

        private static void WriteRect(Json j, string key, Rect r)
        {
            j.Key(key).Arr().Val(r.X).Val(r.Y).Val(r.Width).Val(r.Height).EndArr();
        }
    }
}
