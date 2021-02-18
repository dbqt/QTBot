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
                return this.isDisconnected;
            }
            set
            {
                this.isDisconnected = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDisconnected)));
            }
        }

        public bool IsDialogVisible
        {
            get
            { 
                return this.isDialogVisible;
            }
            set 
            { 
                this.isDialogVisible = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDialogVisible)));
            }
        }

        public MainContent()
        {
            if (MainContent.Instance != null)
            {
                Utilities.Log("Error with MainContent - MainContent was created a second time!");
                return;
            }

            MainContent.Instance = this;

            InitializeComponent();
            DataContext = this;

            QTCore.Instance.OnConnected += InstanceOnConnected;
            QTCore.Instance.OnDisconnected += InstanceOnDisconnected;

            // Setup views
            HideAllViews();
            this.Home.Visibility = Visibility.Visible;

            // Setup dialog system
            this.DialogBoxMainButton.Click += DialogBoxMainButtonClick;
            this.DialogBoxSecondaryButton.Click += DialogBoxSecondaryButtonClick;

            CheckUpdate();
        }

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            this.IsDisconnected = false;
        }

        private void InstanceOnDisconnected(object sender, EventArgs e)
        {
            this.IsDisconnected = true;
        }

        ~MainContent()
        {
            this.DialogBoxMainButton.Click -= DialogBoxMainButtonClick;
            this.DialogBoxSecondaryButton.Click -= DialogBoxSecondaryButtonClick;
            QTCore.Instance.OnConnected -= InstanceOnConnected;
            QTCore.Instance.OnDisconnected -= InstanceOnDisconnected;
        }

        private async void CheckUpdate()
        {
            this.UpdateAlertButton.Visibility = Visibility.Collapsed;

            var needToUpdate = await Utilities.CheckForUpdate();
            if (needToUpdate)
            {
                Utilities.ExecuteOnUIThread(() =>
                {
                    this.UpdateAlertButton.Visibility = Visibility.Visible;
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
            this.dialogMainAction?.Invoke();
        }

        private void DialogBoxSecondaryButtonClick(object sender, RoutedEventArgs e)
        {
            this.dialogSecondaryAction?.Invoke();
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
                this.DialogBoxTitle.Text = string.Empty;
                this.DialogBoxTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.DialogBoxTitle.Text = options.Title;
                this.DialogBoxTitle.Visibility = Visibility.Visible;
            }

            if (string.IsNullOrWhiteSpace(options.Message))
            {
                this.DialogBoxMessage.Text = string.Empty;
                this.DialogBoxMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.DialogBoxMessage.Text = options.Message;
                this.DialogBoxMessage.Visibility = Visibility.Visible;
            }

            if (options.MainButton != null)
            {
                this.DialogBoxMainButton.Content = options.MainButton.Label;
                this.dialogMainAction = options.MainButton.Callback;
                this.DialogBoxMainButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.DialogBoxMainButton.Visibility = Visibility.Collapsed;
            }

            if (options.SecondaryButton != null)
            {
                this.DialogBoxSecondaryButton.Content = options.SecondaryButton.Label;
                this.dialogSecondaryAction = options.SecondaryButton.Callback;
                this.DialogBoxSecondaryButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.DialogBoxSecondaryButton.Visibility = Visibility.Collapsed;
            }

            this.DialogProgressBar.Visibility = options.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;

            this.IsDialogVisible = true;
        }

        public void DismissDialog()
        {
            this.IsDialogVisible = false;
        }
    }
}
