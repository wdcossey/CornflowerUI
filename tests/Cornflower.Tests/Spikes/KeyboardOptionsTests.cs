using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Headless.XUnit;
using Avalonia.Input.TextInput;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Spikes;

/// <summary>
/// Spike 8: how a theme can tell the platform keyboard a masked box holds a password (#104) and leave every other box
/// as it was. Do the <c>TextInputOptions</c> inherit, does a style setter whose binding gives no value leave an
/// ancestor's, and does a value set at style priority and then taken away leave it?
/// </summary>
/// <remarks>
/// What Avalonia 12.1.4 does: the three <c>PhonePasswordKeyboard</c> sets inherit, so an ancestor's reaches a box and
/// the box's reaches what it holds; a style setter whose binding gives <c>UnsetValue</c> still hides an ancestor's
/// value (the box gets the default); a value set at style priority, once taken away, gives the ancestor's back. So
/// <c>PhonePasswordKeyboard</c> sets its values from code, and takes them away entirely while a box isn't masked.
/// </remarks>
[Collection(SpikeCollection.Name)]
public class KeyboardOptionsTests
{
    private sealed class Unset : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            AvaloniaProperty.UnsetValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }

    [AvaloniaFact]
    public void The_Options_Inherit()
    {
        Assert.True(TextInputOptions.ContentTypeProperty.Inherits);
        Assert.True(TextInputOptions.IsSensitiveProperty.Inherits);
        Assert.True(TextInputOptions.ShowSuggestionsProperty.Inherits);

        var child = new Border();
        var parent = new StackPanel { Children = { child } };
        TextInputOptions.SetContentType(parent, TextInputContentType.Email);
        using var scene = new Scene(parent, themes: [new Avalonia.Themes.Simple.SimpleTheme()]);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(child));
    }

    [AvaloniaFact]
    public void A_Style_Setter_Whose_Binding_Gives_No_Value_Hides_An_Ancestors()
    {
        var box = new Border();
        var style = new Style(x => x.OfType<Border>())
        {
            Setters =
            {
                new Setter(TextInputOptions.ContentTypeProperty, new Binding("Tag")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                    Converter = new Unset(),
                }),
            },
        };
        var parent = new StackPanel { Children = { box } };
        parent.Styles.Add(style);
        TextInputOptions.SetContentType(parent, TextInputContentType.Email);
        using var scene = new Scene(parent, themes: [new Avalonia.Themes.Simple.SimpleTheme()]);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(box));
    }

    [AvaloniaFact]
    public void A_Value_At_Style_Priority_Taken_Away_Gives_The_Ancestors_Back()
    {
        var box = new Border();
        var parent = new StackPanel { Children = { box } };
        TextInputOptions.SetContentType(parent, TextInputContentType.Email);
        using var scene = new Scene(parent, themes: [new Avalonia.Themes.Simple.SimpleTheme()]);
        Dispatcher.UIThread.RunJobs();

        var set = box.SetValue(TextInputOptions.ContentTypeProperty, TextInputContentType.Password, BindingPriority.Style);
        Assert.Equal(TextInputContentType.Password, TextInputOptions.GetContentType(box));
        Assert.NotNull(set);
        set.Dispose();
        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(box));
    }
}
