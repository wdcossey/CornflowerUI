using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// A control that opens a popup: a flyout, a dialog, a picker, an open ComboBox. The host is placed on a stage
    /// like any specimen; <see cref="Open"/> opens the popup from it and returns whatever <see cref="Close"/> needs.
    /// </summary>
    internal sealed class PopupSpecimen
    {
        public string Name;
        public string Variant;
        public double Height = 100;
        public int SettleMs = 900;
        public Func<FrameworkElement> MakeHost;
        public Func<FrameworkElement, object> Open;
        public Action<FrameworkElement, object> Close;
    }

    /// <summary>What the reference app opens and captures outside the stage. Add a popup by adding lines here.</summary>
    internal static class PopupCatalog
    {
        public static List<PopupSpecimen> All()
        {
            var list = new List<PopupSpecimen>();
            Func<FrameworkElement> button = () => new Button { Content = "open" };

            list.Add(Flyout("Flyout", "default", FlyoutPlacementMode.Top));
            list.Add(Flyout("Flyout", "full", FlyoutPlacementMode.Full));

            list.Add(new PopupSpecimen
            {
                Name = "MenuFlyout",
                Variant = "four-items",
                MakeHost = button,
                Open = host =>
                {
                    var m = new MenuFlyout();
                    m.Items.Add(new MenuFlyoutItem { Text = "first item" });
                    m.Items.Add(new ToggleMenuFlyoutItem { Text = "toggle item", IsChecked = true });
                    m.Items.Add(new MenuFlyoutSeparator());
                    m.Items.Add(new MenuFlyoutItem { Text = "disabled item", IsEnabled = false });
                    m.ShowAt(host);
                    return m;
                },
                Close = (host, h) => ((MenuFlyout)h).Hide(),
            });

            list.Add(Dialog("default", false));
            list.Add(Dialog("full-size", true));

            list.Add(ListPicker("single", ListPickerFlyoutSelectionMode.Single));
            list.Add(ListPicker("multiple", ListPickerFlyoutSelectionMode.Multiple));

            list.Add(new PopupSpecimen
            {
                Name = "PickerFlyout",
                Variant = "confirmation-buttons",
                MakeHost = button,
                Open = host =>
                {
                    var p = new PickerFlyout { Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) }, ConfirmationButtonsVisible = true };
                    var op = p.ShowAtAsync(host);
                    return p;
                },
                Close = (host, h) => ((PickerFlyout)h).Hide(),
            });

            list.Add(new PopupSpecimen
            {
                Name = "DatePickerFlyout",
                Variant = "default",
                MakeHost = button,
                Open = host =>
                {
                    var d = new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) };
                    var op = d.ShowAtAsync(host);
                    return d;
                },
                Close = (host, h) => ((DatePickerFlyout)h).Hide(),
            });

            list.Add(new PopupSpecimen
            {
                Name = "TimePickerFlyout",
                Variant = "default",
                MakeHost = button,
                Open = host =>
                {
                    var t = new TimePickerFlyout { Time = new TimeSpan(12, 30, 0) };
                    var op = t.ShowAtAsync(host);
                    return t;
                },
                Close = (host, h) => ((TimePickerFlyout)h).Hide(),
            });

            // The page's app bar (#23): compact and minimal, closed and open. The phone draws it outside the page, so
            // only -Grab sees its pixels.
            list.Add(BottomAppBar("compact", AppBarClosedDisplayMode.Compact, false));
            list.Add(BottomAppBar("compact-open", AppBarClosedDisplayMode.Compact, true));
            list.Add(BottomAppBar("minimal", AppBarClosedDisplayMode.Minimal, false));
            list.Add(BottomAppBar("minimal-open", AppBarClosedDisplayMode.Minimal, true));

            // Every count from 3, which opens inline, to 10, which opens the full-screen picker: where it flips (#25).
            for (var count = 3; count <= 10; count++)
            {
                list.Add(Combo("open-" + count + "-items", count));
            }

            return list;
        }

        private static PopupSpecimen Flyout(string name, string variant, FlyoutPlacementMode placement)
        {
            return new PopupSpecimen
            {
                Name = name,
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var f = new Flyout
                    {
                        Placement = placement,
                        Content = new TextBlock { Text = "flyout content", TextWrapping = TextWrapping.Wrap },
                    };
                    f.ShowAt(host);
                    return f;
                },
                Close = (host, h) => ((Flyout)h).Hide(),
            };
        }

        private static PopupSpecimen Dialog(string variant, bool fullSize)
        {
            return new PopupSpecimen
            {
                Name = "ContentDialog",
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var d = new ContentDialog
                    {
                        Title = "content dialog",
                        Content = "Agjy 0123 the dialog's body text, long enough to wrap onto a second line.",
                        PrimaryButtonText = "ok",
                        SecondaryButtonText = "cancel",
                        FullSizeDesired = fullSize,
                    };
                    var op = d.ShowAsync();
                    return d;
                },
                Close = (host, h) => ((ContentDialog)h).Hide(),
            };
        }

        private static PopupSpecimen ListPicker(string variant, ListPickerFlyoutSelectionMode mode)
        {
            return new PopupSpecimen
            {
                Name = "ListPickerFlyout",
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var p = new ListPickerFlyout
                    {
                        ItemsSource = new[] { "one", "two", "three", "four", "five" },
                        SelectionMode = mode,
                    };
                    p.SelectedIndex = 1;
                    PickerFlyoutBase.SetTitle(p, "LIST PICKER");
                    var op = p.ShowAtAsync(host);
                    return p;
                },
                Close = (host, h) => ((ListPickerFlyout)h).Hide(),
            };
        }

        /// <summary>What <see cref="BottomAppBar"/> opened, for its Close.</summary>
        private sealed class AppBarHandle
        {
            public Page Page;
            public CommandBar Bar;
            public DispatcherTimer OpenTimer;
            public bool WantOpen;
            public bool Opened;
        }

        /// <summary>
        /// A CommandBar as the page's BottomAppBar, with three primary commands (one disabled, one a checked toggle) and
        /// two secondary ones, set on the page that holds the stage and taken off again on close. The shell draws it,
        /// so only -Grab sees its pixels.
        /// </summary>
        private static PopupSpecimen BottomAppBar(string variant, AppBarClosedDisplayMode mode, bool open)
        {
            return new PopupSpecimen
            {
                Name = "BottomAppBar",
                Variant = variant,
                SettleMs = open ? 2200 : 1500,
                MakeHost = () => new TextBlock { Text = "page with an app bar", Margin = new Thickness(0, 12, 0, 0) },
                Open = host =>
                {
                    var bar = new CommandBar { ClosedDisplayMode = mode };
                    bar.PrimaryCommands.Add(new AppBarButton { Label = "add", Icon = new SymbolIcon(Symbol.Add) });
                    bar.PrimaryCommands.Add(new AppBarToggleButton { Label = "favorite", Icon = new SymbolIcon(Symbol.Favorite), IsChecked = true });
                    bar.PrimaryCommands.Add(new AppBarButton { Label = "delete", Icon = new SymbolIcon(Symbol.Delete), IsEnabled = false });
                    bar.SecondaryCommands.Add(new AppBarButton { Label = "first menu item" });
                    bar.SecondaryCommands.Add(new AppBarButton { Label = "second menu item" });
                    bar.RequestedTheme = ThemeOf(host);
                    var h = new AppBarHandle { Page = CurrentPage(), Bar = bar, WantOpen = open };
                    bar.Opened += (s, e) => h.Opened = true;
                    h.Page.BottomAppBar = bar;
                    if (open)
                    {
                        // Set as the bar is attached, IsOpen is ignored (2026-11-09): open it once the bar has loaded.
                        h.OpenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                        h.OpenTimer.Tick += (s, e) =>
                        {
                            h.OpenTimer.Stop();
                            bar.IsOpen = true;
                        };
                        bar.Loaded += (s, e) => h.OpenTimer.Start();
                    }

                    return h;
                },
                Close = (host, handle) =>
                {
                    var h = (AppBarHandle)handle;
                    if (h == null)
                    {
                        return;
                    }

                    if (h.OpenTimer != null)
                    {
                        h.OpenTimer.Stop();
                    }

                    // Never take a popup away mid-animation (see the README): close the bar, and detach it once closed.
                    if (h.Bar.IsOpen)
                    {
                        h.Bar.Closed += (s, e) => h.Page.BottomAppBar = null;
                        h.Bar.IsOpen = false;
                    }
                    else
                    {
                        h.Page.BottomAppBar = null;
                    }

                    if (h.WantOpen && !h.Opened)
                    {
                        throw new InvalidOperationException("the app bar never opened");
                    }
                },
            };
        }

        /// <summary>The theme the stage asked for: the first RequestedTheme set above the host.</summary>
        private static ElementTheme ThemeOf(FrameworkElement host)
        {
            for (DependencyObject e = host; e != null; e = VisualTreeHelper.GetParent(e))
            {
                var f = e as FrameworkElement;
                if (f != null && f.RequestedTheme != ElementTheme.Default)
                {
                    return f.RequestedTheme;
                }
            }

            return ElementTheme.Default;
        }

        /// <summary>The page the window's frame shows: the app's only page, MainPage.</summary>
        private static Page CurrentPage()
        {
            var frame = Window.Current.Content as Frame;
            var page = frame != null ? frame.Content as Page : null;
            if (page == null)
            {
                throw new InvalidOperationException("the window's content is not a Frame showing a Page");
            }

            return page;
        }

        private static PopupSpecimen Combo(string variant, int count)
        {
            return new PopupSpecimen
            {
                Name = "ComboBox",
                Variant = variant,
                Height = 400,
                MakeHost = () =>
                {
                    var c = new ComboBox { Header = "combo box" };
                    for (var i = 1; i <= count; i++)
                    {
                        c.Items.Add("item " + i);
                    }

                    c.SelectedIndex = 0;
                    return c;
                },
                Open = host =>
                {
                    ((ComboBox)host).IsDropDownOpen = true;
                    return null;
                },
                Close = (host, h) => ((ComboBox)host).IsDropDownOpen = false,
            };
        }
    }
}
