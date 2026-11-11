using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RefApp
{
    /// <summary>A page of the page-transition probe: parameter "A" is white, anything else the accent blue, inset 19 px on black.</summary>
    public sealed partial class ProbePage : Page
    {
        public ProbePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var a = (e.Parameter as string) == "A";
            Sheet.Background = new SolidColorBrush(a ? Colors.White : Color.FromArgb(255, 0x3E, 0x65, 0xFF));
            Label.Text = "page " + e.Parameter;
        }
    }
}
