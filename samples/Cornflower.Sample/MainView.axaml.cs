using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Cornflower.Sample;

/// <summary>
/// The theme page: the accent, the 20 named accents, the type ramp and the font families,
/// each read through its key, and what the device reports. It has no timing code: <see cref="SampleShell"/> times it,
/// as it does every page, and writes its ready line. First in the catalogue, under the title it shows.
/// </summary>
[SamplePage("theme", Order = 0)]
public partial class MainView : UserControl
{
    internal static readonly string[] NamedAccentNames =
    [
        "Lime", "Green", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
        "Crimson", "Red", "Orange", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe",
    ];

    internal static readonly string[] FontSizeKeys =
    [
        "TextStyleSmallFontSize", "TextStyleMediumFontSize", "TextStyleLargeFontSize",
        "ContentControlFontSize", "TextStyleExtraLargeFontSize", "TextStyleExtraLargePlusFontSize",
        "TextStyleExtraExtraLargeFontSize",
    ];

    internal static readonly string[] FontFamilyKeys =
    [
        "PhoneFontFamilyLight", "PhoneFontFamilySemiLight", "PhoneFontFamilyNormal", "PhoneFontFamilySemiBold",
    ];

    private TopLevel? _top;
    private IInsetsManager? _insets;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
        this.GetControl<TextBlock>("PageTitle").Text = SamplePageCatalog.TitleOf(GetType());

        var accents = this.GetControl<WrapPanel>("NamedAccents");
        foreach (var name in NamedAccentNames)
            accents.Children.Add(Swatch(name));

        var ramp = this.GetControl<StackPanel>("TypeRamp");
        foreach (var key in FontSizeKeys)
        {
            var line = new TextBlock { Name = key, Text = key };
            line.Bind(TextBlock.FontSizeProperty, this.GetResourceObservable(key));
            ramp.Children.Add(line);
        }

        var families = this.GetControl<StackPanel>("FontFamilies");
        foreach (var key in FontFamilyKeys)
        {
            var line = new TextBlock { Name = key, Text = key };
            line.Bind(TextBlock.FontFamilyProperty, this.GetResourceObservable(key));
            families.Children.Add(line);
        }
    }

    /// <summary>What the device reports, as shown on the page (<see cref="Diagnostics.DeviceFacts"/>).</summary>
    internal string DeviceFacts { get; private set; } = "";

    private Border Swatch(string name)
    {
        var brush = new SolidColorBrush();
        brush.Bind(SolidColorBrush.ColorProperty, this.GetResourceObservable($"CornflowerAccent{name}Color"));
        return new Border
        {
            Name = $"Accent{name}",
            Width = 96,
            Height = 96,
            Margin = new Thickness(0, 0, 9.6, 9.6),
            Background = brush,
            Child = new TextBlock
            {
                Text = name.ToLowerInvariant(),
                Margin = new Thickness(6, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Bottom,
                FontSize = 12,
            },
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        _top = top;
        _insets = top.InsetsManager;
        if (_insets is not null)
            _insets.SafeAreaChanged += OnSafeAreaChanged;
        top.ScalingChanged += OnScalingChanged;
        UpdateFacts();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null)
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
        _insets = null;
        if (_top is not null)
            _top.ScalingChanged -= OnScalingChanged;
        _top = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateFacts();
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => UpdateFacts();

    private void OnScalingChanged(object? sender, EventArgs e) => UpdateFacts();

    private void UpdateFacts()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        DeviceFacts = Diagnostics.DeviceFacts.Of(top, this);
        this.GetControl<TextBlock>("DeviceFactsText").Text = DeviceFacts.Replace(' ', '\n');
    }
}
