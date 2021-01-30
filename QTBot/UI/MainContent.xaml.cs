using QTBot.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace QTBot
{
    /// <summary>
    /// Interaction logic for MainContent.xaml
    /// </summary>
    public partial class MainContent : UserControl
    {
        public MainContent()
        {
            InitializeComponent();

            HideAllViews();
            this.Home.Visibility = Visibility.Visible;

            this.UpdateAlertButton.Visibility = Visibility.Collapsed;
            CheckUpdate();
        }

        private async void CheckUpdate()
        {
            var needToUpdate = await Utilities.CheckForUpdate();
            if (needToUpdate || true)
            {
                Utilities.ExecuteOnUIThread(() =>
                {
                    this.UpdateAlertButton.Visibility = Visibility.Visible;
                });
            }
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            HideAllViews();

            var clickedPageName = ((MenuItem)sender).Header.ToString();

            switch (clickedPageName)
            {
                case "Setup":
                    this.Setup.Visibility = Visibility.Visible;
                    break;
                case "Events":
                    this.Events.Visibility = Visibility.Visible;
                    break;
                case "Timers":
                    this.Timers.Visibility = Visibility.Visible;
                    break;
                case "Commands":
                    this.Commands.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    this.Settings.Visibility = Visibility.Visible;
                    break;
                case "Home":
                default:
                    this.Home.Visibility = Visibility.Visible;
                    break;
            }
            this.Header.Text = clickedPageName;
            this.MenuButton.IsChecked = false;
        }

        /// <summary>
        /// Collapses all views in the main content.
        /// </summary>
        private void HideAllViews()
        {
            this.Home.Visibility = Visibility.Collapsed;
            this.Setup.Visibility = Visibility.Collapsed;
            this.Events.Visibility = Visibility.Collapsed;
            this.Timers.Visibility = Visibility.Collapsed;
            this.Commands.Visibility = Visibility.Collapsed;
            this.Settings.Visibility = Visibility.Collapsed;
        }

        private void UpdateAlertButtonClick(object sender, RoutedEventArgs e)
        {
            Utilities.ShowMessage("Update test!");
        }
    }
}
