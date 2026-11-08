using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// A page to use by hand: a button per popup in <see cref="PopupCatalog"/>, ContentDialog first, and a
    /// dark/light switch. For screenshots taken with the emulator's own tool (Additional Tools, Screenshot),
    /// since RenderTargetBitmap can't see popups. run.ps1 -Demo deploys it.
    /// </summary>
    internal static class Demo
    {
        public static void Show(Grid host)
        {
            var theme = ElementTheme.Dark;
            var root = new Grid { RequestedTheme = theme, Background = new SolidColorBrush(Colors.Black) };
            var panel = new StackPanel { Margin = new Thickness(19, 24, 19, 96) };
            root.Children.Add(new ScrollViewer { Content = panel });

            var themes = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            Action<ElementTheme> setTheme = t =>
            {
                theme = t;
                root.RequestedTheme = t;
                root.Background = new SolidColorBrush(t == ElementTheme.Dark ? Colors.Black : Colors.White);
            };
            var dark = new Button { Content = "dark", Margin = new Thickness(0, 0, 12, 0) };
            dark.Click += (s, e) => setTheme(ElementTheme.Dark);
            var light = new Button { Content = "light" };
            light.Click += (s, e) => setTheme(ElementTheme.Light);
            themes.Children.Add(dark);
            themes.Children.Add(light);
            panel.Children.Add(new TextBlock { Text = "CORNFLOWER DEMO", FontSize = 15, FontWeight = Windows.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
            panel.Children.Add(themes);

            var popups = PopupCatalog.All().OrderBy(p => p.Name == "ContentDialog" ? 0 : 1).ToList();
            foreach (var p in popups)
            {
                panel.Children.Add(new TextBlock { Text = p.Name + " · " + p.Variant, FontSize = 20, Margin = new Thickness(0, 12, 0, 0) });
                var element = p.MakeHost();
                var button = element as Button;
                if (button != null)
                {
                    var spec = p;
                    button.Content = "open";
                    button.Click += (s, e) => spec.Open(button);
                }

                panel.Children.Add(element);
            }

            // Popups don't inherit the page's theme; give each open one the chosen theme, as the suite does.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            timer.Tick += (s, e) =>
            {
                foreach (var popup in VisualTreeHelper.GetOpenPopups(Window.Current))
                {
                    var child = popup.Child as FrameworkElement;
                    if (child != null && child.RequestedTheme != theme)
                    {
                        child.RequestedTheme = theme;
                    }
                }
            };
            timer.Start();

            host.HorizontalAlignment = HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            host.Children.Add(root);
        }
    }
}
