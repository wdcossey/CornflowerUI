// Types shaped as Windows.winmd declares WinRT controls, for the checklist generator's tests: the
// generator reads this assembly's metadata in place of the SDK's, which stays outside the repo. A
// dependency property is a static ...Property property beside the instance property it backs.
#pragma warning disable CS0067 // The events are read as metadata, never raised.
#pragma warning disable CA1822

namespace Windows.UI.Xaml.Controls
{
    public class Control
    {
        public double FontSize { get; set; }

        public static object FontSizeProperty => null!;
    }

    public class ContentControl : Control
    {
        public object? Content { get; set; }

        public static object ContentProperty => null!;
    }

    public class Button : Primitives.ButtonBase
    {
        public object? Flyout { get; set; }

        public static object FlyoutProperty => null!;
    }

    public delegate void RoutedEventHandler(object sender, object e);

    namespace Primitives
    {
        public class ButtonBase : ContentControl
        {
            public object? Command { get; set; }

            public static object CommandProperty => null!;

            public bool IsPressed { get; }

            public static object IsPressedProperty => null!;

            /// <summary>Not a dependency property: there's no static IsDefaultishProperty.</summary>
            public bool IsDefaultish { get; set; }

            public event RoutedEventHandler? Click;

            protected virtual void OnToggle()
            {
            }

            public void Focus(int state)
            {
            }

            private void Hidden()
            {
            }
        }

        public class ToggleButton : ButtonBase
        {
            public bool? IsChecked { get; set; }

            public static object IsCheckedProperty => null!;

            public event RoutedEventHandler? Checked;
        }
    }
}
