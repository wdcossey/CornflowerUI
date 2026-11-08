using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace RefApp
{
    /// <summary>One control in one configuration. The suite captures it at rest, then in every visual state its template declares.</summary>
    internal sealed class Specimen
    {
        public string Name;
        public string Variant;
        public double Height = 120;
        public bool FullBleed;
        public bool States = true;
        public Func<FrameworkElement> Make;
    }

    /// <summary>
    /// What the reference app captures. Add a control by adding lines here, nothing else. Content strings are
    /// lower case, as the phone's own UI wrote them.
    /// </summary>
    internal static class Catalog
    {
        private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";

        /// <summary>Every TextBlock style key in the phone's generic.xaml and themeresources.xaml (2026-11-07 inventory).</summary>
        private static readonly string[] TextStyles =
        {
            "BaseTextBlockStyle", "HeaderTextBlockStyle", "SubheaderTextBlockStyle", "TitleTextBlockStyle", "BodyTextBlockStyle",
            "GroupHeaderTextBlockStyle", "ControlHeaderTextBlockStyle", "ControlContextualInfoTextBlockStyle",
            "ListViewItemTextBlockStyle", "ListViewItemSubheaderTextBlockStyle", "ListViewItemContentTextBlockStyle",
            "ListViewEmptyStaticTextBlockStyle", "FlyoutPickerTitleTextBlockStyle", "ComboBoxPlaceholderTextBlockStyle",
        };

        public static List<Specimen> All()
        {
            var list = new List<Specimen>();
            Action<string, string, double, Func<FrameworkElement>> add = (name, variant, height, make) =>
                list.Add(new Specimen { Name = name, Variant = variant, Height = height, Make = make });

            foreach (var key in TextStyles)
            {
                var k = key;
                list.Add(new Specimen
                {
                    Name = "TextBlock",
                    Variant = k,
                    Height = 120,
                    States = false,
                    Make = () => (FrameworkElement)XamlReader.Load(
                        "<TextBlock " + Ns + " Style=\"{ThemeResource " + k + "}\" Text=\"Agjy 0123 cornflower\" />"),
                });
            }

            add("Button", "default", 100, () => new Button { Content = "button" });
            add("HyperlinkButton", "default", 100, () => new HyperlinkButton { Content = "hyperlink button" });
            add("RepeatButton", "default", 100, () => new RepeatButton { Content = "repeat button" });
            add("ToggleButton", "unchecked", 100, () => new ToggleButton { Content = "toggle button" });
            add("ToggleButton", "checked", 100, () => new ToggleButton { Content = "toggle button", IsChecked = true });
            add("CheckBox", "unchecked", 100, () => new CheckBox { Content = "check box" });
            add("CheckBox", "checked", 100, () => new CheckBox { Content = "check box", IsChecked = true });
            add("CheckBox", "indeterminate", 100, () => new CheckBox { Content = "check box", IsThreeState = true, IsChecked = null });
            add("RadioButton", "unchecked", 100, () => new RadioButton { Content = "radio button" });
            add("RadioButton", "checked", 100, () => new RadioButton { Content = "radio button", IsChecked = true });
            add("ToggleSwitch", "off", 140, () => new ToggleSwitch { Header = "toggle switch" });
            add("ToggleSwitch", "on", 140, () => new ToggleSwitch { Header = "toggle switch", IsOn = true });
            add("ToggleSwitch", "no-header", 110, () => new ToggleSwitch());
            add("Slider", "value-30", 140, () => new Slider { Header = "slider", Value = 30 });
            add("ProgressBar", "value-40", 60, () => new ProgressBar { Value = 40 });
            add("ProgressBar", "indeterminate", 60, () => new ProgressBar { IsIndeterminate = true });
            add("ProgressRing", "active", 120, () => new ProgressRing { IsActive = true });
            add("TextBox", "empty", 140, () => new TextBox { Header = "text box", PlaceholderText = "placeholder" });
            add("TextBox", "text", 140, () => new TextBox { Header = "text box", Text = "Agjy 0123" });
            add("PasswordBox", "password", 140, () => new PasswordBox { Header = "password box", Password = "secret", IsPasswordRevealButtonEnabled = true });
            add("ComboBox", "closed", 140, () =>
            {
                var c = new ComboBox { Header = "combo box" };
                c.Items.Add("one");
                c.Items.Add("two");
                c.Items.Add("three");
                c.SelectedIndex = 0;
                return c;
            });
            add("DatePicker", "default", 140, () => new DatePicker { Header = "date picker", Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) });
            add("TimePicker", "default", 140, () => new TimePicker { Header = "time picker", Time = new TimeSpan(12, 30, 0) });
            add("AppBarButton", "default", 120, () => new AppBarButton { Label = "add", Icon = new SymbolIcon(Symbol.Add) });
            add("AppBarToggleButton", "unchecked", 120, () => new AppBarToggleButton { Label = "pin", Icon = new SymbolIcon(Symbol.Pin) });
            add("AppBarToggleButton", "checked", 120, () => new AppBarToggleButton { Label = "pin", Icon = new SymbolIcon(Symbol.Pin), IsChecked = true });
            add("ListViewItem", "default", 100, () => new ListViewItem { Content = "list view item" });
            add("ListViewItem", "selected", 100, () => new ListViewItem { Content = "list view item", IsSelected = true });
            add("ListView", "three-items", 260, () =>
            {
                var l = new ListView();
                l.Items.Add("one");
                l.Items.Add("two");
                l.Items.Add("three");
                l.SelectedIndex = 1;
                return l;
            });

            list.Add(new Specimen
            {
                Name = "Pivot",
                Variant = "two-items",
                Height = 320,
                FullBleed = true,
                Make = () =>
                {
                    var p = new Pivot { Title = "CORNFLOWER" };
                    p.Items.Add(new PivotItem { Header = "one", Content = new TextBlock { Text = "first pivot item" } });
                    p.Items.Add(new PivotItem { Header = "two", Content = new TextBlock { Text = "second pivot item" } });
                    return p;
                },
            });
            list.Add(new Specimen
            {
                Name = "Hub",
                Variant = "two-sections",
                Height = 400,
                FullBleed = true,
                Make = () =>
                {
                    var h = new Hub { Header = "hub" };
                    h.Sections.Add(new HubSection { Header = "one" });
                    h.Sections.Add(new HubSection { Header = "two" });
                    return h;
                },
            });

            return list;
        }
    }
}
