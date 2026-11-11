using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
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
    /// </summary>
    internal sealed partial class Suite
    {
        private const int TraySettleMs = 1000;

        private static readonly string[] SettingsControls = { "ComboBox", "ProgressBar", "ProgressRing", "ToggleSwitch" };

        public async Task RunNativeAsync()
        {
            var started = DateTimeOffset.Now;
            _out = ApplicationData.Current.LocalFolder;
            foreach (var old in await _out.GetFilesAsync())
            {
                await old.DeleteAsync();
            }

            _log.Visibility = Visibility.Collapsed;
            var j = new Json().Obj();
            j.Prop("tool", "Cornflower refapp native");
            j.Prop("started", started.ToString("o", CultureInfo.InvariantCulture));
            Display(j.Key("display"));

            // The tray as the app launched, before any call to it; then each orientation, each bounds mode, shown and hidden.
            j.Key("tray").Arr();
            await Write("progress.txt", "tray launch");
            await Task.Delay(TraySettleMs);
            Tray(j, "launch", null);
            var view = ApplicationView.GetForCurrentView();
            var statusBar = StatusBar.GetForCurrentView();
            foreach (var orientation in new[] { DisplayOrientations.Portrait, DisplayOrientations.Landscape, DisplayOrientations.LandscapeFlipped })
            {
                await Write("progress.txt", "tray " + orientation);
                var rotated = await Rotate(orientation);
                foreach (var mode in new[] { ApplicationViewBoundsMode.UseVisible, ApplicationViewBoundsMode.UseCoreWindow })
                {
                    var accepted = view.SetDesiredBoundsMode(mode);
                    foreach (var shown in new[] { true, false })
                    {
                        if (shown)
                        {
                            await statusBar.ShowAsync();
                        }
                        else
                        {
                            await statusBar.HideAsync();
                        }

                        await Task.Delay(TraySettleMs);
                        Tray(j, orientation.ToString(), shown, mode, accepted, rotated);
                    }
                }
            }

            j.EndArr();

            // Back to what the suite runs in: portrait, the visible bounds, the tray hidden.
            view.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            await statusBar.HideAsync();
            await Rotate(DisplayOrientations.Portrait);
            await Task.Delay(TraySettleMs);
            _width = Window.Current.Bounds.Width;
            j.Prop("stageWidth", _width);

            j.Key("templateSettings").Arr();
            var specimens = Catalog.All().Where(s => SettingsControls.Contains(s.Name)).ToList();
            foreach (var count in new[] { 3, 4, 5 })
            {
                specimens.Add(OpenCombo(count, 0));
            }

            specimens.Add(OpenCombo(5, 2));
            specimens.Add(OpenCombo(5, 4));

            // The catalogue's ring stretches to its slot; the ring probe's two are its default size and 120 square.
            specimens.Add(new Specimen { Name = "ProgressRing", Variant = "default-size", Height = 120, Make = () => new ProgressRing { IsActive = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } });
            specimens.Add(new Specimen { Name = "ProgressRing", Variant = "120", Height = 140, Make = () => new ProgressRing { IsActive = true, Width = 120, Height = 120, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } });
            foreach (var s in specimens)
            {
                await Write("progress.txt", "templateSettings " + s.Name + " " + s.Variant);
                await Settings(j, s);
            }

            j.EndArr();
            _host.Children.Clear();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;

            j.Key("errors").Arr();
            foreach (var e in _errors)
            {
                j.Val(e);
            }

            j.EndArr();
            j.Prop("finished", DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
            j.EndObj();
            await Write("native.json", j.ToString());
            await Write("done.txt", string.Format(CultureInfo.InvariantCulture, "native readings {0}, errors {1}", _count, _errors.Count));
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
        /// Asks for one orientation and waits up to 5 s for the window to take it. Returns whether the display reports
        /// it: the phone rotates an app to its only preferred orientation.
        /// </summary>
        private static async Task<bool> Rotate(DisplayOrientations orientation)
        {
            var display = DisplayInformation.GetForCurrentView();
            DisplayInformation.AutoRotationPreferences = orientation;
            for (var waited = 0; waited < 5000 && display.CurrentOrientation != orientation; waited += 100)
            {
                await Task.Delay(100);
            }

            await Task.Delay(TraySettleMs);
            return display.CurrentOrientation == orientation;
        }

        private static void Display(Json j)
        {
            var di = DisplayInformation.GetForCurrentView();
            j.Obj()
                .Prop("rawPixelsPerViewPixel", di.RawPixelsPerViewPixel)
                .Prop("rawDpiX", di.RawDpiX)
                .Prop("rawDpiY", di.RawDpiY)
                .Prop("logicalDpi", di.LogicalDpi)
                .Prop("resolutionScale", (int)di.ResolutionScale)
                .Prop("resolutionScaleName", di.ResolutionScale.ToString())
                .Prop("nativeOrientation", di.NativeOrientation.ToString())
                .Prop("currentOrientation", di.CurrentOrientation.ToString());
            WriteRect(j, "windowBounds", Window.Current.Bounds);
            j.EndObj();
        }

        private void Tray(Json j, string orientation, bool? shown, ApplicationViewBoundsMode? mode = null, bool accepted = true, bool rotated = true)
        {
            _count++;
            var view = ApplicationView.GetForCurrentView();
            var statusBar = StatusBar.GetForCurrentView();
            var content = Window.Current.Content as FrameworkElement;
            j.Obj();
            j.Prop("orientation", orientation);
            j.Prop("rotated", rotated);
            j.Prop("tray", shown == null ? "as launched" : shown.Value ? "shown" : "hidden");
            j.Prop("boundsModeAsked", mode == null ? null : mode.Value.ToString());
            j.Prop("boundsModeAccepted", accepted);
            j.Prop("boundsMode", view.DesiredBoundsMode.ToString());
            j.Prop("viewOrientation", view.Orientation.ToString());
            j.Key("display");
            Display(j);
            WriteRect(j, "windowBounds", Window.Current.Bounds);
            WriteRect(j, "coreWindowBounds", Window.Current.CoreWindow.Bounds);
            WriteRect(j, "visibleBounds", view.VisibleBounds);
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
                        var at = fe.TransformToVisual(null).TransformPoint(new Point(0, 0));
                        j.Key("bounds").Arr().Val(at.X).Val(at.Y).Val(fe.ActualWidth).Val(fe.ActualHeight).EndArr();
                    }

                    j.EndObj();
                }

                j.EndArr();
                var stage = _host.TransformToVisual(null).TransformPoint(new Point(0, 0));
                j.Key("stageHostOrigin").Arr().Val(stage.X).Val(stage.Y).EndArr();
                var frame = content as Frame;
                var page = frame != null ? frame.Content as FrameworkElement : null;
                if (page != null)
                {
                    var at = page.TransformToVisual(null).TransformPoint(new Point(0, 0));
                    j.Key("page").Arr().Val(at.X).Val(at.Y).Val(page.ActualWidth).Val(page.ActualHeight).EndArr();
                    var root = ((Page)page).Content as FrameworkElement;
                    if (root != null)
                    {
                        at = root.TransformToVisual(null).TransformPoint(new Point(0, 0));
                        j.Key("pageContent").Arr().Val(at.X).Val(at.Y).Val(root.ActualWidth).Val(root.ActualHeight).EndArr();
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
        /// instance each, in every state the template declares, forced as the suite forces it.
        /// </summary>
        private async Task Settings(Json j, Specimen s)
        {
            try
            {
                var stage = await Place(ElementTheme.Dark, s);
                var element = stage.Element;
                Reading(j, s, "rest", null, element);
                var combo = element as ComboBox;
                if (combo != null)
                {
                    combo.IsDropDownOpen = true;
                    Reading(j, s, "opening", null, element);
                    await Task.Delay(800);
                    Reading(j, s, "open", null, element);
                    combo.IsDropDownOpen = false;
                    await Task.Delay(800);
                    Reading(j, s, "closed again", null, element);
                }

                var toggle = element as ToggleSwitch;
                if (toggle != null)
                {
                    toggle.IsOn = !toggle.IsOn;
                    Reading(j, s, "toggled", null, element);
                    await Task.Delay(800);
                    Reading(j, s, "toggled, settled", null, element);
                }

                foreach (var st in s.States ? await StatesOf(s) : new List<KeyValuePair<string, string>>())
                {
                    stage = await Place(ElementTheme.Dark, s);
                    var control = (Control)stage.Element;
                    var applied = VisualStateManager.GoToState(control, st.Value, false);
                    await Task.Delay(StateSettleMs);
                    Reading(j, s, st.Key + "." + st.Value, applied, control);
                }
            }
            catch (Exception ex)
            {
                _errors.Add("templateSettings " + s.Name + " " + s.Variant + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void Reading(Json j, Specimen s, string step, bool? stateApplied, FrameworkElement element)
        {
            _count++;
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

        private static void WriteRect(Json j, string key, Rect r)
        {
            j.Key(key).Arr().Val(r.X).Val(r.Y).Val(r.Width).Val(r.Height).EndArr();
        }
    }
}
