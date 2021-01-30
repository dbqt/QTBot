using QTBot.Core;
using QTBot.Helpers;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using System.Windows.Shapes;

namespace QTBot
{
    /// <summary>
    /// Interaction logic for LegacyContent.xaml
    /// </summary>
    public partial class LegacyContent : UserControl
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

        public LegacyContent()
        {
            InitializeComponent();

            this.DataContext = this;

            this.IsRedemptionInChat = false;

            QTCore.Instance.OnConnected += Instance_OnConnected;
            QTCore.Instance.OnDisonnected += Instance_OnDisonnected;
            // Start as disconnected
            Instance_OnDisonnected(null, null);

            CheckConfig();

            Update();

        }

        private async Task Update()
        {
            using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/dbqt/QTBot-releases"))
            {
                var updateInfo = await mgr.CheckForUpdate();
                var hasUpdate = updateInfo.CurrentlyInstalledVersion.Version != updateInfo.FutureReleaseEntry.Version;

                await mgr.UpdateApp();

                if (hasUpdate)
                {
                    Utilities.ExecuteOnUIThread(() =>
                        Utilities.ShowMessage("I got an update, please reboot me :)", "QTBot has updated")
                    );
                }
            }
        }

        private void CheckConfig()
        {
            QTCore.Instance.LoadConfigs();
            if (QTCore.Instance.IsConfigured)
            {
                this.ConfigCheck.Visibility = Visibility.Collapsed;
                this.ConfigCheck1.Visibility = Visibility.Collapsed;
                this.Connect.IsEnabled = true;
            }
            else
            {
                this.ConfigCheck.Visibility = Visibility.Visible;
                this.ConfigCheck1.Visibility = Visibility.Visible;
                this.Connect.IsEnabled = false;
            }

            this.CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            this.CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;

            var options = QTCore.Instance.TwitchOptions;
            this.IsRedemptionInChat = options.IsRedemptionInChat;
            this.IsTagUserBox.IsChecked = options.IsRedemptionTagUser;
            this.UserNameTextBox.Text = options.RedemptionTagUser;
            this.IsAutoShoutOutBox.IsChecked = options.IsAutoShoutOutHost;
        }

        #region Events
        private void Instance_OnConnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = true;
                this.TestMessage.IsEnabled = this.isConnected;
                this.TestRedeem1.IsEnabled = this.isConnected;
                this.TestRedeem2.IsEnabled = this.isConnected;
                this.ConnectionStatus.Text = "Connected";
                this.Connect.Content = "Disconnect";
            });
        }

        private void Instance_OnDisonnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = false;
                this.TestMessage.IsEnabled = this.isConnected;
                this.TestRedeem1.IsEnabled = this.isConnected;
                this.TestRedeem2.IsEnabled = this.isConnected;
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
                _ = QTCore.Instance.Setup();
            }
        }

        private void OnTestMessageClick(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.TestMessage();
        }

        private void TestRedeem1_Click(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.TestRedemption1();
        }

        private void TestRedeem2_Click(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.TestRedemption2();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var twitchOptions = new TwitchOptions()
            {
                IsRedemptionInChat = this.IsRedemptionInChat,
                IsRedemptionTagUser = this.IsTagUserBox.IsChecked ?? false,
                RedemptionTagUser = this.UserNameTextBox.Text,
                IsAutoShoutOutHost = this.IsAutoShoutOutBox.IsChecked ?? false
            };

            QTCore.Instance.SetupTwitchOptions(twitchOptions);
        }

        #endregion Events
    }
}
