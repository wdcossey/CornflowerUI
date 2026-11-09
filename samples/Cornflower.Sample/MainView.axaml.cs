using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Cornflower.Sample;

/// <summary>
/// The sample's one page: the accent, the 20 named accents, the type ramp and the font families,
/// each read through its key, and what the device reports. Each time the page is shown, once it has
/// drawn, it writes one <c>CORNFLOWER ready</c> line to standard output (logcat's <c>DOTNET</c> tag on Android), which
/// <c>tools/android/run.ps1</c> waits for.
/// </summary>
public partial class MainView : UserControl
{
    internal const string ReadyMarker = "CORNFLOWER ready";

    /// <summary>Where the ready line goes. Standard output; a test replaces it.</summary>
    internal static Action<string> Report { get; set; } = Console.WriteLine;

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

    /// <summary>What the device reports, as shown on the page and in the ready line.</summary>
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

        // The callback runs before the next frame is drawn; the one it asks for runs after it.
        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ => ReportReady()));
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

        var size = top.ClientSize;
        var safe = _insets?.SafeAreaPadding ?? default;
        DeviceFacts = string.Format(CultureInfo.InvariantCulture,
            "scaling={0:0.###} view={1:0.##}x{2:0.##} raw={3:0}x{4:0} safe={5:0.##},{6:0.##},{7:0.##},{8:0.##}",
            top.RenderScaling, size.Width, size.Height,
            size.Width * top.RenderScaling, size.Height * top.RenderScaling,
            safe.Left, safe.Top, safe.Right, safe.Bottom);
        this.GetControl<TextBlock>("DeviceFactsText").Text = DeviceFacts.Replace(' ', '\n');
    }

    private void ReportReady()
    {
        UpdateFacts();
        Report($"{ReadyMarker} {DeviceFacts}");
    }
}
