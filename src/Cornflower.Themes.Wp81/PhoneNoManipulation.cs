using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// WinRT's <c>ManipulationMode="None"</c>, which the phone's Slider style sets: a touch that starts on the control is
/// the control's, so a drag on it never pans the <c>ScrollViewer</c> it is in. Avalonia's <c>ScrollViewer</c> takes
/// a touch drag that passes its threshold by its gesture recogniser, capturing the pointer from the Slider's track,
/// so the page scrolls and the thumb stops. Turned on, this asks Avalonia to skip gesture recognition for every
/// pointer event that reaches the control, as each one tunnels through it to its parts.
/// </summary>
/// <remarks>
/// It reads and changes nothing but the event's gesture flag (<see cref="PointerEventArgs.PreventGestureRecognition"/>);
/// the control's own handling goes on. Turning it off removes the handlers.
/// </remarks>
internal static class PhoneNoManipulation
{
    /// <summary>Set by a theme: no pan or zoom gesture starts on the control.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("IsEnabled", typeof(PhoneNoManipulation));

    static PhoneNoManipulation()
    {
        IsEnabledProperty.Changed.AddClassHandler<InputElement>((element, change) =>
        {
            element.RemoveHandler(InputElement.PointerPressedEvent, Prevent);
            element.RemoveHandler(InputElement.PointerMovedEvent, Prevent);
            element.RemoveHandler(InputElement.PointerReleasedEvent, Prevent);
            if (change.GetNewValue<bool>())
            {
                element.AddHandler(InputElement.PointerPressedEvent, Prevent, RoutingStrategies.Tunnel, handledEventsToo: true);
                element.AddHandler(InputElement.PointerMovedEvent, Prevent, RoutingStrategies.Tunnel, handledEventsToo: true);
                element.AddHandler(InputElement.PointerReleasedEvent, Prevent, RoutingStrategies.Tunnel, handledEventsToo: true);
            }
        });
    }

    public static bool GetIsEnabled(InputElement element) => element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(InputElement element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void Prevent(object? sender, PointerEventArgs e) => e.PreventGestureRecognition();
}
