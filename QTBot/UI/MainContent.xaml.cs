using QTBot.Helpers;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static QTBot.Helpers.Utilities;

namespace QTBot
{
    /// <summary>
    /// Interaction logic for MainContent.xaml
    /// </summary>
    public partial class MainContent : UserControl, INotifyPropertyChanged
    {
        public static MainContent Instance = null;

        private bool isDialogVisible = false;
        private Action dialogMainAction;
        private Action dialogSecondaryAction;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool isDisconnected = true;
        public bool IsDisconnected
        {
            get
            {
                return isDisconnected;
            }
            set
            {
                isDisconnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDisconnected)));
            }
        }

        public bool IsDialogVisible
        {
            get
            {
                return isDialogVisible;
            }
            set
            {
                isDialogVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDialogVisible)));
            }
        }

        public MainContent()
        {
            if (MainContent.Instance != null)
            {
                Utilities.Log(Microsoft.Extensions.Logging.LogLevel.Error, "Error with MainContent - MainContent was created a second time!");
                return;
            }

            MainContent.Instance = this;

            InitializeComponent();
            DataContext = this;

            QTCore.Instance.OnConnected += InstanceOnConnected;
            QTCore.Instance.OnDisconnected += InstanceOnDisconnected;

            // Setup views
            HideAllViews();
            Home.Visibility = Visibility.Visible;

            // Setup dialog system
            DialogBoxMainButton.Click += DialogBoxMainButtonClick;
            DialogBoxSecondaryButton.Click += DialogBoxSecondaryButtonClick;

            CheckUpdate();
        }

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            IsDisconnected = false;
        }

        private void InstanceOnDisconnected(object sender, EventArgs e)
        {
            IsDisconnected = true;
        }

        ~MainContent()
        {
            DialogBoxMainButton.Click -= DialogBoxMainButtonClick;
            DialogBoxSecondaryButton.Click -= DialogBoxSecondaryButtonClick;
            QTCore.Instance.OnConnected -= InstanceOnConnected;
            QTCore.Instance.OnDisconnected -= InstanceOnDisconnected;
        }

        private async void CheckUpdate()
        {
            UpdateAlertButton.Visibility = Visibility.Collapsed;

            var needToUpdate = await Utilities.CheckForUpdate();
            if (needToUpdate)
            {
                Utilities.ExecuteOnUIThread(() =>
                {
                    UpdateAlertButton.Visibility = Visibility.Visible;
                });
            }
        }

        /// <summary>
        /// Callback when a menu item is clicked, will switch view content to the appropriate one using the Header string.
        /// </summary>
        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            HideAllViews();

            var clickedPageName = ((MenuItem)sender).Header.ToString();

            switch (clickedPageName)
            {
                case "Setup":
                    Setup.Visibility = Visibility.Visible;
                    break;
                case "Events":
                    Events.Visibility = Visibility.Visible;
                    break;
                case "Timers":
                    Timers.Visibility = Visibility.Visible;
                    break;
                case "Commands":
                    Commands.Visibility = Visibility.Visible;
                    break;
                case "Integrations":
                    Integrations.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    Settings.Visibility = Visibility.Visible;
                    break;
                case "Home":
                default:
                    Home.Visibility = Visibility.Visible;
                    break;
            }
            Header.Text = clickedPageName;
            MenuButton.IsChecked = false;
        }

        private void UpdateAlertButtonClick(object sender, RoutedEventArgs e)
        {
            var updateDialog = new DialogBoxOptions()
            {
                Title = "There is a new update!",
                Message = "Would you like to update now?",
                MainButton = new DialogBoxOptions.DialogBoxButtonOptions()
                {
                    Label = "Update",
                    Callback = async () =>
                        {
                            ShowDialog(new DialogBoxOptions()
                            {
                                Title = "Updating",
                                ShowProgressBar = true
                            });
                            await Task.Delay(1000);
                            await Utilities.UpdateApplication();
                        }
                },
                SecondaryButton = new DialogBoxOptions.DialogBoxButtonOptions()
                {
                    Label = "Cancel",
                    Callback = () => DismissDialog()
                }
            };

            ShowDialog(updateDialog);
        }

        private void DialogBoxMainButtonClick(object sender, RoutedEventArgs e)
        {
            dialogMainAction?.Invoke();
        }

        private void DialogBoxSecondaryButtonClick(object sender, RoutedEventArgs e)
        {
            dialogSecondaryAction?.Invoke();
        }

        /// <summary>
        /// Collapses all views in the main content.
        /// </summary>
        private void HideAllViews()
        {
            Home.Visibility = Visibility.Collapsed;
            Setup.Visibility = Visibility.Collapsed;
            Events.Visibility = Visibility.Collapsed;
            Timers.Visibility = Visibility.Collapsed;
            Commands.Visibility = Visibility.Collapsed;
            Integrations.Visibility = Visibility.Collapsed;
            Settings.Visibility = Visibility.Collapsed;
        }

        public void ShowSimpleDialog(string title, string message)
        {
            ShowDialog(new DialogBoxOptions()
            {
                Title = title,
                Message = message,
                SecondaryButton = new DialogBoxOptions.DialogBoxButtonOptions()
                {
                    Label = "Okai :3",
                    Callback = () => DismissDialog()
                }
            });
        }

        /// <summary>
        /// Show a dialog box with the configured options
        /// </summary>
        public void ShowDialog(DialogBoxOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Title))
            {
                DialogBoxTitle.Text = string.Empty;
                DialogBoxTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                DialogBoxTitle.Text = options.Title;
                DialogBoxTitle.Visibility = Visibility.Visible;
            }

            if (string.IsNullOrWhiteSpace(options.Message))
            {
                DialogBoxMessage.Text = string.Empty;
                DialogBoxMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                DialogBoxMessage.Text = options.Message;
                DialogBoxMessage.Visibility = Visibility.Visible;
            }

            if (options.MainButton != null)
            {
                DialogBoxMainButton.Content = options.MainButton.Label;
                dialogMainAction = options.MainButton.Callback;
                DialogBoxMainButton.Visibility = Visibility.Visible;
            }
            else
            {
                DialogBoxMainButton.Visibility = Visibility.Collapsed;
            }

            if (options.SecondaryButton != null)
            {
                DialogBoxSecondaryButton.Content = options.SecondaryButton.Label;
                dialogSecondaryAction = options.SecondaryButton.Callback;
                DialogBoxSecondaryButton.Visibility = Visibility.Visible;
            }
            else
            {
                DialogBoxSecondaryButton.Visibility = Visibility.Collapsed;
            }

            DialogProgressBar.Visibility = options.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;

            IsDialogVisible = true;
        }

        public void DismissDialog()
        {
            IsDialogVisible = false;
        }
    }
}
