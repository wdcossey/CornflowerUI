using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RefApp
{
    public sealed partial class MainPage : Page
    {
        private bool _started;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            try
            {
                await StatusBar.GetForCurrentView().HideAsync();
            }
            catch (Exception)
            {
                // Not fatal: each stage is rendered on its own, not read from the screen.
            }

            if ((await Options.LoadAsync()).Demo)
            {
                Log.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Demo.Show(StageHost);
                return;
            }

            await new Suite(StageHost, Log).RunAsync();
        }
    }
}
