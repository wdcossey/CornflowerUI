using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace RefApp
{
    internal sealed partial class Suite
    {
        /// <summary>
        /// Opens a popup from its host on a stage and records it.
        ///
        /// RenderTargetBitmap can't see popups on the phone (read on 2026-11-08): rendering a popup's child throws
        /// ArgumentException, and rendering the whole window leaves popups out. Moving a popup's child onto a stage
        /// renders, but not as the phone draws it: the content lays out again and loses its state. So a popup gets
        /// its tree dumped where it is, in window coordinates (the numbers, without pixels), and the host's stage is
        /// rendered, because opening a ComboBox changes the control itself. Pixels of a popup need a capture of the
        /// emulator's screen. Popups don't inherit the stage's theme, so each popup's child is given the requested
        /// theme after it opens.
        /// </summary>
        private async Task CapturePopup(ElementTheme theme, PopupSpecimen p)
        {
            // Held for a screen grab, the stage fills the window, so the page behind the popup has the theme's background.
            var height = _holdMs > 0 ? Window.Current.Bounds.Height : p.Height;
            var s = new Specimen { Name = p.Name, Variant = p.Variant, Height = height, Make = p.MakeHost };
            var baseName = Id(theme, s, "popup");
            _log.Text = string.Format(CultureInfo.InvariantCulture, "{0}/{1} {2}", _count + 1, _total, baseName);
            await Write("progress.txt", baseName);
            Stage stage = null;
            object handle = null;
            try
            {
                stage = await Place(theme, s);
                handle = p.Open(stage.Element);
                await Task.Delay(p.SettleMs);

                var popups = VisualTreeHelper.GetOpenPopups(Window.Current).ToList();
                foreach (var popup in popups)
                {
                    var child = popup.Child as FrameworkElement;
                    if (child != null)
                    {
                        child.RequestedTheme = theme;
                    }
                }

                await Task.Delay(250);
                await RenderStage(theme, s, stage, popups.Count);
                if (popups.Count == 0 && !p.System)
                {
                    _errors.Add(baseName + ": no popup open");
                }

                for (var k = 0; k < popups.Count; k++)
                {
                    var child = popups[k].Child as FrameworkElement;
                    if (child == null)
                    {
                        _errors.Add(baseName + ": popup " + k + " has no FrameworkElement child");
                        continue;
                    }

                    child.UpdateLayout();
                    var r = child.TransformToVisual(null).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
                    var j = new Json().Obj();
                    j.Prop("theme", theme.ToString());
                    j.Prop("specimen", s.Name);
                    j.Prop("variant", s.Variant);
                    j.Prop("popup", k);
                    j.Prop("openPopups", popups.Count);
                    j.Key("popupOffset").Arr().Val(popups[k].HorizontalOffset).Val(popups[k].VerticalOffset).EndArr();
                    j.Key("inWindow").Arr().Val(r.X).Val(r.Y).Val(r.Width).Val(r.Height).EndArr();
                    j.Key("tree");
                    TreeDump.Write(j, child, child);
                    j.EndObj();
                    await Write(Id(theme, s, "popup" + k) + ".json", j.ToString());
                }

                if (_holdMs > 0)
                {
                    // vm\grab-loop.ps1 grabs the emulator's window all along; run.ps1 picks the frame inside this span.
                    var from = DateTimeOffset.UtcNow;
                    await Task.Delay(_holdMs);
                    var line = baseName + "\t" + from.ToString("o", CultureInfo.InvariantCulture) + "\t" + DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\n";
                    var timeline = await _out.CreateFileAsync("held.txt", CreationCollisionOption.OpenIfExists);
                    await FileIO.AppendTextAsync(timeline, line);
                }
            }
            catch (Exception ex)
            {
                _errors.Add(baseName + ": " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try
                {
                    if (stage != null)
                    {
                        p.Close(stage.Element, handle);
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add(baseName + ": close: " + ex.GetType().Name + ": " + ex.Message);
                }

                // Full-screen pickers and dialogs animate out. Wait for them: closing a popup by force mid-animation
                // is the likeliest reason the app died in the first runs.
                for (var waited = 0; waited < 3000 && VisualTreeHelper.GetOpenPopups(Window.Current).Count > 0; waited += 100)
                {
                    await Task.Delay(100);
                }

                var left = VisualTreeHelper.GetOpenPopups(Window.Current).Count;
                if (left > 0)
                {
                    _errors.Add(baseName + ": " + left + " popup(s) still open 3 s after closing");
                }
            }
        }

        private async Task RenderStage(ElementTheme theme, Specimen s, Stage stage, int popupCount)
        {
            var name = Id(theme, s, "stage");
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

            _count++;
            var j = new Json().Obj();
            j.Prop("file", name + ".png");
            j.Prop("theme", theme.ToString());
            j.Prop("specimen", s.Name);
            j.Prop("variant", s.Variant);
            j.Prop("group", "popup");
            j.Prop("state", "stage");
            j.Prop("openPopups", popupCount);
            j.Key("pixels").Arr().Val(rtb.PixelWidth).Val(rtb.PixelHeight).EndArr();
            j.Key("stage").Arr().Val(_width).Val(s.Height).EndArr();
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
                .Prop("group", "popup")
                .Prop("state", "stage")
                .Prop("stateApplied", true)
                .EndObj();
        }
    }
}
