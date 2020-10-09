using QTBot.Core;
using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace QTBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isConnected = false;

        private bool isRedemptionInChat = false;
        public bool IsRedemptionInChat 
        {
            get { return this.isRedemptionInChat; } 
            set
            {
                this.isRedemptionInChat = value;
                this.IsTagUserBox.IsEnabled = value;
                this.IsTagUserLabel.IsEnabled = value;
                this.UserNameTextBox.IsEnabled = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            this.IsRedemptionInChat = false;

            QTCore.Instance.OnConnected += Instance_OnConnected;
            QTCore.Instance.OnDisonnected += Instance_OnDisonnected;
            // Start as disconnected
            Instance_OnDisonnected(null, null);

            CheckConfig();
        }

        private void CheckConfig()
        {
            QTCore.Instance.LoadConfigs();
            if (QTCore.Instance.IsConfigured)
            {
                this.ConfigCheck.Visibility = Visibility.Collapsed;
                this.Connect.IsEnabled = true;
            }
            else
            {
                this.ConfigCheck.Visibility = Visibility.Visible;
                this.Connect.IsEnabled = false;
            }

            this.CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            this.CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;

            var options = QTCore.Instance.TwitchOptions;
            this.IsRedemptionInChat = options.IsRedemptionInChat;
            this.IsTagUserBox.IsChecked = options.IsRedemptionTagUser;
            this.UserNameTextBox.Text = options.RedemptionTagUser;
        }

        #region Events
        private void Instance_OnConnected(object sender, EventArgs e)
        {
            ExecuteOnUIThread(() =>
            {
                this.isConnected = true;
                this.TestMessage.IsEnabled = this.isConnected;
                this.ConnectionStatus.Text = "Connected";
                this.Connect.Content = "Disconnect";
            });
        }

        private void Instance_OnDisonnected(object sender, EventArgs e)
        {
            ExecuteOnUIThread(() =>
            {
                this.isConnected = false;
                this.TestMessage.IsEnabled = this.isConnected;
                this.ConnectionStatus.Text = "Disconnected";
                this.Connect.Content = "Connect";
            });
        }

        private void OnOpenConfigClick(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigManager.GetConfigDirectory());
        }

        private void OnReloadConfigClick(object sender, RoutedEventArgs e)
        {
            CheckConfig();
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (this.isConnected)
            {
                QTCore.Instance.Disconnect();
            }
            else
            {
                QTCore.Instance.Setup();
            }
        }

        private void OnTestMessageClick(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.Test();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var twitchOptions = new TwitchOptions()
            {
                IsRedemptionInChat = this.IsRedemptionInChat,
                IsRedemptionTagUser = this.IsTagUserBox.IsChecked ?? false,
                RedemptionTagUser = this.UserNameTextBox.Text
            };

            QTCore.Instance.SetupTwitchOptions(twitchOptions);
        }

        #endregion Events

        private void ExecuteOnUIThread(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        } 
    }
}
